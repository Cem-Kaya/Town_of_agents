// File: Assets/Editor/ProjectMetricsExporter.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ProjectMetricsExporter : EditorWindow
{
    [MenuItem("Tools/Export Project Metrics")]
    public static void ExportProjectMetrics()
    {
        var metrics = new ProjectMetrics();

        // Count all prefabs and assets in project
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();

        metrics.totalAssets = allAssetPaths.Length;
        metrics.prefabs = allAssetPaths.Count(p => p.StartsWith("Assets/") && p.EndsWith(".prefab"));
        metrics.scripts = allAssetPaths.Count(p => p.StartsWith("Assets/Scripts") &&p.EndsWith(".cs"));
        metrics.textures = allAssetPaths.Count(p => p.StartsWith("Assets/") && p.EndsWith(".png") || p.EndsWith(".jpg"));
        metrics.sprites = allAssetPaths.Count(p => p.StartsWith("Assets/") && AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(Sprite));
        //metrics.materials = allAssetPaths.Count(p => p.EndsWith(".mat"));
        metrics.scenes = UnityEngine.SceneManagement.SceneManager.sceneCount; //allAssetPaths.Count(p => p.EndsWith(".unity"));
        
        // Count GameObjects and components in open scenes
        int totalGameObjects = 0;
        int totalComponents = 0;

        for (int i = 0; i < metrics.scenes; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var go in scene.GetRootGameObjects())
            {
                var allChildren = go.GetComponentsInChildren<Transform>(true);
                totalGameObjects += allChildren.Length;
                foreach (var child in allChildren)
                    totalComponents += child.GetComponents<Component>().Length;
            }
        }
        metrics.gameObjects = totalGameObjects;
        metrics.components = totalComponents;

        // Export to JSON
        string json = JsonUtility.ToJson(metrics, true);
        File.WriteAllText("ProjectMetrics.json", json);
        Debug.Log($"✅ Metrics exported to ProjectMetrics.json\n{json}");
    }

    [System.Serializable]
    public class ProjectMetrics
    {
        public int totalAssets;
        public int prefabs;
        public int scripts;
        public int textures;
        public int sprites;
        public int materials;
        public int scenes;
        public int gameObjects;
        public int components;
    }
}
