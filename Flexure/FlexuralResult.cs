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
        /// <summary>Factored moment capacity φMn (in-lb)</summary>
        public double Mu { get; set; }

        /// <summary>Nominal moment capacity Mn (in-lb)</summary>
        public double Mn { get; set; }

        /// <summary>Strength reduction factor φ</summary>
        public double Phi { get; set; }

        /// <summary>Strength reduction factor φ</summary>
        public double PhiMn { get; set; }

        /// <summary>Balanced steel ratio ρb</summary>
        public double RhoBalanced { get; set; }

        /// <summary>Actual steel ratio ρ = AsT / (b*d)</summary>
        public double RhoActual { get; set; }

        /// <summary>Neutral axis depth c from top fiber</summary>
        public double NeutralAxis { get; set; }

        /// <summary>Total tension steel area AsT (in^2)</summary>
        public double AsT { get; set; }

        /// <summary>Total compression steel area AsC (in^2)</summary>
        public double AsC { get; set; }

        /// <summary>Effective tension steel centroid d (in) from top fiber</summary>
        public double d { get; set; }

        /// <summary> Effective compression steel centroid c (in) from top fiber</summary>
        public double c { get; set; }

        /// <summary>Compression steel centroid d' (in) from top fiber</summary>
        public double dPrime { get; set; }

        /// <summary>Steel yield strain EpsY = Fy/Es</summary>
        public double EpsY { get; set; }

        /// <summary>Steel yield strain EpsY = Fy/Es</summary>
        public double EpsT { get; set; }

        /// <summary>Concrete stress block factor β1</summary>
        public double Beta1 { get; set; }

        /// <summary>Number of tension steel layers</summary>
        public int TensionLayerCount { get; set; }

        /// <summary>Number of compression steel layers</summary>
        public int CompressionLayerCount { get; set; }

        /// <summary>Moment contribution of concrete compression (in-lb)</summary>
        public double ConcreteMoment { get; set; }

        /// <summary>Moment contribution of tension steel (in-lb)</summary>
        public double TensionMoment { get; set; }

        /// <summary>Moment contribution of compression steel (in-lb)</summary>
        public double CompressionMoment { get; set; }

        /// <summary>Warnings such as over-reinforced or non-yielding compression steel</summary>
        public string Warnings { get; set; }

        /// <summary>Bisection iterations used to solve neutral axis depth</summary>
        public int Iterations { get; set; }
    }

}
