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

        public List<BigLLNode> Nodes;
        public int Length;

        BigLLNode Head;
        BigLLNode Foot;

        public BigLLNode HeadX;
        public BigLLNode HeadY;
        public BigLLNode HeadTime;
        public BigLLNode HeadScore;
        public BigLLNode FootX;
        public BigLLNode FootY;
        public BigLLNode FootTime;
        public BigLLNode FootScore;

        public BigLL(params Order[] orders) //Assumes orders are ordered on orderId
        {
            Head = new BigLLNode();
            Foot = new BigLLNode();
            Head.Seq.Prev = Foot;
            Foot.Seq.Next = Head;
            Length = orders.Length;
            Nodes = new List<BigLLNode>(orders.Length);

            for (int i = 0; i < orders.Length; i++)
            {
                BigLLNode n = AppendOrder(orders[i]);
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

            BigLLNode[] temp = Nodes.ToArray();
            foreach(BigLLNode node in temp)
            {
                Nodes.Sort((a, b) => 
                    GD.JourneyTime[node.Order.MatrixId, a.Order.MatrixId]
                        .CompareTo(
                            GD.JourneyTime[node.Order.MatrixId, b.Order.MatrixId]
                            )
                        );

                node.Nearest = Nodes.ToArray();
            }
        }

        private BigLLNode AppendOrder(Order o)
        {
            BigLLNode n = new BigLLNode(o);
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

    public class BigLLNode
    {
        public Order Order;

        public DoubleLink Seq = new DoubleLink();
        public DoubleLink SeqX = new DoubleLink();
        public DoubleLink SeqY = new DoubleLink();
        public DoubleLink SeqDist = new DoubleLink();
        public DoubleLink SeqScore = new DoubleLink(); //Score is defined as TimeToEmpty to distance to dump ratio.

        public List<DoubleLink> Loops = new List<DoubleLink>();

        public BigLLNode[] Nearest;

        public int[] NumVisits = new int[5];

        public readonly bool IsSentry;
        public BigLLNode(Order o)
        {
            Order = o;
            IsSentry = false;
        }
        public BigLLNode()
        {
            IsSentry = true;
        }

        public List<BigLLNode> GetNearestLoopNodes(out List<int> days)
        {
            foreach(int[] combi in GD.AllowedDayCombinations[Order.Frequency])
            {
                List<BigLLNode> att = new List<BigLLNode>(Order.Frequency);
                List<int> attd = new List<int>(Order.Frequency);
                foreach(int day in combi)
                {
                    BigLLNode take = null;
                    for(int i = 0; i < Nearest.Length; i++)
                    {
                        BigLLNode curr = Nearest[i];
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
            return new List<BigLLNode>(0);
        }
    }

    public class DoubleLink
    {
        public BigLLNode Next;
        public BigLLNode Prev;

        public DoubleLink(BigLLNode next, BigLLNode prev)
        {
            Next = next;
            Prev = prev;
        }

        public DoubleLink() {; }
    }

    public class Order
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
            JourneyTime = GD.JourneyTime[GD.Dump.MatrixId, MatrixId];
            Score = (JourneyTime + 1) / TimeToEmpty;
        }

        public Order() { }

        public override string ToString()
        {
            return $"oid{OrderId}; mid{MatrixId}; s{Score}; jt{JourneyTime}; f{Frequency}PWK; nc{NumContainers}; vpc{VolPerContainer}; tte{TimeToEmpty}; x{XCoord}; y{YCoord}; {Name}";
        }
    }
}
