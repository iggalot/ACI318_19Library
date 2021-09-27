using System;
using System.Collections.Generic;
using static ACI319_19Library.Constants.RebarConstants;

namespace ACI318_19Library
{
    /// <summary>
    /// Enum to hold the different types of development length that can be analyzed
    /// </summary>
    public enum DevelopmentLengthTypes
    {
        DEV_LENGTH_UNDEFINED = -1,
        DEV_LENGTH_STRAIGHT = 0,
        DEV_LENGTH_STANDARD_HOOK = 1,
        DEV_LENGTH_HOOK_TIES =2,
        DEV_LENGTH_COMPRESSION = 3
    }


    /// <summary>
    /// Base develope length object class that holds information common to all  types of development lengths.
    /// </summary>
    public class BaseDevelopmentLength
    {
        public bool DEBUG_MODE = false;

        public DevelopmentLengthTypes DevLengthType = DevelopmentLengthTypes.DEV_LENGTH_UNDEFINED;

        #region Public Properties

        public List<string> StatusMessageList { get; set; } = new List<string>();

        public int BarSize { get; set; } = 3;
        public double BarDiameter { get; set; }
        public double SteelYieldStrength { get; set; } = 60000;
        public double ConcreteCompStrength { get; set; } = 4000;

        public bool LightWeightConcreteStatus { get; set; } = false;

        public bool EpoxyBarStatus { get; set; } = true;

        public double SideCover { get; set; } = 3;

        public double BottomCover { get; set; } = 3;

        public double CC_Spacing { get; set; } = 3;

        public double Lambda { get; set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.  Used on initial form setup
        /// </summary>
        public BaseDevelopmentLength()
        {
            DevLengthType = DevelopmentLengthTypes.DEV_LENGTH_UNDEFINED;
        }

        /// <summary>
        /// Detailed constructor
        /// </summary>
        /// <param name="devType">Type of development length for this object <see cref="DevelopmentLengthTypes"/></param>
        /// <param name="sizeBar">Rebar size #</param>
        /// <param name="fy">Steel yield strength</param>
        /// <param name="fc">Concrete compressive strength</param>
        /// <param name="mode">DEBUG mode for printing extra info to Console</param>
        /// <param name="cc_spacing">Center to center spacing of bars being developed</param>
        /// <param name="side">Side cover from center of bar to nearest concrete surface (side)</param>
        /// <param name="bottom">Bottom or top cover requirement from center of bar to nearest concrete surface (top/bottom)</param>
        /// <param name="lw">Is the concrete lightweight?</param>
        /// <param name="epoxy">Is the concrete epoxy covered?</param>
        public BaseDevelopmentLength(
            DevelopmentLengthTypes devType,
            int sizeBar, 
            double fy, 
            double fc, 
            bool mode,
            double cc_spacing ,
            double side,
            double bottom,
            bool lw,
            bool epoxy
            )
        {
            BarSize = sizeBar;

            // Validate input information

            // 1. Validate bar size
            bool sizeValid = false;
            foreach(var item in RebarData.Sizes)
            {
                if(BarSize == item)
                {
                    sizeValid = true;
                    break;
                }
            }
            if (!sizeValid)
            {
                throw new InvalidOperationException("ERROR: Invalid bar size - barSize #" + sizeBar.ToString());
            }

            // 2. Validate material strengths
            if (fy <= 0 || fc <= 0)
            {
                throw new InvalidOperationException("ERROR: Material Strength is invalid - fy=" + SteelYieldStrength.ToString() + " f'c=" + ConcreteCompStrength);
            }

            // 3. Validate cover distances
            if (cc_spacing < 0 || side < 0 || bottom< 0)
            {
                throw new InvalidOperationException("ERROR: Cover and spacing reqs must be positive = CC_spacing=" + cc_spacing.ToString() + " : side_cover=" + side.ToString() + " : top/bottom cover=" + bottom.ToString());
            }

            // Set properties
            DevLengthType = devType;
            DEBUG_MODE = mode;
            BarDiameter = sizeBar / 8.0;
            SteelYieldStrength = fy;
            ConcreteCompStrength = fc;
            CC_Spacing = cc_spacing;
            SideCover = side;
            BottomCover = bottom;
            LightWeightConcreteStatus = lw;
            EpoxyBarStatus = epoxy;
            Lambda = LightWeightConcreteStatus ? 0.75 : 1.0;

            StatusMessageList.Add("Bar Size = " + BarSize + " -- dia. = " + BarDiameter);
            StatusMessageList.Add("fy = " + SteelYieldStrength + "      f'c = " + ConcreteCompStrength);
            StatusMessageList.Add("lambda = " + Lambda.ToString() + " -- " + (LightWeightConcreteStatus ? "" : " NOT ") + "lightweight concrete");
        }

        #endregion

        /// <summary>
        /// Computes the development length characteristics of the object
        /// </summary>
        /// <returns></returns>
        public virtual double DevLength() { return -1; }

        /// <summary>
        /// Displays the status messages
        /// </summary>
        /// <returns></returns>
        public virtual string StatusMessagesDisplayString()
        {
            string str = "";
            foreach (var msg in StatusMessageList)
                str += msg + "\n";
            return str;
        }
    }
}
