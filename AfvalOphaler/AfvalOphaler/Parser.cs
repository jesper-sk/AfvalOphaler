using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AfvalOphaler
{
    static class Parser
    {
        // Spaties weghalen
        // Splitsen op ;
        // DoMagic(overgebleven_dingen)

        public static void ParseDistances(string dir, int nIds, out int[,] dists, out int[,] times)
        {
            string[] lines = File.ReadAllLines(dir);
            dists = times = new int[nIds+1, nIds+1];
            for(int i = 1; i < lines.Length; i++)
            {
                string[] row = lines[i].Split(';');
                int[] rowd = new int[row.Length];
                for(int j = 0; j < row.Length; j++)
                {
                    rowd[j] = int.Parse(row[j]);
                }
                dists[rowd[0], rowd[1]] = rowd[2];
                times[rowd[0], rowd[1]] = rowd[3];
            }
        }

        public static BigLL ParseOrders(string dir, int[,] t)
        {
            string[] lines = File.ReadAllLines(dir);
            Order[] orders = new Order[lines.Length-1];
            for (int i = 1; i < lines.Length; i++)
            {
                Order o = new Order(lines[i].Split(';'), t);
                //Console.WriteLine(o.ToString());
                orders[i-1] = o;
            }
            return new BigLL(orders);
        }
    }
}
