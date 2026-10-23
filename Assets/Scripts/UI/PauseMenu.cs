using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Her ada sahnesinde Canvas üstünde durur. Esc / Start ile açılıp kapanır.
public class PauseMenu : MonoBehaviour
{
    public static event Action<bool> OnPauseChanged;

    [Header("References")]
    [SerializeField] GameObject pausePanel;
    // Gamepad ile menüde gezinmek için ilk seçili buton
    [SerializeField] GameObject firstSelectedButton;

    [Header("Scenes")]
    [SerializeField] int mainMenuSceneIndex = 0;

    bool isPaused;

    void OnEnable()
    {
        InputManager.OnPause += PauseInputs;
    }

    void OnDisable()
    {
        InputManager.OnPause -= PauseInputs;
    }

    void Start()
    {
        SetPaused(false);
    }

    void PauseInputs()
    {
        SetPaused(!isPaused);
    }

    void SetPaused(bool value)
    {
        isPaused = value;
        pausePanel.SetActive(isPaused);

        // timeScale 0: fizik, coroutine'lerdeki WaitForSeconds ve deltaTime durur
        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;

        if (isPaused && firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        OnPauseChanged?.Invoke(isPaused);
    }

    // Aşağıdakiler butonların OnClick'ine Inspector'dan bağlanır
    public void ResumeGame()
    {
        SetPaused(false);
    }

    // Kararma bitene kadar oyun duraklatılmış kalır, timeScale'i SceneTransition yüklemeden önce 1 yapar
    public void RestartIsland()
    {
        SceneTransition.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        SceneTransition.LoadSceneWithFade(mainMenuSceneIndex);
    }
}
