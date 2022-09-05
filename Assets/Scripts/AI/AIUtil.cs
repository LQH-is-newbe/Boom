using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUtil {
    public const float allowedError = 0.06f;
    public const float enterErrorTime = 0.1f;
    public const float preventDis = 0.01f;
    public const float moveLagErrorTime = 0.02f;
    public const float decideTime = 0.1f;

    public static Vector2Int PosToMapBlock(Vector2Int pos) {
        int x = pos.x >= 0 ? pos.x / 2 : -1;
        int y = pos.y >= 0 ? pos.y / 2 : -1;
        return new Vector2Int(x, y);
    }

    public static Vector2 PosToMapPos(Vector2Int pos) {
        Vector2Int mapBlock = PosToMapBlock(pos);
        float mapPosX = mapBlock.x;
        if (pos.x % 2 == 0) mapPosX += preventDis + Character.colliderHalfSize.x;
        else mapPosX += 1 - preventDis - Character.colliderHalfSize.x;
        float mapPosY = mapBlock.y;
        if (pos.y % 2 == 0) mapPosY += preventDis + Character.colliderHalfSize.y;
        else mapPosY += 1 - preventDis - Character.colliderHalfSize.y;
        return new Vector2(mapPosX, mapPosY);
    }

    public static float PosMapManhattanDistance(Vector2Int a, Vector2Int b) {
        Vector2 aMapPos = PosToMapPos(a);
        Vector2 bMapPos = PosToMapPos(b);
        float result = MathF.Abs(aMapPos.x - bMapPos.x) + Mathf.Abs(aMapPos.y - bMapPos.y);
        return result;
    }

    public static bool TestPutBomb(SearchNode node, AIDecideContext aiContext, AIStrategyContext aiStrategyContext) {
        Vector2Int mapBlock = PosToMapBlock(node.pos);
        AIPredictionCharacter character = aiContext.prediction.characters[aiContext.playerId];
        if (character.bombNum.ValueAt(node.time) >= character.bombCapacity || aiContext.prediction.map[mapBlock].Bomb(node.time) != null) return false;
        List<AIPredictionEvent> testAdditionalEvents = new(aiContext.additionalEvents);
        AIPredictionEvent testAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.BombCreate, node.time, new Bomb(mapBlock, character.bombPower, character.id));
        testAdditionalEvents.Add(testAdditionalEvent);
        if (PathFinding.SearchWayOut(character.speed, node.pos, node.time, aiContext.aiPredictionGenerator.Generate(aiContext.playerId, testAdditionalEvents, aiContext.assumeCharacterPutBomb), aiContext.ignoreExplode)) {
            aiStrategyContext.targetNode = node;
            aiStrategyContext.newAdditionalEvent = testAdditionalEvent;
            return true;
        } else {
            return false;
        }
    }

    public static string InstructionsToString(List<Instruction> instructions) {
        string result = "";
        for (int i = 0; i < instructions.Count; ++i) {
            result += instructions[i] + " ";
        }
        return result;
    }

    public static string EventsToString(List<AIPredictionEvent> events) {
        string result = "";
        for (int i = 0; i < events.Count; ++i) {
            result += events[i] + " ";
        }
        return result;
    }
}
