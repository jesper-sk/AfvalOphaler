using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfvalOphaler
{
    class Schedule
    {
        public Route[,] routes;
        public double totalTime;
        public double CalculateTotalTime()
        {
            double total = 0;
            foreach (Route r in routes) total += r.totalTime;
            return total;
        }
        public double totalPenalty;
        public double CalculateTotalPenalty()
        {
            double total = 0;
            foreach (Route r in routes) total += r.totalPenalty;
            return total;
        }
        public double Score { get => totalTime + totalPenalty; }

        public Schedule()
        {
            routes = new Route[5, 2];
        }

        double GetNeighborDelta()
        {
            return 0;
        }

        public Func<Schedule, double, double>[] neighborOperators = { addOperator, deleteOperator, transferOperator, swapOperator };
        static Func<Schedule, double, double> addOperator = (currState, currTime) => (Add(currState) - currTime);
        static Func<Schedule, double, double> deleteOperator = (currState, currTime) => (Delete(currState) - currTime);
        static Func<Schedule, double, double> transferOperator = (currState, currTime) => (Transfer(currState) - currTime);
        static Func<Schedule, double, double> swapOperator = (currState, currTime) => (Swap(currState) - currTime);
        static double Add(Schedule s)
        {
            return 0;
        }
        static double Delete(Schedule s)
        {
            return 0;
        }
        static double Transfer(Schedule s)
        {
            return 0;
        }
        static double Swap(Schedule s)
        {
            return 0;
        }
    }

    class Route
    {
        public List<Loop> loops;
        public double totalTime;
        public double totalPenalty;

        public Route()
        {
            loops = new List<Loop>();
        }
    }

    class Loop
    {
        public double time;
        public double penalty;

        public Loop()
        {

        }
    }
}
