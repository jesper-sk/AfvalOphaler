using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AfvalOphaler
{
    #region Schedule
    /// <summary>
    /// Main class that represents the routes
    /// that the garbage trucks will drive each day.
    /// </summary>
    public class Schedule
    {
        #region Public Variables
        public double Duration = 0;                 // Total travel time
        public double Penalty;                      // Sum of 3*freq*time_to_empty of each order not planned in
        public double Score => Duration + Penalty;  // The score of the schedule

        public List<Order> UnScheduledOrders;       // List of orders that not have been planned in yet

        // The route of for each dump truck for each (working)day
        // DayRoutes[i][j] is the route for truck j on day i:
        public DayRoute[][] DayRoutes; 
        #endregion

        #region Constructor(s)
        public Schedule(List<Order> orders)
        {
            // Initialize unscheduled orders and penalty (since no order is planned yet):
            UnScheduledOrders = orders.ToList();
            foreach (Order o in UnScheduledOrders) Penalty += 3 * o.Frequency * o.TimeToEmpty;

            // Initialize a DayRoute for each truck for each day:
            DayRoutes = new DayRoute[5][];
            for (int d = 0; d < 5; d++)
            {
                DayRoutes[d] = new DayRoute[2];
                for (int t = 0; t < 2; t++) DayRoutes[d][t] = new DayRoute(d, t);
            }
        }
        #endregion

        #region Score and Penalty Calculation
        /// <summary>
        /// Used to calculate the score by calculating the duration and the penalty.
        /// </summary>
        /// <returns>The calculated score</returns>
        public double CalculateScore()
        {
            CalculateDuration();
            CalculatePenalty();
            return Score;
        }
        /// <summary>
        /// Used to calculate the duration by iterating over each planned order,
        /// and adding the travel times and empty times together.
        /// </summary>
        public void CalculateDuration()
        {
            double duration = 0;
            foreach (DayRoute[] bigDay in DayRoutes) foreach (DayRoute day in bigDay) duration += day.Duration;
            Duration = duration;
        }
        /// <summary>
        /// Used to calculate the total penalty 
        /// by iterating over the order that not have been planned in yet.
        /// </summary>
        public void CalculatePenalty()
        {
            double penalty = 0;
            foreach (Order o in UnScheduledOrders) penalty += 3 * o.Frequency * o.TimeToEmpty;
            Penalty = penalty;
        }
        #endregion

        #region [DEPRICATED] Add/Remove
        //public int AddLoop(int day, int truck)
        //{
        //    Duration += 30;
        //    return dayRoutes[day][truck].AddLoop();
        //}

        //public Node AddOrder(Order o, Node nextTo, int loop, int day, int truck)
        //{
        //    DayRoute route = dayRoutes[day][truck];
        //    Duration -= route.Duration;
        //    Node newAdded = route.AddOrderToLoop(o, nextTo, loop);
        //    Duration += route.Duration;
        //    Penalty -= 3 * o.Frequency * o.TimeToEmpty;
        //    return newAdded;
        //}

        //public void RemoveNode(Node toDelete, int loop, int day, int truck)
        //{
        //    DayRoute route = dayRoutes[day][truck];
        //    Duration -= route.Duration;
        //    route.RemoveNodeFromLoop(toDelete, loop);
        //    Duration += route.Duration;
        //    Penalty += 3 * toDelete.Data.TimeToEmpty;
        //}
        #endregion

        #region Operations
        private Func<Schedule, NeighborOperation>[] ops =
        {
            new Func<Schedule, NeighborOperation>((s) => new RandomAddOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomDeleteOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomTransferOperation(s)),
            new Func<Schedule, NeighborOperation>((s) => new RandomSwapOperation(s))
        };
        /// <summary>
        /// Creates an array of neighboroperations.
        /// Each operation has a chance to get in the array 
        /// given by the provided probability distribution.
        /// </summary>
        /// <param name="probDist">The chance for each operation to get in the array</param>
        /// <param name="nOps">Length of the neighboroperations array</param>
        /// <returns>An array of neighboroperations</returns>
        public NeighborOperation[] GetOperations(double[] probDist, int nOps)
        {
            NeighborOperation[] res = new NeighborOperation[nOps];
            for(int j = 0; j < nOps; j++)
            {
                double acc = 0;
                double p = StaticRandom.NextDouble();
                for (int i = 0; i < probDist.Length; i++)
                {
                    acc += probDist[i];
                    if (p <= acc)
                    {
                        res[j] = ops[i](this);
                        break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Main class that defines what a neighboroperation looks like
        /// and what methods it needs.
        /// </summary>
        public abstract class NeighborOperation
        {
            public bool IsEvaluated { get; protected set; } = false;
            public double TotalDelta => DeltaTime + DeltaPenalty;
            public double DeltaTime { get; protected set; } = double.NaN;
            public double DeltaPenalty { get; protected set; } = double.NaN;

            public Schedule State;

            public NeighborOperation(Schedule s)
            {
                State = s;
            }

            /// <summary>
            /// Evaluate the operation, 
            /// this returns a boolean that indicates if the operation may be performed.
            /// The deltaduration and deltapenalty are saved in the operation.
            /// </summary>
            /// <returns>Whether the operation may be performed</returns>
            public bool Evaluate()
            {
                if (_Evaluate(out double dT, out double dP))
                {
                    IsEvaluated = true;
                    DeltaTime = dT;
                    DeltaPenalty = dP;
                    return true;
                }
                else return false;
            }

            /// <summary>
            /// Apply the operator. 
            /// This modifies the schedule.
            /// </summary>
            public void Apply()
            {
                if (!IsEvaluated) throw new InvalidOperationException("Evaluate operation first!");
                _Apply();
            }

            // The methodes that need to be implemented by the different operations:
            protected abstract bool _Evaluate(out double deltaTime, out double deltaPenalty);
            protected abstract void _Apply();
            public abstract override string ToString();
        }

        /// <summary>
        /// Randomly picks an order to be added to the schedule and passes it to the AddOperation.
        /// </summary>
        public class RandomAddOperation : NeighborOperation
        {
            private AddOperation addOp;

            public RandomAddOperation(Schedule s) : base(s)
            {
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                // Pick random order:
                int ind = StaticRandom.Next(0, State.UnScheduledOrders.Count);

                // Evaluate the addition:
                addOp = new AddOperation(State, ind);
                bool possible = addOp.Evaluate();
                deltaTime = addOp.DeltaTime;
                deltaPenalty = addOp.DeltaPenalty;
                return possible;
            }

            protected override void _Apply() => addOp.Apply();

            public override string ToString() => $"RandomAddOperation, Evaluated: {IsEvaluated}";
        }

        /// <summary>
        /// Adds a given order to the schedule.
        /// </summary>
        public class AddOperation : NeighborOperation
        {
            private readonly int orderIndex;
            private readonly Order toAdd;
            private readonly int nAdditions;
            private List<double> deltas;
            public List<Node> whereToAdd;
            private List<int> whereToAddDays;
            private List<int> whereToAddTrucks;
            public AddOperation(Schedule s, int orderIndex) : base(s)
            {
                this.orderIndex = orderIndex;
                if (State.UnScheduledOrders.Count == 0) toAdd = null;
                else toAdd = State.UnScheduledOrders[orderIndex];
                nAdditions = toAdd?.Frequency ?? -1;
            }
            public AddOperation(Schedule s, Order toAdd) : base(s)
            {
                orderIndex = -1;
                this.toAdd = toAdd;
                nAdditions = 1;
            }
        
            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                // Initialize variables:
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                if (toAdd == null) return false;

                // Get the possible day combinations the order may be added:
                List<int[]> combis = GD.AllowedDayCombinations[nAdditions].ToList();

                // Try each day combination:
                while (!(combis.Count == 0)) 
                {
                    int[] combi = combis[StaticRandom.Next(0, combis.Count)];
                    int everyDayInCombiAllowed = 0;
                    deltas = new List<double>(nAdditions);
                    whereToAdd = new List<Node>(nAdditions);
                    whereToAddDays = new List<int>(nAdditions);
                    whereToAddTrucks = new List<int>(nAdditions);
                    foreach (int day in combi)
                    {
                        // Try to add order in a random truck:
                        int truck = StaticRandom.Next(0, 2);
                        if (State.DayRoutes[day][truck].EvaluateRandomAdd(toAdd, out double delta1, out Node where1)) // MISS NIET BEIDE TRUCKS PROBEREN
                        {
                            deltas.Add(delta1);
                            whereToAdd.Add(where1);
                            whereToAddDays.Add(day);
                            whereToAddTrucks.Add(truck);
                            everyDayInCombiAllowed++;
                            continue;
                        }
                        // Try the other truck:
                        else if (State.DayRoutes[day][1 - truck].EvaluateRandomAdd(toAdd, out double delta2, out Node where2))
                        {
                            deltas.Add(delta2);
                            whereToAdd.Add(where2);
                            whereToAddDays.Add(day);
                            whereToAddTrucks.Add(1 - truck);
                            everyDayInCombiAllowed++;
                            continue;
                        }
                    }
                    // Check if there is a possibility to add the order to each day of the day combination:
                    if (everyDayInCombiAllowed == nAdditions)
                    {
                        deltaTime = deltas.Sum();
                        deltaPenalty = -(3 * nAdditions * toAdd.TimeToEmpty);
                        return true;
                    }
                    // It is not possible to add order to each day of the day combination,
                    // Remove the day combi, and try the next one:
                    combis.Remove(combi);
                }
                // No possibility to add order:
                return false;
            }

            protected override void _Apply()
            {
                // Add the order to the dayroutes:
                for(int i = 0; i < whereToAdd.Count; i++)
                {
                    DayRoute curr = State.DayRoutes[whereToAddDays[i]][whereToAddTrucks[i]];
                    State.Duration -= curr.Duration;
                    curr.AddOrder(toAdd, whereToAdd[i]);
                    State.Duration += curr.Duration;
                }
                // Update penalty:
                State.Penalty -= 3 * nAdditions * toAdd.TimeToEmpty;
                // Remove order from th unscheduled orders list:
                if (orderIndex != -1) State.UnScheduledOrders.RemoveAt(orderIndex);
                else State.UnScheduledOrders.Remove(toAdd);
            }

            public override string ToString() => $"AddOperation, Evaluated: {IsEvaluated}";
        }

        /// <summary>
        /// Removes a random order from the schedule.
        /// </summary>
        public class RandomDeleteOperation : NeighborOperation
        {
            public Order OrderToRemove { get; private set; }

            Node toRemove;
            int day;
            int truck;
            public RandomDeleteOperation(Schedule s) : base(s)
            {
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                void SetToRemove(Node r)
                {
                    toRemove = r;
                    OrderToRemove = r.Data;
                }

                // Get a random day and truck:
                int d = StaticRandom.Next(0, 5);
                int t = StaticRandom.Next(0, 2);
                // Get a node that can be removed from the corresponding DayRoute:
                if (State.DayRoutes[d][t].EvaluateRandomRemove(out Node rem1, out double delta1))
                {
                    SetToRemove(rem1);
                    day = d;
                    truck = t;
                    deltaTime = delta1;
                    deltaPenalty = 3 * rem1.Data.Frequency * rem1.Data.TimeToEmpty;
                    return true;
                }
                // If no such node exists, try the other truck on the same day:
                else if (State.DayRoutes[d][1 - t].EvaluateRandomRemove(out Node rem2, out double delta2))
                {
                    SetToRemove(rem2);
                    day = d;
                    truck = 1 - t;
                    deltaTime = delta2;
                    deltaPenalty = 3 * rem2.Data.Frequency * rem2.Data.TimeToEmpty;
                    return true;
                }
                // No nodes can be removed from the picked day:
                else
                {
                    deltaTime = double.NaN;
                    deltaPenalty = double.NaN;
                    return false;
                }
            }

            protected override void _Apply()
            {             
                // Remove node and update times:
                DayRoute curr = State.DayRoutes[day][truck];
                State.Duration -= curr.Duration;
                curr.RemoveNode(toRemove);
                State.Duration += curr.Duration;

                // Add the order back to the unscheduled orders list and update the penalty:
                State.UnScheduledOrders.Add(toRemove.Data);
                State.Penalty += 3 * toRemove.Data.TimeToEmpty;
            }

            public override string ToString() => $"RandomDeleteOperation, Evaluated: {IsEvaluated}";
        }

        /// <summary>
        /// Deletes a random order,
        /// tries to add it back to the schedule in another place.
        /// </summary>
        public class RandomTransferOperation : NeighborOperation
        {
            RandomDeleteOperation delOp;
            AddOperation addOp;
            public RandomTransferOperation(Schedule s) : base(s)
            {
                delOp = new RandomDeleteOperation(s);
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                // Initialize variables:
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                // Try to remove a random order:
                if (!delOp.Evaluate()) return false;
                // Try to add it back to a new place:
                addOp = new AddOperation(State, delOp.OrderToRemove);
                if (!addOp.Evaluate()) return false;

                // Set the change in time and penalty:
                deltaTime = delOp.DeltaTime + addOp.DeltaTime;
                deltaPenalty = delOp.DeltaPenalty + addOp.DeltaPenalty;

                return true;
            }

            protected override void _Apply()
            {
                // Remove order:
                delOp.Apply();
                // Add order:
                addOp.Apply();
            }

            public override string ToString() => $"RandomTransferOperation, Evaluated: {IsEvaluated}";
        }

        /// <summary>
        /// Takes to (scheduled) orders and swaps their places.
        /// </summary>
        public class RandomSwapOperation : NeighborOperation
        {
            public Node toSwap1;
            public Node toSwap2;
            int d1;
            int d2;
            int t1;
            int t2;
            double dt1;
            double dt2;

            public RandomSwapOperation(Schedule s) : base(s)
            {
            }

            protected override bool _Evaluate(out double deltaTime, out double deltaPenalty)
            {
                // Initialize variables:
                deltaTime = double.NaN;
                deltaPenalty = double.NaN;
                // Pick a random day and truck:
                d1 = StaticRandom.Next(0, 5);
                t1 = StaticRandom.Next(0, 2);
                // Get a random node from the corresponding DayRoute:
                if (State.DayRoutes[d1][t1].EvaluateSwap1(out toSwap1, out double roomLeft_d1, out double freeTime_d1, out double timeLeft_d1))
                {
                    // Pick another random day and truck such that d1 and d2 differ:
                    do d2 = StaticRandom.Next(0, 5); while (d2 == d1);
                    // Check if there is a node in that DayRoute that can be swapped with swap1:
                    if (State.DayRoutes[d2][t2].EvaluateSwap2(toSwap1, roomLeft_d1, freeTime_d1, timeLeft_d1, out toSwap2, out dt1, out dt2))
                    {
                        deltaTime = dt1 + dt2;
                        deltaPenalty = 0;
                        return true;
                    }
                }
                return false;
            }

            protected override void _Apply()
            {
                // Update state duration:
                State.Duration += DeltaTime;

                // Swap the nodes:
                State.DayRoutes[d1][t1].Swap1(toSwap2, toSwap1, dt1);
                State.DayRoutes[d2][t2].Swap2(toSwap1, toSwap2, dt2);

                // Update their tourindices:
                int tempTourIndex = toSwap1.TourIndex;
                toSwap1.TourIndex = toSwap2.TourIndex;
                toSwap2.TourIndex = tempTourIndex;
            }

            public override string ToString() => $"RandomSwapOperation, Evaluated {IsEvaluated}";
        }
        #endregion

        #region Optimization
        /// <summary>
        /// Runs 2.5-opt over all DayRoutes.
        /// </summary>
        public void OptimizeAllDays()
        {
            for (int d = 0; d < 5; d -= -1)
                for (int t = 0; t < 2; t -= -1)
                    OptimizeDay(d, t);
        }
        /// <summary>
        /// Runs 2.5 opt over one DayRoute.
        /// </summary>
        /// <param name="day">The day on which 2.5-opt needs to be applied</param>
        /// <param name="truck">The truck on which 2.5-opt needs to be applied</param>
        public void OptimizeDay(int day, int truck)
        {
            Duration -= DayRoutes[day][truck].Duration;
            DayRoutes[day][truck].Optimize();
            Duration += DayRoutes[day][truck].Duration;
        }
        #endregion

        #region ToResult and ToStrings
        /// <summary>
        /// Converts the schedule to a schedule result that only contains the checkstring and scores.
        /// </summary>
        /// <returns>A ScheduleResult of the Schedule</returns>
        public ScheduleResult ToResult() => new ScheduleResult()
        {
            Score = Score,
            Stats = GetStatisticsBuilder(),
            Check = ToCheckStringBuilder(),
            String = ToString()
        };
        public override string ToString() => $"Score: {Score}, Total time: {Duration}, Total Penalty: {Penalty}";
        public string ToCheckString() => ToCheckStringBuilder().ToString();
        /// <summary>
        /// Converts the current routes to a string that is accepted by the checker.
        /// </summary>
        /// <returns>A StringBuilder that contains the routes of the Schedule in checker format</returns>
        public StringBuilder ToCheckStringBuilder()
        {
            StringBuilder b = new StringBuilder();
            for (int t = 0; t < 2; t++)
            {
                for (int d = 0; d < 5; d++)
                {
                    int lastDump = -3;
                    int ordOfDay = 0;
                    foreach (Node n in DayRoutes[d][t])
                        if(!n.IsDump)
                            b.AppendLine($"{t + 1}; {d + 1}; {++ordOfDay}; {n.Data.OrderId}");
                        else if (ordOfDay != lastDump)
                        {
                            b.AppendLine($"{t + 1}; {d + 1}; {++ordOfDay}; {n.Data.OrderId}");
                            lastDump = ordOfDay;
                        }
                }
            }
            return b;
        }
        public string GetStatistics() => GetStatisticsBuilder().ToString();
        /// <summary>
        /// Makes a StringBuilder that contains the score, totaltime, totalpenalty
        /// and for each day and truck the durations and amount of space used.
        /// </summary>
        /// <returns>A StringBuilder with statistics about the schedule and each day</returns>
        public StringBuilder GetStatisticsBuilder()
        {
            StringBuilder res = new StringBuilder();
            res.AppendLine("Score = " + Score);
            res.AppendLine("TotalTime = " + Duration);
            res.AppendLine("TotalPenalty = " + Penalty);
            for (int i = 0; i < 5; i++)
            {
                res.AppendLine($"Day {i}:");
                res.AppendLine($"Truck 1: {DayRoutes[i][0]}");
                res.AppendLine($"Truck 2: {DayRoutes[i][1]}");
            }
            return res;
        }
        #endregion
    }
    #endregion
    
    #region ScheduleResult
    /// <summary>
    /// Contains the statistics and checkstring of a Schedule.
    /// </summary>
    public class ScheduleResult
    {
        public double Score;
        public StringBuilder Stats;
        public StringBuilder Check;
        public string String = "";
    }
    #endregion
}
