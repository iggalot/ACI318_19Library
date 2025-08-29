using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;

namespace ACI318_19Library
{
    /// <summary>
    /// Ductility classification per ACI 318-19 for flexure φ selection.
    /// </summary>
    public enum DuctilityClass
    {
        TensionControlled,
        Transition,
        CompressionControlled
    }

    public class CrossSection
    {
        private Func<double, double> Fx_eq;  // The sum of forces X = 0 equation for all our terms -- this equation is needed by the solver

        // Geometry
        public double Width { get; set; }   // b, in
        public double Depth { get; set; }   // h, in
        public double Cover { get; set; }   // in

        // Materials
        public double Fck { get; set; }     // psi (f'c)
        public double Fy { get; set; }      // psi

        // Reinforcement layers
        public List<RebarLayer> TensionRebars { get; set; } = new List<RebarLayer>();
        public List<RebarLayer> CompressionRebars { get; set; } = new List<RebarLayer>();

        // constants
        private const double EpsilonCu = 0.003;       // ultimate concrete strain
        private const double Es = 29000000.0;         // psi (modulus of steel)

        public CrossSection(double width, double depth, double cover, double fck, double fy)
        {
            Width = width;
            Depth = depth;
            Cover = cover;
            Fck = fck;
            Fy = fy;
        }

        public void AddTensionRebar(string barSize, int count, RebarCatalog catalog, double depth)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double d = depth; // centroid depth from top compression fiber
            TensionRebars.Add(new RebarLayer(barSize, count, rebar, d));
        }

