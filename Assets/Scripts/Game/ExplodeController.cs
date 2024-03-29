using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class ExplodeController : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> spritePath = new();
    public NetworkVariable<float> rotateAngle = new();
    public GameObject explodePrefab;
    public GameObject display;
    private Timer extendTimer;
    private Timer destroyTimer;
    public Explode explode;
    public float TimeToExtend { get { return extendTimer.TimeRemain(); } }
    public float TimeToDestroy { get { return destroyTimer.TimeRemain(); } }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!IsServer) return;
        if (other.CompareTag("Character")) {
            Character character = other.GetComponent<CharacterController>().character;
            if (character.IsAlive) other.GetComponent<CharacterController>().character.ChangeHealth(-1);
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            extendTimer = gameObject.AddComponent<Timer>();
            extendTimer.Init(Explode.extendTime, () => { explode.Extend(); });
            gameObject.AddComponent<Timer>().Init(explode.ExistTime + Explode.fadeAwayTime, () => { Destroy(gameObject); });
            destroyTimer = gameObject.AddComponent<Timer>();
            destroyTimer.Init(explode.ExistTime, () => {
                FadeAwayClientRpc();
                Destroy(GetComponent<BoxCollider2D>());
                explode.Destroy();
            });
            string path = "Explode/Sprites/";
            if (explode.Direction == Direction.zero) {
                path += "center";
            } else {
                if (explode.PowerLeft == 1) {
                    path += "end";
                } else {
                    path += "middle";
                }
                rotateAngle.Value = 0;
                if (explode.Direction == Direction.left) rotateAngle.Value = 90f;
                else if (explode.Direction == Direction.down) rotateAngle.Value = 180f;
                else if (explode.Direction == Direction.right) rotateAngle.Value = 270f;
            }
            spritePath.Value = new FixedString64Bytes(path);
        }
        if (IsClient) {
            display.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(spritePath.Value.Value);
            display.transform.rotation = Quaternion.identity;
            display.transform.Rotate(0, 0, rotateAngle.Value, Space.Self);
        }
    }

    [ClientRpc]
    private void FadeAwayClientRpc() {
        AlphaGradient fadeAway = gameObject.AddComponent<AlphaGradient>();
        fadeAway.Init(true, display, Explode.fadeAwayTime);
    }
}
