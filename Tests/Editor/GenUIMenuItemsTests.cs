using System.Collections.Generic;
using NUnit.Framework;
using Theoriz.GenUI.Editor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// EditMode tests for "Add GenUI to Scene". Both of its steps are guarded, so the entry must be
    /// safe to run on a scene that already has GenUI, an EventSystem, both or neither — that is
    /// what stops it recreating the duplicate EventSystem this package used to ship.
    ///
    /// The fixture deliberately creates no scene: `EditorSceneManager.NewScene` refuses to run
    /// additively while an untitled scene is unsaved, which made these tests depend on the state
    /// the editor happened to be in. Instead it deactivates whatever the open scene already holds
    /// so the code under test sees a clean slate, and reactivates it afterwards — nothing the user
    /// had is destroyed.
    /// </summary>
    public class GenUIMenuItemsTests
    {
        // Guards against hanging the editor if a lookup ever stops excluding inactive objects.
        const int SearchCap = 100;

        List<GameObject> _hidden;

        [SetUp]
        public void SetUp()
        {
            _hidden = new List<GameObject>();
            HideActive<UIMaster>();
            HideActive<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            // Anything of these types still active is what the test itself created.
            DestroyActive<UIMaster>();
            DestroyActive<EventSystem>();

            foreach (var go in _hidden)
            {
                if (go != null)
                    go.SetActive(true);
            }

            _hidden.Clear();
        }

        [Test]
        public void Menu_OnEmptyScene_AddsGenUIAndEventSystem()
        {
            GenUIMenuItems.AddGenUIToScene();

            Assert.AreEqual(1, CountActive<UIMaster>());
            Assert.AreEqual(1, CountActive<EventSystem>());
        }

        [Test]
        public void Menu_CreatesAnEventSystemDrivenByTheInputSystem()
        {
            GenUIMenuItems.AddGenUIToScene();

            Assert.IsNotNull(Object.FindAnyObjectByType<InputSystemUIInputModule>(),
                "The created EventSystem must carry an InputSystemUIInputModule.");
        }

        [Test]
        public void Menu_RunTwice_AddsNeitherTwice()
        {
            GenUIMenuItems.AddGenUIToScene();
            GenUIMenuItems.AddGenUIToScene();

            Assert.AreEqual(1, CountActive<UIMaster>());
            Assert.AreEqual(1, CountActive<EventSystem>());
        }

        [Test]
        public void Menu_WithExistingEventSystem_LeavesItAlone()
        {
            var mine = new GameObject("My EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            GenUIMenuItems.AddGenUIToScene();

            Assert.AreEqual(1, CountActive<EventSystem>());
            Assert.AreSame(mine, Object.FindAnyObjectByType<EventSystem>(),
                "The host project's EventSystem must be the one still there.");
            Assert.IsNull(mine.GetComponent<InputSystemUIInputModule>(), "It must not be reconfigured.");
            Assert.AreEqual(1, CountActive<UIMaster>(), "GenUI must still be added.");
        }

        /// <summary>
        /// The demo-scene path: GenUI already present but no EventSystem — the second step has to
        /// run even though the first one found nothing to do.
        /// </summary>
        [Test]
        public void Menu_WithExistingGenUI_StillAddsEventSystem()
        {
            GenUIMenuItems.EnsureGenUI();
            Assert.AreEqual(0, CountActive<EventSystem>());

            GenUIMenuItems.AddGenUIToScene();

            Assert.AreEqual(1, CountActive<UIMaster>());
            Assert.AreEqual(1, CountActive<EventSystem>());
        }

        // FindAnyObjectByType skips inactive objects, so deactivating each hit walks the whole set.
        // It is the only lookup that is neither deprecated on Unity 6.5 nor missing on the 2022.3
        // this package targets — hence this shape rather than FindObjectsByType.
        void HideActive<T>() where T : Component
        {
            for (var i = 0; i < SearchCap; i++)
            {
                var found = Object.FindAnyObjectByType<T>();
                if (found == null)
                    return;

                found.gameObject.SetActive(false);
                _hidden.Add(found.gameObject);
            }
        }

        static void DestroyActive<T>() where T : Component
        {
            for (var i = 0; i < SearchCap; i++)
            {
                var found = Object.FindAnyObjectByType<T>();
                if (found == null)
                    return;

                Object.DestroyImmediate(found.gameObject);
            }
        }

        // Counts the active GameObjects carrying T, restoring every one it hid to do so.
        static int CountActive<T>() where T : Component
        {
            var seen = new List<GameObject>();

            for (var i = 0; i < SearchCap; i++)
            {
                var found = Object.FindAnyObjectByType<T>();
                if (found == null)
                    break;

                found.gameObject.SetActive(false);
                seen.Add(found.gameObject);
            }

            foreach (var go in seen)
                go.SetActive(true);

            return seen.Count;
        }
    }
}
