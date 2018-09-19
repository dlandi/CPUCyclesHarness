using System;
using System.Threading.Tasks;
using System.Threading;
using SmartContracts;

namespace TestCyclesAndAsync
{

    class Program
    {
        private class MySmartContract : SmartContract
        {
            public override bool DoShortLoop(long numLoops)
            {
                Console.WriteLine("Executing ShortLoop");
                for (int i = 0; i < numLoops * 1000000; i++) { }
                return true;
            }
            public override bool DoComplexLoop(long numLoops)
            {
                Console.WriteLine("Executing ComplexLoop");
                for (int i = 0; i < numLoops * 10000000; i++) { }

                return true;
            }
            /// <summary>
            /// All task will be cancelled if/when this method completes loop.
            /// The task maybe cancelled by from an outside task via the TokenSource
            /// </summary>
            /// <param name="tokenSource"></param>
            /// <returns></returns>
            public async Task<bool> DurationLimiter(CancellationTokenSource tokenSource, int durationLimit)
            {                
                await Task.Run(() =>
                {
                    while (0 < 1)//endless
                    {
                        if (tokenSource.Token.IsCancellationRequested)
                        {
                            // clean up before exiting
                            //tokenSource.Token.ThrowIfCancellationRequested();
                            Print(ConsoleColor.Cyan, "Task Cancelled... Exiting Outer Loop...");
                            break;
                        }
                        for (int i = 0; i < durationLimit; i++)
                        {
                            if (tokenSource.Token.IsCancellationRequested)
                            {
                                // clean up before exiting
                                //tokenSource.Token.ThrowIfCancellationRequested();
                                Print(ConsoleColor.Cyan, "Task Cancelled... Exiting Inner Loop...");
                                break;
                            }
                        }
                    }
                }, tokenSource.Token);
                Print(ConsoleColor.Cyan, "DoTask() Completed...");
                Print(ConsoleColor.Cyan, "-----------------------------");
                return true;
            }

            public void InitializeDelegates()
            {
                delegates.Add(Tuple.Create(new DelegateMethod(DoComplexLoop), "NumLoops", (long)100, "GasLimit", (long)10000000000));

                delegates.Add(Tuple.Create(new DelegateMethod(DoShortLoop), "NumLoops", (long)10, "GasLimit", (long)20000000000));

                delegates.Add(Tuple.Create(new DelegateMethod(DoComplexLoop), "NumLoops", (long)100, "GasLimit", (long)100000000));

                delegates.Add(Tuple.Create(new DelegateMethod(DoShortLoop), "NumLoops", (long)10, "GasLimit", (long)20000000));

                delegates.Add(Tuple.Create(new DelegateMethod(DoShortLoop), "NumLoops", (long)10, "GasLimit", (long)20000000));

                delegates.Add(Tuple.Create(new DelegateMethod(DoComplexLoop), "NumLoops", (long)100, "GasLimit", (long)100000000));
            }
        }


        static void Main(string[] args)
        {
            MySmartContract contract =
                new MySmartContract()
                {
                    ContractId = new Guid().ToString(),
                    ContractName = "TestContract"
                };

            Console.WriteLine(string.Format(" ContractAddr: {0};\n ContractId: {1};\n ContractName: {2}",
                                    contract.ContractAddress, contract.ContractId, contract.ContractName));
            Console.WriteLine("-----------------------------");

            contract.InitializeDelegates();

            //NOTE: GAS CONTROLS DURATION OF EXECUTION IN THIS DEMO 
            // "Gas" is measured in CPU Cycles...
            int gas = 5000; //multiplied internally by 1,000,000 cycles
            //YardStick is passed into DurationLimiter, representing the maximum duration allowed until all tasks are cancelled.
            int yardStick = 1000000000; //in cycles
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var taskDelegates = Task.Run(() => contract.Execute(gas));
            taskDelegates.ContinueWith((cancelThis) => { tokenSource.Cancel(); });
            
            contract.ExecuteAndMonitor(Task.Run(async () => await contract.DurationLimiter(tokenSource, yardStick)), gas, tokenSource).Wait();
                                                         
            Console.Read();
        }
    }
}
