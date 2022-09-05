using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CharacterController : NetworkBehaviour {
    private const float blockedErrorStart = 0.2f;
    private const float blockedErrorEnd = 0.5f;
    private const float blinkSolidTime = 0.1f;
    private const float blinkTransparentTime = 0.1f;

    private readonly NetworkVariable<FixedString64Bytes> characterName = new();
    private readonly NetworkVariable<bool> isNPC = new();
    private readonly NetworkVariable<int> id = new();
    private readonly NetworkVariable<int> clientPlayerId = new();
    public NetworkVariable<bool> alive = new(true);
    public NetworkVariable<float> speed = new(Character.initialSpeed);

    [SerializeField]
    private GameObject display;
    [SerializeField]
    private SpriteRenderer circle;

    private NetworkAnimator networkAnimator;
    private Animator animator;
    private CharacterAvatarHealth characterAvatarHealth;

    public Character character;

    public override void OnNetworkSpawn() {
        networkAnimator = display.GetComponent<NetworkAnimator>();
        GetComponent<BoxCollider2D>().size = Character.colliderHalfSize * 2;
        if (IsOwner) {
            if (isNPC.Value) {
                gameObject.AddComponent<BotController>();
            } else {
                PlayerController playerController = gameObject.AddComponent<PlayerController>();
                playerController.Init(clientPlayerId.Value);
            }
        }
        if (IsServer) {
            gameObject.tag = "Character";
        }
        if (IsClient) {
            circle.color = Character.colors[id.Value];
            animator = display.GetComponent<Animator>();
            display.transform.localPosition = new Vector2(0, -Character.colliderHalfSize.y);
            AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animatorOverrideController["Dead"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Dead");
            animatorOverrideController["Hit_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Hit_left");
            animatorOverrideController["Hit_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Hit_right");
            animatorOverrideController["Idle_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Idle_left");
            animatorOverrideController["Idle_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Idle_right");
            animatorOverrideController["Walk_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Walk_left");
            animatorOverrideController["Walk_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Walk_right");
            animator.runtimeAnimatorController = animatorOverrideController;
        }
    }

    public void Init(Character character, CharacterAvatarHealth characterAvatarHealth) {
        this.character = character;
        characterName.Value = new FixedString64Bytes(character.CharacterName);
        isNPC.Value = character.IsNPC;
        id.Value = character.Id;
        this.characterAvatarHealth = characterAvatarHealth;
        clientPlayerId.Value = character.ClientPlayerId;
    }

    // move

    public void Move(Direction direction, float positionChangeDistance = 0) {
        if (direction == Direction.zero) {
            networkAnimator.SetAnimation("Moving", false);
            return;
        }
        if (direction == Direction.left) networkAnimator.SetAnimation("Direction", -1f);
        else if (direction == Direction.right) networkAnimator.SetAnimation("Direction", 1f);
        networkAnimator.SetAnimation("Moving", true);
        Vector2 curMapPos = transform.position;
        Vector2 newMapPos = curMapPos + direction.Vector2 * positionChangeDistance;
        bool canGo = true;
        foreach (Vector2 vertexPos in Character.directionVertices[direction.index]) {
            Vector2 newVertexMapPos = newMapPos + vertexPos;
            if (newVertexMapPos.x < 0 || newVertexMapPos.x > Static.mapSize || newVertexMapPos.y < 0 || newVertexMapPos.y > Static.mapSize) return;
            Vector2 curVertexMapPos = curMapPos + vertexPos;
            Vector2Int curVertexMapBlock = Util.MapPosToMapBlock(curVertexMapPos);
            Vector2Int newVertexMapBlock = Util.MapPosToMapBlock(newVertexMapPos);
            if (!curVertexMapBlock.Equals(newVertexMapBlock) && Static.hasObstacle[newVertexMapBlock]) {
                canGo = false;
            }
        }
        Vector2 positionChange;
        if (canGo) {
            positionChange = direction.Vector2 * positionChangeDistance;
        } else {
            Vector2 newBoundaryMidPoint = newMapPos + Character.boundaryMidPoints[direction.index];
            Vector2Int newMapBlock = Util.MapPosToMapBlock(newBoundaryMidPoint);
            Vector2Int newNeighborMapBlock;
            if (direction.horizontal) {
                if (newBoundaryMidPoint.y > newMapBlock.y + 0.5f) newNeighborMapBlock = newMapBlock + Vector2Int.up;
                else newNeighborMapBlock = newMapBlock + Vector2Int.down;
            } else {
                if (newBoundaryMidPoint.x > newMapBlock.x + 0.5f) newNeighborMapBlock = newMapBlock + Vector2Int.right;
                else newNeighborMapBlock = newMapBlock + Vector2Int.left;
            }
            if (newNeighborMapBlock.x < 0 || newNeighborMapBlock.x >= Static.mapSize || newNeighborMapBlock.y < 0 || newNeighborMapBlock.y >= Static.mapSize) return;
            // So there must be two blocks
            if (Static.hasObstacle[newMapBlock] && Static.hasObstacle[newNeighborMapBlock]) return;
            Vector2Int nonEmptyBlock;
            Vector2Int emptyBlock;
            if (Static.hasObstacle[newMapBlock]) {
                nonEmptyBlock = newMapBlock;
                emptyBlock = newNeighborMapBlock;
            } else {
                nonEmptyBlock = newNeighborMapBlock;
                emptyBlock = newMapBlock;
            }
            float shiftToMidPoint = Mathf.Abs(direction.horizontal ? newBoundaryMidPoint.y - nonEmptyBlock.y - 0.5f : newBoundaryMidPoint.x - nonEmptyBlock.x - 0.5f);
            float distanceMultiplier;
            if (shiftToMidPoint < blockedErrorStart) return;
            else if (shiftToMidPoint < blockedErrorEnd) distanceMultiplier = (shiftToMidPoint - blockedErrorStart) / (blockedErrorEnd - blockedErrorStart);
            else distanceMultiplier = 1;
            Vector2Int curMapBlock = Util.MapPosToMapBlock(curMapPos);
            Vector2Int curNeighborMapBlock = direction.horizontal ? new(curMapBlock.x, newNeighborMapBlock.y) : new(newNeighborMapBlock.x, curMapBlock.y);
            if (Static.hasObstacle[curNeighborMapBlock]) return;
            positionChange = (emptyBlock - nonEmptyBlock) * positionChangeDistance * distanceMultiplier;
        }
        Vector2 decidedNewMapPos = curMapPos + positionChange;
        transform.position = decidedNewMapPos;
        if (IsServer) MoveServerCall(decidedNewMapPos.x, decidedNewMapPos.y);
        else MoveServerRpc(decidedNewMapPos.x, decidedNewMapPos.y);
        if (positionChange.x < 0) {
            networkAnimator.SetAnimation("Direction", -1f);
        } else if (positionChange.x > 0) {
            networkAnimator.SetAnimation("Direction", 1f);
        }
    }

    private void MoveServerCall(float x, float y) {
        character.Position = new(x, y);
        transform.position = new(x, y);
        MoveClientRpc(x, y, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    private void MoveServerRpc(float x, float y) {
        MoveServerCall(x, y);
    }

    [ClientRpc]
    private void MoveClientRpc(float x, float y, ClientRpcParams clientRpcParams = default) {
        transform.position = new Vector2(x, y);
    }

    // put bomb

    public void PutBomb() {
        if (IsServer) {
            PutBombServerCall();
        } else {
            PutBombServerRpc();
        }
    }

    private void PutBombServerCall() {
        character.PutBomb();
    }

    [ServerRpc]
    private void PutBombServerRpc() {
        PutBombServerCall();
    }

    // other

    public void Hit() {
        gameObject.AddComponent<Timer>().Init(Character.timeInvincible, () => { character.IsInvincible = false; });
        networkAnimator.AnimationClientRpc("Hit");
        BlinkClientRpc();
    }

    public void ChangeHealth(int health) {
        characterAvatarHealth.lives.Value = health;
    }

    public void Dead() {
        networkAnimator.AnimationClientRpc("Hit");
        alive.Value = false;
        gameObject.AddComponent<Timer>().Init(0.1f, () => { networkAnimator.AnimationClientRpc("Dead", true); });
        gameObject.AddComponent<Timer>().Init(3, () => { Destroy(gameObject); });
    }

    [ClientRpc]
    private void BlinkClientRpc() {
        gameObject.AddComponent<Blink>().Init(display.GetComponent<SpriteRenderer>(), Character.timeInvincible, blinkSolidTime, blinkTransparentTime);
    }
}
