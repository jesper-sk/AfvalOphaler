using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NOp = AfvalOphaler.Schedule.NeighborOperation;

namespace AfvalOphaler
{
    class Solver
    {
        #region Variables & Constructor
        //private ScheduleResult[] top10;
        public bool UserInterrupt = false;

        private List<Order> orders;
        public Solver(List<Order> orders, bool sBest = false)
        {
            Application.EnableVisualStyles();
            //top10 = new ScheduleResult[10];
            best = new ScheduleResult() { Score = double.MaxValue };
            this.orders = orders;
        }
        #endregion

        #region Solving
        private int[] iterationCounters;
        private ScheduleResult[] bestResults;
        public Task<ScheduleResult> StartSolving(int threads, int opCount, int maxI, int maxNoChange) => Task.Run(() => Solve(threads, opCount, maxI, maxNoChange));
        private ScheduleResult Solve(int threads, int opCount, int maxI, int maxNoChange)
        {
            Task[] tasks = new Task[threads];
            statii = new SolverStatus[threads];
            iterationCounters = new int[threads];
            bestResults = new ScheduleResult[threads];
            for (int i = 0; i < threads; i -= -1) bestResults[i] = new ScheduleResult() { Score = double.MaxValue };

            Task statusUpdater = Task.Factory.StartNew(() => StatusUpdater(threads));
            for (int i = 0; i < threads; i++)
            {
                int index = i;
                tasks[index] = Task.Factory.StartNew(() => SolveOne(maxI, opCount, maxNoChange, index, ref bestResults[index]));
            }
            Task.WaitAll(tasks);
            stopStatusUpdater = true;
            statusUpdater.Wait();
            return best;
        }

        private void SolveOne(int maxI, int opCount, int maxNoChange, int taskID, ref ScheduleResult best)
        {
            Schedule start = new Schedule(orders);

            int i;
            LocalSolver[] solvs;
            int s;

            #region AddOperation
            statii[taskID] = SolverStatus.Doing_Add;
            int maxAddI = 30000;
            int maxNoChangeAdd = 3000;
            solvs = new LocalSolver[]
            {
                new RandomHillClimbLocalSolver(start),
                new SaLocalSolver(start, 0.6, 0.99999)
            };
            foreach (LocalSolver ls in solvs) ls.Init();
            s = 0;

            bool stopAdd = false;
            i = 0; int noChangeAdd = 0;
            while (!stopAdd)
            {
                LocalSolver solver = solvs[s];
                double[] probs = new double[] { 7 / 8.0, 1 / 8.0, 0 };
                if (solver.GetNext(probs, opCount)) //Add, Delete, Transfer
                {
                    noChangeAdd = 0;
                    if (solver.schedule.Score < best.Score)
                    {
                        best = solver.schedule.ToResult();
                        //AddScheduleToTop(best);
                    }
                }
                else noChangeAdd++;

                if (i % 500 == 0) for (int opt = 0; opt < 25; opt++) solver.schedule.OptimizeAllDays();
                if (i % 10000 == 0) iterationCounters[taskID] = i;
                if (i % 15000 == 0) s = 1 - s;
                stopAdd = noChangeAdd == maxNoChangeAdd
                    || ++i == maxAddI
                    || UserInterrupt;
            }
            #endregion

            #region AllOperations
            statii[taskID] = SolverStatus.Doing_All;
            solvs = new LocalSolver[]
            {
                new RandomHillClimbLocalSolver(start),
                new SaLocalSolver(start, 1, 0.99999)
            };
            foreach (LocalSolver ls in solvs) ls.Init();
            s = 1;

            bool stop = false;
            i = 0; int noChange = 0;
            while (!stop)
            {
                LocalSolver solv = solvs[s];
                //double[] probs = new double[] { 1, 0, 0 };
                double[] probs = new double[] { 1 / 9.0, 6 / 9.0, 2 / 9.0 };
                if (solv.GetNext(probs, opCount)) //Add, Delete, Transfer
                {
                    noChange = 0;
                    if (solv.schedule.Score < best.Score)
                    {
                        best = solv.schedule.ToResult();
                        //AddScheduleToTop(best);
                    }
                }
                else noChange++;
                if (i % 1000 == 0) for (int opt = 0; opt < 50; opt++) solv.schedule.OptimizeAllDays();
                if (i % 10000 == 0) iterationCounters[taskID] = i;
                //if (i % 10000 == 0) s = 1 - s;
                stop = noChange == maxNoChange
                    || ++i == maxI
                    || UserInterrupt;
            }
            #endregion

            //UpdateStatus(taskID, $"Done. Best: {best.Score}");
            statii[taskID] = SolverStatus.Done;

            #region Old
            ////Console.WriteLine($"Task {TaskID} started.");
            //Schedule start = new Schedule(orders);
            ////LocalSolver solver = new RandomHillClimbLocalSolver(start);
            //ScheduleResult best = new ScheduleResult() { Score = double.MaxValue };
            //LocalSolver[] solvs = new LocalSolver[]
            //{
            //    //new SaLocalSolver(start, 0.99, 0.999999),
            //    new RandomHillClimbLocalSolver(start),
            //    new SaLocalSolver(start, 0.6, 0.99999)
            //};
            //foreach (LocalSolver ss in solvs) ss.Init();
            //int s = 0;

            //bool stop = false;
            //int i = 0;
            //int noChange = 0;
            //while (!stop)
            //{
            //    LocalSolver solver = solvs[s];
            //    //double[] probs = new double[] { 1, 0, 0 };
            //    double[] probs = new double[] { 7 / 8.0, 1 / 8.0, 0 };
            //    if (solver.GetNext(probs, opCount)) //Add, Delete, Transfer
            //    {
            //        noChange = 0;
            //        if (solver.schedule.Score < best.Score)
            //        {                     
            //            best = solver.schedule.ToResult();
            //            AddScheduleToTop(best);
            //        }
            //    }
            //    else
            //    {
            //        noChange++;
            //    }
            //    if (i % 500 == 0) for (int opt = 0; opt < 25; opt++) solver.schedule.OptimizeAllDays();
            //    if (i % 10000 == 0) UpdateStatus(TaskID, $"Task {TaskID} on iteration: {i}");
            //    if (i % 15000 == 0) s = 1 - s;
            //    stop = noChange == 2500
            //        || ++i == 30000
            //        || UserInterrupt;
            //}
            //stop = false;

            //LocalSolver solv = new SaLocalSolver(start, 0.7, 0.9999);
            //solv.Init();
            ////Console.WriteLine($"Task {TaskID} transfer: {start.Penalty}");
            //while (!stop)
            //{
            //    //double[] probs = new double[] { 1, 0, 0 };
            //    double[] probs = new double[] { 1/9.0, 1 / 9.0, 7 / 9.0 };
            //    if (solv.GetNext(probs, opCount)) //Add, Delete, Transfer
            //    {
            //        noChange = 0;
            //        if (solv.schedule.Score < best.Score)
            //        {
            //            best = solv.schedule.ToResult();
            //            AddScheduleToTop(best);
            //        }
            //    }
            //    else
            //    {
            //        noChange++;
            //    }
            //    if (i % 1000 == 0) for (int opt = 0; opt < 100; opt++) solv.schedule.OptimizeAllDays();
            //    //if (i % 10000 == 0) Console.WriteLine($"Task {TaskID} on iteration: {i}");
            //    stop = noChange == maxI
            //        || ++i == maxI
            //        || UserInterrupt;
            //}
            ////Console.WriteLine($"Task {TaskID} done.");
            #endregion
        }
        #endregion

