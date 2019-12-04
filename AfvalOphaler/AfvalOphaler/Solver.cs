using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace AfvalOphaler
{
    class Solver
    {
        Schedule startSchedule;
        Schedule[] top10Schedules;
        int threads;
        Random rnd;
        public Solver(Schedule _startSchedule, int _threads = 1)
        {
            startSchedule = _startSchedule;
            threads = _threads;
            rnd = new Random();
        }

        public void StartSolving()
        {
            top10Schedules = new Schedule[10];
            var tasks = new Task[threads];
            for (int i = 0; i < threads; i++) tasks[i] = Task.Run(() => DoSolving(startSchedule.Clone(), 0));
            Task.WaitAll(tasks);
        }

        void DoSolving(Schedule state, int iteration, int maxiterations = 1000000, int op_count = 10, int noChangeCount = 0, int maxNoChange = 1000)
        {
            if (iteration == maxiterations) lock (addlock) { AddScheduleToTop(state); }

            // Bepaal op_count operaties die je gaat doen
            NeighborResult[] results = new NeighborResult[op_count];
            double[] opChances = new double[] { 0.25, 0.50, 0.75, 1 };
            for (int i = 0; i < op_count; i++)
            {
                double op = rnd.NextDouble();
                if (op < opChances[0]) results[i] = Schedule.neighborOperators[0](state);
                else if ((opChances[0] < op) && (op < opChances[1])) results[i] = Schedule.neighborOperators[1](state);
                else if ((opChances[1] < op) && (op < opChances[2])) results[i] = Schedule.neighborOperators[2](state);
                else if ((opChances[2] < op) && (op < opChances[3])) results[i] = Schedule.neighborOperators[3](state);
            }

            // Bepaal afhankelijk van zoekalgortime welke je echt doet.
            // Nu ff hillclimb (lekker greedy)
            int bestindex = 0;
            double bestdelta = 0;
            for (int i = 1; i < op_count; i++)
            {
                if (results[i].delta < bestdelta)
                {
                    bestindex = i;
                    bestdelta = results[i].delta;
                }
            }
            if (bestdelta < 0)
            {
                results[bestindex].ApplyOperator();
                DoSolving(state, iteration++, maxiterations, noChangeCount, maxNoChange);
            }
            else if (noChangeCount == maxNoChange)
            {
                DoSolving(state, iteration++, maxiterations, noChangeCount++, maxNoChange);
            }
            else
            {
                lock (addlock)
                {
                    AddScheduleToTop(state);
                }
            }
        }   

        private readonly object addlock = new object();
        void AddScheduleToTop(Schedule s)
        {
            double s_score = s.Score;
            for (int i = 0; i < 10; i++)
                if (s_score > top10Schedules[i].Score)
                {
                    for (int j = 9; j > i; j--) top10Schedules[j] = top10Schedules[j - 1];
                    top10Schedules[i] = s;
                }
        }

        public Schedule GetBestSchedule() { return top10Schedules[0]; }
    }
}
