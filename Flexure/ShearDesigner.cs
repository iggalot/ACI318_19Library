using System;

namespace ACI318_19Library
{
    public static class ShearDesigner
    {
        /// <summary>
        /// Computes nominal and design shear strength of a rectangular reinforced concrete section.
        /// Uses ACI 318-19 Chapter 22 provisions for shear design.
        /// </summary>
        /// <param name="section">CrossSection object containing geometry and reinforcement data</param>
        /// <returns>DesignResultModel populated with shear results (Vc, Vs, Vn_kips, PhiShear)</returns>
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
            // 1. Compute concrete shear Vc (kips)
            // ACI 318-19, Eq. 22.5.2.1
            // --------------------------
            // Convert to kips (1 kip = 1000 lb)
            double sqrt_fck = Math.Sqrt(fck_psi);
            double Vc_lbs = 2 * sqrt_fck * b * d;  // lbs
            double Vc_kips = Vc_lbs / 1000.0;      // kips

            double Vs_max_lbs = 8 * sqrt_fck * b * d; // lbs
            double Vs_max_kips = Vs_max_lbs / 1000.0;  // kips


            // --------------------------
            // 2. Compute steel contribution Vs (kips)
            // --------------------------
            // Shear reinforcement (stirrups)
            
            double Av = RebarCatalog.RebarTable[section.Av_barSize].Area * section.StirrupsLegs;
            double Vs_lbs = Av * fy_psi * d / section.ShearSpacing;
            double Vs_kips = Vs_lbs / 1000.0;


            // --------------------------
            // 3. Nominal shear strength
            // --------------------------
            double Vn_kips = Vc_kips + Vs_kips;

            // --------------------------
            // 4. Strength reduction factor phi
            // --------------------------
            double phiV = 0.75; // ACI tension-controlled default for shear

            // --------------------------
            // 5. Populate DesignResultModel
            // --------------------------
            model.Vc = Vc_kips;
            model.Vs = Vs_kips;
            model.Vn = Vn_kips;
            model.PhiShear = phiV;
            model.ShearWarnings = warnings;
        }
    }
}
