using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Sahnenin görünüşünü mevsim değerine göre ayarlar (0 = sonbahar, 1 = kış).
// Gökyüzü, güneş, sis, ortam rengi, renk tonu (iki Volume) ve parçacıklar iki uç değer arasında Lerp edilir.
// Değer SeasonManager'dan gelir ve oyuncu ilerledikçe yavaşça değişir, bu yüzden görünüm de yavaşça değişir.
// Kurulum: SeasonAtmosphere prefab'ını sahneye bir kez koy. Işık ve kamera kendiliğinden bulunur.
// Yukarı bakan yüzeylerde kar birikmesi shader işi, sonra yapılacak.
public class SeasonVisuals : MonoBehaviour
{
    [Header("Sky")]
    // Unity'nin hazır gökyüzü (Skybox/Procedural) için. Asıl etkiyi atmosfer kalınlığı yapar:
    // kalın atmosfer = turuncu, sıcak ufuk (sonbahar), ince atmosfer = soluk, soğuk mavi (kış)
    [SerializeField] float autumnAtmosphereThickness = 1.8f;
    [SerializeField] float winterAtmosphereThickness = 0.75f;
    // Tint boya gibi değil, ışığın havada saçılmasını değiştirir: 0.5 nötr, açık renk = daha soluk gök
    [SerializeField] Color autumnSkyTint = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] Color winterSkyTint = new Color(0.8f, 0.82f, 0.86f);
    // Ufkun altının rengi
    [SerializeField] Color autumnGroundColor = new Color(0.45f, 0.33f, 0.25f);
    [SerializeField] Color winterGroundColor = new Color(0.8f, 0.84f, 0.9f);
    [SerializeField] float autumnSkyExposure = 1.25f;
    [SerializeField] float winterSkyExposure = 1.5f;

    [Header("Light")]
    // Boşsa sahnedeki Directional Light kullanılır
    [SerializeField] Light sunLight;
    [SerializeField] Color autumnLightColor = new Color(1f, 0.85f, 0.6f);
    [SerializeField] Color winterLightColor = new Color(0.7f, 0.82f, 1f);
    [SerializeField] float autumnLightIntensity = 1.1f;
    [SerializeField] float winterLightIntensity = 0.8f;

    [Header("Fog")]
    [SerializeField] Color autumnFogColor = new Color(0.85f, 0.65f, 0.45f);
    [SerializeField] Color winterFogColor = new Color(0.78f, 0.84f, 0.9f);
    [SerializeField] float autumnFogDensity = 0.002f;
    [SerializeField] float winterFogDensity = 0.02f;

    [Header("Ambient")]
    [SerializeField] Color autumnAmbientColor = new Color(0.55f, 0.42f, 0.3f);
    [SerializeField] Color winterAmbientColor = new Color(0.45f, 0.5f, 0.6f);

    [Header("Color Grading")]
    // İki global Volume: renk ayarları Inspector'dan profillerde yapılır, burada sadece ağırlıkları kayar
    [SerializeField] Volume autumnVolume;
    [SerializeField] Volume winterVolume;

    [Header("Particles")]
    // İkisi de boş bırakılabilir
    [SerializeField] ParticleSystem leafParticles;
    [SerializeField] ParticleSystem snowParticles;
    [SerializeField] float leafMaxEmissionRate = 10f;
    [SerializeField] float snowMaxEmissionRate = 40f;
    // Yapraklar bu mevsim değerinde biter, kış adasının yakınında hiç yaprak olmasın
    [SerializeField] float leafEndSeasonValue = 0.65f;
    // Kar bu mevsim değerinden sonra başlar, sonbahar adasının yakınında hiç kar olmasın
    [SerializeField] float snowStartSeasonValue = 0.35f;

    ParticleSystem.EmissionModule leafEmission;
    ParticleSystem.EmissionModule snowEmission;

    Material skyboxMaterial;
    float lastAppliedSeasonValue = -1f;

    void Start()
    {
        FindSunLight();
        CreateSkyboxCopy();
        EnableCameraPostProcessing();
        SetupParticles();
        ApplySeason(GetSeasonValue());
    }

    void Update()
    {
        // Sadece değer değişince uygula
        float seasonValue = GetSeasonValue();
        if (seasonValue != lastAppliedSeasonValue)
        {
            ApplySeason(seasonValue);
        }
    }

    void FindSunLight()
    {
        if (sunLight == null)
        {
            sunLight = RenderSettings.sun;
        }

        if (sunLight == null)
        {
            foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (sceneLight.type == LightType.Directional)
                {
                    sunLight = sceneLight;
                    break;
                }
            }
        }
    }

    // Asıl skybox dosyası değişmesin diye kopyasıyla çalışıyoruz (oyun bitince kopya kaybolur)
    void CreateSkyboxCopy()
    {
        if (RenderSettings.skybox == null || !RenderSettings.skybox.HasProperty("_SkyTint"))
        {
            return;
        }

        skyboxMaterial = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyboxMaterial;
    }

    // Kamera hareket edince fazladan partikül çıkmasın, miktarı sadece mevsim belirlesin
    void SetupParticles()
    {
        if (leafParticles != null)
        {
            leafEmission = leafParticles.emission;
            leafEmission.rateOverDistanceMultiplier = 0f;
        }

        if (snowParticles != null)
        {
            snowEmission = snowParticles.emission;
            snowEmission.rateOverDistanceMultiplier = 0f;
        }
    }

    // Volume'lar sadece kamerada post-processing açıksa görünür
    void EnableCameraPostProcessing()
    {
        if (Camera.main == null)
        {
            return;
        }

        UniversalAdditionalCameraData cameraData = Camera.main.GetUniversalAdditionalCameraData();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
        }
    }

    float GetSeasonValue()
    {
        return SeasonManager.Instance != null ? SeasonManager.Instance.GetSeasonValue() : 0f;
    }

    void ApplySeason(float seasonValue)
    {
        SetSky(seasonValue);
        SetLight(seasonValue);
        SetFog(seasonValue);
        SetAmbient(seasonValue);
        SetColorGrading(seasonValue);
        SetParticles(seasonValue);
        lastAppliedSeasonValue = seasonValue;
    }

    void SetSky(float seasonValue)
    {
        if (skyboxMaterial == null)
        {
            return;
        }

        skyboxMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(autumnAtmosphereThickness, winterAtmosphereThickness, seasonValue));
        skyboxMaterial.SetColor("_SkyTint", Color.Lerp(autumnSkyTint, winterSkyTint, seasonValue));
        skyboxMaterial.SetColor("_GroundColor", Color.Lerp(autumnGroundColor, winterGroundColor, seasonValue));
        skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(autumnSkyExposure, winterSkyExposure, seasonValue));
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
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(autumnAmbientColor, winterAmbientColor, seasonValue);
    }

    void SetColorGrading(float seasonValue)
    {
        if (autumnVolume != null)
        {
            autumnVolume.weight = 1f - seasonValue;
        }

        if (winterVolume != null)
        {
            winterVolume.weight = seasonValue;
        }
    }

    void SetParticles(float seasonValue)
    {
        // Yapraklar azalır, kar artar
        if (leafParticles != null)
        {
            // Örnek: bitiş 0.65 iken mevsim 0 -> 0, 0.325 -> 0.5, 0.8 -> 1
            float leafFadeAmount = Mathf.InverseLerp(0f, leafEndSeasonValue, seasonValue);
            leafEmission.rateOverTimeMultiplier = Mathf.Lerp(leafMaxEmissionRate, 0f, leafFadeAmount);
        }

        if (snowParticles != null)
        {
            // Örnek: başlangıç 0.35 iken mevsim 0.2 -> 0, 0.675 -> 0.5, 1 -> 1
            float snowAmount = Mathf.InverseLerp(snowStartSeasonValue, 1f, seasonValue);
            snowEmission.rateOverTimeMultiplier = Mathf.Lerp(0f, snowMaxEmissionRate, snowAmount);
        }
    }
}
