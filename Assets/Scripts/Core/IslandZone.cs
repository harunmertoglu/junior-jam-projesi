using UnityEngine;

// Bir adanın tamamını kaplayan büyük trigger. Oyuncu adaya girince (yürüyerek, IslandExit ile
// ışınlanarak ya da checkpoint'e dönerek) o adanın mevsim değerini SeasonManager'a yazar.
// Tüm adalar tek sahnede olduğu için mevsim sahneye göre değil, bu bölgelere göre değişir.
// Kurulum: her adaya bir tane, BoxCollider (Is Trigger) adayı ve havasını kaplasın, bölgeler üst üste binmesin.
public class IslandZone : MonoBehaviour
{
    [Header("Island")]
    // Ada 1: 0, Ada 2: 0.2, Ada 3: 0.4, Ada 4: 0.6, Ada 5: 0.8, Ada 6: 1
    [SerializeField, Range(0f, 1f)] float seasonValue;
    // Ada 1: 10, Ada 2: 9 ... Ada 6: 5
    [SerializeField] float recallDuration = 10f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        SeasonManager.Instance.SetIslandSeason(seasonValue, recallDuration);
    }
}
