using DispatchedThreading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Examples
{
    // Data transfer object
    public class CheckerBreakpointContext
    {
        public Account account;
        public MasterToken token;
        public Stopwatch watch;

        public CheckerBreakpointContext(
            Account account,
            MasterToken token,
            Stopwatch watch)
        {
            this.account = account;
            this.token = token;
            this.watch = watch;
        }
    }

    public class Checker
    {
        private TimeSpan timeout;

        public Checker(TimeSpan timeout)
        {
            this.timeout = timeout;
        }

        public void HandleBreakpointIfNeeded(CheckerBreakpointContext context)
        {
            context.watch.Stop();

            try
            {
                if (context.watch.Elapsed > timeout)
                    context.token.Cancel();
                context.token.ThrowOrWaitIfRequested();
            }
            catch
            {
                // If timeout exceeded, then account is invalid
                context.account.State = AccountState.Invalid;
                throw;
            }

            context.watch.Start();
        }

        public void ProcessAccount(Account account)
        {
            Stopwatch watch = Stopwatch.StartNew();
            CheckerBreakpointContext context = new(account, account.Token, watch);

            // Pretend calculating for 2 * 500 / 1000 = 1 second
            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(500);
                // Handle token state each 0.5 seconds
                HandleBreakpointIfNeeded(context);
            }

            // 50% valid, 50% invalid
            account.State = new Random().Next(2) == 0
                ? AccountState.Valid : AccountState.Invalid;

            return;
        }
    }
}
