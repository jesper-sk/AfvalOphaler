﻿using System;
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
        public Day[,] days;
        //public Stack<Order> bestRatioedOrders;
        //public Queue<Order> notPlannedOrders;
        public List<Order> nonPlannedOrders;

        public double totalTime;
        public double CalculateTotalTime()
        {
            double total = 0;
            foreach (Day d in days) total += (720-d.TimeLeft);
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
            days = new Day[5, 2];
            for (int d = 0; d < 5; d++) for (int t = 0; t < 2; t++) days[d, t] = new Day();
            Rnd = new Random();

            //orders.Sort((a, b) => (a.Score.CompareTo(b.Score)) * -1);
            //bestRatioedOrders = new Stack<Order>(orders);
            //notPlannedOrders = new Queue<Order>();

            //nonPlannedOrders = orders.ToList();
            //nonPlannedOrders = orders.OrderBy(o => o.Score).ThenByDescending(o => o.Frequency).ToList();
            nonPlannedOrders = orders.OrderByDescending(o => o.Frequency).ThenBy(o => o.Score).ToList();
            //nonPlannedOrders = orders.OrderBy(o => o.XCoord).ThenBy(o => o.YCoord).ThenBy(o => o.Score).ToList();

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
            currState => Swap(currState)
            //addOperator,
            //deleteOperator, 
            //transferOperator, 
            //swapOperator 
        };
        public static Func<Schedule, NeighborResult> addOperator = (currState) => Add(currState);
        public static Func<Schedule, NeighborResult> deleteOperator = (currState) => Delete(currState);
        public static Func<Schedule, NeighborResult> transferOperator = (currState) => Transfer(currState);
        public static Func<Schedule, NeighborResult> swapOperator = (currState) => Swap(currState);
        static NeighborResult Add(Schedule s)
        {
            // Pak node die nog niet in loop zit en beste afstand/tijd ratio heeft
            if (s.nonPlannedOrders.Count == 0) return new ImpossibleResult(s, null);

            //int notPickedOrderIndex = (int)(s.nonPlannedOrders.Count * Math.Pow(s.Rnd.NextDouble(), 1.0 / 50.0));
            //int notPickedOrderIndex = s.Rnd.Next(0, s.nonPlannedOrders.Count);
            int notPickedOrderIndex = s.Rnd.Next(0, (s.nonPlannedOrders.Count / 10));
            Order bestNotPicked = s.nonPlannedOrders[notPickedOrderIndex];
            int[][] combis = GD.AllowedDayCombinations[bestNotPicked.Frequency]; // Gives all possible ways to plan the order.

            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            // ASAP
            ///*
            List<Node> nextTos = new List<Node>(bestNotPicked.Frequency);
            List<int> loopIndices = new List<int>(bestNotPicked.Frequency);
            List<int> days = new List<int>(bestNotPicked.Frequency);
            List<int> trucks = new List<int>(bestNotPicked.Frequency);
            List<double> deltas = new List<double>(bestNotPicked.Frequency);

            bool planningFound = false;
            for (int c = 0; c < combis.Length; c++)
            {
                int truckFoundForAllDays = 0;
                nextTos = new List<Node>(bestNotPicked.Frequency);
                loopIndices = new List<int>(bestNotPicked.Frequency);
                days = new List<int>(bestNotPicked.Frequency);
                trucks = new List<int>(bestNotPicked.Frequency);
                deltas = new List<double>(bestNotPicked.Frequency);

                for (int d = 0; d < combis[c].Length; d++)
                {
                    bool truckFound = false;
                    for (int t = 0; t < 2; t++)
                    {
                        Node where;
                        if (s.days[combis[c][d], t].EvaluateAddition(bestNotPicked, out where, out double delta, out int loop))
                        {
                            nextTos.Add(where);
                            loopIndices.Add(loop);
                            days.Add(combis[c][d]);
                            trucks.Add(t);
                            deltas.Add(delta);

                            truckFound = true;
                            truckFoundForAllDays++;
                        }
                    }
                    if (!truckFound) break;
                }
                if (truckFoundForAllDays == combis[c].Length) { planningFound = true; break; }
            }
            if (planningFound)
            {
                return new AddResult(s, bestNotPicked, notPickedOrderIndex, nextTos, loopIndices, days, trucks, deltas);
            }
            else
            {
                return new ImpossibleResult(s, new List<Order> { bestNotPicked });
            }
            //*/

            // BEST
            // Tries to plan the order is the most optimal place, not the earliest place.  
            /*
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
                        if (s.days[combis[c][d], t].EvaluateAddition(bestNotPicked, out Node where, out double delta, out int loop) && delta < bestTruckDelta)
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
                return new ImpossibleResult(s, new List<Order> { bestNotPicked });
            }
            */

        }
        static NeighborResult Delete(Schedule s)
        {
            // Pak willekeurige node in een loop
            // verwijder deze
            Random rnd = new Random();
            List<Loop> candidates = new List<Loop>();
            for (int d = 0; d < 5; d -= -1)
            {
                for (int t = 0; t < 2; t -= -1)
                {
                    Day day = s.days[d, t];
                    for (int l = 0; l < day.Loops.Count; l-=-1)
                    {
                        if (day.Loops[l].Count > 0) candidates.Add(day.Loops[l]);
                    }
                }
            }
            bool removed = false;
            while (!removed)
            {
                int index = rnd.Next(0, candidates.Count);
                Loop l = candidates[index];
                int nodeindex = rnd.Next(1, l.Count + 1);
                Node n = l.Start;
                for (int i = 0; i < nodeindex; i-=-1)
                {
                    n = n.Next;
                }
                if (n.Data.Frequency == 1)
                {
                    l.RemoveNode(n);
                    removed = true;
                }
            }
            return null;
        }
        static NeighborResult Transfer(Schedule s)
        {
            // Pak een willekeurige node in een loop
            // voeg deze toe aan de dichtsbijzijnde andere loop
            throw new NotImplementedException();
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
                    List<Loop> loops = days[d, t].Loops;
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
                res.AppendLine($"Truck 1: {days[i, 0]}");
                res.AppendLine($"Truck 2: {days[i, 1]}");
            }
            return res.ToString();
        }
        #endregion
    }

    public class Day
    {
        public List<Loop> Loops;
        public double TimeLeft;

        public Day()
        {
            Loops = new List<Loop>() { new Loop() };
            TimeLeft = 690;
        }

        public bool EvaluateAddition(Order order, out Node bestNode, out double bestDeltaTime, out int bestLoop)
        {
            bestNode = null;
            bestDeltaTime = double.MaxValue;
            bestLoop = -1;

            for(int i = 0; i < Loops.Count; i++)
            {
                Loop loop = Loops[i];
                if (loop.EvaluateOptimalAddition(order, out Node lOpt, out double _, out double lTd))
                {
                    //Console.WriteLine($"EvaluateOptimalAddition == TRUE!!!, lTd = {lTd}");
                    //Console.WriteLine($"newtimeleft: {newTimeLeft}");
                    if (TimeLeft >= lTd && lTd < bestDeltaTime)
                    {
                        //Console.WriteLine("updating best loop...");
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
                    Loops.Add(new Loop());
                    TimeLeft -= 30;
                    bestLoop = Loops.Count - 1;
                    bestDeltaTime = order.JourneyTimeFromDump + order.JourneyTimeToDump + order.TimeToEmpty + 30;
                    bestNode = Loops[bestLoop].Start;
                }
                return false;
            }
            return true;
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

        public Node Start; //Order references to Dump

        public Loop()
        {
            Start = new Node();
            Duration = 30;              //Het storten moet één keer per Loop (lus) gebeuren
            RoomLeft = 20000;       //Gecomprimeerd
            Count = 0;
        }

        public bool EvaluateOptimalAddition(Order order, out Node opt, out double newRoomLeft, out double td)
        {
            td = 0;
            opt = null;
            newRoomLeft = RoomLeft - (order.NumContainers * order.VolPerContainer) * 0.2; //Het gecomprimeerde gewicht dat erbij zou komen

            if (newRoomLeft <= 0)
            {
                //Console.WriteLine("Can't add cuz it will /schenden/ weightconstraint... ");
                return false;    //Toevoegen zal de gewichtsconstraint schenden
            }

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

        public override double GetTotalDelta()
        {
            double totalExtraTravelTime = deltas.Sum();
            double penaltyReduction = 3 * order.Frequency * order.TimeToEmpty;
            return (totalExtraTravelTime - penaltyReduction);
        }

        public override void ApplyOperator()
        {
            for(int i = 0; i < nextTos.Count; i++)
            {
                state.days[dayIndices[i], truckIndices[i]].AddOrderToLoop(order, nextTos[i], loopIndices[i]);
                state.totalTime += deltas[i];
            }
            state.totalPenalty -= 3 * order.Frequency * order.TimeToEmpty;
            state.nonPlannedOrders.RemoveAt(orderIndex);
        }
    }
    public class DeleteResult : NeighborResult
    {
        public DeleteResult(Schedule s) : base(s)
        {

        }

        public override double GetTotalDelta() { throw new NotImplementedException(); }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class TransferResult : NeighborResult
    {
        public TransferResult(Schedule s) : base(s)
        {

        }

        public override double GetTotalDelta() { throw new NotImplementedException(); }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class SwapResult : NeighborResult
    {
        public SwapResult(Schedule s) : base(s)
        {

        }

        public override double GetTotalDelta() { throw new NotImplementedException(); }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
    }

    public class ImpossibleResult : NeighborResult
    {
        List<Order> failedOrders;
        public ImpossibleResult(Schedule s, List<Order> failed) : base(s)
        {
            failedOrders = failed;
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
