using System.IO;
using UnityEditor;
using UnityEngine;

// Sumor (SunSoul) ve Lencten (JourneyCharacter) için URP Lit materyallerini oluşturur,
// texture'ları bağlar ve modellerin import ayarlarında bu materyalleri kullanır.
// Menü: Tools > NPC > Setup Character Materials
public static class NpcMaterialSetup
{
    const string Root = "Assets/3rdPartyAssets/NPC Characters/";

    [MenuItem("Tools/NPC/Setup Character Materials")]
    static void Setup()
    {
        SetupSumor();
        SetupLencten();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("NPC materyalleri hazır: Sumor_Mat ve Lencten_Mat");
    }

    static void SetupSumor()
    {
        string dir = Root + "SunSoul/";
        MarkNormalMap(dir + "textures/initialShadingGroup_Normal_DirectX.png", flipGreen: true);

        Material material = CreateLitMaterial(dir + "Materials/Sumor_Mat.mat");
        SetTexture(material, "_BaseMap", dir + "textures/initialShadingGroup_Base_Color.jpg");
        SetTexture(material, "_MetallicGlossMap", dir + "textures/initialShadingGroup_Metallic.png", "_METALLICSPECGLOSSMAP");
        SetTexture(material, "_BumpMap", dir + "textures/initialShadingGroup_Normal_DirectX.png", "_NORMALMAP");
        SetTexture(material, "_OcclusionMap", dir + "textures/ao.png", "_OCCLUSIONMAP");
        if (SetTexture(material, "_EmissionMap", dir + "textures/glow.png", "_EMISSION"))
        {
            material.SetColor("_EmissionColor", Color.white);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        material.SetFloat("_Smoothness", 0.4f);

        RemapModelMaterials(dir + "source/all.obj", material);
    }

    static void SetupLencten()
    {
        string dir = Root + "JourneyCharacter/";
        string tex = dir + "textures/Cloth and Character_";
        MarkNormalMap(tex + "Normal_OpenGL.png", flipGreen: false);

        Material material = CreateLitMaterial(dir + "Materials/Lencten_Mat.mat");
        SetTexture(material, "_BaseMap", tex + "Base_Color_v2.png");
        SetTexture(material, "_MetallicGlossMap", tex + "Metallic.png", "_METALLICSPECGLOSSMAP");
        SetTexture(material, "_BumpMap", tex + "Normal_OpenGL.png", "_NORMALMAP");
        SetTexture(material, "_OcclusionMap", tex + "Mixed_AO.png", "_OCCLUSIONMAP");
        material.SetFloat("_Smoothness", 0.3f);

        RemapModelMaterials(dir + "source/Jorney_clothes_v3.fbx", material);
    }

    static Material CreateLitMaterial(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    static bool SetTexture(Material material, string property, string texturePath, string keyword = null)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning("Texture bulunamadı: " + texturePath);
            return false;
        }

        material.SetTexture(property, texture);
        if (keyword != null) material.EnableKeyword(keyword);
        EditorUtility.SetDirty(material);
        return true;
    }

    static void MarkNormalMap(string texturePath, bool flipGreen)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.NormalMap;
        // DirectX normal map'lerin yeşil kanalı Unity'ye göre ters
        importer.flipGreenChannel = flipGreen;
        importer.SaveAndReimport();
    }

    // Modelin içindeki tüm materyal slotlarını verilen materyale bağlar
    static void RemapModelMaterials(string modelPath, Material material)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("Model bulunamadı: " + modelPath);
            return;
        }

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (asset is Material embedded)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
            }
        }
        importer.SaveAndReimport();
    }
}
