using System;
using System.Collections.Generic;
using System.Linq;
using QueueingSystem.Models;

namespace QueueingSystem.Simulation
{
    public enum ServiceDistribution { Exponential, Erlang, Uniform, Normal, LogNormal }
    public enum ArrivalDistribution { Poisson, Uniform }

    /// <summary>
    /// Generic discrete-event simulation engine.
    /// Drives M/M/1, M/G/1, G/G/1, and M/M/c by swapping
    /// arrival and service variate generators.
    /// </summary>
    public class EventEngine
    {
        private readonly RandomVariates _rng;

        public EventEngine(int seed = 42) { _rng = new RandomVariates(seed); }

        public SimulationResult Run(SimConfig cfg)
        {
            Customer.Reset();

            var queue      = new Queue<Customer>();
            var servers    = Enumerable.Range(1, cfg.NumServers).Select(i => new Server(i)).ToList();
            var served     = new List<Customer>();

            double clock   = 0;
            double nextArr = GenerateInterArrival(cfg);
            int arrived    = 0;

            while (clock < cfg.DurationMinutes)
            {
                // Find soonest service completion
                var busyServer  = servers.Where(s => s.IsBusy).OrderBy(s => s.BusyUntil).FirstOrDefault();
                double nextSvc  = busyServer?.BusyUntil ?? double.MaxValue;

                if (nextArr <= nextSvc && nextArr < cfg.DurationMinutes)
                {
                    // ── ARRIVAL EVENT ──────────────────────────────────
                    clock = nextArr;
                    arrived++;
                    var c = new Customer(clock);

                    var freeServer = servers.FirstOrDefault(s => !s.IsBusy);
                    if (freeServer != null)
                    {
                        double svc = GenerateServiceTime(cfg);
                        freeServer.StartServing(c, clock, svc);
                    }
                    else
                    {
                        queue.Enqueue(c);
                    }

                    nextArr = clock + GenerateInterArrival(cfg);
                }
                else if (busyServer != null && nextSvc < cfg.DurationMinutes)
                {
                    // ── SERVICE COMPLETION EVENT ───────────────────────
                    clock = nextSvc;

                    // Record the finished customer (ServiceEndTime == clock)
                    var done = new Customer(busyServer.BusyUntil - busyServer.Utilization(clock));
                    // Use direct timing from server's last customer via StartServing tracking
                    busyServer.FinishServing();

                    if (queue.Count > 0)
                    {
                        var next   = queue.Dequeue();
                        double svc = GenerateServiceTime(cfg);
                        busyServer.StartServing(next, clock, svc);
                        served.Add(next);
                    }
                }
                else break;
            }

            return BuildResult(cfg, arrived, served, servers);
        }

        private double GenerateInterArrival(SimConfig cfg)
        {
            double meanInterArrival = 60.0 / cfg.Lambda; // minutes
            return cfg.ArrivalDist switch
            {
                ArrivalDistribution.Poisson => _rng.Exponential(cfg.Lambda / 60.0),
                ArrivalDistribution.Uniform => _rng.UniformArrival(meanInterArrival, cfg.Ca),
                _ => _rng.Exponential(cfg.Lambda / 60.0)
            };
        }

        private double GenerateServiceTime(SimConfig cfg)
        {
            double meanSvc = 60.0 / cfg.Mu; // minutes
            return cfg.ServiceDist switch
            {
                ServiceDistribution.Exponential => _rng.Exponential(cfg.Mu / 60.0),
                ServiceDistribution.Erlang       => _rng.Erlang(cfg.ErlangK, cfg.Mu / 60.0),
                ServiceDistribution.Uniform      => _rng.Uniform(meanSvc * (1 - cfg.Cs * Math.Sqrt(3)),
                                                                   meanSvc * (1 + cfg.Cs * Math.Sqrt(3))),
                ServiceDistribution.Normal       => _rng.Normal(meanSvc, cfg.Cs * meanSvc),
                ServiceDistribution.LogNormal    => _rng.LogNormal(meanSvc, cfg.Cs * meanSvc),
                _ => _rng.Exponential(cfg.Mu / 60.0)
            };
        }

