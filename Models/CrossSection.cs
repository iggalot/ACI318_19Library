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
        private Func<double, double> Fx_eq;  // The sum of forces X = 0 equation for all our terms -- this equation is needed by the solver

        // Geometry
        public double Width { get; set; }   // b, in
        public double Depth { get; set; }   // h, in
        public double Cover { get; set; }   // in

        // Materials
        public double Fck_psi { get; set; }     // psi (f'c)
        public double Fy_psi { get; set; }      // psi

        // Balanced reinforcement ratio ρb
        public double RhoBalanced
        {
            get
            {
                double eps_y = Fy_psi / Es_psi; // steel yield strain
                double eps_cu = 0.003;    // concrete crushing strain
                return (0.85 * Fck_psi * GetBeta1(Fck_psi)) /
                       (Fy_psi * (1.0 + eps_y / eps_cu));
            }
        }

        // Reinforcement layers
        public List<RebarLayer> TensionRebars { get; set; } = new List<RebarLayer>();
        public List<RebarLayer> CompressionRebars { get; set; } = new List<RebarLayer>();

        // constants
        private const double EpsilonCu = 0.003;       // ultimate concrete strain
        private const double Es_psi = 29000000.0;         // psi (modulus of steel)

        public CrossSection BaseClone(CrossSection section)
        {
            return new CrossSection()
            {
                Width = section.Width,
                Depth = section.Depth,
                Cover = section.Cover,
                Fck_psi = section.Fck_psi,
                Fy_psi = section.Fy_psi,
            };
        }
        // default constructor
        public CrossSection() { }

        public CrossSection(double width, double depth, double cover, double fck, double fy)
        {
            Width = width;
            Depth = depth;
            Cover = cover;
            Fck_psi = fck;
            Fy_psi = fy;
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

        // Helper: effective tension steel centroid
        private double dEffective()
        {
            if (TensionRebars.Count == 0) return Depth - Cover - 0.5;
            return TensionRebars.Sum(l => l.SteelArea * l.DepthFromTop) / TensionRebars.Sum(l => l.SteelArea);
        }



        // Max steel ratio (ACI often limits to 0.75ρb)
        public double RhoMax => 0.75 * RhoBalanced;

        // Current required ratio
        public double RhoRequired(double As_required)
        {
            return As_required / (Width * Depth);
        }

        private double GetBeta1(double fck)
        {
            // ACI 318-19 Table 22.2.2.4.3 approximation
            if (fck <= 4000) return 0.85;
            if (fck >= 8000) return 0.65;
            return 0.85 - 0.05 * ((fck - 4000.0) / 1000.0);
        }

        public DesignResultModel ComputeFlexuralStrength()
        {
            return ComputeFlexuralStrength(Width, Depth, TensionRebars, CompressionRebars, Fck_psi, Fy_psi, Es_psi, EpsilonCu);
        }

        public DesignResultModel ComputeFlexuralStrength(double b, double depth, 
            List<RebarLayer> TensionRebars, List<RebarLayer> CompressionRebars, double fck=4000, double fy=60000, double es=29000000, double EpsilonCu = 0.003)
        {
            double beta1 = GetBeta1(fck);
            double tolerance = 1e-6;
            string warnings = "";

            // Centroid of compression steel (d') and total Area of compression steel, Asc
            double AsC = CompressionRebars.Sum(r => r.SteelArea);
            double dPrime = (AsC > 0) ? CompressionRebars.Sum(r => r.SteelArea * r.DepthFromTop) / AsC : 0.0;

            // Centroid of tension steel and total area of tension steel, AsT
            double AsT = TensionRebars.Sum(r => r.SteelArea);
            double d = dEffective();

            // concrete factor = 0.85 * f'c * b * Beta1
            double concreteFactor = 0.85 * fck / 1000 * b * beta1;

            // Sum forces in x-dir = 0 equation writer
            Fx_eq = BuildSumFxFunction(concreteFactor, AsC, dPrime, AsT, d, b, beta1, fck / 1000.0, fy / 1000.0, es / 1000.0);

            // Solve using dynamically constructed F -- use Bisection method
            //double c = SolveForX(Fx_eq, xMin: 0.001, xMax: 5.0 * depth);

            // Use quadratic solveer
            //double c = SolveForNeutralAxisQuadratic(AsC, dPrime, AsT, d, b, beta1, fck / 1000.0, fy / 1000.0, es / 1000.0, EpsilonCu);

            // Use a new method:
            Func<double, double> F = c1 =>
            {
                // Concrete compressive force
                double a1 = beta1 * c1;
                double Cconc1 = 0.85 * fck * b * a1;

                // Compression steel contribution
                double Csteel1 = 0.0;
                foreach (var layer in CompressionRebars)
                {
                    double eps = EpsilonCu * (c1 - layer.DepthFromTop) / c1;
                    double fs1 = Math.Sign(eps) * Math.Min(Math.Abs(eps * es), fy);
                    Csteel1 += layer.SteelArea * fs1;
                }

                // Tension steel contribution
                double Tsteel1 = 0.0;
                foreach (var layer in TensionRebars)
                {
                    double eps = EpsilonCu * (layer.DepthFromTop - c1) / c1;
                    double fs2 = Math.Sign(eps) * Math.Min(Math.Abs(eps * es), fy);
                    Tsteel1 += layer.SteelArea * fs2;
                }

                // Equilibrium: sum of compressive forces minus tensile forces
                return Cconc1 + Csteel1 - Tsteel1;
            };

            var roots = FindAllPositiveRoots(F, 0.001, 5.0 * depth);
            warnings += $"\nRoots: ";
            foreach(var r in roots)
            {
                warnings += $"{r:F6} , ";
            }

            double c = roots.Min();
            warnings +=$"\nNeutral Axis location solver = {c:F6}";

            double a = beta1 * c;

            // compute stresses in steel  (negative for tension, positive for compression)
            double eps_comp = EpsilonCu * (c -  dPrime) / c;
            double eps_tens = -EpsilonCu * (d - c) / c ;
            warnings += $"\neps_s_prime = {eps_comp:F6} and eps_tens = {eps_tens:F6}";

            // compute stresses in steel
            double fs = Math.Sign(eps_tens) * Math.Min(Math.Abs(eps_tens * es), fy);
            double fs_prime = Math.Sign(eps_comp) * Math.Min(Math.Abs(eps_comp * es), fy);
            warnings += $"\nfs_prime = {fs_prime:F2} ksi  and fs = {fs:F2} ksi";

            // compute forces
            double Cconc = 0.85 * fck * b * a;
            double Csteel = fs_prime * AsC;
            double Tsteel = fs * AsT;
            warnings += $"\nCconc = {Cconc:F2} kips  and Csteel = {Csteel:F2} kips  and Tsteel = {Tsteel:F2} kips";

            // moments about top fiber -- CW positive -- compressive forcs are negative, tensile forces are positive
            double Mconc_kipin = Cconc * a / 2.0;
            double Msteel_comp_kipin = Csteel* dPrime; // comp_sign needed incase the "compression" steel actually ends up being tensile
            double Msteel_tens__kipin = Tsteel * d;
            double Mn_kipin = Mconc_kipin + Msteel_comp_kipin + Msteel_tens__kipin;
            warnings += $"\nMtot = {Mn_kipin:F2} kip-in  and Mconc_kipin = {Mconc_kipin:F2} kip-in  and Msteel_comp_kipin = {Msteel_comp_kipin:F2} kip-in  and Msteel = {Msteel_tens__kipin:F2} kip-in";

            // alternative method -- compute about the Tensile steel -- CW negative...so need to flip the signs
            double Mconc2_kipin = -Cconc * (d - a / 2);
            double Msteel_comp2_kipin = -Csteel * (d - dPrime); // comp_sign needed incase the "compression" steel actually ends up being tensile
            double Mn2_kipin = Mconc2_kipin + Msteel_comp2_kipin;
            warnings += $"\nMtot = {Mn2_kipin:F2} kip-in  and Mconc_kipin = {Mconc2_kipin:F2} kip-in  and Msteel_comp_2 = {Msteel_comp2_kipin:F2} kip-in";

            // verify they mathch
            double diff = Mn_kipin - Mn2_kipin;
            warnings += $"\n-- Checking methods for Mn_kipin diff = {diff:F2} kip-in";


            // compute the tensilestrain in the extreme most tensile renforcement so that we can determine the true value of phi
            // -- search for the TensionRebar with the largest "DepthFromTop" value
            double maxDepth = TensionRebars.Max(r => r.DepthFromTop);
            double eps_y = fy / es;
            double eps_tens_max = -EpsilonCu + maxDepth / c * EpsilonCu;
            warnings += $"\nConcrete strain = {EpsilonCu:F6}    Steel yield strain = {eps_y:F6}  Max actual steel tensile strain = {eps_tens_max:F6}";

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
            double phi_Mn = Mn_kipin * phi;

            // Balanced ratio
            double rhoActual = AsT / (b * d);
            double rhoBal = 0.85 * fck * beta1 / fy * (EpsilonCu / (EpsilonCu + eps_y));
            if (rhoActual > rhoBal)
                warnings += "\nSection is over-reinforced; ";

            return new DesignResultModel
            {
                crossSection = this,
                Mn = Math.Abs(Mn_kipin),           // convert in-lb to kip-ft
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

        public string DisplayInfo()
        {
            return
                $"Width: {Width} in\n" +
                $"DepthFromTop: {Depth} in\n" +
                $"Cover: {Cover} in\n" +
                $"fck: {Fck_psi} psi\n" +
                $"fy_ksi: {Fy_psi} psi\n";
        }


        public Func<double, double> BuildSumFxFunction(double concreteFactor, double AsC, double dPrime, double AsT, double d, double width,double beta1, double fck_ksi, double fy_ksi, double Es_ksi)
        {
            // Return F(c) = Cc + Cs - Ts
            return c =>
            {
                // Whitney block depth
                double a = beta1 * c;

                // Concrete compressive force
                double Cc = 0.85 * fck_ksi * width * a; // kips if fck_ksi in ksi, b and a in in

                // Compressive steel force (if any)
                double Cs = 0.0;
                if (AsC > 0)
                {
                    double epsC = EpsilonCu * (c - dPrime) / c;
                    double fsC = Math.Sign(epsC) * Math.Min(Math.Abs(epsC * Es_ksi), fy_ksi);
                    Cs = fsC * AsC; // kips
                }

                // Tensile steel force
                double Ts = 0.0;
                if (AsT > 0)
                {
                    double epsT = EpsilonCu * (d - c) / c;
                    double fsT = Math.Sign(epsT) * Math.Min(Math.Abs(epsT * Es_ksi), fy_ksi);
                    Ts = fsT * AsT; // kips
                }

                // Equilibrium: compressive forces minus tensile forces
                return Cc + Cs - Ts;
            };
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

        /// (double concreteFactor, double AsC, double dPrime, double AsT, double d, double width,double beta1, double fck_ksi, double fy_ksi, double Es_ksi)

        public double SolveForNeutralAxisQuadratic(
            double AsC, double dPrime,
            double AsT, double d,
            double b, double beta1, double fck_ksi, double fy_ksi,
            double Es_ksi, double EpsilonCu)
        {
            // ----- Step 1: compute steel properties -----
            // -- already provided by function call


            // ----- Step 2: quadratic coefficients -----
            // Quadratic coefficients (elastic assumption)
            double A = 0.85 * fck_ksi * beta1 * b;
            double B = AsC * Es_ksi + AsT * Es_ksi;
            double C = AsT * Es_ksi * d + AsC * Es_ksi * dPrime;

            double discriminant = B * B - 4 * A * C;
            if (discriminant < 0)
                throw new Exception("No real neutral axis root found!");

            double sqrtDisc = Math.Sqrt(discriminant);
            double r1 = (-B + sqrtDisc) / (2 * A);
            double r2 = (-B - sqrtDisc) / (2 * A);

            // Collect positive roots
            var positiveRoots = new List<double>();
            if (r1 > 0) positiveRoots.Add(r1);
            if (r2 > 0) positiveRoots.Add(r2);

            if (positiveRoots.Count == 0)
                throw new Exception("No positive neutral axis found");

            // Pick smallest positive root
            return positiveRoots.Min();
        }

        public List<double> FindAllPositiveRoots(Func<double, double> F, double cMin, double cMax, int samples = 1000)
        {
            var roots = new List<double>();
            double prevC = cMin;
            double prevF = F(prevC);

            for (int i = 1; i <= samples; i++)
            {
                double c = cMin + i * (cMax - cMin) / samples;
                double f = F(c);

                if (prevF * f < 0) // sign change detected
                {
                    // Refine using bisection
                    double root = RefineRootBisection(F, prevC, c);
                    if (root > 0) roots.Add(root);
                }

                prevC = c;
                prevF = f;
            }

            return roots;
        }

        private double RefineRootBisection(Func<double, double> F, double low, double high, double tol = 1e-6, int maxIter = 100)
        {
            double fLow = F(low);
            double fHigh = F(high);

            if (fLow * fHigh > 0) throw new Exception("No sign change");

            double mid = 0.0;
            for (int iter = 0; iter < maxIter; iter++)
            {
                mid = 0.5 * (low + high);
                double fMid = F(mid);

                if (Math.Abs(fMid) < tol) return mid;

                if (fLow * fMid < 0)
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

        /// <summary>
        /// Limiting singly-reinforced capacity at ρmax (tension-controlled, φ = 0.90).
        /// Returns AsMax (in^2), MnMax (in-lb), PhiMnMax (in-lb).
        /// </summary>
        public (double AsMax, double MnMax, double PhiMnMax) GetSinglyReinforcedLimit()
        {
            double dEff = Depth - Cover;
            double b = Width;
            double fc = Fck_psi;
            double fy = Fy_psi;

            double rhoMax = RhoMax;
            double AsMax = rhoMax * b * dEff;

            // Whitney block depth
            double a = (AsMax * fy) / (0.85 * fc * b);

            // Nominal moment (in-lb)
            double Mn = AsMax * fy * (dEff - a / 2.0);

            // Tension-controlled phi
            double phi = 0.90;
            double PhiMn = phi * Mn;

            return (AsMax, Mn, PhiMn);
        }
    }
}
