using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfvalOphaler
{
    public class BigLL : IEnumerable
    {
        public List<Node> Nodes;
        public int Length;

        Node Head;
        Node Foot;

        public Node HeadX;
        public Node HeadY;
        public Node HeadTime;
        public Node HeadScore;
        public Node FootX;
        public Node FootY;
        public Node FootTime;
        public Node FootScore;

        public BigLL(params Order[] orders) //Assumes orders are ordered on orderId
        {
            Head = new Node();
            Foot = new Node();
            Head.Seq.Prev = Foot;
            Foot.Seq.Next = Head;
            Length = orders.Length;
            Nodes = new List<Node>(orders.Length);

            for (int i = 0; i < orders.Length; i++)
            {
                Node n = AppendOrder(orders[i]);
                if (orders[i].OrderId == 0) Console.WriteLine(i);
                Nodes.Add(n);
            }

            Nodes.Sort((a, b) => a.Order.XCoord.CompareTo(b.Order.XCoord));
            HeadX = Nodes[0];
            for (int i = 1; i < Length; i++)
            {
                Nodes[i].SeqX.Next = Nodes[i - 1];
                Nodes[i - 1].SeqX.Prev = Nodes[i];
            }
            FootX = Nodes[Length - 1];

            Nodes.Sort((a, b) => a.Order.YCoord.CompareTo(b.Order.YCoord));
            HeadY = Nodes[0];
            for (int i = 1; i < Length; i++)
            {
                Nodes[i].SeqY.Next = Nodes[i - 1];
                Nodes[i - 1].SeqY.Prev = Nodes[i];
            }
            FootY = Nodes[Length - 1];

            Nodes.Sort((a, b) => a.Order.JourneyTime.CompareTo(b.Order.JourneyTime));
            HeadTime = Nodes[0];
            for (int i = 1; i < Length; i++)
            {
                Nodes[i].SeqDist.Next = Nodes[i - 1];
                Nodes[i - 1].SeqDist.Prev = Nodes[i];
            }
            FootTime = Nodes[Length - 1];

            Nodes.Sort((a, b) => a.Order.Score.CompareTo(b.Order.Score));
            HeadScore = Nodes[0];
            for (int i = 1; i < Length; i++)
            {
                Nodes[i].SeqScore.Next = Nodes[i - 1];
                Nodes[i - 1].SeqScore.Prev = Nodes[i];
            }
            FootScore = Nodes[Length - 1];
        }

        private Node AppendOrder(Order o)
        {
            Node n = new Node(o);
            n.Seq.Prev = Foot;
            n.Seq.Next = Foot.Seq.Next;
            Foot.Seq.Next.Seq.Prev = n;
            Foot.Seq.Next = n;
            return n;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class Node
    {
        public Order Order;

        public DoubleLink Seq = new DoubleLink();
        public DoubleLink SeqX = new DoubleLink();
        public DoubleLink SeqY = new DoubleLink();
        public DoubleLink SeqDist = new DoubleLink();
        public DoubleLink SeqScore = new DoubleLink(); //Score is defined as TimeToEmpty to distance to dump ratio.

        public List<DoubleLink> Loops = new List<DoubleLink>();

        public readonly bool IsSentry;
        public Node(Order o)
        {
            Order = o;
            IsSentry = false;
        }
        public Node()
        {
            IsSentry = true;
        }
    }

    public class DoubleLink
    {
        public Node Next;
        public Node Prev;

        public DoubleLink(Node next, Node prev)
        {
            Next = next;
            Prev = prev;
        }

        public DoubleLink() {; }
    }

    public struct Order
    {
        public readonly int OrderId;
        public readonly string Name;
        public readonly int Frequency;
        public readonly int NumContainers;
        public readonly int VolPerContainer;
        public readonly double TimeToEmpty;
        public readonly int MatrixId;
        public readonly int XCoord;
        public readonly int YCoord;
        public readonly int JourneyTime;
        public readonly double Score;

        public Order (string[] row, int[,] t)
        {
            OrderId = int.Parse(row[0]);
            Name = row[1].Trim();
            Frequency = row[2][0] - '0';
            NumContainers = int.Parse(row[3]);
            VolPerContainer = int.Parse(row[4]);
            TimeToEmpty = double.Parse(row[5]);
            MatrixId = int.Parse(row[6]);
            XCoord = int.Parse(row[7]);
            YCoord = int.Parse(row[8]);
            JourneyTime = t[Dump.MatrixId, MatrixId];
            Score = Math.Round(((NumContainers * VolPerContainer) + (TimeToEmpty * 100)) / JourneyTime, 3);
        }

        public override string ToString()
        {
            return $"oid{OrderId}; mid{MatrixId}; s{Score}; jt{JourneyTime}; f{Frequency}PWK; nc{NumContainers}; vpc{VolPerContainer}; tte{TimeToEmpty}; x{XCoord}; y{YCoord}; {Name}";
        }
    }
}
