using UnityEngine;

// Oyuncu bu alana ilk girdiğinde hikayeyi bir adıma ilerletir. Level olmadığı için "buraya vardın" demenin yolu bu.
// Örnek: 2. adanın girişine koy, Step To Reach 3 yap. Ada 1'deki NPC'nin 3 isteyen konuşması o zaman açılır.
// Girip çıkmak bir şey değiştirmez: hikaye zaten o adımdaysa ya da ilerideyse hiçbir şey olmaz.
// Kurulum: boş obje + BoxCollider (Is Trigger) + bu script.
public class StoryStepZone : MonoBehaviour
{
    [SerializeField] int stepToReach = 1;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        StoryProgress.ReachStep(stepToReach);
    }
}
