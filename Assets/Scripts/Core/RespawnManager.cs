using System;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    public static event Action OnPlayerRespawned;

    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;
    // Adanın başlangıç noktası, henüz checkpoint alınmadıysa buraya dönülür
    [SerializeField] Transform startPoint;

    Transform currentCheckpoint;

    void Awake()
    {
        Instance = this;
        currentCheckpoint = startPoint;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    public void RespawnPlayer()
    {
        // Checkpoint yerde durur, ayaklar oraya gelsin diye pivotu ayak yüksekliği kadar yukarı koyuyoruz
        Vector3 respawnPosition = currentCheckpoint.position + Vector3.up * playerMovement.GetFeetHeight();
        playerMovement.TeleportTo(respawnPosition);
        OnPlayerRespawned?.Invoke();
    }

    // IslandExit çağırır: oyuncuyu sonraki adanın başına taşır, orası yeni checkpoint olur.
    // OnPlayerRespawned de çalışır, böylece dash geri dönüş noktası silinir ve taşınan tohum oyuncuyla gelir.
    public void MovePlayerToIsland(Transform islandStartPoint)
    {
        SetCheckpoint(islandStartPoint);
        RespawnPlayer();
    }
}
