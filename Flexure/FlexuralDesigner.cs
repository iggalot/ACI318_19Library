using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ACI318_19Library
{
    public enum MemberTypes
    {
        MBR_BEAM,
        MBR_ONE_WAY_SLAB,
        MBR_TWO_WAY_SLAB,
        MBR_WALL,
        MBR_RETAINING_WALL,
        MBR_COLUMN
    }

    public class FlexuralDesigner
    {
        const double max_bars_per_layer = 6;

        private static Func<double, double> Fx_eq;  // The sum of forces X = 0 equation for all our terms -- this equation is needed by the solver

        public List<FlexuralDesignResultModel> DesignAllSectionsForMu(
            double MuTarget_kipft,
            double fck_psi = 4000, double fy_psi = 60000, double eps_cu = 0.003, double es_psi = 29000000,
            double tension_cover = 1.5,
            double compression_cover = 1.5,
            double side_cover = 1.5,
            double clear_spacing = 1.5)
        {
            double MuTarget_kip_in = MuTarget_kipft * 12.0;

            ConcreteCrossSection baseSection = new ConcreteCrossSection();
            List<FlexuralDesignResultModel> successfulDesigns = new List<FlexuralDesignResultModel>();

            var widths = Enumerable.Range(4, 32).Select(i => (double)i).ToArray();
            var depths = Enumerable.Range(4, 32).Select(i => (double)i).ToArray();
            var barSizes = new[] { "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11" };
            int max_bars_per_layer = 8;

            Parallel.ForEach(widths, b =>
            {
                List<FlexuralDesignResultModel> localResults = new List<FlexuralDesignResultModel>();

                // Estimate R for ideal rho
                ConcreteCrossSection tempSection = new ConcreteCrossSection(width: b, height: depths.Min());
                //tempSection.Width = b;
                //tempSection.Height = depths.Min();
                double rho_ideal = 0.01;
                rho_ideal = Math.Max(tempSection.RhoMin, Math.Min(rho_ideal, tempSection.RhoMax));

                double R_psi = ComputeRfromRho(fck_psi, fy_psi, rho_ideal);

                double d_min_required = Math.Sqrt(MuTarget_kip_in * 1000 / (R_psi * b));

                foreach (var h in depths.Where(h => h >= d_min_required))
                {
                    ConcreteCrossSection section = tempSection.BaseClone(tempSection);
                    section.Width = b;
                    section.Height = h;

                    double d_eff = section.dEffective();
                    double area_steel_min = section.RhoMin * b * h;
                    double area_steel_max = section.RhoMax * b * h;

                    // Generate feasible tension bars
                    List<RebarLayer> tensionOptions = new List<RebarLayer>();
                    foreach (var size in barSizes)
                    {
                        double As_bar = RebarCatalog.RebarTable[size].Area;
                        int minQty = (int)Math.Ceiling(area_steel_min / As_bar);
                        int maxQty = Math.Min(max_bars_per_layer, (int)Math.Floor(area_steel_max / As_bar));

                        for (int qty = minQty; qty <= maxQty; qty++)
                        {
                            if (RebarSpacingHorizontalIsValid(section, size, qty))
                            {
                                tensionOptions.Add(new RebarLayer(size, qty, RebarCatalog.RebarTable[size], h - tension_cover));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    var rebarLayers = tensionOptions.OrderBy(x => x.SteelArea);

                    bool tensionOnlySatisfied = false;

                    foreach (var layer in rebarLayers)
                    {
                        section.TensionRebars.Clear();
                        section.AddTensionRebar(layer.BarSize, layer.Qty, layer.DepthFromTop);

                        var designResult = ComputeFlexuralMomentCapacity(section);

                        if (designResult == null) 
                        {
                            return;
                        }

                        if (designResult.eps_T < 0.005)
                            break;

                        if (designResult.PhiMn_lbin >= MuTarget_kip_in)
                        {
                            designResult.DepthToEpsT = layer.DepthFromTop;
                            localResults.Add(designResult);
                            tensionOnlySatisfied = true;
                            break; // stop at first tension-only solution
                        }
                    }

                    // If tension-only not enough, try doubly reinforced
                    if (!tensionOnlySatisfied)
                    {
                        foreach (var layer in rebarLayers)
                        {
                            foreach (var compSize in barSizes)
                            {
                                for (int compQty = 1; compQty <= 3; compQty++) // limit compression bars
                                {
                                    section.TensionRebars.Clear();
                                    section.CompressionRebars.Clear();

                                    section.AddTensionRebar(layer.BarSize, layer.Qty, layer.DepthFromTop);
                                    section.AddCompressionRebar(compSize, compQty, compression_cover);

                                    var designResult = ComputeFlexuralMomentCapacity(section);

                                    if ((designResult == null))
                                    {
                                        return;
                                    }

                                    if (designResult.eps_T < 0.005)
                                        break;

                                    if (designResult.PhiMn_lbin >= MuTarget_kip_in)
                                    {
                                        localResults.Add(designResult);
                                        break;
                                    }
                                }
                                if (localResults.Count > 0 && localResults.Last().PhiMn_lbin >= MuTarget_kip_in)
                                    break;
                            }
                            if (localResults.Count > 0 && localResults.Last().PhiMn_lbin >= MuTarget_kip_in)
                                break;
                        }
                    }
                }

                lock (successfulDesigns)
                {
                    successfulDesigns.AddRange(localResults);
                }
            });

            return successfulDesigns
                .OrderBy(r => r.crossSection.Width)
                .ThenBy(r => r.crossSection.Height)
                .ToList();
        }

        private static bool RebarSpacingHorizontalIsValid(ConcreteCrossSection section, string size, int qty)
        {
            return (qty - 1) * section.ClearSpacing + 2.0 * section.SideCover + qty * RebarCatalog.RebarTable[size].Diameter < section.Width;
        }

        private static double ComputeRfromRho(double fck_psi, double fy_psi, double rho)
        {
            return rho * fy_psi * (1 - 0.59 * rho * fy_psi / fck_psi);
        }

        public static FlexuralDesignResultModel ComputeFlexuralMomentCapacity(ConcreteCrossSection section)
        {
            // if we don't have any tension rebars defined, exit the calculations.
            if (section == null || section.TensionRebars == null || section.TensionRebars.Count <= 0)  return null;

            double b= section.Width;
            double depth = section.Height;
            ObservableCollection<RebarLayer> TensionRebars = section.TensionRebars;
            ObservableCollection<RebarLayer> CompressionRebars = section.CompressionRebars;
            double fck_psi = section.Fck_psi;
            double fy_psi = section.Fy_psi;
            double es_psi = section.Es_psi;
            double EpsilonCu = section.EpsilonCu;

            double beta1 = GetBeta1(fck_psi);
            double tolerance = 1e-6;
            string warnings = "";

            // Centroid of compression steel (d_in') and total Area of compression steel, Asc
            double AsC = CompressionRebars.Sum(r => r.SteelArea);
            double dPrime_in = (AsC > 0) ? CompressionRebars.Sum(r => r.SteelArea * r.DepthFromTop) / AsC : 0.0;

            // Centroid of tension steel and total area of tension steel, AsT
            double AsT = TensionRebars.Sum(r => r.SteelArea);
            double d_in = section.dEffective();

            // concrete factor = 0.85 * f'c * b * Beta1
            double concreteFactor = 0.85 * fck_psi / 1000 * b * beta1;

            // Sum forces in x-dir = 0 equation writer
            Fx_eq = BuildSumFxFunction(concreteFactor, AsC, dPrime_in, AsT, d_in, b, beta1, fck_psi / 1000.0, fy_psi / 1000.0, es_psi / 1000.0, EpsilonCu);

            // Solve using dynamically constructed F -- use Bisection method
            //double c = SolveForX(Fx_eq, xMin: 0.001, xMax: 5.0 * depth);

            // Use quadratic solveer
            //double c = SolveForNeutralAxisQuadratic(AsC, dPrime_in, AsT, d_in, b, beta1, fck_psi / 1000.0, fy_psi / 1000.0, es_psi / 1000.0, EpsilonCu);

            // Use a new method:
            Func<double, double> F = c1 =>
            {
                // Concrete compressive force
                double a1 = beta1 * c1;
                double Cconc1 = 0.85 * fck_psi * b * a1;

                // Compression steel contribution
                double Csteel1 = 0.0;
                foreach (var layer in CompressionRebars)
                {
                    double eps = EpsilonCu * (c1 - layer.DepthFromTop) / c1;
                    double fs1 = Math.Sign(eps) * Math.Min(Math.Abs(eps * es_psi), fy_psi);
                    Csteel1 += layer.SteelArea * fs1;
                }

                // Tension steel contribution
                double Tsteel1 = 0.0;
                foreach (var layer in TensionRebars)
                {
                    double eps = EpsilonCu * (layer.DepthFromTop - c1) / c1;
                    double fs2 = Math.Sign(eps) * Math.Min(Math.Abs(eps * es_psi), fy_psi);
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

            try
            {
                double c = roots.Min();
                warnings += $"\nNeutral Axis location solver = {c:F6}";

                double a = beta1 * c;

                // compute stresses in steel  (negative for tension, positive for compression)
                double eps_comp = EpsilonCu * (c - dPrime_in) / c;
                double eps_tens = -EpsilonCu * (d_in - c) / c;
                warnings += $"\neps_s_prime = {eps_comp:F6} and eps_tens = {eps_tens:F6}";

                // compute stresses in steel
                double fs_ksi = Math.Sign(eps_tens) * Math.Min(Math.Abs(eps_tens * es_psi), fy_psi) / 1000.0;
                double fs_prime_ksi = Math.Sign(eps_comp) * Math.Min(Math.Abs(eps_comp * es_psi), fy_psi) / 1000.0;
                warnings += $"\nfs_prime = {fs_prime_ksi:F2} ksi  and fs_ksi = {fs_ksi:F2} ksi";

                // compute forces
                double Cconc_kips = 0.85 * fck_psi / 1000.0 * b * a;
                double Csteel_kips = fs_prime_ksi * AsC;
                double Tsteel_kips = fs_ksi * AsT;
                warnings += $"\nCconc = {Cconc_kips:F2} kips  and Csteel_kips = {Csteel_kips:F2} kips  and Tsteel_kips = {Tsteel_kips:F2} kips";

                // moments about top fiber -- CW positive -- compressive forcs are negative, tensile forces are positive
                double Mconc_kipin = Cconc_kips * a / 2.0;
                double Msteel_comp_kipin = Csteel_kips * dPrime_in; // comp_sign needed incase the "compression" steel actually ends up being tensile
                double Msteel_tens__kipin = Tsteel_kips * d_in;
                double Mn_kipin = Mconc_kipin + Msteel_comp_kipin + Msteel_tens__kipin;
                warnings += $"\nMtot = {Mn_kipin:F2} kip-in  and Mconc_kipin = {Mconc_kipin:F2} kip-in  and Msteel_comp_kipin = {Msteel_comp_kipin:F2} kip-in  and Msteel = {Msteel_tens__kipin:F2} kip-in";

                // alternative method -- compute about the Tensile steel -- CW negative...so need to flip the signs
                double Mconc2_kipin = -Cconc_kips * (d_in - a / 2);
                double Msteel_comp2_kipin = -Csteel_kips * (d_in - dPrime_in); // comp_sign needed incase the "compression" steel actually ends up being tensile
                double Mn2_kipin = Mconc2_kipin + Msteel_comp2_kipin;
                warnings += $"\nMtot = {Mn2_kipin:F2} kip-in  and Mconc_kipin = {Mconc2_kipin:F2} kip-in  and Msteel_comp_2 = {Msteel_comp2_kipin:F2} kip-in";

                // verify they mathch
                double diff = Mn_kipin - Mn2_kipin;
                warnings += $"\n-- Checking methods for Mn_kipin diff = {diff:F2} kip-in";


                // compute the tensilestrain in the extreme most tensile renforcement so that we can determine the true value of phi
                // -- search for the TensionRebar with the largest "DepthFromTop" value
                double maxDepth_in = TensionRebars.Max(r => r.DepthFromTop);
                double eps_y = fy_psi / es_psi;
                double eps_tens_max = -EpsilonCu + maxDepth_in / c * EpsilonCu;
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

                FlexuralDesignResultModel design = new FlexuralDesignResultModel()
                {
                    crossSection = section,
                    Mn_lbin = Math.Abs(Mn_kipin),           // convert in-lb to kip-ft
                    PhiFlexure = phi,
                    NeutralAxis = c,
                    FlexuralWarnings = warnings,
                    eps_T = eps_tens_max,
                    DepthToEpsT = maxDepth_in
                };

                // Check the overreinforcement status
                if (design.IsOverreinforced is true)
                    warnings += "\nSection is over-reinforced; ";

                return design;
            }  catch
            {
                MessageBox.Show("No roots found in solving for neutral axis position.");
                return null;
            }
        }


        public static double GetBeta1(double fck)
        {
            // ACI 318-19 Table 22.2.2.4.3 approximation
            if (fck <= 4000) return 0.85;
            if (fck >= 8000) return 0.65;
            return 0.85 - 0.05 * ((fck - 4000.0) / 1000.0);
        }


        private static bool RebarLayerFitsInWidth(ConcreteCrossSection trialSection)
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

        List<FlexuralDesignResultModel> FilterIdealDesignsByDepth(List<FlexuralDesignResultModel> successfulSections)
        {
            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Width,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.crossSection.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{RebarCatalog.RebarTable[layer.BarSize].Diameter}")
                    )
                })
                .Select(g => g.OrderBy(r => r.crossSection.Height).First()) // smallest depth wins
                .ToList();

            return filtered;
        }

        List<FlexuralDesignResultModel> FilterIdealDesignsByWidth(List<FlexuralDesignResultModel> successfulSections)
        {
            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Height,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.crossSection.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{RebarCatalog.RebarTable[layer.BarSize].Diameter}")
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

        /// (double concreteFactor, double AsC, double dPrime_in, double AsT, double d_in, double width,double beta1, double fck_ksi, double fy_ksi, double es_ksi)

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
        public (double AsMax, double MnMax, double PhiMnMax) GetSinglyReinforcedLimit(ConcreteCrossSection section)
        {
            double dEff = section.Height - section.TensionCover;
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
