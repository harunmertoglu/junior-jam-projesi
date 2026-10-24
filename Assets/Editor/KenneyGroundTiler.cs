using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Junyorjem > Kenney Ground Döşe
// Sahnedeki ground objelerinin üstüne Kenney platformer bloklarını döşer.
// Bloğun üst yüzeyi ground'ın üst yüzeyiyle hizalanır (pivot = alt merkez varsayımı).
// Tekrar çalıştırınca önceki blokları silip yeniden döşer. Ctrl+Z ile geri alınabilir.
public static class KenneyGroundTiler
{
    const string snowBlockPath = "Assets/3rdPartyAssets/kenney_platformer/FBX format/block-snow.fbx";
    const string grassBlockPath = "Assets/3rdPartyAssets/kenney_platformer/FBX format/block-grass.fbx";

    [MenuItem("Junyorjem/Kenney Ground Döşe")]
    static void TileAll()
    {
        // Kar/kış bölgesi: melting platform ve snowflake platformların yanındaki alanlar
        Tile("GroundB", snowBlockPath);
        Tile("GroundC", snowBlockPath);
        Tile("GroundD", snowBlockPath);

        // Sonbahar bölgesi: ıslak/yeşil, kardan önce
        Tile("WetGround", grassBlockPath);

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("KenneyGroundTiler: döşeme tamamlandı.");
    }

    static void Tile(string groundName, string fbxPath)
    {
        GameObject ground = GameObject.Find(groundName);
        if (ground == null)
        {
            Debug.LogWarning($"KenneyGroundTiler: '{groundName}' sahnede bulunamadı, atlanıyor.");
            return;
        }

        // Önceki container varsa temizle
        string containerName = groundName + "_Kenney";
        GameObject existing = GameObject.Find(containerName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxModel == null)
        {
            Debug.LogError($"KenneyGroundTiler: FBX yüklenemedi: {fbxPath}");
            return;
        }

        Vector3 pos = ground.transform.position;
        Vector3 scale = ground.transform.lossyScale;
        int tilesX = Mathf.Max(1, Mathf.RoundToInt(scale.x));
        int tilesZ = Mathf.Max(1, Mathf.RoundToInt(scale.z));

        // Bloğun alt yüzeyi ground'ın üst yüzeyiyle hizalı (pivot = alt merkez)
        float topY = pos.y + scale.y * 0.5f;

        GameObject container = new GameObject(containerName);
        container.transform.SetParent(ground.transform.parent);
        Undo.RegisterCreatedObjectUndo(container, "Kenney Ground");

        for (int x = 0; x < tilesX; x++)
        {
            for (int z = 0; z < tilesZ; z++)
            {
                float bx = pos.x - scale.x * 0.5f + 0.5f + x;
                float bz = pos.z - scale.z * 0.5f + 0.5f + z;

                GameObject block = Object.Instantiate(fbxModel, container.transform);
                block.transform.position = new Vector3(bx, topY, bz);
                Undo.RegisterCreatedObjectUndo(block, "Kenney Block");
            }
        }

        // Ground cube görselini gizle: artık Kenney blokları temsil ediyor
        MeshRenderer cubeRenderer = ground.GetComponent<MeshRenderer>();
        if (cubeRenderer != null)
        {
            Undo.RecordObject(cubeRenderer, "Hide Ground Cube");
            cubeRenderer.enabled = false;
        }

        Debug.Log($"KenneyGroundTiler: '{groundName}' → {tilesX}×{tilesZ} = {tilesX * tilesZ} blok eklendi.");
    }
}
