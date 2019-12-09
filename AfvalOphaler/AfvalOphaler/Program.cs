#region Definings
//#define FINAL         // Whether to run the final build
//#define CLUSTER       // Whether to cluster all orders
//#define VISUALIZER    // Whether to show the visualizer
//#define TEST          // Whether to use Solver and Schedule
//#define NTEST         // Whether to use NSolver and NSchedule
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
using NAfvalOphaler;
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
            int maxIterations = 100000;
            int maxNoChange = 10000;

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
            NAfvalOphaler.Solver solver = new NAfvalOphaler.Solver(orders);
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
        private async static void AwaitAndPrintResults(NAfvalOphaler.Solver solver, Task<ScheduleResult[]> results)
        {
            //Task userInterruptAwaiter = Task.Factory.StartNew(() => AwaitUserInterrupt(solver));
            await results;
            //if (!userInterruptAwaiter.IsCompleted) Console.WriteLine("close");
            //await userInterruptAwaiter;

            ScheduleResult res = results.Result[0];
            Console.WriteLine("===============" +
                            "\n= BEST RESULT =" +
                            "\n===============");
            Console.WriteLine(res.Stats);

            File.WriteAllText(@".\result.txt", res.Check.ToString());
        }
        private static void AwaitUserInterrupt(NAfvalOphaler.Solver solver)
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
        #endregion
    }

    #region Global Data
    public static class GD
    {
        public static double[,] JourneyTime;

        public static Order Dump = new Order()
        {
            OrderId = 0,
            Name = "Dump",
            MatrixId = 287,
            XCoord = 56343016,
            YCoord = 513026712,
            TimeToEmpty = 30
        };
        //public static BigLLNode DumpLLing = new BigLLNode(Dump);

        #region Allowed Combinations for Order Scheduling
        // [Frequentie, aantal_combinaties, allowed_days_in_combi]
        public static readonly int[][][] AllowedDayCombinations =
        {
            new int[][] { new int[] {} },
            new int[][]
            {
                new int[] {0},
                new int[] {1},
                new int[] {2},
                new int[] {3},
                new int[] {4}
            },
            new int[][]
            {
                new int[] {0, 3},
                new int[] {1, 4}
            },
            new int[][]
            {
                new int[] {0, 2, 4}
            },
            new int[][]
            {
                new int[] {1,2,3,4}, //ma niet
                new int[] {0,2,3,4}, //di niet
                new int[] {0,1,3,4}, //wo niet
                new int[] {0,1,2,4}, //do niet
                new int[] {0,1,2,3}  //vr niet
            },
            new int[][]
            {
                new int[] {0,1,2,3,4}
            }
        };
        #endregion
    }
    #endregion

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

