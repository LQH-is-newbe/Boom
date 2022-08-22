using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bomb: MapElement {
    public const float explodeTime = 2f;
    private static readonly GameObject bombPrefab = Resources.Load<GameObject>("Bomb/Bomb");
    public int BombPower { get; }
    public int CreaterId { get; }

    public Bomb(Vector2Int mapPos, int bombPower, int createrId): base(mapPos) {
        BombPower = bombPower;
        CreaterId = createrId;
    }

    public void Create() {
        Character creater = Character.characters[CreaterId];
        creater.BombNum++;
        Static.mapBlocks[MapBlock].element = this;
        GameObject bomb = UnityEngine.Object.Instantiate(bombPrefab, new(MapBlock.x, MapBlock.y), Quaternion.identity);
        bomb.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Bomb/Sprites/" + creater.BombName);
        BombController controller = bomb.GetComponent<BombController>();
        Static.controllers[this] = controller;
        controller.bomb = this;
        bomb.GetComponent<NetworkObject>().Spawn(true);
    }

    public void CreatePrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.ChangeBomb(this, time);
        prediction.characters[CreaterId].bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum + 1; });
        events.Add(new(MapBlock, AIPredictionEvent.Type.BombTrigger, time + explodeTime, this), time + explodeTime);
    }

    public void Trigger() {
        if (Character.characters.TryGetValue(CreaterId, out Character creater)) {
            creater.BombNum--;
        }
        ((BombController)Static.controllers[this]).Destroy();
        Static.mapBlocks[MapBlock].element = null;
        Static.controllers.Remove(this);
        Explode explode = new(MapBlock, BombPower, Direction.zero);
        explode.Create();
    }

    public void TriggerPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent> events, float time) {
        if (prediction.characters.TryGetValue(CreaterId, out AIPredictionCharacter creater)) {
            creater.bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum - 1; }); ;
        }
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        Explode explode = new Explode(MapBlock, BombPower, Direction.zero);
        predictionMapBlock.AddExplodeStart(time);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeExtend, time + Explode.extendTime, explode), time + Explode.extendTime);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeDestroy, time + explode.ExistTime + AI.enterErrorTime, explode), time + explode.ExistTime + AI.enterErrorTime);
        predictionMapBlock.ChangeBomb(null, time);
    }
}
