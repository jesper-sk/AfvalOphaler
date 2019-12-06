using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfvalOphaler
{
    public class Schedule
    {
        #region Variables
        public Day[,] Days;
        //public Stack<Order> bestRatioedOrders;
        //public Queue<Order> notPlannedOrders;
        public List<Order> nonPlannedOrders;

        public double totalTime;
        public double CalculateTotalTime()
        {
            double total = 0;
            foreach (Day d in Days) total += (720-d.TimeLeft);
            totalTime = total;
            return total;
        }
        public double totalPenalty;
        public double CalculateTotalPenalty()
        {
            double total = 0;
            //foreach (Order o in bestRatioedOrders) total +=  o.Frequency * 3 * o.TimeToEmpty;
            //foreach (Order o in notPlannedOrders) total += o.Frequency * 3 * o.TimeToEmpty;
            foreach (Order o in nonPlannedOrders) total += o.Frequency * 3 * o.TimeToEmpty;
            totalPenalty = total;
            return total;
        }
        public double Score { get => CalculateScore(); }
        public double CalculateScore() { return CalculateTotalTime() + CalculateTotalPenalty(); }
        #endregion

        #region Constructor and Clone
        public Schedule(List<Order> orders)
        {
            Days = new Day[5, 2];
            for (int d = 0; d < 5; d++) for (int t = 0; t < 2; t++) Days[d, t] = new Day(t, d);
            Rnd = new Random();

            //orders.Sort((a, b) => (a.Score.CompareTo(b.Score)) * -1);
            //bestRatioedOrders = new Stack<Order>(orders);
            //notPlannedOrders = new Queue<Order>();

            //nonPlannedOrders = orders.ToList();

            //nonPlannedOrders = orders.OrderBy(o => o.Score).ToList();
            nonPlannedOrders = orders.OrderBy(o => o.Score).ThenByDescending(o => o.Frequency).ToList(); // Redelijk goed resultaat
            //nonPlannedOrders = orders.OrderByDescending(o => o.Frequency).ThenBy(o => o.Score).ToList(); // Slechter resultaat
            
            //nonPlannedOrders = orders.OrderBy(o => o.Cluster).ThenBy(o => o.Frequency).ThenBy(o => o.Score).ToList(); // Beter resultaat met ClusterAdd en BEST en ASAP
            //nonPlannedOrders = orders.OrderBy(o => o.Frequency).ThenBy(o => o.Cluster).ThenBy(o => o.Score).ToList(); // Slechter resultaat met ClusterAdd

            CalculateTotalPenalty();
        }

        public Schedule Clone()
        {      
            return this; //Hey jochie
        }
        #endregion

        #region Neighbor Operations
        public Random Rnd;
        public static Func<Schedule, NeighborResult>[] neighborOperators =
        {
            currState => Add(currState),
            currState => Delete(currState),
            currState => Transfer(currState),
            //currState => AddByCluster(currState)
            //currState => Swap(currState)
        };
        public static Func<Schedule, NeighborResult> addOperator = (currState) => Add(currState);
        public static Func<Schedule, NeighborResult> addByClusterOperator = (currState) => AddByCluster(currState);
        public static Func<Schedule, NeighborResult> deleteOperator = (currState) => Delete(currState);
        public static Func<Schedule, NeighborResult> transferOperator = (currState) => Transfer(currState);
        public static Func<Schedule, NeighborResult> swapOperator = (currState) => Swap(currState);

        static NeighborResult Add(Schedule s)
        {
            // Pak node die nog niet in loop zit en beste afstand/tijd ratio heeft
            if (s.nonPlannedOrders.Count == 0) return new ImpossibleResult(s);

            //int notPickedOrderIndex = (int)(s.nonPlannedOrders.Count * Math.Pow(s.Rnd.NextDouble(), 1.0 / 50.0));
            //int notPickedOrderIndex = s.Rnd.Next(0, s.nonPlannedOrders.Count);
            int notPickedOrderIndex = s.Rnd.Next(0, (s.nonPlannedOrders.Count / 5));
            Order bestNotPicked = s.nonPlannedOrders[notPickedOrderIndex];
            int[][] combis = GD.AllowedDayCombinations[bestNotPicked.Frequency]; // Gives all possible ways to plan the order.

            // BEST
            // Tries to plan the order is the most optimal place, not the earliest place.  
            ///*
            List<Node> bestnextTos = new List<Node>(bestNotPicked.Frequency);
            List<int> bestloopIndices = new List<int>(bestNotPicked.Frequency);
            List<int> bestdays = new List<int>(bestNotPicked.Frequency);
            List<int> besttrucks = new List<int>(bestNotPicked.Frequency);
            List<double> bestdeltas = new List<double>(bestNotPicked.Frequency);

            bool planningFound = false;
            double bestDeltaSum = double.MaxValue;
            for (int c = 0; c < combis.Length; c++)
            {
                int truckFoundForAllDays = 0;
                List<Node> nextTos = new List<Node>(bestNotPicked.Frequency);
                List<int> loopIndices = new List<int>(bestNotPicked.Frequency);
                List<int> days = new List<int>(bestNotPicked.Frequency);
                List<int> trucks = new List<int>(bestNotPicked.Frequency);
                List<double> deltas = new List<double>(bestNotPicked.Frequency);

                for (int d = 0; d < combis[c].Length; d++)
                {
                    bool truckFound = false;
                    double bestTruckDelta = double.MaxValue;
                    int bestTruck = -1;
                    Node bestWhere = null;
                    int bestLoop = -1;
                    for (int t = 0; t < 2; t++)
                    {
                        if (s.Days[combis[c][d], t].EvaluateAddition(bestNotPicked, out Node where, out double delta, out int loop) && delta < bestTruckDelta)
                        {
                            bestWhere = where;
                            bestLoop = loop;
                            bestTruck = t;
                            bestTruckDelta = delta;

                            truckFound = true;
                        }
                    }
                    if (!truckFound) break;
                    else
                    {
                        nextTos.Add(bestWhere);
                        loopIndices.Add(bestLoop);
                        days.Add(combis[c][d]);
                        trucks.Add(bestTruck);
                        deltas.Add(bestTruckDelta);

                        truckFoundForAllDays++;
                    }

                }
                if (truckFoundForAllDays == combis[c].Length) 
                { 
                    if (deltas.Sum() < bestDeltaSum)
                    {
                        bestnextTos = nextTos;
                        bestloopIndices = loopIndices;
                        bestdays = days;
                        besttrucks = trucks;
                        bestdeltas = deltas;
                        planningFound = true; 
                    }
                }
            }
            if (planningFound)
            {
                return new AddResult(s, bestNotPicked, notPickedOrderIndex, bestnextTos, bestloopIndices, bestdays, besttrucks, bestdeltas);
            }
            else
            {
                return new ImpossibleResult(s);
            }
            //*/

        }

        static NeighborResult AddByCluster(Schedule s)
        {
            if (s.nonPlannedOrders.Count == 0) return new ImpossibleResult(s);

            Random rnd = new Random();
            int index = rnd.Next(0, s.nonPlannedOrders.Count / 5);
            //int index = 0;
            Order bestNotPicked = s.nonPlannedOrders[index];
            int[][] combis = GD.AllowedDayCombinations[bestNotPicked.Frequency];

            #region BEST
            // Tries to plan the order is the most optimal place, not the earliest place.  
            ///*
            List<Node> bestnextTos = new List<Node>(bestNotPicked.Frequency);
            List<int> bestloopIndices = new List<int>(bestNotPicked.Frequency);
            List<int> bestdays = new List<int>(bestNotPicked.Frequency);
            List<int> besttrucks = new List<int>(bestNotPicked.Frequency);
            List<double> bestdeltas = new List<double>(bestNotPicked.Frequency);

            bool planningFound = false;
            double bestDeltaSum = double.MaxValue;
            for (int c = 0; c < combis.Length; c++)
            {
                int truckFoundForAllDays = 0;
                List<Node> nextTos = new List<Node>(bestNotPicked.Frequency);
                List<int> loopIndices = new List<int>(bestNotPicked.Frequency);
                List<int> days = new List<int>(bestNotPicked.Frequency);
                List<int> trucks = new List<int>(bestNotPicked.Frequency);
                List<double> deltas = new List<double>(bestNotPicked.Frequency);

                for (int d = 0; d < combis[c].Length; d++)
                {
                    bool truckFound = false;
                    double bestTruckDelta = double.MaxValue;
                    int bestTruck = -1;
                    Node bestWhere = null;
                    int bestLoop = -1;
                    for (int t = 0; t < 2; t++)
                    {
                        if (s.Days[combis[c][d], t].EvaluateAddition(bestNotPicked, out Node where, out double delta, out int loop, true) && delta < bestTruckDelta)
                        {
                            bestWhere = where;
                            bestLoop = loop;
                            bestTruck = t;
                            bestTruckDelta = delta;

                            truckFound = true;
                        }
                    }
                    if (!truckFound) break;
                    else
                    {
                        nextTos.Add(bestWhere);
                        loopIndices.Add(bestLoop);
                        days.Add(combis[c][d]);
                        trucks.Add(bestTruck);
                        deltas.Add(bestTruckDelta);

                        truckFoundForAllDays++;
                    }

                }
                if (truckFoundForAllDays == combis[c].Length)
                {
                    if (deltas.Sum() < bestDeltaSum)
                    {
                        bestnextTos = nextTos;
                        bestloopIndices = loopIndices;
                        bestdays = days;
                        besttrucks = trucks;
                        bestdeltas = deltas;
                        planningFound = true;
                    }
                }
            }
            if (planningFound)
            {
                return new AddResult(s, bestNotPicked, index, bestnextTos, bestloopIndices, bestdays, besttrucks, bestdeltas);
            }
            else
            {
                return new ImpossibleResult(s);
            }
            //*/
            #endregion
        }
        static NeighborResult Delete(Schedule s)
        {
            // Krijg voor elke dag voor elke truck de meest extreme node.
            // Pak met kans één van deze nodes met bias naar de slechtste.
            List<Tuple<Node, double, int, int, int>> worsten = new List<Tuple<Node, double, int, int, int>>(10);

            for (int d = 0; d < 5; d -= -1)
            {
                for (int t = 0; t < 2; t -= -1)
                {
                    if (s.Days[d, t].EvaluateDeletion(false, out Node worst, out double delta, out int loop))
                    {
                        worsten.Add(new Tuple<Node, double, int, int, int>(worst, delta, d, t, loop));
                    }
                }
            }

            if (worsten.Count > 0)
            {
                Random rnd = new Random();
                worsten.Sort((a, b) => (a.Item2.CompareTo(b.Item2)));
                int index = (int)(worsten.Count * Math.Pow(rnd.NextDouble(), (1.0 / 5.0)));
                Tuple<Node, double, int, int, int> chosenWorst = worsten[index];
                return new DeleteResult(s, chosenWorst);
            }
            else return new ImpossibleResult(s);
        }
        static NeighborResult Transfer(Schedule s)
        {
            List<Transfer> worsten = new List<Transfer>();
            for (int d = 0; d < 5; d++)
            {
                for (int t = 0; t < 2; t++)
                {
                    if (s.Days[d, t].EvaluateDeletion(true, out Node worst, out double delDelta, out int delLoop))
                    {
                        bool high = worst.Data.Frequency > 1;
                        int start = high ? d : 0;
                        int end = high ? d + 1 : 5;
                        for (int ad = start; ad < end; ad++)
                        {
                            for (int at = 0; at < 2; at++)
                            {
                                if (s.Days[ad, at].EvaluateAddition(worst.Data, out Node bestNode, out double addDelta, out int addLoop))
                                {
                                    worsten.Add(new Transfer()
                                    {
                                        ToTransfer = worst,
                                        AddDayIndex = ad,
                                        DelDayIndex = d,
                                        DelLoopIndex = delLoop,
                                        AddLoopIndex = addLoop,
                                        AddTruckIndex = at,
                                        DelTruckIndex = t,
                                        AddNextTo = bestNode,
                                        DelDelta = delDelta,
                                        AddDelta = addDelta
                                    });
                                }
                            }
                        }
                    }
                }
            }
            if (worsten.Count == 0) return new ImpossibleResult(s);
            worsten.Sort((a, b) => (a.AddDelta + a.DelDelta).CompareTo(b.AddDelta + b.DelDelta));
            Random rnd = new Random();
            int index = (int)(worsten.Count * Math.Pow(rnd.NextDouble(), (1.0 / 5.0)));
            Transfer chosenWorst = worsten[index];
            return chosenWorst.ToResult(s);
        }
        static NeighborResult Swap(Schedule s)
        {
            // Pak twee willekeurige nodes uit twee verschillende loops.
            // swap deze
            throw new NotImplementedException();
        }
        #endregion

        #region To Strings
        public override string ToString()
        {
            return $"Total time: {CalculateTotalTime()}, Total Penalty: {CalculateTotalPenalty()}";
        }

        public string ToCheckString()
        {
            StringBuilder b = new StringBuilder();
            for(int t = 0; t < 2; t++)
            {
                for(int d = 0; d < 5; d++)
                {
                    List<Loop> loops = Days[d, t].Loops;
                    int global = 1;
                    for(int l = 0; l < loops.Count; l++)
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
        #endregion
    }

    struct Transfer
    {
        public Node ToTransfer;
        public int DelDayIndex;
        public int DelLoopIndex;
        public int DelTruckIndex;
        public int AddDayIndex;
        public int AddLoopIndex;
        public int AddTruckIndex;
        public Node AddNextTo;
        public double DelDelta;
        public double AddDelta;

        public TransferResult ToResult(Schedule s)
        {
            AddResult ares = new AddResult(s, ToTransfer.Data, -1, AddNextTo, AddLoopIndex, AddDayIndex, AddTruckIndex, AddDelta);
            DeleteResult dres = new DeleteResult(s, ToTransfer, DelDelta, DelDayIndex, DelTruckIndex, DelLoopIndex);
            return new TransferResult(s, dres, ares);
        }
    }

    public class Day
    {
        public List<Loop> Loops;
        public double TimeLeft;

        public readonly int DayIndex;
        public readonly int Truck;

        public Day(int dind, int trind)
        {
            Loops = new List<Loop>() { new Loop(dind, trind) };
            TimeLeft = 690;
            DayIndex = dind;
            Truck = trind;
        }

        public Loop AddLoop()
        {
            Loop l = new Loop(DayIndex, Truck);
            Loops.Add(l);
            return l;
        }

        public bool EvaluateAddition(Order order, out Node bestNode, out double bestDeltaTime, out int bestLoop, bool evaluateCluster = false)
        {
            bestNode = null;
            bestDeltaTime = double.MaxValue;
            bestLoop = -1;

            for(int i = 0; i < Loops.Count; i++)
            {
                Loop loop = Loops[i];
                if (evaluateCluster && loop.Cluster != - 1 && loop.Cluster != order.Cluster) continue;
                if (loop.EvaluateOptimalAddition(order, out Node lOpt, out double _, out double lTd))
                {
                    if (TimeLeft >= lTd && lTd < bestDeltaTime)
                    {
                        bestNode = lOpt;
                        bestDeltaTime = lTd;
                        bestLoop = i;
                    }
                }
            }
            if (bestLoop == -1)
            {
                if (order.JourneyTimeFromDump + order.JourneyTimeToDump + order.TimeToEmpty + 30 <= TimeLeft) // Check if adding new loop helps
                {
                    AddLoop();
                    TimeLeft -= 30;
                    bestLoop = Loops.Count - 1;
                    bestDeltaTime = order.JourneyTimeFromDump + order.JourneyTimeToDump + order.TimeToEmpty + 30;
                    bestNode = Loops[bestLoop].Start;
                }
                return false;
            }
            return true;
        }

        public bool EvaluateDeletion(bool isTransfer, out Node worst, out double opt, out int loop)
        {
            worst = null;
            opt = double.MaxValue;
            loop = -1;
            for (int i = 0; i < Loops.Count; i++)
            {
                Loop curr = Loops[i];
                if (curr.EvaluteOptimalDeletion(isTransfer, out Node lworst, out double lopt))
                {
                    if (lopt > opt)
                    {
                        opt = lopt;
                        worst = lworst;
                        loop = i;
                    }
                }
            }
            return worst != null;
        }

        public Node AddOrderToLoop(Order order, Node nextTo, int loopIndex)
        {
            //Console.WriteLine($"Timeleft before AddOrder: {Loops[loopIndex].Duration}");
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

        public override string ToString()
        {
            return $"LoopCount={Loops.Count}, timeLeft={TimeLeft}";
        }
    }

    public class Loop
    {
        public double Duration;
        public double RoomLeft;
        public int Count;
        public int Cluster;
        public int Day;
        public int Truck;

        public Node Start; //Order references to Dump


        public Loop(int day, int truck)
        {
            Start = new Node();
            Duration = 30;              //Het storten moet één keer per Loop (lus) gebeuren
            RoomLeft = 20000;       //Gecomprimeerd
            Count = 0;
            Cluster = -1;
            Day = day;
            Truck = truck;
        }

        public bool EvaluateOptimalAddition(Order order, out Node opt, out double newRoomLeft, out double td)
        {
            td = 0;
            opt = null;
            newRoomLeft = RoomLeft - (order.NumContainers * order.VolPerContainer) * 0.2; // Het gecomprimeerde gewicht dat erbij zou komen

            if (newRoomLeft <= 0) return false;    // Toevoegen zal de gewichtsconstraint schenden

            double best = GD.JourneyTime[order.MatrixId, Start.Data.MatrixId];
            opt = Start;
            Node curr = Start.Next;
            while (!curr.IsDump)
            {
                double t = GD.JourneyTime[order.MatrixId, curr.Data.MatrixId];
                if (t < best)
                {
                    opt = curr;
                    best = t;
                }
                curr = curr.Next;
            }

            double tNext = GD.JourneyTime[order.MatrixId, opt.Next.Data.MatrixId];
            double tPrev = GD.JourneyTime[order.MatrixId, opt.Prev.Data.MatrixId];

            if (tPrev < tNext) opt = opt.Prev;

            //Console.WriteLine($"opt: {opt.Data}");
            //Console.WriteLine($"order: {order}");

            // Calculate delta
            //Console.WriteLine($"timetoempty: {order.TimeToEmpty}");
            //Console.WriteLine($"journeytime erbij: {GD.JourneyTime[opt.Data.MatrixId, order.MatrixId]}");
            //Console.WriteLine($"journeytime erbij: {GD.JourneyTime[order.MatrixId, opt.Next.Data.MatrixId]}");
            //Console.WriteLine($"journeytime eraf: {GD.JourneyTime[opt.Data.MatrixId, opt.Next.Data.MatrixId]}");
            td = (order.TimeToEmpty
                + GD.JourneyTime[opt.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, opt.Next.Data.MatrixId]
                - GD.JourneyTime[opt.Data.MatrixId, opt.Next.Data.MatrixId]);

            return true;
        }

        public bool EvaluteOptimalDeletion(bool isTransfer, out Node worst, out double opt)
        {
            Node curr = Start.Next;
            opt = double.MaxValue;
            worst = null;

            while (!curr.IsDump)
            {
                double to = GD.JourneyTime[curr.Prev.Data.MatrixId, curr.Data.MatrixId];
                double from = GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId];
                double inplace = GD.JourneyTime[curr.Prev.Data.MatrixId, curr.Next.Data.MatrixId];
                //double tte = isTransfer ? 0 : curr.Data.TimeToEmpty;
                //double delta = inplace - (from + to) + tte * 2;
                double tte = isTransfer ? 0 : (3 * curr.Data.Frequency * curr.Data.TimeToEmpty) - curr.Data.TimeToEmpty;
                double delta = inplace - (from + to) + tte;
                if (delta < opt)
                {
                    opt = delta;
                    worst = curr;
                }
                curr = curr.Next;
            }

            return (worst != null);
        }

        public Node AddOrder(Order order, Node nextTo)
        {
            Cluster = order.Cluster;
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

        public override string ToString()
        {
            return $"nodeCount={Count}, time={Duration}, roomLeft={RoomLeft}";
        }
    }

    public class Node
    {
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
        }
    }

    #region Operator Results
    public abstract class NeighborResult
    {
        public Schedule state;

        public NeighborResult(Schedule s)
        {
            state = s;
        }

        public abstract double GetTotalDelta();

        public abstract void ApplyOperator();

    }
    public class AddResult : NeighborResult
    {
        Order order;
        int orderIndex;
        List<Node> nextTos;
        List<int> loopIndices;
        List<int> dayIndices;
        List<int> truckIndices;
        List<double> deltas;

        public AddResult(Schedule s, Order order, int orderIndex, List<Node> nextTos, List<int> loopIndices, List<int> dayIndices, List<int> truckIndices, List<double> deltas) : base(s)
        {
            this.order = order;
            this.orderIndex = orderIndex;
            this.nextTos = nextTos;
            this.loopIndices = loopIndices;
            this.dayIndices = dayIndices;
            this.truckIndices = truckIndices;
            this.deltas = deltas;
        }

        public AddResult(Schedule s, Order order, int orderIndex, Node nextTo, int loopIndex, int dayIndex, int truckIndex, double delta) : base(s)
        {
            this.order = order;
            this.orderIndex = orderIndex;
            nextTos = new List<Node>() { nextTo };
            loopIndices = new List<int>() { loopIndex };
            dayIndices = new List<int>() { dayIndex };
            truckIndices = new List<int>() { truckIndex };
            deltas = new List<double>() { delta };
        }

        public void SetIndex(int i) { this.orderIndex = i; }

        public override double GetTotalDelta()
        {
            double totalExtraTravelTime = deltas.Sum();
            double penaltyReduction = 3 * order.Frequency * order.TimeToEmpty;
            return (totalExtraTravelTime - penaltyReduction);
        }

        public override void ApplyOperator()
        {
            for (int i = 0; i < nextTos.Count; i++)
            {
                state.Days[dayIndices[i], truckIndices[i]].AddOrderToLoop(order, nextTos[i], loopIndices[i]);
                state.totalTime += deltas[i];
            }
            state.totalPenalty -= 3 * order.Frequency * order.TimeToEmpty;
            state.nonPlannedOrders.RemoveAt(orderIndex);
        }
    }
    public class DeleteResult : NeighborResult
    {
        Node toDelete;
        double delta;
        int day;
        int truck;
        int loop;

        public DeleteResult(Schedule s, Node toDelete, double delta, int day, int truck, int loop) : base(s)
        {
            this.toDelete = toDelete;
            this.delta = delta;
            this.day = day;
            this.truck = truck;
            this.loop = loop;
        }
        public DeleteResult(Schedule s, Tuple<Node, double, int, int, int> del) : base(s)
        {
            toDelete = del.Item1;
            delta = del.Item2;
            day = del.Item3;
            truck = del.Item4;
            loop = del.Item5;
        }

        public override double GetTotalDelta()
        {
            return delta;
        }

        public override void ApplyOperator()
        {
            state.Days[day, truck].RemoveNodeFromLoop(toDelete, loop);
            Order o = toDelete.Data;
            state.totalTime += (delta - 4 * o.Frequency * o.TimeToEmpty); // Delta should be only change in drivetime, so should not include the penalty.
            state.totalPenalty += 3 * o.Frequency * o.TimeToEmpty;
            state.nonPlannedOrders.Add(toDelete.Data);
        }
    }
    public class TransferResult : NeighborResult
    {
        DeleteResult d;
        AddResult a;
        public TransferResult(Schedule s, DeleteResult d, AddResult a) : base(s)
        {
            this.d = d;
            this.a = a;
        }

        public override double GetTotalDelta()
        {
            return d.GetTotalDelta() + a.GetTotalDelta();
        }

        public override void ApplyOperator()
        {
            d.ApplyOperator();
            a.SetIndex(state.nonPlannedOrders.Count - 1);
            a.ApplyOperator();
        }
    }
    public class SwapResult : NeighborResult
    {
        List<DeleteResult> des;
        List<AddResult> ads;
        List<int> days;
        List<int> trucks;
        List<int> loops;
        public SwapResult(Schedule s, List<DeleteResult> des, List<AddResult> ads, List<int> days, List<int> trucks, List<int> loops) : base(s)
        {

        }

        public override double GetTotalDelta() { throw new NotImplementedException(); }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
    }

    public class ImpossibleResult : NeighborResult
    {
        public ImpossibleResult(Schedule s) : base(s)
        {
        }

        public override double GetTotalDelta()
        {
            throw new NotImplementedException();
        }

        public override void ApplyOperator()
        {
            Console.WriteLine("APPLYING IMPOSSIBLE OPERATOR, DAKANNIEHE!!!");
            //Console.WriteLine("Trying to apply ImpossibleOperator...");
            //throw new InvalidOperationException(); 
        }
    }
    #endregion

}
