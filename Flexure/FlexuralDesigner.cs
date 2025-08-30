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
        public List<DesignResultModel> DesignAllSections(
            double MuTarget_kipft,                  // target factored moment

            double cover,

            double fck = 4000, double fy = 60000, double es = 29000000)
        {
            List<DesignResultModel> successfulSections = new List<DesignResultModel>();
            double MuTarget_kip_in = MuTarget_kipft * 12.0; // convert to kip-in

            RebarCatalog catalog = new RebarCatalog();

            List<double> widths = new List<double>();                     // candidate widths (in)
            List<double> depths = new List<double>();                     // candidate depths (in)

            // define the tension options for bars at a distance of "h-cover" from the top of the beam
            // start with the largest first
            var barSizes = new[] { "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11" };

            // candidate compression bars for the top of the beam section
            List<RebarLayer> compressionOptions = new List<RebarLayer>();

            foreach (var size in barSizes)
            {
                for (int qty = 1; qty <= 1; qty++)
                    compressionOptions.Add(new RebarLayer(size, qty, catalog.RebarTable[size], cover));
            }

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


                    var section = new CrossSection
                    {
                        Width = b,
                        Depth = h,
                        Cover = cover,
                        Fck_psi = fck,
                        Fy_psi = fy,
                    };


                    var tensionOptions = new List<RebarLayer>();

                    foreach (var size in barSizes)
                    {
                        for (int qty = 1; qty <= 1; qty++)
                            tensionOptions.Add(new RebarLayer(size, qty, catalog.RebarTable[size], h - cover));
                    }

                    // choose the largest and check if the moment is enough.  If it is, we can iterate through all the bar sizes
                    // otherwise there's no point in continuing with this depth iteration.
                    List<RebarLayer> rebarLayer = tensionOptions.OrderByDescending(x => x.SteelArea).ToList();
                    DesignResultModel first_design = null;
                    if(rebarLayer.Count > 0)
                    {
                        RebarLayer first_test = rebarLayer[0];
                        section.AddTensionRebar(first_test.BarSize, first_test.Qty, catalog, first_test.DepthFromTop);
                        first_design = section.ComputeFlexuralStrength();
                    }

                    // Try single-layer tension reinforcement first
                    bool foundTensionOnlySolution = false;

                    // Order small to large (try most economical first)
                    foreach (var tensLayer in tensionOptions.OrderBy(opt => opt.SteelArea))
                    {
                        var trialSection = section.BaseClone(section); // copy the section without the rebar
                        trialSection.AddTensionRebar(tensLayer.BarSize, tensLayer.Qty, catalog, tensLayer.DepthFromTop);

                        var result = trialSection.ComputeFlexuralStrength();

                        if (result.PhiMn >= MuTarget_kip_in && result.eps_T > 0.005)
                        {
                            successfulSections.Add(result);
                            foundTensionOnlySolution = true;
                            break; // optional: stop at first success
                        }
                    }

                    // If no pure tension solution, try doubly reinforced
                    bool success = false;
                    if (!foundTensionOnlySolution)
                    {
                        foreach (var tensLayer in tensionOptions.OrderBy(opt => opt.SteelArea))
                        {
                            foreach (var compSize in barSizes)
                            {
                                for (int compQty = 1; compQty <= 3; compQty++) // limit compression bars
                                {
                                    var trialSection = section.BaseClone(section);
                                    trialSection.AddTensionRebar(tensLayer.BarSize, tensLayer.Qty, catalog, tensLayer.DepthFromTop);

                                    // compression layer is placed at "cover" depth from top
                                    trialSection.AddCompressionRebar(compSize, compQty, catalog, cover);

                                    var result = trialSection.ComputeFlexuralStrength();

                                    if (result.PhiMn >= MuTarget_kip_in && result.eps_T > 0.005)
                                    {
                                        successfulSections.Add(result);
                                        success = true;
                                    }

                                    if (success)
                                        break;
                                }
                                if (success)
                                    break;
                            }
                            if (success)
                                break; // exit back to depth selection
                        }
                    }
                }
            }

            // filter for unnecessary duplicates of rebar sizes and depths and layers...
            List<DesignResultModel> filtered = successfulSections;
       //     List<DesignResult> filtered = FilterIdealDesignsByWidth(FilterIdealDesignsByDepth(successfulSections));

            // then sort in ascending order of width
            return filtered
                .OrderBy(r => r.crossSection.Width)
                .ThenBy(r => r.crossSection.Depth)
                .ToList();
        }


        List<DesignResultModel> FilterIdealDesignsByDepth(List<DesignResultModel> successfulSections)
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

        List<DesignResultModel> FilterIdealDesignsByWidth(List<DesignResultModel> successfulSections)
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
