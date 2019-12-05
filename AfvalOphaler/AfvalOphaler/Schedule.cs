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

        public Schedule()
        {
            days = new Day[5, 2];
            rnd = new Random();
        }

        public Schedule Clone()
        {      
            return this; //Hey jochie
        }

        #region Neighbor Operations

        Random rnd;
        public static Func<Schedule, NeighborResult>[] neighborOperators = { addOperator, deleteOperator, transferOperator, swapOperator };
        public static Func<Schedule, AddResult> addOperator = (currState) => Add(currState);
        public static Func<Schedule, DeleteResult> deleteOperator = (currState) => Delete(currState);
        public static Func<Schedule, TransferResult> transferOperator = (currState) => Transfer(currState);
        public static Func<Schedule, SwapResult> swapOperator = (currState) => Swap(currState);
        static AddResult Add(Schedule s)
        {
            // Pak node die nog niet in loop zit en beste afstand/tijd ratio heeft
            Order bestnotpicked = s.bestRatioedOrders.Pop();
            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            
            // ASAP
            /* foreach day
                if (day.evaluateAddition())
                    return new AddResult(true)
               if (nodayfound)
                return new AddResult(false)
            */
            // BEST

            BigLLNode[] nearest = new BigLLNode[5];

            throw new NotImplementedException();
        }
        static DeleteResult Delete(Schedule s)
        {
            // Pak willekeurige node in een loop
            // verwijder deze
            throw new NotImplementedException();
        }
        static TransferResult Transfer(Schedule s)
        {
            // Pak een willekeurige node in een loop
            // voeg deze toe aan de dichtsbijzijnde andere loop
            throw new NotImplementedException();
        }
        static SwapResult Swap(Schedule s)
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

        public void AddOrderToLoop(Order order, Node nextTo, int loop)
        {

        }
    }

    public class Loop
    {
        //LinkedList van nodes

        public double Time;
        public double RoomLeft;

        public Node Start; //Order references to Dump

        public Loop()
        {
            Start = new Node();
            Time = 30;              //Het storten moet één keer per Loop (lus) gebeuren
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

        public void AddOrder(Order order, Node nextTo)
        {
            nextTo.AppendNext(order);
            Time += order.TimeToEmpty
                + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId];

            RoomLeft -= order.NumContainers * order.VolPerContainer * 0.2;
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
        public double delta;
        public NeighborResult(Schedule s, double d)
        {
            state = s;
            delta = d;
        }

        public abstract void ApplyOperator();
        public abstract void DiscardOperator();
    }
    public class AddResult : NeighborResult
    {
        Order toAdd;
        public AddResult(Schedule s, double d, Order o) : base(s, d)
        {
            toAdd = o;
        }

        public override void ApplyOperator()
        {

        }
        public override void DiscardOperator()
        {
            state.bestRatioedOrders.Push(toAdd);
        }
    }
    public class DeleteResult : NeighborResult
    {
        public DeleteResult(Schedule s, double d) : base(s, d)
        {

        }

        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class TransferResult : NeighborResult
    {
        public TransferResult(Schedule s, double d) : base(s, d)
        {

        }
        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }
    public class SwapResult : NeighborResult
    {
        public SwapResult(Schedule s, double d) : base(s, d)
        {

        }
        public override void ApplyOperator() { Console.WriteLine("Hey Jochie"); }
        public override void DiscardOperator() { Console.WriteLine("Hey Jochie"); }
    }

    public class ImpossibleResult : NeighborResult
    {
        List<Order> failedOrders;
        public ImpossibleResult(Schedule s, double d, List<Order> failed) : base(s, d)
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
