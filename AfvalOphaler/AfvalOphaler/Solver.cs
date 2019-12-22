using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static AfvalOphaler.GD;
using NOp = AfvalOphaler.Schedule.NeighborOperation;


namespace AfvalOphaler
{
    class Solver
    {
        #region Variables & Constructor
        public bool UserInterrupt = false;  // Indicates if user has pressed the interrupt key

        private List<Order> orders;
        /// <summary>
        /// Constructor, initializes some variables.
        /// </summary>
        /// <param name="orders"></param>
        public Solver(List<Order> orders)
        {
            best = new ScheduleResult() { Score = double.MaxValue };
            this.orders = orders;
        }
        #endregion

        #region Solving
        private int[] iterationCounters;        // Contains for each solver task the current iteration count
        private ScheduleResult[] bestResults;   // Contains for each solver task the current best schedule
        /// <summary>
        /// Main entry point to the solver.
        /// Starts the main Solve method and returns it as task with result.
        /// </summary>
        /// <returns>Solve as task with a scheduleresult that contains the best schedule as result</returns>
        public Task<ScheduleResult> StartSolving() => Task.Run(() => Solve());
        /// <summary>
        /// Starts the actual solvers, returns the best schedule when all solvers are done.
        /// </summary>
        /// <returns>The best schedule when all solvers are done</returns>
        private ScheduleResult Solve()
        {
            // Initialize all variables:
            Task[] tasks = new Task[ThreadCount];
            stati = new SolverStatus[ThreadCount];
            iterationCounters = new int[ThreadCount];
            bestResults = new ScheduleResult[ThreadCount];
            for (int i = 0; i < ThreadCount; i -= -1) bestResults[i] = new ScheduleResult() { Score = double.MaxValue };

            // Start the statusupdater and leaderboard:
            Task statusUpdater = Task.Factory.StartNew(() => StatusUpdater());
            // Start the solvers:
            for (int i = 0; i < ThreadCount; i++)
            {
                int index = i;
                tasks[index] = Task.Factory.StartNew(() => SolveOne(index, ref bestResults[index]));
            }
            // Wait untill all solvers are done:
            Task.WaitAll(tasks);
            // Stop the statusupdater:
            stopStatusUpdater = true;
            statusUpdater.Wait();
            // Return the best schedule:
            return best;
        }

        /// <summary>
        /// Main method for each solver task.
        /// In each iteration it generates opCount neighbors using the following operators:
        /// - Add
        /// - Delete
        /// - Transfer
        /// - Swap
        /// (for explanations see the Schedule class)
        /// Then it uses a LocalSolver to pick the next state from the neighbors,
        /// since each local solving algorithm uses a different method to determine the next state.
        /// </summary>
        /// <param name="taskID">The id of the task, used as index for the arrays</param>
        /// <param name="best">The best schedule of this solver task</param>
        private void SolveOne(int taskID, ref ScheduleResult best)
        {
            // Initialize variables
            Schedule start = new Schedule(orders);
            int i;
            LocalSolver[] solvs;
            int s;

            // We first perform only the addition of orders to an empty schedule
            // This way we end up with filled in schedule that,
            //  depending on the LocalSolver used, can be quite optimised already
            #region AddOperation
            stati[taskID] = SolverStatus.Doing_Add;
            // Initialize some LocalSolvers
            solvs = new LocalSolver[]
            {
                new RandomHillClimbLocalSolver(start),
                new SaLocalSolver(start, 0.6, 0.99999)
            };
            foreach (LocalSolver ls in solvs) ls.Init();
            s = 0;  // Used to choose which LocalSolver is used to get the next state

            bool stopAdd = false;
            i = 0; int noChangeAdd = 0; // Iteration counters
            while (!stopAdd)
            {
                LocalSolver solver = solvs[s];
                double[] probs = new double[] { 7 / 8.0, 1 / 8.0, 0, 0 };
                if (solver.GetNext(probs, OperationCount)) //Add, Delete, Transfer, Swap
                {
                    // The LocalSolver accepted a new state
                    noChangeAdd = 0;
                    // Check if next state is better than current best:
                    if (solver.schedule.Score < best.Score)
                        best = solver.schedule.ToResult();
                }
                else noChangeAdd++;

                if (i % OptimizeInterval == 0) for (int opt = 0; opt < OptimizeIterations; opt++) solver.schedule.OptimizeAllDays();
                if (i % UpdateIterationStatusInverval == 0) iterationCounters[taskID] = i;
                if (i % SwitchSearchAlgorithmInterval_AddPhase == 0) s = 1 - s; // Used to switch LocalSolver
                stopAdd = 
                    noChangeAdd == MaxNoChange_AddPhase
                    || ++i == MaxIterations_AddPhase
                    || UserInterrupt;
            }
            #endregion

            // Once we have a filled in schedule it makes more sense to perform the other operators
            // We generate opCount neighbors using the Add, Delete, Transfer (first delete than add) and Swap
            // Since opCount does not have to be equal to 4 (we have four operators) we use a probability distribution.
            // For each neighbor then with the given chance one of the operators in picked and used to generate a neigbor
            // Once again there is also the possibility to choose the LocalSolver
            //  that picks the next state from the neighbors 
            #region AllOperations
            stati[taskID] = SolverStatus.Doing_All;
            // Initialize some LocalSolvers:
            solvs = new LocalSolver[]
            {
                new RandomHillClimbLocalSolver(start),
                new SaLocalSolver(start, 1, 0.99999)
            };
            foreach (LocalSolver ls in solvs) ls.Init();
            s = 1;

            bool stop = false;
            i = 0; int noChange = 0; // Iteration counters
            while (!stop)
            {
                LocalSolver solv = solvs[s];
                // Try-out probability distributions, goal -> make them dynamic and dependent on iteration count
                //double[] probs = new double[] { 1, 0, 0, 0 }; // Add only
                //double[] probs = new double[] { 0, 0, 0, 1 }; // Swap only
                //double[] probs = new double[] { 2 / 12.0, 5 / 12.0, 5 / 12.0, 0 / 12.0 };
                double[] probs = new double[] { 1 / 12.0, 1 / 12.0, 5 / 12.0, 5 / 12.0 };
                if (solv.GetNext(probs, OperationCount)) //Add, Delete, Transfer, Swap
                {
                    // Solver accepted new state
                    noChange = 0;
                    // Check if next state is better than current best:
                    if (solv.schedule.Score < best.Score)
                        best = solv.schedule.ToResult();
                }
                else noChange++;
                if (i % OptimizeInterval == 0) for (int opt = 0; opt < OptimizeInterval; opt++) solv.schedule.OptimizeAllDays();
                if (i % UpdateIterationStatusInverval == 0) iterationCounters[taskID] = i;
                //if (i % SwitchSearchAlgorithmInterval_AllPhase == 0) s = 1 - s; // Used to switch LocalSolver
                stop = noChange == MaxNoChange_AllPhase
                    || ++i == MaxIterations_AllPhase
                    || UserInterrupt;
            }
            #endregion

            // Solver done, update status accordingly:
            stati[taskID] = SolverStatus.Done;         
        }
        #endregion

