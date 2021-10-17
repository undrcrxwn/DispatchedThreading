using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DispatchedThreading
{
    public class ThreadDistributor<TPayload>
    {
        public event EventHandler<TPayload> TaskCompleted;

        private MasterToken token;
        private ObservableCollection<TPayload> payloads;
        private Func<TPayload, bool> predicate;
        private Action<TPayload> handler;
        private int superfluousDistributonsCount;
        private object abortionLocker = new object();
        private object payloadsLocker = new object();

        private int _ThreadCount;
        public int ThreadCount
        {
            get => _ThreadCount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException();

                int delta = value - _ThreadCount;
                _ThreadCount = value;

                for (var i = 0; i < delta; i++)
                    Distribute();

                if (delta < 0)
                    superfluousDistributonsCount -= delta;
            }
        }

        public ThreadDistributor(
            int threadCount,
            ObservableCollection<TPayload> payloads,
            Func<TPayload, bool> predicate,
            Action<TPayload> handler,
            MasterToken token = null)
        {
            this.token = token ?? new MasterToken();
            this.token.Pause();

            ThreadCount = threadCount;
            this.payloads = payloads;
            this.predicate = predicate;
            this.handler = handler;

            TaskCompleted += (sender, e) => Distribute();
        }

        public void Start() => token.Continue();

        public void Stop() => token.Pause();

        public void Distribute()
        {
            // New distribution
            Task.Factory.StartNew(() =>
            {
                token.ThrowOrWaitIfRequested();
                lock (abortionLocker)
                {
                    if (superfluousDistributonsCount > 0)
                    {
                        superfluousDistributonsCount--;
                        return;
                    }
                }

                token.ThrowOrWaitIfRequested();
                TPayload payload;
                try
                {
                    lock (payloadsLocker)
                        payload = payloads.First(predicate);
                }
                catch
                {
                    // New subscription
                    payloads.CollectionChanged += OnCollectionChanged;
                    return;
                }

                token.ThrowOrWaitIfRequested();
                try { handler(payload); }
                catch { return; }
                TaskCompleted?.Invoke(this, payload);

                Thread.CurrentThread.Abort();
            },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Subscribtion acquired
                payloads.CollectionChanged -= OnCollectionChanged;
                Distribute();
            }
        }
    }
}
