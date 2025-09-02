using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ACI318_19Library
{
    // Add this container class (returns design output)
    public class ShearDesignResultModel
    {
        /// <summary>Concrete cross section</summary>
        public CrossSection crossSection { get; set; }

        /// <summary>Warnings (e.g. over-reinforced, compression steel not yielded)</summary>
        public string Warnings { get; set; }

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

    }
}
