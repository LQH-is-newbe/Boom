using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CharacterController : NetworkBehaviour {
    private NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<bool> alive = new(true);
    private NetworkVariable<bool> isNPC = new();
    public NetworkVariable<float> speed = new(Character.initialSpeed);
    private NetworkVariable<int> id = new();

    [SerializeField]
    private GameObject display;
    [SerializeField]
    private SpriteRenderer circle;

    private Rigidbody2D rigidbody2d;
    private NetworkAnimator networkAnimator;
    private Animator animator;
    private CharacterAvatarHealth characterAvatarHealth;

    private float invincibleTimer;
    private float deadTimer = -1;
    public Character character;

    private void Update() {
        if (IsOwner && transform.hasChanged) {
            if (IsServer) MoveServerCall(transform.position.x, transform.position.y);
            else MoveServerRpc(transform.position.x, transform.position.y);
        }
        if (IsServer) {
            if (invincibleTimer > 0) {
                invincibleTimer -= Time.deltaTime;
                if (invincibleTimer <= 0) {
                    character.IsInvincible = false;
                }
            }
            if (deadTimer > 0) {
                deadTimer -= Time.deltaTime;
                if (deadTimer <= 0) {
                    Destroy(gameObject);
                }
            }
        }
    }

    public override void OnNetworkSpawn() {
        networkAnimator = display.GetComponent<NetworkAnimator>();
        GetComponent<BoxCollider2D>().size = Static.characterColliderSize;
        if (IsOwner) {
            rigidbody2d = GetComponent<Rigidbody2D>();
            if (isNPC.Value) {
                gameObject.AddComponent<BotController>();
            } else {
                gameObject.AddComponent<PlayerController>();
            }
        }
        if (IsServer) {
            gameObject.tag = "Character";
        }
        if (IsClient) {
            circle.color = Character.characterColors[id.Value];
            animator = display.GetComponent<Animator>();
            display.transform.localPosition = new Vector2(0, -Static.characterColliderSize.y / 2);
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
    }

    public void Move(Vector2 positionChange) {
        if (positionChange.magnitude != 0) {
            rigidbody2d.MovePosition(rigidbody2d.position + positionChange);
            if (positionChange.x < 0) {
                networkAnimator.SetAnimation("Direction", -1f);
            } else if (positionChange.x > 0) {
                networkAnimator.SetAnimation("Direction", 1f);
            }
            networkAnimator.SetAnimation("Moving", true);
        } else {
            networkAnimator.SetAnimation("Moving", false);
        }
    }

    public void PutBomb() {
        if (IsServer) PutBombServerCall();
        else PutBombServerRpc();
    }

    public void PutBombServerCall() {
        character.PutBomb();
    }

    [ServerRpc]
    public void PutBombServerRpc() {
        PutBombServerCall();
    }

    public void Hit() {
        invincibleTimer = Character.timeInvincible;
        networkAnimator.AnimationClientRpc("Hit");
    }

    public void ChangeHealth(int health) {
        characterAvatarHealth.lives.Value = health;
    }

    public void Dead() {
        networkAnimator.AnimationClientRpc("Dead", true);
        alive.Value = false;
        deadTimer = 3;
    }

    public void MoveServerCall(float x, float y) {
        character.Position = new(x, y);
        transform.position = new(x, y);
        MoveClientRpc(x, y, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    public void MoveServerRpc(float x, float y) {
        MoveServerCall(x, y);
    }

    [ClientRpc]
    public void MoveClientRpc(float x, float y, ClientRpcParams clientRpcParams = default) {
        transform.position = new Vector2(x, y);
    }
}
