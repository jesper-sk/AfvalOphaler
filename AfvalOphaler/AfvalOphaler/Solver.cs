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
        Schedule[] startSchedules;
        Schedule[] top10Schedules;
        int threads;
        Random rnd;
        public Solver(Schedule[] _startSchedules, int _threads)
        {
            startSchedules = _startSchedules;
            threads = _threads;
            rnd = new Random();
        }

        public void StartSolving(int maxIterations, int opCount, int maxNoChange)
        {
            top10Schedules = new Schedule[10];
            /*
            var tasks = new Task[threads];            
            for (int i = 0; i < threads; i++)
            {
                tasks[i] = Task.Run(() => DoSolving(startSchedules[i], 0, maxIterations, opCount, 0, maxNoChange));
            }
            Task.WaitAll(tasks);
            */
            Parallel.ForEach(startSchedules, s => { DoSolving(s, 0, maxIterations, opCount, 0, maxNoChange); });
        }

        void DoSolving(Schedule state, int iteration, int maxIterations, int opCount, int noChangeCount, int maxNoChange)
        {
            Console.WriteLine($"Doing iteration {iteration}...");
            if (iteration >= maxIterations) 
            { 
                lock (addlock) { AddScheduleToTop(state); } 
                return; 
            }

            // Bepaal op_count operaties die je gaat doen
            /*
            NeighborResult[] results = new NeighborResult[opCount];
            double[] opChances = new double[] { 0.25, 0.50, 0.75, 1 };
            for (int i = 0; i < op_count; i++)
            {
                double op = rnd.NextDouble();
                if (op < opChances[0]) results[i] = Schedule.neighborOperators[0](state);
                else if ((opChances[0] < op) && (op < opChances[1])) results[i] = Schedule.neighborOperators[1](state);
                else if ((opChances[1] < op) && (op < opChances[2])) results[i] = Schedule.neighborOperators[2](state);
                else if ((opChances[2] < op) && (op < opChances[3])) results[i] = Schedule.neighborOperators[3](state);
            }
            */
            List<NeighborResult> results = new List<NeighborResult>(opCount);
            for (int i = 0; i < opCount; i++)
            {
                //Func<Schedule, NeighborResult> op = Schedule.neighborOperators[0];
                NeighborResult res = Schedule.addOperator(state);
                //Console.WriteLine($"res delta: {res.totalDelta}");
                results.Add(res);
            }

            // Bepaal afhankelijk van zoekalgortime welke je echt doet.
            // Nu ff hillclimb (lekker greedy)
            int bestindex = 0;
            double bestdelta = results[0].totalDelta;
            for (int i = 1; i < opCount; i++)
            {
                if (results[i].totalDelta < bestdelta)
                {
                    bestindex = i;
                    bestdelta = results[i].totalDelta;
                }
            }

            // Apply best operator, discard the rest;
            results[bestindex].ApplyOperator();
            int j = 0;
            while (j < bestindex) { results[j].DiscardOperator(); j++; }
            j++;
            while (j < opCount) { results[j].DiscardOperator(); j++; }

            //Console.WriteLine($"State after iteration: time={state.CalculateTotalTime()}, penaly={state.CalculateTotalPenalty()}");
            Console.WriteLine("---");
            iteration++;
            DoSolving(state, iteration, maxIterations, opCount, noChangeCount, maxNoChange);

            /*
            if (bestdelta < 0)
            {
                // Apply best operator, discard the rest;
                results[bestindex].ApplyOperator();
                int i = 0;
                while (i < bestindex) { results[i].DiscardOperator(); i++; }
                i++;
                while (i < opCount) { results[i].DiscardOperator(); i++; }

                DoSolving(state, iteration++, maxIterations, opCount, noChangeCount, maxNoChange);
            }
            else if (noChangeCount < maxNoChange)
            {
                DoSolving(state, iteration++, maxIterations, opCount, noChangeCount++, maxNoChange);
            }
            else
            {
                lock (addlock)
                {
                    AddScheduleToTop(state);
                }
            }
            */
        }   

        private readonly object addlock = new object();
        void AddScheduleToTop(Schedule s)
        {
            double s_score = s.Score;
            for (int i = 0; i < 10; i++)
                if (top10Schedules[i] == null) top10Schedules[i] = s;
                else if (s_score < top10Schedules[i].Score)
                {
                    for (int j = 9; j > i; j--) top10Schedules[j] = top10Schedules[j - 1];
                    top10Schedules[i] = s;
                }
        }

        public Schedule GetBestSchedule() { return top10Schedules[0]; }
    }
}
