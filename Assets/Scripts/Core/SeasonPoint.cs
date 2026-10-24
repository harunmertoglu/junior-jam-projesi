using UnityEngine;

// Mevsim işaret noktası. Adanın ortasına konur: sonbahar adası 0, ortadaki ada 0.5, kış adası 1.
// SeasonManager oyuncuya en yakın iki noktaya bakıp aradaki değeri hesaplar, böylece geçiş yumuşak olur.
public class SeasonPoint : MonoBehaviour
{
    [Header("Season")]
    // 0 = sonbahar, 1 = kış
    [SerializeField, Range(0f, 1f)] float seasonValue;

    public float GetSeasonValue()
    {
        return seasonValue;
    }

    // Sahnede yerini görmek için: sonbahar turuncu, kış mavi daire (sadece Editor'de çizilir)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.Lerp(new Color(1f, 0.5f, 0f), Color.cyan, seasonValue);
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
