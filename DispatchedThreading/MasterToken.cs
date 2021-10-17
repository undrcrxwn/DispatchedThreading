﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DispatchedThreading
{
    public class MasterToken
    {
        public bool IsCancellationRequested { get; private set; }
        public bool IsPauseRequested { get; private set; }

        public void Cancel() => IsCancellationRequested = true;
        public void Pause() => IsPauseRequested = true;
        public void Continue() => IsPauseRequested = false;

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                throw new OperationCanceledException();
        }

        public void WaitIfPauseRequested()
        {
            while (IsPauseRequested)
            {
                Thread.Sleep(50);
                ThrowIfCancellationRequested();
            }
        }

        public void ThrowOrWaitIfRequested()
        {
            ThrowIfCancellationRequested();
            WaitIfPauseRequested();
        }
    }

    public class MasterTokenSource
    {
        public List<MasterToken> Tokens = new List<MasterToken>();

        public MasterToken MakeToken()
        {
            var token = new MasterToken();
            Tokens.Add(token);
            return token;
        }

        public void Cancel() => Tokens.ToList().ForEach(x => x.Cancel());
        public void Pause() => Tokens.ToList().ForEach(x => x.Pause());
        public void Continue() => Tokens.ToList().ForEach(x => x.Continue());
    }
}
