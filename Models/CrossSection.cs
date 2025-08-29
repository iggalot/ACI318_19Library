using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Compute flexural capacity by solving for neutral axis depth c (bisection),
        /// accounting for elastic/yield behavior of both tension and compression steel.
        /// Returns flexural result (Mn, phiMn, strains, warnings, etc).
        /// </summary>
        /// <param name="bEff">Optional effective width to use instead of section width (in)</param>
        public FlexuralResult ComputeFlexuralStrength(double? bEff = null)
        {
            double b = bEff ?? Width;

            double AsT = TensionRebars.Sum(r => r.SteelArea);
            double AsC = CompressionRebars.Sum(r => r.SteelArea);

            if (AsT <= 0)
                throw new InvalidOperationException("No tension reinforcement defined.");

            double d = TensionRebars.Average(r => r.Depth);
            double dPrime = (CompressionRebars.Count > 0) ? CompressionRebars.Average(r => r.Depth) : 0.0;

            double beta1 = GetBeta1(Fck);
            double epsY = Fy / Es;

            // Balanced reinforcement ratio rho_b per ACI (ρ_b = 0.85*β1*(f'c/fy)*(ε_cu/(ε_cu+ε_y)))
            double rhoBalanced = 0.85 * beta1 * (Fck / Fy) * (EpsilonCu / (EpsilonCu + epsY));

            // Reinforcement ratio rho
            double rho = AsT / (b * d);

            // We'll solve for c numerically using bisection on force equilibrium:
            // F(c) = C_conc + C_steel - T_steel = 0
            // where:
            //  C_conc = 0.85 * f'c * b * a  (a = beta1 * c)
            //  C_steel = AsC * f_sc (positive if compressive)
            //  T_steel = AsT * f_st (positive if tensile)
            //
            // f_sc and f_st come from strains which depend on c:
            //  eps_st = eps_cu * (d - c) / c
            //  eps_sc = eps_cu * (c - d') / c
            //
            // steel stress: f = Es*eps (clamped to ±fy)

            Func<double, double> equilibrium = (c) =>
            {
                if (c <= 0) return double.PositiveInfinity;

                double a = beta1 * c;
                double Cconc = 0.85 * Fck * b * a; // compressive force in concrete

                // tension steel strain & stress
                double eps_st = EpsilonCu * (d - c) / c;
                double fst = Es * eps_st;
                // clamp to yield in tension (direction)
                if (Math.Abs(fst) >= Fy)
                    fst = Math.Sign(fst) * Fy;

                // compression steel strain & stress
                double eps_sc = EpsilonCu * (c - dPrime) / c;
                double fsc = Es * eps_sc;
                if (Math.Abs(fsc) >= Fy)
                    fsc = Math.Sign(fsc) * Fy;

                // Force sign convention:
                // Cconc positive (compressive)
                // Csteel positive if compression (fsc > 0), negative if in tension
                // Tsteel positive (tensile) = AsT * fst (fst should be positive in tension)
                double Csteel = AsC * fsc; // may be negative if compression bars are actually in tension
                double Tsteel = AsT * fst; // may be negative if steel in compression (unlikely if c < d)
                // We want residual = compressive - tensile
                return (Cconc + Csteel) - Tsteel;
            };

            // Setup bisection bounds
            double cLow = 1e-6;
            double cHigh = Math.Max(d * 5.0, Depth * 2.0); // reasonably large upper bound

            double fLow = equilibrium(cLow);
            double fHigh = equilibrium(cHigh);

            // If no sign change, try expanding cHigh
            int expandAttempts = 0;
            while (fLow * fHigh > 0 && expandAttempts < 20)
            {
                cHigh *= 2.0;
                fHigh = equilibrium(cHigh);
                expandAttempts++;
            }

            if (fLow * fHigh > 0)
            {
                // Fall back: try using approximate linear a from simple assumption: a = (AsT*Fy - AsC*Fy)/(0.85*f'c*b)
                // This may produce negative/invalid a, so set a small positive and proceed.
                double aApprox = (AsT * Fy - AsC * Fy) / (0.85 * Fck * b);
                if (aApprox <= 1e-6) aApprox = 1e-3;
                double cApprox = aApprox / beta1;
                // still continue but warn
                var resApprox = ComputeResultFromC(cApprox, b, AsT, AsC, d, dPrime, beta1, rho, rhoBalanced, epsY);
                resApprox.Warnings = AppendWarning(resApprox.Warnings, "Equilibrium root-finding failed (no sign change). Returned approximate result based on estimated c.");
                return resApprox;
            }

            // Bisection
            double cMid = 0;
            for (int iter = 0; iter < 200; iter++)
            {
                cMid = 0.5 * (cLow + cHigh);
                double fMid = equilibrium(cMid);

                if (Math.Abs(fMid) < 1e-6)
                    break;

                // decide side
                if (fLow * fMid <= 0)
                {
                    cHigh = cMid;
                    fHigh = fMid;
                }
                else
                {
                    cLow = cMid;
                    fLow = fMid;
                }
            }

            // Compute final results from found c
            var result = ComputeResultFromC(cMid, b, AsT, AsC, d, dPrime, beta1, rho, rhoBalanced, epsY);

            return result;
        }

        private static string AppendWarning(string existing, string add)
        {
            if (string.IsNullOrWhiteSpace(existing)) return add;
            return existing + " " + add;
        }

        private FlexuralResult ComputeResultFromC(double c,
                                                  double b,
                                                  double AsT,
                                                  double AsC,
                                                  double d,
                                                  double dPrime,
                                                  double beta1,
                                                  double rho,
                                                  double rhoBalanced,
                                                  double epsY)
        {
            var res = new FlexuralResult();
            res.AsT = AsT;
            res.AsC = AsC;
            res.As = AsT + AsC;
            res.d = d;
            res.dPrime = dPrime;

            // a and concrete compression
            double a = beta1 * c;
            double Cconc = 0.85 * Fck * b * a;

            // strains
            double eps_t = EpsilonCu * (d - c) / c;
            double eps_sc = EpsilonCu * (c - dPrime) / c;

            // steel stresses (signed): positive = tension, negative = compression in sign convention of Es*eps
            double fs_t = Es * eps_t;
            double fs_c = Es * eps_sc;

            // clamp to yield magnitudes but keep sign
            if (Math.Abs(fs_t) >= Fy)
                fs_t = Math.Sign(fs_t) * Fy;
            if (Math.Abs(fs_c) >= Fy)
                fs_c = Math.Sign(fs_c) * Fy;

            // Forces: Tsteel (positive tensile) = AsT * fs_t
            //        Csteel (compressive positive) = AsC * (-fs_c) if fs_c negative means compressive?
            // We'll compute with consistent sign:
            //   Take fs_t positive for tension, fs_c positive for compression:
            double f_st_mag = fs_t; // should be positive for tensile (if negative, then it's compressive)
            double f_sc_mag = -fs_c; // if fs_c is negative (compression), f_sc_mag positive. If fs_c positive (tension), f_sc_mag negative.

            double Tsteel = AsT * f_st_mag; // tensile force (may be negative if steel actually compressive)
            double Csteel = AsC * f_sc_mag; // compressive force (may be negative if steel actually tensile)

            // Nominal moment: contribution of tension steel about compression resultant (lever arm from steel to centroid of compression block)
            // Use: Mn = AsT * f_st * (d - a/2) + AsC * f_sc_comp * (d - d')
            // Where f_sc_comp is compressive stress magnitude (positive).
            // If compression steel actually in tension, its contribution will be negative (subtracted).
            double Mn = 0.0;
            // contribution from tension steel (signed)
            Mn += AsT * f_st_mag * (d - a / 2.0);
            // contribution from compression steel (use compressive magnitude Csteel and lever arm to tension resultant)
            // If Csteel positive (actual compression), its moment arm to tension resultant is (d - dPrime)
            Mn += Csteel * (d - dPrime);

            // Compute phi using tension steel strain eps_t
            double phi;
            DuctilityClass ductility;
            double eps_y = epsY; // passed in

            if (eps_t >= 0.005)
            {
                phi = 0.90;
                ductility = DuctilityClass.TensionControlled;
            }
            else if (eps_t <= eps_y)
            {
                phi = 0.65;
                ductility = DuctilityClass.CompressionControlled;
            }
            else
            {
                double slope = 0.25 / (0.005 - eps_y);
                phi = 0.65 + (eps_t - eps_y) * slope;
                if (phi < 0.65) phi = 0.65;
                if (phi > 0.90) phi = 0.90;
                ductility = DuctilityClass.Transition;
            }

            // Is compression steel yielding?
            bool compSteelYields = Math.Abs(fs_c) >= Fy;

            // Over-reinforced?
            bool isOver = rho > rhoBalanced;

            // Compose warnings
            string warnings = "";
            if (isOver) warnings = AppendWarning(warnings, "Section is over-reinforced (ρ > ρ_b) — compression failure likely (brittle).");
            if (!compSteelYields && AsC > 0) warnings = AppendWarning(warnings, "Compression steel does not yield; elastic stress used for compression steel.");
            if (c <= 1e-6) warnings = AppendWarning(warnings, "Neutral axis depth c is extremely small — check geometry or reinforcement.");

            // Fill result
            res.c = c;
            res.a = a;
            res.Mn = Mn;
            res.Phi = phi;
            res.PhiMn = phi * Mn;
            res.EpsilonT = eps_t;
            res.EpsilonY = eps_y;
            res.Ductility = ductility;
            res.Rho = rho;
            res.RhoBalanced = rhoBalanced;
            res.IsOverReinforced = isOver;
            res.CompressionSteelYields = compSteelYields;
            res.Warnings = warnings;

            return res;
        }

        private double GetBeta1(double fck)
        {
            // ACI 318-19 Table 22.2.2.4.3 approximation
            if (fck <= 4000) return 0.85;
            if (fck >= 8000) return 0.65;
            return 0.85 - 0.05 * ((fck - 4000.0) / 1000.0);
        }
    }

    public class RebarLayer
    {
        public string BarSize { get; private set; }
        public int Count { get; private set; }
        public Rebar Bar { get; private set; }
        public double Depth { get; private set; } // centroid depth from top fiber, in

        public RebarLayer(string barSize, int count, Rebar bar, double depth)
        {
            BarSize = barSize;
            Count = count;
            Bar = bar;
            Depth = depth;
        }

        public double SteelArea => Count * Bar.Area;
    }
}
