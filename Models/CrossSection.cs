using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls.Primitives;

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

    public class CrossSection
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

        public string SectionSummaryString { get => $"{Width} in. x {Height} in."; }
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

        // default constructor
        public CrossSection() { }

        public CrossSection(double width, double depth,
            double fck_psi = 4000, double fy_psi = 60000, double epsilon_cu = 0.003, double es_psi = 29000000.0,
            double tension_cover = 1.5, double compression_cover=1.5, double side_cover=1.5, double clear_spacing=1.5
            )
        {
            Width = width;
            Height = depth;
            TensionCover = tension_cover;
            CompressionCover = compression_cover;
            SideCover = side_cover;
            ClearSpacing = clear_spacing;
            Fck_psi = fck_psi;
            Fy_psi = fy_psi;
            EpsilonCu = epsilon_cu;
            Es_psi = es_psi;
        }

        public CrossSection BaseClone(CrossSection section)
        {
            return new CrossSection()
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

        public void AddTensionRebar(string barSize, int count, RebarCatalog catalog, double depth)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double d = depth; // centroid depth from top compression fiber
            TensionRebars.Add(new RebarLayer(barSize, count, rebar, d));
        }

        public void AddCompressionRebar(string barSize, int count, RebarCatalog catalog, double depth)
        {
            if (!catalog.RebarTable.ContainsKey(barSize))
                throw new ArgumentException("Unknown bar size: " + barSize);

            var rebar = catalog.RebarTable[barSize];
            double dPrime = depth; // centroid depth from top compression fiber
            CompressionRebars.Add(new RebarLayer(barSize, count, rebar, dPrime));
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
