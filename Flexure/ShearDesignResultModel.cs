namespace ACI318_19Library
{
    // Add this container class (returns design output)
    public class ShearDesignResultModel
    {
        /// <summary>Concrete cross section</summary>
        public CrossSection crossSection { get; set; }

        /// <summary>FlexuralWarnings (e.g. over-reinforced, compression steel not yielded)</summary>
        public string ShearWarnings { get; set; }

        // ======================
        // SHEAR SUMMARY
        // ======================

        /// <summary>Concrete shear contribution Vc (kips)</summary>
        public double Vc { get; set; }

        /// <summary>Steel shear contribution Vs (kips)</summary>
        public double Vs { get; set; }

        /// <summary>Nominal shear capacity Vn (kips)</summary>
        public double Vn { get; set; }

        /// <summary>Strength reduction factor for shear (φ)</summary>
        public double PhiShear { get; set; } = 0.75;

        /// <summary>Design shear strength φVn (kips)</summary>
        public double PhiVn { get => PhiShear * Vn; }

        public double Av_over_s { get; set; }

        public string DisplayShearInfo()
        {
            string str = string.Empty;

            // Shear display
            if (Vn > 0)
            {
                str += $" | φVn = {PhiVn:F1} kips (Vc={Vc:F1}, Vs={Vs:F1})";
            }

            if (!string.IsNullOrWhiteSpace(ShearWarnings))
                str += $" | FlexuralWarnings: {ShearWarnings}";

            return str;
        }
    }
}
