using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Theoriz.GenUI.Tests
{
    /// <summary>
    /// PlayMode tests for <see cref="EventSystemCheck"/>, the runtime half of "GenUI does not own
    /// the scene's EventSystem". The helper must report a missing one and never create anything —
    /// silently adding one is what produced the duplicate-EventSystem conflict in the first place.
    /// </summary>
    public class EventSystemCheckTests
    {
        GameObject _created;

        [TearDown]
        public void TearDown()
        {
            // A leaked EventSystem poisons every later test in the run.
            if (_created != null)
                Object.DestroyImmediate(_created);
        }

        [UnityTest]
        public IEnumerator NoEventSystem_LogsWarning_AndReturnsFalse()
        {
            Assert.IsNull(Object.FindAnyObjectByType<EventSystem>(), "The scene must start without an EventSystem.");

            LogAssert.Expect(LogType.Warning, EventSystemCheck.MissingMessage);

            Assert.IsFalse(EventSystemCheck.WarnIfMissing());

            yield return null;

            Assert.IsNull(Object.FindAnyObjectByType<EventSystem>(), "The check must not create an EventSystem.");
        }

        [UnityTest]
        public IEnumerator EventSystemPresent_NoWarning_AndReturnsTrue()
        {
            _created = new GameObject("EventSystem", typeof(EventSystem));

            yield return null;

            Assert.IsTrue(EventSystemCheck.WarnIfMissing());
            Assert.AreEqual(1, CountIn<EventSystem>(SceneManager.GetActiveScene()),
                "The check must not add a second EventSystem.");

            LogAssert.NoUnexpectedReceived();
        }

        // Counts components in one scene. FindObjectsByType's only 2022.3 overload takes a
        // FindObjectsSortMode, which Unity 6.5 deprecates, so walking the scene is what keeps this
        // warning-free across the supported range — and it scopes the count to the test's own scene
        // instead of everything the editor happens to have loaded.
        static int CountIn<T>(Scene scene) where T : Component
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }
    }
}