        public void AddCompressionRebar(string barSize, int count, RebarCatalog catalog, double depth)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double dPrime = depth; // centroid depth from top compression fiber
            CompressionRebars.Add(new RebarLayer(barSize, count, rebar, dPrime));
        }

        private static string AppendWarning(string existing, string add)
        {
            if (string.IsNullOrWhiteSpace(existing)) return add;
            return existing + " " + add;
        }

        /// <summary>
        /// Helper: sum all tension steel areas
        /// </summary>
        private double AsT_total()
        {
            return TensionRebars.Sum(r => r.SteelArea);
        }


        // Helper: effective tension steel centroid
        private double dEffective()
        {
            if (TensionRebars.Count == 0) return Depth - Cover - 0.5;
            return TensionRebars.Sum(l => l.SteelArea * l.Depth) / TensionRebars.Sum(l => l.SteelArea);
        }



        private double GetBeta1(double fck)
        {
            // ACI 318-19 Table 22.2.2.4.3 approximation
            if (fck <= 4000) return 0.85;
            if (fck >= 8000) return 0.65;
            return 0.85 - 0.05 * ((fck - 4000.0) / 1000.0);
        }

        public DesignResult ComputeFlexuralStrength_AllSteel(double Mu)
        {
            return ComputeFlexuralStrength_AllSteel(Mu, Width, Depth, Fck, Fy, Es, TensionRebars, CompressionRebars, EpsilonCu);
        }

        public DesignResult ComputeFlexuralStrength_AllSteel(double Mu, double b, double depth, double Fck, double Fy, double Es,
            List<RebarLayer> TensionRebars, List<RebarLayer> CompressionRebars, double EpsilonCu = 0.003)
        {
            double beta1 = GetBeta1(Fck);
            double tolerance = 1e-6;
            int maxIter = 200;
            string warnings = "";


            // Centroid of compression steel (d') and total Area of compression steel, Asc
            double AsC = CompressionRebars.Sum(r => r.SteelArea);
            double dPrime = (AsC > 0) ? CompressionRebars.Sum(r => r.SteelArea * r.Depth) / AsC : 0.0;

            // Centroid of tension steel and total area of tension steel, AsT
            double AsT = TensionRebars.Sum(r => r.SteelArea);
            double d = dEffective();

            // concrete factor = 0.85 * f'c * b * Beta1
            double concreteFactor = 0.85 * Fck / 1000 * b * beta1;

            // Solve using dynamically constructed F
            Fx_eq = BuildSumFxFunction(concreteFactor, AsC, dPrime, AsT, d);
            double c = SolveForX(Fx_eq, xMin: 0.001, xMax: 5.0 * depth);
            warnings +=$"\nNeutral Axis location solver = {c:F6}";

            // compute stresses in steel
            double eps_comp = -EpsilonCu + dPrime / c * EpsilonCu;
            double eps_tens = -EpsilonCu + d / c * EpsilonCu;
            warnings += $"\neps_s_prime = {eps_comp:F6} and eps_tens = {eps_tens:F6}";

            // compute stresses in steel
            double fs_prime = Math.Min(Math.Abs(eps_comp) * Es / 1000.0, Fy / 1000.0);
            double comp_sign = eps_comp / Math.Abs(eps_comp);

            double fs = Math.Min(Math.Abs(eps_tens) * Es / 1000.0, Fy / 1000.0);
            warnings += $"\nfs_prime = {comp_sign * fs_prime:F2} ksi  and fs = {fs:F2} ksi";

            // compute forces
            double Cconc = 0.85 * Fck / 1000.0 * b * beta1 * c;
            double Csteel = comp_sign * fs_prime * AsC;
            double Tsteel = fs * AsT;

            warnings += $"\nCconc = {Cconc:F2} kips  and Csteel = {Csteel:F2} kips  and Tsteel = {Tsteel:F2} kips";

            // moments about top fiber -- compressive forcs are negative, tensile forces are positive
            double Mconc = -Cconc * beta1 * c / 2.0;
            double Msteel_comp = comp_sign * fs_prime * AsC * dPrime; // comp_sign needed incase the "compression" steel actually ends up being tensile
            double Msteel_tens = fs * AsT * d;
            double Mn = Mconc + Msteel_comp + Msteel_tens;

            warnings += $"\nMtot = {Mn:F2} kip-in  and Mconc = {Mconc:F2} kip-in  and Msteel = {Msteel_comp:F2} kip-in  and Msteel = {Msteel_tens:F2} kip-in";

            // compute the tensilestrain in the extreme most tensile renforcement so that we can determine the true value of phi
            // -- search for the TensionRebar with the largest "Depth" value
            double maxDepth = TensionRebars.Max(r => r.Depth);
            double eps_y = Fy / 1000.0 / Es;
            double eps_tens_max = -EpsilonCu + maxDepth / c * EpsilonCu;
            warnings += $"\nSteel yield strain = {eps_y:F6}  Max steel tensile strain = {eps_tens_max:F6}";

            // now compute phi
            double phi = 0.9;
            if(eps_tens_max >= 0.005) { phi = 0.9; }
            else if (eps_tens_max >= 0.002) { phi = 0.65; }
            else
            {
                phi = 0.65 + (eps_tens_max - 0.002) / (0.003) * (0.90 - 0.65);
            }
            warnings += $"\nphi = {phi:F3}";

            // compute nominal moment
            double phi_Mn = Mn * phi;

            // Balanced ratio
            double rhoActual = AsT / (b * d);
            double rhoBal = 0.85 * Fck * beta1 / Fy * (EpsilonCu / (EpsilonCu + eps_y));
            if (rhoActual > rhoBal)
                warnings += "\nSection is over-reinforced; ";

            return new DesignResult
            {
                crossSection = this,
                Mu = Mu,
                Mn = Mn / 12.0,           // convert in-lb to kip-ft
                Phi = phi,
                NeutralAxis = c,
                CompressionRebars = CompressionRebars,
                TensionRebars = TensionRebars,
                Warnings = warnings,
                eps_T = eps_tens_max,
                RhoActual = rhoActual,
                RhoBalanced = rhoBal,
                Beta1 = beta1
            };
        }


        ///// <summary>
        ///// Helper: compute trial φMn for a given AsT without permanently modifying TensionRebars
        ///// </summary>
        //private FlexuralResult ComputeTrialFlexuralStrength(double AsT, double b, double d, double dPrime, double AsC)
        //{
        //    var tempLayers = new List<RebarLayer>
        //    {
        //        new RebarLayer( AsT, d)
        //    };

        //    var originalTension = TensionRebars.ToList();
        //    TensionRebars.Clear();
        //    TensionRebars.AddRange(tempLayers);

        //    var result = ComputeFlexuralStrengthPerLayer(b);

        //    // Restore original layers
        //    TensionRebars.Clear();
        //    TensionRebars.AddRange(originalTension);

        //    return result;
        //}

        public string DisplayInfo()
        {
            return
                $"Width: {Width} in\n" +
                $"Depth: {Depth} in\n" +
                $"Cover: {Cover} in\n" +
                $"Fck: {Fck} psi\n" +
                $"Fy: {Fy} psi\n";
        }

        public Func<double, double> BuildSumFxFunction(double concreteFactor, double as_c, double centroid_c, double as_t, double centroid_t)
        {
            // Define a list of terms as Func<double,double>
            var terms = new List<Func<double, double>>();

            // Example: original terms
            double A1 = 10, A2 = 20, A3 = 15, C1 = 0.1, D1 = 0.05;

            // Add concrete term
            A1 = concreteFactor;
            terms.Add(x => A1 * x);

            // Add compressive steel term
            if (as_c > 0)
            {
                A2 = as_c * 29000;  // area of compresive steel As' * Es
                C1 = 0.003 * centroid_c;  // eps_cu*d'
                terms.Add(x => (-0.003 + C1 / x) * A2);
            }

            // Add tensile steel term
            if (as_t > 0)
            {
                A3 = as_t * 29000;  // As*Es   area of tensile steel
                D1 = 0.003 * centroid_t;
                terms.Add(x => -((-0.003 + D1 / x) * A3));
            }

            // You can dynamically add more terms
            // terms.Add(x => ...);

            // Combine all terms into a single function
            return x => terms.Sum(term => term(x));
        }

        /// <summary>
        /// Helper: solve for X such that F(X) = 0
        /// </summary>
        /// <param name="f"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="tol"></param>
        /// <param name="maxIter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        double SolveForX(Func<double, double> f, double xMin = 0.001, double xMax = 100.0,
                 double tol = 1e-6, int maxIter = 1000)
        {
            double low = xMin;
            double high = xMax;
            double fLow = f(low);
            double fHigh = f(high);

            if (fLow * fHigh > 0)
                throw new Exception("Bisection method requires a sign change.");

            double mid = 0.0;
            for (int iter = 0; iter < maxIter; iter++)
            {
                mid = 0.5 * (low + high);
                double fMid = f(mid);

                if (Math.Abs(fMid) < tol)
                    return mid;

                if (fMid * fLow < 0)
                {
                    high = mid;
                    fHigh = fMid;
                }
                else
                {
                    low = mid;
                    fLow = fMid;
                }
            }
            return mid;
        }
    }
}
