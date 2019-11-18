using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AfvalOphaler
{
    class Program
    {
        const string distanceDir = @"..\..\data\dist.txt";
        const string ordersDir = @"..\..\data\order.txt";

        static void Main(string[] args)
        {
            string[] text = File.ReadAllLines(ordersDir);
            Console.WriteLine(text[0]);
            Console.ReadKey();
        }
    }
}
