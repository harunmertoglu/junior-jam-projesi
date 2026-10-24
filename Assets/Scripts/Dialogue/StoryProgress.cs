using System;
using UnityEngine;

// Oyunun hikayede nerede olduğu: tek bir sayı. 0'dan başlar, sadece ileri gider.
// Konuşmalar (StoryNPC) ve alanlar (StoryStepZone) bu sayıyı artırır, NPC'ler buna bakıp ne söyleyeceğini seçer.
// Bir alana girip çıkmak sayıyı değiştirmez, sadece bir konuşma bitince ya da yeni bir yere varınca artar.
public static class StoryProgress
{
    public static event Action<int> OnStepChanged;

    static int currentStep;

    // Domain reload kapalıyken de Play'e her basışta hikaye baştan başlasın
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStory()
    {
        currentStep = 0;
    }

    public static int GetStep()
    {
        return currentStep;
    }

    // Hikaye geri gitmez: küçük bir sayı verilirse bir şey olmaz
    public static void ReachStep(int step)
    {
        if (step <= currentStep)
        {
            return;
        }

        currentStep = step;
        OnStepChanged?.Invoke(currentStep);
    }
}
