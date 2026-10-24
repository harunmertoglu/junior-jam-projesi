using System.Collections;
using UnityEngine;

// Oyuncu üstüne basınca yavaşça erir.
// Tamamen erirse kaybolur, bir süre sonra başlangıç boyutunda geri gelir.
// Oyuncu ayrılırsa platform tekrar donar (büyür).
// Oyuncu üstündeyken XZ'de serbestçe hareket edebilir, Y kilitlidir.
public class SnowflakePlatform : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] Transform platformVisual;
    [SerializeField] Collider platformCollider;

    [Header("Erime / Donma")]
    [SerializeField] float meltSpeed = 0.3f;       // saniyede erime oranı (0 → 1)
    [SerializeField] float refreezeSpeed = 0.5f;   // saniyede donma oranı (1 → 0)
    [SerializeField] float respawnDelay = 3f;

    PlayerMovement playerMovement;
    Vector3 originalVisualScale;
    float meltProgress;         // 0 = tam dolu, 1 = tamamen eridi
    bool hasPlayerOnTop;        // her FixedUpdate sıfırlanır, OnCollisionStay set eder
    bool isRespawning;

    void Awake()
    {
        originalVisualScale = platformVisual.localScale;
    }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();
    }

    void FixedUpdate()
    {
        if (isRespawning) return;

        if (hasPlayerOnTop)
        {
            meltProgress += meltSpeed * Time.fixedDeltaTime;
            if (meltProgress >= 1f)
            {
                FullyMelted();
                return;
            }
        }
        else
        {
            meltProgress = Mathf.Max(0f, meltProgress - refreezeSpeed * Time.fixedDeltaTime);
        }

        UpdateVisual();
        hasPlayerOnTop = false; // sonraki frame OnCollisionStay yeniden set eder
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        hasPlayerOnTop = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                playerMovement?.LockToSurface();
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        playerMovement?.UnlockFromSurface();
    }

    void UpdateVisual()
    {
        float heightScale = 1f - meltProgress;
        platformVisual.localScale = new Vector3(
            originalVisualScale.x,
            originalVisualScale.y * heightScale,
            originalVisualScale.z
        );
    }

    void FullyMelted()
    {
        meltProgress = 1f;
        playerMovement?.UnlockFromSurface();
        platformCollider.enabled = false;
        platformVisual.gameObject.SetActive(false);
        isRespawning = true;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        meltProgress = 0f;
        platformVisual.localScale = originalVisualScale;
        platformVisual.gameObject.SetActive(true);
        platformCollider.enabled = true;
        hasPlayerOnTop = false;
        isRespawning = false;
    }
}
