using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class Util {
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

    public static string JoinRoom(int roomId, string password = null) {
        var requestBody = new { roomId, password, numPlayers = Static.playerNames.Count };
        var stringContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var response = Sync(Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/join-room", stringContent));
        string responseString = Sync(response.Content.ReadAsStringAsync());
        if (response.StatusCode == HttpStatusCode.Forbidden) {
            return responseString;
        }
        StartTransition();
        var returnBody = JsonConvert.DeserializeObject<JoinRoomReturn>(responseString);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Static.httpServerAddress, returnBody.port);
        ConnectionData connectionData = new();
        connectionData.passcode = returnBody.passcode;
        connectionData.playerNames = Static.playerNames;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        NetworkManager.Singleton.StartClient();
        return "success";
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
