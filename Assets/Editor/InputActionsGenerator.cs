using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

// Sadece Editor'de çalışır.
// Menü: Junyorjem > Regenerate Input Actions
// InputSystem_Actions'ta action ekleyip çıkarınca InputActions.cs kendiliğinden güncellenmezse bunu çalıştır.
public static class InputActionsGenerator
{
    const string actionsPath = "Assets/InputSystem_Actions.inputactions";
    const string wrapperPath = "Assets/Scripts/InputActions.cs";

    [MenuItem("Junyorjem/Regenerate Input Actions")]
    public static void RegenerateInputActions()
    {
        InputActionAsset actionsAsset = InputActionAsset.FromJson(File.ReadAllText(actionsPath));

        InputActionCodeGenerator.Options options = new InputActionCodeGenerator.Options
        {
            sourceAssetPath = actionsPath,
            className = "InputActions",
            namespaceName = ""
        };

        InputActionCodeGenerator.GenerateWrapperCode(wrapperPath, actionsAsset, options);
        AssetDatabase.ImportAsset(wrapperPath);
        Debug.Log("InputActionsGenerator: " + wrapperPath + " güncellendi");
    }
}
