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
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out int[,] t);
            Console.WriteLine("Parsing order.txt");
            BigLL l = Parser.ParseOrders(ordersDir, t);

            // Solving:
            Schedule startState = new Schedule();
            Solver solver = new Solver(startState);
            solver.StartSolving();
            Schedule bestSchedule = solver.GetBestSchedule();
            Console.WriteLine(bestSchedule);

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

    public static class Dump
    {
        public const int XCoord = 56343016;
        public const int YCoord = 513026712;
        public const int MatrixId = 287;
        public const int OrderId = 0;
    }
}

