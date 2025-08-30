namespace ACI318_19Library
{
    public class RebarLayer
    {
        public string BarSize { get; private set; }
        public int Qty { get; private set; }
        public Rebar Bar { get; private set; }
        public double DepthFromTop { get; private set; }  // centroid depth from extreme compression fiber

        // backing field for manual steel area
        private double? _manualSteelArea;

        // Constructor for catalog-based layer
        public RebarLayer(string barSize, int count, Rebar bar, double depth)
        {
            BarSize = barSize;
            Qty = count;
            Bar = bar;
            DepthFromTop = depth;
        }

        // Constructor for manual trial layer
        public RebarLayer(double steelArea, double depth)
        {
            _manualSteelArea = steelArea;
            DepthFromTop = depth;
        }

        // Compute SteelArea: either manual or catalog-based
        public double SteelArea
        {
            get
            {
                if (_manualSteelArea.HasValue)
                    return _manualSteelArea.Value;
                if (Bar != null && Qty > 0)
                    return Qty * Bar.Area;
                return 0.0;
            }
        }
    }

}
