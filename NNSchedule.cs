using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AfvalOphaler.GD;

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
            new Func<Schedule, NeighborOperation>((s) => new RandomSwapOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new AddDumpOperation(s))
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
            public double Evaluation
            {
                get
                {
                    //Hier meer spul
                    return TotalDelta;
                }
            }
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

        public class AddDumpOperation : NeighborOperation
        {
            private AddOperation Operation;

            public AddDumpOperation(Schedule s) : base(s)
            {
                Operation = new AddOperation(s, Dump);
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                if (Operation.Evaluate())
                {
                    deltaTime = Operation.DeltaTime;
                    deltaPenalty = Operation.DeltaPenalty;
                    return true;
                }
                deltaTime = deltaPenalty = double.NaN;
                return false;
            }

            protected override void _Apply()
            {
                Operation.Apply();
            }

            public override string ToString() => $"AddDumpOperation, Evaluated: {IsEvaluated}";
        }

        public class AddOperation : NeighborOperation
        {
            private readonly int[] excludedDayCombi;
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
            public AddOperation(Schedule s, Order toAdd, int[] excluded = null) : base(s)
            {
                orderIndex = -1;
                this.toAdd = toAdd;
                nAdditions = toAdd.Frequency;
                excludedDayCombi = excluded;
            }
            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                if (toAdd == null) return false;

                List<int[]> combis = AllowedDayCombinations[nAdditions].ToList();
                //int[] combi = combis[State.Rnd.Next(0, combis.Length)]; // MISS ALLE COMBIS PROBEREN
                //Console.WriteLine($"Chosen day combination: {Util.ArrToString(combi)}");

                while (!(combis.Count == 0)) 
                {
                    int[] combi = combis[StaticRandom.Next(0, combis.Count)];
                    if (excludedDayCombi != null && combi == excludedDayCombi) goto next;
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
                    next:
                    combis.Remove(combi);
                }
                return false;
            }

            protected override void _Apply()
            {
                Node[] all = new Node[whereToAdd.Count];
                for(int i = 0; i < whereToAdd.Count; i++)
                {
                    DayRoute curr = State.DayRoutes[whereToAddDays[i]][whereToAddTrucks[i]];
                    State.Duration -= curr.Duration;
                    all[i] = curr.AddOrder(toAdd, whereToAdd[i]);
                    State.Duration += curr.Duration;
                }
                for(int i = 0; i < all.Length; i++)
                {
                    Node[] others = new Node[all.Length - 1];
                    int j = 0;
                    for(; j < i; j++)
                    {
                        others[j] = all[j];
                    }
                    for(; j < others.Length; j++)
                    {
                        others[j] = all[j + 1];
                    }
                    all[i].SetOthers(others);
                }
                if (toAdd.OrderId != 0) State.Penalty -= 3 * nAdditions * toAdd.TimeToEmpty;
                if (orderIndex != -1) State.UnScheduledOrders.RemoveAt(orderIndex);
                else State.UnScheduledOrders.Remove(toAdd);
            }

            public override string ToString() => $"AddOperation, Evaluated: {IsEvaluated}";
        }

        public class RandomDeleteOperation : NeighborOperation
        {
            public Order OrderToRemove { get; private set; }
            public int[] ExcludedDayCombi { get; private set; }

            //Node toRemove;
            //int day;
            //int truck;

            Node[] toRemove;
            int[] days;
            int[] trucks;

            public RandomDeleteOperation(Schedule s) : base(s)
            {
                //Hé jochie
            }
            //protected bool old_Evaluate(out double deltaTime, out double deltaPenalty)
            //{
            //    void SetToRemove(Node r)
            //    {
            //        toRemove = r;
            //        OrderToRemove = r.Data;
            //    }

            //    int d = StaticRandom.Next(0, 5);
            //    int t = StaticRandom.Next(0, 2);
            //    if (State.DayRoutes[d][t].EvaluateRandomDelete(out Node rem1, out double delta1))
            //    {
            //        SetToRemove(rem1);
            //        day = d;
            //        truck = t;
            //        deltaTime = delta1;
            //        deltaPenalty = 3 * rem1.Data.Frequency * rem1.Data.TimeToEmpty;
            //        return true;
            //    }
            //    // Uncomment below als niet evalueren ook andere truck:
            //    else if (State.DayRoutes[d][1 - t].EvaluateRandomDelete(out Node rem2, out double delta2))
            //    {
            //        SetToRemove(rem2);
            //        day = d;
            //        truck = 1 - t;
            //        deltaTime = delta2;
            //        deltaPenalty = 3 * rem2.Data.Frequency * rem2.Data.TimeToEmpty;
            //        return true;
            //    }
            //    else
            //    {
            //        deltaTime = double.NaN;
            //        deltaPenalty = double.NaN;
            //        return false;
            //    }
            //}

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                int d = StaticRandom.Next(0, 5);
                int ts = StaticRandom.Next(0, 2);
                for(int t = ts; t != (1-ts); t = 1 - t)
                {
                    if (State.DayRoutes[d][t].EvaluateRandomDeleteOrder(out toRemove, out trucks, out days, out double nodeDeltaTime))
                    {
                        deltaPenalty = 3 * toRemove[0].Data.Frequency * toRemove[0].Data.TimeToEmpty;
                        double[] dts = new double[days.Length];
                        dts[0] = nodeDeltaTime;
                        for(int i = 1; i < trucks.Length; i++)
                        {
                            if (!State.DayRoutes[days[i]][trucks[i]].EvaluateRemoveNode(toRemove[i], out dts[i])) 
                                goto next;
                        }
                        deltaTime = dts.Sum() + nodeDeltaTime;
                        OrderToRemove = toRemove[0].Data;
                        ExcludedDayCombi = days;
                        return true;
                    }
                    next:;
                }
                deltaTime = deltaPenalty = double.NaN;
                return false;
            }

            protected override void _Apply()
            {

                for(int i = 0; i < toRemove[0].Data.Frequency; i++)
                {
                    DayRoute curr = State.DayRoutes[days[i]][trucks[i]];
                    State.Duration -= curr.Duration;
                    curr.RemoveNode(toRemove[i]);
                    State.Duration += curr.Duration;
                }
                if (!toRemove[0].IsDump)
                {
                    State.UnScheduledOrders.Add(toRemove[0].Data);
                    State.Penalty += 3 * toRemove[0].Data.Frequency * toRemove[0].Data.TimeToEmpty;
                }
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
                addOp = new AddOperation(State, delOp.OrderToRemove, delOp.ExcludedDayCombi);
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

            double dt1;
            double dt2;

            Node[] swaps1;
            int[] days1;
            int[] trucks1;

            Node[] swaps2;
            int[] days2;
            int[] trucks2;

            double[] dts1;
            double[] dts2;

            //private StringBuilder log;

            public RandomSwapOperation(Schedule s) : base(s)
            {
                //log = new StringBuilder();
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                //Console.WriteLine("Evaluating swap:");
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                d1 = StaticRandom.Next(0, 5);
                t1 = StaticRandom.Next(0, 2);
                //Console.WriteLine($"Swap1: day {d1} truck {t1}");
                if (State.DayRoutes[d1][t1].EvaluateSwap1(out toSwap1, out double roomLeft_d1, out double freeTime_d1, out double timeLeft_d1))
                {
                    if (toSwap1.Data.Frequency == 1)
                    {
                        do d2 = StaticRandom.Next(0, 5); while (d2 == d1);
                        do t2 = StaticRandom.Next(0, 2); while (t2 == t1);
                    }
                    else
                    {
                        d2 = d1;
                        t2 = t1;
                    }

                    //Console.WriteLine($"Evaluated. Swap2: day {d2} truck {t2}");
                    if (State.DayRoutes[d2][t2].EvaluateSwap2(toSwap1, roomLeft_d1, freeTime_d1, timeLeft_d1, out toSwap2, /*log,*/ out dt1, out dt2))
                    {
                        deltaTime = dt1 + dt2;
                        deltaPenalty = 0;
                        //Console.WriteLine("Swap!");
                        return true;
                    }
                }
                //Console.WriteLine("Swapping didn't make sense");
                //Console.ReadKey(true);
                return false;
            }

            protected bool n_Evaluate(out double deltaTime, out double deltaPenalty)
            {
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                d1 = StaticRandom.Next(0, 5);
                t1 = StaticRandom.Next(0, 2);

                if (State.DayRoutes[d1][t1].EvaluateSwapRandomOrder(out swaps1, out days1, out trucks1))
                {
                    int freq = swaps1.Length;
                    double[] freeSpaces_1 = new double[freq];
                    double[] freeTimes_1 = new double[freq];
                    double[] timeLefts_1 = new double[freq];
                    for(int i = 0; i < freq; i++)
                    {
                        if (!State.DayRoutes[days1[i]][trucks1[i]].EvaluateSwapNode1(swaps1[i], out freeSpaces_1[i], out freeTimes_1[i], out timeLefts_1[i]))
                            return false;
                    }
                    trucks2 = new int[freq];
                    dts1 = new double[freq];
                    dts2 = new double[freq];
                    swaps2 = new Node[freq];
                    List<int[]> combis = new List<int[]>(AllowedDayCombinations[freq]);
                    combis.Remove(new List<int>(days1).OrderBy(o => o).ToArray());
                    while(combis.Count != 0)
                    {
                        int ind = StaticRandom.Next(0, combis.Count);
                        days2 = combis[ind];
                        for (int i = 0; i < freq; i++)
                        {
                            trucks2[i] = StaticRandom.Next(0, 2);
                            if (!State.DayRoutes[days2[i]][trucks2[i]].EvaluateSwapNode2(swaps1[i], freeSpaces_1[i], freeTimes_1[i], timeLefts_1[i], out swaps2[i], out dts1[i], out dts2[i]))
                                if (!State.DayRoutes[days2[i]][trucks2[i] = 1 - trucks2[i]].EvaluateSwapNode2(swaps1[i], freeSpaces_1[i], freeTimes_1[i], timeLefts_1[i], out swaps2[i], out dts1[i], out dts2[i]))
                                    goto next;
                        }
                        deltaTime = dts1.Sum() + dts2.Sum();
                        deltaPenalty = 0;
                        return true;
                    next:
                        combis.RemoveAt(ind);
                    }
                }
                return false;
            }

            protected void n_Apply()
            {
                for(int i = 0; i < swaps1.Length; i++)
                {
                    int d1 = days1[i];
                    int d2 = days2[i];
                    int t1 = trucks1[i];
                    int t2 = trucks2[i];
                    Node toSwap1 = swaps1[i];
                    Node toSwap2 = swaps2[i];

                    State.DayRoutes[d1][t1].Swap1(toSwap2, toSwap1, /*log,*/ dt1);
                    State.DayRoutes[d2][t2].Swap2(toSwap1, toSwap2, /*log,*/ dt2);
                    int tempTourIndex = toSwap1.TourIndex;
                    toSwap1.TourIndex = toSwap2.TourIndex;
                    toSwap2.TourIndex = tempTourIndex;
                    int tempTruckIndex = toSwap1.TruckIndex;
                    toSwap1.TruckIndex = toSwap2.TruckIndex;
                    toSwap2.TruckIndex = tempTruckIndex;
                    int tempDayIndex = toSwap1.DayIndex;
                    toSwap1.DayIndex = toSwap2.DayIndex;
                    toSwap2.DayIndex = tempDayIndex;
                }

                State.Duration += DeltaTime;
            }

            protected override void _Apply()
            {
                //log.AppendLine($"Applying swap:\n" +
                //    $"Before Swap:\n" +
                //    $"toSwap1: {toSwap1}\n" +
                //    $"toSwap2: {toSwap2}\n" +
                //    $"Roomlefts before:\n" +
                //    $"roomleft[d1][t1][swap1] = {State.DayRoutes[d1][t1].roomLefts[toSwap1.TourIndex]}\n" +
                //    $"roomleft[d2][t2][swap2] = {State.DayRoutes[d2][t2].roomLefts[toSwap2.TourIndex]}\n" +
                //    $"Duration before swap: {State.Duration}\n" +
                //    $"Duration d1t1: {State.DayRoutes[d1][t1].Duration}\n" +
                //    $"Duration d2t2: {State.DayRoutes[d2][t2].Duration}");

                //State.Duration -= State.DayRoutes[d1][t1].Duration;
                //State.Duration -= State.DayRoutes[d2][t2].Duration;
                State.Duration += DeltaTime;

                State.DayRoutes[d1][t1].Swap1(toSwap2, toSwap1, /*log,*/ dt1);
                State.DayRoutes[d2][t2].Swap2(toSwap1, toSwap2, /*log,*/ dt2);
                int tempTourIndex = toSwap1.TourIndex;
                toSwap1.TourIndex = toSwap2.TourIndex;
                toSwap2.TourIndex = tempTourIndex;
                int tempTruckIndex = toSwap1.TruckIndex;
                toSwap1.TruckIndex = toSwap2.TruckIndex;
                toSwap2.TruckIndex = tempTruckIndex;
                int tempDayIndex = toSwap1.DayIndex;
                toSwap1.DayIndex = toSwap2.DayIndex;
                toSwap2.DayIndex = tempDayIndex;


                //State.Duration += State.DayRoutes[d1][t1].Duration;
                //State.Duration += State.DayRoutes[d2][t2].Duration;

                //log.AppendLine($"---\nAfter swap:\n" +
                //    $"Roomlefts after:\n" +
                //    $"roomleft[d1][t1][swap1] = {State.DayRoutes[d1][t1].roomLefts[toSwap1.TourIndex]}\n" +
                //    $"roomleft[d2][t2][swap2] = {State.DayRoutes[d2][t2].roomLefts[toSwap2.TourIndex]}\n" +
                //    $"Duration after swap: {State.Duration}\n" +
                //    $"Duration d1t1: {State.DayRoutes[d1][t1].Duration}\n" +
                //    $"Duration d2t2: {State.DayRoutes[d2][t2].Duration}");

                //if (State.DayRoutes[d1][t1].TimeLeft < 0 || State.DayRoutes[d2][t2].TimeLeft < 0 || State.DayRoutes[d1][t1].TimeLeft > 720 || State.DayRoutes[d2][t2].TimeLeft > 720)
                //{
                //    Console.WriteLine("JOCHIE GAAT NIET GOED!");
                //    Console.WriteLine("Stats in evaluate:");
                //    Console.WriteLine(log.ToString());
                //    Console.WriteLine($"TimeLeft d1t1: {State.DayRoutes[d1][t1].TimeLeft}\n" +
                //        $"TimeLeft d2t2: {State.DayRoutes[d2][t2].TimeLeft}");
                //    Console.WriteLine("=====");
                //    Console.ReadKey(true);
                //}
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
        public List<Node> nodes; //Always contains all nodes, in no specific order 

        public TourEnumerableIndexer Tours { get; private set; }

        private bool firstAdd = true;

        public DayRoute(int dind, int trind)
        {
            TimeLeft = 720;
            DayIndex = dind;
            TruckIndex = trind;

            Node dump0 = new Node(DayIndex, TruckIndex, 0);
            Node dump1 = new Node(DayIndex, TruckIndex, 1);
            Node dumpL = new Node(DayIndex, TruckIndex);

            dump0.Next = dump1;

            dump1.Next = dumpL;
            dump1.Prev = dump0;

            dumpL.Prev = dump1;

            dumps = new List<Node> { dump0, dump1 };
            nodes = new List<Node> { dump0, dump1 };
            roomLefts = new List<double> { 100000, 100000 };

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
                                + JourneyTime[curr.Data.MatrixId, toAdd.MatrixId]
                                + JourneyTime[toAdd.MatrixId, curr.Next.Data.MatrixId]
                                - JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId])
                    {
                        //Console.WriteLine($"found candidate, adding to list");
                        if (curr.Data.OrderId != toAdd.OrderId) candidateNodes.Add(curr);
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
                    + JourneyTime[whereToAdd.Data.MatrixId, toAdd.MatrixId]
                    + JourneyTime[toAdd.MatrixId, whereToAdd.Next.Data.MatrixId]
                    - JourneyTime[whereToAdd.Data.MatrixId, whereToAdd.Next.Data.MatrixId];
                return true;
                //Console.WriteLine($"Adding next to {whereToAdd}");
                //Console.WriteLine($"Room left: {roomLefts[whereToAdd.TourIndex] - totalSpaceOfOrder}");
            }
            return false;
        }

        public bool EvaluateRandomDelete(out Node toRemove, out double deltaTime)
        {
            toRemove = null;
            deltaTime = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node chosen = nodes[ind];

                if (chosen.IsDump) continue;

                if (chosen.Data.Frequency > 1) continue;


                deltaTime = JourneyTime[chosen.Prev.Data.MatrixId, chosen.Next.Data.MatrixId]
                    - (chosen.Data.TimeToEmpty
                        + JourneyTime[chosen.Prev.Data.MatrixId, chosen.Data.MatrixId]
                        + JourneyTime[chosen.Data.MatrixId, chosen.Next.Data.MatrixId]);

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

        public bool EvaluateRandomDeleteOrder(out Node[] toRemove, out int[] trucks, out int[] days, out double nodeDeltaTime)
        {
            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node chosen = nodes[ind];

                if (chosen.IsDump) continue;

                if (!EvaluateRemoveNode(chosen, out nodeDeltaTime)) continue;

                toRemove = new Node[chosen.Data.Frequency];
                trucks = new int[toRemove.Length];
                days = new int[trucks.Length];
                toRemove[0] = chosen;
                trucks[0] = TruckIndex;
                days[0] = DayIndex;
                for(int j = 1; j < chosen.Data.Frequency; j++)
                {
                    toRemove[j] = chosen.Others[j - 1];
                    trucks[j] = chosen.Others[j - 1].TruckIndex;
                    days[j] = chosen.Others[j - 1].DayIndex;
                }

                return true;
            }
            toRemove = null; trucks = null; days = null; nodeDeltaTime = double.NaN;
            return false;
        }

        public bool EvaluateRemoveNode(Node toRem, out double deltaTime)
        {
            deltaTime = double.NaN;

            if (toRem.IsDump)
            {
                if (toRem.TourIndex == 0) return false;
                double spaceLeft = 100000 - (200000 - roomLefts[toRem.TourIndex] - roomLefts[toRem.TourIndex - 1]);
                if (spaceLeft < 0) return false;
            }

            deltaTime = JourneyTime[toRem.Prev.Data.MatrixId, toRem.Next.Data.MatrixId]
            - (toRem.Data.TimeToEmpty
                + JourneyTime[toRem.Prev.Data.MatrixId, toRem.Data.MatrixId]
                + JourneyTime[toRem.Data.MatrixId, toRem.Next.Data.MatrixId]);

            if (deltaTime > TimeLeft) return false;

            return true;
        }

        public bool EvaluateSwapRandomOrder(out Node[] toSwapOut, out int[] currDays, out int[] currTrucks)
        {
            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                // Pak random node
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap1 = nodes[index];

                //TODO HIER ALLE A PRIORI CHECKS
                if (swap1.IsDump) continue;
                if (swap1.Data.Frequency == 3 || swap1.Data.Frequency == 5) continue;   //Only one possible day combination, impossible to swap

                int freq = swap1.Data.Frequency;
                toSwapOut = new Node[freq];
                currDays = new int[freq];
                currTrucks = new int[freq];
                toSwapOut[0] = swap1;
                currDays[0] = swap1.DayIndex;
                currTrucks[0] = swap1.TruckIndex;
                for (int j = 0; j < freq - 1; j++)
                {
                    toSwapOut[j + 1] = swap1.Others[j];
                    currDays[j + 1] = swap1.Others[j].DayIndex;
                    currTrucks[j + 1] = swap1.Others[j].TruckIndex;
                }
              
                return true;
            }

            toSwapOut = null; currDays = null; currTrucks = null; 
            return false;
        }

        public bool EvaluateSwapNode1(Node toSwapOut, out double freedUpSpace, out double freedUpTime, out double timeLeft)
        {
            if (toSwapOut.IsDump) { freedUpSpace = freedUpTime = timeLeft = double.NaN; return false; }

            freedUpTime = 0
                + TimeLeft
                + toSwapOut.Data.TimeToEmpty
                + JourneyTime[toSwapOut.Prev.Data.MatrixId, toSwapOut.Data.MatrixId]
                + JourneyTime[toSwapOut.Data.MatrixId, toSwapOut.Next.Data.MatrixId];

            freedUpSpace = roomLefts[toSwapOut.TourIndex]
                + toSwapOut.Data.VolPerContainer * toSwapOut.Data.NumContainers;

            timeLeft = TimeLeft;

            return true;
        }

        public bool EvaluateSwapNode2(Node swap1, double vrije_ruimte_in_dag1, double vrije_tijd_in_dag1_met_timeleft_dag1, double timeleft_dag1, out Node toSwapOut, out double dt1, out double dt2)
        {
            //PAK NIET EEN MET HETZELFDE ORDERID
            toSwapOut = null;
            dt1 = dt2 = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                // Kies random node
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap2 = nodes[index];

                if (swap2.IsDump || swap2.Data.Frequency > 1) continue; //Also guarantees no swap with same order id (if f == 1, the swap1 order is planned in another day anyways)

                //
                //Check of swap2 past op de plek van swap1
                //

                // Bereken de benodigde vrije ruimte voor swap2
                double benodigd_ruimte_swap2 = swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Stop als er niet genoeg spaceLeft in dag1 is
                if (benodigd_ruimte_swap2 > vrije_ruimte_in_dag1) continue;

                // Bereken benodigde tijd in dag1 als swap2 daar gezet wordt
                double benodigde_tijd_swap2 = 0
                    + swap2.Data.TimeToEmpty
                    + JourneyTime[swap1.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + JourneyTime[swap2.Data.MatrixId, swap1.Next.Data.MatrixId];

                // Stop als er niet genoeg tijd over is in dag1 voor swap2
                if (benodigde_tijd_swap2 > vrije_tijd_in_dag1_met_timeleft_dag1) continue;

                //
                //Check of swap1 past op de plek van swap2
                //

                // Bereken de vrije ruimte op dag2
                double vrije_ruimte_dag2 = roomLefts[swap2.TourIndex]
                    + swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Bereken de benodigde ruimte voor swap1
                double benodigde_ruimte_swap1 = swap1.Data.VolPerContainer * swap1.Data.NumContainers;

                // Stop als er niet genoeg vrije ruimte in dag2 is voor swap1
                if (benodigde_ruimte_swap1 > vrije_ruimte_dag2) continue;

                // Bereken de vrije tijd in dag
                double vrije_tijd_in_dag2_met_timeleft_dag2 = 0
                    + TimeLeft
                    + swap2.Data.TimeToEmpty
                    + JourneyTime[swap2.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + JourneyTime[swap2.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Bereken de benodigde tijd voor swap1
                double benodigde_tijd_swap1 = 0
                    + swap1.Data.TimeToEmpty
                    + JourneyTime[swap2.Prev.Data.MatrixId, swap1.Data.MatrixId]
                    + JourneyTime[swap1.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Stop als er niet genoeg vrije tijd is in dag2
                if (benodigde_tijd_swap1 > vrije_tijd_in_dag2_met_timeleft_dag2) continue;

                dt1 = benodigde_tijd_swap2 - (vrije_tijd_in_dag1_met_timeleft_dag1 - timeleft_dag1);
                dt2 = benodigde_tijd_swap1 - (vrije_tijd_in_dag2_met_timeleft_dag2 - TimeLeft);

                toSwapOut = swap2;
                return true;
            }
            return false;
        }

        public bool EvaluateSwap1(out Node toSwapOut, out double vrije_ruimte_als_swap1_weg_is, out double tijd_die_vrijkomt_als_swap1_wordt_verwijderd, out double time_left)
        {
            toSwapOut = null;
            vrije_ruimte_als_swap1_weg_is = double.NaN;
            tijd_die_vrijkomt_als_swap1_wordt_verwijderd = double.NaN;
            time_left = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                // Pak random node
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap1 = nodes[index];

                // Stop als swap1 een dump is of freq > 1
                if (swap1.IsDump) continue;
                if (swap1.Data.Frequency > 1) continue;

                // Bereken de tijd die vrijkomt als we swap1 verwijderen
                tijd_die_vrijkomt_als_swap1_wordt_verwijderd = 0
                    + TimeLeft
                    + swap1.Data.TimeToEmpty
                    + JourneyTime[swap1.Prev.Data.MatrixId, swap1.Data.MatrixId]
                    + JourneyTime[swap1.Data.MatrixId, swap1.Next.Data.MatrixId];

                // Bereken de vrije ruimte als swap1 weg is
                vrije_ruimte_als_swap1_weg_is =
                    roomLefts[swap1.TourIndex]
                    + swap1.Data.VolPerContainer * swap1.Data.NumContainers;

                time_left = TimeLeft;
                toSwapOut = swap1;
                return true;
                //break;
            }
            //if (toSwapOut == null) return false;
            //return true;
            return false;
        }
        public bool EvaluateSwap2(Node swap1, double vrije_ruimte_in_dag1, double vrije_tijd_in_dag1_met_timeleft_dag1, double timeleft_dag1, out Node toSwapOut, out double dt1, out double dt2)
        {
            toSwapOut = null;
            dt1 = dt2 = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < ResearchLimit; i++)
            {
                // Kies random node
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap2 = nodes[index];

                //if (swap1.Data.OrderId == swap2.Data.OrderId) continue;
                // Stop als swap2 dump is of freq > 1
                if (swap2.IsDump) continue;
                if (swap2.Data.Frequency > 1) continue;

                //
                //Check of swap2 past op de plek van swap1
                //

                // Bereken de benodigde vrije ruimte voor swap2
                double benodigd_ruimte_swap2 = swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Stop als er niet genoeg spaceLeft in dag1 is
                if (benodigd_ruimte_swap2 > vrije_ruimte_in_dag1) continue;

                // Bereken benodigde tijd in dag1 als swap2 daar gezet wordt
                double benodigde_tijd_swap2 = 0
                    + swap2.Data.TimeToEmpty
                    + JourneyTime[swap1.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + JourneyTime[swap2.Data.MatrixId, swap1.Next.Data.MatrixId];

                // Stop als er niet genoeg tijd over is in dag1 voor swap2
                if (benodigde_tijd_swap2 > vrije_tijd_in_dag1_met_timeleft_dag1) continue;

                //
                //Check of swap1 past op de plek van swap2
                //

                // Bereken de vrije ruimte op dag2
                double vrije_ruimte_dag2 = roomLefts[swap2.TourIndex]
                    + swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Bereken de benodigde ruimte voor swap1
                double benodigde_ruimte_swap1 = swap1.Data.VolPerContainer * swap1.Data.NumContainers;

                // Stop als er niet genoeg vrije ruimte in dag2 is voor swap1
                if (benodigde_ruimte_swap1 > vrije_ruimte_dag2) continue;

                // Bereken de vrije tijd in dag
                double vrije_tijd_in_dag2_met_timeleft_dag2 = 0
                    + TimeLeft
                    + swap2.Data.TimeToEmpty
                    + JourneyTime[swap2.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + JourneyTime[swap2.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Bereken de benodigde tijd voor swap1
                double benodigde_tijd_swap1 = 0
                    + swap1.Data.TimeToEmpty
                    + JourneyTime[swap2.Prev.Data.MatrixId, swap1.Data.MatrixId]
                    + JourneyTime[swap1.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Stop als er niet genoeg vrije tijd is in dag2
                if (benodigde_tijd_swap1 > vrije_tijd_in_dag2_met_timeleft_dag2) continue;

                //log.AppendLine($"rS_in: {benodigde_ruimte_swap1}, space_swapIn: {vrije_ruimte_dag2}\n" +
                //    $"rS_out: {benodigd_ruimte_swap2}, space_swapOut: {vrije_ruimte_in_dag1}");
                //log.AppendLine($"rT_in: {benodigde_tijd_swap1}, t_in: {vrije_tijd_in_dag2_met_timeleft_dag2}, timeleft: {TimeLeft}, tnew_in: {vrije_tijd_in_dag2_met_timeleft_dag2 - TimeLeft} \n" +
                //    $"rT_out: {benodigde_tijd_swap2}, t_out: {vrije_tijd_in_dag1_met_timeleft_dag1}, timeleftout: {timeleft_dag1}, tnew_out: {vrije_tijd_in_dag1_met_timeleft_dag1 - timeleft_dag1}");

                dt1 = benodigde_tijd_swap2 - (vrije_tijd_in_dag1_met_timeleft_dag1 - timeleft_dag1);
                dt2 = benodigde_tijd_swap1 - (vrije_tijd_in_dag2_met_timeleft_dag2 - TimeLeft);

                //log.AppendLine($"Delta d1: {dt1}");
                //log.AppendLine($"Delta d2: {dt2}");

                //log.AppendLine($"deltaTime: {dt1 + dt2}");

                // dt1: Verschil in TimeLeft voor d1t1, pos iff kost tijd om swap1 weg te halen uit zijn dag en swap2 daar te zetten
                // dt2: Verschil in TimeLeft voor d2t2, pos iff kost tijd om swap2 hier weg te halen en swap1 hier te zetten.

                toSwapOut = swap2;
                return true;
            }

            //if (toSwapOut == null) return false;
            //return true;
            return false;
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
                    + JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                    + JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                    - JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

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
                roomLefts.Insert(n.TourIndex, 100000 - newSpaceTaken);
                roomLefts[n.TourIndex - 1] -= newSpaceTaken;
            }

            nodes.Add(n);

            //if (n.IsDump)
            //{
            //    Console.WriteLine($"\nAdding dump {n.ID}, no. dumps now {dumps.Count}, ncnow {nodes.Count}");
            //}

            return n;
        }
        public void RemoveNode(Node n)
        {
            Order order = n.Data;

            //Console.WriteLine($"Tte: {order.TimeToEmpty}");
            //Console.WriteLine($"From ");

            TimeLeft += (order.TimeToEmpty
                + JourneyTime[n.Prev.Data.MatrixId, order.MatrixId]
                + JourneyTime[order.MatrixId, n.Next.Data.MatrixId]
                - JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId]);

            roomLefts[n.TourIndex] += (order.NumContainers * order.VolPerContainer);

            if (n.IsDump)
            {
                for (Node curr = n.Next; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex--;

                dumps.RemoveAt(n.TourIndex);
                roomLefts[n.TourIndex - 1] -= 100000 - roomLefts[n.TourIndex];
                roomLefts.RemoveAt(n.TourIndex);
            }
            //if (n.IsDump)
            //{
            //    Console.Write($"\nRemoving dump {n.ID}, no. dumps now {dumps.Count}, ncbefore {nodes.Count}");
            //}
            n.Remove();
            if (!nodes.Remove(n)) throw new HeyJochieException();
            //if (n.IsDump)
            //{
            //    Console.WriteLine($" ncnow {nodes.Count}\n{Util.IListToString(nodes)}");
            //}

        }

        // Total swap operator:
        // a b c && 1 2 3 => a 2 c && 1 b 3

        //  
        // 
        public void Swap1(Node swapIn, Node swapOut, /*StringBuilder log,*/ double dt)
        {
            double deltaTime = dt;
            //double deltaTime = swapOut.Data.TimeToEmpty
            //    + GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapOut.Data.MatrixId]
            //    + GD.JourneyTime[swapOut.Data.MatrixId, swapOut.Next.Data.MatrixId]
            //    - swapIn.Data.TimeToEmpty
            //    - GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapIn.Data.MatrixId]
            //    - GD.JourneyTime[swapIn.Data.MatrixId, swapOut.Next.Data.MatrixId];
            //log.AppendLine($"Swap1:\nTimeLeftBefore: {TimeLeft}\nDeltaTime: {deltaTime}");
            TimeLeft -= deltaTime;
            //log.AppendLine($"TimeLeftAfter: {TimeLeft}");
            //+ swapOut.Data.TimeToEmpty
            //+ GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapOut.Data.MatrixId]
            //+ GD.JourneyTime[swapOut.Data.MatrixId, swapOut.Next.Data.MatrixId]
            //- swapIn.Data.TimeToEmpty
            //- GD.JourneyTime[swapOut.Prev.Data.MatrixId, swapIn.Data.MatrixId]
            //- GD.JourneyTime[swapIn.Data.MatrixId, swapOut.Next.Data.MatrixId];

            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;

            Node temp = swapOut.Next;
            swapOut.Next = swapIn.Next;
            swapIn.Next.Prev = swapOut;
            swapIn.Next = temp;
            temp.Prev = swapIn;

            nodes.Remove(swapOut);
            nodes.Add(swapIn);
        }
        public void Swap2(Node swapIn, Node swapOut, /*StringBuilder log,*/ double dt)
        {
            Node temp = swapOut.Prev;
            swapOut.Prev = swapIn.Prev;
            swapIn.Prev.Next = swapOut;
            swapIn.Prev = temp;
            temp.Next = swapIn;
            double deltaTime = dt;
            //double deltaTime = 0
            //    - swapOut.Data.TimeToEmpty
            //    - GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapOut.Data.MatrixId]
            //    - GD.JourneyTime[swapOut.Data.MatrixId, swapIn.Next.Data.MatrixId]
            //    + swapIn.Data.TimeToEmpty
            //    + GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapIn.Data.MatrixId]
            //    + GD.JourneyTime[swapIn.Data.MatrixId, swapIn.Next.Data.MatrixId];
            //log.AppendLine($"Swap2:\nTimeLeftBefore: {TimeLeft}\nDeltaTime: {deltaTime}");
            TimeLeft -= deltaTime;
            //log.AppendLine($"TimeLeftAfter: {TimeLeft}");

            //TimeLeft = TimeLeft
            //    + swapOut.Data.TimeToEmpty
            //    + GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapOut.Data.MatrixId]
            //    + GD.JourneyTime[swapOut.Data.MatrixId, swapIn.Next.Data.MatrixId]
            //    - swapIn.Data.TimeToEmpty
            //    - GD.JourneyTime[swapIn.Prev.Data.MatrixId, swapIn.Data.MatrixId]
            //    - GD.JourneyTime[swapIn.Data.MatrixId, swapIn.Next.Data.MatrixId];

            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;

            nodes.Remove(swapOut);
            nodes.Add(swapIn);
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
                    + JourneyTime[i.Data.MatrixId, iplus.Data.MatrixId]          // 0-1 eraf
                    + JourneyTime[iplus.Data.MatrixId, iplus.Next.Data.MatrixId] // 1-2 eraf
                    + JourneyTime[j.Data.MatrixId, jplus.Data.MatrixId];         // 8-9 eraf

                Node curr = i.Next.Next;    // 2
                Node stop = j;              // 8

                while (curr != stop)
                {
                    Node temp = curr.Next;
                    TimeLeft = TimeLeft
                        + JourneyTime[curr.Data.MatrixId, temp.Data.MatrixId];
                    curr.Next = curr.Prev;
                    TimeLeft = TimeLeft
                        - JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId];
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
                    - JourneyTime[i.Data.MatrixId, j.Data.MatrixId]          // 0-8 erbij
                    - JourneyTime[j.Data.MatrixId, j.Next.Data.MatrixId]     // 8-7 erbij
                    - JourneyTime[iplus.Data.MatrixId, jplus.Data.MatrixId]; // 1-9 erbij
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
                    + JourneyTime[iprev.Data.MatrixId, i.Data.MatrixId]
                    + JourneyTime[i.Data.MatrixId, inext.Data.MatrixId] 
                    + JourneyTime[j.Data.MatrixId, jnext.Data.MatrixId]
                    - JourneyTime[iprev.Data.MatrixId, inext.Data.MatrixId] 
                    - JourneyTime[j.Data.MatrixId, i.Data.MatrixId] 
                    - JourneyTime[i.Data.MatrixId, jnext.Data.MatrixId];
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

                        double del_dist = JourneyTime[x1.Data.MatrixId, x2.Data.MatrixId] + JourneyTime[y1.Data.MatrixId, y2.Data.MatrixId];
                        double X1Y1 = JourneyTime[x1.Data.MatrixId, y1.Data.MatrixId];
                        double X2Y2 = JourneyTime[x2.Data.MatrixId, y2.Data.MatrixId];

                        if (del_dist - (X1Y1 + X2Y2) > 0)
                        {
                            //Console.WriteLine("Doing 2-opt move...");
                            Two_Opt_Move(x1, y1);
                            return;
                        }
                        else
                        {
                            double X2Y1 = JourneyTime[x2.Data.MatrixId, y1.Data.MatrixId];
                            Node z1 = x2.Next;
                            if (z1 != y1)
                            {
                                if ((del_dist + JourneyTime[x2.Data.MatrixId, z1.Data.MatrixId]) - (X2Y2 + X2Y1 + JourneyTime[x1.Data.MatrixId, z1.Data.MatrixId]) > 0)
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
                                    if ((del_dist + JourneyTime[y1.Data.MatrixId, z1.Data.MatrixId]) - (X1Y1 + X2Y1 + JourneyTime[y2.Data.MatrixId, z1.Data.MatrixId]) > 0)
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

    //#region [DEPRECATED] Loop
    //public class Loop : IEnumerable
    //{
    //    #region Variables & Constructor
    //    public double Duration;
    //    public double RoomLeft;
    //    public int Count;

    //    public Node Start; //Order references to Dump

    //    //public Loop()
    //    //{
    //    //    Start = new Node();
    //    //    Duration = 30;              //Het storten moet één keer per Loop (lus) gebeuren
    //    //    RoomLeft = 20000;           //Gecomprimeerd
    //    //    Count = 0;
    //    //}

    //    public Node[] ToList()
    //    {
    //        Node[] nodes = new Node[Count];
    //        Node curr = Start.Next; int i = 0;
    //        while (!curr.IsDump)
    //        {
    //            nodes[i] = curr;
    //            curr = curr.Next;
    //            i++; 
    //        } 
    //        return nodes;
    //    }
    //    #endregion

    //    #region Order Addition/Removal
    //    public Node AddOrder(Order order, Node nextTo)
    //    {
    //        Duration += (order.TimeToEmpty
    //            + JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
    //            + JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
    //            - JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

    //        Node n = nextTo.AppendNext(order);

    //        RoomLeft -= (order.NumContainers * order.VolPerContainer * 0.2);
    //        Count++;

    //        return n;
    //    }
    //    public void RemoveNode(Node n)
    //    {
    //        Order order = n.Data;

    //        Duration -= order.TimeToEmpty + JourneyTime[n.Prev.Data.MatrixId, order.MatrixId] + JourneyTime[order.MatrixId, n.Next.Data.MatrixId];
    //        Duration += JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId];
            
    //        n.Remove();

    //        RoomLeft += (order.NumContainers * order.VolPerContainer * 0.2);
    //        Count--;
    //    }
    //    #endregion



    //    public bool EvaluateRandomAdd(Random rnd)
    //    {
    //        while (true)
    //        {
    //            int nodeI = rnd.Next(0, Count);

    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return $"nodeCount={Count}, time={Duration}, roomLeft={RoomLeft}";
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return new DayScheduleEnumerator(Start);
    //    }
    //}
    //#endregion

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
        public int TruckIndex;
        public int DayIndex;

        public Node[] Others { get; private set; }

        public long ID = GlobalCounter.Next(); //for debugging purposes

        //public Node()
        //{
        //    IsDump = true;
        //    Data = GD.Dump;
        //    Prev = Next = this;
        //}

        public Node(int dayInd, int truckInd) //Voor invoegen van Sentry, alleen bij constructor DayRoute!
        {
            IsDump = true;
            IsSentry = true;
            Data = Dump;
            TourIndex = -1;
            Next = this;
            TruckIndex = truckInd;
            DayIndex = dayInd;
        } 
        public Node(int dayInd, int truckInd, int dumpId) //Voor invoegen van dump
        {
            IsDump = true;
            IsSentry = false;
            Data = Dump;
            TourIndex = dumpId;
            TruckIndex = truckInd;
            DayIndex = dayInd;
        }
        public Node(Order o, int dayInd, int truckInd, int tourInd) //Voor invoegen normale order of dump
        {
            IsDump = o.OrderId == 0;
            Data = o;
            TourIndex = tourInd;
            IsSentry = false;
            TruckIndex = truckInd;
            DayIndex = dayInd;
        }
        public void SetOthers(Node[] others)
        {
           Others = others;
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
            Node n = new Node(o, DayIndex, TruckIndex, TourIndex)
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
            return n.ID == ID;
        }
        public override int GetHashCode() //This method will not work for dumps, as they are the only order that can occur multiple times in one day.
        {
            unchecked
            {
                int hash = 17;
                hash *= 23 + TruckIndex.GetHashCode();
                hash *= 23 + DayIndex.GetHashCode();
                hash *= 23 + Data.OrderId.GetHashCode();
                return hash;
            }
        }
        public string ToLongString() => $"{(IsSentry ? "Sentry" : "Node")} ti{TourIndex}: {Data}";
        public override string ToString() => $"ID {ID}{(IsDump ? (IsSentry ? " (Sentry)" : " (Dump)") : "")}";
        #endregion
    }
    #endregion
}
