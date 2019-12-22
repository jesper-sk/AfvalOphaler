using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AfvalOphaler
{
    #region DayRoute
    /// <summary>
    /// Contains the route that one truck will drive on one day.
    /// A DayRoute consists of multiple Tours (Dump->...->Dump).
    /// </summary>
    public class DayRoute : IEnumerable
    {
        #region Variables & Constructor
        public double Duration => 720 - TimeLeft;
        public double TimeLeft;

        public readonly int DayIndex;
        public readonly int TruckIndex;

        public List<Node> dumps;
        public List<double> roomLefts;
        private List<Node> nodes; //Always contains all nodes, in no specific order 

        public TourEnumerableIndexer Tours { get; private set; }

        private bool firstAdd = true;
        private const int lim = 10;

        public DayRoute(int dind, int trind)
        {
            TimeLeft = 720;
            DayIndex = dind;
            TruckIndex = trind;

            Node dump0 = new Node(0);
            Node dumpL = new Node();
            Node dump1 = new Node(1);

            dump0.Next = dump1;

            dump1.Next = dumpL;
            dump1.Prev = dump0;

            dumpL.Prev = dump1;

            dumps = new List<Node> { dump0, dump1 };
            roomLefts = new List<double> { 100000, 100000 };
            nodes = new List<Node> { dump0, dump1 };

            Tours = new TourEnumerableIndexer(this);
        }

        public class TourEnumerableIndexer
        {
            private DayRoute owner;

            public TourEnumerableIndexer(DayRoute o)
            {
                owner = o;
            }

            public IEnumerable this[int index]
            {
                get => new TourEnumerable(owner.dumps[index]);
            }
        }
        #endregion

        #region Evaluate
        /// <summary>
        /// Checks if an order may be added to this DayRoute.
        /// </summary>
        /// <param name="toAdd">The order that may be added to the DayRoute</param>
        /// <param name="deltaTime">The change in traveltime to this DayRoute if the order would be added</param>
        /// <param name="whereToAdd">If the order may be added, this variable contains the node after which the order may be added</param>
        /// <returns>Whether there is a possible location in this DayRoute where the order can be added</returns>
        public bool EvaluateRandomAdd(Order toAdd, out double deltaTime, out Node whereToAdd)
        {
            // Set variables:
            deltaTime = double.NaN;
            whereToAdd = null;

            // Check if there is enough time in this DayRoute:
            if (toAdd.TimeToEmpty > TimeLeft)
                return false;

            // Create a list of possible tours to which the order may be added considering the space the order takes:
            double totalSpaceOfOrder = toAdd.VolPerContainer * toAdd.NumContainers;
            List<int> candidateTours = new List<int>(roomLefts.Count);
            for (int i = 0; i < roomLefts.Count; i++)
            {
                if (roomLefts[i] >= totalSpaceOfOrder)
                {
                    candidateTours.Add(i);
                }
            }

            // For each possible tour get the possible nodes after which the order may be added:
            List<Node> candidateNodes = new List<Node>();
            foreach (int i in candidateTours)
            {
                foreach (Node curr in Tours[i])
                {
                    if (TimeLeft >
                            toAdd.TimeToEmpty
                            + GD.JourneyTime[curr.Data.MatrixId, toAdd.MatrixId]
                            + GD.JourneyTime[toAdd.MatrixId, curr.Next.Data.MatrixId]
                            - GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId])
                    {
                        candidateNodes.Add(curr);
                    }
                }
            }

            // Pick a random nodes after which we are going to place the order:
            if (candidateNodes.Count > 0)
            {
                Random rnd = new Random();
                whereToAdd = candidateNodes[rnd.Next(0, candidateNodes.Count)];
                while (whereToAdd.Data.OrderId == toAdd.OrderId) whereToAdd = candidateNodes[rnd.Next(0, candidateNodes.Count)];
                deltaTime = toAdd.TimeToEmpty
                    + GD.JourneyTime[whereToAdd.Data.MatrixId, toAdd.MatrixId]
                    + GD.JourneyTime[toAdd.MatrixId, whereToAdd.Next.Data.MatrixId]
                    - GD.JourneyTime[whereToAdd.Data.MatrixId, whereToAdd.Next.Data.MatrixId];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Picks a random node to remove from this DayRoute and returns the change in traveltime.
        /// </summary>
        /// <param name="toRemove">The node to remove</param>
        /// <param name="deltaTime">The change in traveltime</param>
        /// <returns>Whether there is a node to remove in this DayRoute</returns>
        public bool EvaluateRandomRemove(out Node toRemove, out double deltaTime)
        {
            toRemove = null;
            deltaTime = double.NaN;

            // Try each node in the DayRoute:
            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < lim; i++)
            {
                // Pick a random node:
                int ind;
                do ind = StaticRandom.Next(0, nodes.Count); while (!dones.Add(ind));
                Node chosen = nodes[ind];

                // Accept node if frequency == 1, !isDump and removal of node would not exceed the timeleft:
                if (chosen.Data.Frequency > 1) continue;
                if (chosen.IsDump) continue;

                deltaTime = GD.JourneyTime[chosen.Prev.Data.MatrixId, chosen.Next.Data.MatrixId]
                    - (chosen.Data.TimeToEmpty
                        + GD.JourneyTime[chosen.Prev.Data.MatrixId, chosen.Data.MatrixId]
                        + GD.JourneyTime[chosen.Data.MatrixId, chosen.Next.Data.MatrixId]);

                if (deltaTime > TimeLeft) continue;
                toRemove = chosen;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the first node to swap in the swap-operation.
        /// </summary>
        /// <param name="toSwapOut"></param>
        /// <param name="vrije_ruimte_als_swap1_weg_is"></param>
        /// <param name="tijd_die_vrijkomt_als_swap1_wordt_verwijderd"></param>
        /// <param name="time_left"></param>
        /// <returns>Whether there is a node that can be swapped out</returns>
        public bool EvaluateSwap1(out Node toSwapOut, out double vrije_ruimte_als_swap1_weg_is, out double tijd_die_vrijkomt_als_swap1_wordt_verwijderd, out double time_left)
        {
            toSwapOut = null;
            vrije_ruimte_als_swap1_weg_is = double.NaN;
            tijd_die_vrijkomt_als_swap1_wordt_verwijderd = double.NaN;
            time_left = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < lim; i++)
            {
                // Pick random node:
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap1 = nodes[index];

                // Only go further if the frequency == 1
                if (swap1.Data.Frequency > 1 || swap1.IsDump) continue;

                // Calculate increase in timeleft if we would remove this node:
                tijd_die_vrijkomt_als_swap1_wordt_verwijderd = 0
                    + TimeLeft
                    + swap1.Data.TimeToEmpty
                    + GD.JourneyTime[swap1.Prev.Data.MatrixId, swap1.Data.MatrixId]
                    + GD.JourneyTime[swap1.Data.MatrixId, swap1.Next.Data.MatrixId];

                // Calculate increase in roomleft if we would remove this node:
                vrije_ruimte_als_swap1_weg_is =
                    roomLefts[swap1.TourIndex]
                    + swap1.Data.VolPerContainer * swap1.Data.NumContainers;

                time_left = TimeLeft;
                toSwapOut = swap1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds a node that can be swapped with swap1.
        /// </summary>
        /// <param name="swap1">The node that needs to be placed in this DayRoute</param>
        /// <param name="vrije_ruimte_in_dag1">The room left in the DayRoute of swap1</param>
        /// <param name="vrije_tijd_in_dag1_met_timeleft_dag1">The time left in the DayRoute of swap1 as if swap1 is removed</param>
        /// <param name="timeleft_dag1">The TimeLeft of the DayRoute of swap1</param>
        /// <param name="toSwapOut">Will contain the node with which swap1 needs to be swapped</param>
        /// <param name="dt1">The change in time in the DayRoute of swap1</param>
        /// <param name="dt2">The change in time in the DayRoute of swap2</param>
        /// <returns>Whether there is a node that can be swapped with swap1</returns>
        public bool EvaluateSwap2(Node swap1, double vrije_ruimte_in_dag1, double vrije_tijd_in_dag1_met_timeleft_dag1, double timeleft_dag1, out Node toSwapOut, out double dt1, out double dt2)
        {
            toSwapOut = null;
            dt1 = dt2 = double.NaN;

            HashSet<int> dones = new HashSet<int>();
            for (int i = 0; i < nodes.Count && i < lim; i++)
            {
                // Pick random node:
                int index;
                do index = StaticRandom.Next(0, nodes.Count); while (!dones.Add(index));
                Node swap2 = nodes[index];

                // Stop if swap2 is a dump or freq > 1:
                if (swap2.Data.Frequency > 1 || swap2.IsDump) continue;

                //
                // Check if swap2 can be placed in the location of swap1
                //
                // Calculate the required truckspace to place swap2 in the location of swap1:
                double benodigd_ruimte_swap2 = swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Stop if there is not enough space in the DayRoute of swap1:
                if (benodigd_ruimte_swap2 > vrije_ruimte_in_dag1) continue;

                // Calculate the required time in the DayRoute of swap1 if swap2 would be placed there:
                double benodigde_tijd_swap2 = 0
                    + swap2.Data.TimeToEmpty
                    + GD.JourneyTime[swap1.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + GD.JourneyTime[swap2.Data.MatrixId, swap1.Next.Data.MatrixId];

                // Stop if there is not enough roomleft in the DayRoute of swap1:
                if (benodigde_tijd_swap2 > vrije_tijd_in_dag1_met_timeleft_dag1) continue;

                //
                // Check if swap1 may be placed in the place of swap2
                //
                // Calculate the roomleft if swap2 would be removed:
                double vrije_ruimte_dag2 = roomLefts[swap2.TourIndex]
                    + swap2.Data.VolPerContainer * swap2.Data.NumContainers;

                // Calculate the required space if swap1 would be placed here:
                double benodigde_ruimte_swap1 = swap1.Data.VolPerContainer * swap1.Data.NumContainers;

                // Stop if there is not enough roomleft in this DayRoute if swap2 would be removed and swap1 be placed here:
                if (benodigde_ruimte_swap1 > vrije_ruimte_dag2) continue;

                // Calcuate the timeleft if swap2 would be removed:
                double vrije_tijd_in_dag2_met_timeleft_dag2 = 0
                    + TimeLeft
                    + swap2.Data.TimeToEmpty
                    + GD.JourneyTime[swap2.Prev.Data.MatrixId, swap2.Data.MatrixId]
                    + GD.JourneyTime[swap2.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Calculate the required time for swap1 if it would be placed here:
                double benodigde_tijd_swap1 = 0
                    + swap1.Data.TimeToEmpty
                    + GD.JourneyTime[swap2.Prev.Data.MatrixId, swap1.Data.MatrixId]
                    + GD.JourneyTime[swap1.Data.MatrixId, swap2.Next.Data.MatrixId];

                // Stop if there is not enough free time to remove swap2 and place swap1:
                if (benodigde_tijd_swap1 > vrije_tijd_in_dag2_met_timeleft_dag2) continue;

                //
                // All tests are passed so we are allowed to swap swap1 with swap2
                //

                // dt1: delta time for the DayRoute of swap1 if swap1 is removed and swap2 is added:
                dt1 = benodigde_tijd_swap2 - (vrije_tijd_in_dag1_met_timeleft_dag1 - timeleft_dag1);
                // dt2: delta time for this DayRoute if swap2 is removed and swap1 added:
                dt2 = benodigde_tijd_swap1 - (vrije_tijd_in_dag2_met_timeleft_dag2 - TimeLeft);

                toSwapOut = swap2;
                return true;
            }
            // No node passed all tests:
            return false;
        }
        #endregion

        #region Tour Modifications
        /// <summary>
        /// Adds an order after the given node.
        /// </summary>
        /// <param name="order">The order to add</param>
        /// <param name="nextTo">After which node the order needs to be added</param>
        /// <returns>The order as node appended after nextTo</returns>
        public Node AddOrder(Order order, Node nextTo)
        {
            if (firstAdd)
            {
                firstAdd = false;
                TimeLeft -= 60;
            }

            // Update time:
            TimeLeft -= (order.TimeToEmpty
                    + GD.JourneyTime[nextTo.Data.MatrixId, order.MatrixId]
                    + GD.JourneyTime[order.MatrixId, nextTo.Next.Data.MatrixId]
                    - GD.JourneyTime[nextTo.Data.MatrixId, nextTo.Next.Data.MatrixId]);

            // Add order after nextTo:
            Node n = nextTo.AppendNext(order);

            // Update roomleft:
            roomLefts[n.TourIndex] -= (order.NumContainers * order.VolPerContainer);

            // Update TourIndices if the added order is a dump:
            if (n.IsDump)
            {
                for (Node curr = n; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex++;

                double newSpaceTaken = 0;
                for (Node curr = n.Next; !curr.IsDump; curr = curr.Next)
                    newSpaceTaken += curr.Data.NumContainers * curr.Data.VolPerContainer;

                dumps.Insert(n.TourIndex, n);
                roomLefts.Insert(n.TourIndex, 20000 - newSpaceTaken);
                roomLefts[n.TourIndex - 1] -= newSpaceTaken;
            }

            nodes.Add(n);
            return n;
        }

        /// <summary>
        /// Removes a node from the DayRoute.
        /// </summary>
        /// <param name="n">Node to remove</param>
        public void RemoveNode(Node n)
        {
            Order order = n.Data;

            // Update time:
            TimeLeft += (order.TimeToEmpty
                + GD.JourneyTime[n.Prev.Data.MatrixId, order.MatrixId]
                + GD.JourneyTime[order.MatrixId, n.Next.Data.MatrixId]
                - GD.JourneyTime[n.Prev.Data.MatrixId, n.Next.Data.MatrixId]);

            // Update room:
            roomLefts[n.TourIndex] += (order.NumContainers * order.VolPerContainer);

            // Update tourindices if the node was a dump:
            if (n.IsDump)
            {
                for (Node curr = n.Next; !curr.IsSentry; curr = curr.Next)
                    curr.TourIndex--;

                dumps.RemoveAt(n.TourIndex);
                roomLefts[n.TourIndex - 1] -= 20000 - roomLefts[n.TourIndex];
                roomLefts.RemoveAt(n.TourIndex);
            }

            n.Remove();
            nodes.Remove(n);
        }

        /// <summary>
        /// Updates the nexts of the nodes that are swapped.
        /// And updates the timeleft and roomleft of the DayRoute of swap1.
        /// </summary>
        /// <param name="swapIn">swap2</param>
        /// <param name="swapOut">swap1</param>
        /// <param name="dt">Change in time for the DayRoute of swap1</param>
        public void Swap1(Node swapIn, Node swapOut, double dt)
        {
            // Update time:
            double deltaTime = dt;
            TimeLeft = TimeLeft - deltaTime;

            // Update room:
            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;

            // Update nexts:
            Node temp = swapOut.Next;
            swapOut.Next = swapIn.Next;
            swapIn.Next.Prev = swapOut;
            swapIn.Next = temp;
            temp.Prev = swapIn;

            nodes.Remove(swapOut);
            nodes.Add(swapIn);
        }

        /// <summary>
        /// Updates the prevs of the nodes that are swapped.
        /// And updates the timeleft and roomleft of the DayRoute of swap2.
        /// </summary>
        /// <param name="swapIn">swap1</param>
        /// <param name="swapOut">swap2</param>
        /// <param name="dt">Change in time for the DayRoute of Swap2</param>
        public void Swap2(Node swapIn, Node swapOut, double dt)
        {
            // Updates prevs:
            Node temp = swapOut.Prev;
            swapOut.Prev = swapIn.Prev;
            swapIn.Prev.Next = swapOut;
            swapIn.Prev = temp;
            temp.Next = swapIn;

            // Update time:
            double deltaTime = dt;
            TimeLeft = TimeLeft - deltaTime;

            // Update room:
            roomLefts[swapOut.TourIndex] = roomLefts[swapOut.TourIndex]
                + swapOut.Data.NumContainers * swapOut.Data.VolPerContainer
                - swapIn.Data.NumContainers * swapIn.Data.VolPerContainer;

            nodes.Remove(swapOut);
            nodes.Add(swapIn);
        }
        #endregion

        #region Optimization
        public void Optimize()
        {
            // 2-opt move:
            void Two_Opt_Move(Node i, Node j)
            {
                // Node i is placed after node i, 
                // then the piece of route between i and j is reversed placed after node j
                Node iplus = i.Next;
                Node jplus = j.Next;
                iplus.Prev = i.Next.Next;
                j.Next = j.Prev;

                TimeLeft = TimeLeft
                    + GD.JourneyTime[i.Data.MatrixId, iplus.Data.MatrixId]
                    + GD.JourneyTime[iplus.Data.MatrixId, iplus.Next.Data.MatrixId]
                    + GD.JourneyTime[j.Data.MatrixId, jplus.Data.MatrixId];

                Node curr = i.Next.Next;
                Node stop = j;

                // Reverse route between i and j:
                while (curr != stop)
                {
                    Node temp = curr.Next;
                    TimeLeft = TimeLeft
                        + GD.JourneyTime[curr.Data.MatrixId, temp.Data.MatrixId];
                    curr.Next = curr.Prev;
                    TimeLeft = TimeLeft
                        - GD.JourneyTime[curr.Data.MatrixId, curr.Next.Data.MatrixId];
                    curr.Prev = temp;
                    curr = curr.Prev;
                }

                i.Next = j;
                j.Prev = i;
                iplus.Next = jplus;
                jplus.Prev = iplus;

                TimeLeft = TimeLeft
                    - GD.JourneyTime[i.Data.MatrixId, j.Data.MatrixId]
                    - GD.JourneyTime[j.Data.MatrixId, j.Next.Data.MatrixId]
                    - GD.JourneyTime[iplus.Data.MatrixId, jplus.Data.MatrixId];
            }

            // 3-opt move:
            void Two_Opt_Node_Shift_Move(Node i, Node j) // Node i is placed between Node j and Node j.Next
            {
                // Update locations of i, j and their prevs and nexts:
                Node iprev = i.Prev;
                Node inext = i.Next;

                Node jnext = j.Next;

                iprev.Next = inext;
                inext.Prev = iprev;

                j.Next = i;
                i.Next = jnext;
                i.Prev = j;
                jnext.Prev = i;

                // Update traveltimes:
                TimeLeft = TimeLeft
                    + GD.JourneyTime[iprev.Data.MatrixId, i.Data.MatrixId]
                    + GD.JourneyTime[i.Data.MatrixId, inext.Data.MatrixId]
                    + GD.JourneyTime[j.Data.MatrixId, jnext.Data.MatrixId]
                    - GD.JourneyTime[iprev.Data.MatrixId, inext.Data.MatrixId]
                    - GD.JourneyTime[j.Data.MatrixId, i.Data.MatrixId]
                    - GD.JourneyTime[i.Data.MatrixId, jnext.Data.MatrixId];
            }

            // Implementation of 2.5-opt
            // Each combination of nodes in evaluated
            // First it is checked whether a 2-opt move would decrease total traveltime
            // If not, then it is checked whether a 3-opt move would decrease total traveltime
            for (int i = 0; i < dumps.Count; i++)
            {
                for (Node x = dumps[i]; !(x.Next.IsDump || x.Next.Next.IsDump); x = x.Next)
                {
                    Node x1 = x;
                    Node x2 = x.Next;
                    for (Node y = x2.Next; !(y.IsDump || y.Next.IsDump); y = y.Next)
                    {
                        Node y1 = y;
                        Node y2 = y.Next;

                        double del_dist = GD.JourneyTime[x1.Data.MatrixId, x2.Data.MatrixId] + GD.JourneyTime[y1.Data.MatrixId, y2.Data.MatrixId];
                        double X1Y1 = GD.JourneyTime[x1.Data.MatrixId, y1.Data.MatrixId];
                        double X2Y2 = GD.JourneyTime[x2.Data.MatrixId, y2.Data.MatrixId];

                        // Check if 2-opt move would decrease traveltime:
                        if (del_dist - (X1Y1 + X2Y2) > 0)
                        {
                            Two_Opt_Move(x1, y1);
                            return;
                        }
                        else
                        {
                            // Check if 3-opt move would decrease traveltime:
                            double X2Y1 = GD.JourneyTime[x2.Data.MatrixId, y1.Data.MatrixId];
                            Node z1 = x2.Next;
                            if (z1 != y1)
                            {
                                if ((del_dist + GD.JourneyTime[x2.Data.MatrixId, z1.Data.MatrixId]) - (X2Y2 + X2Y1 + GD.JourneyTime[x1.Data.MatrixId, z1.Data.MatrixId]) > 0)
                                {
                                    Two_Opt_Node_Shift_Move(x2, y1);
                                    return;
                                }
                            }
                            else
                            {
                                z1 = y1.Prev;
                                if (z1 != x2)
                                {
                                    if ((del_dist + GD.JourneyTime[y1.Data.MatrixId, z1.Data.MatrixId]) - (X1Y1 + X2Y1 + GD.JourneyTime[y2.Data.MatrixId, z1.Data.MatrixId]) > 0)
                                    {
                                        Two_Opt_Node_Shift_Move(y1, x1);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Overrides / Inherited Implementations     
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"TourCount={dumps.Count},\tTimeleft={Math.Round(TimeLeft, 2)}\tRoomLefst=");
            foreach (double left in roomLefts)
            {
                sb.Append($"\t{left}");
            }
            return sb.ToString();
        }
        public IEnumerator GetEnumerator()
        {
            if (dumps.Count == 0) return null;
            return new DayScheduleEnumerator(dumps[0]);
        }

        public string GetRoute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Node n in this)
            {
                sb.AppendLine(n.Data.OrderId.ToString());
            }
            return sb.ToString();
        }
        #endregion
    }
    #endregion

    #region Loop Enum
    /// <summary>
    /// Used to enumerate over the nodes in a DayRoute
    /// </summary>
    public class TourEnumerable : IEnumerable
    {
        private Node start;

        public TourEnumerable(Node start)
        {
            this.start = start;
        }

        public IEnumerator GetEnumerator() => new TourEnumerator(start);
    }

    /// <summary>
    /// Used to enumerate over the nodes in a DayRoute
    /// </summary>
    public class TourEnumerator : IEnumerator
    {
        private readonly Node dump;
        private Node next;
        private Node curr;

        private int endIndex;

        object IEnumerator.Current => curr;
        public TourEnumerator(Node dump)
        {
            this.dump = dump;
            next = dump;
            endIndex = dump.TourIndex + 1;
        }
        bool IEnumerator.MoveNext()
        {
            curr = next;
            next = curr.Next;
            return !((curr.IsDump && curr.TourIndex == endIndex) || curr.IsSentry);
        }
        void IEnumerator.Reset()
        {
            next = curr;
            curr = null;
        }
    }

    /// <summary>
    /// Used to enumerate over the nodes in a DayRoute
    /// </summary>
    public class DayScheduleEnumerator : IEnumerator
    {
        private readonly Node dump;
        private Node curr;
        private Node prev;

        object IEnumerator.Current => curr;
        public DayScheduleEnumerator(Node dump)
        {
            this.dump = dump;
            curr = dump;
        }
        bool IEnumerator.MoveNext()
        {
            prev = curr;
            curr = curr.Next;
            return !prev.IsSentry;
        }
        void IEnumerator.Reset()
        {
            curr = dump;
        }
    }
    #endregion

    #region Node
    /// <summary>
    /// Main class that represents a destination in the schedule.
    /// Each planned order has order.Frequency nodes in the schedule.
    /// </summary>
    public class Node : IEquatable<object>
    {
        #region Variables & Constructors
        public readonly Order Data;     // The order that of which the node is made
        public readonly bool IsDump;    // Whether the node is the dump
        public readonly bool IsSentry;  // Whether the node is the dump at the end of a route

        public int TourIndex;   // Which route of the day the node is
        public Node Prev;
        public Node Next;

        // Used in constructor of DayRoute for creation of a sentry:
        public Node()
        {
            IsDump = true;
            IsSentry = true;
            Data = GD.Dump;
            TourIndex = -1;
            Next = this;
        }
        // Used for the 'creation' of a dump:
        public Node(int dumpId)
        {
            IsDump = true;
            IsSentry = false;
            Data = GD.Dump;
            TourIndex = dumpId;
        }
        // Used for creating a new node from an order:
        public Node(Order o, int tourInd)
        {
            IsDump = o.OrderId == 0;
            Data = o;
            TourIndex = tourInd;
            IsSentry = false;
        }
        #endregion

        #region Modifications
        /// <summary>
        /// Adds an order as a new node after this node.
        /// </summary>
        /// <param name="o">The order to add</param>
        /// <returns>The order as node appended after this node</returns>
        public Node AppendNext(Order o)
        {
            Node n = new Node(o, TourIndex)
            {
                Prev = this,
                Next = Next
            };

            Next.Prev = n;
            Next = n;

            return n;
        }
        /// <summary>
        /// Removes this node.
        /// Easily done because of the linked list datastructure.
        /// </summary>
        public void Remove()
        {
            Next.Prev = Prev;
            Prev.Next = Next;
            Next = null;
            Prev = null;
        }
        #endregion

        #region Overrides
        public override bool Equals(object o)
        {
            if (!(o is Node n)) return false;
            return Data.OrderId.Equals(n.Data.OrderId) && TourIndex.Equals(n.TourIndex);
        }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => $"{(IsSentry ? "Sentry" : "Node")} ti{TourIndex}: {Data}";
        #endregion
    }
    #endregion
}
