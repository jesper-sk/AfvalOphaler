using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GD = AfvalOphaler.GD;
using Order = AfvalOphaler.Order;

namespace NAfvalOphaler
{
    public class Schedule
    {
        //Public Variables
        public double Duration = 0;
        public double Penalty;
        public double Score => Duration + Penalty;

        //Private Variables
        private DayRoute[][] dayRoutes;     //zo dat dayRoutes[i][j] is de dagroute van dag i voor truck j
        private List<Order> orders;

        public Schedule(List<Order> orders)
        {
            this.orders = orders;

            dayRoutes = new DayRoute[5][];
            for (int d = 0; d < 5; d++) dayRoutes[d] = new DayRoute[2];

            foreach (Order o in orders) Penalty += 3 * o.Frequency * o.TimeToEmpty;
        }

        public int AddLoop(int day, int truck)
        {
            Duration += 30;
            return dayRoutes[day][truck].AddLoop();
        }

        public void AddOrder(Order o, Node nextTo, int loop, int day, int truck)
        {
            DayRoute route = dayRoutes[day][truck];
            Duration -= route.Duration;
            route.AddOrderToLoop(o, nextTo, loop);
            Duration += route.Duration;
            Penalty -= 3 * o.Frequency * o.TimeToEmpty;
        }

        public void RemoveNode(Node toDelete, int loop, int day, int truck)
        {
            DayRoute route = dayRoutes[day][truck];
            Duration -= route.Duration;
            route.RemoveNodeFromLoop(toDelete, loop);
            Duration += route.Duration;
            Penalty += 3 * toDelete.Data.TimeToEmpty;
        }

        public override string ToString()
        {
            return $"Score: {Score}, Total time: {Duration}, Total Penalty: {Penalty}";
        }

        public string ToCheckString()
        {
            StringBuilder b = new StringBuilder();
            for (int t = 0; t < 2; t++)
            {
                for (int d = 0; d < 5; d++)
                {
                    List<Loop> loops = Days[d, t].Loops;
                    int global = 1;
                    for (int l = 0; l < loops.Count; l++)
                    {
                        Loop curr = loops[l];
                        Node ord = curr.Start;

                        do
                        {
                            ord = ord.Next;
                            b.AppendLine($"{t + 1}; {d + 1}; {global++}; {ord.Data.OrderId}");
                        } while (!ord.IsDump);
                    }
                }
            }
            return b.ToString();
        }

        public string GetStatistics()
        {
            StringBuilder res = new StringBuilder();
            res.AppendLine("Score = " + Score);
            res.AppendLine("TotalTime = " + totalTime);
            res.AppendLine("TotalPenalty = " + totalPenalty);
            for (int i = 0; i < 5; i++)
            {
                res.AppendLine($"Day {i}:");
                res.AppendLine($"Truck 1: {Days[i, 0]}");
                res.AppendLine($"Truck 2: {Days[i, 1]}");
            }
            return res.ToString();
        }
    }

    public class DayRoute
    {
        #region Variables & Constructor
        public List<Loop> Loops;
        public double TimeLeft;
        public readonly int DayIndex;
        public readonly int TruckIndex;

        public double Duration => 720 - TimeLeft;

        public DayRoute(int dind, int trind)
        {
            Loops = new List<Loop>();
            TimeLeft = 720;
            DayIndex = dind;
            TruckIndex = trind;
        }
        #endregion

        #region Loops Modifications
        public int AddLoop()
        {
            Loops.Add(new Loop());
            TimeLeft -= 30;
            return Loops.Count - 1;
        }
        public Node AddOrderToLoop(Order order, Node nextTo, int loopIndex)
        {
            TimeLeft += Loops[loopIndex].Duration;
            Node res = Loops[loopIndex].AddOrder(order, nextTo);      
            TimeLeft -= Loops[loopIndex].Duration;
            return res;
        }
        public void RemoveNodeFromLoop(Node n, int loopIndex)
        {
            TimeLeft += Loops[loopIndex].Duration;
            Loops[loopIndex].RemoveNode(n);
            TimeLeft -= Loops[loopIndex].Duration;
        }
        #endregion

        public override string ToString()
        {
            return $"LoopCount={Loops.Count}, timeLeft={TimeLeft}";
        }
    }

    public class Loop
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

        public override string ToString()
        {
            return $"nodeCount={Count}, time={Duration}, roomLeft={RoomLeft}";
        }
    }

    public class Node
    {
        #region Variables & Constructors
        public readonly Order Data;
        public readonly bool IsDump;

        public Node Prev;
        public Node Next;

        public Node()
        {
            IsDump = true;
            Data = GD.Dump;
            Prev = Next = this;
        }
        public Node(Order o)
        {
            IsDump = false;
            Data = o;
        }
        #endregion

        #region Modifications
        public Node AppendNext(Order o)
        {
            Node n = new Node(o)
            {
                Prev = this,
                Next = Next
            };

            Next.Prev = n;
            Next = n;

            return n;
        }
        public void Remove()
        {
            Next.Prev = Prev;
            Prev.Next = Next;
            Next = null;
            Prev = null;
        }
        #endregion
    }
}
