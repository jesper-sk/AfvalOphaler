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
        //TODO: Add Dump

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

            Node[] temp = Nodes.ToArray();
            foreach(Node node in temp)
            {
                Nodes.Sort((a, b) => 
                    GlobalData.JourneyTime[node.Order.MatrixId, a.Order.MatrixId]
                        .CompareTo(
                            GlobalData.JourneyTime[node.Order.MatrixId, b.Order.MatrixId]
                            )
                        );

                node.Nearest = Nodes.ToArray();
            }
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

        public Node[] Nearest;

        public int[] NumVisits = new int[5];

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

        public List<Node> GetNearestLoopNodes(out List<int> days)
        {
            foreach(int[] combi in GlobalData.AllowedDayCombinations[Order.Frequency])
            {
                List<Node> att = new List<Node>(Order.Frequency);
                List<int> attd = new List<int>(Order.Frequency);
                foreach(int day in combi)
                {
                    Node take = null;
                    for(int i = 0; i < Nearest.Length; i++)
                    {
                        Node curr = Nearest[i];
                        if (curr.NumVisits[day] > 0)
                        {
                            take = curr;
                            break;
                        }
                    }
                    if (take != null)
                    {
                        att.Add(take);
                        attd.Add(day);
                    }
                    else break; //Nothing found, proceed to next daycombination
                }
                if (att.Count == Order.Frequency)
                {
                    days = attd;
                    return att;
                }
            }
            days = new List<int>(0);
            return new List<Node>(0);
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
        public int OrderId;
        public string Name;
        public int Frequency;
        public int NumContainers;
        public int VolPerContainer;
        public double TimeToEmpty;
        public int MatrixId;
        public int XCoord;
        public int YCoord;
        public int JourneyTime;
        public double Score;

        public Order (string[] row)
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
            JourneyTime = GlobalData.JourneyTime[GlobalData.Dump.Order.MatrixId, MatrixId];
            Score = Math.Round(((NumContainers * VolPerContainer) + (TimeToEmpty * 100)) / JourneyTime, 3);
        }

        public override string ToString()
        {
            return $"oid{OrderId}; mid{MatrixId}; s{Score}; jt{JourneyTime}; f{Frequency}PWK; nc{NumContainers}; vpc{VolPerContainer}; tte{TimeToEmpty}; x{XCoord}; y{YCoord}; {Name}";
        }
    }
}
