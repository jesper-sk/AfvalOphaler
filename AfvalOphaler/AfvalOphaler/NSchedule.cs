using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GD = AfvalOphaler.GD;
using Order = AfvalOphaler.Order;

namespace NAfvalOphaler
{
    #region Schedule
    public class Schedule
    {
        #region Public Variables
        public double Duration = 0;
        public double Penalty;
        public double Score => Duration + Penalty;
        public double CaculateDuration()
        {
            double duration = 0;
            foreach (DayRoute[] bigDay in dayRoutes) foreach (DayRoute day in bigDay) duration += day.Duration;
            Duration = duration;
            return duration;
            //Loop l = dayRoutes[0][0].Loops[0];
            //double duration = 30 + GD.JourneyTime[l.Start.Data.MatrixId, l.Start.Next.Data.MatrixId];
            //Node curr = l.Start.Next;
            //while (!curr.IsDump)
            //{
            //    duration += GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId];
            //    curr = curr.Next;
            //}
            //return duration;
        }
        public double CalculatePenalty()
        {
            double penalty = 0;
            foreach (Order o in orders) penalty += 3 * o.Frequency * o.TimeToEmpty;
            Penalty = penalty;
            return penalty;
        }
        #endregion

        #region Private Variables
        public DayRoute[][] dayRoutes;     //zo dat dayRoutes[i][j] is de dagroute van dag i voor truck j
        private List<Order> orders;
        private Random rnd;
        #endregion

        #region Constructor(s)
        public Schedule(List<Order> orders)
        {
            this.orders = orders.ToList();
            foreach (Order o in this.orders) Penalty += 3 * o.Frequency * o.TimeToEmpty;

            dayRoutes = new DayRoute[5][];
            for (int d = 0; d < 5; d++)
            {
                dayRoutes[d] = new DayRoute[2];
                for (int t = 0; t < 2; t++) dayRoutes[d][t] = new DayRoute(d, t);
            }

            rnd = new Random();
        }
        #endregion

        #region [DEPRICATED] Add/Remove
        //public int AddLoop(int day, int truck)
        //{
        //    Duration += 30;
        //    return dayRoutes[day][truck].AddLoop();
        //}

        //public Node AddOrder(Order o, Node nextTo, int loop, int day, int truck)
        //{
        //    DayRoute route = dayRoutes[day][truck];
        //    Duration -= route.Duration;
        //    Node newAdded = route.AddOrderToLoop(o, nextTo, loop);
        //    Duration += route.Duration;
        //    Penalty -= 3 * o.Frequency * o.TimeToEmpty;
        //    return newAdded;
        //}

        //public void RemoveNode(Node toDelete, int loop, int day, int truck)
        //{
        //    DayRoute route = dayRoutes[day][truck];
        //    Duration -= route.Duration;
        //    route.RemoveNodeFromLoop(toDelete, loop);
        //    Duration += route.Duration;
        //    Penalty += 3 * toDelete.Data.TimeToEmpty;
        //}
        #endregion

        #region ToStrings
        public ScheduleResult ToResult() => new ScheduleResult() 
        { 
            Score = Score, 
            Stats = GetStatisticsBuilder(), 
            Check = ToCheckStringBuilder() 
        };
        public override string ToString() => $"Score: {Score}, Total time: {Duration}, Total Penalty: {Penalty}";
        public string ToCheckString() => ToCheckStringBuilder().ToString();
        public StringBuilder ToCheckStringBuilder()
        {
            StringBuilder b = new StringBuilder();
            for (int t = 0; t < 2; t++)
            { 
                for (int d = 0; d < 5; d++)
                {
                    int ordOfDay = 0;
                    foreach (Node n in dayRoutes[d][t])
                        b.AppendLine($"{t + 1}; {d + 1}; {++ordOfDay}; {n.Data.OrderId}");
                }
            }
            //for (int t = 0; t < 2; t++)
            //    for (int d = 0; d < 5; d++)
            //    {
            //        List<Loop> loops = dayRoutes[d][t].Loops;
            //        int global = 1;
            //        for (int l = 0; l < loops.Count; l++)
            //        {
            //            Loop curr = loops[l];
            //            Node ord = curr.Start;

            //            do
            //            {
            //                ord = ord.Next;
            //                b.AppendLine($"{t + 1}; {d + 1}; {global++}; {ord.Data.OrderId}");
            //            } while (!ord.IsDump);
            //        }
            //    }
            return b;
        }
        public string GetStatistics() => GetStatisticsBuilder().ToString();
        public StringBuilder GetStatisticsBuilder()
        {
            StringBuilder res = new StringBuilder();
            res.AppendLine("Score = " + Score);
            res.AppendLine("TotalTime = " + Duration);
            res.AppendLine("TotalPenalty = " + Penalty);
            for (int i = 0; i < 5; i++)
            {
                res.AppendLine($"Day {i}:");
                res.AppendLine($"Truck 1: {dayRoutes[i][0]}");
                res.AppendLine($"Truck 2: {dayRoutes[i][1]}");
            }
            return res;
        }
        #endregion

