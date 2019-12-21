using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;

namespace AfvalOphaler
{
    static class Parser
    {
        /// <summary>
        /// Parses the dist.txt file that contains the distance and time between each matrix-ID.
        /// </summary>
        /// <param name="dir">Directory of dist.txt</param>
        /// <param name="nIds">Number of matrix-IDs</param>
        /// <param name="dists">2D array that will contain the distance between each matrix-ID</param>
        /// <param name="times">2D array that will contain the time between each matrix-ID (in minutes)</param>
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
                    rowd[j] = int.Parse(row[j]);
                dists[rowd[0], rowd[1]] = rowd[2];
                times[rowd[0], rowd[1]] = rowd[3] / 60.0;
            }           
        }

        /// <summary>
        /// Parses only the x- and y-coordinates of the orders in the order.txt file.
        /// </summary>
        /// <param name="dir">Directory of order.txt</param>
        /// <returns>A list of points that represents the location of each order</returns>
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

        // [DEPRECATED]
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

        /// <summary>
        /// Parses the order.txt file to a list of Orders/
        /// </summary>
        /// <param name="dir">Directory of order.txt</param>
        /// <returns>List of parsed Orders</returns>
        public static List<Order> ParseOrdersArr(string dir)
        {
            string[] lines = File.ReadAllLines(dir);
            List<Order> orders = new List<Order>(lines.Length - 1);
            for (int i = 1; i < lines.Length; i++)
                orders.Add(new Order(lines[i].Split(';')));
            return orders;
        }

        /// <summary>
        /// Assigns a cluster to each order following the K-Means clustering algorithm:
        /// - Assign each order to a random cluster
        /// - Calculate middle-point of each cluser
        /// - While (change_in_middle_points)
        ///     - Assign each order to nearest cluster
        ///     - Recalculate middle-point of each cluster
        /// </summary>
        /// <param name="orders">The orders to cluster</param>
        /// <param name="k">Amount of clusters</param>
        /// <param name="maxI">Maximum iterations</param>
        public static void KMeansClusterOrders(List<Order> orders, int k, int maxI)
        {
            Random rnd = new Random();
            List<Pointc> clusters = new List<Pointc>(k);
            int xmax = orders.Max(a => a.XCoord);
            int ymax = orders.Max(a => a.YCoord);
            int xmin = orders.Min(a => a.XCoord);
            int ymin = orders.Min(a => a.YCoord);

            int npk = orders.Count / k;

            // Initialize clusters
            List<int> ks = new List<int>(k);
            for (int i = 0; i < k; i++)
            {
                clusters.Add(new Pointc());
                ks.Add(i);
            }

            ks.Sort((a, b) => rnd.Next(-1, 2));

            // Assign each order to a cluster
            int j = 0;
            for (int i = 0; i < orders.Count; i++)
            {
                if (j == k)
                {
                    j = 0;
                    ks.Sort((a, b) => rnd.Next(-1, 2));
                }
                orders[i].Cluster = ks[j++];
            }

            // Do:
            // - Update middle-points of each cluster
            // - Assign each order to the nearest cluster
            int iter = 0;
            do { UpdateMeans(); /*Console.WriteLine(iter++);*/ iter++; } while (UpdateClusters() && iter < maxI);
         
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
                    if (ninc[i] == 0) continue;
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
                // Check if there are any empty clusters:
                // (Not used at the moment)
                if (EmptyCluster()) return false;

                return changed;
            }

            // Calculates the Euclidean distance between two points:
            int EucDist(int x, int y, Pointc p2) => Convert.ToInt32(Math.Sqrt(Math.Pow(x - p2.X, 2) + Math.Pow(y - p2.Y, 2)));

            // Checks if there are empty clusters:
            bool EmptyCluster()
            {
                int[] ninc = new int[k];
                foreach (Order order in orders)
                    ninc[order.Cluster]++;
                foreach (int i in ninc)
                    if (i == 0) return true;
                return false;
            }

        }

        // Small helper class to represent points in 2D space:
        public class Pointc
        {
            public int X;
            public int Y;
        }
    }
}
