using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GrassScatter : EditorWindow
{
    private const string GrassPrefabFolder =
        "Assets/Imported/EnvironmentAssetKit/Atlas/Prefabs_Atlas/Grass";

    private const float RaycastStartHeight = 10f;
    private const float RaycastMaxDistance = 20f;
    private const float SurfaceSinkOffset = 0.01f;
    private const int TestModeSampleCount = 20;

    private bool targetGround = true;
    private bool targetSoilBase = true;
    private float density = 0.1f;
    private bool testMode = true;
    private float minScale = 0.5f;
    private float maxScale = 0.8f;
    private bool randomYRotation = true;
    private int randomSeed = 0;
    private string parentName = "Grass_Scatter";

    [MenuItem("Tools/Grass Scatter")]
    private static void Open()
    {
        GetWindow<GrassScatter>("Grass Scatter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        targetGround = EditorGUILayout.Toggle("Ground", targetGround);
        targetSoilBase = EditorGUILayout.Toggle("Soil_Base (Flowerbed)", targetSoilBase);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Distribution", EditorStyles.boldLabel);
        density = EditorGUILayout.FloatField("Density (per m²)", density);
        testMode = EditorGUILayout.Toggle(
            new GUIContent("Test Mode", $"Place only {TestModeSampleCount} samples per target."),
            testMode
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Variation", EditorStyles.boldLabel);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);
        randomSeed = EditorGUILayout.IntField(
            new GUIContent("Random Seed", "0 = time-based; nonzero = reproducible."),
            randomSeed
        );

        EditorGUILayout.Space();
        parentName = EditorGUILayout.TextField("Parent GO Name", parentName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Grass"))
        {
            Scatter();
        }
        if (GUILayout.Button("Clear Existing"))
        {
            ClearExisting();
        }
    }

    private void Scatter()
    {
        if (minScale <= 0f || maxScale < minScale)
        {
            EditorUtility.DisplayDialog(
                "Grass Scatter",
                "Min/Max scale invalid (min must be > 0 and max >= min).",
                "OK"
            );
            return;
        }

        var prefabs = LoadGrassPrefabs();
        if (prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Grass Scatter",
                $"No prefabs found under {GrassPrefabFolder}.",
                "OK"
            );
            return;
        }

        if (randomSeed != 0)
        {
            Random.InitState(randomSeed);
        }

        var parent = GetOrCreateParent();
        int placed = 0;
        int skipped = 0;

        if (targetGround)
        {
            var ground = FindByName("Ground");
            if (ground == null)
            {
                Debug.LogWarning("[GrassScatter] No GameObject named 'Ground' found.");
            }
            else
            {
                ScatterOnto(ground, prefabs, parent, ref placed, ref skipped);
            }
        }

        if (targetSoilBase)
        {
            var soil = FindByName("Soil_Base");
            if (soil == null)
            {
                Debug.LogWarning("[GrassScatter] No GameObject named 'Soil_Base' found.");
            }
            else
            {
                ScatterOnto(soil, prefabs, parent, ref placed, ref skipped);
            }
        }

        Debug.Log(
            $"[GrassScatter] Placed {placed} grass instances. Skipped {skipped} samples (raycast miss or wrong surface)."
        );

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneMarkDirty(activeScene);
    }

    private static void EditorSceneMarkDirty(UnityEngine.SceneManagement.Scene scene)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }

    private void ScatterOnto(
        GameObject target,
        List<GameObject> prefabs,
        Transform parent,
        ref int placed,
        ref int skipped
    )
    {
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[GrassScatter] '{target.name}' has no MeshRenderer; skipping.");
            return;
        }
        var bounds = renderer.bounds;

        var addedCollider = EnsureCollider(target, out bool didAdd);
        if (didAdd)
        {
            Physics.SyncTransforms();
        }

        int sampleCount;
        if (testMode)
        {
            sampleCount = TestModeSampleCount;
        }
        else
        {
            float area = bounds.size.x * bounds.size.z;
            sampleCount = Mathf.Max(1, Mathf.RoundToInt(area * density));
        }

        for (int i = 0; i < sampleCount; i++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            var origin = new Vector3(x, bounds.max.y + RaycastStartHeight, z);

            if (
                !Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    RaycastMaxDistance + RaycastStartHeight,
                    ~0,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                skipped++;
                continue;
            }

            if (hit.collider.gameObject != target)
            {
                skipped++;
                continue;
            }

            var prefab = prefabs[Random.Range(0, prefabs.Count)];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = hit.point - Vector3.up * SurfaceSinkOffset;

            float yaw = randomYRotation ? Random.Range(0f, 360f) : 0f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float s = Random.Range(minScale, maxScale);
            go.transform.localScale = Vector3.one * s;

            Undo.RegisterCreatedObjectUndo(go, "Scatter Grass");
            placed++;
        }

        if (didAdd && addedCollider != null)
        {
            Object.DestroyImmediate(addedCollider);
        }
    }

    private static Component EnsureCollider(GameObject target, out bool added)
    {
        added = false;
        var existing = target.GetComponent<Collider>();
        if (existing != null)
        {
            return existing;
        }
        var mc = target.AddComponent<MeshCollider>();
        var mf = target.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mc.sharedMesh = mf.sharedMesh;
        }
        added = true;
        return mc;
    }

    private static List<GameObject> LoadGrassPrefabs()
    {
        var list = new List<GameObject>();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { GrassPrefabFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                list.Add(prefab);
            }
        }
        return list;
    }

    private Transform GetOrCreateParent()
    {
        var existing = GameObject.Find(parentName);
        if (existing != null)
        {
            return existing.transform;
        }
        var go = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(go, "Create Grass Parent");
        return go.transform;
    }

    private void ClearExisting()
    {
        var existing = GameObject.Find(parentName);
        if (existing == null)
        {
            Debug.Log($"[GrassScatter] No '{parentName}' GameObject to clear.");
            return;
        }
        Undo.DestroyObjectImmediate(existing);
        Debug.Log($"[GrassScatter] Cleared '{parentName}'.");
    }

    private static GameObject FindByName(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.name != name)
                continue;
            if (!go.scene.IsValid())
                continue;
            if (go.hideFlags != HideFlags.None)
                continue;
            return go;
        }
        return null;
    }
}
