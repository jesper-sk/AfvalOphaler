//#define FINAL
#define TEST

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
#if TEST
            /*RouteVisualizer vis = new RouteVisualizer(Parser.ParseOrderCoordinates(ordersDir));
            Application.DoEvents();
            vis.WindowState = FormWindowState.Maximized;
            Application.DoEvents();
            Console.ReadKey();*/

            Console.WriteLine("Parsing dist.txt");
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out double[,] t);
            GD.JourneyTime = t;
            Console.WriteLine("Parsing order.txt");
            List<Order> orders = Parser.ParseOrdersArr(ordersDir);

            // Solving:
            int threads = 100;
            Schedule[] startStates = new Schedule[threads];
            for (int i = 0; i < threads; i -= -1) startStates[i] = new Schedule(orders);
            Solver solver = new Solver(startStates, threads);
            solver.StartSolving(2000, 5, 500);
            Schedule bestSchedule = solver.GetBestSchedule();
            Console.WriteLine("Solving done, score of best schedule: " + bestSchedule.CalculateScore());
            string bestcheckstring = bestSchedule.ToCheckString();
            File.WriteAllText(@".\result.txt", bestcheckstring);
            //Console.WriteLine(bestcheckstring);
            Console.WriteLine("done");
            Console.ReadKey();

#else
            Console.WriteLine("Parsing dist.txt");
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out int[,] t);
            Console.WriteLine("Parsing order.txt");
            BigLL l = Parser.ParseOrders(ordersDir, t);

            Node curr = l.HeadX;
            for (int i = 0; i < 9; i++)
            {
                Console.WriteLine(curr.Order);
                curr = curr.SeqX.Prev;
            }
            Console.WriteLine("");
            curr = l.HeadY;
            for (int i = 0; i < 9; i++)
            {
                Console.WriteLine(curr.Order);
                curr = curr.SeqY.Prev;
            }
            Console.WriteLine("");
            curr = l.HeadTime;
            for (int i = 0; i < 9; i++)
            {
                Console.WriteLine(curr.Order);
                curr = curr.SeqDist.Prev;
            }
            Console.WriteLine("");
            curr = l.FootScore;
            for (int i = 0; i < 9; i++)
            {
                Console.WriteLine(curr.Order);
                curr = curr.SeqScore.Next;
            }
            Console.WriteLine("");
            while (true)
            {
                string[] inp = Console.ReadLine().Split();
                if (inp[0] == "t")
                {
                    Console.WriteLine($"{t[int.Parse(inp[1]), int.Parse(inp[2])]}");
                }
                else if (inp[0] == "d")
                {
                    Console.WriteLine($"{d[int.Parse(inp[1]), int.Parse(inp[2])]}");
                }
            }
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
            YCoord = 513026712
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
}

