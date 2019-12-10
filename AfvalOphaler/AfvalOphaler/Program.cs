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

            int threads = 10;
            int operationCount = 10;
            int maxIterations = 30000;
            int maxNoChange = 3000;

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
            Solver solver = new Solver(orders);
            var results = solver.StartSolving(threads, operationCount, maxIterations, maxNoChange);                  
            Task awaitAndPrintResults = Task.Factory.StartNew(() => AwaitAndPrintResults(solver, results));
            Task.WaitAll(new Task[] { awaitAndPrintResults });

#elif CUSTOM
#endif
#endif
            Console.ReadKey();
        }
        #endregion

        #region Await Solver/User Then Print Results
        private async static void AwaitAndPrintResults(Solver solver, Task<ScheduleResult> results)
        {
            //Task userInterruptAwaiter = Task.Factory.StartNew(() => AwaitUserInterrupt(solver));
            await results;
            //if (!userInterruptAwaiter.IsCompleted) Console.WriteLine("close");
            //await userInterruptAwaiter;
            ScheduleResult res = results.Result;
            PrintResult(res);           
        }
        private static void AwaitUserInterrupt(Solver solver)
        {
            while (true)
            {
                string userInput = Console.ReadLine();
                if (userInput == "close")
                {
                    solver.UserInterrupt = true;
                    return;
                }
            }
        }
        private static void PrintResult(ScheduleResult res, bool writeToFile = true, string fileName = "result")
        {
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

