using System;
using UnityEngine;

// Hikaye anlatan karakter. Oyuncu yaklaşınca (trigger'a girince) konuşur.
// Konuşmalar listesi sırayla söylenir ama her biri hikaye belli bir adıma gelince açılır.
// Örnek: 1. konuşma "Gerekli Adım 0", bitince hikaye 1 olur. 2. konuşma "Gerekli Adım 3":
// oyuncu girip çıksa da 2. konuşma gelmez, hikaye 3'e ulaşınca (başka bir NPC ya da StoryStepZone ile) söylenir.
// Yeni bir şey yoksa waitingDialogue söylenir (boşsa susar).
// Kurulum: karaktere bu script + Is Trigger işaretli bir collider (eklenince SphereCollider kendiliğinden gelir).
public class StoryNPC : MonoBehaviour
{
    [Serializable]
    public class Conversation
    {
        public DialogueData dialogue;
        // Hikaye en az bu adımdaysa söylenir
        public int requiredStep;
        // Konuşma bitince hikaye bu adıma geçer. 0 = hikayeyi ilerletmez
        public int stepAfter;
    }

    [Header("Dialogue")]
    [SerializeField] Conversation[] conversations;
    // Söylenecek yeni bir şey yokken (sırası gelmedi ya da hepsini söyledi): ör. "Yolun açık olsun". Boş bırakılabilir
    [SerializeField] DialogueData waitingDialogue;

    [Header("Look")]
    // Konuşurken oyuncuya dönsün (sadece yatayda)
    [SerializeField] bool turnToPlayer = true;
    [SerializeField] float turnSpeed = 6f;

    // Kaç konuşma söylendi. Liste sırayla gider, söylenen tekrar söylenmez
    int conversationIndex;
    Transform playerTransform;
    bool isTalking;

    void Reset()
    {
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
        }
        trigger.isTrigger = true;
        trigger.radius = 3f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        DialogueManager dialogueManager = DialogueManager.GetOrCreate();
        if (dialogueManager.IsOpen())
        {
            return;
        }

        Conversation nextConversation = GetNextConversation();
        DialogueData dialogueToPlay = nextConversation != null ? nextConversation.dialogue : waitingDialogue;
        if (dialogueToPlay == null)
        {
            return;
        }

        playerTransform = other.transform;
        isTalking = true;
        dialogueManager.StartDialogue(dialogueToPlay, () => DialogueFinished(nextConversation));
    }

    // Sıradaki konuşma, hikaye onun adımına geldiyse. Gelmediyse ya da liste bittiyse null
    Conversation GetNextConversation()
    {
        if (conversations == null || conversationIndex >= conversations.Length)
        {
            return null;
        }

        Conversation conversation = conversations[conversationIndex];
        return StoryProgress.GetStep() >= conversation.requiredStep ? conversation : null;
    }

    void DialogueFinished(Conversation finishedConversation)
    {
        isTalking = false;

        // waitingDialogue bittiyse hiçbir şey ilerlemez
        if (finishedConversation == null)
        {
            return;
        }

        conversationIndex++;
        StoryProgress.ReachStep(finishedConversation.stepAfter);
    }

    void Update()
    {
        if (!turnToPlayer || !isTalking || playerTransform == null)
        {
            return;
        }

        Vector3 lookDirection = playerTransform.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
