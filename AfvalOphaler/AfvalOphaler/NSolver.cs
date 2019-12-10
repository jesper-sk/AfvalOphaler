using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NOp = NAfvalOphaler.Schedule.NeighborOperation;
using Order = AfvalOphaler.Order;
using Util = AfvalOphaler.Util;

namespace NAfvalOphaler
{
    class Solver
    {
        #region Variables & Constructor
        private ScheduleResult[] top10;
        public bool UserInterrupt = false;

        private List<Order> orders;
        public Solver(List<Order> orders)
        {
            top10 = new ScheduleResult[10];
            this.orders = orders;
        }
        #endregion

        #region Solving
        public Task<ScheduleResult[]> StartSolving(int threads, int opCount, int maxI, int maxNoChange) => Task.Run(() => Solve(threads, opCount, maxI, maxNoChange));
        private ScheduleResult[] Solve(int threads, int opCount, int maxI, int maxNoChange)
        {
            Task[] tasks = new Task[threads];
            for (int i = 0; i < threads; i++)
            {
                int index = i;
                tasks[index] = Task.Factory.StartNew(() => SolveOne(maxI, opCount, maxNoChange, index));
            }
            Task.WaitAll(tasks);
            return top10;
        }
        private void SolveOne(int maxI, int opCount, int maxNoChange, int TaskID)
        {
            Console.WriteLine($"Task {TaskID} started.");
            Schedule start = new Schedule(orders);
            //LocalSolver solver = new RandomHillClimbLocalSolver(start);
            ScheduleResult best = new ScheduleResult() { Score = double.MaxValue };
            LocalSolver[] solvs = new LocalSolver[]
            {
                new RandomHillClimbLocalSolver(start),
                new SteepestHillClimbLocalSolver(start)
            };
            foreach (LocalSolver ss in solvs) ss.Init();
            int s = 0;
            //Console.WriteLine(start.GetStatistics());

            bool stop = false;
            int i = 0;
            int noChange = 0;
            while (!stop)
            {
                LocalSolver solver = solvs[s];
                if (solver.GetNext(new double[] { 1, 0, 0 }, opCount)) //Add, Delete, Transfer
                {
                    noChange = 0;
                    if (solver.schedule.Score < best.Score)
                    {                     
                        best = solver.schedule.ToResult();
                        lock (addlock) AddScheduleToTop(best);
                    }
                }
                else
                {
                    noChange++;
                }
                if (i % 100 == 0) for (int opt = 0; opt < 10; opt++) solver.schedule.OptimizeAllDays();
                if (i % 10000 == 0) Console.WriteLine($"Task {TaskID} on iteration: {i}");
                if (i % 5000 == 0) s = 1 - s;
                stop = noChange == maxNoChange
                    || ++i == maxI
                    || UserInterrupt;
            }
            Console.WriteLine($"Task {TaskID} done.");
        }
        #endregion

        #region OLD
        /*
        public Task[] nStartSolving(int maxIterations, int opCount, int maxNoChange)
        {
            LocalSolver solver = new GreedyHillClimbLocalSolver();
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

        private void nDoSolving(Schedule state, int maxIterations, int opCount, int maxNoChange, int maxNoChangeAdd, LocalSolver solver)
        {
            int noChange = 0;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                Console.WriteLine($"Iteration {iter}...");
                //if (iter % 1000 == 0) Console.WriteLine($"Iteration {iter}...");
                
                bool change = solver.GenerateNextState(state);

                if (!change)
                {
                    if (noChange >= maxNoChange) break;
                    else noChange++;
                }
            }
            Console.WriteLine("Solving done");
            lock (addlock) { AddScheduleToTop(state); }
        }*/
        #endregion

        #region LeaderBoard
        private readonly object addlock = new object();
        void AddScheduleToTop(ScheduleResult s)
        {
            //Console.WriteLine("Pushing schedule to ranking: " + s.Score);
            double s_score = s.Score;
            for (int i = 0; i < 10; i++)
                if (top10[i] == null) top10[i] = s;
                else if (s_score < top10[i].Score)
                {
                    for (int j = 9; j > i; j--) top10[j] = top10[j - 1];
                    top10[i] = s;
                }
        }
        #endregion

    }

    #region LocalSolvers
    abstract class LocalSolver
    {
        public readonly Schedule schedule;
        public LocalSolver(Schedule s)
        {
            schedule = s;
        }

        public abstract void Init();
        public abstract bool GetNext(double[] probDist, int nOps);
    }

    #region Steepest HillClimb : Apply best successor of n random neighbors.
    class SteepestHillClimbLocalSolver : LocalSolver
    {
        public SteepestHillClimbLocalSolver(Schedule s) : base(s)
        {      
        }

        public override void Init()
        {
        }

        public override bool GetNext(double[] probDist, int nOps)
        {
            var ops = schedule.GetOperations(probDist, nOps);
            NOp best = null;
            //double opt = double.MaxValue;
            double opt = 0;
            for(int i = 0; i < nOps; i++)
            {
                //Console.WriteLine($"operation: {ops[i]}");
                if (ops[i].Evaluate())
                {
                    double delta = ops[i].TotalDelta;
                    //Console.WriteLine($"Evaluated, delta = {delta}");
                    if (delta < opt)
                    {
                        best = ops[i];
                        opt = delta;
                    }
                }
            }
            if (best == null)
            {
                //Console.WriteLine("none evaluated...");
                return false;
            }
            //Console.WriteLine($"\n{schedule.GetStatistics()}");
            best.Apply();
            return true;
        }
    }
    #endregion

    #region Random HillClimb : Apply random operation, take successor if score decreases.
    class RandomHillClimbLocalSolver : LocalSolver
    {
        Random rnd;
        public RandomHillClimbLocalSolver(Schedule s) : base(s)
        {
        }

        public override void Init()
        {
            rnd = new Random();
        }

        public override bool GetNext(double[] probDist, int nOps)
        {
            List<NOp> ops = new List<NOp>(schedule.GetOperations(probDist, nOps));
            while (ops.Count != 0)
            {
                int i = rnd.Next(0, ops.Count);
                if (ops[i].Evaluate() && ops[i].TotalDelta < 0)
                {
                    ops[i].Apply();
                    return true;
                }
                else
                {
                    ops.RemoveAt(i);
                }
            }
            return false;
        }
    }
    #endregion

    #region Simulated Annealing
    class SaLocalSolver : LocalSolver
    {
        public readonly double cs;
        public readonly double a;

        private double c;
        private Random rnd;

        public SaLocalSolver(Schedule s, double cs, double a) : base(s)
        {
            this.cs = cs;
            this.a = a;
        }

        public override void Init()
        {
            c = cs;
            rnd = new Random();
        }

        public override bool GetNext(double[] probDist, int nOps)
        {
            List<NOp> ops = new List<NOp>(schedule.GetOperations(probDist, nOps));
            while (ops.Count != 0)
            {
                int i = rnd.Next(0, ops.Count);
                NOp op = ops[i];
                ops.RemoveAt(i);
                if (op.Evaluate())
                {
                    double delta = op.TotalDelta;
                    if (delta < 0)
                    {
                        op.Apply();
                        return true;
                    }
                    else
                    {
                        double p = Prob(delta, c);
                        double r = rnd.NextDouble();

                        if (p > r)
                        {
                            op.Apply();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        static double Prob(double delta, double temp) => Math.Exp(-delta / temp);
    }
    #endregion

    #endregion

}
