using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshBakeHelper
{
    [MenuItem("Tools/Bake Classroom NavMesh")]
    public static void BakeClassroomNavMesh()
    {
        GameObject host = GameObject.Find("NavMeshSurface");
        if (host == null)
        {
            host = new GameObject("NavMeshSurface");
            Undo.RegisterCreatedObjectUndo(host, "Create NavMeshSurface");
        }

        NavMeshSurface surface = host.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = host.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;

        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(host.scene);
        Debug.Log(
            $"[NavMeshBakeHelper] Bake complete. navMeshData={(surface.navMeshData != null ? "present" : "null")}"
        );
    }
}
