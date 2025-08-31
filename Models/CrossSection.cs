using System;
using System.Collections.Generic;
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

    public class CrossSection
    {

        // Geometry
        public double Width { get; set; }   // b, in
        public double Depth { get; set; }   // h, in
        public double TensionCover { get; set; }   // in
        public double CompressionCover { get; set; }   // in
        public double SideCover { get; set; } // in
        public double ClearSpacing { get; set; } // in.

        // Materials
        public double Fck_psi { get; set; }     // psi (f'c)
        public double Fy_psi { get; set; }      // psi

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

        // Reinforcement layers
        public ObservableCollection<RebarLayer> TensionRebars { get; set; } = new ObservableCollection<RebarLayer>();
        public ObservableCollection<RebarLayer> CompressionRebars { get; set; } = new ObservableCollection<RebarLayer>();

        // constants
        public const double EpsilonCu = 0.003;       // ultimate concrete strain
        public const double Es_psi = 29000000.0;         // psi (modulus of steel)

        public CrossSection BaseClone(CrossSection section)
        {
            return new CrossSection()
            {
                Width = section.Width,
                Depth = section.Depth,
                TensionCover = section.TensionCover,
                CompressionCover = section.CompressionCover,
                SideCover = section.SideCover,
                ClearSpacing = section.ClearSpacing,
                Fck_psi = section.Fck_psi,
                Fy_psi = section.Fy_psi,
            };
        }
        // default constructor
        public CrossSection() { }

        public CrossSection(double width, double depth, double tension_cover, double compression_cover, double side_cover, double clear_spacing, double fck, double fy)
        {
            Width = width;
            Depth = depth;
            TensionCover = tension_cover;
            CompressionCover = compression_cover;
            SideCover = side_cover;
            ClearSpacing = clear_spacing;
            Fck_psi = fck;
            Fy_psi = fy;
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
            if (TensionRebars.Count == 0) return Depth - TensionCover - 0.5;
            return TensionRebars.Sum(l => l.SteelArea * l.DepthFromTop) / TensionRebars.Sum(l => l.SteelArea);
        }



        // Max steel ratio (ACI often limits to 0.75ρb)
        public double RhoMax => 0.75 * RhoBalanced;

        // Current required ratio
        public double RhoRequired(double As_required)
        {
            return As_required / (Width * Depth);
        }





        public string DisplayInfo()
        {
            return
                $"Width: {Width} in\n" +
                $"DepthFromTop: {Depth} in\n" +
                $"TensionCover: {TensionCover} in\n" +
                $"CompressionCover: {CompressionCover} in\n" +
                $"SideCover: {SideCover} in\n" +

                $"fck: {Fck_psi} psi\n" +
                $"fy_ksi: {Fy_psi} psi\n";
        }


    }
}
