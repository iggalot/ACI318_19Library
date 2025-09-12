namespace ACI318_19Library
{
    /// <summary>
    /// Used for longitudinal or primary flexural reinforcement
    /// </summary>
    public class RebarStirrupLayer
    {
        public string BarSize { get; private set; }
        public int NumShearLegs { get; private set; }
        public Rebar Bar { get; private set; }
        public double StartPos { get; set; }
        public double EndPos { get; set; }
        public double Spacing { get; private set; }  // centroid depth from extreme compression fiber

        public double Av_over_S
        {
            get => RebarCatalog.RebarTable[BarSize].Area * NumShearLegs / Spacing;
        }

        // backing field for manual steel area
        private double? _manualSteelArea;

        // Constructor for catalog-based layer
        public RebarStirrupLayer(string barSize, int count, Rebar bar, double spacing, double start=0, double end=1000)
        {
            BarSize = barSize;
            NumShearLegs = count;
            Bar = bar;
            StartPos = start;
            EndPos = end;
            Spacing = spacing;
        }

        // Compute SteelArea: either manual or catalog-based
        public double SteelArea
        {
            get
            {
                if (_manualSteelArea.HasValue)
                    return _manualSteelArea.Value;
                if (Bar != null && NumShearLegs > 0)
                    return NumShearLegs * Bar.Area;
                return 0.0;
            }
        }
    }

}
