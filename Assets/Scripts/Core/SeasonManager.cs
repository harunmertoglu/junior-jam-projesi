using UnityEngine;

// Her ada sahnesinde bir tane bulunur. Değerler adaya göre Inspector'dan girilir.
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
}
