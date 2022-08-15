using System.Collections;
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
    public static readonly Vector2[] characterInitialPositions = {
        AI.PosToMapPos(new Vector2Int(13, 23)),
        AI.PosToMapPos(new Vector2Int(13, 19)),
        AI.PosToMapPos(new Vector2Int(1, 1)),
        AI.PosToMapPos(new Vector2Int(26, 1))};
    public static readonly Color32[] characterColors = {
        new(48, 172, 224, 255),
        new(92, 222, 114, 255),
        new(229, 220, 84, 255),
        new(231, 88, 80, 255)
    };

    private static readonly GameObject characterAvatarHealthPrefab = Resources.Load<GameObject>("UI/CharacterAvatarHealth");
    private static readonly GameObject characterPrefab = Resources.Load<GameObject>("Characters/Character");
    public readonly Dictionary<Collectable.Type, int> collectables = new();

    public Vector2 Position { get; set; }
    private float speed;
    public float Speed { get { return speed; } set { speed = value; ((CharacterController)Static.controllers[this]).speed.Value = value; } }
    public int BombPower { get; set; }
    public int BombCapacity { get; set; }
    public int health;
    public int BombNum { get; set; }
    public bool IsInvincible { get; set; }
    public bool IsAlive { get; set; }
    public int Id { get; }
    public string CharacterName { get; }
    public bool IsNPC { get; }


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

        Position = characterInitialPositions[index];
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
            //CharacterController controller = (CharacterController)Static.controllers[this];
            //Debug.Log("double bomb at " + mapBlock + "with speed " + speed + "at pos (" + controller.transform.position.x + "," + controller.transform.position.y + ") fixedDeltaTime: " + Time.fixedDeltaTime);
            //Time.timeScale = 0;
            return;
        }
        Bomb bomb = new(mapBlock, BombPower, Id);
        bomb.Create();
    }

    public void ChangeHealth(int amount) {
        if (!Static.networkVariables.gameRunning.Value) return;
        CharacterController controller = (CharacterController)Static.controllers[this];
        if (amount < 0) {
            //if (IsNPC) {
            //    Debug.Log("bot hurt at (" + Position.x + "," + Position.y + ")");
            //    Time.timeScale = 0;
            //}
            if (IsInvincible) return;
            controller.Hit();
            IsInvincible = true;
        }
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        controller.ChangeHealth(health);
        if (health == 0) {
            Collectable.CreateCharacterDeadDrops(this);
            IsAlive = false;
            characters.Remove(Id);
            controller.Dead();
            GameObject.Find("GameStateController").GetComponent<GameStateController>().TestPlayerWins();
        }
    }
}
