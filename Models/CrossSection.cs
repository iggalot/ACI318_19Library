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

        public DesignResult ComputeFlexuralStrength_AllSteel()
        {
            return ComputeFlexuralStrength_AllSteel(Width, Depth, Fck, Fy, Es, TensionRebars, CompressionRebars, EpsilonCu);
        }

        public DesignResult ComputeFlexuralStrength_AllSteel(double b, double depth, double Fck, double Fy, double Es,
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
            double Xsol = SolveForX(Fx_eq, xMin: 0.001, xMax: 5.0 * depth);
            warnings +=$"\nNeutral Axis location solver = {Xsol:F6}";










            // Total tension steel
            double trialAs = TensionRebars.Sum(r => r.SteelArea);

            // Bisection bounds for neutral axis
            double cLow = 0.001;
            double cHigh = d * 5.0;
            double c = 0.0;
            int iter = 0;

            while (iter < maxIter)
            {
                c = 0.5 * (cLow + cHigh);
                double a = beta1 * c;

                // Concrete compressive force
                double Cconc = 0.85 * Fck * b * a;

                // Steel forces (all layers)
                double Fsteel = 0.0;
                foreach (var layer in TensionRebars.Concat(CompressionRebars))
                {
                    double eps = EpsilonCu * (layer.Depth - c) / c;
                    double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                    Fsteel += layer.SteelArea * fs;
                }

                // Equilibrium: concrete + steel vs zero axial load
                double F = Cconc - Fsteel;

                if (Math.Abs(F) < tolerance) break;

                if (F > 0)
                    cHigh = c; // forces too high, reduce c
                else
                    cLow = c;  // forces too low, increase c

                iter++;
            }

            double aFinal = beta1 * c;

            // Concrete moment about tension face
            double Mconcrete = 0.85 * Fck * b * aFinal * (d - aFinal / 2.0);

            // Steel moment contribution (all layers)
            double SteelMoment = 0.0;
            double tensile_steel = 0.0;
            double compression_steel = 0.0;

            foreach (var layer in TensionRebars.Concat(CompressionRebars))
            {
                double eps = EpsilonCu * (layer.Depth - c) / c;
                double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                double leverArm = layer.Depth - aFinal / 2.0;
                SteelMoment += layer.SteelArea * fs * leverArm;

                if (fs < Fy && layer.Depth < c)
                    warnings += $"\nCompression steel at {layer.Depth:F2} in did not yield; ";

                if (TensionRebars.Contains(layer))
                {
                    tensile_steel += layer.SteelArea;
                } else if (CompressionRebars.Contains(layer))
                {
                    compression_steel += layer.SteelArea;
                }
            }

            double Mn = Mconcrete + SteelMoment;

            // φ factor per ACI318-19
            double epsTmax = 0.005;
            double epsT = (TensionRebars.Count > 0) ? EpsilonCu * (d - c) / c : 0.006;
            double phi = (epsT >= epsTmax) ? 0.9 : Math.Max(0.65, 0.65 + 0.25 * (epsT / epsTmax));

            double Mu = Mn * phi;

            // Balanced ratio
            double rhoActual = trialAs / (b * d);
            double rhoBal = 0.85 * Fck * b * beta1 * c / (Fy * d);
            if (rhoActual > rhoBal)
                warnings += "\nSection is over-reinforced; ";

            return new DesignResult
            {
                Mn = Mn / 12.0,           // convert in-lb to kip-ft
                Mu = Mu / 12.0,           // kip-ft
                Phi = phi,
                NeutralAxis = c,
                RequiredAs = trialAs,
                ProvidedAs_Tension = tensile_steel,
                ProvidedAs_Compression = compression_steel,
                Iterations = iter,
                Warnings = warnings,
                eps_T = epsT
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
