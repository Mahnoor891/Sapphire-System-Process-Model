using System;

namespace QueueingSystem.Models
{
    /// <summary>
    /// Analytical formulas for all four queueing models.
    /// M/M/1 : Poisson arrivals, Exponential service, 1 server
    /// M/G/1 : Poisson arrivals, General service,     1 server  (Pollaczek-Khinchine)
    /// G/G/1 : General arrivals, General service,     1 server  (Kingman's approximation)
    /// M/M/c : Poisson arrivals, Exponential service, c servers (Erlang C)
    /// </summary>
    public static class QueueFormulas
    {
        // ══════════════════════════════════════════════════════════════
        //  M/M/1
        // ══════════════════════════════════════════════════════════════

        public static double MM1_Rho(double lambda, double mu) => lambda / mu;

        /// <summary>Average number in queue: Lq = ρ²/(1−ρ)</summary>
        public static double MM1_Lq(double lambda, double mu)
        {
            double rho = MM1_Rho(lambda, mu);
            if (rho >= 1) return double.PositiveInfinity;
            return (rho * rho) / (1 - rho);
        }

        /// <summary>Average number in system: L = ρ/(1−ρ)</summary>
        public static double MM1_L(double lambda, double mu)
        {
            double rho = MM1_Rho(lambda, mu);
            if (rho >= 1) return double.PositiveInfinity;
            return rho / (1 - rho);
        }

        /// <summary>Average wait in queue (hours): Wq = λ/[μ(μ−λ)]</summary>
        public static double MM1_Wq(double lambda, double mu)
        {
            if (lambda >= mu) return double.PositiveInfinity;
            return lambda / (mu * (mu - lambda));
        }

        /// <summary>Average time in system (hours): W = 1/(μ−λ)</summary>
        public static double MM1_W(double lambda, double mu)
        {
            if (lambda >= mu) return double.PositiveInfinity;
            return 1.0 / (mu - lambda);
        }

        public static double MM1_P0(double lambda, double mu) => 1 - MM1_Rho(lambda, mu);

        // ══════════════════════════════════════════════════════════════
        //  M/G/1  (Pollaczek-Khinchine mean value formula)
        //  Requires: mean service time E[S] = 1/mu
        //            service time variance Var[S]
        //  Coefficient of variation: Cs = StdDev[S] / E[S]
        // ══════════════════════════════════════════════════════════════

        public static double MG1_Rho(double lambda, double mu) => lambda / mu;

        /// <summary>
        /// P-K formula: Lq = ρ² + λ²·Var[S]  /  2(1−ρ)
        /// Equivalently using Cs: Lq = ρ²(1 + Cs²) / 2(1−ρ)
        /// </summary>
        public static double MG1_Lq(double lambda, double mu, double cs)
        {
            double rho = MG1_Rho(lambda, mu);
            if (rho >= 1) return double.PositiveInfinity;
            return (rho * rho * (1 + cs * cs)) / (2 * (1 - rho));
        }

        public static double MG1_Wq(double lambda, double mu, double cs)
        {
            double lq = MG1_Lq(lambda, mu, cs);
            if (double.IsInfinity(lq)) return double.PositiveInfinity;
            return lq / lambda;
        }

        public static double MG1_W(double lambda, double mu, double cs) =>
            MG1_Wq(lambda, mu, cs) + (1.0 / mu);

        public static double MG1_L(double lambda, double mu, double cs) =>
            lambda * MG1_W(lambda, mu, cs);

        // ══════════════════════════════════════════════════════════════
        //  G/G/1  (Kingman's approximation / VCA formula)
        //  Ca = coeff. of variation of inter-arrival times
        //  Cs = coeff. of variation of service times
        // ══════════════════════════════════════════════════════════════

        public static double GG1_Rho(double lambda, double mu) => lambda / mu;

        /// <summary>
        /// Kingman's approximation:
        /// Wq ≈ (ρ/(μ(1−ρ))) · (Ca² + Cs²)/2
        /// </summary>
        public static double GG1_Wq(double lambda, double mu, double ca, double cs)
        {
            double rho = GG1_Rho(lambda, mu);
            if (rho >= 1) return double.PositiveInfinity;
            double mmWq = rho / (mu * (1 - rho));          // M/M/1 base
            double cvFactor = (ca * ca + cs * cs) / 2.0;  // variability factor
            return mmWq * cvFactor;
        }

        public static double GG1_W(double lambda, double mu, double ca, double cs) =>
            GG1_Wq(lambda, mu, ca, cs) + (1.0 / mu);

        public static double GG1_Lq(double lambda, double mu, double ca, double cs) =>
            lambda * GG1_Wq(lambda, mu, ca, cs);

        public static double GG1_L(double lambda, double mu, double ca, double cs) =>
            lambda * GG1_W(lambda, mu, ca, cs);

        // ══════════════════════════════════════════════════════════════
        //  M/M/c  (Erlang C)
        // ══════════════════════════════════════════════════════════════

        public static double MMc_Rho(double lambda, double mu, int c) =>
            lambda / (c * mu);

        public static double MMc_P0(double lambda, double mu, int c)
        {
            double r = lambda / mu;
            double sum = 0;
            double fact = 1;
            for (int n = 0; n < c; n++)
            {
                if (n > 0) fact *= n;
                sum += Math.Pow(r, n) / fact;
            }
            fact *= c;
            double rho = lambda / (c * mu);
            double last = Math.Pow(r, c) / fact / (1 - rho);
            return 1.0 / (sum + last);
        }

        public static double MMc_ErlangC(double lambda, double mu, int c)
        {
            double r    = lambda / mu;
            double rho  = lambda / (c * mu);
            double p0   = MMc_P0(lambda, mu, c);
            double fact = 1;
            for (int n = 1; n <= c; n++) fact *= n;
            return (Math.Pow(r, c) / fact) * (1.0 / (1 - rho)) * p0;
        }

        public static double MMc_Lq(double lambda, double mu, int c)
        {
            double rho = MMc_Rho(lambda, mu, c);
            if (rho >= 1) return double.PositiveInfinity;
            return MMc_ErlangC(lambda, mu, c) * rho / (1 - rho);
        }

        public static double MMc_Wq(double lambda, double mu, int c) =>
            MMc_Lq(lambda, mu, c) / lambda;

        public static double MMc_W(double lambda, double mu, int c) =>
            MMc_Wq(lambda, mu, c) + (1.0 / mu);

        public static double MMc_L(double lambda, double mu, int c) =>
            lambda * MMc_W(lambda, mu, c);
    }
}
