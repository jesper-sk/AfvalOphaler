using System;
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
        public Stack<Node> bestRatioedOrders;

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
            foreach (Node n in bestRatioedOrders) total += (3 * n.Order.TimeToEmpty);
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
            Node bestnotpicked = s.bestRatioedOrders.Peek();
            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            Node[] nearest = new Node[5];

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
        public List<int> loops;
        public double totalTime;

        public Day()
        {
            loops = new List<int>();
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
        Node toAdd;
        Node[] whereToAdd;
        int[] whichLoops;
        public AddResult(double d, Node t, Node[] where, int[] which) : base(d)
        {
            toAdd = t;
            whereToAdd = where;
            whichLoops = which;
        }

        public override void ApplyOperator()
        {
            for (int i = 0; i < whereToAdd.Length; i++)
            {
                Node temp = whereToAdd[i].Loops[i].Next;
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
