using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AfvalOphaler
{
    public static class Util
    {
        public static string ListToString(List<int> l)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            if (l.Count != 0)
            {
                sb.Append(l[0]);
                for (int j = 1; j < l.Count; j++)
                {
                    int i = l[j];
                    sb.Append($", {i}");
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static string ArrToString(int[] l)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            if (l.Length != 0)
            {
                sb.Append(l[0]);
                for (int j = 1; j < l.Length; j++)
                {
                    int i = l[j];
                    sb.Append($", {i}");
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

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
        //public static BigLLNode DumpLLing = new BigLLNode(Dump);

        // [Frequentie, aantal_combinaties, allowed_days_in_combi]
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

    //Thread-safe static random number generator class
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
