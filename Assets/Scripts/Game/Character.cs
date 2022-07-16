using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;

public class Character : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public float initialSpeed;
    private float speed;
    public int initialBombPower;
    private int bombPower;
    public int initialBombCapacity;
    private int bombCapacity;
    public int maxHealth;
    private int health;
    private int bombNum = 0;
    private Rigidbody2D rigidbody2d;
    private Vector2 positionChange = Vector2.zero;
    public GameObject bombPrefab;
    public CharacterAvatarHealth characterAvatarHealth;
    private bool isInvincible = false;
    public float timeInvincible;
    private float invincibleTimer;
    private bool alive = true;
    private NetworkAnimator networkAnimator;
    private GameStateController gameStateController;

    private void Start() {
        rigidbody2d = GetComponent<Rigidbody2D>();
        speed = initialSpeed;
        networkAnimator = GetComponent<NetworkAnimator>();
        if (NetworkManager.Singleton.IsServer) {
            bombPower = initialBombPower;
            bombCapacity = initialBombCapacity;
            health = maxHealth;
            gameStateController = GameObject.FindGameObjectWithTag("GameStateController").GetComponent<GameStateController>();
        }
    }

    private void Update() {
        if (!NetworkManager.Singleton.IsServer) return;
        if (isInvincible) {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0) {
                isInvincible = false;
            }
        }
    }

    private void FixedUpdate() {
        if (!IsOwner) return;
        Vector2 position = rigidbody2d.position;
        position.x += positionChange.x;
        position.y += positionChange.y;
        positionChange = Vector2.zero;
        rigidbody2d.MovePosition(position);
        TransformServerRpc(rigidbody2d.position.x, rigidbody2d.position.y, NetworkManager.Singleton.LocalClientId);
    }

    public override void OnNetworkSpawn() {
        AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["Dead"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Dead");
        animatorOverrideController["Hit_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Hit_left");
        animatorOverrideController["Hit_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Hit_right");
        animatorOverrideController["Idle_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Idle_left");
        animatorOverrideController["Idle_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Idle_right");
        animatorOverrideController["Walk_left"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Walk_left");
        animatorOverrideController["Walk_right"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/Walk_right");
        GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
    }

    public void Move(float horizontal, float vertical) {
        if (!alive) return;
        if (horizontal != 0 || vertical != 0) {
            positionChange.x += speed * horizontal * Time.deltaTime;
            positionChange.y += speed * vertical * Time.deltaTime;
            if (horizontal != 0) networkAnimator.SetAnimation("Direction", horizontal);
            networkAnimator.SetAnimation("Moving", true);
        } else {
            networkAnimator.SetAnimation("Moving", false);
        }
    }

    [ServerRpc]
    public void PutBombServerRpc() {
        if (bombNum < bombCapacity) {
            int x = (int)Mathf.Floor(rigidbody2d.position.x);
            int y = (int)Mathf.Floor(rigidbody2d.position.y);
            if (Static.map[x, y] == null) {
                GameObject bomb = Instantiate(bombPrefab, new Vector2(x, y), Quaternion.identity);
                Bomb bombController = bomb.GetComponent<Bomb>();
                bomb.GetComponent<NetworkObject>().Spawn(true);
                bombController.Init(x, y, this, bombPower);
                ++bombNum;
                Static.map[x, y] = bomb;
            }
        }
    }

    public void NotifyBombExplode() {
        --bombNum;
    }

    public void ChangeHealth(int amount) {
        if (amount < 0) {
            if (isInvincible) return;
            isInvincible = true;
            invincibleTimer = timeInvincible;
            networkAnimator.AnimationClientRpc("Hit");
        }
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        if (health == 0) {
            characterAvatarHealth.HealthChangeClientRpc("You Died");
            networkAnimator.AnimationClientRpc("Dead", true);
            alive = false;
            DeadClientRpc();
            Static.livingPlayers.Remove(OwnerClientId);
            gameStateController.TestPlayerWins();
        } else {
            characterAvatarHealth.HealthChangeClientRpc(health.ToString());
        }
    }

    [ClientRpc]
    public void ChangeSpeedClientRpc(float amount) {
        speed += amount;
    }

    public void ChangeBombPower(int amount) {
        bombPower += amount;
    }

    public void ChangeBombCapacity(int amount) {
        bombCapacity += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TransformServerRpc(float x, float y, ulong clientId) {
        transform.position = new Vector2(x, y);
        TransformClientRpc(x, y, Util.GetClientRpcParamsExcept(clientId));
    }

    [ClientRpc]
    public void TransformClientRpc(float x, float y, ClientRpcParams clientRpcParams = default) {
        transform.position = new Vector2(x, y);
    }

    [ClientRpc]
    public void DeadClientRpc() {
        alive = false;
    }
}
