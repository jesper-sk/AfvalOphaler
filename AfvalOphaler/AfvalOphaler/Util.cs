using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public static class GD
    {
        public static readonly Random rnd = new Random();

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
}
