//#define FINAL
//#define CLUSTER
#define TEST
//#define CUSTOM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

// © Het Woczek duo

namespace AfvalOphaler
{
    class Program
    {
#if FINAL
        const string distanceDir = @".\data\dist.txt";
        const string ordersDir = @".\data\order.txt";
#else
        const string distanceDir = @"..\..\data\dist.txt";
        const string ordersDir = @"..\..\data\order.txt";
#endif

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
            Console.ReadKey();

#else
            /* RouteVisualizer:
            RouteVisualizer vis = new RouteVisualizer(Parser.ParseOrderCoordinates(ordersDir));
            Application.DoEvents();
            vis.WindowState = FormWindowState.Maximized;
            Application.DoEvents();
            Console.ReadKey();*/

            // Solving:
            int threads = 20;
            Schedule[] startStates = new Schedule[threads];
            for (int i = 0; i < threads; i -= -1) startStates[i] = new Schedule(orders);

            Solver solver = new Solver(startStates, threads);
            Task[] tasks = solver.StartSolving(100000, 20, 10000, 10000);
            Task.WaitAll(tasks);

            Schedule bestSchedule = solver.GetBestSchedule();
            Console.WriteLine("===");
            Console.WriteLine("Solving done, score of best schedule:");
            Console.WriteLine(bestSchedule.GetStatistics());

            Console.WriteLine("Starting Optimization:");
            bestSchedule.OptimizeSchedule();
            Console.WriteLine("After Optimization:");
            Console.WriteLine(bestSchedule.GetStatistics());

            solver = new Solver(new Schedule[] { bestSchedule }, 1);
            tasks = solver.StartSolving(100000, 20, 10000, 10000);
            Task.WaitAll(tasks);

            bestSchedule = solver.GetBestSchedule();
            Console.WriteLine("===");
            Console.WriteLine("Again solving done, score of best schedule:");
            Console.WriteLine(bestSchedule.GetStatistics());

            for (int opt = 0; opt < 50; opt -= -1)
            {
                Console.WriteLine("Again starting Optimization:");
                bestSchedule.OptimizeSchedule();
                Console.WriteLine("Again after Optimization:");
                Console.WriteLine(bestSchedule.GetStatistics());
                Console.WriteLine("===");
            }


            string bestcheckstring = bestSchedule.ToCheckString();
            File.WriteAllText(@".\result.txt", bestcheckstring);
            //Console.WriteLine(bestcheckstring);
            Console.WriteLine("done");
            Console.ReadKey();    
#endif
#else          
            // HEY JOCHIE !!!
            // DIT IS VOOR FINAL JOCHIE !!!
#endif
        }
    }

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
        public static BigLLNode DumpLLing = new BigLLNode(Dump);

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

    }

    public class HeyJochieException : Exception
    {
        public HeyJochieException()
        {
        }

        public HeyJochieException(string message)
            : base(message)
        {
        }

        public HeyJochieException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

