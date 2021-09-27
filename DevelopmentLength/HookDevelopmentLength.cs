using System;
using System.Linq;

namespace ACI318_19Library.DevelopmentLength
{
    /// <summary>
    /// Enum for the type of Hooks that can be handled by this object
    /// HOOK_STANDARD -- hooks are 90 or 180 and not used as confining steel
    /// HOOK_STIRRUP_TIE -- hooks used for confining steel in 90 / 135 / 180 and #8 and smaller bars
    /// </summary>
    public enum HookTypes
    {
        HOOK_UNDEFINED = -1,
        HOOK_STANDARD = 0,    
        HOOK_STIRRUP_TIE = 1
    }

    public class HookDevelopmentLength : BaseDevelopmentLength
    {
        #region Public Properties
        /// <summary>
        /// The hook type
        /// </summary>
        public HookTypes HookType { get; set; } = HookTypes.HOOK_UNDEFINED;

        /// <summary>
        /// PSI Factors per Table 25.4.3.2
        /// </summary>
        public double PSI_E { get; set; }
        public double PSI_R { get; set; }
        public double PSI_O { get; set; }
        public double PSI_C { get; set; }

        // Total cross sectional area of bars confining hook
        public double A_TH {get; set; } = 0.0;

        // Total cross sectional area of hooks or headed bars being developed at critical section
        public double A_HS { get; set; } // don't make this zero...it breaks the PSI_R checks

        // Properties of the hook
        public double LDH { get; set; } = -1;   // development length of standard hook (includes bend diameter)
        public double BendDia { get; set; } = 0; // inside bend diameter of hook
        public double L_EXT { get; set; } = -1; // extension after bend diemater
        public double BendAngle { get; set; } = 90; // Angle of the bend in degrees

        // Does the bar terminate inside a column -- used by PSI_O factor
        public bool TerminateInsideColumnStatus { get; set; } = false;

        #endregion

        #region Constructor
        /// <summary>
        /// Default construct
        /// </summary>
        /// <param name="bar_size">Rebar size #</param>
        /// <param name="steel_strength">Steel yield strength</param>
        /// <param name="concrete_strength">Concrete compressive strength</param>
        /// <param name="DEBUG_MODE">Debug mode for extra console information being displayed</param>
        /// <param name="hook_type">Type of hook <see cref="HookTypes"/></param>
        /// <param name="bend_angle">Angle of the hook in degrees</param>
        /// <param name="adj_cc_spacing">Center to center distance of adjacent bars being developed</param>
        /// <param name="side_cover">Edge distance from center of bar to nearest concrete surface (left / right)</param>
        /// <param name="bottom_cover">Edge distance from center of bar to nearest concrete surface (top / bottom)</param>
        /// <param name="lightweight">Is the concrete lightweight?</param>
        /// <param name="epoxy_coated">Is the rebar epoxy or zinc coated?</param>
        /// <param name="terminate_in_column">Does the bar terminate inside a column -- Used by PSI_O in Table 25.4.3.2</param>
        public HookDevelopmentLength(
            int bar_size,
            double steel_strength,
            double concrete_strength,
            bool DEBUG_MODE,
            HookTypes hook_type,
            double bend_angle,
            double adj_cc_spacing = 3,
            double side_cover = 3,
            double bottom_cover = 3,
            bool lightweight = false,
            bool epoxy_coated = true,
            bool terminate_in_column = false
            ) : base(
                DevelopmentLengthTypes.DEV_LENGTH_STANDARD_HOOK,
                bar_size,
                steel_strength,
                concrete_strength,
                DEBUG_MODE,
                adj_cc_spacing,
                side_cover,
                bottom_cover,
                lightweight,
                epoxy_coated)
        {
            HookType = hook_type;
            BendAngle = bend_angle;
            A_HS = (Math.PI * BarDiameter * BarDiameter / 4.0);
            TerminateInsideColumnStatus = terminate_in_column;
        }
        #endregion

