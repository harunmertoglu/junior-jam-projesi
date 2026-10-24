using System;
using System.Collections;
using UnityEngine;

// Belli aralıklarla yükselip alçalan alev. Alçaldığında geçilir.
// Yapı: RisingFlame (bu script) -> Flame (trigger collider + HazardZone + görsel)
// Flame alçaldığında zeminin altına girmeli ki oyuncuya değmesin.
public class RisingFlame : MonoBehaviour
{
    public static event Action OnFlameRising;

    [Header("References")]
    [SerializeField] Transform flameTransform;

    [Header("Heights")]
    [SerializeField] float loweredHeight = -2f;
    [SerializeField] float raisedHeight = 0f;

    [Header("Timing")]
    [SerializeField] float loweredDuration = 1.5f;
    [SerializeField] float raisedDuration = 1.5f;
    [SerializeField] float moveDuration = 0.3f;
    // Yan yana birkaç alev farklı zamanda çalışsın diye
    [SerializeField] float startDelay;
    // Kışta (mevsim = 1) tüm süreler bununla çarpılır: 0.6 = %40 daha hızlı
    [SerializeField] float winterTimeMultiplier = 0.6f;

    float timeMultiplier = 1f;

    void Start()
    {
        SetFlameHeight(loweredHeight);
        StartCoroutine(FlameRoutine());
    }

    IEnumerator FlameRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // Mevsim oyuncu ilerledikçe değişir (SeasonManager), bu yüzden her turun başında yeniden okunur
            UpdateTimeMultiplier();

            yield return new WaitForSeconds(loweredDuration * timeMultiplier);

            OnFlameRising?.Invoke();
            yield return StartCoroutine(MoveFlame(loweredHeight, raisedHeight));

            yield return new WaitForSeconds(raisedDuration * timeMultiplier);

            yield return StartCoroutine(MoveFlame(raisedHeight, loweredHeight));
        }
    }

    IEnumerator MoveFlame(float fromHeight, float toHeight)
    {
        float currentMoveDuration = moveDuration * timeMultiplier;
        float moveTimer = 0f;

        while (moveTimer < currentMoveDuration)
        {
            moveTimer += Time.deltaTime;
            SetFlameHeight(Mathf.Lerp(fromHeight, toHeight, moveTimer / currentMoveDuration));
            yield return null;
        }

        SetFlameHeight(toHeight);
    }

    void UpdateTimeMultiplier()
    {
        float seasonValue = SeasonManager.Instance != null ? SeasonManager.Instance.GetSeasonValue() : 0f;
        timeMultiplier = Mathf.Lerp(1f, winterTimeMultiplier, seasonValue);
    }

    void SetFlameHeight(float height)
    {
        Vector3 flamePosition = flameTransform.localPosition;
        flamePosition.y = height;
        flameTransform.localPosition = flamePosition;
    }
}
