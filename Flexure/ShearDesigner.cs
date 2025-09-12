using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Navigation;

namespace ACI318_19Library
{
    public static class ShearDesigner
    {
        /// <summary>
        /// Computes nominal and design shear strength of a rectangular reinforced concrete section.
        /// Uses ACI 318-19 Chapter 22 provisions for shear design.
        /// </summary>
        /// <param name="section">CrossSection object containing geometry and reinforcement data</param>
        /// <returns>DesignResultModel populated with shear results (Vc_kip, Vs_kip, Vn_kips, PhiShear)</returns>
        public static void ComputeShearCapacity(ConcreteCrossSection section, ref FlexuralDesignResultModel model)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            double b = section.Width;           // width of web (in)
            double d = section.dEffective();   // effective depth to tension steel (in)
            double fck_psi = section.Fck_psi;      // concrete compressive strength (psi)
            double fy_psi = section.Fy_psi;        // steel yield strength (psi)
            double es_psi = section.Es_psi;        // modulus of elasticity of steel
            string warnings = "";

            // --------------------------
            // 1. Compute concrete shear Vc_kip (kips)
            // ACI 318-19, Eq. 22.5.2.1
            // --------------------------
            // Convert to kips (1 kip = 1000 lb)
            double Vc_kips = ComputeVc_kips(section);      // kips
            double vs_max_kips = ComputeVsMax_kips(section);  // kips

            // --------------------------
            // 2. Compute steel and concrete contributions to nominal shear strength for each rebar layer
            // --------------------------
            // Shear reinforcement (stirrups)

            double phiV = 0.75;
            double vc_kips = ComputeVc_kips(section);




            // --------------------------
            // 5. Populate DesignResultModel
            // --------------------------
            foreach (var layer in section.StirrupRebars)
            {
                double vs_kips = ComputeVs_kips(section, layer);

                model.ShearResults.Add(new ShearLayerResult
                {
                    Layer = layer,
                    Vs_kip = vs_kips,
                    Vn_kip = vs_kips + vc_kips,
                    PhiVn_kip = phiV * (vs_kips + vc_kips)
                });
            }

            model.Vs_max_kip = vs_max_kips;
            model.Vc_kip = vc_kips;
            model.PhiShear = phiV;
            model.ShearWarnings = warnings;
        }

        public static double ComputeVc_kips(ConcreteCrossSection section)
        {
            return (2.0 * Math.Sqrt(section.Fck_psi) * section.Width * section.dEffective() / 1000.0);
        }

        public static double ComputeVsMax_kips(ConcreteCrossSection section)
        {
            return (8.0 * Math.Sqrt(section.Fck_psi) * section.Width * section.dEffective() / 1000.0);
        }

        /// <summary>
        /// A function to compute the shear capacity, Vs of a shear stirrup layer
        /// </summary>
        /// <param name="section"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static double ComputeVs_kips(ConcreteCrossSection section, RebarStirrupLayer layer)
        {
            if(section != null && layer != null)
            {
                double Av = RebarCatalog.RebarTable[layer.BarSize].Area * layer.NumShearLegs;
                double vs = Av * section.Fy_psi * section.dEffective() / layer.Spacing / 1000.0;
                return Math.Min(vs, ComputeVsMax_kips(section));
            }

            return 0.0;
        }

        /// <summary>
        /// ACI318-19 Section 9.7.6.2.2
        /// -- Atl east one leg of a transverse stirrup must cross the shear plane at spacing not exceeding the smaller of
        /// d/2 (where d is the effective depth
        /// or 24 in., whichever is smaller.
        /// </summary>
        /// <param name="section">The section being considered</param>
        /// <param name="layer">The shear stirrup layer being considered</param>
        /// <param name="Vs">The shear required to be carried by the stirrups</param>
        /// <returns></returns>
        public static double GetMaxStirrupSpacing(ConcreteCrossSection section, RebarStirrupLayer layer, double Vs_req)
        {
            double max1, max2;
            double max3 = section.Fy_psi * layer.SteelArea / (0.75 * Math.Sqrt(section.Fck_psi) * section.Width);
            double max4 = section.Fy_psi * layer.SteelArea / (50 * section.Width);
            // Lightly loaded in shear
            if (Vs_req <= 4.0 * Math.Sqrt(section.Fck_psi) * section.Width * section.dEffective())
            {
                max1 = 24.0;
                max2 = 0.5 * section.dEffective();
            } 

            // Heavily loaded in shear
            else
            {
                max1 = 12;
                max2 = 0.25 * section.dEffective();
            }

            return Math.Min(max1, Math.Min(max2, Math.Min(max3, max4)));
        }

        /// <summary>
        /// Minimum spacing of shear reinforcement.
        /// -- a practical limit is 3" as a minimum, though even this may not be optimal
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static double GetMinStirrupSpacing(ConcreteCrossSection section)
        {
            return 3.0;
        }

        public static double GetAvMin_Over_S(ConcreteCrossSection section)
        {
            double min1 = 50 * section.Width / section.Fy_psi;
            double min2 = 0.75 * Math.Sqrt(section.Fck_psi) * section.Width / section.Fy_psi;

            return Math.Max(min1, min2);
        }
    }
}
