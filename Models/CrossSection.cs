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

        public void AddTensionRebar(string barSize, int count, RebarCatalog catalog)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double d = Depth - Cover - rebar.Diameter / 2.0; // centroid depth from top compression fiber
            TensionRebars.Add(new RebarLayer(barSize, count, rebar, d));
        }

        public void AddCompressionRebar(string barSize, int count, RebarCatalog catalog)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double dPrime = Cover + rebar.Diameter / 2.0; // centroid depth from top compression fiber
            CompressionRebars.Add(new RebarLayer(barSize, count, rebar, dPrime));
        }

        private static string AppendWarning(string existing, string add)
        {
            if (string.IsNullOrWhiteSpace(existing)) return add;
            return existing + " " + add;
        }

        /// <summary>
        /// Computes neutral axis depth 'c' (from extreme compression fiber) for a doubly reinforced rectangular section.
        /// Uses a simple bisection method to satisfy equilibrium: Cc + Cs = Ts.
        /// </summary>
        /// <param name="b">Effective width of section (in)</param>
        /// <param name="d">Effective tension steel depth (in)</param>
        /// <param name="AsC">Total compression steel area (in^2)</param>
        /// <param name="dPrime">Centroid of compression steel from top (in)</param>
        /// <param name="tolerance">Convergence tolerance (in)</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <returns>Neutral axis depth c (in)</returns>
        private double ComputeNeutralAxisDepth(double b, double d, double AsC, double dPrime, double fck, double fy, double tolerance = 1e-4, int maxIterations = 50)
        {
            double cLow = 0.1;         // cannot be zero
            double cHigh = d;          // cannot exceed tension steel depth
            double c = 0.5 * (cLow + cHigh);

            int iter = 0;
            while (iter < maxIterations)
            {
                double a = 0.85 * c; // equivalent rectangular stress block depth

                double Cc = 0.85 * fck * b * a;     // concrete compressive force
                double Cs = AsC * fy;               // compression steel force
                double Ts = AsT_total() * fy;       // total tension steel force (AsT_total is sum of tension layers)

                double res = (Cc + Cs) - Ts;

                if (Math.Abs(res) < 1e-4) // equilibrium satisfied
                    break;

                if (res > 0) // compression too high -> reduce c
                    cHigh = c;
                else         // compression too low -> increase c
                    cLow = c;

                c = 0.5 * (cLow + cHigh);
                iter++;
            }

            return c;
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

        public DesignResult DesignForMoment(double Mu_kipft,
                                    RebarCatalog catalog,
                                    double? bEff = null,
                                    double toleranceIn2 = 1e-4,
                                    int maxIterations = 60)
        {
            double b = bEff ?? Width;
            double Mu_inlb = Mu_kipft * 12000.0; // kip-ft → in-lb
            double d = dEffective();

            // Initial bounds for AsT
            double AsT_low = 1e-6;
            double AsT_high = Math.Max(0.05 * b * d, 1.0);

            // Expand upper bound until φMn ≥ Mu
            int expandCount = 0;
            while (expandCount < 40)
            {
                var trial = ComputeFlexuralStrengthPerLayer(b, AsT_high);
                if (trial.Mu >= Mu_inlb) break;
                AsT_high *= 2.0;
                expandCount++;
            }

            var highTrial = ComputeFlexuralStrengthPerLayer(b, AsT_high);
            if (highTrial.Mu < Mu_inlb)
            {
                return new DesignResult
                {
                    crossSection = this,
                    RequiredAs = AsT_high,
                    ProvidedAs = AsT_high,
                    Mu = highTrial.Mu,
                    Mn = highTrial.Mn,
                    Phi = highTrial.Phi,
                    Iterations = 0,
                    Warnings = AppendWarning(highTrial.Warnings, "Unable to reach Mu with practical AsT upper bound."),
                    //ConcreteDesignInfo = $"Required As = {AsT_high:F3} in²\nUnable to reach Mu"
                };
            }

            // Bisection to find minimal AsT
            int iter = 0;
            double AsT_final = AsT_high;

            while (iter < maxIterations && (AsT_high - AsT_low) > toleranceIn2)
            {
                double mid = 0.5 * (AsT_low + AsT_high);
                var trialMid = ComputeFlexuralStrengthPerLayer(b, mid);

                if (trialMid.Mu >= Mu_inlb)
                    AsT_high = mid;
                else
                    AsT_low = mid;

                iter++;
            }

            AsT_final = AsT_high;

            // Select practical bars from catalog
            var sortedBars = catalog.RebarTable.Values.OrderByDescending(r => r.Area).ToList();
            string chosenBar = null;
            int chosenCount = 0;
            double providedAs = 0.0;

            foreach (var bar in sortedBars)
            {
                int count = (int)Math.Ceiling(AsT_final / bar.Area);
                if (count <= 0) continue;

                double areaProvided = count * bar.Area;

                if (chosenBar == null || count < chosenCount || (count == chosenCount && areaProvided < providedAs))
                {
                    chosenBar = bar.Designation;
                    chosenCount = count;
                    providedAs = areaProvided;
                }
            }

            // Compute final flexural capacity with selected bars
            var finalResult = ComputeFlexuralStrengthPerLayer(b, providedAs);

            // Build ConcreteDesignInfo string
            string info = $"Required As = {AsT_final:F3} in²\n" +
                          $"Selected: {chosenCount} x {chosenBar} (Provided As = {providedAs:F3} in²)\n" +
                          $"φMn = {finalResult.Mu / 12000.0:F2} kip-ft, φ = {finalResult.Phi:F3}\n" +
                          $"{(string.IsNullOrWhiteSpace(finalResult.Warnings) ? "" : "Warnings: " + finalResult.Warnings)}";

            return new DesignResult
            {
                crossSection = this,
                RequiredAs = AsT_final,
                SelectedBar = chosenBar,
                BarCount = chosenCount,
                ProvidedAs = providedAs,
                Mu = Mu_inlb,
                Mn = finalResult.Mn,
                Phi = finalResult.Phi,
                Iterations = iter,
                Warnings = finalResult.Warnings,
            };
        }



        /// <summary>
        /// Automated design routine: choose As (tension steel area) to resist Mu (kip-ft).
        /// Uses existing compression steel (AsC) and section geometry/materials.
        /// Returns DesignResult with recommended bar selection using a greedy approach.
        /// </summary>
        // Updated signature
        public FlexuralResult ComputeFlexuralStrengthPerLayer(double b, double? trialAsT = null)
        {
            double beta1 = GetBeta1(Fck);
            double tolerance = 1e-6;
            int maxIter = 200;

            double d = dEffective();
            double AsC = CompressionRebars.Sum(r => r.SteelArea);
            double dPrime = (CompressionRebars.Count > 0) ? CompressionRebars.Sum(r => r.Depth * r.SteelArea) / AsC : 0.0;

            // Temporarily replace tension layers if trialAsT is supplied
            List<RebarLayer> originalTension = null;
            double trialAs = 0.0;
            if (trialAsT.HasValue)
            {
                originalTension = TensionRebars.ToList();
                TensionRebars.Clear();
                TensionRebars.Add(new RebarLayer(trialAsT.Value, d));
                trialAs = trialAsT.Value;
            }
            else
            {
                trialAs = TensionRebars.Sum(r => r.SteelArea);
            }

            double cLow = 1e-6;
            double cHigh = Depth * 5.0;
            double c = 0.0;
            int iter = 0;
            string warnings = "";

            while (iter < maxIter)
            {
                c = ComputeNeutralAxisDepth(b, d, trialAs, dPrime, Fck, Fy); // mid-point for bisection
                double a = beta1 * c;

                // Concrete compressive force
                double Cconc = 0.85 * Fck * b * a;

                // Steel forces
                double T = 0.0;
                foreach (var layer in TensionRebars)
                {
                    double eps = EpsilonCu * (layer.Depth - c) / c;
                    double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                    T += layer.SteelArea * fs;
                }

                double Csteel = 0.0;
                foreach (var layer in CompressionRebars)
                {
                    double eps = EpsilonCu * (c - layer.Depth) / c;
                    double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                    Csteel += layer.SteelArea * fs;
                }

                double F = Cconc + Csteel - T;

                if (Math.Abs(F) < tolerance) break;

                if (F > 0)
                    cHigh = c;
                else
                    cLow = c;

                iter++;
            }

            double aFinal = beta1 * c;

            // Moments
            double tensionMoment = 0.0;
            foreach (var layer in TensionRebars)
            {
                double eps = EpsilonCu * (layer.Depth - c) / c;
                double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                tensionMoment += layer.SteelArea * fs * (layer.Depth - aFinal / 2.0); // lever arm: d - a/2
            }

            double compressionMoment = 0.0;
            foreach (var layer in CompressionRebars)
            {
                double eps = EpsilonCu * (c - layer.Depth) / c;
                double fs = Math.Sign(eps) * Math.Min(Math.Abs(eps * Es), Fy);
                compressionMoment += layer.SteelArea * fs * (c - layer.Depth); // lever arm: distance to neutral axis
                if (Math.Abs(fs) < Fy)
                    warnings += $"Compression steel at {layer.Depth:F2} in did not yield; ";
            }

            double Mconcrete = 0.85 * Fck * b * aFinal * (aFinal / 2.0);
            double Mn = Mconcrete + tensionMoment + compressionMoment;

            // φ factor (lower bound 0.65)
            double epsT = (TensionRebars.Count > 0) ? EpsilonCu * (d - c) / c : 0.006;
            double epsTmax = 0.005;
            double phi = (epsT >= epsTmax) ? 0.9 : Math.Max(0.65, 0.65 + 0.25 * (epsT / epsTmax));

            // Balanced ratio
            double rhoActual = trialAs / (b * d);
            double rhoBal = 0.85 * Fck * b * beta1 * c / (Fy * d);
            if (rhoActual > rhoBal)
                warnings += "Section is over-reinforced; ";

            // Restore original tension layers
            if (trialAsT.HasValue && originalTension != null)
            {
                TensionRebars.Clear();
                TensionRebars.AddRange(originalTension);
            }

            return new FlexuralResult
            {
                Mn = Mn,
                Mu = Mn * phi,
                Phi = phi,
                NeutralAxis = c,
                AsT = trialAs,
                AsC = AsC,
                d = d,
                dPrime = dPrime,
                Beta1 = beta1,
                RhoActual = rhoActual,
                RhoBalanced = rhoBal,
                TensionLayerCount = TensionRebars.Count,
                CompressionLayerCount = CompressionRebars.Count,
                ConcreteMoment = Mconcrete,
                TensionMoment = tensionMoment,
                CompressionMoment = compressionMoment,
                Iterations = iter,
                Warnings = warnings,
                EpsY = Fy / Es
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
    }
}
