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
        public Day[,] days;
        public Stack<Order> bestRatioedOrders;
        public Queue<Order> notPlannedOrders;

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
            foreach (Order o in bestRatioedOrders) total += (3 * o.TimeToEmpty);
            foreach (Order o in notPlannedOrders) total += (3 * o.TimeToEmpty);
            totalPenalty = total;
            return total;
        }
        public double Score { get => totalTime + totalPenalty; }
        public double CalculateScore() { return (CalculateTotalTime() + CalculateTotalPenalty()); }
        #endregion

        #region Constructor and Clone
        public Schedule(List<Order> orders)
        {
            orders.Sort((a, b) => (a.Score.CompareTo(b.Score)) * -1);
            days = new Day[5, 2];
            for (int d = 0; d < 5; d++) for (int t = 0; t < 2; t++) days[d, t] = new Day();
            rnd = new Random();

            bestRatioedOrders = new Stack<Order>(orders);
            notPlannedOrders = new Queue<Order>();
            CalculateTotalPenalty();
        }

        public Schedule Clone()
        {      
            return this; //Hey jochie
        }
        #endregion

        #region Neighbor Operations
        Random rnd;
        public static Func<Schedule, NeighborResult>[] neighborOperators = { addOperator, deleteOperator, transferOperator, swapOperator };
        public static Func<Schedule, NeighborResult> addOperator = (currState) => Add(currState);
        public static Func<Schedule, NeighborResult> deleteOperator = (currState) => Delete(currState);
        public static Func<Schedule, NeighborResult> transferOperator = (currState) => Transfer(currState);
        public static Func<Schedule, NeighborResult> swapOperator = (currState) => Swap(currState);
        static NeighborResult Add(Schedule s)
        {
            // Pak node die nog niet in loop zit en beste afstand/tijd ratio heeft
            Order bestNotPicked;
            if (s.bestRatioedOrders.Count == 0) 
            {
                //Console.WriteLine("Empty stack...");
                if (s.notPlannedOrders.Count > 0) bestNotPicked = s.notPlannedOrders.Dequeue();
                else
                {
                    //Console.WriteLine("Queue empty too...");
                    return new ImpossibleResult(s, new double[] { 0 }, null);
                }
            }
            bestNotPicked = s.bestRatioedOrders.Pop();

            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            // ASAP
            int[][] combis = GD.AllowedDayCombinations[bestNotPicked.Frequency]; // Gives all possible ways to plan the order.
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
                        Node where = null;
                        if (s.days[combis[c][d], t].EvaluateAddition(bestNotPicked, out where, out double delta, out int loop))
                        {
                            //Console.WriteLine("EvaluateAddition == TRUE!!!");
                            nextTos.Add(where);
                            loopIndices.Add(loop);
                            days.Add(combis[c][d]);
                            trucks.Add(t);
                            deltas.Add(delta);

                            truckFound = true;
                            truckFoundForAllDays++;
                            break;
                        }
                    }
                    if (!truckFound) break;
                }
                if (truckFoundForAllDays == combis[c].Length) { planningFound = true; break; }
            }
            if (planningFound)
            {
                //Console.WriteLine("Planning found...");
                /*
                Console.WriteLine($"Freq: {bestNotPicked.Frequency}");
                for (int d = 0; d < days.Count; d++)
                {
                    Console.WriteLine($"Choosen day for {d}e inplan: {days[d]}, loop: {loopIndices[d]}");
                }
                */
                return new AddResult(s, bestNotPicked, nextTos.ToArray(), loopIndices.ToArray(), days.ToArray(), trucks.ToArray(), deltas.ToArray());
            }
            else
            {
                Console.WriteLine("No planning found for order:");
                Console.WriteLine(bestNotPicked);
                return new ImpossibleResult(s, deltas.ToArray(), new List<Order> { bestNotPicked });
            }

            // BEST
            // Tries to plan the order is the most optimal place, not the earliest place.         
        }
        static NeighborResult Delete(Schedule s)
        {
            // Pak willekeurige node in een loop
            // verwijder deze
            throw new NotImplementedException();
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
                        while (!ord.Next.IsDump)
                        {
                            b.AppendLine($"{t + 1}; {d + 1}; {global++}; {ord.Data.OrderId}");
                            ord = ord.Next;
                        }                     
                    }
                    b.AppendLine($"{t + 1}; {d + 1}; {global++}; 0");
                }
            }
            return b.ToString();
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
    }

    #region Operator Results
    public abstract class NeighborResult
    {
        public Schedule state;
        public double[] deltas;
        public double totalDelta;
        public NeighborResult(Schedule s, double[] d)
        {
            state = s;
            deltas = d;
            foreach (double delta in deltas) totalDelta += delta;
        }

        public abstract void ApplyOperator();
        public abstract void DiscardOperator();
    }
    public class AddResult : NeighborResult
    {
        Order order;
        Node[] nextTos;
        int[] loopIndices;
        int[] dayIndices;
        int[] truckIndices;

        public AddResult(Schedule s, Order order, Node[] nextTos, int[] loopIndices, int[] dayIndices, int[] truckIndices, double[] deltas) : base(s, deltas)
        {
            this.order = order;
            this.nextTos = nextTos;
            this.loopIndices = loopIndices;
            this.dayIndices = dayIndices;
            this.truckIndices = truckIndices;
        }

        public override void ApplyOperator()
        {
            //Console.WriteLine("Applying AddOperator...");
            for(int i = 0; i < nextTos.Length; i++)
            {
                state.days[dayIndices[i], truckIndices[i]].AddOrderToLoop(order, nextTos[i], loopIndices[i]);
                state.totalTime += deltas[i];
            }
            state.totalPenalty -= 3 * order.TimeToEmpty;
        }
        public override void DiscardOperator()
        {
            state.bestRatioedOrders.Push(order);
        }
    }
    public class DeleteResult : NeighborResult
    {
        public DeleteResult(Schedule s, double[] d) : base(s, d)
        {

        }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class TransferResult : NeighborResult
    {
        public TransferResult(Schedule s, double[] d) : base(s, d)
        {

        }
        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class SwapResult : NeighborResult
    {
        public SwapResult(Schedule s, double[] d) : base(s, d)
        {

        }
        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }

    public class ImpossibleResult : NeighborResult
    {
        List<Order> failedOrders;
        public ImpossibleResult(Schedule s, double[] d, List<Order> failed) : base(s, d)
        {
            failedOrders = failed;
        }
        public override void ApplyOperator() 
        {
            DiscardOperator();
            //Console.WriteLine("Trying to apply ImpossibleOperator...");
            //throw new InvalidOperationException(); 
        }
        public override void DiscardOperator()
        {
            foreach (Order f in failedOrders) state.notPlannedOrders.Enqueue(f);
        }
    }
    #endregion

}
