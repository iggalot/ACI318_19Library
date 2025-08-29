namespace ACI318_19Library
{
    /// <summary>
    /// Class for storing the flexural results from Whitney Stress Block
    /// </summary>
    /// <summary>
    /// Stores the results of a flexural strength calculation (ACI 318-19).
    /// All values are for a single section analysis and are returned in in-lb unless noted.
    /// </summary>
    public class FlexuralResult
    {
        /// <summary>Total reinforcement area (As,t + As,c), in^2</summary>
        public double As { get; set; }

        /// <summary>Tension reinforcement area (As,t), in^2</summary>
        public double AsT { get; set; }

        /// <summary>Compression reinforcement area (As,c), in^2</summary>
        public double AsC { get; set; }

        /// <summary>Effective depth to tension reinforcement centroid (d), in</summary>
        public double d { get; set; }

        /// <summary>Distance from extreme compression fiber to compression steel centroid (d'), in</summary>
        public double dPrime { get; set; }

        /// <summary>Equivalent stress block depth (a), in</summary>
        public double a { get; set; }

        /// <summary>Neutral axis depth from compression face (c = a / β1), in</summary>
        public double c { get; set; }

        /// <summary>Nominal flexural strength (Mn), in-lb</summary>
        public double Mn { get; set; }

        /// <summary>Design flexural strength (φMn), in-lb</summary>
        public double PhiMn { get; set; }

        /// <summary>Strength reduction factor (φ) used to compute φMn</summary>
        public double Phi { get; set; }

        /// <summary>Computed extreme tension steel strain ε_t (unitless)</summary>
        public double EpsilonT { get; set; }

        /// <summary>Computed steel yield strain ε_y (Fy / Es) (unitless)</summary>
        public double EpsilonY { get; set; }

        /// <summary>Ductility classification (TensionControlled, Transition, CompressionControlled)</summary>
        public DuctilityClass Ductility { get; set; }

        /// <summary>Reinforcement ratio ρ = AsT / (b * d)</summary>
        public double Rho { get; set; }

        /// <summary>Balanced reinforcement ratio ρ_b per ACI</summary>
        public double RhoBalanced { get; set; }

        /// <summary>True if section is over-reinforced (ρ &gt; ρ_b)</summary>
        public bool IsOverReinforced { get; set; }

        /// <summary>True if compression steel yields (fs' reaches fy)</summary>
        public bool CompressionSteelYields { get; set; }

        /// <summary>Human-readable warnings (empty if none)</summary>
        public string Warnings { get; set; } = "";
    }
}
