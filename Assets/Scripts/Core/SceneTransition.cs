using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sahneler arası siyaha kararma. Sahneye eklemene gerek yok, ilk çağrıda kendini kurar.
// Kullanım: SceneTransition.LoadSceneWithFade(sahneNumarası);
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Fade")]
    [SerializeField] float fadeOutDuration = 0.5f;
    [SerializeField] float fadeInDuration = 0.5f;

    Image fadeImage;
    bool isTransitioning;

    public static void LoadSceneWithFade(int buildIndex)
    {
        // Obje yoksa oluştur, AddComponent sırasında Awake hemen çalışır
        if (Instance == null)
        {
            new GameObject("SceneTransition").AddComponent<SceneTransition>();
        }

        Instance.StartTransition(buildIndex);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeCanvas();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void CreateFadeCanvas()
    {
        // En üstte çizilen kendi Canvas'ı
        Canvas fadeCanvas = gameObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000;
        // Raycaster olmazsa siyah resim tıklamaları engelleyemez
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject fadeImageObject = new GameObject("FadeImage");
        fadeImageObject.transform.SetParent(transform, false);
        fadeImage = fadeImageObject.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        // Anchor 0..1 = tüm ekranı kapla
        RectTransform fadeRect = fadeImage.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
    }

    void StartTransition(int buildIndex)
    {
        // Geçiş sürerken gelen ikinci istek yok sayılır
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(buildIndex));
    }

    IEnumerator TransitionRoutine(int buildIndex)
    {
        isTransitioning = true;
        fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeOutDuration));

        // Duraklatma menüsünden gelindiyse yeni sahne donuk başlamasın
        Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex);

        // Yükleme karesi uzun sürer, o süreyi kararmaya saymayalım
        yield return null;
        yield return null;

        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration));

        fadeImage.raycastTarget = false;
        isTransitioning = false;
    }

    IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float fadeDuration)
    {
        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            // unscaledDeltaTime: timeScale 0 iken de ilerler
            fadeTimer += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, fadeTimer / fadeDuration));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    void SetFadeAlpha(float alpha)
    {
        Color fadeColor = fadeImage.color;
        fadeColor.a = alpha;
        fadeImage.color = fadeColor;
    }
}
