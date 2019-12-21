using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AfvalOphaler
{
    public static class Util
    {
        /// <summary>
        /// Prints a list as a string of all elements to string seperated with a comma.
        /// </summary>
        /// <param name="l">The list of integers thats needs to be printed</param>
        /// <returns>A string of all elements to string seperated with a comma</returns>
        public static string ListToString(List<object> l)
        {
            if (l == null || l.Count == 0) return "";

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(l[0]);
            for (int j = 1; j < l.Count; j++)
                sb.Append($", {l[j].ToString()}");
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Prints an array as a string of all elements to string seperated with a comma.
        /// </summary>
        /// <param name="l">Int array that needs to be printed</param>
        /// <returns>A string of all elements to string seperated with a comma</returns>
        public static string ArrToString(int[] l)
        {
            if (l == null || l.Length == 0) return "";

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(l[0]);
            for (int j = 1; j < l.Length; j++)
            {
                int i = l[j];
                sb.Append($", {i}");
            }
            sb.Append("}");
            return sb.ToString();
        }
        /// <summary>
        /// Prints an array as a string of all elements to string seperated with a comma.
        /// </summary>
        /// <param name="l">String array that needs to be printed</param>
        /// <returns>A string of all elements to string seperated with a comma</returns>
        public static string ArrToString(string[] l)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            if (l.Length != 0)
            {
                sb.Append(l[0]);
                for (int j = 1; j < l.Length; j++)
                {
                    string i = l[j];
                    sb.Append($", {i}");
                }
            }
            sb.Append("}");
            return sb.ToString();
        }
    }

    public static partial class GD
    {
        // 2D double array that gives the time (minutes) between each matrix-ID
        public static double[,] JourneyTime;

        // Static predefined order that is the dump
        // The dump is special order that only has a time constraint
        public static Order Dump = new Order()
        {
            OrderId = 0,
            Name = "Dump",
            MatrixId = 287,
            XCoord = 56343016,
            YCoord = 513026712,
            TimeToEmpty = 30
        };

        // NOT USED:
        //public static BigLLNode DumpLLing = new BigLLNode(Dump);

        // Prefined constant that contains for each order frequency 
        //  the combination of days in which the order may be planned
        // [frequency][amount_of_combinations][allowed_days_in_combination]
        public static readonly int[][][] AllowedDayCombinations =
        {
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

    /// <summary>
    /// Thread-safe static random number generator class:
    /// Causes multiple randoms in a multi-threaded environment to have different seeds.
    /// This class is defined because multiple randoms all created around the same time
    /// will have (almost) equal seeds thus causing them to act about the same.
    /// This class we prevent this, thus causing multiple randoms created around the same time
    ///     be all random.
    /// </summary>
    public static class StaticRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> rnd = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int Next(int minValue, int maxValue) => rnd.Value.Next(minValue, maxValue);

        public static int Next(int maxValue) => rnd.Value.Next(maxValue);

        public static int Next() => rnd.Value.Next();

        public static double NextDouble() => rnd.Value.NextDouble();

        public static int[] RandomSequence(int n, int minValue, int maxValue)
        {
            int[] res = new int[n];
            HashSet<int> done = new HashSet<int>();
            for(int i = 0; i < n; i++)
            {
                int sample;
                do sample = rnd.Value.Next(minValue, maxValue); while (!done.Add(sample));
                res[i] = sample;
            }
            return res;
        }
    }
}
