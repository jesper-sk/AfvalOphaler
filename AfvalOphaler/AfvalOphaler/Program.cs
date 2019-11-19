using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// © Het Woczek duo

namespace AfvalOphaler
{
    class Program
    {
        const string distanceDir = @"..\..\data\dist.txt";
        const string ordersDir = @"..\..\data\order.txt";

        static void Main(string[] args)
        {
            Console.WriteLine("Parsing...");
            Parser.ParseDistances(distanceDir, 1098, out int[,] d, out int[,] t);
            Console.WriteLine("Done");
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
        }
    }
}
