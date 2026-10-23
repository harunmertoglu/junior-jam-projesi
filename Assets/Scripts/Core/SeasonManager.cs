using UnityEngine;

// Tüm adalar TEK gameplay sahnesinde (TestScene). Bu yüzden sahnede bir tane SeasonManager var.
// Inspector değerleri ilk adanınki; oyuncu bir adaya girince IslandZone SetIslandSeason ile değiştirir.
// Mevsimi kullanan scriptler (GroundSurface, MeltingPlatform, RisingFlame, PlayerDash) değeri her seferinde
// buradan okur, bu yüzden değer değişince kendiliğinden uyum sağlarlar. Değeri bir yerde saklamayın.
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance;

    [Header("Season")]
    // 0 = sonbahar, 1 = kış. Her ada yaklaşık 0.2 artar.
    [SerializeField, Range(0f, 1f)] float seasonValue;

    [Header("Recall")]
    // Ada 1: 10, Ada 2: 9 ... Ada 6: 5
    [SerializeField] float recallDuration = 10f;

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

    public float GetSeasonValue()
    {
        return seasonValue;
    }

    public float GetRecallDuration()
    {
        return recallDuration;
    }

    // IslandZone çağırır: oyuncu yeni bir adaya girdi
    public void SetIslandSeason(float seasonValue, float recallDuration)
    {
        this.seasonValue = Mathf.Clamp01(seasonValue);
        this.recallDuration = recallDuration;
    }
}
