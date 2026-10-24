using UnityEngine;
using UnityEngine.SceneManagement;

// Adanın sonundaki trigger. Tüm adalar TEK gameplay sahnesinde (TestScene), bu yüzden sahne yüklemez:
// oyuncuyu sonraki adanın başlangıç noktasına taşır. Mevsim, SeasonManager'da yeni konuma göre yavaşça kayar.
// Son adada Next Island Start Point boş bırakılır: o zaman Build Settings'teki sonraki sahne
// (bitiş sinematiği) yüklenir. Sıra: başlangıç sinematiği, gameplay (TestScene), bitiş sinematiği.
// Geçiş efekti (kararma ya da rüzgârın yaprakla taşıması) sonra eklenecek.
public class IslandExit : MonoBehaviour
{
    [Header("Next Island")]
    // Sonraki adanın başındaki boş obje. Son adada boş bırak.
    [SerializeField] Transform nextIslandStartPoint;

    bool isLoading;

    void OnTriggerEnter(Collider other)
    {
        if (isLoading || !other.CompareTag("Player"))
        {
            return;
        }

        if (nextIslandStartPoint != null)
        {
            RespawnManager.Instance.MovePlayerToIsland(nextIslandStartPoint);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
