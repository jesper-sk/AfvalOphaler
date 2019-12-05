﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace AfvalOphaler
{
    static class Parser
    {
        // Spaties weghalen
        // Splitsen op ;
        // DoMagic(overgebleven_dingen)

        public static void ParseDistances(string dir, int nIds, out int[,] dists, out double[,] times)
        {
            string[] lines = File.ReadAllLines(dir);
            dists = new int[nIds + 1, nIds + 1];
            times = new double[nIds+1, nIds+1];
            for(int i = 1; i < lines.Length; i++)
            {
                string[] row = lines[i].Split(';');
                int[] rowd = new int[row.Length];
                for(int j = 0; j < row.Length; j++)
                {
                    rowd[j] = int.Parse(row[j]);
                }
                dists[rowd[0], rowd[1]] = rowd[2];
                times[rowd[0], rowd[1]] = rowd[3] / 60.0;
            }           
        }

        public static List<Point> ParseOrderCoordinates(string dir)
        {
            List<Point> orders = new List<Point>();
            string[] lines = File.ReadAllLines(dir);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] splitline = lines[i].Trim().Split(';');
                int x = int.Parse(splitline[7]);
                int y = int.Parse(splitline[8]);
                orders.Add(new Point(x, y));
            }
            return orders;
        }

        public static BigLL ParseOrders(string dir)
        {
            string[] lines = File.ReadAllLines(dir);
            Order[] orders = new Order[lines.Length-1];
            for (int i = 1; i < lines.Length; i++)
            {
                Order o = new Order(lines[i].Split(';'));
                //Console.WriteLine(o.ToString());
                orders[i-1] = o;
            }
            return new BigLL(orders);
        }

        public static List<Order> ParseOrdersArr(string dir)
        {
            string[] lines = File.ReadAllLines(dir);
            List<Order> orders = new List<Order>(lines.Length - 1);
            for (int i = 1; i < lines.Length; i++)
            {
                Order o = new Order(lines[i].Split(';'));
                //Console.WriteLine(o.ToString());
                orders.Add(o);
            }
            return orders;
        }
    }
}
