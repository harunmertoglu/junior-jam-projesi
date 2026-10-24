using UnityEngine;

// Oyuncuyu A noktasından B noktasına taşıyan süzülme objesi.
// Oyuncu trigger'a girince snap olur, zıplayarak ya da kendi isteğiyle ayrılabilir.
// Hedefe ulaşınca başlangıç noktasına döner ve tekrar bekler.
public class GlideObject : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] Transform targetPoint;
    [SerializeField] float moveSpeed = 5f;
    // true ise oyuncu binene kadar bekler, false ise hemen hareket eder
    [SerializeField] bool waitForPlayer = true;

    [Header("Tutuş Noktası")]
    // Objenin merkezine göre oyuncunun snap olacağı yer. Şemsiye altı için Y eksi ver (ör. -1.5)
    [SerializeField] Vector3 hangOffset = new Vector3(0f, -1.5f, 0f);

    PlayerMovement playerMovement;
    bool hasPlayer;
    bool arrived;
    bool isMoving;
    Vector3 startPosition;
    // targetPoint child olduğu için hareketle birlikte kayar;
    // Awake'te dünya pozisyonunu bir kez sabitliyoruz
    Vector3 targetWorldPosition;

    public Vector3 CurrentVelocity { get; private set; }

    void Awake()
    {
        startPosition = transform.position;
        targetWorldPosition = targetPoint.position;
        isMoving = !waitForPlayer;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        RespawnManager.OnPlayerRespawned += ResetGlider;
    }

    void OnDisable()
    {
        RespawnManager.OnPlayerRespawned -= ResetGlider;
    }

    void FixedUpdate()
    {
        if (arrived || !isMoving) return;

        Vector3 toTarget = targetWorldPosition - transform.position;
        float stepSize = moveSpeed * Time.fixedDeltaTime;

        if (toTarget.magnitude <= stepSize)
        {
            transform.position = targetWorldPosition;
            CurrentVelocity = Vector3.zero;
            Arrive();
            return;
        }

        CurrentVelocity = toTarget.normalized * moveSpeed;
        transform.position += CurrentVelocity * Time.fixedDeltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (arrived || hasPlayer) return;
        if (!other.CompareTag("Player")) return;

        hasPlayer = true;
        isMoving = true;
        playerMovement.AttachToGlider(transform, this);
    }

    public Vector3 GetSnapPosition()
    {
        return transform.position + hangOffset;
    }

    public void OnPlayerDetached()
    {
        hasPlayer = false;
    }

    void Arrive()
    {
        if (hasPlayer)
        {
            hasPlayer = false;
            playerMovement.DetachFromGlider(CurrentVelocity);
        }

        ResetGlider();
    }

    void ResetGlider()
    {
        if (hasPlayer)
        {
            hasPlayer = false;
            playerMovement.DetachFromGlider(Vector3.zero);
        }

        transform.position = startPosition;
        CurrentVelocity = Vector3.zero;
        arrived = false;
        isMoving = !waitForPlayer;
    }

    void OnDrawGizmos()
    {
        if (targetPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPoint.position, 0.3f);

            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawLine(transform.position, targetPoint.position);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        Vector3 snapPos = transform.position + hangOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(snapPos, 0.25f);
        Gizmos.DrawLine(transform.position, snapPos);
    }
}
