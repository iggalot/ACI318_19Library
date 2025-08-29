namespace ACI318_19Library
{
    public class RebarLayerModel
    {
        public string BarSize { get; private set; }
        public int Count { get; private set; }
        public Rebar Bar { get; private set; }
        public double Depth { get; private set; }  // centroid depth from extreme compression fiber

        public RebarLayerModel(string barSize, int count, Rebar bar, double depth)
        {
            BarSize = barSize;
            Count = count;
            Bar = bar;
            Depth = depth;
        }

        public double SteelArea => Count * Bar.Area;
    }
}
