using UnityEngine;

// Sahnenin görünüşünü mevsim değerine göre ayarlar (0 = sonbahar, 1 = kış).
// Işık, sis, ortam rengi ve parçacıklar iki uç değer arasında Lerp edilir.
// Yukarı bakan yüzeylerde kar birikmesi shader işi, sonra yapılacak.
public class SeasonVisuals : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] Light sunLight;
    [SerializeField] Color autumnLightColor = new Color(1f, 0.85f, 0.6f);
    [SerializeField] Color winterLightColor = new Color(0.7f, 0.82f, 1f);
    [SerializeField] float autumnLightIntensity = 1.1f;
    [SerializeField] float winterLightIntensity = 0.8f;

    [Header("Fog")]
    [SerializeField] Color autumnFogColor = new Color(0.85f, 0.65f, 0.45f);
    [SerializeField] Color winterFogColor = new Color(0.78f, 0.84f, 0.9f);
    [SerializeField] float autumnFogDensity = 0.002f;
    [SerializeField] float winterFogDensity = 0.03f;

    [Header("Ambient")]
    [SerializeField] Color autumnAmbientColor = new Color(0.55f, 0.4f, 0.28f);
    [SerializeField] Color winterAmbientColor = new Color(0.45f, 0.5f, 0.6f);

    [Header("Particles")]
    // İkisi de boş bırakılabilir
    [SerializeField] ParticleSystem leafParticles;
    [SerializeField] ParticleSystem snowParticles;
    [SerializeField] float leafMaxEmissionRate = 10f;
    [SerializeField] float snowMaxEmissionRate = 40f;

    float lastAppliedSeasonValue = -1f;

    void Start()
    {
        // Işık atanmadıysa Lighting ayarlarındaki güneşi kullan
        if (sunLight == null)
        {
            sunLight = RenderSettings.sun;
        }

        ApplySeason(GetSeasonValue());
    }

    void Update()
    {
        // Sadece değer değişince uygula (play mode'da slider ile denemek için)
        float seasonValue = GetSeasonValue();
        if (seasonValue != lastAppliedSeasonValue)
        {
            ApplySeason(seasonValue);
        }
    }

    float GetSeasonValue()
    {
        return SeasonManager.Instance != null ? SeasonManager.Instance.GetSeasonValue() : 0f;
    }

    void ApplySeason(float seasonValue)
    {
        SetLight(seasonValue);
        SetFog(seasonValue);
        SetAmbient(seasonValue);
        SetParticles(seasonValue);
        lastAppliedSeasonValue = seasonValue;
    }

    void SetLight(float seasonValue)
    {
        if (sunLight == null)
        {
            return;
        }

        sunLight.color = Color.Lerp(autumnLightColor, winterLightColor, seasonValue);
        sunLight.intensity = Mathf.Lerp(autumnLightIntensity, winterLightIntensity, seasonValue);
    }

    void SetFog(float seasonValue)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Color.Lerp(autumnFogColor, winterFogColor, seasonValue);
        RenderSettings.fogDensity = Mathf.Lerp(autumnFogDensity, winterFogDensity, seasonValue);
    }

    void SetAmbient(float seasonValue)
    {
        // Flat modda ortam ışığı tek bir renktir, skybox'tan hesaplanmaz
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(autumnAmbientColor, winterAmbientColor, seasonValue);
    }

    void SetParticles(float seasonValue)
    {
        // Yapraklar azalır, kar artar
        if (leafParticles != null)
        {
            ParticleSystem.EmissionModule leafEmission = leafParticles.emission;
            leafEmission.rateOverTimeMultiplier = Mathf.Lerp(leafMaxEmissionRate, 0f, seasonValue);
        }

        if (snowParticles != null)
        {
            ParticleSystem.EmissionModule snowEmission = snowParticles.emission;
            snowEmission.rateOverTimeMultiplier = Mathf.Lerp(0f, snowMaxEmissionRate, seasonValue);
        }
    }
}