        #region LeaderBoard / Statii
        private enum SolverStatus
        {
            Not_Initialized,
            Doing_Add,
            Doing_All,
            Done
        }
        private SolverStatus[] statii;
        private ScheduleResult best;

        private bool stopStatusUpdater = false;
        private void StatusUpdater(int threads)
        {
            Console.Clear();
            Stopwatch watch = Stopwatch.StartNew();
            int currBestIndex = -1;
            while (!stopStatusUpdater && !UserInterrupt)
            {           
                for (int i = 0; i < threads; i++)
                {
                    ScheduleResult curr = bestResults[i];
                    if (curr.Score < best.Score)
                    {
                        currBestIndex = i;
                        best = curr;
                    }
                    SolverStatus stat = statii[i];
                    string extraInfo;
                    switch (stat)
                    { // In order of most time on status:
                        case SolverStatus.Doing_All:
                            extraInfo = $", iteration: {iterationCounters[i]}";
                            break;
                        case SolverStatus.Done:
                            extraInfo = $", best: {bestResults[i].Score}";
                            break;
                        case SolverStatus.Doing_Add:
                            extraInfo = $", iteration: {iterationCounters[i]}";
                            break;
                        default: extraInfo = ""; break;
                    }
                    //string extraInfo = (stat == SolverStatus.Done || stat == SolverStatus.Not_Initialized) ? "" : $", iteration: {iterationCounters[i]}";
                    Console.SetCursorPosition(0, i);
                    Console.WriteLine($"Task {i}: {stat}{extraInfo}");
                }
                Console.SetCursorPosition(0, threads);
                Console.WriteLine($"===\nBest result produced on task {currBestIndex}:\n{best.String}");
                Console.WriteLine($"===\nCurrent runtime: {watch.Elapsed}");
                Thread.Sleep(5000);
            }
        }

        private readonly object lockobject = new object();
        void AddScheduleToTop(ScheduleResult s)
        {
            #region Top10 [DEPRICATED]
            ////Console.WriteLine("Pushing schedule to ranking: " + s.Score);
            //double s_score = s.Score;
            //for (int i = 0; i < 10; i++)
            //    if (top10[i] == null) top10[i] = s;
            //    else if (s_score < top10[i].Score)
            //    {
            //        for (int j = 9; j > i; j--) top10[j] = top10[j - 1];
            //        top10[i] = s;
            //    }
            #endregion
            lock (lockobject) 
            {
                if (s.Score < best.Score) best = s;
            }
        }
        void UpdateStatus(int taskId, string update)
        {
            lock (lockobject)
            {
                Console.SetCursorPosition(0, taskId + 1);
                Console.WriteLine($"Task {taskId}: {update}");
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
                if (ops[i].Evaluate() /*&& ops[i].TotalDelta < 0*/)
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
                c *= a;
            }
            return false;
        }
        static double Prob(double delta, double temp) => Math.Exp(-delta / temp);
    }
    #endregion

    #endregion

}
