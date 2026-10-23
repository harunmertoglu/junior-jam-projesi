using UnityEngine;

// Islak zemin. Kışa yaklaştıkça buza döner, daha kaygan olur.
// PlayerMovement üstünde durduğu zeminde bu script'i arar.
public class GroundSurface : MonoBehaviour
{
    [Header("Slipperiness")]
    // 0 = normal zemin, 1 = tamamen kaygan
    [SerializeField, Range(0f, 1f)] float autumnSlipperiness = 0.6f;
    [SerializeField, Range(0f, 1f)] float winterSlipperiness = 0.9f;

    public float GetSlipperiness()
    {
        float seasonValue = SeasonManager.Instance != null ? SeasonManager.Instance.GetSeasonValue() : 0f;
        return Mathf.Lerp(autumnSlipperiness, winterSlipperiness, seasonValue);
    }
}
