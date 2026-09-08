using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Theoriz.GenUI.Tests
{
    public class CatchUpTarget : MonoBehaviour
    {
        public float myValue;
    }

    public class CatchUpControllable : Controllable
    {
        [OCFProperty] public float myValue;
    }

    /// <summary>
    /// PlayMode test for UIMaster catching up on controllables that registered before it existed.
    ///
    /// Guards the removed -100 execution order: UIMaster.Awake subscribes to controllableAdded while
    /// Controllable registers in OnEnable, so without the catch-up a UIMaster whose Awake runs second
    /// misses everything already registered. It also covers a UIMaster created at runtime - an
    /// additively loaded scene, or Add GenUI to Scene during Play - which came up blank whatever its
    /// order. Do not weaken it.
    /// </summary>
    public class UIMasterCatchUpTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        GameObject New(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }

        void RegisterControllable(string id)
        {
            var go = New("catchup-" + id);
            go.SetActive(false);
            var target = go.AddComponent<CatchUpTarget>();
            var controllable = go.AddComponent<CatchUpControllable>();
            controllable.controllableTargetScript = target;
            controllable.controllableUsePresets = false;
            controllable.controllableId = id;
            // Awake and OnEnable run here, so the id is registered before any UIMaster exists.
            go.SetActive(true);
        }

        /// <summary>
        /// The minimum hierarchy UIMaster.Awake reads: a child canvas carrying the CanvasScaler, a
        /// scroll view under it, and a ScrollRect with its content. Added while inactive so Awake
        /// runs when the object is activated, not when the component is added.
        /// </summary>
        UIMaster BuildUIMaster()
        {
            var root = New("catchup-uimaster");
            root.SetActive(false);

            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(root.transform, false);

            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollView.transform.SetParent(canvas.transform, false);

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollView.transform, false);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = (RectTransform)content.transform;

            var master = root.AddComponent<UIMaster>();
            root.SetActive(true);

            return master;
        }

        [SetUp]
        public void SetUp()
        {
            // UIMaster.Start warns when the scene has none, and a warning is noise, not a failure.
            // No input module: the project is on the Input System, and StandaloneInputModule reads
            // the legacy UnityEngine.Input, which throws under that setting.
            var events = New("catchup-eventsystem");
            events.AddComponent<EventSystem>();
        }

        [UnityTest]
        public IEnumerator UIMasterBuiltAfterRegistration_StillPanelsEveryControllable()
        {
            // Deliberately not in ComparePanels order, so the assertion cannot pass by luck.
            var ids = new[] { "zeta", "alpha", "mike" };

            foreach (var id in ids)
                RegisterControllable(id);

            yield return null;

            var master = BuildUIMaster();

            yield return null;

            var content = master.GetComponentInChildren<ScrollRect>(true).content;
            var panelled = new List<string>();

            //Every panel root is named "Panel", so identity comes from the Controllable it was built for.
            foreach (Transform child in content)
            {
                var panel = child.GetComponentInChildren<PanelUI>(true);
                if (panel != null && panel.LinkedControllable != null)
                    panelled.Add(panel.LinkedControllable.controllableId);
            }

            Assert.AreEqual(ids.Length, panelled.Count,
                "A UIMaster created after registration must still build a panel for every controllable.");

            var expected = new List<string>(ids);
            expected.Sort((a, b) => GenUIPanelSettings.ComparePanels(a, 0, b, 0));

            CollectionAssert.AreEqual(expected, panelled,
                "Panels must be ordered by ComparePanels, not by the order they were created in.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();

            // Controllable registration is static and survives between tests.
            ControllableMaster.RegisteredControllables.Clear();
        }
    }
}
