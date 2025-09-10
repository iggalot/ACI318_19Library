using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ACI318_19Library
{
    // Add this container class (returns design output)
    public class FlexuralDesignResultModel
    {
        /// <summary>Concrete cross section</summary>
        public CrossSection crossSection { get; set; }

        // ======================
        // FLEXURE SUMMARY
        // ======================

        public string TensionRebarSummary
        {
            get
            {
                string str = String.Empty;
                if (crossSection.TensionRebars.Count == 0)
                {
                    return "None";
                }

                foreach (var layer in crossSection.TensionRebars)
                {
                    str += $"{layer.Qty}-{layer.BarSize} at {layer.DepthFromTop}, ";
                }
                return str;
            }
        }

        public string CompressionRebarSummary
        {
            get
            {
                string str = String.Empty;
                if (crossSection.CompressionRebars.Count == 0)
                {
                    return "None";
                }

                foreach (var layer in crossSection.CompressionRebars)
                {
                    str += $"{layer.Qty}-{layer.BarSize} at {layer.DepthFromTop}, ";
                }
                return str;
            }
        }

        /// <summary>Final computed φMn (in-lb)</summary>
        public double PhiMn { get => PhiFlexure * Mn; }

        /// <summary>Final computed Mn (in-lb)</summary>
        public double Mn { get; set; }

        /// <summary>Final phi used for flexure</summary>
        public double PhiFlexure { get; set; } = 0.9;

        // <summary>Neutral axis location, c (in)</summary>
        public double NeutralAxis { get; set; }

        // whitney block depth 'a'
        public double a { get => NeutralAxis * Beta1; }

        /// <summary>tensile strain at extreme tension bar</summary>
        public double eps_T { get; set; }

        /// <summary>depth in cross section to extreme tension bar</summary>
        public double DepthToEpsT { get; set; }

        /// <summary>Provided area by selection (in^2)</summary>
        public double AsC { get => crossSection.CompressionRebars.Sum(r => r.SteelArea); }

        /// <summary>Provided area by selection (in^2)</summary>
        public double AsT { get => crossSection.TensionRebars.Sum(r => r.SteelArea); }

        public double AreaGross { get => crossSection.AreaGross; }

        /// <summary>Concrete stress block factor β1</summary>
        public double Beta1 { get => FlexuralDesigner.GetBeta1(crossSection.Fck_psi); }

        /// <summary>Actual steel ratio ρ = AsT / (b*d)</summary>
        public double RhoActual { get => AsT / crossSection.AreaGross; }

        public bool IsOverreinforced { get => RhoActual > crossSection.RhoBalanced; }

        /// <summary>FlexuralWarnings (e.g. over-reinforced, compression steel not yielded)</summary>
        public string FlexuralWarnings { get; set; }

        /// <summary>FlexuralWarnings (e.g. over-reinforced, compression steel not yielded)</summary>
        public string ShearWarnings { get; set; }

        // ======================
        // SHEAR SUMMARY
        // ======================

        /// <summary>Concrete shear contribution Vc (kips)</summary>
        public double Vc { get; set; }

        /// <summary>Steel shear contribution Vs (kips)</summary>
        public double Vs { get; set; }

        /// <summary>Nominal shear capacity Vn (kips)</summary>
        public double Vn { get; set; }

        /// <summary>Strength reduction factor for shear (φ)</summary>
        public double PhiShear { get; set; } = 0.75;

        /// <summary>Design shear strength φVn (kips)</summary>
        public double PhiVn { get => PhiShear * Vn; }

        public double Av_over_s { get; set; }

        public string DisplayShearInfo()
        {
            string str = string.Empty;

            // Shear display
            if (Vn > 0)
            {
                str += $" | φVn = {PhiVn:F1} kips (Vc={Vc:F1}, Vs={Vs:F1})";
            }

            if (!string.IsNullOrWhiteSpace(ShearWarnings))
                str += $" | ShearWarnings: {ShearWarnings}";

            return str;
        }

        // ======================
        // REFLECTION-BASED DISPLAY
        // ======================

        public string ConcreteDesignInfo
        {
            get
            {
                var sb = new StringBuilder();

                foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip the ConcreteDesignInfo property itself to avoid recursion
                    if (prop.Name == nameof(ConcreteDesignInfo)) continue;

                    object value = prop.GetValue(this);

                    if (prop.Name == nameof(crossSection.TensionRebars))
                    {
                        List<RebarLayer> tens_rebar_obj = value as List<RebarLayer>;

                        string str = String.Empty;
                        for (int i = 0; i < tens_rebar_obj.Count; i++)
                        {
                            str += $"({tens_rebar_obj[i].Qty})-{tens_rebar_obj[i].BarSize} at {tens_rebar_obj[i].DepthFromTop},  ";
                        }
                        sb.AppendLine($"{prop.Name}: {tens_rebar_obj.Count} tension rebar layers -- {str}");
                        continue;
                    }

                    if (prop.Name == nameof(crossSection.CompressionRebars))
                    {
                        List<RebarLayer> comp_rebar_obj = value as List<RebarLayer>;

                        string str = String.Empty;
                        for (int i = 0; i < comp_rebar_obj.Count; i++)
                        {
                            str += $"({comp_rebar_obj[i].Qty})-{comp_rebar_obj[i].BarSize} at {comp_rebar_obj[i].DepthFromTop},  ";
                        }
                        sb.AppendLine($"{prop.Name}: {comp_rebar_obj.Count} compression rebar layers -- {str}");
                        continue;
                    }

                    if (value == null)
                    {
                        sb.AppendLine($"{prop.Name}: null");
                        continue;
                    }

                    // If the property has a DisplayInfo method, call it
                    MethodInfo displayMethod = value.GetType().GetMethod("DisplayInfo", BindingFlags.Public | BindingFlags.Instance);
                    if (displayMethod != null)
                    {
                        try
                        {
                            object displayValue = displayMethod.Invoke(value, null);
                            sb.AppendLine(displayValue?.ToString() ?? $"{prop.Name}: (null)");
                        }
                        catch
                        {
                            sb.AppendLine($"{prop.Name}: (error calling DisplayInfo)");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{prop.Name}: {value}");
                    }
                }

                return sb.ToString();
            }
        }

        // ======================
        // RESULT SUMMARY FOR DISPLAY
        // ======================

        public string ResultSummary
        {
            get
            {
                string str = string.Empty;
                str += crossSection != null
                    ? DisplayFlexuralModelInfo()
                    : $"(No section) | Mu={PhiMn:F0} kip-in";

                return str;
            }
        }

        public string DisplayFlexuralModelInfo()
        {
            string str = string.Empty;
            str += $"W: {crossSection.Width} x D: {crossSection.Height} | Ag = {crossSection.AreaGross} sq.in. | PhiMn={PhiMn:F0} kip-in";

            if (crossSection.TensionRebars.Count > 0)
            {
                str += " | Tension: ";
                foreach (var layer in crossSection.TensionRebars)
                {
                    str += $"{layer.Qty}-{layer.BarSize} at {layer.DepthFromTop}, ";
                }
            }
            if (crossSection.CompressionRebars.Count > 0)
            {
                str += " | Compression: ";
                foreach (var layer in crossSection.CompressionRebars)
                {
                    str += $"{layer.Qty}-{layer.BarSize} at {layer.DepthFromTop}, ";
                }
            }


            if (!string.IsNullOrWhiteSpace(FlexuralWarnings))
                str += $" | FlexuralWarnings: {FlexuralWarnings}";

            return str;
        }
    }
}
