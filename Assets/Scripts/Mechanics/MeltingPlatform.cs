using System;
using System.Collections;
using UnityEngine;

// Buz/kar platform. Üstüne basınca yavaşça incelir, erir, bir süre sonra geri gelir.
// Kışa yaklaştıkça yaz ruhu daha hızlı eritir.
// Yapı: Platform (Collider + bu script) -> Visual (mesh)
public class MeltingPlatform : MonoBehaviour
{
    public static event Action OnPlatformMelted;

    [Header("References")]
    [SerializeField] Collider platformCollider;
    [SerializeField] Transform platformVisual;

    [Header("Timing")]
    [SerializeField] float meltDuration = 2.5f;
    // Kışta (mevsim = 1) erime süresi bununla çarpılır: 0.6 = %40 daha çabuk
    [SerializeField] float winterMeltMultiplier = 0.6f;
    [SerializeField] float respawnDelay = 4f;

    bool isMelting;

    void OnCollisionStay(Collision collision)
    {
        if (isMelting || !collision.collider.CompareTag("Player"))
        {
            return;
        }

        // Pivot değil collider'ın en alt noktası (ayak tabanı) kullanılır
        bool isPlayerOnTop = collision.collider.bounds.min.y >= platformCollider.bounds.max.y - 0.1f;
        if (!isPlayerOnTop)
        {
            return;
        }

        StartCoroutine(MeltRoutine());
    }

    IEnumerator MeltRoutine()
    {
        isMelting = true;

        float seasonValue = SeasonManager.Instance != null ? SeasonManager.Instance.GetSeasonValue() : 0f;
        float currentMeltDuration = meltDuration * Mathf.Lerp(1f, winterMeltMultiplier, seasonValue);

        Vector3 visualStartScale = platformVisual.localScale;
        float meltTimer = 0f;

        while (meltTimer < currentMeltDuration)
        {
            meltTimer += Time.deltaTime;
            float meltProgress = meltTimer / currentMeltDuration;

            // Eridikçe yüksekliği %30'a kadar iner, oyuncu ne kadar kaldığını görür
            float heightScale = Mathf.Lerp(1f, 0.3f, meltProgress);
            platformVisual.localScale = new Vector3(visualStartScale.x, visualStartScale.y * heightScale, visualStartScale.z);
            yield return null;
        }

        SetPlatformActive(false);
        OnPlatformMelted?.Invoke();

        yield return new WaitForSeconds(respawnDelay);

        platformVisual.localScale = visualStartScale;
        SetPlatformActive(true);
        isMelting = false;
    }

    void SetPlatformActive(bool value)
    {
        platformCollider.enabled = value;
        platformVisual.gameObject.SetActive(value);
    }
}
