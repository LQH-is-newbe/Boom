using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Character {
    public const float initialSpeed = 4;
    public const int initialBombPower = 2;
    public const int initialBombCapacity = 1;
    public const int maxHealth = 3;
    public const float timeInvincible = 2;

    public static readonly Dictionary<int, Character> characters = new();
    public static readonly Vector2 colliderHalfSize = new(0.3f, 0.2f);
    public static readonly Vector2[] vertices = {
        new(colliderHalfSize.x, colliderHalfSize.y),
        new(-colliderHalfSize.x, colliderHalfSize.y),
        new(-colliderHalfSize.x, -colliderHalfSize.y),
        new(colliderHalfSize.x, -colliderHalfSize.y)
    };
    public static readonly Vector2[][] directionVertices = {
        new Vector2[] { vertices[1], vertices[2] },
        new Vector2[] { vertices[3], vertices[0] },
        new Vector2[] { vertices[0], vertices[1] },
        new Vector2[] { vertices[2], vertices[3] },
    };
    public static readonly Vector2[] boundaryMidPoints = {
        new(-colliderHalfSize.x, 0),
        new(colliderHalfSize.x, 0),
        new(0 , colliderHalfSize.y),
        new(0, -colliderHalfSize.y)
    };
    public static readonly Vector2[] initialPositions = {
        AI.PosToMapPos(new Vector2Int(1, 26)),
        AI.PosToMapPos(new Vector2Int(26, 26)),
        AI.PosToMapPos(new Vector2Int(1, 1)),
        AI.PosToMapPos(new Vector2Int(26, 1))};
    public static readonly Color32[] colors = {
        new(48, 172, 224, 255),
        new(92, 222, 114, 255),
        new(229, 220, 84, 255),
        new(231, 88, 80, 255)
    };
    public static readonly string[] names = { "Trinny", "Mimmo", "Nou", "Duu" };

    private static readonly GameObject characterAvatarHealthPrefab = Resources.Load<GameObject>("UI/CharacterAvatarHealth");
    private static readonly GameObject characterPrefab = Resources.Load<GameObject>("Characters/Character");
    public readonly Dictionary<Collectable.Type, int> collectables = new();

    public Vector2 Position { get; set; }
    private float speed;
    public float Speed { get { return speed; } set { speed = value; ((CharacterController)Static.controllers[this]).speed.Value = value; } }
    public int BombPower { get; set; }
    public int BombCapacity { get; set; }
    private int health;
    public int BombNum { get; set; }
    public bool IsInvincible { get; set; }
    public bool IsAlive { get; set; }
    public int Id { get; }
    public string CharacterName { get; }
    public bool IsNPC { get; }
    public int ClientPlayerId { get; }
    public string BombName { get; }


    public Character(Player player) {
        speed = initialSpeed;
        BombPower = initialBombPower;
        BombCapacity = initialBombCapacity;
        BombNum = 0;
        health = maxHealth;
        IsInvincible = false;
        IsAlive = true;

        Id = player.Id;
        CharacterName = player.CharacterName;
        IsNPC = player.IsNPC;
        ClientPlayerId = player.ClientPlayerId;
        BombName = player.BombName;
    }

    public Character Copy() {
        return (Character)MemberwiseClone();
    }

    public void Create(int index, Player player) {
        GameObject characterAvatarHealth = Object.Instantiate(characterAvatarHealthPrefab);
        CharacterAvatarHealth characterAvatarHealthController = characterAvatarHealth.GetComponent<CharacterAvatarHealth>();
        characterAvatarHealthController.characterName.Value = new(player.CharacterName);
        characterAvatarHealthController.playerName.Value = new(player.Name);
        characterAvatarHealthController.lives.Value = maxHealth;
        characterAvatarHealthController.playerId.Value = index + 1;
        if (!player.IsNPC) characterAvatarHealth.GetComponent<NetworkObject>().SpawnWithOwnership(player.ClientId);
        else characterAvatarHealth.GetComponent<NetworkObject>().Spawn();

        Position = initialPositions[index];
        GameObject character = Object.Instantiate(characterPrefab, Position, Quaternion.identity);
        CharacterController controller = character.GetComponent<CharacterController>();
        Static.controllers[this] = controller;
        controller.Init(this, characterAvatarHealthController);
        characters[Id] = this;
        if (!player.IsNPC) {
            character.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.ClientId, true);
        } else {
            character.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    public void PutBomb() {
        if (!IsAlive || BombNum >= BombCapacity) return;
        int x = (int)Mathf.Floor(Position.x);
        int y = (int)Mathf.Floor(Position.y);
        Vector2Int mapBlock = new(x, y);
        if (Static.mapBlocks[mapBlock].element != null) {
            return;
        }
        Bomb bomb = new(mapBlock, BombPower, Id);
        bomb.Create();
    }

    public void ChangeHealth(int amount) {
        if (!Static.networkVariables.gameRunning.Value) return;
        CharacterController controller = (CharacterController)Static.controllers[this];
        if (amount < 0 && IsInvincible) return;
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        controller.ChangeHealth(health);
        if (health == 0) {
            List<Collectable> deadDrops = Collectable.AssignRandomPosition(collectables);
            foreach (Collectable collectable in deadDrops) {
                collectable.Create(true, new(Position.x - 0.5f, Position.y - 0.5f));
            }
            IsAlive = false;
            characters.Remove(Id);
            controller.Dead();
            GameObject.Find("GameStateController").GetComponent<GameStateController>().TestPlayerWins();
        } else if (amount < 0) {
            controller.Hit();
            IsInvincible = true;
        }
    }
}
