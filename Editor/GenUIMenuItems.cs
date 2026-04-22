using System.IO;
using UnityEditor;
using UnityEngine;

namespace Theoriz.GenUI.Editor
{
    public static class GenUIMenuItems
    {
        [MenuItem("Theoriz/GenUI/Add GenUI to Scene")]
        public static void AddGenUIToScene()
        {
            string root = GetPackageRootPath();
            if (root == null)
            {
                Debug.LogError("Could not locate the GenUI package root.");
                return;
            }

            string prefabPath = $"{root}/Samples/Prefab/GenUI.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"GenUI prefab not found at '{prefabPath}'.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add GenUI to Scene");
            Selection.activeGameObject = instance;
        }

        static string GetPackageRootPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GenUIMenuItems).Assembly);
            if (packageInfo != null)
                return packageInfo.assetPath;

            // Fallback for assemblies living under Assets/ (not a proper UPM package)
            var guids = AssetDatabase.FindAssets("GenUIMenuItems t:MonoScript");
            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                // scriptPath = "<root>/Editor/GenUIMenuItems.cs" — go up two levels
                return Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)).Replace('\\', '/');
            }

            return null;
        }
    }
}
