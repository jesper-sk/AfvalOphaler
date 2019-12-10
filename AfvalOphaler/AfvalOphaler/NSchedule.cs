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
            foreach (DayRoute[] bigDay in DayRoutes) foreach (DayRoute day in bigDay) duration += day.Duration;
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
            foreach (Order o in UnScheduledOrders) penalty += 3 * o.Frequency * o.TimeToEmpty;
            Penalty = penalty;
            return penalty;
        }

        public List<Order> UnScheduledOrders;
        #endregion

        #region Private Variables
        public DayRoute[][] DayRoutes;     //zo dat dayRoutes[i][j] is de dagroute van dag i voor truck j
        private Random Rnd;
        #endregion

        #region Constructor(s)
        public Schedule(List<Order> orders)
        {
            UnScheduledOrders = orders.ToList();
            foreach (Order o in UnScheduledOrders) Penalty += 3 * o.Frequency * o.TimeToEmpty;

            DayRoutes = new DayRoute[5][];
            for (int d = 0; d < 5; d++)
            {
                DayRoutes[d] = new DayRoute[2];
                for (int t = 0; t < 2; t++) DayRoutes[d][t] = new DayRoute(d, t);
            }

            Rnd = new Random();
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

        #region Operations
        private Func<Schedule, NeighborOperation>[] ops =
        {
            new Func<Schedule, NeighborOperation>((s) => new RandomAddOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomDeleteOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomTransferOperation(s))
        };

        public NeighborOperation[] GetOperations(double[] probDist, int nOps)
        {
            NeighborOperation[] res = new NeighborOperation[nOps];
            for(int j = 0; j < nOps; j++)
            {
                double acc = 0;
                double p = Rnd.NextDouble();
                for (int i = 0; i < probDist.Length; i++)
                {
                    acc += probDist[i];
                    if (p <= acc)
                    {
                        res[j] = ops[i](this);
                        break;
                    }
                }
            }
            return res;
        }

        public abstract class NeighborOperation
        {
            public bool IsEvaluated { get; protected set; } = false;

            public double TotalDelta => DeltaTime + DeltaPenalty;
            public double DeltaTime { get; protected set; } = double.NaN;
            public double DeltaPenalty { get; protected set; } = double.NaN;

            public Schedule State;

            public NeighborOperation(Schedule s)
            {
                State = s;
            }

            public void Apply()
            {
                if (!IsEvaluated) throw new InvalidOperationException("Evaluate operation first!");
                _Apply();
            }

            public bool Evaluate()
            {
                if (_Evaluate(out double dT, out double dP))
                {
                    IsEvaluated = true;
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
            private Order toAdd;
            private AddOperation Operation;
            //private List<double> deltas;
            //private List<Node> whereToAdd;
            //private List<int> whereToAddDays;
            //private List<int> whereToAddTrucks;

            public RandomAddOperation(Schedule s) : base(s)
            {
                //Hé jochie          
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                toAdd = State.UnScheduledOrders[State.Rnd.Next(0, State.UnScheduledOrders.Count)];
                Operation = new AddOperation(State, toAdd);
                bool possible = Operation.Evaluate();
                deltaTime = Operation.DeltaTime;
                deltaPenalty = Operation.DeltaPenalty;
                return possible;

                #region deprecated
                //int[][] combis = GD.AllowedDayCombinations[toAdd.Frequency];
                //int[] combi = combis[state.Rnd.Next(0, combis.Length)]; // MISS ALLE COMBIS PROBEREN

                //int everyDayInCombiAllowed = 0;
                //deltas = new List<double>(toAdd.Frequency);
                //whereToAdd = new List<Node>(toAdd.Frequency);
                //whereToAddDays = new List<int>(toAdd.Frequency);
                //whereToAddTrucks = new List<int>(toAdd.Frequency);
                //foreach (int day in combi)
                //{
                //    int truck = state.Rnd.Next(0, 2);
                //    if (state.DayRoutes[day][truck].EvaluateRandomAdd(toAdd, out double delta1, out Node where1)) // MISS NIET BEIDE TRUCKS PROBEREN
                //    {
                //        deltas.Add(delta1);
                //        whereToAdd.Add(where1);
                //        whereToAddDays.Add(day);
                //        whereToAddTrucks.Add(truck);
                //        everyDayInCombiAllowed++;
                //        continue;
                //    }
                //    else if (state.DayRoutes[day][1 - truck].EvaluateRandomAdd(toAdd, out double delta2, out Node where2))
                //    {
                //        deltas.Add(delta2);
                //        whereToAdd.Add(where2);
                //        whereToAddDays.Add(day);
                //        whereToAddTrucks.Add(truck);
                //        everyDayInCombiAllowed++;
                //    }
                //}
                //if (everyDayInCombiAllowed == toAdd.Frequency)
                //{
                //    deltaTime = deltas.Sum();
                //    deltaPenalty = -(3 * toAdd.Frequency * toAdd.TimeToEmpty);
                //    return true;
                //}
                //else
                //{
                //    deltaTime = double.NaN;
                //    deltaPenalty = double.NaN;
                //    return false;
                //}
                #endregion
            }

            protected override void _Apply() => Operation.Apply();
        }

        public class AddOperation : NeighborOperation
        {
            private readonly Order toAdd;
            private List<double> deltas;
            private List<Node> whereToAdd;
            private List<int> whereToAddDays;
            private List<int> whereToAddTrucks;
            public AddOperation(Schedule s, Order toAdd) : base(s)
            {
                this.toAdd = toAdd;
            }
            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                int[][] combis = GD.AllowedDayCombinations[toAdd.Frequency];
                int[] combi = combis[State.Rnd.Next(0, combis.Length)]; // MISS ALLE COMBIS PROBEREN

                int everyDayInCombiAllowed = 0;
                deltas = new List<double>(toAdd.Frequency);
                whereToAdd = new List<Node>(toAdd.Frequency);
                whereToAddDays = new List<int>(toAdd.Frequency);
                whereToAddTrucks = new List<int>(toAdd.Frequency);
                foreach (int day in combi)
                {
                    int truck = State.Rnd.Next(0, 2);
                    if (State.DayRoutes[day][truck].EvaluateRandomAdd(toAdd, out double delta1, out Node where1)) // MISS NIET BEIDE TRUCKS PROBEREN
                    {
                        deltas.Add(delta1);
                        whereToAdd.Add(where1);
                        whereToAddDays.Add(day);
                        whereToAddTrucks.Add(truck);
                        everyDayInCombiAllowed++;
                        continue;
                    }
                    else if (State.DayRoutes[day][1 - truck].EvaluateRandomAdd(toAdd, out double delta2, out Node where2))
                    {
                        deltas.Add(delta2);
                        whereToAdd.Add(where2);
                        whereToAddDays.Add(day);
                        whereToAddTrucks.Add(truck);
                        everyDayInCombiAllowed++;
                    }
                }
                if (everyDayInCombiAllowed == toAdd.Frequency)
                {
                    deltaTime = deltas.Sum();
                    deltaPenalty = -(3 * toAdd.Frequency * toAdd.TimeToEmpty);
                    return true;
                }
                else
                {
                    deltaTime = double.NaN;
                    deltaPenalty = double.NaN;
                    return false;
                }
            }

            protected override void _Apply()
            {
                for(int i = 0; i < whereToAdd.Count; i++)
                    State.DayRoutes[whereToAddDays[i]][whereToAddTrucks[i]].AddOrder(toAdd, whereToAdd[i]);
            }
        }

        public class RandomDeleteOperation : NeighborOperation
        {
            public Order OrderToRemove { get; private set; }

            Node toRemove;
            int day;
            int truck;
            public RandomDeleteOperation(Schedule s) : base(s)
            {
                //Hé jochie
            }
            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                int d = State.Rnd.Next(0, 5);
                int t = State.Rnd.Next(0, 2);
                if (State.DayRoutes[d][t].EvaluateRandomRemove(out Node rem1, out double delta1))
                {
                    SetToRemove(rem1);
                    day = d;
                    truck = t;
                    deltaTime = delta1;
                    deltaPenalty = 3 * rem1.Data.Frequency * rem1.Data.TimeToEmpty;
                    return true;
                }
                // Uncomment below als niet evalueren ook andere truck:
                else if (State.DayRoutes[d][1 - t].EvaluateRandomRemove(out Node rem2, out double delta2))
                {
                    SetToRemove(rem2);
                    day = d;
                    truck = t;
                    deltaTime = delta2;
                    deltaPenalty = 3 * rem1.Data.Frequency * rem1.Data.TimeToEmpty;
                    return true;
                }
                else
                {
                    deltaTime = double.NaN;
                    deltaPenalty = double.NaN;
                    return false;
                }
            }

            private void SetToRemove(Node r)
            {
                toRemove = r;
                OrderToRemove = r.Data;
            }
            protected override void _Apply()
            {
                State.DayRoutes[day][truck].RemoveNode(toRemove);
            }
        }
        public class RandomTransferOperation : NeighborOperation
        {
            RandomDeleteOperation delOp;
            AddOperation addOp;
            public RandomTransferOperation(Schedule s) : base(s)
            {
                //Hé jochie
                delOp = new RandomDeleteOperation(s);
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;

                //Weet niet of dit goed gaat als Evaluate van del en add maar een mogelijkheid proberen, 
                //miss voor transfer beetje weinig
                if (!delOp.Evaluate()) return false;
                addOp = new AddOperation(State, delOp.OrderToRemove);
                if (!addOp.Evaluate()) return false;

                deltaTime = delOp.DeltaTime + addOp.DeltaTime;
                deltaPenalty = delOp.DeltaPenalty + addOp.DeltaPenalty;

                return true;
            }

            protected override void _Apply()
            {
                delOp.Apply();
                addOp.Apply();
            }
        }
        #endregion

        #region Optimization
        public void OptimizeAllDays()
        {

        }
        public void OptimizeDay(int day, int truck)
        {
            DayRoutes[day][truck].Optimize();
        }
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
                    foreach (Node n in DayRoutes[d][t])
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
                res.AppendLine($"Truck 1: {DayRoutes[i][0]}");
                res.AppendLine($"Truck 2: {DayRoutes[i][1]}");
            }
            return res;
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
        public List<double> roomLefts;

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
            roomLefts = new List<double> { 20000 };
        }

        List<Node> ToList()
        {
            List<Node> nodes = new List<Node>();

            foreach (Node node in this) nodes.Add(node);
            nodes.RemoveAt(nodes.Count - 1);

            return nodes;
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

            roomLefts[n.TourIndex] -= (order.NumContainers * order.VolPerContainer * 0.2);

            if (n.IsDump)
            {
                for (Node curr = n; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex++;

                double newSpaceTaken = 0;
                for (Node curr = n.Next; !curr.IsDump; curr = curr.Next)
                    newSpaceTaken += curr.Data.NumContainers * curr.Data.VolPerContainer * 0.2;

                dumps.Insert(n.TourIndex, n);
                roomLefts.Insert(n.TourIndex, 20000 - newSpaceTaken);
                roomLefts[n.TourIndex - 1] -= newSpaceTaken;
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

            roomLefts[n.TourIndex] += (order.NumContainers * order.VolPerContainer * 0.2);

            if (n.IsDump)
            {
                for (Node curr = n.Next; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex--;

                dumps.RemoveAt(n.TourIndex);
                roomLefts[n.TourIndex - 1] -= 20000 - roomLefts[n.TourIndex];
                roomLefts.RemoveAt(n.TourIndex);
            }

            n.Remove();

            //throw new AfvalOphaler.HeyJochieException("Das nog helemaal niet geimplementeerd jochie!");
            //TimeLeft += Loops[loopIndex].Duration;
            //Loops[loopIndex].RemoveNode(n);
            //TimeLeft -= Loops[loopIndex].Duration;
        }

        #endregion

        #region Evauluate
        public bool EvaluateRandomAdd(Order toAdd, out double deltaTime, out Node whereToAdd)
        {
            deltaTime = double.NaN;
            whereToAdd = null;
            if (toAdd.TimeToEmpty > TimeLeft) return false;

            double totalSpaceOfOrder = toAdd.VolPerContainer * toAdd.NumContainers * 0.2;
            List<int> candidateTours = new List<int>(roomLefts.Count);
            for (int i = 0; i < roomLefts.Count; i++) if (roomLefts[i] >= totalSpaceOfOrder) candidateTours.Add(i);
            List<Node> candidateNodes = new List<Node>();
            foreach (int i in candidateTours) 
                for (Node curr = dumps[i].Next; !curr.IsDump; curr = curr.Next) 
                    if (toAdd.TimeToEmpty
                        + GD.JourneyTime[curr.Data.MatrixId, toAdd.MatrixId] 
                        + GD.JourneyTime[toAdd.MatrixId, curr.Next.Data.MatrixId] 
                        - GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId] 
                        < TimeLeft) 
                        candidateNodes.Add(curr);

            if (candidateNodes.Count > 0)
            {
                Random rnd = new Random();
                whereToAdd = candidateNodes[rnd.Next(0, candidateNodes.Count)];
                deltaTime = toAdd.TimeToEmpty
                    + GD.JourneyTime[whereToAdd.Data.MatrixId, toAdd.MatrixId] 
                    + GD.JourneyTime[toAdd.MatrixId, whereToAdd.Next.Data.MatrixId] 
                    - GD.JourneyTime[whereToAdd.Data.MatrixId, whereToAdd.Next.Data.MatrixId];
                return true;
            }
            
            return false;
        }
        public bool EvaluateRandomRemove(out Node toRemove, out double deltaTime)
        {
            Random rnd = new Random();
            List<Node> candidates = ToList();
            Node theChosenOne = candidates[rnd.Next(0, candidates.Count)];
            double delta = GD.JourneyTime[theChosenOne.Prev.Data.MatrixId, theChosenOne.Next.Data.MatrixId] 
                - (theChosenOne.Data.TimeToEmpty 
                    + GD.JourneyTime[theChosenOne.Prev.Data.MatrixId, theChosenOne.Data.MatrixId] 
                    + GD.JourneyTime[theChosenOne.Data.MatrixId, theChosenOne.Next.Data.MatrixId]);

            if (delta <= TimeLeft)
            {
                deltaTime = delta;
                if (theChosenOne.IsDump)
                {
                    if ((roomLefts[theChosenOne.TourIndex - 1] - (20000 - roomLefts[theChosenOne.TourIndex])) > 0)
                    {
                        toRemove = theChosenOne;
                        return true;
                    }

                    toRemove = null;
                    deltaTime = double.NaN;
                    return false;
                }

                toRemove = theChosenOne;
                return true;
            }

            toRemove = null;
            deltaTime = double.NaN;
            return false;

            throw new AfvalOphaler.HeyJochieException("Das nog helemaal niet geimplementeerd jochie!");
        }
        #endregion

        #region Optimization
        public void Optimize()
        {
            void Two_Opt_Move(Node[] loop, Node i, Node j)
            {
                //Console.WriteLine($"Changing {i} to {i.Next}\nAnd {j} to {j.Next}\nTo: {i}->{j} and\n{i.Next}->{j.Next}");
                Node iplus = i.Next;
                Node jplus = j.Next;
                iplus.Prev = i.Next.Next;
                j.Next = j.Prev;

                Node curr = i.Next.Next;
                Node stop = j;

                while (curr != stop)
                {
                    Node temp = curr.Next;
                    curr.Next = curr.Prev;
                    curr.Prev = temp;
                    curr = curr.Prev;
                }

                i.Next = j;
                j.Prev = i;
                iplus.Next = jplus;
                jplus.Prev = iplus;
                //Console.WriteLine($"i.Next: {i.Next}\ni.Next.Prev: {i.Next.Prev}\nj.Next: {j.Next}\nj.Next.Prev: {j.Next.Prev}");
                //Console.WriteLine("---");
            }
            void Two_Opt_Node_Shift_Move(Node i, Node j)
            {
                // Node i is placed between Node j and Node j.Next
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

            foreach (var tour in Tours)
            {
                foreach (Node curr in tour)
                {

                }
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
