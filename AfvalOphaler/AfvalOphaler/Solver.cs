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

            LocalSolver solver = new HillClimbLocalSolver();
            //LocalSolver solver = new SaLocalSolver(0.5, 0.9999);
            solver.Init();
            Parallel.ForEach(startSchedules, s => { DoSolving(s, maxIterations, opCount, maxNoChange, solver); });
        }

        void DoSolving(Schedule state, int maxIterations, int opCount, int maxNoChange, LocalSolver solver)
        {
            int noChange = 0;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                List<NeighborResult> results = new List<NeighborResult>(opCount);
                for (int i = 0; i < opCount; i++)
                {
                    Func<Schedule, NeighborResult> op = Schedule.neighborOperators[0];
                    NeighborResult res = op(state); // <- { AddResult, ImpossibleResult }

                    results.Add(res);
                }
                if (!solver.ApplyAccordingly(results))
                {
                    noChange++;
                }
                else noChange = 0;
                if (noChange > maxNoChange) break;
                if (iter % 10000 == 0)
                {
                    Console.WriteLine(iter);
                    //Schedule.deleteOperator(state);
                }
            }
            lock(addlock) { AddScheduleToTop(state); }
        }   

        private readonly object addlock = new object();
        void AddScheduleToTop(Schedule s)
        {
            Console.WriteLine("Adding schedule to top: " + s.CalculateScore());
            double s_score = s.CalculateScore();
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

    public abstract class LocalSolver
    {
        public abstract void Init();

        public abstract bool ApplyAccordingly(List<NeighborResult> results);
    }

    public class HillClimbLocalSolver : LocalSolver
    {
        public override void Init()
        {
            // Hé jochie
        }

        public override bool ApplyAccordingly(List<NeighborResult> results)
        {
            int bestIndex = -1;
            double bestdelta = double.MaxValue;
            for (int i = 0; i < results.Count; i++)
            {
                if (!(results[i] is ImpossibleResult) && results[i].GetTotalDelta() < bestdelta)
                {
                    bestIndex = i;
                    bestdelta = results[i].GetTotalDelta();
                }
            }

            if (bestIndex == -1) return false;
            results[bestIndex].ApplyOperator();
            return true;
        }
    }

    public class SaLocalSolver : LocalSolver
    {
        public readonly double cs;
        public readonly double a;

        private double c;
        private Random rnd;
        public SaLocalSolver(double cs, double a)
        {
            this.cs = cs;
            this.a = a;
        }

        public override void Init()
        {
            c = cs;
            rnd = new Random();
        }

        public override bool ApplyAccordingly(List<NeighborResult> results)
        {
            bool applied = false;
            int i = 0;
            for(; i < results.Count; i++)
            {
                var curr = results[i];
                if (curr is ImpossibleResult) continue;
                double delta = curr.GetTotalDelta();
                if (delta < 0)
                {
                    curr.ApplyOperator();
                    applied = true;
                    break;
                }
                else
                {
                    double p = Prob(delta, c);
                    double r = rnd.NextDouble();

                    if (p > r)
                    {
                        curr.ApplyOperator();
                        applied = true;
                        break;
                    }
                }
            }
            c *= a;
            return applied;
        }

        static double Prob(double delta, double temp) => Math.Exp(-delta / temp);
    }
}


