using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmartContracts
{
    public interface ISmartContractToken
    {
        /* Total amount of tokens */
        UInt64 TotalSuppy { get; set; }
        //function transfer(address to, uint value) public returns (bool);
        bool TransferTo(string address_to, UInt32 value);
        //function transferFrom(address from, address to, uint value) public returns (bool);
        bool TransferFrom(string address_to, UInt32 value);
        //function approve(address spender, uint value) public returns (bool);
        bool Approve(string address_spender, UInt32 value);
        //function balanceOf(address owner) public constant returns(uint);
        UInt32 BalanceOf(string address_owner);
        //function allowance(address owner, address spender) public constant returns(uint);   
        UInt32 Allowance(string address_owner, string address_spender);
    }

    public interface ISmartContractStandardToken : ISmartContractToken
    {

    }

    public interface ISmartContract
    {
        string ContractId { get; set; }
        string ContractName { get; set; }
        string ContractAddress { get; set; }
        void Execute(Int64 GasLimit);
        //void ExecuteProfile(Int64 GasLimit);
    }

    public class SmartContract : ISmartContract
    {
        internal const string pszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        public string ContractId { get; set; } = Guid.NewGuid().ToString();
        public string ContractName { get; set; } = "NewContract" + DateTime.UtcNow.ToShortDateString() + ":" + DateTime.UtcNow.ToShortTimeString();
        public string ContractAddress { get; set; } = pszBase58;

        public List<Tuple<DelegateMethod, string, long, string, long>> delegates { get; set; } = new List<Tuple<DelegateMethod, string, long, string, long>>();
        public SmartContract()
        {
            //initialize threadcycle timer
            for (int i = 0; i < 5; i++)
            {
                ThreadCycleCounter.ThreadCycles();
            }
            //delegates.Add(Tuple.Create(new DelegateMethod(DoShortLoop),"NumLoops", (long)10, "GasLimit", (long)100));

            //delegates.Add(Tuple.Create(new DelegateMethod(DoComplexLoop), "NumLoops", (long)100, "GasLimit", (long)1000));
        }

        /// <summary>
        /// A Smart Contract is an Execution Engine, 
        /// firing methods, counting the cost, incrementing/decrementing funds  and imposing a 
        /// throttle on execution against available "funds"; and, 
        /// ultimately writing data to the blockchain
        /// </summary>
        public virtual void Execute(Int64 GasLimit)
        {
            long loopcount = 0;
            foreach(Tuple<DelegateMethod, string, long, string, long> tup in delegates)
            {
                try
                {
                    loopcount++;
                    ulong start, end;
                    start = ThreadCycleCounter.ThreadCycles();
                    tup.Item1.Invoke(tup.Item3);
                    end = ThreadCycleCounter.ThreadCycles();
                    ulong cycles = end - start;

                    if((cycles  >0) && (long)cycles > tup.Item5) //Note: tup.Item5 = GasLimit for This Delegate
                    {
                        Print(ConsoleColor.Red, String.Format("Gas Limit Exceeded for Delegate#{0} - GasSpent: {1}; GasLimit: {2}", 
                                                           loopcount, cycles, tup.Item5));
                    }

                    Console.WriteLine(String.Format("Executed Delegate#{0} - GasSpent: {1}; GasLimit: {2}",
                                                           loopcount, cycles, tup.Item5));
                    Console.WriteLine("-----------------------------");

                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        public delegate bool DelegateMethod(long numLoops);

        public virtual bool DoShortLoop(long numLoops)
        {

            return true;
        }

        public virtual bool DoComplexLoop(long numLoops)
        {

            return true;
        }

        public async Task<long> DoMetricAsync(long gas, CancellationTokenSource tokenSource)
        {
            long i = 0;
            await Task.Run(() =>
            {
                for (i = 0; i < gas * 1000000; i++)
                {
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        // clean up before exiting
                        Print(ConsoleColor.Cyan, "DoMetric() Cancelled...");
                        return i;
                    }
                }
                Print(ConsoleColor.Cyan, String.Format("DoMetric() Completed ... {0} loops", i));
                return i;
            });

            return i;
        }
        
        public  async Task<bool> ExecuteAndMonitor<T>(Task<T> task, long gas, CancellationTokenSource tokenSource)
        {
            Print(ConsoleColor.Cyan, "-----------------------------");
            Print(ConsoleColor.Cyan, "Running ExecuteAndMonitor()...");
            Print(ConsoleColor.Cyan, "-----------------------------");
            Task pacer = Task.Run(async () => await DoMetricAsync(gas, tokenSource));
            Task firstToFinish =  await Task.WhenAny(pacer, task);
            if (firstToFinish == pacer)
            {
                //// The pacer finished first 
                Print(ConsoleColor.Cyan, "Pacer Completed: Gas Limit Exceeded...");
                Print(ConsoleColor.Cyan, "-----------------------------");

                tokenSource.Cancel();
                // deal with any exception                 
                await task.ContinueWith(HandleException);
                //throw new TimeoutException();//which would be caught by HandleException

            }
            Print(ConsoleColor.Cyan, "ExecuteAndMonitor Completed...");
            return true; // If we reach here, the original task already finished 
        }

        private void HandleException<T>(Task<T> task)
        {
            if (task.Exception != null)
            {
                Print(ConsoleColor.Cyan, "Operation Cancelled: Gas Limit Exceeded...");
            }
        }

        #region Helpers 
        protected void Print(ConsoleColor color, string txt)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(txt);
            Console.ResetColor();
        }
        #endregion
    }

    #region Platform Wrapper Code

    /// <summary>
    /// A wrapper around the Native OS' QueryThreadCycleTime() method
    /// </summary>
    static class ThreadCycleCounter
    {
        public static ulong ThreadCycles()
        {
            ulong cycles;
            if (!QueryThreadCycleTime(PseudoHandle, out cycles))
                throw new System.ComponentModel.Win32Exception();
            return cycles;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryThreadCycleTime(IntPtr hThread, out ulong cycles);
        private static readonly IntPtr PseudoHandle = (IntPtr)(-2);
    }


    #endregion



}