        #region LeaderBoard / Stati
        private enum SolverStatus
        {
            Not_Initialized,
            Doing_Add,
            Doing_All,
            Done
        }
        private SolverStatus[] stati;
        private ScheduleResult best;

        private bool stopStatusUpdater = false;
        /// <summary>
        /// Gets the best schedule on a fixed interval
        /// if STATUS is defined:
        /// Prints the status of each solver thread together with
        /// - the current runtime.
        /// - the best score so far.
        /// This is updated each refreshTime miliseconds.
        /// </summary>
        /// <param name="threads">Amount of solvertasks started</param>
        /// <param name="refreshTime">Refresh delay (in miliseconds)</param>
        private void StatusUpdater()
        {
            Console.Clear();
            Stopwatch watch = Stopwatch.StartNew();
            int currBestIndex = -1;

            while(!(stopStatusUpdater || UserInterrupt))
            {

                for (int i = 0; i < ThreadCount; i++)
                {
                    ScheduleResult curr = bestResults[i];
                    if (curr.Score < best.Score)
                    {
                        currBestIndex = i;
                        best = curr;
                    }
                    if (ShowStatus)
                    {
                        SolverStatus stat = stati[i];
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
                        Console.SetCursorPosition(0, i);
                        Console.WriteLine($"Task {i}: {stat}{extraInfo}");
                    }
                }

                if (ShowStatus)
                {
                    Console.SetCursorPosition(0, ThreadCount);
                    Console.WriteLine($"===\nBest result produced on task {currBestIndex}:\n{best.String}");
                    Console.WriteLine($"===\nCurrent runtime: {watch.Elapsed}");
                }

                Thread.Sleep(ConsoleUpdateInterval);
            }

            //Timer updater = new Timer(ConsoleUpdateInterval);
            //updater.Elapsed += Updater_Elapsed;
            //updater.Start();
            //void Updater_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            //{
            //    if (stopStatusUpdater || UserInterrupt) updater.Stop();

            //    for (int i = 0; i < ThreadCount; i++)
            //    {
            //        ScheduleResult curr = bestResults[i];
            //        if (curr.Score < best.Score)
            //        {
            //            currBestIndex = i;
            //            best = curr;
            //        }
            //        if (ShowStatus)
            //        {
            //            SolverStatus stat = stati[i];
            //            string extraInfo;
            //            switch (stat)
            //            { // In order of most time on status:
            //                case SolverStatus.Doing_All:
            //                    extraInfo = $", iteration: {iterationCounters[i]}";
            //                    break;
            //                case SolverStatus.Done:
            //                    extraInfo = $", best: {bestResults[i].Score}";
            //                    break;
            //                case SolverStatus.Doing_Add:
            //                    extraInfo = $", iteration: {iterationCounters[i]}";
            //                    break;
            //                default: extraInfo = ""; break;
            //            }
            //            Console.SetCursorPosition(0, i);
            //            Console.WriteLine($"Task {i}: {stat}{extraInfo}");
            //        }
            //    }

            //    if (ShowStatus)
            //    {
            //        Console.SetCursorPosition(0, ThreadCount);
            //        Console.WriteLine($"===\nBest result produced on task {currBestIndex}:\n{best.String}");
            //        Console.WriteLine($"===\nCurrent runtime: {watch.Elapsed}");
            //    }
            //}
            //// Block while the updater is active:
            //while (updater.Enabled) ;
        }
        #endregion
    }
    
