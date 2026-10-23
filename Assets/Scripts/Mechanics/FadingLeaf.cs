using System;
using System.Collections;
using UnityEngine;

// Üstüne basınca titreyerek solar, kırılıp düşer, bir süre sonra geri gelir.
// Yapı: Leaf (Collider + bu script) -> Visual (mesh)
public class FadingLeaf : MonoBehaviour
{
    public static event Action OnLeafBroken;

    [Header("References")]
    [SerializeField] Collider leafCollider;
    [SerializeField] Transform leafVisual;

    [Header("Timing")]
    [SerializeField] float fadeDuration = 0.9f;
    [SerializeField] float respawnDelay = 3f;

    [Header("Shake")]
    [SerializeField] float shakeAmount = 0.05f;

    bool isFading;

    void OnCollisionStay(Collision collision)
    {
        if (isFading || !collision.collider.CompareTag("Player"))
        {
            return;
        }

        // Yandan değmek sayılmaz, oyuncunun ayakları yaprağın üst yüzeyinde olmalı
        // Pivot değil collider'ın en alt noktası (ayak tabanı) kullanılır, pivot nerede olursa olsun çalışır
        bool isPlayerOnTop = collision.collider.bounds.min.y >= leafCollider.bounds.max.y - 0.1f;
        if (!isPlayerOnTop)
        {
            return;
        }

        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        isFading = true;

        Vector3 visualStartPosition = leafVisual.localPosition;
        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            // insideUnitSphere: yarıçapı 1 olan küre içinde rastgele bir nokta
            leafVisual.localPosition = visualStartPosition + UnityEngine.Random.insideUnitSphere * shakeAmount;
            fadeTimer += Time.deltaTime;
            yield return null;
        }

        leafVisual.localPosition = visualStartPosition;
        SetLeafActive(false);
        OnLeafBroken?.Invoke();

        yield return new WaitForSeconds(respawnDelay);

        SetLeafActive(true);
        isFading = false;
    }

    void SetLeafActive(bool value)
    {
        leafCollider.enabled = value;
        leafVisual.gameObject.SetActive(value);
    }
}
