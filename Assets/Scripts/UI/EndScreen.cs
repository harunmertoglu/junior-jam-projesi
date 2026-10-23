using UnityEngine;
using UnityEngine.EventSystems;

// Bitiş sahnesi: final metni ve credits. Kar efekti görsel aşamada eklenecek.
public class EndScreen : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] GameObject firstSelectedButton;

    [Header("Scenes")]
    [SerializeField] int mainMenuSceneIndex = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    // Butonun OnClick'ine Inspector'dan bağlanır
    public void ReturnToMainMenu()
    {
        SceneTransition.LoadSceneWithFade(mainMenuSceneIndex);
    }
}
