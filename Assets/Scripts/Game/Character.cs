using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Character : NetworkBehaviour {
    public const float initialSpeed = 4;
    public const int initialBombPower = 4;
    public const int initialBombCapacity = 100;
    public const int maxHealth = 3;
    public const float timeInvincible = 1;

    private NetworkVariable<FixedString64Bytes> characterName = new();
    private NetworkVariable<bool> alive = new(true);
    private NetworkVariable<bool> isNPC = new();

    [SerializeField]
    private GameObject bombPrefab;
    [SerializeField]
    private GameObject display;

    private Rigidbody2D rigidbody2d;
    private NetworkAnimator networkAnimator;
    private GameStateController gameStateController;
    private Animator animator;
    private CharacterAvatarHealth characterAvatarHealth;

    public Vector2 Position { get { return transform.position; } }
    private float speed;
    public float Speed { get { return speed; } set { speed = value; ChangeSpeedClientRpc(value); } }
    public int BombPower { get; set; }
    public int BombCapacity { get; set; }
    private int health;
    public int BombNum { get; set; }
    private float invincibleTimer;
    public int Id { get; set; }
    private float deadTimer = -1;

    private void Update() {
        if (!IsServer) return;
        if (invincibleTimer > 0) {
            invincibleTimer -= Time.deltaTime;
        }
        if (deadTimer > 0) {
            deadTimer -= Time.deltaTime;
            if (deadTimer < 0) {
                Destroy(gameObject);
            }
        }
    }

    public override void OnNetworkSpawn() {
        networkAnimator = display.GetComponent<NetworkAnimator>();
        GetComponent<BoxCollider2D>().size = Static.characterColliderSize;
        Speed = initialSpeed;
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
            BombPower = initialBombPower;
            BombCapacity = initialBombCapacity;
            BombNum = 0;
            health = maxHealth;
            gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateController>();
        }
        if (IsClient) {
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

    public void Init(Player player, CharacterAvatarHealth characterAvatarHealth) {
        Id = player.Id;
        characterName.Value = new FixedString64Bytes(player.CharacterName);
        isNPC.Value = player.IsNPC;
        this.characterAvatarHealth = characterAvatarHealth;
        player.Character = this;
    }

    public void Move(Vector2 positionChange) {
        if (!alive.Value || !IsOwner) return;
        if (positionChange.magnitude != 0) {
            rigidbody2d.MovePosition(rigidbody2d.position + positionChange);
            if (IsServer) MoveServerCall(rigidbody2d.position.x, rigidbody2d.position.y);
            else MoveServerRpc(rigidbody2d.position.x, rigidbody2d.position.y);
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
        if (!alive.Value || !IsOwner) return;
        if (IsServer) PutBombServerCall();
        else PutBombServerRpc();
    }

    public void PutBombServerCall() {
        if (!alive.Value || BombNum >= BombCapacity) return;
        int x = (int)Mathf.Floor(transform.position.x);
        int y = (int)Mathf.Floor(transform.position.y);
        Vector2Int pos = new(x, y);
        if (Static.map[pos] == null) {
            GameObject bomb = Instantiate(bombPrefab, new Vector2(x, y), Quaternion.identity);
            BombController bombController = bomb.GetComponent<BombController>();
            bombController.Init(this, BombPower);
            bomb.GetComponent<NetworkObject>().Spawn(true);
            ++BombNum;
        }
    }

    [ServerRpc]
    public void PutBombServerRpc() {
        PutBombServerCall();
    }

    public void ChangeHealth(int amount) {
        if (amount < 0) {
            if (invincibleTimer > 0) return;
            invincibleTimer = timeInvincible;
            networkAnimator.AnimationClientRpc("Hit");
        }
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        characterAvatarHealth.lives.Value = health;
        if (health == 0) {
            networkAnimator.AnimationClientRpc("Dead", true);
            alive.Value = false;
            Player.livingPlayers.Remove(Player.players[Id]);
            deadTimer = 3;
            gameStateController.TestPlayerWins();
        }
    }

    [ClientRpc]
    public void ChangeSpeedClientRpc(float speed) {
        this.speed = speed;
    }

    public void MoveServerCall(float x, float y) {
        MoveClientRpc(x, y, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    public void MoveServerRpc(float x, float y) {
        transform.position = new Vector2(x, y);
        MoveServerCall(x, y);
    }

    [ClientRpc]
    public void MoveClientRpc(float x, float y, ClientRpcParams clientRpcParams = default) {
        transform.position = new Vector2(x, y);
    }
}
