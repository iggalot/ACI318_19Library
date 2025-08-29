using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ACI318_19Library
{
    // Add this container class (returns design output)
    public class DesignResult
    {
        /// <summary>Concrete cross section</summary>
        public CrossSection crossSection { get; set; }

        // Reinforcement layers
        public List<RebarLayer> TensionRebars { get; set; } = new List<RebarLayer>();
        public List<RebarLayer> CompressionRebars { get; set; } = new List<RebarLayer>();

        /// <summary>Provided area by selection (in^2)</summary>
        public double AsC { get => CompressionRebars.Sum(r => r.SteelArea); }

        /// <summary>Provided area by selection (in^2)</summary>
        public double AsT { get => TensionRebars.Sum(r => r.SteelArea); }

        /// <summary>Final computed Mn (in-lb)</summary>
        public double Mu { get; set; }

        /// <summary>Final computed φMn (in-lb)</summary>
        public double PhiMn { get => Phi * Mn; }

        /// <summary>Final computed Mn (in-lb)</summary>
        public double Mn { get; set; }

        /// <summary>Final phi used</summary>
        public double Phi { get; set; }

        // <summary>Neutral axis location, c (in)</summary>
        public double NeutralAxis { get; set; }

        /// <summary>tensile strain at extreme tension bar</summary>
        public double eps_T { get; set; }

        /// <summary>Concrete stress block factor β1</summary>
        public double Beta1 { get; set; }

        /// <summary>Balanced steel ratio ρb</summary>
        public double RhoBalanced { get; set; }

        /// <summary>Actual steel ratio ρ = AsT / (b*d)</summary>
        public double RhoActual { get; set; }

        /// <summary>Warnings (e.g. over-reinforced, compression steel not yielded)</summary>
        public string Warnings { get; set; }

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

    }

}
