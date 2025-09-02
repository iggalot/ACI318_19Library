using System;
using System.Reflection;

namespace ACI318_19Library.Flexure
{
    /// <summary>
    /// A class for proportioning first guess of dimensions of members based on span.  Uses 
    /// common rules of thumb to get started.
    /// </summary>
    public static class MemberDimensionProportioner
    {
        /// <summary>
        /// Returns a first guess at the dimensions of the cross section as (double Width (in), double Depth (in))
        /// </summary>
        /// <param name="dimension"
        /// -- For Beams -> Simply supported span</param>
        /// -- For 1-Way Slab -> Simply supported span
        /// -- For 2-Way Slab -> Longer of simply supportedspans
        /// -- For Walls --> Use height for h/depth ratio.  Use 12" for width, Depth for thickness
        /// -- For Retaining Walls --> Use height, H.  Use 12" for width, Depth for thickness
        /// -- For Columns --> Use column length.
        /// <param name="member_type"></param>
        /// <returns></returns>
        public static (double, double) ProportionDimensions(double dimension_ft, MemberTypes member_type)
        {
            switch (member_type)
            {
                case MemberTypes.MBR_BEAM:
                    return (Math.Ceiling(dimension_ft * 12.0 / 20.0), Math.Ceiling(dimension_ft * 12.0 * 0.4));
                case MemberTypes.MBR_ONE_WAY_SLAB:
                    return (12.0, Math.Ceiling(dimension_ft * 12.0 / 20.0));
                case MemberTypes.MBR_TWO_WAY_SLAB:
                    return (12.0, Math.Ceiling(dimension_ft * 12.0 / 30.0));
                case MemberTypes.MBR_WALL:
                    return (12.0, Math.Ceiling(dimension_ft * 12.0 / 25.0));
                case MemberTypes.MBR_RETAINING_WALL:
                    return (12.0, Math.Ceiling(dimension_ft * 12.0 * 0.1));
                case MemberTypes.MBR_COLUMN:
                    return (Math.Ceiling(dimension_ft * 12.0 / 40.0), Math.Ceiling(dimension_ft * 12.0 / 40.0));
                default:
                    return (12.0, 12.0);
            }
        }
    }
}
