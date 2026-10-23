using UnityEngine;
using UnityEngine.SceneManagement;

// Adanın sonundaki trigger. Build Settings'teki bir sonraki sahneyi kararma ile yükler.
// Sıra: 0 MainMenu, 1-5 Island1-5, 6 End
public class IslandExit : MonoBehaviour
{
    bool isLoading;

    void OnTriggerEnter(Collider other)
    {
        if (isLoading || !other.CompareTag("Player"))
        {
            return;
        }

        isLoading = true;
        SceneTransition.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
