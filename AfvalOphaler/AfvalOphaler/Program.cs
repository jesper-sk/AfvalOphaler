//#define FINAL         // Whether to run the final build. If uncommented, program will look for the dist.txt and order.txt in the same directory as
                        // the executable. If commented, program will look for them in the data folder in our solution
//#define CLUSTER       // Whether to cluster all orders. As we currently don't use clustering, it is commented by default.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Threading;

// © Het Woczek duo

namespace AfvalOphaler
{
    public static partial class GD
    {
        public const bool ShowStatus = true;                // Whether to show the status in the console. For debuggin purposes
        public const int ConsoleUpdateInterval = 1000;      // Delay of the statusUpdater

        public const int ResearchLimit = 20;                //The amount of times a neighbor operation should retry to search a possible candidate if previous failed

        public const int ThreadCount = 10;                  // The amount of solver instances that should run on parallel
        public const int OperationCount = 20;               // How many neighbors per iteration should be generated     

        public const int MaxIterations_AddPhase = 30000;    // Maximum number of iterations per solver for the All phase
        public const int MaxNoChange_AddPhase = 3000;       // Maximum number of iterations without state change per solver for the All phase

        public const int MaxIterations_AllPhase = 500000;   // Maximum number of iterations per solver for the All phase
        public const int MaxNoChange_AllPhase = 75000;      // Maximum number of iterations without state change per solver for the All phase
    }
    class Program
    {
        #region Input FileDirectory Declarations
#if FINAL
        const string distanceDir = @".\data\dist.txt";
        const string ordersDir = @".\data\order.txt";
#else
        const string distanceDir = @"..\..\data\dist.txt";
        const string ordersDir = @"..\..\data\order.txt";
#endif
        #endregion

        #region Main
        /// <summary>
        /// Entry point of the program.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Parsing times and distances:
            Console.WriteLine("Parsing dist.txt");
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out double[,] t);
            GD.JourneyTime = t;
            Console.WriteLine("Parsing order.txt");
            List<Order> orders = Parser.ParseOrdersArr(ordersDir);

#if CLUSTER
            // Clustering:
            bool clusterorders = true;
            if (clusterorders)
            {
                Console.WriteLine("Clustering...");
                int clustercount = 4;
                Parser.KMeansClusterOrders(orders, clustercount, 1000);
            }
#endif

            solver = new Solver(orders);
            results = solver.StartSolving();
            AwaitAndPrintResults();

            Console.WriteLine("\nPress any key to exit");
            ConsoleKeyInfo key;
            do key = Console.ReadKey(); while (key.Key == ConsoleKey.RightWindows || key.Key == ConsoleKey.LeftWindows);
        }
        #endregion

        #region Await Solver/User Then Print Results
        static Solver solver;
        static Task<ScheduleResult> results;
        /// <summary>
        /// Starts the solver.
        /// Awaits until either:
        /// - All solver instances are finished.
        /// - Userinterrupt.
        /// Then prints the best result.
        /// </summary>
        private static void AwaitAndPrintResults()
        {
            Task userInterruptAwaiter = Task.Factory.StartNew(() => AwaitUserInterrupt(solver));
            results.Wait();
            if (!userInterruptAwaiter.IsCompleted) solverStillGoing = false;
            userInterruptAwaiter.Wait();
            ScheduleResult res = results.Result;
            Console.Beep();
            PrintResult(res);             
        }

        private static bool solverStillGoing = true;
        /// <summary>
        /// Awaits userinterrupt (keypress of either the X or Escape key).
        /// If the user pressed a key the solver is stopped.
        /// Is stopped itself if all solvers are done.
        /// </summary>
        /// <param name="solver"></param>
        private static void AwaitUserInterrupt(Solver solver)
        {
            while (solverStillGoing)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo userInput = Console.ReadKey(true);
                    if (userInput.Key == ConsoleKey.X || userInput.Key == ConsoleKey.Escape)
                    {
                        Console.Clear();
                        Console.WriteLine("Stopping all solver tasks...");
                        solver.UserInterrupt = true;
                        return;
                    }
                }
                else Thread.Sleep(50);
            }
        }
        /// <summary>
        /// Prints the best solver result.
        /// </summary>
        /// <param name="best">The best schedule result</param>
        /// <param name="writeToFile">if true saves the best result as string ready to put in the checker</param>
        /// <param name="fileName">filename of the file in which the best result is saved, ignored if writeToFile=false</param>
        private static void PrintResult(ScheduleResult best, bool writeToFile = true, string fileName = "result")
        {
            Console.Clear();
            Console.WriteLine("===============" +
                            "\n= BEST RESULT =" +
                            "\n===============");
            Console.WriteLine(best.Stats);
            if (writeToFile) File.WriteAllText($@".\{fileName}.txt", best.Check.ToString());
            Console.WriteLine("===============");
        }
        #endregion
    }

#region Custom Exception [Can be ignored]
    [Serializable]
    public class HeyJochieException : Exception
    {
        public HeyJochieException() : base() { }
        public HeyJochieException(string message) : base(message) { }
        public HeyJochieException(string message, Exception inner) : base(message, inner) { }
        protected HeyJochieException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
#endregion

}

