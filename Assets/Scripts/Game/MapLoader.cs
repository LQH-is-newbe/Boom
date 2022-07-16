using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class MapLoader : NetworkBehaviour {
    public GameObject destroyablePrefab;
    public GameObject NoneDestroyablePrefab;

    public void LoadMap() {
        var dataset = Resources.Load<TextAsset>("Maps/map");
        var dataLines = dataset.text.Split('\n');
        int n = dataLines.Length - 1;
        for (int i = 0; i < n; i++) {
            var data = dataLines[i].Split();
            for (int j = 0; j < n; j++) {
                if (!string.Equals(data[j], "0")) {
                    GameObject block = null;
                    if (string.Equals(data[j], "1") || string.Equals(data[j], "2")) {
                        block = Instantiate(NoneDestroyablePrefab, new Vector2(j, n - i - 1), Quaternion.identity);
                    } else {
                        block = Instantiate(destroyablePrefab, new Vector2(j, n - i - 1), Quaternion.identity);
                    }
                    block.GetComponent<SpriteLoader>().path.Value = new FixedString64Bytes("Maps/Blocks/Sprites/" + data[j]);
                    block.transform.SetParent(transform);
                    block.GetComponent<NetworkObject>().Spawn();
                    Static.map[j, n - i - 1] = block;
                }
            }
        }
    }
}
