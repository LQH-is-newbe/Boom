using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;

public enum Direction {
    Left = 0,
    Right = 1,
    Up = 2,
    Down = 3,
    None = 4
}

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
    //Vector2Int mapBlock = PosToMapBlock(pos);
    //Collectable closestCollectable = null;
    //int closestCollectableDis = 100;
    //foreach (Collectable collectable in Static.collectables) {
    //    int distance = Mathf.Abs(mapBlock.x - collectable.MapPos.x) + Mathf.Abs(mapBlock.y - collectable.MapPos.y);
    //    if (distance < closestCollectableDis) {
    //        closestCollectableDis = distance;
    //        closestCollectable = collectable;
    //    }
    //}
    //Destroyable closestDestroyable = null;
    //int closestDestroyableDis = 100;
    //foreach (Destroyable destroyable in Static.destroyables) {
    //    int distance = Mathf.Abs(mapBlock.x - destroyable.MapPos.x) + Mathf.Abs(mapBlock.y - destroyable.MapPos.y);
    //    if (distance < closestDestroyableDis) {
    //        closestDestroyableDis = distance;
    //        closestDestroyable = destroyable;
    //    }
    //}

}
