using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bomb: MapElement {
    public const float explodeTime = 2f;
    private static GameObject bombPrefab = Resources.Load<GameObject>("Bomb/Bomb");
    private int bombPower;
    private int createrId;
    public int BombPower { get { return bombPower; } }
    public int CreaterId { get { return createrId; } }

    public Bomb(Vector2Int mapPos, int bombPower, int createrId): base(mapPos) {
        this.bombPower = bombPower;
        this.createrId = createrId;
    }

    public void Create() {
        Character.characters[createrId].BombNum++;
        Static.mapBlocks[MapBlock].element = this;
        GameObject bomb = UnityEngine.Object.Instantiate(bombPrefab, new(MapBlock.x, MapBlock.y), Quaternion.identity);
        BombController controller = bomb.GetComponent<BombController>();
        Static.controllers[this] = controller;
        controller.bomb = this;
        bomb.GetComponent<NetworkObject>().Spawn(true);
    }

    public void CreatePrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.ChangeBomb(this, time);
        prediction.characters[CreaterId].bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum + 1; });
        events.Add(new(MapBlock, AIPredictionEvent.Type.BombExplode, time + explodeTime, this), time + explodeTime);
    }


    public void Trigger() {
        Character creater;
        if (Character.characters.TryGetValue(createrId, out creater)) {
            creater.BombNum--;
        }
        ((BombController)Static.controllers[this]).Destroy();
        Static.mapBlocks[MapBlock].element = null;
        Static.controllers.Remove(this);
        Explode explode = new(MapBlock, BombPower, Direction.zero);
        explode.Create();
    }

    public void TriggerPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        AIPredictionCharacter creater;
        if (prediction.characters.TryGetValue(createrId, out creater)) {
            creater.bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum - 1; }); ;
        }
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        Explode explode = new Explode(MapBlock, BombPower, Direction.zero);
        predictionMapBlock.AddExplodeStart(time);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeExtend, time + Explode.explodeInterval, explode), time + Explode.explodeInterval);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeDestroy, time + explode.ExistTime, explode), time + explode.ExistTime);
        predictionMapBlock.ChangeBomb(null, time);
    }
}
