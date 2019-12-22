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
    public static partial class GD
    {
        public const bool ShowStatus = true;
        public const int ConsoleUpdateInterval = 1000;

        public const int ResearchLimit = 10;    //The amount of times a neighbor operation should retry to search a possible candidate if previous failed

        public const int ThreadCount = 10;
        public const int OperationCount = 10;
        public const int MaxIterations = 500000;
        public const int MaxNoChange = 75000;
    }
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
            results = solver.StartSolving(GD.ThreadCount, GD.OperationCount, GD.MaxIterations, GD.MaxNoChange);
            //Task awaitAndPrintResults = Task.Factory.StartNew(() => 
            //Task.WaitAll(new Task[] { awaitAndPrintResults });
            AwaitAndPrintResults();

#elif CUSTOM
            Console.WriteLine($"Order {75}: id{orders[75]}");
            Console.WriteLine($"Order {23}: id{orders[23]}");
            Console.WriteLine($"Order {345}: {orders[345]}");
            Console.WriteLine($"Order {133}: id{orders[133]}");

            Schedule s = new Schedule(orders);

            DayRoute route1 = new DayRoute(0, 0);
            Node first = route1.AddOrder(orders[75], route1.dumps[0]);
            route1.AddOrder(orders[23], first);
            route1.RemoveNode(route1.dumps[1]);

            Console.WriteLine("---");
            // Route 1: 0 - 75 - 23 - 0
            Console.WriteLine($"t 287 -> {orders[75].MatrixId}: {GD.JourneyTime[287, orders[75].MatrixId]}");
            Console.WriteLine($"t {orders[75].MatrixId} -> {orders[23].MatrixId}: {GD.JourneyTime[orders[75].MatrixId, orders[23].MatrixId]}");
            Console.WriteLine($"t {orders[23].MatrixId} -> 287: {GD.JourneyTime[orders[23].MatrixId, 287]}");

            DayRoute route2 = new DayRoute(0, 1);
            Node second = route2.AddOrder(orders[345], route2.dumps[0]);
            route2.AddOrder(orders[133], second);
            route2.RemoveNode(route2.dumps[1]);

            Console.WriteLine("---");
            // Route 2: 0 - 345 - 133 - 0
            Console.WriteLine($"t 287 -> {orders[345].MatrixId}: {GD.JourneyTime[287, orders[345].MatrixId]}");
            Console.WriteLine($"t {orders[345].MatrixId} -> {orders[133].MatrixId}: {GD.JourneyTime[orders[345].MatrixId, orders[133].MatrixId]}");
            Console.WriteLine($"t {orders[133].MatrixId} -> 287: {GD.JourneyTime[orders[133].MatrixId, 287]}");

            s.DayRoutes[0][0] = route1;
            s.DayRoutes[1][0] = route2;

            Console.WriteLine("---");
            // New:
            // Route 1: 0 - 345 - 23 - 0
            // Route 2: 0 - 75 - 133 - 0
            Console.WriteLine($"t {orders[345].MatrixId} -> {orders[23].MatrixId}: {GD.JourneyTime[orders[345].MatrixId, orders[23].MatrixId]}");
            Console.WriteLine($"t {orders[75].MatrixId} -> {orders[133].MatrixId}: {GD.JourneyTime[orders[75].MatrixId, orders[133].MatrixId]}");

            Console.WriteLine("---");
            Console.WriteLine(route1.ToString());
            Console.WriteLine(route2.ToString());

            PrintResult(s.ToResult(), fileName: "beforeTest");

            double d1Erbij = 0
                + GD.JourneyTime[first.Prev.Data.MatrixId, first.Data.MatrixId]
                + GD.JourneyTime[first.Data.MatrixId, first.Next.Data.MatrixId]
                + first.Data.TimeToEmpty
                ;
            double d1Eraf = 0
                + GD.JourneyTime[first.Prev.Data.MatrixId, second.Data.MatrixId]
                + GD.JourneyTime[second.Data.MatrixId, first.Next.Data.MatrixId]
                + second.Data.TimeToEmpty
                ;
            double dt1 = 0
                - d1Erbij
                + d1Eraf
                ;
            Console.WriteLine("---");
            Console.WriteLine($"d1Erbij: {d1Erbij}\n" +
                $"d1Eraf: {d1Eraf}\n" +
                $"dt1: {dt1}");
            //double dt1 = 0
            //    + GD.JourneyTime[first.Prev.Data.MatrixId, first.Data.MatrixId]
            //    + GD.JourneyTime[first.Data.MatrixId, first.Next.Data.MatrixId]
            //    + first.Data.TimeToEmpty
            //    - GD.JourneyTime[first.Prev.Data.MatrixId, second.Data.MatrixId]
            //    - GD.JourneyTime[second.Data.MatrixId, first.Next.Data.MatrixId]
            //    - second.Data.TimeToEmpty
            //    ;
            double d2Erbij = 0
                + GD.JourneyTime[second.Prev.Data.MatrixId, second.Data.MatrixId]
                + GD.JourneyTime[second.Data.MatrixId, second.Next.Data.MatrixId]
                + second.Data.TimeToEmpty
                ;
            double d2Eraf = 0
                + GD.JourneyTime[second.Prev.Data.MatrixId, first.Data.MatrixId]
                + GD.JourneyTime[first.Data.MatrixId, second.Next.Data.MatrixId]
                + first.Data.TimeToEmpty
                ;
            double dt2 = 0
                - d2Erbij
                + d2Eraf
                ;
            Console.WriteLine("---");
            Console.WriteLine($"d2Erbij: {d2Erbij}\n" +
                $"d2Eraf: {d2Eraf}\n" +
                $"d21: {dt2}");
            //double dt2 = 0
            //    + GD.JourneyTime[second.Prev.Data.MatrixId, second.Data.MatrixId]
            //    + GD.JourneyTime[second.Data.MatrixId, second.Next.Data.MatrixId]
            //    + second.Data.TimeToEmpty
            //    - GD.JourneyTime[second.Prev.Data.MatrixId, first.Data.MatrixId]
            //    - GD.JourneyTime[first.Data.MatrixId, second.Next.Data.MatrixId]
            //    - first.Data.TimeToEmpty
            //    ;

            route1.Swap1(second, first, new StringBuilder(), dt1);
            route2.Swap2(first, second, new StringBuilder(), dt2);

            Console.WriteLine("---");
            Console.WriteLine(route1.ToString());
            Console.WriteLine(route2.ToString());

            Console.WriteLine("---");
            PrintResult(s.ToResult(), fileName:"test");

#endif
#endif
            ConsoleKeyInfo key;
            do key = Console.ReadKey(); while (key.Key == ConsoleKey.RightWindows || key.Key == ConsoleKey.LeftWindows);
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

