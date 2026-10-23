using System;
using UnityEngine;

// Mevsim tohumu. Trigger collider. Alınınca kızın başının üstünde onu takip eder.
// Geri dönüşte ve checkpoint'e dönüşte de kızın yanında kalır.
public class SeasonSeed : MonoBehaviour
{
    public static event Action OnSeedPicked;

    [Header("Follow")]
    [SerializeField] Vector3 followOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] float followSpeed = 12f;

    Transform playerTransform;
    bool isCarried;
    bool isPlaced;

    void OnEnable()
    {
        PlayerDash.OnRecalled += SnapToPlayer;
        RespawnManager.OnPlayerRespawned += SnapToPlayer;
    }

    void OnDisable()
    {
        PlayerDash.OnRecalled -= SnapToPlayer;
        RespawnManager.OnPlayerRespawned -= SnapToPlayer;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCarried || isPlaced || !other.CompareTag("Player"))
        {
            return;
        }

        playerTransform = other.transform;
        isCarried = true;
        OnSeedPicked?.Invoke();
    }

    void LateUpdate()
    {
        if (!isCarried)
        {
            return;
        }

        Vector3 targetPosition = playerTransform.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    // Işınlanınca tohum haritanın öbür ucundan uçarak gelmesin, direkt yanında olsun
    void SnapToPlayer()
    {
        if (!isCarried)
        {
            return;
        }

        transform.position = playerTransform.position + followOffset;
    }

    public bool IsCarried()
    {
        return isCarried;
    }

    public void PlaceAt(Vector3 placePosition)
    {
        isCarried = false;
        isPlaced = true;
        transform.position = placePosition;
    }
}
