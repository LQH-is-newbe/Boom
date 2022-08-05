using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;

public class Util {
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

    public static void StartTransition() {
        GameObject transition = Resources.Load<GameObject>("Transition");
        GameObject.Instantiate(transition);
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

    public static void NotifyServerAddPlayer() {
        Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-join", Static.roomIdJson);
    }

    public static void NotifyServerRemovePlayer() {
        Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-leave", Static.roomIdJson);
    }

}
