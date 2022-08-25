using Unity.Netcode;

public class BombController : NetworkBehaviour {
    public float TimeToExplode { get { return timer.TimeRemain(); } }
    public Bomb bomb;
    private Timer timer;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            gameObject.tag = "Bomb";
            timer = gameObject.AddComponent<Timer>();
            timer.Init(Bomb.explodeTime, () => { bomb.Trigger(); });
        }
        Static.hasObstacle[new((int)transform.position.x, (int)transform.position.y)] = true;
    }

    public override void OnDestroy() {
        Static.hasObstacle[new((int)transform.position.x, (int)transform.position.y)] = false;
    }

    public void Destroy() {
        Destroy(gameObject);
    }
}
