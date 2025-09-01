using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ACI318_19Library
{
    public class FlexuralDesigner
    {
        const double max_bars_per_layer = 6;

        private static Func<double, double> Fx_eq;  // The sum of forces X = 0 equation for all our terms -- this equation is needed by the solver

        /// <summary>
        /// Iterates through all candidate section dimensions and steel layers,
        /// returning a list of all combinations that meet or exceed the target moment.
        /// </summary>
        public List<DesignResultModel> DesignAllSections(
            double MuTarget_kipft,                  // target factored moment
            double tension_cover,
            double compression_cover,
            double side_cover,
            double clear_spacing,

            double fck = 4000, double fy = 60000, double es = 29000000)
        {
            List<DesignResultModel> successfulSections = new List<DesignResultModel>();
            double MuTarget_kip_in = MuTarget_kipft * 12.0; // convert to kip-in

            RebarCatalog catalog = new RebarCatalog();

            List<double> widths = new List<double>();                     // candidate widths (in)
            List<double> depths = new List<double>();                     // candidate depths (in)

            // define the tension options for bars at a distance of "h-cover" from the top of the beam
            // start with the largest first
            var barSizes = new[] { "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11" };

            // candidate compression bars for the top of the beam section
            List<RebarLayer> compressionOptions = new List<RebarLayer>();

            foreach (var size in barSizes)
            {
                for (int qty = 1; qty <= max_bars_per_layer; qty++)
                    compressionOptions.Add(new RebarLayer(size, qty, catalog.RebarTable[size], compression_cover));
            }

            // load the acceptable widths
            for (int i = 0; i < 32; i++)
            {
                widths.Add(4 + 1.0 * i);
            }
            // load the acceptable depths
            for (int i = 0; i < 32; i++)
            {
                depths.Add(4 + 1.0 * i);
            }

            foreach (var b in widths)
            {
                foreach (var h in depths)
                {
                    var section = new CrossSection
                    {
                        Width = b,
                        Depth = h,
                        TensionCover = tension_cover,
                        CompressionCover = compression_cover,
                        SideCover = side_cover,
                        ClearSpacing = clear_spacing,
                        Fck_psi = fck,
                        Fy_psi = fy,
                    };


                    var tensionOptions = new List<RebarLayer>();

                    foreach (var size in barSizes)
                    {
                        for (int qty = 1; qty <= max_bars_per_layer; qty++)
                            tensionOptions.Add(new RebarLayer(size, qty, catalog.RebarTable[size], h - tension_cover));
                    }

                    // choose the largest and check if the moment is enough.  If it is, we can iterate through all the bar sizes
                    // otherwise there's no point in continuing with this depth iteration.
                    List<RebarLayer> rebarLayer = tensionOptions.OrderByDescending(x => x.SteelArea).ToList();


                    DesignResultModel first_design = null;
                    if(rebarLayer.Count > 0)
                    {
                        RebarLayer first_test = rebarLayer[0];
                        section.AddTensionRebar(first_test.BarSize, first_test.Qty, catalog, first_test.DepthFromTop);
                        first_design = ComputeFlexuralStrength(section);
                    }

                    // Try single-layer tension reinforcement first
                    bool foundTensionOnlySolution = false;

                    // Order small to large (try most economical first)
                    foreach (var tensLayer in tensionOptions.OrderBy(opt => opt.SteelArea))
                    {
                        var trialSection = section.BaseClone(section); // copy the section without the rebar
                        trialSection.AddTensionRebar(tensLayer.BarSize, tensLayer.Qty, catalog, tensLayer.DepthFromTop);

                        // does the rebar fit in the width of the current section
                        if (RebarLayerFitsInWidth(trialSection) is false)
                        {
                            continue;
                        }

                        var result = ComputeFlexuralStrength(trialSection);

                        if (result.PhiMn >= MuTarget_kip_in && result.eps_T > 0.005)
                        {
                            successfulSections.Add(result);
                            foundTensionOnlySolution = true;
                            break; // optional: stop at first success
                        }
                    }

                    // If no pure tension solution, try doubly reinforced
                    bool success = false;
                    if (!foundTensionOnlySolution)
                    {
                        foreach (var tensLayer in tensionOptions.OrderBy(opt => opt.SteelArea))
                        {
                            foreach (var compSize in barSizes)
                            {
                                for (int compQty = 1; compQty <= 3; compQty++) // limit compression bars
                                {
                                    var trialSection = section.BaseClone(section);
                                    trialSection.AddTensionRebar(tensLayer.BarSize, tensLayer.Qty, catalog, tensLayer.DepthFromTop);

                                    // compression layer is placed at "cover" depth from top
                                    trialSection.AddCompressionRebar(compSize, compQty, catalog, compression_cover);
                                    
                                    // do the rebar layers fit in the width of the current section
                                    if (RebarLayerFitsInWidth(trialSection) is false)
                                    {
                                        continue;
                                    }

                                    var result = ComputeFlexuralStrength(trialSection);

                                    if (result.PhiMn >= MuTarget_kip_in && result.eps_T > 0.005)
                                    {
                                        successfulSections.Add(result);
                                        success = true;
                                    }

                                    if (success)
                                        break;
                                }
                                if (success)
                                    break;
                            }
                            if (success)
                                break; // exit back to depth selection
                        }
                    }
                }
            }

            // filter for unnecessary duplicates of rebar sizes and depths and layers...
            List<DesignResultModel> filtered = successfulSections;
            //List<DesignResultModel> filtered = FilterIdealDesignsByWidth(FilterIdealDesignsByDepth(successfulSections));

            // then sort in ascending order of width
            return filtered
                .OrderBy(r => r.crossSection.Width)
                .ThenBy(r => r.crossSection.Depth)
                .ToList();
        }

        public static DesignResultModel ComputeFlexuralStrength(CrossSection section)
        {
            double b= section.Width;
            double depth = section.Depth;
            ObservableCollection<RebarLayer> TensionRebars = section.TensionRebars;
            ObservableCollection<RebarLayer> CompressionRebars = section.CompressionRebars;
            double fck = section.Fck_psi;
            double fy = section.Fy_psi;
            double es = section.Es_psi;
            double EpsilonCu = section.EpsilonCu;

            double beta1 = GetBeta1(fck);
            double tolerance = 1e-6;
            string warnings = "";

            // Centroid of compression steel (d') and total Area of compression steel, Asc
            double AsC = CompressionRebars.Sum(r => r.SteelArea);
            double dPrime = (AsC > 0) ? CompressionRebars.Sum(r => r.SteelArea * r.DepthFromTop) / AsC : 0.0;

            // Centroid of tension steel and total area of tension steel, AsT
            double AsT = TensionRebars.Sum(r => r.SteelArea);
            double d = section.dEffective();

            // concrete factor = 0.85 * f'c * b * Beta1
            double concreteFactor = 0.85 * fck / 1000 * b * beta1;

            // Sum forces in x-dir = 0 equation writer
            Fx_eq = BuildSumFxFunction(concreteFactor, AsC, dPrime, AsT, d, b, beta1, fck / 1000.0, fy / 1000.0, es / 1000.0, EpsilonCu);

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
            foreach (var r in roots)
            {
                warnings += $"{r:F6} , ";
            }

            double c = roots.Min();
            warnings += $"\nNeutral Axis location solver = {c:F6}";

            double a = beta1 * c;

            // compute stresses in steel  (negative for tension, positive for compression)
            double eps_comp = EpsilonCu * (c - dPrime) / c;
            double eps_tens = -EpsilonCu * (d - c) / c;
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
            double Msteel_comp_kipin = Csteel * dPrime; // comp_sign needed incase the "compression" steel actually ends up being tensile
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
            if (eps_tens_max >= 0.005) { phi = 0.9; }
            else if (eps_tens_max <= 0.002) { phi = 0.65; }
            else
            {
                phi = 0.65 + (eps_tens_max - 0.002) / (0.003) * (0.90 - 0.65);
            }
            warnings += $"\nphi = {phi:F3}";

            // compute nominal moment
            double phi_Mn = Mn_kipin * phi;


            DesignResultModel design = new DesignResultModel()
            {
                crossSection = section,
                Mn = Math.Abs(Mn_kipin),           // convert in-lb to kip-ft
                Phi = phi,
                NeutralAxis = c,
                Warnings = warnings,
                eps_T = eps_tens_max,
                DepthToEpsT = maxDepth
            };

            // Check the overreinforcement status
            if (design.IsOverreinforced is true)
                warnings += "\nSection is over-reinforced; ";

            return design; ;
        }

        public static double GetBeta1(double fck)
        {
            // ACI 318-19 Table 22.2.2.4.3 approximation
            if (fck <= 4000) return 0.85;
            if (fck >= 8000) return 0.65;
            return 0.85 - 0.05 * ((fck - 4000.0) / 1000.0);
        }


        private static bool RebarLayerFitsInWidth(CrossSection trialSection)
        {
            if (trialSection.TensionRebars.Count > 0)
            {
                foreach(var layer in trialSection.TensionRebars)
                {
                    if (layer.Bar.Diameter * layer.Qty + 2.0 * trialSection.SideCover + (layer.Qty - 1) * trialSection.ClearSpacing > trialSection.Width)
                        return false;
                }
                foreach (var layer in trialSection.CompressionRebars)
                {
                    if (layer.Bar.Diameter * layer.Qty + 2.0 * trialSection.SideCover + (layer.Qty - 1) * trialSection.ClearSpacing > trialSection.Width)
                        return false;
                }
            }
            return true;
        }

        List<DesignResultModel> FilterIdealDesignsByDepth(List<DesignResultModel> successfulSections)
        {
            RebarCatalog catalog = new RebarCatalog();

            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Width,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.crossSection.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{catalog.RebarTable[layer.BarSize].Diameter}")
                    )
                })
                .Select(g => g.OrderBy(r => r.crossSection.Depth).First()) // smallest depth wins
                .ToList();

            return filtered;
        }

        List<DesignResultModel> FilterIdealDesignsByWidth(List<DesignResultModel> successfulSections)
        {
            RebarCatalog catalog = new RebarCatalog();

            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Depth,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.crossSection.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{catalog.RebarTable[layer.BarSize].Diameter}")
                    )
                })
                .Select(g => g.OrderBy(r => r.crossSection.Width).First()) // smallest depth wins
                .ToList();

            return filtered;
        }


        public static Func<double, double> BuildSumFxFunction(double concreteFactor, double AsC, double dPrime, double AsT, double d, double width, double beta1, double fck_ksi, double fy_ksi, double es_ksi, double eps_cu)
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
                    double epsC = eps_cu * (c - dPrime) / c;
                    double fsC = Math.Sign(epsC) * Math.Min(Math.Abs(epsC * es_ksi), fy_ksi);
                    Cs = fsC * AsC; // kips
                }

                // Tensile steel force
                double Ts = 0.0;
                if (AsT > 0)
                {
                    double epsT = eps_cu * (d - c) / c;
                    double fsT = Math.Sign(epsT) * Math.Min(Math.Abs(epsT * es_ksi), fy_ksi);
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

        /// (double concreteFactor, double AsC, double dPrime, double AsT, double d, double width,double beta1, double fck_ksi, double fy_ksi, double es_ksi)

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

        public static List<double> FindAllPositiveRoots(Func<double, double> F, double cMin, double cMax, int samples = 250)
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

        private static double RefineRootBisection(Func<double, double> F, double low, double high, double tol = 1e-6, int maxIter = 100)
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
        public (double AsMax, double MnMax, double PhiMnMax) GetSinglyReinforcedLimit(CrossSection section)
        {
            double dEff = section.Depth - section.TensionCover;
            double b = section.Width;
            double fc = section.Fck_psi;
            double fy = section.Fy_psi;

            double rhoMax = section.RhoMax;
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
