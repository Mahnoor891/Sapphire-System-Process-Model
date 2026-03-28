using System;
using QueueingSystem.Models;

namespace QueueingSystem.Reports
{
    public static class ConsoleDisplay
    {
        public static void PrintResult(SimulationResult r)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ╔══════════════════════════════════════════════════════╗");
            Console.WriteLine($"  ║  Results — {r.ModelName,-43}║");
            Console.WriteLine($"  ╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ── Simulation ──────────────────────────────────────────");
            Console.ResetColor();
            Row("Customers arrived",   $"{r.TotalArrived}");
            Row("Customers served",    $"{r.TotalServed}");
            Row("Avg wait in queue",   $"{r.AvgWaitInQueue:F4} min");
            Row("Avg time in system",  $"{r.AvgTimeInSystem:F4} min");
            Row("Max wait in queue",   $"{r.MaxWaitInQueue:F4} min");
            Row("Server utilization",  $"{r.ServerUtilization:P2}");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ── Theoretical ─────────────────────────────────────────");
            Console.ResetColor();
            Row("Utilization  ρ",      $"{r.Theoretical_Rho:F4}");
            Row("Avg in queue Lq",     $"{r.Theoretical_Lq:F4} customers");
            Row("Avg in system L",     $"{r.Theoretical_L:F4} customers");
            Row("Avg wait    Wq",      $"{r.Theoretical_Wq:F4} min");
            Row("Avg time     W",      $"{r.Theoretical_W:F4} min");

            // Stability check
            Console.WriteLine();
            if (r.Theoretical_Rho < 1)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✔ System STABLE  (ρ = {r.Theoretical_Rho:F4} < 1)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ System UNSTABLE (ρ = {r.Theoretical_Rho:F4} ≥ 1) — queue grows indefinitely!");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void PrintComparisonTable(
            double lambda, double mu,
            double cs_mg1 = 0.6, double ca_gg1 = 0.8, double cs_gg1 = 0.8,
            int maxServers = 4)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║          All-Model Comparison — Sapphire Outlet                    ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine($"  λ={lambda}/hr  μ={mu}/hr  Cs(M/G/1)={cs_mg1}  Ca(G/G/1)={ca_gg1}  Cs(G/G/1)={cs_gg1}\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  {"Model",-10} {"ρ",-8} {"Lq",-8} {"L",-8} {"Wq(min)",-10} {"W(min)",-10} {"Stable"}");
            Console.WriteLine($"  {new string('─', 60)}");
            Console.ResetColor();

            PrintRow("M/M/1",
                QueueFormulas.MM1_Rho(lambda, mu),
                QueueFormulas.MM1_Lq(lambda, mu),
                QueueFormulas.MM1_L(lambda, mu),
                QueueFormulas.MM1_Wq(lambda, mu) * 60,
                QueueFormulas.MM1_W(lambda, mu)  * 60);

            PrintRow("M/G/1",
                QueueFormulas.MG1_Rho(lambda, mu),
                QueueFormulas.MG1_Lq(lambda, mu, cs_mg1),
                QueueFormulas.MG1_L(lambda, mu, cs_mg1),
                QueueFormulas.MG1_Wq(lambda, mu, cs_mg1) * 60,
                QueueFormulas.MG1_W(lambda, mu, cs_mg1)  * 60);

            PrintRow("G/G/1",
                QueueFormulas.GG1_Rho(lambda, mu),
                QueueFormulas.GG1_Lq(lambda, mu, ca_gg1, cs_gg1),
                QueueFormulas.GG1_L(lambda, mu, ca_gg1, cs_gg1),
                QueueFormulas.GG1_Wq(lambda, mu, ca_gg1, cs_gg1) * 60,
                QueueFormulas.GG1_W(lambda, mu, ca_gg1, cs_gg1)  * 60);

            for (int c = 2; c <= maxServers; c++)
            {
                double rho = QueueFormulas.MMc_Rho(lambda, mu, c);
                if (rho >= 1) { PrintRow($"M/M/{c}", rho, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity); continue; }
                PrintRow($"M/M/{c}",
                    rho,
                    QueueFormulas.MMc_Lq(lambda, mu, c),
                    QueueFormulas.MMc_L(lambda, mu, c),
                    QueueFormulas.MMc_Wq(lambda, mu, c) * 60,
                    QueueFormulas.MMc_W(lambda, mu, c)  * 60);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Note: M/G/1 Wq ≤ M/M/1 Wq when Cs < 1 (more regular service = shorter waits).");
            Console.WriteLine("  Note: G/G/1 uses Kingman's approximation — an upper bound estimate.");
            Console.ResetColor();
        }

        private static void PrintRow(string model, double rho, double lq, double l, double wq, double w)
        {
            bool stable = rho < 1;
            Console.ForegroundColor = stable
                ? (wq < 3 ? ConsoleColor.Green : wq < 8 ? ConsoleColor.Yellow : ConsoleColor.Red)
                : ConsoleColor.Red;

            string lqStr = double.IsInfinity(lq) ? "∞" : $"{lq:F3}";
            string lStr  = double.IsInfinity(l)  ? "∞" : $"{l:F3}";
            string wqStr = double.IsInfinity(wq) ? "∞" : $"{wq:F3}";
            string wStr  = double.IsInfinity(w)  ? "∞" : $"{w:F3}";

            Console.WriteLine($"  {model,-10} {rho,-8:F4} {lqStr,-8} {lStr,-8} {wqStr,-10} {wStr,-10} {(stable ? "✔ STABLE" : "✗ UNSTABLE")}");
            Console.ResetColor();
        }

        private static void Row(string label, string value)
        {
            Console.Write($"  {label,-28}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  {value}");
            Console.ResetColor();
        }
    }
}
