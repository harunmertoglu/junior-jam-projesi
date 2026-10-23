using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Ana menü: Başla -> giriş metni -> Ada 1. Çık oyunu kapatır.
public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject introPanel;

    [Header("Selection")]
    // Gamepad ile menüde gezinmek için her panelin ilk seçili butonu
    [SerializeField] GameObject mainFirstButton;
    [SerializeField] GameObject introFirstButton;

    [Header("Scenes")]
    [SerializeField] int firstIslandSceneIndex = 1;

    void Start()
    {
        // Duraklatmadan ana menüye dönüldüyse oyun donuk kalmasın
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mainPanel.SetActive(true);
        introPanel.SetActive(false);
        SelectButton(mainFirstButton);
    }

    void SelectButton(GameObject button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button);
        }
    }

    // Aşağıdakiler butonların OnClick'ine Inspector'dan bağlanır
    public void StartGame()
    {
        mainPanel.SetActive(false);
        introPanel.SetActive(true);
        SelectButton(introFirstButton);
    }

    public void ContinueFromIntro()
    {
        SceneManager.LoadScene(firstIslandSceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
