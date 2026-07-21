using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Theoriz.GenUI.Editor
{
    public static class GenUIMenuItems
    {
        // Two independent steps, each a no-op when the scene already has what it provides, so the
        // entry is safe to run on a scene that has GenUI, an EventSystem, both or neither.
        [MenuItem("Theoriz/GenUI/Add GenUI to Scene")]
        public static void AddGenUIToScene()
        {
            var genUI = EnsureGenUI();
            EnsureEventSystem();

            if (genUI != null)
                Selection.activeGameObject = genUI;
        }

        // Instantiates the GenUI prefab unless the open scenes already hold a UIMaster; returns the
        // GenUI object either way, or null if the prefab could not be found.
        public static GameObject EnsureGenUI()
        {
            var existing = Object.FindAnyObjectByType<UIMaster>();
            if (existing != null)
            {
                Debug.Log("[GenUI] GenUI is already in the scene; leaving it as it is.");
                return existing.gameObject;
            }

            string root = GetPackageRootPath();
            if (root == null)
            {
                Debug.LogError("Could not locate the GenUI package root.");
                return null;
            }

            string prefabPath = $"{root}/Samples/Prefab/GenUI.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"GenUI prefab not found at '{prefabPath}'.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add GenUI to Scene");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return instance;
        }

        // Creates an EventSystem only when the open scenes have none. A pre-existing one is left
        // untouched: GenUI never replaces, disables or reconfigures the host project's EventSystem.
        public static GameObject EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
                return existing.gameObject;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            // No AssignDefaultActions() call: InputSystemUIInputModule.OnEnable assigns the default
            // actions itself when the module has none.
            eventSystem.AddComponent<InputSystemUIInputModule>();

            Undo.RegisterCreatedObjectUndo(eventSystem, "Add EventSystem");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return eventSystem;
        }

        static string GetPackageRootPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GenUIMenuItems).Assembly);
            if (packageInfo != null)
                return packageInfo.assetPath;

            // Fallback for assemblies living under Assets/ (not a proper UPM package)
            foreach (var guid in AssetDatabase.FindAssets("GenUIMenuItems t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);

                // FindAssets matches on substrings, so anything merely starting with the name comes
                // back too (GenUIMenuItemsTests, or a user script). Only this file locates the root.
                if (Path.GetFileNameWithoutExtension(scriptPath) != "GenUIMenuItems")
                    continue;

                // scriptPath = "<root>/Editor/GenUIMenuItems.cs" — go up two levels
                return Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)).Replace('\\', '/');
            }

            return null;
        }
    }
}
