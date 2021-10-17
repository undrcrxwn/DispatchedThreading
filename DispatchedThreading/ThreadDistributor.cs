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
        public MasterToken SelfToken;
        public MasterTokenSource InternalTokenSource = new MasterTokenSource();
        public event EventHandler<TPayload> OnTaskCompleted;

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

                // if delta is positive
                for (var i = 0; i < delta; i++)
                    Distribute();

                // if delta is negative
                if (delta < 0)
                    superfluousDistributonsCount -= delta;
            }
        }

        public ThreadDistributor(
            int threadCount,
            ObservableCollection<TPayload> payloads,
            Func<TPayload, bool> predicate,
            Action<TPayload> handler,
            MasterToken token)
        {
            SelfToken = token;
            token.Pause();
            this.payloads = payloads;
            this.predicate = predicate;
            this.handler = handler;
            OnTaskCompleted += (sender, e) => Distribute();
            ThreadCount = threadCount;
        }

        public ThreadDistributor(
            int threadCount,
            ObservableCollection<TPayload> payloads,
            Func<TPayload, bool> predicate,
            Action<TPayload> handler)
        : this(threadCount, payloads, predicate, handler, new MasterToken()) { }

        public void Distribute()
        {
            // New distribution
            Task.Factory.StartNew(() =>
            {
                SelfToken.ThrowOrWaitIfRequested();
                lock (abortionLocker)
                {
                    if (superfluousDistributonsCount > 0)
                    {
                        superfluousDistributonsCount--;
                        return;
                    }
                }

                SelfToken.ThrowOrWaitIfRequested();
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

                SelfToken.ThrowOrWaitIfRequested();
                try { handler(payload); }
                catch { return; }
                OnTaskCompleted?.Invoke(this, payload);

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
