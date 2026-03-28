using System;

namespace QueueingSystem.Models
{
    public class Customer
    {
        private static int _idCounter = 1;

        public int    Id               { get; }
        public double ArrivalTime      { get; set; }
        public double ServiceStartTime { get; set; }
        public double ServiceEndTime   { get; set; }
        public bool   Balked           { get; set; }

        public double WaitInQueue   => ServiceStartTime - ArrivalTime;
        public double ServiceTime   => ServiceEndTime   - ServiceStartTime;
        public double TimeInSystem  => ServiceEndTime   - ArrivalTime;

        public Customer(double arrivalTime)
        {
            Id          = _idCounter++;
            ArrivalTime = arrivalTime;
        }

        public static void Reset() => _idCounter = 1;
    }
}
