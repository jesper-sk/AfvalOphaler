using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAfvalOphaler
{
    class NSolver
    {
        Schedule[] startSchedules;
        Schedule[] top10Schedules;
        int threads;
        Random rnd;
        public NSolver(Schedule[] _startSchedules, int _threads)
        {
            startSchedules = _startSchedules;
            threads = _threads;
            rnd = new Random();
        }

        public Task[] StartSolving(int maxIterations, int opCount, int maxNoChange, int maxNoChangeAdd)
        {
            LocalSolver solver = new HillClimbLocalSolver();
            top10Schedules = new Schedule[10];

            var tasks = new Task[threads];
            for (int i = 0; i < threads; i-=-1)
            {
                int index = i;
                tasks[index] = Task.Factory.StartNew(() => DoSolving(startSchedules[index], maxIterations, opCount, maxNoChange, maxNoChangeAdd, solver));
                //tasks[i] = Task.Run(() => DoSolving(startSchedules[i], 0, maxIterations, opCount, 0, maxNoChange));
            }
            return tasks;
        }

        private void DoSolving(Schedule state, int maxIterations, int opCount, int maxNoChange, int maxNoChangeAdd, LocalSolver solver)
        {
            int noChange = 0;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                Console.WriteLine($"Iteration {iter}...");
                //if (iter % 1000 == 0) Console.WriteLine($"Iteration {iter}...");

                List<NeighborResult> results = new List<NeighborResult>(opCount);
                for (int o = 0; o < opCount; o++)
                {

                }


                bool change = solver.GenerateNextState(state, results);

                if (!change)
                {
                    if (noChange >= maxNoChange) break;
                    else noChange++;
                }
            }
            Console.WriteLine("Solving done");
            lock (addlock) { AddScheduleToTop(state); }
        }

        private readonly object addlock = new object();
        void AddScheduleToTop(Schedule s)
        {
            Console.WriteLine("Pushing schedule to ranking: " + s.Score);
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

    abstract class LocalSolver
    {
        public LocalSolver()
        {

        }
        public abstract bool GenerateNextState(Schedule state, List<NeighborResult> neighbors);
    }

    class HillClimbLocalSolver : LocalSolver
    {
        public HillClimbLocalSolver() : base()
        {

        }

        public override bool GenerateNextState(Schedule state, List<NeighborResult> neighbors)
        {
            neighbors[0].Apply(state);
            return true;
        }
    }
}
