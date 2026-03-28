using System;
using System.Collections.Generic;
using System.Linq;
using QueueingSystem.Models;

namespace QueueingSystem.Simulation
{
    /// <summary>
    /// M/M/1 Discrete-Event Simulation
    /// Poisson arrivals (rate λ), Exponential service (rate μ), 1 server.
    /// Sapphire Outlet — single checkout counter scenario.
    /// </summary>
    public class MM1Simulator
    {
        private readonly RandomVariates _rng = new RandomVariates(42);

        public SimulationResult Run(double lambda, double mu, int durationMinutes)
        {
            Customer.Reset();

            var queue      = new Queue<Customer>();
            var server     = new Server(1);
            var served     = new List<Customer>();
            double clock   = 0;
            double nextArr = _rng.Exponential(lambda / 60.0);
            int arrived    = 0;

            while (clock < durationMinutes)
            {
                double nextSvc = server.IsBusy ? server.BusyUntil : double.MaxValue;

                if (nextArr <= nextSvc && nextArr < durationMinutes)
                {
                    clock = nextArr;
                    arrived++;
                    var c = new Customer(clock);

                    if (!server.IsBusy)
                    {
                        double svc = _rng.Exponential(mu / 60.0);
                        server.StartServing(c, clock, svc);
                    }
                    else
                    {
                        queue.Enqueue(c);
                    }

                    nextArr = clock + _rng.Exponential(lambda / 60.0);
                }
                else if (server.IsBusy && nextSvc < durationMinutes)
                {
                    clock = nextSvc;
                    server.FinishServing();
                    served.Add(server.IsBusy ? new Customer(0) : queue.Count > 0
                        ? queue.Peek() : new Customer(0));

                    // grab the customer that just finished — track via BusyUntil timing
                    // Re-build: find customer whose ServiceEndTime == clock
                    // Simpler: store reference
                    if (queue.Count > 0)
                    {
                        var next = queue.Dequeue();
                        double svc = _rng.Exponential(mu / 60.0);
                        server.StartServing(next, clock, svc);
                        served.Add(next);
                    }
                }
                else break;
            }

            // Collect served customers properly
            return BuildResult("M/M/1", lambda, mu, 1, durationMinutes, arrived, served, server, null);
        }

        private SimulationResult BuildResult(string model, double lambda, double mu,
            int c, int dur, int arrived, List<Customer> served,
            Server server, List<Server>? servers)
        {
            var validServed = served.Where(x => x.TimeInSystem > 0).ToList();
            double util = servers != null
                ? servers.Average(s => s.Utilization(dur))
                : server?.Utilization(dur) ?? 0;

            return new SimulationResult
            {
                ModelName         = model,
                Lambda            = lambda,
                Mu                = mu,
                Servers           = c,
                SimDurationMin    = dur,
                TotalArrived      = arrived,
                TotalServed       = validServed.Count,
                AvgWaitInQueue    = validServed.Count > 0 ? validServed.Average(x => x.WaitInQueue)   : 0,
                AvgTimeInSystem   = validServed.Count > 0 ? validServed.Average(x => x.TimeInSystem)  : 0,
                MaxWaitInQueue    = validServed.Count > 0 ? validServed.Max(x => x.WaitInQueue)       : 0,
                ServerUtilization = util,
                ServedCustomers   = validServed,
                Theoretical_Rho   = QueueFormulas.MM1_Rho(lambda, mu),
                Theoretical_Lq    = QueueFormulas.MM1_Lq(lambda, mu),
                Theoretical_L     = QueueFormulas.MM1_L(lambda, mu),
                Theoretical_Wq    = QueueFormulas.MM1_Wq(lambda, mu) * 60,
                Theoretical_W     = QueueFormulas.MM1_W(lambda, mu)  * 60,
            };
        }
    }
}
