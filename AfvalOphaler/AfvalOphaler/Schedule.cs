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
            rnd = new Random();
        }

        Random rnd;
        public Func<Schedule, double, Schedule>[] neighborOperators = { addOperator, deleteOperator, transferOperator, swapOperator };
        static Func<Schedule, double, Schedule> addOperator = (currState, currTime) => Add(currState);
        static Func<Schedule, double, Schedule> deleteOperator = (currState, currTime) => Delete(currState);
        static Func<Schedule, double, Schedule> transferOperator = (currState, currTime) => Transfer(currState);
        static Func<Schedule, double, Schedule> swapOperator = (currState, currTime) => Swap(currState);
        static Schedule Add(Schedule s)
        {
            // Pak node die nog niet in loop zit en beste afstand/tijd ratio heeft
            // voeg deze node aan dichtstbijzijnde onverzadigde loop toe
            return new Schedule();
        }
        static Schedule Delete(Schedule s)
        {
            // Pak willekeurige node in een loop
            // verwijder deze
            return new Schedule();
        }
        static Schedule Transfer(Schedule s)
        {
            // Pak een willekeurige node in een loop
            // voeg deze toe aan de dichtsbijzijnde andere loop
            return new Schedule();
        }
        static Schedule Swap(Schedule s)
        {
            // Pak twee willekeurige nodes uit twee verschillende loops.
            // swap deze
            return new Schedule();
        }
    }

    class Route
    {
        public List<BigLL> loops;
        public double totalTime;
        public double totalPenalty;

        public Route()
        {
            loops = new List<BigLL>();
        }
    }
}
