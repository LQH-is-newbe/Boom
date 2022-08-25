using Unity.Netcode;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UNET;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Netcode.Transports.WebSocket;

public class Util {
    public static void SetNetworkTransport(bool useWebSocket, string address, ushort port) {
        if (useWebSocket) {
            NetworkManager.Singleton.GetComponent<UNetTransport>().enabled = false;
            WebSocketTransport webSocketTransport = NetworkManager.Singleton.GetComponent<WebSocketTransport>();
            webSocketTransport.enabled = true;
            webSocketTransport.ConnectAddress = address;
            webSocketTransport.Port = port;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.GetComponent<WebSocketTransport>();
        } else {
            NetworkManager.Singleton.GetComponent<WebSocketTransport>().enabled = false;
            UNetTransport uNetTransport = NetworkManager.Singleton.GetComponent<UNetTransport>();
            uNetTransport.enabled = true;
            uNetTransport.MessageBufferSize = 61440;
            uNetTransport.ConnectAddress = address;
            uNetTransport.ConnectPort = port;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        }
    }

    public static void PauseGame(bool pause) {
        Time.timeScale = pause ? 0 : 1;
        Static.paused = pause;
    }

    public static T Sync<T>(Task<T> task) {
        T result = default;
        task.ContinueWith(
            (task) => {
                result = task.Result;
            }
        ).Wait();
        return result;
    }

    public static UnityWebRequest WebRequest(string url, object body = null) {
        string bodyJSON = body != null ? JsonConvert.SerializeObject(body) : "";
        byte[] bytes = Encoding.UTF8.GetBytes(bodyJSON);
        UnityWebRequest request = new (url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    public static IEnumerator WebRequestCoroutine(UnityWebRequest request, Action<long, string> callback) {
        yield return request.SendWebRequest();
        callback(request.responseCode, request.downloadHandler.text);
    }

    public static IEnumerator JoinRoomCoroutine(int roomId, string password, Action<string> callback) {
        var requestBody = new { roomId, password, numPlayers = Static.playerNames.Count };
        return WebRequestCoroutine(WebRequest("http://" + Static.httpServerAddress + "/join-room", requestBody), (statusCode, responseBody) => {
            if (statusCode == 200) {
                StartTransition();
                JoinRoomReturn joinRoomReturn = JsonConvert.DeserializeObject<JoinRoomReturn>(responseBody);
                JoinRoom(joinRoomReturn);
            } else {
                callback(responseBody);
            }
        });
    }

    public static void JoinRoom(JoinRoomReturn joinRoomReturn) {
        StartTransition();
        SetNetworkTransport(true, Static.httpServerAddress, joinRoomReturn.port);
        ConnectionData connectionData = new();
        connectionData.passcode = joinRoomReturn.passcode;
        connectionData.playerNames = Static.playerNames;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        NetworkManager.Singleton.StartClient();
    } 

    public static ClientRpcParams GetClientRpcParamsExcept(ulong clientId) {
        List<ulong> sendIds = new();
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
            if (id != clientId) sendIds.Add(id);
        }
        return new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = sendIds.ToArray()
            }
        };
    }

    public static ClientRpcParams GetClientRpcParamsFor(ulong clientId) {
        List<ulong> sendIds = new();
        sendIds.Add(clientId);
        return new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = sendIds.ToArray()
            }
        };
    }

    public static void StartTransition() {
        GameObject transition = Resources.Load<GameObject>("Transition");
        UnityEngine.Object.Instantiate(transition);
    }

    public static string GetLocalIPAddress() {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

}
