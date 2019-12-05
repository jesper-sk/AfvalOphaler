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
            solver.Init();
            Parallel.ForEach(startSchedules, s => { DoSolving(s, 0, maxIterations, opCount, 0, maxNoChange, solver); });
        }

        void DoSolving(Schedule state, int iteration, int maxIterations, int opCount, int noChangeCount, int maxNoChange, LocalSolver solver)
        {
            Console.WriteLine("---");
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
                Func<Schedule, NeighborResult> op = Schedule.neighborOperators[0];
                NeighborResult res = op(state); // <- { AddResult, ImpossibleResult }

                //NeighborResult res = Schedule.addOperator(state);
                //Console.WriteLine($"res delta: {res.totalDelta}");
                results.Add(res);
            }

            if (solver.ApplyAccordingly(results))
            {
                DoSolving(state, ++iteration, maxIterations, opCount, noChangeCount, maxNoChange, solver);
            }
            else if (noChangeCount < maxNoChange) DoSolving(state, ++iteration, maxIterations, opCount, ++noChangeCount, maxNoChange, solver);
            else
            {
                lock (addlock) { AddScheduleToTop(state); }
                return;
            }

            /*// Bepaal afhankelijk van zoekalgortime welke je echt doet.
            // Nu ff hillclimb (lekker greedy)
            int bestindex = -1;
            double bestdelta = double.MaxValue;
            for (int i = 0; i < opCount; i++)
            {
                if (!(results[i] is ImpossibleResult) && results[i].GetTotalDelta() < bestdelta)
                {
                    bestindex = i;
                    bestdelta = results[i].GetTotalDelta();
                }
            }


            // Apply best operator, discard the rest;
            if (bestindex != -1 && results[bestindex].GetTotalDelta() < 0)
            {
                noChangeCount = 0;
                Console.WriteLine("Applying operator: " + bestindex);
                results[bestindex].ApplyOperator();
                int j = 0;
                while (j < bestindex) { results[j].DiscardOperator(); j++; }
                j++;
                while (j < opCount) { results[j].DiscardOperator(); j++; }
                DoSolving(state, ++iteration, maxIterations, opCount, noChangeCount, maxNoChange);
            }
            else if (noChangeCount < maxNoChange)
            {
                Console.WriteLine("Nog Change");
                foreach (NeighborResult res in results) res.DiscardOperator();
                DoSolving(state, ++iteration, maxIterations, opCount, ++noChangeCount, maxNoChange);
            }
            else
            {
                Console.WriteLine("To long no change, stopping search");
                lock (addlock) { AddScheduleToTop(state); }
                return;
            }*/
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

            for(int i = results.Count - 1; i > bestIndex; i--)
            {
                results[i].DiscardOperator();
            }
            for(int i = bestIndex - 1; i >= 0; i--)
            {
                results[i].DiscardOperator();
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


