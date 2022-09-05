using System.Collections.Generic;

public abstract class AIStrategy {
    public bool putBomb;
    public bool waitRemain;
}

public abstract class StandAloneAIStrategy: AIStrategy {
    public abstract void Run(AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext);
}

public abstract class ComponentAIStrategy : AIStrategy {
    public bool searching = true;

    public abstract bool Found(SearchNode node, AIDecideContext aiContext, AIStrategyContext aiStrategyContext);

    public abstract float Score(float time, AIDecideContext aiDecideContext);
}

public class ClosestCharacter: StandAloneAIStrategy {
    public ClosestCharacter() {
        putBomb = true;
        waitRemain = false;
    }

    public override void Run(AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext) {
        AIPredictionCharacter character = aiDecideContext.prediction.characters[aiDecideContext.playerId];
        List<AIPredictionCharacter> otherCharacters = new();
        foreach (int characterId in aiDecideContext.prediction.characters.Keys) {
            if (characterId != aiDecideContext.playerId) otherCharacters.Add(aiDecideContext.prediction.characters[characterId]);
        }
        otherCharacters.Sort((a, b) => { return AIUtil.PosMapManhattanDistance(a.pos, character.pos).CompareTo(AIUtil.PosMapManhattanDistance(b.pos, character.pos)); });
        int movesWithin = (character.bombPower - 1) * 2;
        float Score(float time) { return 3 - 2 * time; }
        foreach (AIPredictionCharacter otherCharacter in otherCharacters) {
            if (Score((AIUtil.PosMapManhattanDistance(otherCharacter.pos, character.pos) - movesWithin / 2) / character.speed) < aiStrategyContext.highestScore) continue;
            bool OnNodePopCharacter(SearchNode node) {
                if (otherCharacter.pos.Equals(node.pos) 
                    && PathFinding.SearchWayOut(
                        character.speed, 
                        node.pos, 
                        node.time, 
                        aiDecideContext.prediction, 
                        aiDecideContext.ignoreExplode)) {
                    int moveCount = 0;
                    SearchNode cur = node;
                    List<SearchNode> path = new();
                    while (cur != null && moveCount < movesWithin) {
                        ++moveCount;
                        path.Add(cur);
                        cur = cur.prev;
                    }
                    while (path.Count > 0) {
                        SearchNode curNode = path[^1];
                        path.RemoveAt(path.Count - 1);
                        if (Score(curNode.time) <= aiStrategyContext.highestScore) break;
                        if (character.bombNum.ValueAt(curNode.time) < character.bombCapacity && AIUtil.TestPutBomb(curNode, aiDecideContext, aiStrategyContext)) {
                            aiStrategyContext.highestScore = Score(curNode.time);
                            aiStrategyContext.targetStrategy = this;
                            break;
                        }
                    }
                    return true;
                }
                return false;
            }
            float Heauristic(SearchNode node) { return AIUtil.PosMapManhattanDistance(node.pos, otherCharacter.pos) / character.speed; }
            PathFinding.Search(
                character.speed, 
                aiDecideContext.source, 
                aiDecideContext.initTime, 
                aiDecideContext.prediction, 
                aiDecideContext.ignoreExplode, 
                out _, 
                OnNodePop: OnNodePopCharacter, 
                Heuristic: Heauristic);
        }
    }
}
public class ClosestDestroyableNeighbor: ComponentAIStrategy {
    public ClosestDestroyableNeighbor() {
        putBomb = true;
        waitRemain = false;
    }

    public override bool Found(SearchNode node, AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext) {
        AIPredictionCharacter character = aiDecideContext.prediction.characters[aiDecideContext.playerId];
        if (character.bombNum.ValueAt(node.time) >= character.bombCapacity) return false;
        Vector2Int curMapBlock = AIUtil.PosToMapBlock(node.pos);
        for (int i = 0; i < 4; ++i) {
            Vector2Int nextMapBlock = curMapBlock + Direction.directions[i].Vector2Int;
            if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;
            if (aiDecideContext.prediction.map[nextMapBlock].IsNotExplodedDestroyable(node.time + Bomb.explodeTime + Explode.extendTime) 
                && AIUtil.TestPutBomb(node, aiDecideContext, aiStrategyContext)) {
                return true;
            }
        }
        return false;
    }

    public override float Score(float time, AIDecideContext aiContext) {
        return aiContext.prediction.destroyableNum.ValueAt(time) > 0 ? 1 - time : -100;
    }
}

public class ClosestPossibleCollectable : ComponentAIStrategy {
    public ClosestPossibleCollectable() {
        putBomb = false;
        waitRemain = false;
    }

    public override bool Found(SearchNode node, AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext) {
        AIPredictionCharacter character = aiDecideContext.prediction.characters[aiDecideContext.playerId];
        Vector2Int mapBlock = AIUtil.PosToMapBlock(node.pos);
        AIPredictionMapBlock predictionMapBlock = aiDecideContext.prediction.map[mapBlock];
        if (predictionMapBlock.IsAfterDestroyableDestroyed(node.time) && !predictionMapBlock.IsVisited(node.time) 
            && PathFinding.SearchWayOut(character.speed, node.pos, node.time, aiDecideContext.prediction, aiDecideContext.ignoreExplode)) {
            aiStrategyContext.targetNode = node;
            aiStrategyContext.newAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.CharacterVisit, node.time);
            return true;
        }
        return false;
    }

    public override float Score(float time, AIDecideContext aiContext) {
        bool willHavePossibleCollectable = !aiContext.prediction.possibleCollectableNum.ValueBetweenTimeSatisfies((num) => { return num == 0; }, time);
        return willHavePossibleCollectable ? 2f - time : -100;
    }
}

public class ClosestCollectable : ComponentAIStrategy {
    public ClosestCollectable() {
        putBomb = false;
        waitRemain = false;
    }

    public override bool Found(SearchNode node, AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext) {
        AIPredictionCharacter character = aiDecideContext.prediction.characters[aiDecideContext.playerId];
        Vector2Int mapBlock = AIUtil.PosToMapBlock(node.pos);
        AIPredictionMapBlock predictionMapBlock = aiDecideContext.prediction.map[mapBlock];
        if (predictionMapBlock.IsCollectable(node.time) 
            && PathFinding.SearchWayOut(character.speed, node.pos, node.time, aiDecideContext.prediction, aiDecideContext.ignoreExplode)) {
            aiStrategyContext.targetNode = node;
            aiStrategyContext.newAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.CharacterVisit, node.time);
            return true;
        }
        return false;
    }

    public override float Score(float time, AIDecideContext aiContext) {
        return aiContext.prediction.collectableNum.ValueAt(time) > 0 ? 3 - time : -100;
    }
}

public class ClosestSafe : ComponentAIStrategy {
    public ClosestSafe() {
        putBomb = false;
        waitRemain = true;
    }

    public override bool Found(SearchNode node, AIDecideContext aiDecideContext, AIStrategyContext aiStrategyContext) {
        if (aiDecideContext.prediction.map[AIUtil.PosToMapBlock(node.pos)].IsSafe(node.time)) {
            aiStrategyContext.targetNode = node;
            aiStrategyContext.newAdditionalEvent = null;
            return true;
        } else {
            return false;
        }
    }

    public override float Score(float time, AIDecideContext aiContext) {
        return -98;
    }
}