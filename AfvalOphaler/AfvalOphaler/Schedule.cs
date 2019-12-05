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
        public Day[,] days;
        public Stack<Order> bestRatioedOrders;
        public Queue<Order> notPlannedOrders;

        public double totalTime;
        public double CalculateTotalTime()
        {
            double total = 0;
            foreach (Day d in days) total += d.totalTime;
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

        public Schedule(List<Order> orders)
        {
            orders.Sort((a, b) => a.Score.CompareTo(b.Score));
            days = new Day[5, 2];
            rnd = new Random();

            bestRatioedOrders = new Stack<Order>(orders);
            notPlannedOrders = new Queue<Order>();
        }

        public Schedule Clone()
        {      
            return this; //Hey jochie
        }

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
            Order bestNotPicked = s.bestRatioedOrders.Pop();

            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            Node whereToAdd;
            double bestDeltaTime;
            int bestLoopIndex;
            // ASAP
            int[][] combis = GD.AllowedDayCombinations[bestNotPicked.Frequency];
            bool dayFound = false;
            int[] days = new int[bestNotPicked.Frequency];
            int[] trucks = new int[bestNotPicked.Frequency];
            double[] deltas = new double[bestNotPicked.Frequency];
            for (int c = 0; c < combis.Length; c++)
            {

                for (int d = 0; d < combis[c].Length; d++)
                {
                    for (int t = 0; t < 2; t++)
                    {
                        if (s.days[d, t].EvaluateAddition())
                    }
                }
            }
            for (int d = 0; d < 5; d++) 
            { 
                int truck = 0; 
                if (s.days[d, truck].EvaluateAddition(bestNotPicked, out whereToAdd, out bestDeltaTime, out bestLoopIndex) || s.days[d, ++truck].EvaluateAddition(bestNotPicked, out whereToAdd, out bestDeltaTime, out bestLoopIndex)) return new AddResult(s, bestNotPicked, whereToAdd, bestLoopIndex, d, truck, bestDeltaTime); 
            }
            /*
                if (s.days[d, 0].EvaluateAddition(bestNotPicked, out whereToAdd, out bestDeltaTime, out bestLoopIndex))
                {
                    return new AddResult(s, bestNotPicked, whereToAdd, bestLoopIndex, d, 0, bestDeltaTime);
                }
                else if (s.days[d, 1].EvaluateAddition(bestNotPicked, out whereToAdd, out bestDeltaTime, out bestLoopIndex))
                {
                    return new AddResult(s, bestNotPicked, whereToAdd, bestLoopIndex, d, 1, bestDeltaTime);
                }              
            }
            */
            return new ImpossibleResult(s, 0, new List<Order>() { bestNotPicked });
            // BEST
            /*
            bestDeltaTime = double.MaxValue;
            bool dayFound = false;
            int bestDay;
            int bestTruck
            for (int d = 0; d < 5; d -= -1)
            {
                double delta;
                int loop;
                int truck = 0;
                if (s.days[d, 0].EvaluateAddition(bestNotPicked, out whereToAdd, out delta, out loop))
                {
                    
                }
                if (s.days[d, 1].EvaluateAddition(bestNotPicked, out whereToAdd, out delta, out loop))
                {
                    
                }
            }
            if (day
            */
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

        public override string ToString()
        {
            return $"Total time: {CalculateTotalTime()}, Total Penalty: {CalculateTotalPenalty()}";
        }

        public string ToCheckString()
        {
            return "Hey Jochie";
        }

    }

    public class Day
    {
        public List<Loop> Loops;
        public double TimeLeft;

        public List<int> loops;
        public double totalTime;

        public Day()
        {
            Loops = new List<Loop>() { new Loop() };
            TimeLeft = 720;
        }

        public bool EvaluateAddition(Order order, out Node bestNode, out double bestDeltaTime, out int bestLoop)
        {
            bestNode = null;
            bestDeltaTime = double.MaxValue;
            bestLoop = -1;

            int l = Loops.Count;
            for(int i = 0; i < l; i++)
            {
                Loop loop = Loops[i];
                if (loop.EvaluateOptimalAddition(order, out Node lOpt, out double _, out double lTd))
                {
                    if (TimeLeft > bestDeltaTime)
                    {
                        if (lTd < bestDeltaTime)
                        {
                            bestNode = lOpt;
                            bestDeltaTime = lTd;
                            bestLoop = i;
                        }
                    }
                }
            }

            if (bestLoop == -1) return false;
            return true;
        }

        public Node AddOrderToLoop(Order order, Node nextTo, int loopIndex)
        {
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

        public Node Start; //Order references to Dump

        public Loop()
        {
            Start = new Node();
            Duration = 30;              //Het storten moet één keer per Loop (lus) gebeuren
            RoomLeft = 20000;       //Gecomprimeerd
        }

        public bool EvaluateOptimalAddition(Order order, out Node opt, out double newRoomLeft, out double td)
        {
            td = 0;
            opt = null;
            newRoomLeft = (order.NumContainers * order.VolPerContainer) * 0.2; //Het gecomprimeerde gewicht dat erbij zou komen

            if (newRoomLeft > RoomLeft) return false;    //Toevoegen zal de gewichtsconstraint schenden

            int best = GD.JourneyTime[order.MatrixId, Start.Data.MatrixId];
            opt = Start;
            Node curr = Start.Next;
            while (!curr.IsDump)
            {
                int t = GD.JourneyTime[order.MatrixId, curr.Data.MatrixId];
                if (t < best)
                {
                    opt = curr;
                    best = t;
                }
                curr = curr.Next;
            }

            int tNext = GD.JourneyTime[order.MatrixId, opt.Next.Data.MatrixId];
            int tPrev = GD.JourneyTime[order.MatrixId, opt.Prev.Data.MatrixId];

            if (tPrev < tNext) opt = opt.Prev;

            td = order.TimeToEmpty
                + GD.JourneyTime[opt.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, opt.Next.Data.MatrixId]
                - GD.JourneyTime[opt.Data.MatrixId, opt.Next.Data.MatrixId];

            return true;
        }

        public Node AddOrder(Order order, Node nextTo)
        {
            Node n = nextTo.AppendNext(order);
            Duration += order.TimeToEmpty
                + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId];

            RoomLeft -= order.NumContainers * order.VolPerContainer * 0.2;
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
        public override void ApplyOperator() { throw new InvalidOperationException(); }
        public override void DiscardOperator()
        {
            foreach (Order f in failedOrders) state.notPlannedOrders.Enqueue(f);
        }
    }


}