        private SimulationResult BuildResult(SimConfig cfg, int arrived,
            List<Customer> served, List<Server> servers)
        {
            var valid = served.Where(x => x.TimeInSystem > 0 && x.WaitInQueue >= 0).ToList();
            double dur = cfg.DurationMinutes;

            var result = new SimulationResult
            {
                ModelName         = cfg.ModelName,
                Lambda            = cfg.Lambda,
                Mu                = cfg.Mu,
                Servers           = cfg.NumServers,
                SimDurationMin    = (int)dur,
                TotalArrived      = arrived,
                TotalServed       = valid.Count,
                AvgWaitInQueue    = valid.Count > 0 ? valid.Average(x => x.WaitInQueue)  : 0,
                AvgTimeInSystem   = valid.Count > 0 ? valid.Average(x => x.TimeInSystem) : 0,
                MaxWaitInQueue    = valid.Count > 0 ? valid.Max(x => x.WaitInQueue)      : 0,
                ServerUtilization = servers.Average(s => s.Utilization(dur)),
                ServedCustomers   = valid
            };

            // Theoretical values
            switch (cfg.ModelName)
            {
                case "M/M/1":
                    result.Theoretical_Rho = QueueFormulas.MM1_Rho(cfg.Lambda, cfg.Mu);
                    result.Theoretical_Lq  = QueueFormulas.MM1_Lq(cfg.Lambda, cfg.Mu);
                    result.Theoretical_L   = QueueFormulas.MM1_L(cfg.Lambda, cfg.Mu);
                    result.Theoretical_Wq  = QueueFormulas.MM1_Wq(cfg.Lambda, cfg.Mu) * 60;
                    result.Theoretical_W   = QueueFormulas.MM1_W(cfg.Lambda, cfg.Mu)  * 60;
                    break;
                case "M/G/1":
                    result.Theoretical_Rho = QueueFormulas.MG1_Rho(cfg.Lambda, cfg.Mu);
                    result.Theoretical_Lq  = QueueFormulas.MG1_Lq(cfg.Lambda, cfg.Mu, cfg.Cs);
                    result.Theoretical_L   = QueueFormulas.MG1_L(cfg.Lambda, cfg.Mu, cfg.Cs);
                    result.Theoretical_Wq  = QueueFormulas.MG1_Wq(cfg.Lambda, cfg.Mu, cfg.Cs) * 60;
                    result.Theoretical_W   = QueueFormulas.MG1_W(cfg.Lambda, cfg.Mu, cfg.Cs)  * 60;
                    break;
                case "G/G/1":
                    result.Theoretical_Rho = QueueFormulas.GG1_Rho(cfg.Lambda, cfg.Mu);
                    result.Theoretical_Lq  = QueueFormulas.GG1_Lq(cfg.Lambda, cfg.Mu, cfg.Ca, cfg.Cs);
                    result.Theoretical_L   = QueueFormulas.GG1_L(cfg.Lambda, cfg.Mu, cfg.Ca, cfg.Cs);
                    result.Theoretical_Wq  = QueueFormulas.GG1_Wq(cfg.Lambda, cfg.Mu, cfg.Ca, cfg.Cs) * 60;
                    result.Theoretical_W   = QueueFormulas.GG1_W(cfg.Lambda, cfg.Mu, cfg.Ca, cfg.Cs)  * 60;
                    break;
                case "M/M/c":
                    result.Theoretical_Rho = QueueFormulas.MMc_Rho(cfg.Lambda, cfg.Mu, cfg.NumServers);
                    result.Theoretical_Lq  = QueueFormulas.MMc_Lq(cfg.Lambda, cfg.Mu, cfg.NumServers);
                    result.Theoretical_L   = QueueFormulas.MMc_L(cfg.Lambda, cfg.Mu, cfg.NumServers);
                    result.Theoretical_Wq  = QueueFormulas.MMc_Wq(cfg.Lambda, cfg.Mu, cfg.NumServers) * 60;
                    result.Theoretical_W   = QueueFormulas.MMc_W(cfg.Lambda, cfg.Mu, cfg.NumServers)  * 60;
                    break;
            }

            return result;
        }
    }

    /// <summary>Configuration passed to the EventEngine for any model.</summary>
    public class SimConfig
    {
        public string              ModelName    { get; set; } = "M/M/1";
        public double              Lambda       { get; set; } = 20;
        public double              Mu           { get; set; } = 30;
        public int                 NumServers   { get; set; } = 1;
        public double              DurationMinutes { get; set; } = 60;
        public ArrivalDistribution ArrivalDist  { get; set; } = ArrivalDistribution.Poisson;
        public ServiceDistribution ServiceDist  { get; set; } = ServiceDistribution.Exponential;
        public double              Ca           { get; set; } = 1.0; // CV of inter-arrivals
        public double              Cs           { get; set; } = 1.0; // CV of service times
        public int                 ErlangK      { get; set; } = 2;   // Erlang k parameter
    }
}
