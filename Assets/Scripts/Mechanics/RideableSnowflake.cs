using System.Collections;
using UnityEngine;

// Binilebilir kar tanesi.
// Oyuncu üstüne çıkınca erimeye başlar ve oyuncu hareket ettikçe kar tanesi onunla birlikte gider.
// Tamamen erirse kaybolur, bir süre sonra ilk bulunduğu yerde yeniden doğar.
// Erimeden üstünden atlanırsa olduğu yerde kalır ve yavaşça tekrar donar (eski boyutuna döner).
// Yapı: Snowflake (Collider + bu script) -> Visual (mesh)
[RequireComponent(typeof(Rigidbody))]
public class RideableSnowflake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Collider platformCollider;
    [SerializeField] Transform platformVisual;

    [Header("Melt / Freeze")]
    // Tamamen erimesi için üstünde geçirilmesi gereken süre
    [SerializeField] float meltDuration = 3f;
    // Tamamen erimiş halden tam boyuta dönme süresi
    [SerializeField] float refreezeDuration = 2f;
    [SerializeField] float respawnDelay = 2f;

    [Header("Ride")]
    // Oyuncunun ayağı platform üstünün bu kadar altında olsa da "üstünde" sayılır
    [SerializeField] float onTopTolerance = 0.15f;

    Rigidbody platformRigidbody;
    PlayerMovement riderMovement;
    Transform riderTransform;
    Vector3 riderOffset;        // platform - oyuncu, XZ'de
    Vector3 spawnPosition;
    Quaternion spawnRotation;
    Vector3 visualStartScale;
    float meltProgress;         // 0 = tam dolu, 1 = tamamen eridi
    bool isRiding;
    bool touchedFromTopThisStep;
    bool isMelted;

    void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity = false;
        platformRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (platformCollider == null) platformCollider = GetComponent<Collider>();
        if (platformVisual == null && transform.childCount > 0) platformVisual = transform.GetChild(0);

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        visualStartScale = platformVisual.localScale;
    }

    void FixedUpdate()
    {
        if (isMelted) return;

        if (touchedFromTopThisStep && !isRiding) StartRiding();
        else if (!touchedFromTopThisStep && isRiding) StopRiding();
        touchedFromTopThisStep = false;

        if (isRiding)
        {
            FollowRider();

            meltProgress += Time.fixedDeltaTime / meltDuration;
            if (meltProgress >= 1f)
            {
                StartCoroutine(MeltAndRespawnRoutine());
                return;
            }
        }
        else
        {
            meltProgress = Mathf.Max(0f, meltProgress - Time.fixedDeltaTime / refreezeDuration);
        }

        UpdateVisual();
    }

    void OnCollisionStay(Collision collision)
    {
        if (isMelted || !collision.collider.CompareTag("Player")) return;

        // Pivot değil collider'ın en alt noktası (ayak tabanı) kullanılır
        bool isPlayerOnTop = collision.collider.bounds.min.y >= platformCollider.bounds.max.y - onTopTolerance;
        if (!isPlayerOnTop) return;

        touchedFromTopThisStep = true;
        if (riderTransform == null)
        {
            riderTransform = collision.transform;
            riderMovement = collision.collider.GetComponentInParent<PlayerMovement>();
        }
    }

    void StartRiding()
    {
        isRiding = true;
        riderOffset = transform.position - riderTransform.position;
        riderOffset.y = 0f;
        riderMovement?.LockToSurface();
    }

    void StopRiding()
    {
        isRiding = false;
        riderMovement?.UnlockFromSurface();
        riderTransform = null;
        riderMovement = null;
    }

    // Oyuncu kendi girdisiyle yürür, kar tanesi altında onu XZ'de takip eder. Y sabit kalır
    void FollowRider()
    {
        Vector3 targetPosition = riderTransform.position + riderOffset;
        targetPosition.y = transform.position.y;
        platformRigidbody.MovePosition(targetPosition);
    }

    void UpdateVisual()
    {
        float heightScale = 1f - meltProgress;
        platformVisual.localScale = new Vector3(visualStartScale.x, visualStartScale.y * heightScale, visualStartScale.z);
    }

    IEnumerator MeltAndRespawnRoutine()
    {
        isMelted = true;
        if (isRiding) StopRiding();
        SetPlatformActive(false);

        yield return new WaitForSeconds(respawnDelay);

        platformRigidbody.position = spawnPosition;
        platformRigidbody.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        meltProgress = 0f;
        platformVisual.localScale = visualStartScale;
        touchedFromTopThisStep = false;
        SetPlatformActive(true);
        isMelted = false;
    }

    void SetPlatformActive(bool value)
    {
        platformCollider.enabled = value;
        platformVisual.gameObject.SetActive(value);
    }
}