    #region LocalSolvers
    /// <summary>
    /// Main abstract class that implements the LocalSolvers.
    /// </summary>
    abstract class LocalSolver
    {
        public readonly Schedule schedule;
        public LocalSolver(Schedule s)
        {
            schedule = s;
        }

        /// <summary>
        /// Initializes the LocalSolver.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Generates the neighbors of the current state,
        /// and picks the next state according to the way the LocalSolver works that
        ///     overrides this function.
        /// </summary>
        /// <param name="probDist">The probability for each operator</param>
        /// <param name="nOps">Number of neighbors to generate</param>
        /// <returns>True if </returns>
        public abstract bool GetNext(double[] probDist, int nOps);
    }

    #region Steepest HillClimb : Apply best neighbor of n neighbors.
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
            // Create an array of operators that we are going to perform to generate neighbors
            NOp[] ops = schedule.GetOperations(probDist, nOps);

            NOp best = null;
            double opt = 0; // (the best needs to atleast lower the score) 
                    // = double.MaxValue (just find the best schedule)
            // Find the operation the lowers the score the most:
            for (int i = 0; i < nOps; i++)
            {
                // Check if the operator may be performed, without actually performing the operator
                if (ops[i].Evaluate())
                {
                    // Check if the deltascore of this neighbor is better than the current delta
                    double delta = ops[i].TotalDelta;
                    if (delta < opt)
                    {
                        best = ops[i];
                        opt = delta;
                    }
                }
            }
            // No operation found that meets the requirements:
            if (best == null) return false; 

            // Apply the best operation, this does modify the schedule:
            best.Apply();
            return true;
        }
    }
    #endregion

    #region Random HillClimb : Take random neighbor, accept it (if score decreases (optional))
    class RandomHillClimbLocalSolver : LocalSolver
    {
        Random Rand;
        public RandomHillClimbLocalSolver(Schedule s) : base(s)
        {
        }

        public override void Init()
        {
            Rand = new Random();
        }

        public override bool GetNext(double[] probDist, int nOps)
        {
            // Create an array of operators that we are going to perform to generate neighbors
            List<NOp> ops = new List<NOp>(schedule.GetOperations(probDist, nOps));

            // While there are still operations in ops,
            //  take a random operation, 
            //  - Evaluate it, and if it may be performed (AND decreases the score (optional)):
            //      - Apply the operation and return
            //  - Else remove the operator from the list
            while (ops.Count != 0)
            {
                int i = Rand.Next(0, ops.Count);
                if (ops[i].Evaluate() /*&& ops[i].TotalDelta < 0*/)
                {
                    ops[i].Apply();
                    return true;
                }
                else ops.RemoveAt(i);
            }
            return false;
        }
    }
    #endregion

    #region Simulated Annealing
    class SaLocalSolver : LocalSolver
    {
        public readonly double cs;  // Starting temperature
        public readonly double a;   // Fraction of temperature that is left after each iteration

        private double c;   // Current temperature

        Random Rand;

        public SaLocalSolver(Schedule s, double cs, double a) : base(s)
        {
            this.cs = cs;
            this.a = a;

            Rand = new Random();
        }

        public override void Init()
        {
            c = cs;
        }

        public override bool GetNext(double[] probDist, int nOps)
        {
            // Create an array of operators that we are going to perform to generate neighbors
            List<NOp> ops = new List<NOp>(schedule.GetOperations(probDist, nOps));

            // Take a random operator
            // if it may be performed
            //  - Apply it if the operator decreases the score
            //  - Apply the operator with chance decpending on the temperator if the score is increased
            while (ops.Count != 0)
            {
                int i = StaticRandom.Next(0, ops.Count);
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
                        double r = StaticRandom.NextDouble();

                        if (p > r)
                        {
                            op.Apply();
                            return true;
                        }
                    }
                }
                c *= a; // Update temperature
            }
            return false;
        }
        // Get the chance according to the current temperature:
        static double Prob(double delta, double temp) => Math.Exp(-delta / temp);
    }
    #endregion

    #endregion

}
