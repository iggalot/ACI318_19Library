using System.Collections.Generic;
using System.Linq;

namespace ACI318_19Library
{
    public class FlexuralDesigner
    {
        /// <summary>
        /// Iterates through all candidate section dimensions and steel layers,
        /// returning a list of all combinations that meet or exceed the target moment.
        /// </summary>
        public List<DesignResult> DesignAllSections(
            double MuTarget_kipft,                  // target factored moment

            double cover,

            double fck = 4000, double fy = 60000, double es = 29000000)
        {
            List<DesignResult> successfulSections = new List<DesignResult>();
            double MuTarget_kip_in = MuTarget_kipft * 12.0; // convert to kip-in

            RebarCatalog catalog = new RebarCatalog();

            List<double> widths = new List<double>();                     // candidate widths (in)
            List<double> depths = new List<double>();                     // candidate depths (in)
            // candidate compression bars for the top of the beam section
            List<RebarLayer> compressionOptions = new List<RebarLayer>()
            {
                new RebarLayer("#4", 1, catalog.RebarTable["#4"], cover),
                new RebarLayer("#5", 1, catalog.RebarTable["#5"], cover),
                new RebarLayer("#6", 1, catalog.RebarTable["#6"], cover),
                new RebarLayer("#7", 1, catalog.RebarTable["#7"], cover),
                new RebarLayer("#8", 1, catalog.RebarTable["#8"], cover),
                new RebarLayer("#9", 1, catalog.RebarTable["#9"], cover),
            };

            // load the acceptable widths
            for (int i = 0; i < 32; i++)
            {
                widths.Add(4 + 1.0 * i);
            }
            // load the acceptable depths
            for (int i = 0; i < 32; i++)
            {
                depths.Add(4 + 1.0 * i);
            }

            foreach (var b in widths)
            {
                foreach (var h in depths)
                {
                    // define the tension options for bars at a distance of "h-cover" from the top of the beam
                    // start with the largest first
                    List<RebarLayer> tensionOptions = new List<RebarLayer>()
                    {
                        new RebarLayer("#4", 1, catalog.RebarTable["#4"], h - cover),
                        new RebarLayer("#5", 1, catalog.RebarTable["#5"], h - cover),
                        new RebarLayer("#6", 1, catalog.RebarTable["#6"], h - cover),
                        new RebarLayer("#7", 1, catalog.RebarTable["#7"], h - cover),
                        new RebarLayer("#8", 1, catalog.RebarTable["#8"], h - cover),
                        new RebarLayer("#9", 1, catalog.RebarTable["#9"], h - cover),
                    };

                    // try singly reinforced first
                    foreach (var tensLayer in tensionOptions)
                    {

                        var section = new CrossSection
                        {
                            Width = b,
                            Depth = h,
                            Cover = cover,
                            Fck = fck,
                            Fy = fy,
                        };
                        // add a trial bar to the section.
                        section.AddTensionRebar(tensLayer.BarSize, tensLayer.Qty, catalog, tensLayer.DepthFromTop);

                        var result = section.ComputeFlexuralStrength();

                        if (result.PhiMn >= MuTarget_kip_in)
                        {
                            successfulSections.Add(result);
                            break;  // found a good option, so no need to investigate larger steel areas
                        }

                        //foreach (var compLayer in compressionOptions)
                        //{

                        //}
                    }
                }
            }

            // filter for unnecessary duplicates of rebar sizes and depths and layers...
            List<DesignResult> filtered = FilterIdealDesignsByWidth(FilterIdealDesignsByDepth(successfulSections));

            // then sort in ascending order of width
            return filtered
                .OrderBy(r => r.crossSection.Width)
                .ThenBy(r => r.crossSection.Depth)
                .ToList();
        }


        List<DesignResult> FilterIdealDesignsByDepth(List<DesignResult> successfulSections)
        {
            RebarCatalog catalog = new RebarCatalog();

            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Width,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{catalog.RebarTable[layer.BarSize].Diameter}")
                    )
                })
                .Select(g => g.OrderBy(r => r.crossSection.Depth).First()) // smallest depth wins
                .ToList();

            return filtered;
        }

        List<DesignResult> FilterIdealDesignsByWidth(List<DesignResult> successfulSections)
        {
            RebarCatalog catalog = new RebarCatalog();

            var filtered = successfulSections
                .GroupBy(r => new
                {
                    r.crossSection.Depth,
                    // normalize the TensionRebar layers into a comparable signature
                    BarsSignature = string.Join(";",
                        r.TensionRebars
                         .OrderBy(layer => layer.DepthFromTop)
                         .Select(layer => $"{catalog.RebarTable[layer.BarSize].Diameter}")
                    )
                })
                .Select(g => g.OrderBy(r => r.crossSection.Width).First()) // smallest depth wins
                .ToList();

            return filtered;
        }
    }
}
