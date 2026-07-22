using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// EditMode tests pinning UIMaster's static reset, which matches the one ControllableMaster and
    /// OSCMaster already had.
    ///
    /// UIMaster.Instance is assigned in Awake and nulled in OnDestroy, so it is covered in the normal
    /// case. The reset is what covers the abnormal one: with "Enter Play Mode Options ▸ Reload Domain"
    /// off, a session that ends without OnDestroy running leaves Instance pointing at a destroyed
    /// object, and the next session reads it as a live panel.
    ///
    /// These are structural checks. Assigning Instance for real needs a UIMaster component, whose
    /// Awake loads the prefab set and subscribes to ControllableMaster — far more than this is worth
    /// in an EditMode test, and side effects the rest of the suite would inherit.
    /// </summary>
    public class UIMasterResetStaticsTests
    {
        static MethodInfo ResetStatics()
        {
            return typeof(UIMaster).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Test]
        public void UIMaster_DeclaresResetStatics()
        {
            Assert.IsNotNull(ResetStatics(),
                "UIMaster.ResetStatics is gone. Unity calls it by attribute, so nothing else " +
                "references it by name and its removal is silent.");
        }

        [Test]
        public void ResetStatics_RunsBeforeAnySceneObjectAwakes()
        {
            var attribute = (RuntimeInitializeOnLoadMethodAttribute)
                Attribute.GetCustomAttribute(ResetStatics(), typeof(RuntimeInitializeOnLoadMethodAttribute));

            Assert.IsNotNull(attribute, "Unity only calls ResetStatics because of this attribute.");
            Assert.AreEqual(RuntimeInitializeLoadType.SubsystemRegistration, attribute.loadType,
                "UIMaster assigns Instance in Awake, so the clear has to happen before that.");
        }

        /// <summary>
        /// Nothing is registered in EditMode, so this asserts the reset is callable and leaves
        /// Instance null rather than that it cleared a live value.
        /// </summary>
        [Test]
        public void ResetStatics_LeavesInstanceNull()
        {
            ResetStatics().Invoke(null, null);

            Assert.IsNull(UIMaster.Instance);
        }
    }
}
