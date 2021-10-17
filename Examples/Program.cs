using DispatchedThreading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Examples
{
    public class Program
    {
        private static readonly int INITIAL_THREAD_COUNT = 2;
        private static readonly int TIMEOUT_IN_SECONDS = 5;
        private static readonly string INPUT =
            @"pitchayanant_2541@hotmail.com:otto2541
            stxparker@gmail.com:Harley75
            racoonkid008@hotmail.com:Racoonkid007
            postmodx@me.com:modern1X
            rallysim123@gmail.com:kennyp07
            pri3st13@gmail.com:trunks13
            stusut@hotmail.com:Camasule1985
            summerhillmatt@yahoo.com:JTKirk1966
            pointerj13@yahoo.com:mano0126
            PPAP@eircom.net:20rosanna
            stroppycarpet42@gmail.com:Flagman375
            pitchayanant_2541@hotmail.com:otto2541
            stxparker@gmail.com:Harley75
            racoonkid008@hotmail.com:Racoonkid007
            postmodx@me.com:modern1X
            rallysim123@gmail.com:kennyp07
            pri3st13@gmail.com:trunks13
            stusut@hotmail.com:Camasule1985
            summerhillmatt@yahoo.com:JTKirk1966
            pointerj13@yahoo.com:mano0126
            PPAP@eircom.net:20rosanna
            stroppycarpet42@gmail.com:Flagman375";

        private static ThreadDistributor<Account> distributor;
        private static ObservableCollection<Account> accounts;
        private static Checker checker;
        private static MasterTokenSource pipelineTokenSource;

        public static void Main()
        {
            // Initialize members
            accounts = new ObservableCollection<Account>();
            checker = new Checker(TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS));
            pipelineTokenSource = new MasterTokenSource();

            // Parse input to models
            var pairs = INPUT.Split('\n').Select(x => x.Trim().Split(':'));
            foreach (var credentials in pairs)
            {
                accounts.Add(new(credentials[0], credentials[1]));
                Console.WriteLine($"Account loaded:\t{string.Join(':', credentials)}");
            }

            // Initialize thread distributor
            distributor = new ThreadDistributor<Account>(
                INITIAL_THREAD_COUNT,   // Initial thread count
                accounts,               // Payloads
                ReserveAccount,         // Payload-picker predicate
                checker.ProcessAccount, // Payload handler
                pipelineTokenSource.MakeToken()
                );

            Console.WriteLine($"\nThread count = {distributor.ThreadCount}\n");

            distributor.TaskCompleted += (sender, account) =>
                Console.WriteLine($"Task completed:\t{account}");

            // Run the pipeline
            distributor.Start();

            Thread.Sleep(3100); // After first 6 payloads have been processed
            distributor.ThreadCount = 1;
            Console.WriteLine($"\nThread count = {distributor.ThreadCount}\n");

            Thread.Sleep(5100);
            pipelineTokenSource.Pause();
            Console.WriteLine("\nDrinking coffee for 5 seconds...");

            Thread.Sleep(5000);
            pipelineTokenSource.Continue(); // Getting down to business

            distributor.ThreadCount = 100;
            Console.WriteLine($"Thread count = {distributor.ThreadCount}\n");

            Thread.Sleep(2000);
            Console.WriteLine("\nDistributor is still running...");

            Thread.Sleep(2000);
            Console.WriteLine("But we are going to add another account...\n");

            Thread.Sleep(2000);
            accounts.Add(new("another@accou.nt", "pa$$w0rd"));

            distributor.TaskCompleted += (sender, payload) =>
                Console.WriteLine("\nPipeline finished!");

            // Just wait for the threads to finish their work
            Thread.Sleep(Timeout.Infinite);
        }

        // Returns whether the given account was reserved or not
        private static bool ReserveAccount(Account account)
        {
            // Only pick unreserved payloads
            if (account.State == AccountState.Unchecked)
            {
                // Change payload's state to reserve a payload
                // and protect it from getting picked twice
                account.State = AccountState.Reserved;

                account.Token = pipelineTokenSource.MakeToken();
                account.Token.Canceled += (sender) =>
                    Console.WriteLine($"Task canceled:\t{account}");

                return true;
            }
            return false;
        }
    }
}
