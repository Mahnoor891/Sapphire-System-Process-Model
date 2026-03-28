using System;
using System.Collections.Generic;

namespace QueueingSystem.Models
{
    public class SimulationResult
    {
        public string ModelName        { get; set; } = "";
        public double Lambda           { get; set; }
        public double Mu               { get; set; }
        public int    Servers          { get; set; }
        public int    SimDurationMin   { get; set; }

        // Simulated
        public int    TotalArrived     { get; set; }
        public int    TotalServed      { get; set; }
        public double AvgWaitInQueue   { get; set; }
        public double AvgTimeInSystem  { get; set; }
        public double MaxWaitInQueue   { get; set; }
        public double ServerUtilization{ get; set; }

        // Theoretical
        public double Theoretical_Wq   { get; set; }
        public double Theoretical_W    { get; set; }
        public double Theoretical_Lq   { get; set; }
        public double Theoretical_L    { get; set; }
        public double Theoretical_Rho  { get; set; }

        public List<Customer> ServedCustomers { get; set; } = new();
    }
}
