using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Aircraft : NetworkBehaviour {
    private const int aircraftDropNum = 5;
    private const float speed = 4f;
   
    private List<Collectable> drops;
    private int nextDropIndex;

    public override void OnNetworkSpawn() {
        if (!IsServer) return;
        Collectable.Type[] types = Collectable.RandomTypes(aircraftDropNum);
        Dictionary<Collectable.Type, int> typesDic = new();
        foreach(Collectable.Type type in types) {
            if (typesDic.ContainsKey(type)) {
                typesDic[type]++;
            } else {
                typesDic[type] = 1;
            }
        }
        drops = Collectable.AssignRandomPosition(typesDic);
        nextDropIndex = 0;
        AppearClientRpc();
    }

    private void Update() {
        if (!IsServer) return;
        transform.position = new(transform.position.x + speed * Time.deltaTime, transform.position.y);
        if (nextDropIndex < aircraftDropNum && transform.position.x > nextDropIndex * Static.mapSize / aircraftDropNum) {
            drops[nextDropIndex].Create(true, transform.position);
            nextDropIndex++;
        } else if (nextDropIndex == aircraftDropNum) {
            nextDropIndex++;
            FadeAwayClientRpc();
            gameObject.AddComponent<Timer>().Init(1f, () => { Destroy(gameObject); });
        }
    }

    [ClientRpc]
    private void AppearClientRpc() {
        AlphaGradient alphaGradient = gameObject.AddComponent<AlphaGradient>();
        alphaGradient.Init(false, GetComponent<SpriteRenderer>(), 0.5f);
    }

    [ClientRpc]
    private void FadeAwayClientRpc() {
        AlphaGradient alphaGradient = gameObject.AddComponent<AlphaGradient>();
        alphaGradient.Init(true, GetComponent<SpriteRenderer>(), 0.5f);
    }
}
