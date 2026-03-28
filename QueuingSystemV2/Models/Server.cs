using System;

namespace QueueingSystem.Models
{
    public class Server
    {
        public int    Id             { get; }
        public bool   IsBusy        { get; private set; }
        public double BusyUntil     { get; private set; }
        public int    TotalServed   { get; private set; }
        public double TotalBusyTime { get; private set; }

        public Server(int id) { Id = id; }

        public void StartServing(Customer c, double startTime, double duration)
        {
            IsBusy          = true;
            BusyUntil       = startTime + duration;
            TotalBusyTime  += duration;
            c.ServiceStartTime = startTime;
            c.ServiceEndTime   = BusyUntil;
        }

        public void FinishServing()
        {
            IsBusy = false;
            TotalServed++;
        }

        public double Utilization(double simDuration) =>
            simDuration > 0 ? Math.Min(TotalBusyTime / simDuration, 1.0) : 0;
    }
}
