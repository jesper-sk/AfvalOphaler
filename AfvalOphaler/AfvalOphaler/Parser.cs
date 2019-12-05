using System;
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
                orders.Add(new System.Drawing.Point(x, y));
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

        public static void KMeansClusterOrders(List<Order> orders, int k)
        {
            Random rnd = new Random();
            List<Pointc> clusters = new List<Pointc>(k);
            int xmax = orders.Max(a => a.XCoord);
            int ymax = orders.Max(a => a.YCoord);
            int xmin = orders.Min(a => a.XCoord);
            int ymin = orders.Min(a => a.YCoord);

            //Setting clusters
            for (int i = 0; i < k; i++)
            {
                clusters.Add(new Pointc());
                orders[i].Cluster = i;
            }
            for (int i = k; i < orders.Count; i++)
            {
                orders[i].Cluster = rnd.Next(0, k);
            }

            do { UpdateMeans(); Console.WriteLine("Cluster");  } while (UpdateClusters());

            
            void UpdateMeans()
            {
                int[] ninc = new int[k];
                long[] xs = new long[k];
                long[] ys = new long[k];
                foreach (Order order in orders)
                {
                    int c = order.Cluster;
                    ninc[c]++;
                    xs[c] += order.XCoord;
                    ys[c] += order.YCoord;
                }
                for (int i = 0; i < k; i++)
                {
                    clusters[i].X = Convert.ToInt32(xs[i] / ninc[i]);
                    clusters[i].Y = Convert.ToInt32(ys[i] / ninc[i]);
                }
            }

            bool UpdateClusters()
            {
                bool changed = false;
                foreach (Order order in orders)
                {
                    int ind = -1;
                    int mind = int.MaxValue;
                    for(int i = 0; i < k; i++)
                    {
                        int dist = EucDist(order.XCoord, order.YCoord, clusters[i]);
                        if (dist < 0) Console.WriteLine("Overflow");
                        if (dist < mind)
                        {
                            mind = dist;
                            ind = i;
                        }
                    }

                    if (ind != order.Cluster)
                    {
                        order.Cluster = ind;
                        changed = true;
                    }
                }
                if (EmptyCluster())
                {
                    Console.WriteLine("Empty");
                    return false;
                }
                return changed;
            }

            int EucDist(int x, int y, Pointc p2) => Convert.ToInt32(Math.Sqrt(Math.Pow(x - p2.X, 2) + Math.Pow(y - p2.Y, 2)));

            bool EmptyCluster()
            {
                int[] ninc = new int[k];
                foreach (Order order in orders)
                {
                    ninc[order.Cluster]++;
                }
                foreach (int i in ninc)
                {
                    if (i == 0) return true;
                }
                return false;
            }

        }

        public class Pointc
        {
            public int X;
            public int Y;
        }
    }
}
