using System;
using System.Collections.Generic;
using QueueingSystem.Models;
using QueueingSystem.Simulation;
using QueueingSystem.Reports;

namespace QueueingSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowBanner();

            bool running = true;
            while (running)
            {
                ShowMenu();
                string choice = Console.ReadLine()?.Trim() ?? "";
                Console.WriteLine();

                switch (choice)
                {
                    case "1": RunModel("M/M/1"); break;
                    case "2": RunModel("M/G/1"); break;
                    case "3": RunModel("G/G/1"); break;
                    case "4": RunModel("M/M/c"); break;
                    case "5": RunAllModelsComparison(); break;
                    case "6": RunUseCase(); break;
                    case "0": running = false; Console.WriteLine("  Goodbye!\n"); break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  Invalid choice. Press Enter and try again.");
                        Console.ResetColor();
                        break;
                }

                if (running && choice != "0")
                {
                    Console.WriteLine("\n  Press Enter to return to menu...");
                    Console.ReadLine();
                    Console.Clear();
                    ShowBanner();
                }
            }
        }

        // ── Menu ─────────────────────────────────────────────────────
        static void ShowMenu()
        {
            Console.WriteLine("  Select an option:\n");
            Console.WriteLine("    [1]  M/M/1  — Single server, Exponential service");
            Console.WriteLine("    [2]  M/G/1  — Single server, General service (P-K formula)");
            Console.WriteLine("    [3]  G/G/1  — General arrivals & service (Kingman approx.)");
            Console.WriteLine("    [4]  M/M/c  — Multi-server, Exponential service (Erlang C)");
            Console.WriteLine("    [5]  Compare all models side-by-side");
            Console.WriteLine("    [6]  UC-01 Use Case walkthrough (Single Server)");
            Console.WriteLine("    [0]  Exit\n");
            Console.Write("  Choice: ");
        }

        // ── Run any model ─────────────────────────────────────────────
        static void RunModel(string model)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ── {model} Simulation ─────────────────────────────────────");
            Console.ResetColor();

            double lambda = Prompt("  Arrival rate λ (customers/hour)", 20);
            double mu     = Prompt("  Service rate μ (customers/hour per server)", 30);
            int    dur    = (int)Prompt("  Simulation duration (minutes)", 120);

            var engine = new EventEngine();
            var cfg    = new SimConfig
            {
                ModelName       = model,
                Lambda          = lambda,
                Mu              = mu,
                DurationMinutes = dur,
                NumServers      = 1
            };

            // Model-specific settings
            switch (model)
            {
                case "M/G/1":
                    Console.WriteLine("\n  Service distribution for M/G/1:");
                    Console.WriteLine("    [1] Erlang-k  (Cs < 1, regular service)");
                    Console.WriteLine("    [2] Normal     (Cs ≈ 0.3–0.7)");
                    Console.WriteLine("    [3] LogNormal  (Cs > 1, high variability)");
                    Console.Write("  Choice [1]: ");
                    string dist = Console.ReadLine()?.Trim() ?? "1";
                    switch (dist)
                    {
                        case "2":
                            cfg.ServiceDist = ServiceDistribution.Normal;
                            cfg.Cs = Prompt("  Coefficient of variation Cs (e.g. 0.5)", 0.5);
                            break;
                        case "3":
                            cfg.ServiceDist = ServiceDistribution.LogNormal;
                            cfg.Cs = Prompt("  Coefficient of variation Cs (e.g. 1.5)", 1.5);
                            break;
                        default:
                            cfg.ServiceDist = ServiceDistribution.Erlang;
                            cfg.ErlangK = (int)Prompt("  Erlang k (2=moderate, 4=regular)", 2);
                            cfg.Cs = 1.0 / Math.Sqrt(cfg.ErlangK);
                            Console.WriteLine($"  → Cs = 1/√{cfg.ErlangK} = {cfg.Cs:F4}");
                            break;
                    }
                    break;

                case "G/G/1":
                    cfg.ArrivalDist = ArrivalDistribution.Uniform;
                    cfg.ServiceDist = ServiceDistribution.Normal;
                    cfg.Ca = Prompt("  CV of inter-arrivals Ca (1.0 = Poisson, <1 = regular)", 0.8);
                    cfg.Cs = Prompt("  CV of service times  Cs (1.0 = Exponential, <1 = regular)", 0.8);
                    break;

                case "M/M/c":
                    cfg.NumServers  = (int)Prompt("  Number of servers c", 3);
                    cfg.ServiceDist = ServiceDistribution.Exponential;
                    break;
            }

            // Stability pre-check
            double rho = model switch
            {
                "M/M/c" => QueueFormulas.MMc_Rho(lambda, mu, cfg.NumServers),
                _        => QueueFormulas.MM1_Rho(lambda, mu)
            };

            if (rho >= 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n  ✗ System unstable! ρ = {rho:F4} ≥ 1.");
                Console.WriteLine($"  Increase μ, add servers, or reduce λ.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("\n  Running simulation...");
            var result = engine.Run(cfg);
            ConsoleDisplay.PrintResult(result);

            // Export
            Console.Write("  Export results? [Y/n]: ");
            string exp = Console.ReadLine()?.Trim().ToUpper() ?? "Y";
            if (exp != "N")
            {
                string txt = ReportExporter.ExportText(result);
                string csv = ReportExporter.ExportCsv(result);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✔ Text report : {txt}");
                Console.WriteLine($"  ✔ CSV  data   : {csv}");
                Console.ResetColor();
            }
        }

        // ── Compare all models ────────────────────────────────────────
        static void RunAllModelsComparison()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ── All-Model Comparison ─────────────────────────────────");
            Console.ResetColor();

            double lambda = Prompt("  Arrival rate λ (customers/hour)", 20);
            double mu     = Prompt("  Service rate μ (customers/hour)", 30);
            double csMG1  = Prompt("  Cs for M/G/1 (0.5=Erlang, 1.0=Exp, 1.5=LogNormal)", 0.6);
            double caGG1  = Prompt("  Ca for G/G/1 inter-arrivals (0–1)", 0.8);
            double csGG1  = Prompt("  Cs for G/G/1 service times  (0–1)", 0.8);
            int    maxC   = (int)Prompt("  Max servers to compare", 4);

            ConsoleDisplay.PrintComparisonTable(lambda, mu, csMG1, caGG1, csGG1, maxC);

            // Run simulations for all models and export comparison CSV
            Console.Write("\n  Run simulations & export comparison CSV? [Y/n]: ");
            string exp = Console.ReadLine()?.Trim().ToUpper() ?? "Y";
            if (exp != "N")
            {
                var engine  = new EventEngine();
                int dur     = 120;
                var results = new List<SimulationResult>();

                var models = new[]
                {
                    new SimConfig { ModelName="M/M/1", Lambda=lambda, Mu=mu, DurationMinutes=dur, ServiceDist=ServiceDistribution.Exponential },
                    new SimConfig { ModelName="M/G/1", Lambda=lambda, Mu=mu, DurationMinutes=dur, ServiceDist=ServiceDistribution.Erlang, ErlangK=2, Cs=csMG1 },
                    new SimConfig { ModelName="G/G/1", Lambda=lambda, Mu=mu, DurationMinutes=dur, ArrivalDist=ArrivalDistribution.Uniform, ServiceDist=ServiceDistribution.Normal, Ca=caGG1, Cs=csGG1 },
                    new SimConfig { ModelName="M/M/c", Lambda=lambda, Mu=mu, DurationMinutes=dur, NumServers=Math.Max(2,(int)Math.Ceiling(lambda/mu)+1), ServiceDist=ServiceDistribution.Exponential }
                };

                foreach (var cfg in models)
                {
                    double r = cfg.ModelName == "M/M/c"
                        ? QueueFormulas.MMc_Rho(cfg.Lambda, cfg.Mu, cfg.NumServers)
                        : QueueFormulas.MM1_Rho(cfg.Lambda, cfg.Mu);
                    if (r < 1) results.Add(engine.Run(cfg));
                }

                string csv = ReportExporter.ExportComparisonCsv(results);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✔ Comparison CSV: {csv}");
                Console.ResetColor();
            }
        }

        // ── UC-01 Use Case walkthrough ────────────────────────────────
        static void RunUseCase()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║  UC-01: Single Server Queue – Sapphire Outlet (M/M/1)  ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();

            double lambda = Prompt("  λ (customers/hour)", 20);
            double mu     = Prompt("  μ (customers/hour)", 30);
            Console.WriteLine();

            double rho = QueueFormulas.MM1_Rho(lambda, mu);
            if (rho >= 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Precondition failed: ρ={rho:F4} ≥ 1. System unstable.");
                Console.ResetColor(); return;
            }

            Step(1, "Customer arrives at Sapphire outlet checkout counter.");
            Console.WriteLine($"     λ = {lambda}/hr  →  avg inter-arrival = {60.0/lambda:F2} min\n");

            Step(2, "System checks: is the single server (cashier) free?");
            Console.WriteLine("     Decision: YES → direct service   |   NO → join FIFO queue\n");

            Step(3, "If server free → Customer goes directly to counter.");
            Console.WriteLine($"     Avg service time = {60.0/mu:F2} min  (μ = {mu}/hr)\n");

            Step(4, "Cashier serves the customer.");

            Step(5, "Service complete → Customer exits.");

            Step(6, "Server checks queue: next customer or becomes idle.\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ── Alternate Flow: Server Busy ─────────────────────────────");
            Console.ResetColor();
            Console.WriteLine($"  2a. All {1} server busy.");
            Console.WriteLine("  2b. Customer joins back of FIFO queue.");
            Console.WriteLine($"  2c. Avg wait in queue Wq = {QueueFormulas.MM1_Wq(lambda,mu)*60:F2} min");
            Console.WriteLine("  2d. When service ends, next in queue is called. Resume Step 4.\n");

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ── Exception: Balking / Reneging ───────────────────────────");
            Console.ResetColor();
            Console.WriteLine("  Balking  : Customer sees long queue → does not join.");
            Console.WriteLine("  Reneging : Customer joined but leaves before served.\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ── M/M/1 Metrics ───────────────────────────────────────────");
            Console.ResetColor();
            Console.WriteLine($"  {"ρ  Utilization",-30} {rho:F4}");
            Console.WriteLine($"  {"P₀ Server idle prob.",-30} {QueueFormulas.MM1_P0(lambda,mu):P2}");
            Console.WriteLine($"  {"Lq Avg in queue",-30} {QueueFormulas.MM1_Lq(lambda,mu):F4} customers");
            Console.WriteLine($"  {"L  Avg in system",-30} {QueueFormulas.MM1_L(lambda,mu):F4} customers");
            Console.WriteLine($"  {"Wq Avg wait in queue",-30} {QueueFormulas.MM1_Wq(lambda,mu)*60:F4} min");
            Console.WriteLine($"  {"W  Avg time in system",-30} {QueueFormulas.MM1_W(lambda,mu)*60:F4} min");
        }

        // ── Helpers ───────────────────────────────────────────────────
        static void ShowBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║   Lucky One Mall – Sapphire Outlet Queue System  v2.0  ║");
            Console.WriteLine("  ║   Models: M/M/1  |  M/G/1  |  G/G/1  |  M/M/c        ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void Step(int n, string text)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  Step {n}: ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        static double Prompt(string msg, double def)
        {
            Console.Write($"{msg} [{def}]: ");
            return double.TryParse(Console.ReadLine(), out double v) && v > 0 ? v : def;
        }
    }
}
