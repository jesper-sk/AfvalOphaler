using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfvalOphaler.Old_Code
{
    public class Loop : IEnumerable
    {
        #region Variables & Constructor
        public double Duration;
        public double RoomLeft;
        public int Count;

        public Node Start; //Order references to Dump

        public Loop()
        {
            Start = new Node();
            Duration = 30;              //Het storten moet één keer per Loop (lus) gebeuren
            RoomLeft = 20000;           //Gecomprimeerd
            Count = 0;
        }

        public Node[] ToList()
        {
            Node[] nodes = new Node[Count];
            Node curr = Start.Next; int i = 0;
            while (!curr.IsDump)
            {
                nodes[i] = curr;
                curr = curr.Next;
                i++;
            }
            return nodes;
        }
        #endregion

        #region Order Addition/Removal
        public Node AddOrder(Order order, Node nextTo)
        {
            Duration += (order.TimeToEmpty
                + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

            Node n = nextTo.AppendNext(order);

            RoomLeft -= (order.NumContainers * order.VolPerContainer * 0.2);
            Count++;

            return n;
        }
        public void RemoveNode(Node n)
        {
            Order order = n.Data;

            Duration -= order.TimeToEmpty + GD.JourneyTime[n.Prev.Data.MatrixId, order.MatrixId] + GD.JourneyTime[order.MatrixId, n.Next.Data.MatrixId];
            Duration += GD.JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId];

            n.Remove();

            RoomLeft += (order.NumContainers * order.VolPerContainer * 0.2);
            Count--;
        }
        #endregion

        public bool EvaluateRandomAdd(Random rnd)
        {
            while (true)
            {
                int nodeI = rnd.Next(0, Count);

            }
        }

        public override string ToString()
        {
            return $"nodeCount={Count}, time={Duration}, roomLeft={RoomLeft}";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DayScheduleEnumerator(Start);
        }
    }
}
