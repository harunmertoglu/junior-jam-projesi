using UnityEngine;

// Tüm adalar TEK gameplay sahnesinde (TestScene). Bu yüzden sahnede bir tane SeasonManager var.
// Mevsim değeri oyuncunun konumundan gelir: adaların ortasındaki SeasonPoint'lere (sonbahar 0, orta 0.5, kış 1)
// olan uzaklığa göre hesaplanır ve oraya yavaşça kayar. Sahnede SeasonPoint yoksa Inspector değeri sabit kalır.
// Mevsimi kullanan scriptler (GroundSurface, MeltingPlatform, RisingFlame, SeasonVisuals) değeri her seferinde
// buradan okur, bu yüzden değer değişince kendiliğinden uyum sağlarlar. Değeri bir yerde saklamayın.
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance;

    [Header("Season")]
    // 0 = sonbahar, 1 = kış
    [SerializeField, Range(0f, 1f)] float seasonValue;
    // Değer saniyede en fazla bu kadar değişir: 0.25 = ışınlansan bile 0'dan 1'e 4 sn'de geçer
    [SerializeField] float seasonChangeSpeed = 0.25f;

    [Header("References")]
    // Boşsa "Player" tag'li obje bulunur
    [SerializeField] Transform playerTransform;

    [Header("Recall")]
    // Ada 1: 4, Ada 2: 3
    [SerializeField] float recallDuration = 4f;

    SeasonPoint[] seasonPoints;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        seasonPoints = FindObjectsByType<SeasonPoint>(FindObjectsInactive.Exclude);

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // Oyun başında geçiş olmasın, oyuncunun durduğu yerin mevsimiyle başla
        if (HasSeasonPoints())
        {
            seasonValue = GetTargetSeasonValue();
        }
    }

    void Update()
    {
        if (!HasSeasonPoints())
        {
            return;
        }

        float targetSeasonValue = GetTargetSeasonValue();
        seasonValue = Mathf.MoveTowards(seasonValue, targetSeasonValue, seasonChangeSpeed * Time.deltaTime);
    }

    bool HasSeasonPoints()
    {
        return seasonPoints != null && seasonPoints.Length > 0 && playerTransform != null;
    }

    // Oyuncuya en yakın iki noktanın değerini uzaklığa göre karıştırır.
    // Örnek: orta ada (0.5) 10 m, sonbahar adası (0) 30 m uzakta -> Lerp(0.5, 0, 10 / 40) = 0.375
    float GetTargetSeasonValue()
    {
        SeasonPoint nearestPoint = null;
        SeasonPoint secondNearestPoint = null;
        float nearestDistance = float.MaxValue;
        float secondNearestDistance = float.MaxValue;

        foreach (SeasonPoint seasonPoint in seasonPoints)
        {
            float distance = GetHorizontalDistance(playerTransform.position, seasonPoint.transform.position);

            if (distance < nearestDistance)
            {
                secondNearestPoint = nearestPoint;
                secondNearestDistance = nearestDistance;
                nearestPoint = seasonPoint;
                nearestDistance = distance;
            }
            else if (distance < secondNearestDistance)
            {
                secondNearestPoint = seasonPoint;
                secondNearestDistance = distance;
            }
        }

        float totalDistance = nearestDistance + secondNearestDistance;
        if (secondNearestPoint == null || totalDistance < 0.01f)
        {
            return nearestPoint.GetSeasonValue();
        }

        // Yakın olan nokta daha ağır basar. Tam noktanın üstündeysek ağırlık 0, ikisinin ortasındaysak 0.5
        float secondPointWeight = nearestDistance / totalDistance;
        return Mathf.Lerp(nearestPoint.GetSeasonValue(), secondNearestPoint.GetSeasonValue(), secondPointWeight);
    }

    // Yükseklik sayılmaz: zıplamak ya da yüksek bir platforma çıkmak mevsimi değiştirmesin
    float GetHorizontalDistance(Vector3 positionA, Vector3 positionB)
    {
        positionA.y = 0f;
        positionB.y = 0f;
        return Vector3.Distance(positionA, positionB);
    }

    public float GetSeasonValue()
    {
        return seasonValue;
    }

    public float GetRecallDuration()
    {
        return recallDuration;
    }
}