        /// <summary>
        /// Computes the development length parameters for hooked bars.
        /// </summary>
        /// <returns></returns>
        public double HookLength()
        {
            string status_msg = "";
            StatusMessageList.Add("   HookType: " + HookType + "  BendAngle: " + BendAngle.ToString());

            // Compute the PSI factors for the hooked bars
            PSI_E = ComputePSI_E(EpoxyBarStatus, out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_R = ComputePSI_R(out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_O = ComputePSI_O(TerminateInsideColumnStatus, out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_C = ComputePSI_C(out status_msg);
            StatusMessageList.Add(status_msg);

            // Compute the length and bend diameter values
            DetermineHookLengthValues();
            StatusMessageList.Add("LDH: " + LDH.ToString() + "  L_EXT: " + L_EXT.ToString() + "   BendDia: " + BendDia.ToString() + "  BendAngle: " + BendAngle.ToString());
            
            // Debug information display
            if (base.DEBUG_MODE)
            {
                Console.WriteLine("   Analyzing a #" + BarSize.ToString() + " hooked bar with fy=" + SteelYieldStrength.ToString() + " psi amd f'c=" + ConcreteCompStrength.ToString() + "psi");
                Console.WriteLine("   HookType: " + HookType + " -- 0=Standard, 1=Stirrup/Tie");
                Console.WriteLine("   MATS: fy      f'c    lambda");
                Console.WriteLine("         " + SteelYieldStrength.ToString() + " : " + ConcreteCompStrength.ToString() + " : " + Lambda.ToString());
                Console.WriteLine("   Epoxy coated: " + EpoxyBarStatus.ToString() + " : Col. termination: " + TerminateInsideColumnStatus.ToString());
                Console.WriteLine("     PSI  E     R     O     C");
                Console.WriteLine("         " + PSI_E.ToString() + " : " + PSI_R.ToString() + " : " + PSI_O.ToString() + " : " + PSI_C.ToString());
                Console.WriteLine("    Bar terminates in column? " + TerminateInsideColumnStatus);
                Console.WriteLine("   Clear spacing= " + CC_Spacing.ToString() + " : side_cover= " + SideCover.ToString() + " : top_cover=" + BottomCover.ToString());
            }

            // Choose LDH from maximum limits of ACI 25.4.3.1(a), (b), and (c)
            double ldh = (new double[] { LDH, 8 * BarDiameter, 6 }).Max();
            if (LDH == ldh)
                StatusMessageList.Add("- LDH calc controlled by eqn 25.4.3.1(a)");
            else if (ldh == 8 * BarDiameter)
                StatusMessageList.Add("- LDH calc controlled by 8*db per 25.4.3.1(b)");
            else if (ldh == 6)
                StatusMessageList.Add("- LDH calc controlled by min. 6 per 25.4.3.1(c)");

            // Show the status string list
            string status_string = "";
            foreach(var item in StatusMessageList)
            {
                status_string += item + "\n";
            }
            Console.WriteLine(status_string);

            // roundup the result and return;
            return Math.Ceiling(ldh);
        }

        #region LDH Calcs
        protected double ComputeLDH(out string msg)
        {
            double ldh_calc = (SteelYieldStrength * PSI_E * PSI_R * PSI_O * PSI_C) / (55.0 * Lambda * Math.Sqrt(ConcreteCompStrength)) * Math.Pow(BarDiameter, 1.5);
            double ldh_min1 = 8 * BarDiameter;
            double ldh_min2 = 6.0;

            // Determine largest value
            double ldh = (new double[] { ldh_calc, ldh_min1, ldh_min2 }).Max();

            if (ldh == ldh_calc)
                msg = " computed from eqn 25.4.3.1(a)";
            else if (ldh == ldh_min1)
            {
                msg = " computed from 25.4.3.1(b) -- (8 * db)";
            }
            else
                // ldh equals ldh_min2
                msg = "- computed from25.4.3.1(c) -- (6 in.)";
            return ldh;
        }
        #endregion

        #region L_EXT calcs and driver
        /// <summary>
        /// Driver method for determining the value of L_EXT for hooks or ties
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected double ComputeL_EXT(out string msg)
        {
            double bend_dia;
            switch (HookType)
            {
                case HookTypes.HOOK_STANDARD:
                    bend_dia = ComputeL_EXT_ForStandardHook(out msg);
                    break;
                case HookTypes.HOOK_STIRRUP_TIE:
                    bend_dia = ComputeL_EXT_ForStirrupsTies(out msg);
                    break;
                case HookTypes.HOOK_UNDEFINED:
                default:
                    bend_dia = -1;
                    msg = "Invalid hook type received: " + HookType.ToString();
                    break;
            }

            return bend_dia;
        }
        
        /// <summary>
        /// L_EXT for standard hooks
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeL_EXT_ForStandardHook(out string msg)
        {
            msg = BendAngle.ToString() + " deg hook (Table 25.3.1) ";

            if (BendAngle == 90)
            {
                if (BarSize <= 8)
                {
                    msg += " for bar size #3, 4, 5, 6, 7, 8";
                    return 12 * BarDiameter;
                }
                else if (BarSize == 9 || BarSize == 10 || BarSize == 11)
                {
                    msg += " for bar size #9, 10, or 11";
                    return 12 * BarDiameter;
                }
                else
                {
                    msg += " for bar size 14 and 18";
                    return 12 * BarDiameter;
                }
            }
            else if (BendAngle == 135)
            {
                msg += " is non standard";
                return -1;
            }
            else if (BendAngle == 180)
            {
                if (BarSize <= 8)
                {
                    msg += " for bar size #3, 4, 5, 6, 7, 8";
                    return Math.Max(4 * BarDiameter, 2.5);
                }
                else if (BarSize == 9 || BarSize == 10 || BarSize == 11)
                {
                    msg += " for bar size #9, 10, or 11";
                    return Math.Max(4 * BarDiameter, 2.5);
                }
                else
                {
                    msg += " for bar size 14 and 18";
                    return Math.Max(4 * BarDiameter, 2.5);
                }
            }
            else
            {
                msg += " is non standard for angles other than 90, and 180";
                return -1;
            }
        }

        /// <summary>
        /// L_EXT for stirrups and ties
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeL_EXT_ForStirrupsTies(out string msg)
        {
            msg = BendAngle.ToString() + " deg hook (Table 25.3.2) ";

            if (BendAngle == 90)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return Math.Max(6 * BarDiameter, 3);
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return 12 * BarDiameter;
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else if (BendAngle == 135)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return Math.Max(6 * BarDiameter, 3);
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return Math.Max(6 * BarDiameter, 3);
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else if (BendAngle == 180)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return Math.Max(4 * BarDiameter, 2.5);
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return Math.Max(4 * BarDiameter, 2.5);
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else
            {
                msg += " is non standard for angles other than 90, 135, and 180";
                return -1;
            }
        }
        #endregion

        #region Bend Diameter calcs
        /// <summary>
        /// Driver method for determining the bend diameter for hooks or ties
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected double ComputeBendDia(out string msg)
        {
            double bend_dia;
            switch (HookType)
            {
                case HookTypes.HOOK_STANDARD:
                    bend_dia = ComputeBendDia_ForStandardHook(out msg);
                    break;
                case HookTypes.HOOK_STIRRUP_TIE:
                    bend_dia = ComputeBendDia_ForStirrupsTies(out msg);
                    break;
                case HookTypes.HOOK_UNDEFINED:
                default:
                    bend_dia = -1;
                    msg = "Invalid hook type received: " + HookType.ToString();
                    break;
            }

            return bend_dia;
        }

        /// <summary>
        /// Computes the bend diameter for standard hooks
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeBendDia_ForStandardHook(out string msg)
        {
            msg = BendAngle.ToString() + " deg hook (Table 25.3.1) ";

            if (BendAngle == 90)
            {
                if (BarSize <= 8)
                {
                    msg += " for bar size #3, 4, 5, 6, 7, 8";
                    return 6 * BarDiameter;
                }
                else if (BarSize == 9 || BarSize == 10 || BarSize == 11)
                {
                    msg += " for bar size #9, 10, or 11";
                    return 8 * BarDiameter;
                }
                else
                {
                    msg += " for bar size 14 and 18";
                    return 10 * BarDiameter;
                }
            }
            else if (BendAngle == 135)
            {
                    msg += " is non standard";
                    return -1;
            }
            else if (BendAngle == 180)
            {
                if (BarSize <= 8)
                {
                    msg += " for bar size #3, 4, 5, 6, 7, 8";
                    return 6 * BarDiameter;
                }
                else if (BarSize == 9 || BarSize == 10 || BarSize == 11)
                {
                    msg += " for bar size #9, 10, or 11";
                    return 8 * BarDiameter;
                }
                else
                {
                    msg += " for bar size 14 and 18";
                    return 10 * BarDiameter;
                }
            }
            else
            {
                msg += " is non standard for angles other than 90, and 180";
                return -1;
            }
        }

        /// <summary>
        /// Computes the bend diameter for stirrups and ties
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeBendDia_ForStirrupsTies(out string msg)
        {
            msg = BendAngle.ToString() + " deg hook (Table 25.3.2) ";

            if (BendAngle == 90)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return 4 * BarDiameter;
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return 6 * BarDiameter;
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else if (BendAngle == 135)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return 4 * BarDiameter;
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return 6 * BarDiameter;
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else if (BendAngle == 180)
            {
                if (BarSize == 3 || BarSize == 4 || BarSize == 5)
                {
                    msg += " for bar size #3, 4, or 5";
                    return 4 * BarDiameter;
                }
                else if (BarSize == 6 || BarSize == 7 || BarSize == 8)
                {
                    msg += " for bar size #6, 7, or 8";
                    return 6 * BarDiameter;
                }
                else
                {
                    msg += " is non standard for sizes greater than 9";
                    return -1;
                }
            }
            else
            {
                msg += " is non standard for angles other than 90, 135, and 180";
                return -1;
            }
        }
        #endregion

        #region Methods for factors needed for hooks.
        public double ComputePSI_E(bool status, out string msg)
        {
            if (status)
            {
                msg = "PSI_E = 1.2 for epoxy coating per ACI318-19 Table 25.4.3.2";
                return 1.2;
            } else {
                msg = "PSI_E = 1.0 for normal coating per ACI318-19 Table 25.4.3.2";
                return 1.0;
            }
        }

        public double ComputePSI_R(out string msg)
        {
            if (BarSize <= 11)
            {
                if((A_TH >= 0.4 * A_HS) || (CC_Spacing >= 6 * BarDiameter)){
                    msg = "PSI_R = 1.0 - Meets requirements of Table 25.4.3.2";
                    return 1.0;
                }
            }

            msg = "PSI_R = 1.6 - Does not meet requirements of Table 25.4.3.2";
            return 1.6;
        }

        public double ComputePSI_O(bool col_status, out string msg)
        {
            if (BarSize <= 11)
            {
                if (col_status && SideCover >= 2.5){
                    msg = "PSI_O = 1.0 - Bar size is 11 or smaller and terminates in column with more than 2.5 in. side cover";
                    return 1.0;
                } else if (SideCover >= 6 * BarDiameter)
                {
                    msg = "PSI_O = 1.0 - Bar size is 11 or smaller and side cover > 6*db and does not terminate in column";
                    return 1.0;
                }
            }
            msg = "PSI_O = 1.25 - Does not meet requirements of Table 25.4.3.2";
            return 1.25;
        }

        public double ComputePSI_C(out string msg)
        {
            if (ConcreteCompStrength < 6000)
            {
                double val = (ConcreteCompStrength / 15000) + 0.6;
                msg = "PSI_C = " + val.ToString() + " per ACI318-19 Table 25.4.2.5";
                return val;
            }
            else
            {
                msg = "PSI_C = 1.0 for f'c < 6000psi per ACI318-19 Table 25.4.2.5";
                return 1.0;
            }
        }
        #endregion

        /// <summary>
        /// Driver method for computing the parameters of a hook (either tie or standard)
        /// </summary>
        protected void DetermineHookLengthValues()
        {
            string status_msg = "";
            LDH = ComputeLDH(out status_msg);
            StatusMessageList.Add("LDH: " + LDH.ToString() + " " + status_msg);

            L_EXT = ComputeL_EXT(out status_msg);
            StatusMessageList.Add("L_EXT: " + L_EXT.ToString() + " " + status_msg);

            BendDia = ComputeBendDia(out status_msg);
            StatusMessageList.Add("L_BendDiameter = " + BendDia + " " + status_msg);
        }

        public override double DevLength()
        {
            double val = HookLength();
            return val;
        }
    }
}