        #region Operations
        private Func<NeighborOperation>[] ops =
        {
            new Func<NeighborOperation>(() => new RandomAddOperation()),
            new Func<NeighborOperation>(() => new RandomDeleteOperation()),
            new Func<NeighborOperation>(() => new RandomTransferOperation())
        };

        public NeighborOperation[] GetOperations(double[] probDist, int nOps)
        {
            NeighborOperation[] res = new NeighborOperation[nOps];
            for(int j = 0; j < nOps; j++)
            {
                double acc = 0;
                double p = rnd.NextDouble();
                for (int i = 0; i < probDist.Length; i++)
                {
                    acc += probDist[i];
                    if (p <= acc)
                    {
                        res[j] = ops[i]();
                        break;
                    }
                }
            }
            return res;
        }

        public abstract class NeighborOperation
        {
            public bool isEvaluated = false;
            public double? TotalDelta => DeltaTime + DeltaPenalty;

            public double? DeltaTime = null;
            public double? DeltaPenalty = null;

            public void Apply()
            {
                if (!isEvaluated) throw new InvalidOperationException("Evaluate operation first!");
                _Apply();
            }

            public bool Evaluate()
            {
                if (_Evaluate(out double dT, out double dP))
                {
                    isEvaluated = true;
                    DeltaTime = dT;
                    DeltaPenalty = dP;
                    return true;
                }
                else return false;
            }

            protected abstract bool _Evaluate(out double deltaTime, out double deltaPenalty);
            protected abstract void _Apply();
        }

        public class RandomAddOperation : NeighborOperation
        {
            public RandomAddOperation()
            {
                //Hé jochie
                
            }

            protected override void _Apply()
            {
                throw new NotImplementedException();
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            { 
                throw new NotImplementedException();
            }
        }
        public class RandomDeleteOperation : NeighborOperation
        {
            public RandomDeleteOperation()
            {
                //Hé jochie
            }

            protected override void _Apply()
            {
                throw new NotImplementedException();
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                throw new NotImplementedException();
            }
        }
        public class RandomTransferOperation : NeighborOperation
        {
            public RandomTransferOperation()
            {
                //Hé jochie
            }

