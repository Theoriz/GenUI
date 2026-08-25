using System.Threading;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The hand-off between the socket threads and the main thread: order, the backlog cap, and the
    /// rule that a connection notice is never the message dropped.
    /// </summary>
    public class WebMessageQueueTests
    {
        static WebMessage Text(int clientId, string text)
        {
            return new WebMessage { Kind = WebMessageKind.Text, ClientId = clientId, Text = text };
        }

        [Test]
        public void MessagesComeBack_InTheOrderTheyArrived()
        {
            var queue = new WebMessageQueue();
            queue.Enqueue(Text(1, "first"));
            queue.Enqueue(Text(2, "second"));

            WebMessage message;
            Assert.IsTrue(queue.TryDequeue(out message));
            Assert.AreEqual("first", message.Text);
            Assert.AreEqual(1, message.ClientId);

            Assert.IsTrue(queue.TryDequeue(out message));
            Assert.AreEqual("second", message.Text);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void AnEmptyQueue_DequeuesNothing()
        {
            WebMessage message;
            Assert.IsFalse(new WebMessageQueue().TryDequeue(out message));
        }

        [Test]
        public void TextIsDropped_OnceTheBacklogIsFull()
        {
            var queue = new WebMessageQueue();
            for (var i = 0; i < WebMessageQueue.MaxPending; i++)
                Assert.IsTrue(queue.Enqueue(Text(1, "value")));

            Assert.IsFalse(queue.Enqueue(Text(1, "one too many")));
            Assert.AreEqual(WebMessageQueue.MaxPending, queue.Count);
        }

        //A dropped Connected would leave that browser without a schema, and a dropped Disconnected
        //would leak the client - so neither is subject to the cap.
        [Test]
        public void ConnectionNotices_AreNeverDropped()
        {
            var queue = new WebMessageQueue();
            for (var i = 0; i < WebMessageQueue.MaxPending; i++)
                queue.Enqueue(Text(1, "value"));

            Assert.IsTrue(queue.Enqueue(new WebMessage { Kind = WebMessageKind.Connected, ClientId = 2 }));
            Assert.IsTrue(queue.Enqueue(new WebMessage { Kind = WebMessageKind.Disconnected, ClientId = 2 }));
            Assert.AreEqual(WebMessageQueue.MaxPending + 2, queue.Count);
        }

        [Test]
        public void Clearing_EmptiesTheQueue()
        {
            var queue = new WebMessageQueue();
            queue.Enqueue(Text(1, "value"));
            queue.Clear();

            Assert.AreEqual(0, queue.Count);
        }

        //The queue is the one place two threads meet, so it is worth proving nothing is lost there.
        [Test]
        public void ConcurrentWriters_LoseNothing()
        {
            var queue = new WebMessageQueue();
            var threads = new Thread[4];

            for (var t = 0; t < threads.Length; t++)
            {
                threads[t] = new Thread(() =>
                {
                    for (var i = 0; i < 100; i++)
                        queue.Enqueue(Text(1, "value"));
                });
                threads[t].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            Assert.AreEqual(400, queue.Count);
        }
    }
}
