#region Definings
//#define FINAL         // Whether to run the final build
//#define CLUSTER       // Whether to cluster all orders
//#define VISUALIZER    // Whether to show the visualizer
//#define TEST          // Whether to use Solver and Schedule
#define NTEST         // Whether to use NSolver and NSchedule
//#define CUSTOM        // Whether to use Own-Defined small testcases
#endregion

#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Threading;
#endregion

// © Het Woczek duo

namespace AfvalOphaler
{
    class Program
    {
        #region Directory Declarations
#if FINAL
        const string distanceDir = @".\data\dist.txt";
        const string ordersDir = @".\data\order.txt";
#else
        const string distanceDir = @"..\..\data\dist.txt";
        const string ordersDir = @"..\..\data\order.txt";
#endif
        #endregion

        #region Main
        static void Main(string[] args)
        {
            // Getting times and distances:
            Console.WriteLine("Parsing dist.txt");
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out double[,] t);
            GD.JourneyTime = t;
            Console.WriteLine("Parsing order.txt");
            List<Order> orders = Parser.ParseOrdersArr(ordersDir);

            int threads = 1;
            int operationCount = 1;
            int maxIterations = 500000;
            int maxNoChange = 75000;

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
#if VISUALIZER
            RouteVisualizer:
            RouteVisualizer vis = new RouteVisualizer(Parser.ParseOrderCoordinates(ordersDir));
            Application.DoEvents();
            vis.WindowState = FormWindowState.Maximized;
            Application.DoEvents();
            Console.ReadKey();
#endif

#if FINAL
            // HEY JOCHIE !!!
            // DIT IS VOOR FINAL JOCHIE !!!
#else
#if TEST
#if CUSTOM
            orders = orders.OrderBy(o => o.Frequency).ToList();
            NAfvalOphaler.Schedule customSchedule = new NAfvalOphaler.Schedule(orders);
            int loopindex = customSchedule.AddLoop(0, 0);
            NAfvalOphaler.Node curr = customSchedule.dayRoutes[0][0].Loops[loopindex].Start;
            for (int o = 0; o < 10; o++)
            {
                curr = customSchedule.AddOrder(orders[o], curr, loopindex, 0, 0);
            }
            Console.WriteLine("Done adding...");
            File.WriteAllText(@".\beforeOpt.txt", customSchedule.ToCheckString());
            Console.WriteLine("Before opt saved...");
            Console.WriteLine("Starting optimalisation...");
            for (int opt = 0; opt < 10; opt++)
            {
                customSchedule.dayRoutes[0][0].Loops[loopindex].OptimizeLoop();
                Console.WriteLine("Duration: "+ customSchedule.CaculateDuration());
            }
            Console.WriteLine("Optimalisation done...");
            File.WriteAllText(@".\afterOpt.txt", customSchedule.ToCheckString());
            Console.WriteLine("Opt results saved...");
#else
            /* RouteVisualizer:
            RouteVisualizer vis = new RouteVisualizer(Parser.ParseOrderCoordinates(ordersDir));
            Application.DoEvents();
            vis.WindowState = FormWindowState.Maximized;
            Application.DoEvents();
            Console.ReadKey();*/
#endif
#elif NTEST
            solver = new Solver(orders);
            results = solver.StartSolving(threads, operationCount, maxIterations, maxNoChange);
            //Task awaitAndPrintResults = Task.Factory.StartNew(() => 
            //Task.WaitAll(new Task[] { awaitAndPrintResults });
            AwaitAndPrintResults();

#elif CUSTOM
#endif
#endif
            Console.ReadKey();
        }
        #endregion

        #region Await Solver/User Then Print Results
        static Solver solver;
        static Task<ScheduleResult> results;
        private static void AwaitAndPrintResults()
        {
            //Console.CancelKeyPress += Console_CancelKeyPress;
            Task userInterruptAwaiter = Task.Factory.StartNew(() => AwaitUserInterrupt(solver));
            results.Wait();
            if (!userInterruptAwaiter.IsCompleted) solverStillGoing = false;
            userInterruptAwaiter.Wait();
            ScheduleResult res = results.Result;
            Console.Beep();
            PrintResult(res);             
        }

        private static bool solverStillGoing = true;
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
        private static void PrintResult(ScheduleResult res, bool writeToFile = true, string fileName = "result")
        {
            Console.Clear();
            Console.WriteLine("===============" +
                            "\n= BEST RESULT =" +
                            "\n===============");
            Console.WriteLine(res.Stats);

            if (writeToFile) File.WriteAllText($@".\{fileName}.txt", res.Check.ToString());

            Console.WriteLine("===============");
        }
        #endregion
    }


    #region Custom Exception
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