            protected override void _Apply()
            {
                throw new NotImplementedException();
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
    #endregion

    #region ScheduleResult
    public class ScheduleResult
    {
        public double Score;
        public StringBuilder Stats;
        public StringBuilder Check;
    }
    #endregion

    #region DayRoute
    public class DayRoute : IEnumerable
    {
        #region Variables & Constructor
        public double TimeLeft;
        public double Duration => 720 - TimeLeft;

        public readonly int DayIndex;
        public readonly int TruckIndex;

        public Node FirstDump => dumps[0];

        public List<Node> dumps;
        public List<double> spaceLefts;

        public DayRoute(int dind, int trind)
        {
            TimeLeft = 690;
            DayIndex = dind;
            TruckIndex = trind;

            Node dump0 = new Node(0);
            Node dumpL = new Node();

            dump0.Next = dumpL;
            dumpL.Prev = dump0;

            dumps = new List<Node> { dump0 };
            spaceLefts = new List<double> { 20000 };
        }
        #endregion

        #region Tour Modifications
        public Node AddOrder(Order order, Node nextTo)
        {
            TimeLeft -= (order.TimeToEmpty
                    + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                    + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                    - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

            Node n = nextTo.AppendNext(order);

            spaceLefts[n.TourIndex] -= (order.NumContainers * order.VolPerContainer * 0.2);

            if (n.IsDump)
            {
                for (Node curr = n; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex++;

                double newSpaceTaken = 0;
                for (Node curr = n.Next; !curr.IsDump; curr = curr.Next)
                    newSpaceTaken += curr.Data.NumContainers * curr.Data.VolPerContainer * 0.2;

                dumps.Insert(n.TourIndex, n);
                spaceLefts.Insert(n.TourIndex, 20000 - newSpaceTaken);
                spaceLefts[n.TourIndex - 1] -= newSpaceTaken;
            }

            return n;
            //TimeLeft += Loops[loopIndex].Duration;
            //Node res = Loops[loopIndex].AddOrder(order, nextTo);
            //TimeLeft -= Loops[loopIndex].Duration;
            //return res;
        }
        public void RemoveNode(Node n)
        {
            Order order = n.Data;

            TimeLeft += (order.TimeToEmpty 
                + GD.JourneyTime[n.Prev.Data.MatrixId, order.MatrixId] 
                + GD.JourneyTime[order.MatrixId, n.Next.Data.MatrixId]
                - GD.JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId]);

            spaceLefts[n.TourIndex] += (order.NumContainers * order.VolPerContainer * 0.2);

            if (n.IsDump)
            {
                for (Node curr = n.Next; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex--;

                dumps.RemoveAt(n.TourIndex);
                spaceLefts[n.TourIndex - 1] -= 20000 - spaceLefts[n.TourIndex];
                spaceLefts.RemoveAt(n.TourIndex);
            }

            n.Remove();

            //throw new AfvalOphaler.HeyJochieException("Das nog helemaal niet geimplementeerd jochie!");
            //TimeLeft += Loops[loopIndex].Duration;
            //Loops[loopIndex].RemoveNode(n);
            //TimeLeft -= Loops[loopIndex].Duration;
        }

        #endregion

        #region Evaluates
        public bool EvaluateRandomAdd(Random rnd)
        {
            throw new AfvalOphaler.HeyJochieException("Das nog helemaal niet geimplementeerd jochie!");
        }
        public bool EvaluateRandomRemove(Random rnd)
        {
            throw new AfvalOphaler.HeyJochieException("Das nog helemaal niet geimplementeerd jochie!");
        }
        #endregion

        #region Overrides / Inherited Implementations     
        public override string ToString() => $"TourCount={dumps.Count}, Duration={Duration}";    
        public IEnumerator GetEnumerator()
        {
            if (dumps.Count == 0) return null;
            return new LoopEnumerator(dumps[0]);
        }

        public string GetRoute()
        {
            StringBuilder sb = new StringBuilder();
            foreach(Node n in this)
            {
                sb.AppendLine(n.Data.OrderId.ToString());
            }
            return sb.ToString();
        }
        #endregion
    }
    #endregion

    #region [DEPRICATED] Loop
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

        #region Optimization
        public void OptimizeLoop()
        {
            void Two_Opt_Move(Node[] loop, Node i, Node j)
            {
                //Console.WriteLine($"Changing {i} to {i.Next}\nAnd {j} to {j.Next}\nTo: {i}->{j} and\n{i.Next}->{j.Next}");

                // Dit: 0 1 2 3 4 5 6 7 8 9
                // Moet dit worden: 0 8 7 6 5 4 3 2 1 9
                // i = 0
                // j = 8

                Node iplus = i.Next; // 1
                Node jplus = j.Next; // 9
                iplus.Prev = i.Next.Next; // 1.prev = 2
                j.Next = j.Prev;    // 8.next = 7
                // 0 2 1 2 3 4 5 6 7 8 7 9

                Node curr = i.Next.Next; // curr = 2
                Node stop = j;           // stop = 8

                //Console.WriteLine($"Stop: {stop}");
                while (curr != stop)
                {
                    //Console.WriteLine($"curr: {curr}");
                    Node temp = curr.Next;  // temp = 3     // temp = 4
                    curr.Next = curr.Prev;  // 2.next = 1   // 3.next = 2
                    curr.Prev = temp;       // 2.prev = 3   // 3.prev = 4
                    curr = curr.Prev;       // curr = 3     // curr = 4
                }
                // 0       8 7 6 5 4 3 2 1.... 9

                i.Next = j;         // 0.next = 8
                j.Prev = i;         // 8.prev = 0
                iplus.Next = jplus; // 1.next = 9
                jplus.Prev = iplus; // 9.prev = 1
                // 0 8 7 6 5 4 3 2 1 9

                //Console.WriteLine($"i.Next: {i.Next}\ni.Next.Prev: {i.Next.Prev}\nj.Next: {j.Next}\nj.Next.Prev: {j.Next.Prev}");
                //Console.WriteLine("---");
            }
            void Two_Opt_Node_Shift_Move(Node i, Node j)
            {
                // Node i is places between Node j and Node j.Next
                i.Next.Prev = i.Prev;
                i.Prev.Next = i.Next;
                i.Next = j.Next;
                j.Next.Prev = i;
                j.Next = i;
                i.Prev = j;
            
                //Console.WriteLine($"j: {j}\nj.Next: {j.Next}\nj.Next.Next: {j.Next.Next}");
                //Console.WriteLine($"j.Next.Prev: {j.Next.Prev}\nj.Next.Next.Prev: {j.Next.Next.Prev}");
                //Console.WriteLine("---");
            }

            Node[] nodes = ToList();
            for (int i = 0; i < Count - 2; i++)
            {
                Node x1 = nodes[i];
                Node x2 = nodes[i + 1];
                for (int j = i + 2; j < Count - 1; j++)
                {
                    Node y1 = nodes[j];
                    Node y2 = nodes[j + 1];

                    double del_dist = GD.JourneyTime[x1.Data.MatrixId, x2.Data.MatrixId] + GD.JourneyTime[y1.Data.MatrixId, y2.Data.MatrixId];
                    double X1Y1 = GD.JourneyTime[x1.Data.MatrixId, y1.Data.MatrixId];
                    double X2Y2 = GD.JourneyTime[x2.Data.MatrixId, y2.Data.MatrixId];

                    if (del_dist - (X1Y1 + X2Y2) > 0)
                    {
                        Console.WriteLine("Doing 2-opt move...");
                        Two_Opt_Move(nodes, x1, y1);
                        return;
                    } 
                    else
                    {
                        double X2Y1 = GD.JourneyTime[x2.Data.MatrixId, y1.Data.MatrixId];
                        Node z1 = nodes[i + 2];
                        if (z1 != y1)
                        {
                            if ((del_dist + GD.JourneyTime[x2.Data.MatrixId, z1.Data.MatrixId]) - (X2Y2 + X2Y1 + GD.JourneyTime[x1.Data.MatrixId, z1.Data.MatrixId]) > 0)
                            {
                                Console.WriteLine("Doing first 2.5-opt move...");
                                Two_Opt_Node_Shift_Move(x2, y1);
                                return;
                            }
                        }
                        else
                        {
                            z1 = nodes[j - 1];
                            if (z1 != x2)
                            {
                                if ((del_dist + GD.JourneyTime[y1.Data.MatrixId, z1.Data.MatrixId]) - (X1Y1 + X2Y1 + GD.JourneyTime[y2.Data.MatrixId, z1.Data.MatrixId]) > 0)
                                {
                                    Console.WriteLine("Doing second 2.5-opt move...");
                                    Two_Opt_Node_Shift_Move(y1, x1);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
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
            return new LoopEnumerator(Start);
        }
    }
    #endregion

    #region Loop Enum
    public class LoopEnumerator : IEnumerator
    {
        private readonly Node dump;
        private Node curr;
        private Node prev;

        object IEnumerator.Current => curr;
        public LoopEnumerator(Node dump)
        {
            this.dump = dump;
            curr = dump;
        }
        bool IEnumerator.MoveNext()
        {
            prev = curr;
            curr = curr.Next;
            return !prev.IsSentry;
        }
        void IEnumerator.Reset()
        {
            curr = dump;
        }
    }
    #endregion

    #region Node
    public class Node
    {
        #region Variables & Constructors
        public readonly Order Data;
        public readonly bool IsDump;
        public readonly bool IsSentry;

        public Node Prev;
        public Node Next;

        public int TourIndex;

        //public Node()
        //{
        //    IsDump = true;
        //    Data = GD.Dump;
        //    Prev = Next = this;
        //}

        public Node(Order o)
        {
            IsDump = false;
            Data = o;
        }

        public Node()
        {
            IsDump = true;
            IsSentry = true;
            Data = GD.Dump;
            TourIndex = -1;
            Next = this;
        }
        public Node(int dumpId)
        {
            IsDump = true;
            IsSentry = false;
            Data = GD.Dump;
            TourIndex = dumpId;
        }
        public Node(Order o, int tourInd)
        {
            IsDump = o.OrderId == 0;
            Data = o;
            TourIndex = tourInd;
            IsSentry = false;
        }
        #endregion

        #region Modifications
        //public Node AppendNext(Order o)
        //{
        //    Node n = new Node(o)
        //    {
        //        Prev = this,
        //        Next = Next
        //    };

        //    Next.Prev = n;
        //    Next = n;

        //    return n;
        //}
        public void Remove()
        {
            Next.Prev = Prev;
            Prev.Next = Next;
            Next = null;
            Prev = null;
        }
        public Node AppendNext(Order o)
        {
            Node n = new Node(o, TourIndex)
            {
                Prev = this,
                Next = Next
            };

            Next.Prev = n;
            Next = n;

            return n;
        }

        #endregion

        #region Overrides
        public override bool Equals(object o)
        {
            Node n = (Node)o;
            return Data.OrderId.Equals(n.Data.OrderId);
        }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => $"{(IsSentry ? "Sentry" : "Node")} ti{TourIndex}: {Data}";
        #endregion
    }
    #endregion
}
