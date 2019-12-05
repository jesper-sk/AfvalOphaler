using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfvalOphaler
{
    class Schedule
    {
        public Day[,] days;
        public double totalTime;
        public Stack<BigLLNode> bestRatioedOrders;

        public double CalculateTotalTime()
        {
            double total = 0;
            foreach (Day d in days) total += d.totalTime;
            return total;
        }
        public double totalPenalty;
        public double CalculateTotalPenalty()
        {
            double total = 0;
            foreach (BigLLNode n in bestRatioedOrders) total += (3 * n.Order.TimeToEmpty);
            return total;
        }
        public double Score { get => totalTime + totalPenalty; }

        public Schedule()
        {
            days = new Day[5, 2];
            rnd = new Random();
        }

        public Schedule Clone()
        {
            return this;
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
            BigLLNode bestnotpicked = s.bestRatioedOrders.Peek();
            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
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

    class Day
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

        public void EvaluateAddition(Order order)
        {
            Node opt = null;

            foreach(Loop loop in Loops)
            {
                if (loop.EvaluateOptimalAddition(order, out Node lOpt, out double roomLeft, out double td))
                {
                    if (TimeLeft > td)
                    {

                    }
                }
            }
        }
    }

    class Loop
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

    class Node
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
        public double delta;
        public NeighborResult(double d)
        {
            delta = d;
        }

        public abstract void ApplyOperator();
    }
    public class AddResult : NeighborResult
    {
        BigLLNode toAdd;
        BigLLNode[] whereToAdd;
        int[] whichLoops;
        public AddResult(double d, BigLLNode t, BigLLNode[] where, int[] which) : base(d)
        {
            toAdd = t;
            whereToAdd = where;
            whichLoops = which;
        }

        public override void ApplyOperator()
        {
            for (int i = 0; i < whereToAdd.Length; i++)
            {
                BigLLNode temp = whereToAdd[i].Loops[i].Next;
                toAdd.Loops[toAdd.Loops.Count - 1].Next = temp;
                whereToAdd[i].Loops[i].Next = toAdd;           
            }
        }
    }
    public class DeleteResult : NeighborResult
    {
        public DeleteResult(double d) : base(d)
        {

        }

        public override void ApplyOperator()
        {
            
        }
    }
    public class TransferResult : NeighborResult
    {
        public TransferResult(double d) : base(d)
        {

        }
        public override void ApplyOperator()
        {

        }
    }
    public class SwapResult : NeighborResult
    {
        public SwapResult(double d) : base(d)
        {

        }
        public override void ApplyOperator()
        {

        }
    }


}
