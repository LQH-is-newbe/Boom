using System.Collections.Generic;
using UnityEngine;

public class AIDecide: TaskNode {
    private readonly AIContext aiContext;

    public AIDecide(AIContext aiContext) {
        this.aiContext = aiContext;
    }

    private void GetInstructions(SearchNode node, bool putBomb, List<Instruction> currentInstructions) {
        List<Instruction> instructions = new();
        SearchNode cur = node;
        if (putBomb) {
            instructions.Add(new(cur.pos, cur.time, putBomb: true));
        }
        while (cur.prev != null) {
            instructions.Add(new(cur.pos, cur.time));
            if (cur.waitTime > 0) {
                instructions.Add(new(cur.prev.pos, cur.prev.time + cur.waitTime, waitTime: cur.waitTime));
            }
            cur = cur.prev;
        }
        int i = instructions.Count - 1;
        float time = 0;
        while (i >= 0 && instructions[i].time < AIUtil.decideTime) {
            time = instructions[i].time;
            currentInstructions.Add(instructions[i--]);
        }
        if (i >= 0) {
            if (instructions[i].waitTime > 0) {
                Instruction waitRemain = new(instructions[i].pos, AIUtil.decideTime, waitTime: AIUtil.decideTime - time);
                currentInstructions.Add(waitRemain);
            } else {
                currentInstructions.Add(instructions[i]);
                if (i - 1 >= 0 && instructions[i - 1].putBomb) currentInstructions.Add(instructions[i - 1]);
            }
        }
    }

    public override State Evaluate() {
        bool waitRemain = false;
        AIDecideContext aiDecideContext = new();
        aiDecideContext.aiPredictionGenerator = new(0, aiContext.timeRemain);
        aiDecideContext.playerId = aiContext.playerId;
        aiDecideContext.source = aiContext.pos;
        aiDecideContext.initTime = 0;
        int times = 0;
        aiContext.currentInstructions = new();
        while (aiContext.currentInstructions.Count == 0 || aiContext.currentInstructions[^1].time < AIUtil.decideTime) {
            if (times++ > 10) {
                Debug.Log("decide loop");
                break;
            }
            if (waitRemain) {
                Instruction waitRemainInstruction = aiContext.currentInstructions.Count > 0 ?
                            new(aiContext.currentInstructions[^1].pos, AIUtil.decideTime, waitTime: AIUtil.decideTime - aiContext.currentInstructions[^1].time) :
                            new(aiDecideContext.source, AIUtil.decideTime, waitTime: AIUtil.decideTime);
                aiContext.currentInstructions.Add(waitRemainInstruction);
                break;
            }
            AIPrediction prediction = aiDecideContext.aiPredictionGenerator.Generate(aiDecideContext.playerId, aiDecideContext.additionalEvents, aiDecideContext.assumeCharacterPutBomb);
            aiDecideContext.prediction = prediction;
            AIStrategyContext aiStrategyContext = new();
            AIPredictionCharacter character = prediction.characters[aiDecideContext.playerId];
            new ClosestCharacter().Run(aiDecideContext, aiStrategyContext);
            ComponentAIStrategy[] strategies = { new ClosestDestroyableNeighbor(), new ClosestCollectable(), new ClosestPossibleCollectable(), new ClosestSafe() };
            int searchingTargets = strategies.Length;
            bool OnNodePopOthers(SearchNode node) {
                foreach (ComponentAIStrategy strategy in strategies) {
                    if (!strategy.searching) continue;
                    float score = strategy.Score(node.time, aiDecideContext);
                    if (score <= aiStrategyContext.highestScore) {
                        strategy.searching = false;
                        --searchingTargets;
                    } else if (strategy.Found(node, aiDecideContext, aiStrategyContext)) {
                        aiStrategyContext.targetStrategy = strategy;
                        strategy.searching = false;
                        --searchingTargets;
                        aiStrategyContext.highestScore = score;
                    }
                }
                return searchingTargets == 0;
            }

            PathFinding.Search(character.speed, aiDecideContext.source, aiDecideContext.initTime, prediction, aiDecideContext.ignoreExplode, out FastestNodes fastestNodes, OnNodePop: OnNodePopOthers);

            if (aiStrategyContext.targetNode == null) {
                if (aiDecideContext.assumeCharacterPutBomb) {
                    aiDecideContext.assumeCharacterPutBomb = false;
                } else if (!aiDecideContext.ignoreExplode) {
                    aiDecideContext.ignoreExplode = true;
                } else {
                    aiContext.currentInstructions.Add(new(aiDecideContext.source, AIUtil.decideTime, waitTime: AIUtil.decideTime));
                }
            } else {
                GetInstructions(aiStrategyContext.targetNode, aiStrategyContext.targetStrategy.putBomb, aiContext.currentInstructions);
                float lastInstructionTime = aiContext.currentInstructions.Count > 0 ? aiContext.currentInstructions[^1].time : aiDecideContext.initTime;
                if (aiStrategyContext.newAdditionalEvent != null && aiStrategyContext.newAdditionalEvent.time <= lastInstructionTime) aiDecideContext.additionalEvents.Add(aiStrategyContext.newAdditionalEvent);
                waitRemain = aiStrategyContext.targetStrategy.waitRemain;
                aiDecideContext.source = aiStrategyContext.targetNode.pos;
                aiDecideContext.initTime = aiStrategyContext.targetNode.time;
            }
        }
        return State.SUCCESS;
    }
}

public class AIDecideContext {
    public List<AIPredictionEvent> additionalEvents = new();
    public bool assumeCharacterPutBomb = true;
    public bool ignoreExplode = false;
    public AIPredictionGenerator aiPredictionGenerator;
    public int playerId;
    public AIPrediction prediction;
    public Vector2Int source;
    public float initTime;
}

public class AIStrategyContext {
    public float highestScore = -99;
    public AIStrategy targetStrategy;
    public SearchNode targetNode;
    public AIPredictionEvent newAdditionalEvent;
}
