using UnityEditor;
using UnityEngine;

// Menü: Junyorjem > SpeedBoostPad → Buz Görseli Uygula
// Parkur2 ve Parkour3 prefablarındaki tüm SpeedBoostPad objelerinin
// mesh ve materyalini IcePlane asset'iyle değiştirir.
// Collider, SpeedBoostPad script ve transform değerleri değişmez.
public static class SpeedBoostPadIceReplacer
{
    const string iceFbxPath = "Assets/3rdPartyAssets/SnowAssets/IcePlane/source/ice.fbx";
    const string basecolorPath = "Assets/3rdPartyAssets/SnowAssets/IcePlane/textures/New_Graph_basecolor.png";
    const string normalMapPath = "Assets/3rdPartyAssets/SnowAssets/IcePlane/textures/New_Graph_normal.png";
    const string roughnessPath = "Assets/3rdPartyAssets/SnowAssets/IcePlane/textures/New_Graph_roughness.png";
    const string iceMaterialPath = "Assets/3rdPartyAssets/SnowAssets/IcePlane/IcePad.mat";

    static readonly string[] prefabPaths =
    {
        "Assets/ParkourPrefabs/Parkur2.prefab",
        "Assets/ParkourPrefabs/Parkour3.prefab",
    };

    [MenuItem("Junyorjem/SpeedBoostPad → Buz Görseli Uygula")]
    static void ReplaceAll()
    {
        Mesh iceMesh = GetIceMesh();
        if (iceMesh == null)
        {
            Debug.LogError($"SpeedBoostPadIceReplacer: buz mesh yüklenemedi → {iceFbxPath}");
            return;
        }

        EnsureNormalMapImportType();
        Material iceMaterial = GetOrCreateIceMaterial();

        foreach (string path in prefabPaths)
            ReplaceInPrefab(path, iceMesh, iceMaterial);

        AssetDatabase.SaveAssets();
        Debug.Log("SpeedBoostPadIceReplacer: tüm prefablara buz görseli uygulandı.");
    }

    static Mesh GetIceMesh()
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(iceFbxPath);
        if (fbxRoot == null) return null;

        MeshFilter mf = fbxRoot.GetComponentInChildren<MeshFilter>(true);
        return mf != null ? mf.sharedMesh : null;
    }

    static void EnsureNormalMapImportType()
    {
        TextureImporter importer = AssetImporter.GetAtPath(normalMapPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    static Material GetOrCreateIceMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(iceMaterialPath);
        if (existing != null) return existing;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "IcePad";

        Texture2D basecolor = AssetDatabase.LoadAssetAtPath<Texture2D>(basecolorPath);
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);

        if (basecolor != null)
            mat.SetTexture("_BaseMap", basecolor);

        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Roughness map: URP'de smoothness = 1 - roughness olduğu için
        // burada float olarak ayarlıyoruz; buz yüzeyi parlak
        mat.SetFloat("_Smoothness", 0.85f);
        mat.SetFloat("_Metallic", 0f);

        // Roughness texture'ı _MetallicGlossMap'e A kanalı olarak kullanmak
        // daha doğru sonuç verir ama kaynak texture'ın formatına bağlı.
        // Şimdilik ek texture atıyoruz, Unity içinde gerekirse kanalı ayarlayabilirsin.
        if (roughness != null)
            mat.SetTexture("_MetallicGlossMap", roughness);

        AssetDatabase.CreateAsset(mat, iceMaterialPath);
        return mat;
    }

    static void ReplaceInPrefab(string path, Mesh mesh, Material material)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"SpeedBoostPadIceReplacer: prefab yüklenemedi → {path}");
            return;
        }

        SpeedBoostPad[] pads = prefabRoot.GetComponentsInChildren<SpeedBoostPad>(true);

        foreach (SpeedBoostPad pad in pads)
        {
            MeshFilter mf = pad.GetComponent<MeshFilter>();
            MeshRenderer mr = pad.GetComponent<MeshRenderer>();

            if (mf != null) mf.sharedMesh = mesh;
            if (mr != null) mr.sharedMaterials = new Material[] { material };
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log($"SpeedBoostPadIceReplacer: '{path}' → {pads.Length} SpeedBoostPad güncellendi.");
    }
}
