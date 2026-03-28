using System;

namespace QueueingSystem.Simulation
{
    /// <summary>
    /// Generates random variates for different service time distributions.
    /// Used by M/G/1 and G/G/1 simulations.
    /// </summary>
    public class RandomVariates
    {
        private readonly Random _rng;

        public RandomVariates(int seed = 42) { _rng = new Random(seed); }

        /// <summary>Exponential distribution — used in M/M/1, M/M/c, M/G/1 arrivals</summary>
        public double Exponential(double rate) =>
            -Math.Log(1.0 - _rng.NextDouble()) / rate;

        /// <summary>
        /// Erlang-k distribution (sum of k exponentials).
        /// Mean = k/mu, Var = k/mu², Cs = 1/√k  (less variable than exponential)
        /// Used in M/G/1 to model predictable service (e.g. fixed checkout steps).
        /// </summary>
        public double Erlang(int k, double mu)
        {
            double sum = 0;
            for (int i = 0; i < k; i++)
                sum += Exponential(mu * k);
            return sum;
        }

        /// <summary>
        /// Uniform distribution [a, b].
        /// Mean = (a+b)/2, Cs = (b-a) / (√3 · (a+b))
        /// Used in G/G/1 to model bounded service times.
        /// </summary>
        public double Uniform(double a, double b) =>
            a + _rng.NextDouble() * (b - a);

        /// <summary>
        /// Normal distribution (Box-Muller). Clamped to > 0.
        /// Mean = mean, Cs = stddev/mean
        /// </summary>
        public double Normal(double mean, double stddev)
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            return Math.Max(0.001, mean + z * stddev);
        }

        /// <summary>
        /// Log-normal distribution.
        /// High variability — models bursty service times (e.g. complex transactions).
        /// Cs can be > 1.
        /// </summary>
        public double LogNormal(double mean, double stddev)
        {
            double sigma2 = Math.Log(1 + (stddev * stddev) / (mean * mean));
            double mu_ln  = Math.Log(mean) - sigma2 / 2;
            double sigma  = Math.Sqrt(sigma2);
            double u1     = 1.0 - _rng.NextDouble();
            double u2     = 1.0 - _rng.NextDouble();
            double z      = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            return Math.Exp(mu_ln + sigma * z);
        }

        /// <summary>Uniform inter-arrival for G/G/1 (non-Poisson arrivals)</summary>
        public double UniformArrival(double mean, double ca)
        {
            // ca = coefficient of variation of inter-arrivals
            // Uniform: ca = (b-a)/(√3·(a+b)) => solve for [a,b] given mean and ca
            double halfRange = mean * ca * Math.Sqrt(3);
            double a = Math.Max(0.001, mean - halfRange);
            double b = mean + halfRange;
            return Uniform(a, b);
        }
    }
}
