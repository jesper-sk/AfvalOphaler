using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AfvalOphaler
{
    #region Schedule
    public class Schedule
    {
        #region Public Variables
        public double Duration = 0;
        public double Penalty;
        public double Score => Duration + Penalty;

        public List<Order> UnScheduledOrders;

        public DayRoute[][] DayRoutes;     //zo dat dayRoutes[i][j] is de dagroute van dag i voor truck j

        //public Random Rand;
        #endregion

        #region Constructor(s)
        public Schedule(List<Order> orders)
        {
            UnScheduledOrders = orders.ToList();

            foreach (Order o in UnScheduledOrders) Penalty += 3 * o.Frequency * o.TimeToEmpty;

            //UnScheduledOrders.Add(GD.Dump);

            DayRoutes = new DayRoute[5][];
            for (int d = 0; d < 5; d++)
            {
                DayRoutes[d] = new DayRoute[2];
                for (int t = 0; t < 2; t++) DayRoutes[d][t] = new DayRoute(d, t);
            }

            //Rand = new Random();
        }
        #endregion

        #region Score and Penalty Calculation
        public double CalculateScore()
        {
            double dur = CalculateDuration();
            double pen = CalculatePenalty();
            return dur + pen;
        }
        public double CalculateDuration()
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
            new Func<Schedule, NeighborOperation>((s) => new RandomTransferOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomSwapOperation(s))
        };

        public NeighborOperation[] GetOperations(double[] probDist, int nOps)
        {
            NeighborOperation[] res = new NeighborOperation[nOps];
            for(int j = 0; j < nOps; j++)
            {
                double acc = 0;
                double p = StaticRandom.NextDouble();
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
            public abstract override string ToString();
        }

        public class RandomAddOperation : NeighborOperation
        {
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
                int ind = StaticRandom.Next(0, State.UnScheduledOrders.Count);
                //Console.WriteLine($"Try to add {toAdd}");
                Operation = new AddOperation(State, ind);
                bool possible = Operation.Evaluate();
                //Console.WriteLine($"Possible: {possible}");
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

            public override string ToString() => $"RandomAddOperation, Evaluated: {IsEvaluated}";
        }

        public class AddOperation : NeighborOperation
        {
            private readonly int orderIndex;
            private readonly Order toAdd;
            private readonly int nAdditions;
            private List<double> deltas;
            public List<Node> whereToAdd;
            private List<int> whereToAddDays;
            private List<int> whereToAddTrucks;
            public AddOperation(Schedule s, int orderIndex) : base(s)
            {
                this.orderIndex = orderIndex;
                if (State.UnScheduledOrders.Count == 0) toAdd = null;
                else toAdd = State.UnScheduledOrders[orderIndex];
                nAdditions = toAdd?.Frequency ?? -1;
            }
            public AddOperation(Schedule s, Order toAdd) : base(s)
            {
                orderIndex = -1;
                this.toAdd = toAdd;
                nAdditions = 1;
            }
            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                if (toAdd == null) return false;

                List<int[]> combis = GD.AllowedDayCombinations[nAdditions].ToList();
                //int[] combi = combis[State.Rnd.Next(0, combis.Length)]; // MISS ALLE COMBIS PROBEREN
                //Console.WriteLine($"Chosen day combination: {Util.ArrToString(combi)}");

                while (!(combis.Count == 0)) 
                {
                    int[] combi = combis[StaticRandom.Next(0, combis.Count)];
                    int everyDayInCombiAllowed = 0;
                    deltas = new List<double>(nAdditions);
                    whereToAdd = new List<Node>(nAdditions);
                    whereToAddDays = new List<int>(nAdditions);
                    whereToAddTrucks = new List<int>(nAdditions);
                    foreach (int day in combi)
                    {
                        //Console.WriteLine($"Day {day}");
                        int truck = StaticRandom.Next(0, 2);
                        if (State.DayRoutes[day][truck].EvaluateRandomAdd(toAdd, out double delta1, out Node where1)) // MISS NIET BEIDE TRUCKS PROBEREN
                        {
                            //Console.WriteLine($"Truck {truck} Evaluated!");
                            deltas.Add(delta1);
                            whereToAdd.Add(where1);
                            whereToAddDays.Add(day);
                            whereToAddTrucks.Add(truck);
                            everyDayInCombiAllowed++;
                            continue;
                        }
                        else if (State.DayRoutes[day][1 - truck].EvaluateRandomAdd(toAdd, out double delta2, out Node where2))
                        {
                            //Console.WriteLine($"Truck {truck - 1} evaluated!");
                            deltas.Add(delta2);
                            whereToAdd.Add(where2);
                            whereToAddDays.Add(day);
                            whereToAddTrucks.Add(1 - truck);
                            everyDayInCombiAllowed++;
                            continue;
                        }
                        //Console.WriteLine("Didn't evaluate, day impossible");
                    }
                    if (everyDayInCombiAllowed == nAdditions)
                    {
                        deltaTime = deltas.Sum();
                        deltaPenalty = -(3 * nAdditions * toAdd.TimeToEmpty);
                        return true;
                    }
                    combis.Remove(combi);
                }
                return false;
            }

            protected override void _Apply()
            {
                for(int i = 0; i < whereToAdd.Count; i++)
                {
                    DayRoute curr = State.DayRoutes[whereToAddDays[i]][whereToAddTrucks[i]];
                    State.Duration -= curr.Duration;
                    curr.AddOrder(toAdd, whereToAdd[i]);
                    State.Duration += curr.Duration;
                }
                State.Penalty -= 3 * nAdditions * toAdd.TimeToEmpty;
                if (orderIndex != -1) State.UnScheduledOrders.RemoveAt(orderIndex);
                else State.UnScheduledOrders.Remove(toAdd);
            }

            public override string ToString() => $"AddOperation, Evaluated: {IsEvaluated}";
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
                void SetToRemove(Node r)
                {
                    toRemove = r;
                    OrderToRemove = r.Data;
                }

                int d = StaticRandom.Next(0, 5);
                int t = StaticRandom.Next(0, 2);
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
                    truck = 1 - t;
                    deltaTime = delta2;
                    deltaPenalty = 3 * rem2.Data.Frequency * rem2.Data.TimeToEmpty;
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
                //Console.WriteLine($"Deleting {toRemove.Data}");
                DayRoute curr = State.DayRoutes[day][truck];
                //Console.WriteLine($"day{day}, truck{truck}");
                //Console.WriteLine($"Prev duration: {curr.Duration}");
                State.Duration -= curr.Duration;
                curr.RemoveNode(toRemove);
                State.Duration += curr.Duration;
                //Console.WriteLine($"After deletion {curr.Duration}");
                State.UnScheduledOrders.Add(toRemove.Data);
                State.Penalty += 3 * toRemove.Data.TimeToEmpty;
            }

            public override string ToString() => $"RandomDeleteOperation, Evaluated: {IsEvaluated}";
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
                //Console.WriteLine("\nEvaluating transfer operation");
                //Weet niet of dit goed gaat als Evaluate van del en add maar een mogelijkheid proberen, 
                //miss voor transfer beetje weinig
                if (!delOp.Evaluate()) return false;
                //Console.WriteLine($"Removing {delOp.OrderToRemove}");
                addOp = new AddOperation(State, delOp.OrderToRemove);
                if (!addOp.Evaluate()) return false;
                //Console.WriteLine($"Adding next to {addOp.whereToAdd[0]}");

                deltaTime = delOp.DeltaTime + addOp.DeltaTime;
                deltaPenalty = delOp.DeltaPenalty + addOp.DeltaPenalty;

                return true;
            }

            protected override void _Apply()
            {
                delOp.Apply();
                addOp.Apply();
            }

            public override string ToString() => $"RandomTransferOperation, Evaluated: {IsEvaluated}";
        }

        public class RandomSwapOperation : NeighborOperation
        {
            public Node toSwap1;
            public Node toSwap2;
            int d1;
            int d2;
            int t1;
            int t2;

            public RandomSwapOperation(Schedule s) : base(s)
            {

            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                //Console.WriteLine("Evaluating swap:");
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                d1 = StaticRandom.Next(0, 5);
                t1 = StaticRandom.Next(0, 2);
                //Console.WriteLine($"Swap1: day {d1} truck {t1}");
                if (State.DayRoutes[d1][t1].EvaluateSwap1(out toSwap1, out double ss1, out double ts1, out double tl))
                {
                    do d2 = StaticRandom.Next(0, 5); while (d2 == d1);
                    do t2 = StaticRandom.Next(0, 2); while (t2 == t1);
                    //Console.WriteLine($"Evaluated. Swap2: day {d2} truck {t2}");
                    if (State.DayRoutes[d2][t2].EvaluateSwap2(toSwap1, ss1, ts1, tl, out toSwap2, out double dT))
                    {
                        deltaTime = dT;
                        deltaPenalty = 0;
                        //Console.WriteLine("Swap!");
                        return true;
                    }
                }
                //Console.WriteLine("Swapping didn't make sense");
                //Console.ReadKey(true);
                return false;
            }

            protected override void _Apply()
            {
                //Console.WriteLine($"Applying swap between:\n" +
                //    $"toSwap1: {toSwap1}\n" +
                //    $"toSwap2: {toSwap2}\n" +
                //    $"Roomlefts before:\n" +
                //    $"roomleft[d1][t1][swap1] = {State.DayRoutes[d1][t1].roomLefts[toSwap1.TourIndex]}\n" +
                //    $"roomleft[d2][t2][swap2] = {State.DayRoutes[d2][t2].roomLefts[toSwap2.TourIndex]}\n" +
                //    $"Duration before swap: {State.Duration}\n" +
                //    $"Duration d1t1: {State.DayRoutes[d1][t1].Duration}\n" +
                //    $"Duration d2t2: {State.DayRoutes[d2][t2].Duration}");
                State.Duration -= State.DayRoutes[d1][t1].Duration;
                State.Duration -= State.DayRoutes[d2][t2].Duration;
                State.DayRoutes[d1][t1].Swap1(toSwap2, toSwap1);
                State.DayRoutes[d2][t2].Swap2(toSwap1, toSwap2);
                State.Duration += State.DayRoutes[d1][t1].Duration;
                State.Duration += State.DayRoutes[d2][t2].Duration;
                //Console.WriteLine($"---\n" +
                //    $"Roomlefts after:\n" +
                //    $"roomleft[d1][t1][swap1] = {State.DayRoutes[d1][t1].roomLefts[toSwap1.TourIndex]}\n" +
                //    $"roomleft[d2][t2][swap2] = {State.DayRoutes[d2][t2].roomLefts[toSwap2.TourIndex]}\n" +
                //    $"Duration before swap: {State.Duration}\n" +
                //    $"Duration d1t1: {State.DayRoutes[d1][t1].Duration}\n" +
                //    $"Duration d2t2: {State.DayRoutes[d2][t2].Duration}");
                //Console.WriteLine();
            }

            public override string ToString() => $"RandomSwapOperation, Evaluated {IsEvaluated}";
        }
        #endregion

        #region Optimization
        public void OptimizeAllDays()
        {
            for (int d = 0; d < 5; d -= -1)
                for (int t = 0; t < 2; t -= -1)
                    OptimizeDay(d, t);
        }
        public void OptimizeDay(int day, int truck)
        {
            Duration -= DayRoutes[day][truck].Duration;
            DayRoutes[day][truck].Optimize();
            Duration += DayRoutes[day][truck].Duration;
        }
        #endregion

        #region ToStrings
        public ScheduleResult ToResult() => new ScheduleResult()
        {
            Score = Score,
            Stats = GetStatisticsBuilder(),
            Check = ToCheckStringBuilder(),
            String = ToString()
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
                    int lastDump = -3;
                    int ordOfDay = 0;
                    foreach (Node n in DayRoutes[d][t])
                        if(!n.IsDump)
                            b.AppendLine($"{t + 1}; {d + 1}; {++ordOfDay}; {n.Data.OrderId}");
                        else if (ordOfDay != lastDump)
                        {
                            b.AppendLine($"{t + 1}; {d + 1}; {++ordOfDay}; {n.Data.OrderId}");
                            lastDump = ordOfDay;
                        }
                }
            }
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
        public string String = "";
    }
    #endregion

    #region DayRoute
    public class DayRoute : IEnumerable
    {
        #region Variables & Constructor
        public double Duration => 720 - TimeLeft;
        public double TimeLeft;

        public readonly int DayIndex;
        public readonly int TruckIndex;

        public List<Node> dumps;
        public List<double> roomLefts;
        private List<Node> nodes; //Always contains all nodes, in no specific order 

        public TourEnumerableIndexer Tours { get; private set; }

        private bool firstAdd = true;
        private const int lim = 10;

        public DayRoute(int dind, int trind)
        {
            TimeLeft = 720;
            DayIndex = dind;
            TruckIndex = trind;

            Node dump0 = new Node(0);
            Node dumpL = new Node();
            Node dump1 = new Node(1);

            dump0.Next = dump1;

            dump1.Next = dumpL;
            dump1.Prev = dump0;

            dumpL.Prev = dump1;

            dumps = new List<Node> { dump0, dump1 };
            roomLefts = new List<double> { 100000, 100000 };
            nodes = new List<Node> { dump0, dump1 };

            Tours = new TourEnumerableIndexer(this);
        }

        //List<Node> ToList()
        //{
        //    List<Node> nodes = new List<Node>();

        //    foreach (Node node in this) nodes.Add(node);
        //    nodes.RemoveAt(nodes.Count - 1);

        //    return nodes;
        //}

        public class TourEnumerableIndexer
        {
            private DayRoute owner;

            public TourEnumerableIndexer(DayRoute o)
            {
                owner = o;
            }

            public IEnumerable this[int index]
            {
                get => new TourEnumerable(owner.dumps[index]);
            }
        }
        #endregion

        #region Tour Modifications
        public Node AddOrder(Order order, Node nextTo)
        {
            if (firstAdd)
            {
                firstAdd = false;
                TimeLeft -= 60;
            }

            TimeLeft -= (order.TimeToEmpty
                    + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                    + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                    - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

            Node n = nextTo.AppendNext(order);

            roomLefts[n.TourIndex] -= (order.NumContainers * order.VolPerContainer);

            if (n.IsDump)
            {
                for (Node curr = n; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex++;

                double newSpaceTaken = 0;
                for (Node curr = n.Next; !curr.IsDump; curr = curr.Next)
                    newSpaceTaken += curr.Data.NumContainers * curr.Data.VolPerContainer;

                dumps.Insert(n.TourIndex, n);
                roomLefts.Insert(n.TourIndex, 20000 - newSpaceTaken);
                roomLefts[n.TourIndex - 1] -= newSpaceTaken;
            }

            nodes.Add(n);

            return n;
        }
        public void RemoveNode(Node n)
        {
            Order order = n.Data;

            //Console.WriteLine($"Tte: {order.TimeToEmpty}");
            //Console.WriteLine($"From ");

            TimeLeft += (order.TimeToEmpty
                + GD.JourneyTime[n.Prev.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, n.Next.Data.MatrixId]
                - GD.JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId]);

            roomLefts[n.TourIndex] += (order.NumContainers * order.VolPerContainer);

            if (n.IsDump)
            {
                for (Node curr = n.Next; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex--;

                dumps.RemoveAt(n.TourIndex);
                roomLefts[n.TourIndex - 1] -= 20000 - roomLefts[n.TourIndex];
                roomLefts.RemoveAt(n.TourIndex);
            }

            n.Remove();
            nodes.Remove(n);
        }

        #endregion

        #region Evaluate
        public bool EvaluateRandomAdd(Order toAdd, out double deltaTime, out Node whereToAdd)
        {
            //Console.WriteLine($"Evaluating addition of {toAdd.OrderId}");
            deltaTime = double.NaN;
            whereToAdd = null;
            if (toAdd.TimeToEmpty > TimeLeft)
            {
                //Console.WriteLine("Not enough time left, returning false...");
                return false;
            }

            double totalSpaceOfOrder = toAdd.VolPerContainer * toAdd.NumContainers;
            //Console.WriteLine($"Space needed: {totalSpaceOfOrder}");
            List<int> candidateTours = new List<int>(roomLefts.Count);
            for (int i = 0; i < roomLefts.Count; i++)
            {
                if (roomLefts[i] >= totalSpaceOfOrder)
                {
                    candidateTours.Add(i);
                    //Console.WriteLine($"Space: {roomLefts[i]}");
                }
            }
            //Console.WriteLine($"Candidate tourIndices: {Util.ListToString(candidateTours)}");
            List<Node> candidateNodes = new List<Node>();
            foreach (int i in candidateTours)
            {
                //Console.WriteLine($"Tour {i}");
                foreach (Node curr in Tours[i])
                {
                    //Console.WriteLine($"Node {curr.Data.OrderId}");
                    if (TimeLeft >
                            toAdd.TimeToEmpty
                                + GD.JourneyTime[curr.Data.MatrixId, toAdd.MatrixId]
                                + GD.JourneyTime[toAdd.MatrixId, curr.Next.Data.MatrixId]
                                - GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId])
                    {
                        //Console.WriteLine($"found candidate, adding to list");
                        candidateNodes.Add(curr);
                    }
                    else
                    {
                        //Console.WriteLine($"Nog enough time left in day...");
                    }
                }
            }

            if (candidateNodes.Count > 0)
            {             
                Random rnd = new Random();
                whereToAdd = candidateNodes[rnd.Next(0, candidateNodes.Count)];
                while (whereToAdd.Data.OrderId == toAdd.OrderId) whereToAdd = candidateNodes[rnd.Next(0, candidateNodes.Count)];
                deltaTime = toAdd.TimeToEmpty
                    + GD.JourneyTime[whereToAdd.Data.MatrixId, toAdd.MatrixId]
                    + GD.JourneyTime[toAdd.MatrixId, whereToAdd.Next.Data.MatrixId]
                    - GD.JourneyTime[whereToAdd.Data.MatrixId, whereToAdd.Next.Data.MatrixId];
                return true;
                //Console.WriteLine($"Adding next to {whereToAdd}");
                //Console.WriteLine($"Room left: {roomLefts[whereToAdd.TourIndex] - totalSpaceOfOrder}");
            }
            return false;
        }
        public bool EvaluateRandomRemove(out Node toRemove, out double deltaTime)
        {
            toRemove = null;
            deltaTime = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for(int i = 0; i < nodes.Count && i < lim; i++)
            {
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node chosen = nodes[ind];

                if (chosen.Data.Frequency > 1) continue;
                if (chosen.IsDump) continue;

                deltaTime = GD.JourneyTime[chosen.Prev.Data.MatrixId, chosen.Next.Data.MatrixId]
                    - (chosen.Data.TimeToEmpty
                        + GD.JourneyTime[chosen.Prev.Data.MatrixId, chosen.Data.MatrixId]
                        + GD.JourneyTime[chosen.Data.MatrixId, chosen.Next.Data.MatrixId]);

                if (deltaTime > TimeLeft) continue;
                toRemove = chosen;
                break;
            }

            if (toRemove == null) return false;
            return true;

            //if (theChosenOne.IsDump)
            //{
            //    if (!((roomLefts[theChosenOne.TourIndex - 1] - (100000 - roomLefts[theChosenOne.TourIndex])) > 0))
            //    {
            //        return false;
            //    }
            //}
        }

        public bool EvaluateSwap1(out Node toSwapOut, out double space_swapIn, out double time_swapIn, out double time_left)
        {
            toSwapOut = null;
            space_swapIn = double.NaN;
            time_swapIn = double.NaN;
            time_left = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for(int i = 0; i < nodes.Count && i < lim; i++)
            {
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node swapOut = nodes[ind];

                if (swapOut.Data.Frequency > 1 || swapOut.IsDump) continue;

                double deltaTime = swapOut.Data.TimeToEmpty
                    + GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapOut.Data.MatrixId]
                    + GD.JourneyTime[swapOut.Data.MatrixId, swapOut.Next.Data.MatrixId];

                toSwapOut = swapOut;
                time_swapIn = TimeLeft + deltaTime;
                time_left = TimeLeft;
                space_swapIn = roomLefts[toSwapOut.TourIndex] + toSwapOut.Data.VolPerContainer * toSwapOut.Data.NumContainers;
                return true;
                //break;
            }
            //if (toSwapOut == null) return false;
            //return true;
            return false;
        }

        public bool EvaluateSwap2(Node toSwapIn, double space_swapOut, double time_swapOut, double time_leftOut, out Node toSwapOut, out double deltaTime)
        {
            toSwapOut = null;
            deltaTime = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for(int i = 0; i < nodes.Count && i < lim; i++)
            {
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node swapOut = nodes[ind];

                if (swapOut.Data.Frequency > 1 || swapOut.IsDump) continue;

                //Check of chosen past op de plek van toIns
                double reqS_swapOut = swapOut.Data.VolPerContainer * swapOut.Data.NumContainers;

                if (reqS_swapOut > space_swapOut) continue;

                double reqT_swapOut = swapOut.Data.TimeToEmpty
                    + GD.JourneyTime[toSwapIn.Prev.Data.MatrixId, swapOut.Data.MatrixId]
                    + GD.JourneyTime[swapOut.Data.MatrixId, toSwapIn.Next.Data.MatrixId];

                if (reqT_swapOut > time_swapOut) continue;

                //Check of toIns past op de plek van chosen
                double space_swapIn = roomLefts[swapOut.TourIndex] 
                    + swapOut.Data.VolPerContainer * swapOut.Data.NumContainers;

                double reqS_swapIn = toSwapIn.Data.VolPerContainer * toSwapIn.Data.NumContainers;

                if (reqS_swapIn > space_swapIn) continue;

                double time_swapIn = TimeLeft
                    + swapOut.Data.TimeToEmpty
                    + GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapOut.Data.MatrixId]
                    + GD.JourneyTime[swapOut.Data.MatrixId, swapOut.Next.Data.MatrixId];

                double reqT_swapIn = toSwapIn.Data.TimeToEmpty
                    + GD.JourneyTime[swapOut.Prev.Data.MatrixId, toSwapIn.Data.MatrixId]
                    + GD.JourneyTime[toSwapIn.Data.MatrixId, swapOut.Next.Data.MatrixId];

                if (reqT_swapIn > time_swapIn) continue;
                // time_swapin = tijd die het oplevert om swapOut weg te halen + timeleft van dag swapOut
                // reqT_swapin = tijd die het kost om swapIn in te voegen.

                // time_swapout = tijd die het oplevert om swapIn weg te halen + timeleft van dag swapIn
                // reqT_swapout = tijd die het kost om swapOut in de dag van swapIn te zetten
                //Console.WriteLine($"rS_in: {reqS_swapIn}, space_swapIn: {space_swapIn}\n" +
                //    $"rS_out: {reqS_swapOut}, space_swapOut: {space_swapOut}");
                //Console.WriteLine($"rT_in: {reqT_swapIn}, t_in: {time_swapIn}, timeleft: {TimeLeft} \n" +
                //    $"rT_out: {reqT_swapOut}, t_out: {time_swapOut}, timeleftout: {time_leftOut}");
                deltaTime = (reqT_swapIn - (time_swapIn - TimeLeft)) + (reqT_swapOut - (time_swapOut - time_leftOut));
                //Console.WriteLine($"deltaTime: {deltaTime}");
                toSwapOut = swapOut;
                return true;
            }

            //if (toSwapOut == null) return false;
            //return true;
            return false;
        }

        // Total swap operator:
        // a b c && 1 2 3 => a 2 c && 1 b 3

        //  
        // 
        public void Swap1(Node swapIn, Node swapOut)
        {
            TimeLeft = TimeLeft
                + swapOut.Data.TimeToEmpty
                + GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapOut.Data.MatrixId]
                + GD.JourneyTime[swapOut.Data.MatrixId, swapOut.Next.Data.MatrixId]
                - swapIn.Data.TimeToEmpty
                - GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapIn.Data.MatrixId]
                - GD.JourneyTime[swapIn.Data.MatrixId, swapOut.Next.Data.MatrixId];

            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;

            Node temp = swapOut.Next;
            swapOut.Next = swapIn.Next;
            swapIn.Next.Prev = swapOut;
            swapIn.Next = temp;
            temp.Prev = swapIn;
        }

        public void Swap2(Node swapIn, Node swapOut)
        {
            Node temp = swapOut.Prev;
            swapOut.Prev = swapIn.Prev;
            swapIn.Prev.Next = swapOut;
            swapIn.Prev = temp;
            temp.Next = swapIn;

            TimeLeft = TimeLeft
                + swapOut.Data.TimeToEmpty
                + GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapOut.Data.MatrixId]
                + GD.JourneyTime[swapOut.Data.MatrixId, swapIn.Next.Data.MatrixId]
                - swapIn.Data.TimeToEmpty
                - GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapIn.Data.MatrixId]
                - GD.JourneyTime[swapIn.Data.MatrixId, swapIn.Next.Data.MatrixId];

            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;
        }
        #endregion

        #region Optimization
        public void Optimize()
        {
            void Two_Opt_Move(Node i, Node j) // TIME KLOPT NOG NIET
            {
                //Console.WriteLine($"Changing {i} to {i.Next}\nAnd {j} to {j.Next}\nTo: {i}->{j} and\n{i.Next}->{j.Next}");
                // van:     0 1 2 3 4 5 6 7 8 9
                // naar:    0 8 7 6 5 4 3 2 1 9
                // met:     i=0, j=8
                Node iplus = i.Next;        // 1
                Node jplus = j.Next;        // 9
                iplus.Prev = i.Next.Next;  
                j.Next = j.Prev;

                TimeLeft = TimeLeft
                    + GD.JourneyTime[i.Data.MatrixId, iplus.Data.MatrixId]          // 0-1 eraf
                    + GD.JourneyTime[iplus.Data.MatrixId, iplus.Next.Data.MatrixId] // 1-2 eraf
                    + GD.JourneyTime[j.Data.MatrixId, jplus.Data.MatrixId];         // 8-9 eraf

                Node curr = i.Next.Next;    // 2
                Node stop = j;              // 8

                while (curr != stop)
                {
                    Node temp = curr.Next;
                    TimeLeft = TimeLeft
                        + GD.JourneyTime[curr.Data.MatrixId, temp.Data.MatrixId];
                    curr.Next = curr.Prev;
                    TimeLeft = TimeLeft
                        - GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId];
                    curr.Prev = temp;
                    curr = curr.Prev;
                }

                i.Next = j;
                j.Prev = i;
                iplus.Next = jplus;
                jplus.Prev = iplus;
                // van:     0 1 2 3 4 5 6 7 8 9
                // naar:    0 8 7 6 5 4 3 2 1 9
                // met:     i=0, j=8
                TimeLeft = TimeLeft
                    - GD.JourneyTime[i.Data.MatrixId, j.Data.MatrixId]          // 0-8 erbij
                    - GD.JourneyTime[j.Data.MatrixId, j.Next.Data.MatrixId]     // 8-7 erbij
                    - GD.JourneyTime[iplus.Data.MatrixId, jplus.Data.MatrixId]; // 1-9 erbij
                //Console.WriteLine($"i.Next: {i.Next}\ni.Next.Prev: {i.Next.Prev}\nj.Next: {j.Next}\nj.Next.Prev: {j.Next.Prev}");
                //Console.WriteLine("---");
            }
            // IETS MET DURATION EN AFVAL DOEN
            void Two_Opt_Node_Shift_Move(Node i, Node j)
            {
                // i is placed between j and j.next
                Node iprev = i.Prev;
                Node inext = i.Next;

                Node jnext = j.Next;

                iprev.Next = inext;
                inext.Prev = iprev;

                j.Next = i;
                i.Next = jnext;
                i.Prev = j;
                jnext.Prev = i;

                TimeLeft = TimeLeft
                    + GD.JourneyTime[iprev.Data.MatrixId, i.Data.MatrixId]
                    + GD.JourneyTime[i.Data.MatrixId, inext.Data.MatrixId] 
                    + GD.JourneyTime[j.Data.MatrixId, jnext.Data.MatrixId]
                    - GD.JourneyTime[iprev.Data.MatrixId, inext.Data.MatrixId] 
                    - GD.JourneyTime[j.Data.MatrixId, i.Data.MatrixId] 
                    - GD.JourneyTime[i.Data.MatrixId, jnext.Data.MatrixId];
                //Console.WriteLine($"j: {j}\nj.Next: {j.Next}\nj.Next.Next: {j.Next.Next}");
                //Console.WriteLine($"j.Next.Prev: {j.Next.Prev}\nj.Next.Next.Prev: {j.Next.Next.Prev}");
                //Console.WriteLine("---");
            }

            for (int i = 0; i < dumps.Count; i++) 
            {
                for (Node x = dumps[i]; !(x.Next.IsDump || x.Next.Next.IsDump); x = x.Next) // <- last x: (... -> x -> dumps[i+1])
                {
                    Node x1 = x;                    
                    Node x2 = x.Next;
                    for (Node y = x2.Next; !(y.IsDump || y.Next.IsDump); y = y.Next)
                    {
                        Node y1 = y;
                        Node y2 = y.Next;

                        double del_dist = GD.JourneyTime[x1.Data.MatrixId, x2.Data.MatrixId] + GD.JourneyTime[y1.Data.MatrixId, y2.Data.MatrixId];
                        double X1Y1 = GD.JourneyTime[x1.Data.MatrixId, y1.Data.MatrixId];
                        double X2Y2 = GD.JourneyTime[x2.Data.MatrixId, y2.Data.MatrixId];

                        if (del_dist - (X1Y1 + X2Y2) > 0)
                        {
                            //Console.WriteLine("Doing 2-opt move...");
                            Two_Opt_Move(x1, y1);
                            return;
                        }
                        else
                        {
                            double X2Y1 = GD.JourneyTime[x2.Data.MatrixId, y1.Data.MatrixId];
                            Node z1 = x2.Next;
                            if (z1 != y1)
                            {
                                if ((del_dist + GD.JourneyTime[x2.Data.MatrixId, z1.Data.MatrixId]) - (X2Y2 + X2Y1 + GD.JourneyTime[x1.Data.MatrixId, z1.Data.MatrixId]) > 0)
                                {
                                    //Console.WriteLine("Doing first 2.5-opt move...");
                                    Two_Opt_Node_Shift_Move(x2, y1);
                                    return;
                                }
                            }
                            else
                            {
                                z1 = y1.Prev;
                                if (z1 != x2)
                                {
                                    if ((del_dist + GD.JourneyTime[y1.Data.MatrixId, z1.Data.MatrixId]) - (X1Y1 + X2Y1 + GD.JourneyTime[y2.Data.MatrixId, z1.Data.MatrixId]) > 0)
                                    {
                                        //Console.WriteLine("Doing second 2.5-opt move...");
                                        Two_Opt_Node_Shift_Move(y1, x1);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Overrides / Inherited Implementations     
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"TourCount={dumps.Count},\tTimeleft={Math.Round(TimeLeft, 2)}\tRoomLefst=");
            foreach(double left in roomLefts)
            {
                sb.Append($"\t{left}");
            }
            return sb.ToString();
        }
        public IEnumerator GetEnumerator()
        {
            if (dumps.Count == 0) return null;
            return new DayScheduleEnumerator(dumps[0]);
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

    #region [DEPRECATED] Loop
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
    #endregion

    #region Loop Enum
    public class TourEnumerable : IEnumerable
    {
        private Node start;

        public TourEnumerable(Node start)
        {
            this.start = start;
        }

        public IEnumerator GetEnumerator() => new TourEnumerator(start);
    }

    public class TourEnumerator : IEnumerator
    {
        private readonly Node dump;
        private Node next;
        private Node curr;

        private int endIndex;

        object IEnumerator.Current => curr;
        public TourEnumerator(Node dump)
        {
            this.dump = dump;
            next = dump;
            endIndex = dump.TourIndex + 1;
        }
        bool IEnumerator.MoveNext()
        {
            curr = next;
            next = curr.Next;
            return !((curr.IsDump && curr.TourIndex == endIndex) || curr.IsSentry);
        }
        void IEnumerator.Reset()
        {
            next = curr;
            curr = null;
        }
    }

    public class DayScheduleEnumerator : IEnumerator
    {
        private readonly Node dump;
        private Node curr;
        private Node prev;

        object IEnumerator.Current => curr;
        public DayScheduleEnumerator(Node dump)
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
    public class Node : IEquatable<object>
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
        //Voor invoegen van Sentry, alleen bij constructor DayRoute!
        public Node()
        {
            IsDump = true;
            IsSentry = true;
            Data = GD.Dump;
            TourIndex = -1;
            Next = this;
        }
        //Voor invoegen van dump
        public Node(int dumpId)
        {
            IsDump = true;
            IsSentry = false;
            Data = GD.Dump;
            TourIndex = dumpId;
        }
        //Voor invoegen normale order of dump
        public Node(Order o, int tourInd)
        {
            IsDump = o.OrderId == 0;
            Data = o;
            TourIndex = tourInd;
            IsSentry = false;
        }
        #endregion

        #region Modifications
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
            if (!(o is Node n)) return false;
            return Data.OrderId.Equals(n.Data.OrderId) && TourIndex.Equals(n.TourIndex);
        }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => $"{(IsSentry ? "Sentry" : "Node")} ti{TourIndex}: {Data}";
        #endregion
    }
    #endregion
}
