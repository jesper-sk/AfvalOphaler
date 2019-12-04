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

        void StartSolving()
        {
            top10Schedules = new Schedule[10];
            var tasks = new Task[threads];
            for (int i = 0; i < threads; i++) tasks[i] = Task.Run(() => DoSolving(startSchedule, 0));
            Task.WaitAll(tasks);
        }

        void DoSolving(Schedule state, int iteration, int maxiterations = 1000000, int op_count = 10)
        {
            if (iteration == maxiterations) lock (addlock) { AddScheduleToTop(state); }

            // Bepaal 10 operaties die je gaat doen
            Func<Schedule, double, Schedule>[] ops = new Func<Schedule, double, Schedule>[op_count];
            double op = rnd.NextDouble();

            // 'Pas' elke operatie toe -> krijg de delta's
            // Bepaal afhankelijk van zoek algortime welke je echt doet.
            Schedule newState = state;        
            DoSolving(newState, iteration++, maxiterations);
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
    }
}
