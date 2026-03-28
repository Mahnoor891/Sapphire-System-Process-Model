using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QueueingSystem.Models;

namespace QueueingSystem.Reports
{
    public static class ReportExporter
    {
        /// <summary>Exports full simulation result to a text summary file.</summary>
        public static string ExportText(SimulationResult r, string folder = "Reports")
        {
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"{r.ModelName.Replace("/","_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine("║   Lucky One Mall – Sapphire Outlet Queue System Report  ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Model            : {r.ModelName}");
            sb.AppendLine($"Generated        : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Simulation Time  : {r.SimDurationMin} minutes");
            sb.AppendLine($"Arrival Rate λ   : {r.Lambda} customers/hour");
            sb.AppendLine($"Service Rate μ   : {r.Mu} customers/hour");
            sb.AppendLine($"Servers (c)      : {r.Servers}");
            sb.AppendLine();

            sb.AppendLine("── SIMULATED RESULTS ──────────────────────────────────────");
            sb.AppendLine($"  Customers Arrived    : {r.TotalArrived}");
            sb.AppendLine($"  Customers Served     : {r.TotalServed}");
            sb.AppendLine($"  Avg Wait in Queue    : {r.AvgWaitInQueue:F4} min");
            sb.AppendLine($"  Avg Time in System   : {r.AvgTimeInSystem:F4} min");
            sb.AppendLine($"  Max Wait in Queue    : {r.MaxWaitInQueue:F4} min");
            sb.AppendLine($"  Server Utilization   : {r.ServerUtilization:P2}");
            sb.AppendLine();

            sb.AppendLine("── THEORETICAL RESULTS ────────────────────────────────────");
            sb.AppendLine($"  Utilization (ρ)      : {r.Theoretical_Rho:F4}");
            sb.AppendLine($"  Avg in Queue (Lq)    : {r.Theoretical_Lq:F4}");
            sb.AppendLine($"  Avg in System (L)    : {r.Theoretical_L:F4}");
            sb.AppendLine($"  Avg Wait Wq          : {r.Theoretical_Wq:F4} min");
            sb.AppendLine($"  Avg Time W           : {r.Theoretical_W:F4} min");
            sb.AppendLine();

            sb.AppendLine("── CUSTOMER LOG (first 50) ────────────────────────────────");
            sb.AppendLine($"  {"ID",-6} {"Arrived",-12} {"Wait(min)",-12} {"Service(min)",-14} {"Total(min)"}");
            sb.AppendLine($"  {new string('-', 58)}");
            int count = 0;
            foreach (var c in r.ServedCustomers)
            {
                if (count++ >= 50) { sb.AppendLine("  ... (truncated)"); break; }
                sb.AppendLine($"  {c.Id,-6} {c.ArrivalTime,-12:F2} {c.WaitInQueue,-12:F4} {c.ServiceTime,-14:F4} {c.TimeInSystem:F4}");
            }

            File.WriteAllText(file, sb.ToString());
            return file;
        }

        /// <summary>Exports customer-level data to CSV for Excel/analysis.</summary>
        public static string ExportCsv(SimulationResult r, string folder = "Reports")
        {
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"{r.ModelName.Replace("/","_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("CustomerID,ArrivalTime_min,ServiceStart_min,ServiceEnd_min," +
                          "WaitInQueue_min,ServiceDuration_min,TotalTimeInSystem_min");

            foreach (var c in r.ServedCustomers)
                sb.AppendLine($"{c.Id},{c.ArrivalTime:F4},{c.ServiceStartTime:F4}," +
                              $"{c.ServiceEndTime:F4},{c.WaitInQueue:F4}," +
                              $"{c.ServiceTime:F4},{c.TimeInSystem:F4}");

            File.WriteAllText(file, sb.ToString());
            return file;
        }

        /// <summary>Exports a comparison table across multiple models to CSV.</summary>
        public static string ExportComparisonCsv(List<SimulationResult> results, string folder = "Reports")
        {
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"ModelComparison_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("Model,Lambda,Mu,Servers,Rho,Theoretical_Wq_min,Theoretical_W_min," +
                          "Simulated_Wq_min,Simulated_W_min,Utilization");

            foreach (var r in results)
                sb.AppendLine($"{r.ModelName},{r.Lambda},{r.Mu},{r.Servers}," +
                              $"{r.Theoretical_Rho:F4},{r.Theoretical_Wq:F4},{r.Theoretical_W:F4}," +
                              $"{r.AvgWaitInQueue:F4},{r.AvgTimeInSystem:F4},{r.ServerUtilization:F4}");

            File.WriteAllText(file, sb.ToString());
            return file;
        }
    }
}
