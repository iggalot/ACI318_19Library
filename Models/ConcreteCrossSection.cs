using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ACI318_19Library
{
    /// <summary>
    /// Ductility classification per ACI 318-19 for flexure φ selection.
    /// </summary>
    public enum DuctilityClass
    {
        TensionControlled,
        Transition,
        CompressionControlled
    }

    public class ConcreteCrossSection
    {

        // Geometry
        public double Width { get; set; }   // b, in -- total width of the cross section
        public double Height { get; set; }   // h, in -- total height of the cross section
        public double TensionCover { get; set; }   // in
        public double CompressionCover { get; set; }   // in
        public double SideCover { get; set; } // in
        public double ClearSpacing { get; set; } // in.

        // Reinforcement layers
        public ObservableCollection<RebarLayer> TensionRebars { get; set; } = new ObservableCollection<RebarLayer>();
        public ObservableCollection<RebarLayer> CompressionRebars { get; set; } = new ObservableCollection<RebarLayer>();

        // Shear reinforcement properties
        public string Av_barSize { get; set; } = "#4"; // in^2 per stirrup set
        public double ShearSpacing { get; set; } = double.PositiveInfinity; // in
        public int StirrupsLegs { get; set; } = 0; // typically 2 legs

        public string SectionSummaryString { get => $"{Width} in. x {Height} in. = {AreaGross} sq. in."; }
        // constants
        public double EpsilonCu { get; set; } = 0.003;       // ultimate concrete strain
        public double Es_psi { get; set; } = 29000000;         // psi (modulus of steel)
        public double Fck_psi { get; set; } = 4000;     // psi (f'c)
        public double Fy_psi { get; set; } = 60000;      // psi

        // Balanced reinforcement ratio ρb
        public double RhoBalanced
        {
            get
            {
                double eps_y = Fy_psi / Es_psi; // steel yield strain
                double eps_cu = 0.003;    // concrete crushing strain
                return (0.85 * Fck_psi * FlexuralDesigner.GetBeta1(Fck_psi)) /
                       (Fy_psi * (1.0 + eps_y / eps_cu));
            }
        }

        /// <summary>
        /// Establishes a rho_min for the cross section
        /// </summary>
        public double RhoMin_Materials
        {
            get
            {
                return 3.0 * Math.Sqrt(Fck_psi) / Fy_psi;
            }
        }

        /// <summary>
        /// Establishes a rho_min for the cross section dimensions
        /// </summary>
        public double RhoMin_Area
        {
            get
            {
                return 200.0 / Fy_psi;
            }
        }

        public double RhoMin
        {
            get
            {
                return Math.Max(RhoMin_Materials, RhoMin_Area);
            }
        }


        /// <summary>
        /// Computes the rho at a specified strain.
        /// -- 0.005 ensures ductile behavior and phi=0.9
        /// </summary>
        /// <param name="strain"></param>
        /// <returns></returns>
        public double RhoAtStrain(double strain)
        {
                double eps_cu = EpsilonCu;    // concrete crushing strain
                return (0.85 * Fck_psi * FlexuralDesigner.GetBeta1(Fck_psi)) /
                       (Fy_psi * (1.0 + strain / eps_cu));
        }

        // Max steel ratio (ACI often limits to 0.75ρb)
        public double RhoMax => 0.75 * RhoBalanced;

        /// <summary>
        /// tensile yield strain
        /// </summary>
        public double Eps_Y { get => Fy_psi / Es_psi; }

        public double AreaGross { get => Width * Height; }

        // Moment of inertia
        public double Ix { get => Width * Height * Height * Height / 12.0; }

        // Moment of inertia
        public double Iy { get => Width * Width * Height * Height / 12.0; }

        // default constructor
        public ConcreteCrossSection(double fck_psi = 4000, double fy_psi = 60000, double epsilon_cu = 0.003, double es_psi = 29000000.0,
            double tension_cover = 1.5, double compression_cover = 1.5, double side_cover = 1.5, double clear_spacing = 1.5)
        {
            TensionCover = tension_cover;
            CompressionCover = compression_cover;
            SideCover = side_cover;
            ClearSpacing = clear_spacing;
            Fck_psi = fck_psi;
            Fy_psi = fy_psi;
            EpsilonCu = epsilon_cu;
            Es_psi = es_psi;
        }

        public ConcreteCrossSection(double width, double height,
            double fck_psi = 4000, double fy_psi = 60000, double epsilon_cu = 0.003, double es_psi = 29000000.0,
            double tension_cover = 1.5, double compression_cover=1.5, double side_cover=1.5, double clear_spacing=1.5
            )
        {
            Width = width;
            Height = height;
            TensionCover = tension_cover;
            CompressionCover = compression_cover;
            SideCover = side_cover;
            ClearSpacing = clear_spacing;
            Fck_psi = fck_psi;
            Fy_psi = fy_psi;
            EpsilonCu = epsilon_cu;
            Es_psi = es_psi;
        }

        public ConcreteCrossSection BaseClone(ConcreteCrossSection section)
        {
            return new ConcreteCrossSection()
            {
                Width = section.Width,
                Height = section.Height,
                TensionCover = section.TensionCover,
                CompressionCover = section.CompressionCover,
                SideCover = section.SideCover,
                ClearSpacing = section.ClearSpacing,
                Fck_psi = section.Fck_psi,
                Fy_psi = section.Fy_psi,
                EpsilonCu = section.EpsilonCu,
                Es_psi = section.Es_psi,
                TensionRebars = new ObservableCollection<RebarLayer>(section.TensionRebars),
                CompressionRebars = new ObservableCollection<RebarLayer>(section.CompressionRebars)
            };
        }

        public void AddTensionRebar(string barSize, int count, double depth)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = RebarCatalog.RebarTable[barSize];
            double d = depth; // centroid height from top compression fiber
            TensionRebars.Add(new RebarLayer(barSize, count, rebar, d));
        }

        public void AddCompressionRebar(string barSize, int count, double depth)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = RebarCatalog.RebarTable[barSize];
            double dPrime = depth; // centroid height from top compression fiber
            CompressionRebars.Add(new RebarLayer(barSize, count, rebar, dPrime));
        }

        public void AddShearReinforcement(string barSize, int legs, double spacing)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = RebarCatalog.RebarTable[barSize];
            Av_barSize = barSize; // total barSize 
            StirrupsLegs = legs;
            ShearSpacing = spacing;
        }

        // Helper: effective tension steel centroid
        public double dEffective()
        {
            if (TensionRebars.Count == 0) return Height - TensionCover - 0.5;
            return TensionRebars.Sum(l => l.SteelArea * l.DepthFromTop) / TensionRebars.Sum(l => l.SteelArea);
        }

        // Helper: effective compression steel centroid
        public double dPrimeEffective()
        {
            if (TensionRebars.Count == 0) return Height - CompressionCover ;
            return CompressionRebars.Sum(l => l.SteelArea * l.DepthFromTop) / CompressionRebars.Sum(l => l.SteelArea);
        }


        public string DisplayInfo()
        {
            return
                $"Width: {Width} in\n" +
                $"DepthFromTop: {Height} in\n" +
                $"TensionCover: {TensionCover} in\n" +
                $"CompressionCover: {CompressionCover} in\n" +
                $"SideCover: {SideCover} in\n" +

                $"fck: {Fck_psi} psi\n" +
                $"fy_ksi: {Fy_psi} psi\n";
        }
    }
}
