using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers what Ctrl+Z pops and in what order. Coalescing is the part that matters: a slider drag
    /// and a label scrub both write continuously, and without it one press would undo a single frame
    /// of the gesture.
    ///
    /// Capturing a member's value and writing it back need a Controllable and a live panel, so they
    /// are Editor checks.
    /// </summary>
    public class UndoStackTests
    {
        /// <summary>
        /// Stands in for a widget whose member already holds the recorded value - what a field
        /// committed by losing focus, without being edited, leaves on the stack.
        /// </summary>
        class UnchangedWidget : ControllableUI
        {
            public override bool HoldsValue(UndoStack.Value value)
            {
                return true;
            }
        }

        readonly List<GameObject> _created = new List<GameObject>();

        UndoStack _stack;

        [SetUp]
        public void SetUp()
        {
            _stack = new UndoStack();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        ControllableUI MakeWidget()
        {
            var go = new GameObject("Widget");
            _created.Add(go);
            return go.AddComponent<ControllableUI>();
        }

        static UndoStack.Value ValueOf(float number)
        {
            return new UndoStack.Value(new List<object> { number });
        }

        static float NumberIn(UndoStack.Entry entry)
        {
            return (float)entry.Value.Values[0];
        }

        [Test]
        public void EmptyStack_HasNothingToUndo()
        {
            UndoStack.Entry entry;
            Assert.IsFalse(_stack.TryPop(out entry));
        }

        [Test]
        public void PopReturnsTheMostRecentEdit_ThenTheOneBefore()
        {
            var widget = MakeWidget();
            var other = MakeWidget();

            _stack.Record(widget, ValueOf(1f), 0f);
            _stack.Record(other, ValueOf(2f), 10f);

            UndoStack.Entry entry;

            Assert.IsTrue(_stack.TryPop(out entry));
            Assert.AreEqual(2f, NumberIn(entry));

            Assert.IsTrue(_stack.TryPop(out entry));
            Assert.AreEqual(1f, NumberIn(entry));

            Assert.IsFalse(_stack.TryPop(out entry), "Both edits have been undone.");
        }

        /// <summary>
        /// One gesture is one undo, and the value it restores is the one from before the gesture
        /// started - not the one from the previous frame of it.
        /// </summary>
        [Test]
        public void WritesWithinTheWindow_CoalesceAndKeepTheOldestValue()
        {
            var widget = MakeWidget();

            _stack.Record(widget, ValueOf(1f), 0f);
            _stack.Record(widget, ValueOf(2f), 0.1f);
            _stack.Record(widget, ValueOf(3f), 0.2f);

            Assert.AreEqual(1, _stack.Count);

            UndoStack.Entry entry;
            Assert.IsTrue(_stack.TryPop(out entry));
            Assert.AreEqual(1f, NumberIn(entry), "A drag should undo to where it started.");
        }

        /// <summary>
        /// The window slides with each write, so a drag lasting longer than it still coalesces.
        /// </summary>
        [Test]
        public void ALongGesture_StaysOneEntry()
        {
            var widget = MakeWidget();

            for (var frame = 0; frame < 100; frame++)
                _stack.Record(widget, ValueOf(frame), frame * 0.02f);

            Assert.AreEqual(1, _stack.Count);
        }

        [Test]
        public void WritesFurtherApart_StaySeparateEdits()
        {
            var widget = MakeWidget();

            _stack.Record(widget, ValueOf(1f), 0f);
            _stack.Record(widget, ValueOf(2f), UndoStack.CoalesceWindow + 0.1f);

            Assert.AreEqual(2, _stack.Count);
        }

        [Test]
        public void DifferentWidgets_NeverCoalesce()
        {
            var widget = MakeWidget();
            var other = MakeWidget();

            _stack.Record(widget, ValueOf(1f), 0f);
            _stack.Record(other, ValueOf(2f), 0f);

            Assert.AreEqual(2, _stack.Count, "Editing another member is a separate edit however fast it follows.");
        }

        [Test]
        public void PastTheDepthCap_TheOldestEditsFallOff()
        {
            var widget = MakeWidget();
            var other = MakeWidget();

            //Alternating widgets keeps every write a separate entry rather than one coalesced gesture.
            for (var i = 0; i < UndoStack.MaxDepth + 10; i++)
                _stack.Record(i % 2 == 0 ? widget : other, ValueOf(i), i);

            Assert.AreEqual(UndoStack.MaxDepth, _stack.Count);
        }

        /// <summary>
        /// Panels are destroyed when their controllable goes away, which can leave entries pointing at
        /// widgets that no longer exist. Undo has to walk past them rather than throw or appear dead.
        /// </summary>
        [Test]
        public void EntriesWhoseWidgetIsGone_AreSkipped()
        {
            var survivor = MakeWidget();
            var doomed = MakeWidget();

            _stack.Record(survivor, ValueOf(1f), 0f);
            _stack.Record(doomed, ValueOf(2f), 10f);

            Object.DestroyImmediate(doomed.gameObject);

            UndoStack.Entry entry;
            Assert.IsTrue(_stack.TryPop(out entry));
            Assert.AreEqual(1f, NumberIn(entry), "The destroyed widget's edit should be dropped, not returned.");
        }

        /// <summary>
        /// Leaving a field commits it whether or not it was edited, so the stack collects entries that
        /// would restore the value already there. Undo has to reach past them to the last real edit.
        /// </summary>
        [Test]
        public void EntriesThatWouldChangeNothing_AreSkipped()
        {
            var edited = MakeWidget();

            var committedGo = new GameObject("Unchanged");
            _created.Add(committedGo);
            var committed = committedGo.AddComponent<UnchangedWidget>();

            _stack.Record(edited, ValueOf(1f), 0f);
            _stack.Record(committed, ValueOf(2f), 10f);

            UndoStack.Entry entry;
            Assert.IsTrue(_stack.TryPop(out entry));
            Assert.AreEqual(1f, NumberIn(entry), "The no-op entry should be passed over, not returned.");
        }

        [Test]
        public void RecordingAgainstNoWidget_IsIgnored()
        {
            _stack.Record(null, ValueOf(1f), 0f);

            Assert.AreEqual(0, _stack.Count);
        }
    }
}
