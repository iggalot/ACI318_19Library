using System;
using System.Linq;

namespace ACI318_19Library.DevelopmentLength
{
    public class StraightDevelopmentLength : BaseDevelopmentLength
    {
        #region Private Members
        // Stores the product of psi_t * psi_e and the max limit of Table 25.4.2.5
        private double m_PSI_E_X_PSI_T_PRODUCT;
        #endregion

        #region Public Members
        public bool TopBarStatus { get; set; } = true;
        public bool HasMinTransverseReinf { get; set; } = false;
        public double K_TR { get; set; } = 0;
        public bool KTR_Was_Calculated { get; set; } = false;
        public bool TransReinfProvided { get; set; } = false;

        public double C_B { get; set; }
        public double PSI_E { get; set; }
        public double PSI_G { get; set; }
        public double PSI_T { get; set; }
        public double PSI_S { get; set; }

        public double LD { get; set; } = -1;   // development length of the straight bar
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for our straight bar development length object.  Creates the development length
        /// object and sets up the base class parameters.
        /// </summary>
        /// <param name="bar_size">size # of the bar</param>
        /// <param name="steel_strength">yield strength of the rebar</param>
        /// <param name="concrete_strength">compressive strength of concrete</param>
        /// <param name="DEBUG_MODE">should we display DEBUG parameters to console</param>
        /// <param name="ktr">K_TR parameter per ACI 25.4.2.4(b) -- conservatively taken as 0</param>
        /// <param name="tr_provided">is minimum transverse reinforcmeent provided</param>
        /// <param name="adj_cc_spacing">center to center spacing of bars being developed</param>
        /// <param name="side_cover">side cover distance</param>
        /// <param name="bottom_cover">bottom / top cover distance</param>
        /// <param name="lightweight">is the concrete lightweight</param>
        /// <param name="epoxy_coated">is the rebar zinc or epoxy coated</param>
        /// <param name="top_bar_position">is there more than 12 inches of fresh concerete vertically below the development region</param>
        public StraightDevelopmentLength(
            int bar_size,
            double steel_strength,
            double concrete_strength,
            bool DEBUG_MODE,
            double ktr = 0,
            bool ktr_calc_status = false,
            bool tr_provided = false,
            double adj_cc_spacing = 3,
            double side_cover = 3,
            double bottom_cover = 3,
            bool lightweight = false,
            bool epoxy_coated = true,
            bool top_bar_position = true
            ) : base(
                DevelopmentLengthTypes.DEV_LENGTH_STRAIGHT,
                bar_size,
                steel_strength,
                concrete_strength,
                DEBUG_MODE,
                adj_cc_spacing,
                side_cover,
                bottom_cover,
                lightweight ,
                epoxy_coated)
        {

            if (ktr < 0)
                throw new InvalidOperationException("ERROR: Ktr cannot be less than zero - Ktr=" + ktr.ToString());

            K_TR = ktr;
            KTR_Was_Calculated = ktr_calc_status;
            HasMinTransverseReinf = tr_provided;
            TopBarStatus = top_bar_position;
        }

        #endregion

        /// <summary>
        /// Computes the development length parameters for straight bars.
        /// </summary>
        /// <returns></returns>
        private double StraightLength()
        {
            string status_msg = "";
            bool spacing_and_cover_reqs_okay = false;

            PSI_E = ComputePSI_E(out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_S = ComputePSI_S(out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_T = ComputePSI_T(out status_msg);
            StatusMessageList.Add(status_msg);

            PSI_G = ComputePSI_G(out status_msg);
            StatusMessageList.Add(status_msg);

            // Apply the limit from ACI318-19 Table 25.4.2.5 for product of psi_e and psi_t greater than 1.7
            m_PSI_E_X_PSI_T_PRODUCT = ComputePSI_E_x_Psi_T_Product(out status_msg);
            StatusMessageList.Add(status_msg);

            StatusMessageList.Add("Ktr=" + K_TR.ToString() + (KTR_Was_Calculated ? " - was computed using ACI 25.4.2.4b" : " - conservatively assumed per ACI 25.4.2b"));

            // Compute cb from lesser of half of clear cc spacing or side or bottom covers
            C_B = ComputeCb(out status_msg);
            StatusMessageList.Add(status_msg);

            // Compute the (cb + Ktr) / db factor and apply limit of 2.5 from ACI318-19 25.4.2.4 
            double cb_plus_Ktr_factor = ComputeCb_Plus_Ktr(out status_msg);
            StatusMessageList.Add(status_msg);

            // Checks for ACI318-19 Table 25.4.2.3
            spacing_and_cover_reqs_okay = CheckSpacingAndCoverReqs(out status_msg);
            StatusMessageList.Add(status_msg);

            // Determine which equation from Table 25.4.2.3 to use for straight development length
            string devLengthEqString = "";
            double devLength;
            if (BarSize <= 6)
            {
                devLengthEqString += "Bar size 6 and smaller";
                if (spacing_and_cover_reqs_okay)
                {
                    devLengthEqString += " & spacing reqs are okay (ACI318-19 Table 24.4.2.3).";
                    devLength = SmallBarDevLength_SpacingReqsOkay();
                }
                else
                {
                    devLengthEqString += " & spacing reqs exceed limits (ACI318-19 Table 24.4.2.3)";
                    devLength = SmallBarDevLength_SpacingReqsInvalid();
                }
            }
            else
            {
                devLengthEqString += "Bar size greater than 6";
                if (spacing_and_cover_reqs_okay)
                {
                    devLengthEqString += " & spacing reqs are okay (ACI318-19 Table 24.4.2.3).";
                    devLength = LargeBarDevLength_SpacingReqsOkay();
                }
                else
                {
                    devLengthEqString += " & spacing reqs exceed limits (ACI318-19 Table 24.4.2.3)";
                    devLength = LargeBarDevLength_SpacingReqsInvalid();
                }
            }
            StatusMessageList.Add(devLengthEqString);

            // Tension development length per simplified equations of Table 25.4.2.3
            if(devLength < 12)
            {
                StatusMessageList.Add("- Simplified LD= " + devLength.ToString() + " per ACI318-19 25.4.2.1(a)- Table 25.4.2.3 equations for simplified calcs.");
            } else
            {
                StatusMessageList.Add("- Simplified min. LD from 25.4.2.1(b) = 12 in. controls."); 
            }
            devLength = Math.Max(devLength, 12.0);  // applies the limit

            // Tension development length per ACI318-19 Eqn. 25.4.2.4a
            double devLength_allfactors = (3 * SteelYieldStrength * PSI_S * PSI_G * m_PSI_E_X_PSI_T_PRODUCT / (40.0 * Lambda * Math.Sqrt(ConcreteCompStrength) * cb_plus_Ktr_factor)) * BarDiameter;
            if (devLength_allfactors < 12)
            {
                StatusMessageList.Add("- All factors min. LD from 25.4.2.1(b) = 12 in. controls.");
            }
            else
            {
                StatusMessageList.Add("- All factors LD calculated from equation 25.4.2.1(a) controls.");
            }
            devLength_allfactors = Math.Max(devLength_allfactors, 12.0); // applies the limit

            // Set the value to LD
            LD = Math.Min(devLength, devLength_allfactors);
            
            if (DEBUG_MODE)
            {
                Console.WriteLine("   Analyzing a #" + BarSize.ToString() + " straight bar with fy=" + SteelYieldStrength.ToString() + " psi amd f'c=" + ConcreteCompStrength.ToString() + "psi");
                Console.WriteLine("   MATS: fy      f'c    lambda");
                Console.WriteLine("         " + SteelYieldStrength.ToString() + " : " + ConcreteCompStrength.ToString() + " : " + Lambda.ToString());
                Console.WriteLine("   Epoxy coated: " + EpoxyBarStatus.ToString() + " : Top bars: " + TopBarStatus.ToString());

                string str = "   PSI  E     S     T     G";

                if ((PSI_E * PSI_T > 1.7))
                    str += " -- Table 25.4.2.5 limit of 1.7 triggered";
                Console.WriteLine(str);
                Console.WriteLine("        " + PSI_E.ToString() + " : " + PSI_S.ToString() + " : " + PSI_T.ToString() + " : " + PSI_G.ToString());

                if (K_TR == 0)
                    Console.WriteLine("   Conservative Ktr=0 used.");

                str += cb_plus_Ktr_factor.ToString();
                if ((C_B + K_TR) / BarDiameter >= 2.5)
                    str += ((C_B + K_TR) / BarDiameter).ToString() + " - but LIMIT TRIGGERED -- 2.5 used";

                Console.WriteLine("   Clear spacing= " + CC_Spacing.ToString() + " : side_cover= " + SideCover.ToString() + " : top_cover=" + BottomCover.ToString());
                Console.WriteLine("   cb=" + C_B.ToString() + " :   (Cb + Ktr) / db = " + str);

                Console.WriteLine(devLengthEqString);

                Console.WriteLine("    ld = " + devLength.ToString() + " vs. " + devLength_allfactors.ToString());
            }

            StatusMessageList.Add("Table 25.4.2.3 Simplified Eqns for LD = " + devLength.ToString());
            StatusMessageList.Add("ACI318-19 Eqn 25.4.2.4a for LD=" + devLength_allfactors.ToString());

            // Round up the result and choose the smaller of the two computations
            return Math.Ceiling(LD);
        }

        #region Methods for factors needed for straight bars.

        /// <summary>
        /// Computes the epoxy factor per ACI318-19 Table 25.4.2.5
        /// </summary>
        /// <param name="msg">holds the return message from this check</param>
        /// <returns></returns>
        private double ComputePSI_E(out string msg)
        {
            double smallestCover = Math.Min(SideCover, BottomCover);

            if (EpoxyBarStatus)
            {
                // clear spacing less than 6 * db or clear cover less than 3 * db
                if ((smallestCover < 6 * BarDiameter) || (CC_Spacing < 3 * BarDiameter))
                {
                    msg = "PSI_E = 1.5 per ACI318-19 Table 25.4.2.5\nEpoxy coated and cover less than 3db or clear spacing less than 6db";
                    return 1.5;
                } else
                {
                    msg = "PSI_E = 1.2 per ACI318-19 Table 25.4.2.5\nEpoxy coated with no cover or spacing issues";
                    return 1.2;
                }
            }
            else
            {
                msg = "PSI_E = 1.0 per ACI318-19 Table 25.4.2.5\nNon-epoxy coated";
                return 1.0;
            }
        }

        /// <summary>
        /// Computes the reinforcement grade factor per ACI318-19 Table 25.4.2.5
        /// </summary>
        /// <param name="msg">holds the return message from this check</param>
        /// <returns></returns>
        private double ComputePSI_G(out string msg)
        {
            if (SteelYieldStrength <= 60000)
            {
                msg = "PSI_G = 1.0 per ACI318-19 Table 25.4.2.5";
                return 1.0;
            }
            else if (SteelYieldStrength > 80000)
            {
                msg = "PSI_G = 1.3 per ACI318-19 Table 25.4.2.5";
                return 1.3;
            }
            else
            {
                msg = "PSI_G = 1.15 per ACI318-19 Table 25.4.2.5";
                return 1.15;
            }
        }

        /// <summary>
        /// Computes the rebar size factor per ACI318-19 Table 25.4.2.5
        /// </summary>
        /// <param name="msg">holds the return message from this check</param>
        /// <returns></returns>
        private double ComputePSI_S(out string msg)
        {
            if (BarSize <= 7)
            {
                msg = "PSI_S = 0.7 per ACI318-19 Table 25.4.2.5";
                return 0.7;
            }
            else
            {
                msg = "PSI_G = 1.0 per ACI318-19 Table 25.4.2.5";
                return 1.0;
            }
        }

        /// <summary>
        /// Computes the top bar factor for more than 12 in. of fresh concrete below the bar being developed factor per ACI318-19 Table 25.4.2.5
        /// </summary>
        /// <param name="msg">holds the return message from this check</param>
        /// <returns></returns>
        private double ComputePSI_T(out string msg)
        {
            if (TopBarStatus)
            {
                msg = "PSI_T = 1.2 per ACI318-19 Table 25.4.2.5";
                return 1.2;
            }
            else
            {
                msg = "PSI_T = 1.0 per ACI318-19 Table 25.4.2.5";
                return 1.0;
            }
        }

        /// <summary>
        /// Computes the product of PSI_E and PSI_T and imposes the
        /// maximum limit of 1.7 per the footnote [1] of ACI318-19 Table 25.4.2.5
        /// </summary>
        /// <param name="msg">holds the return message from this check</param>
        /// <returns></returns>
        private double ComputePSI_E_x_Psi_T_Product(out string msg)
        {
            msg = "PSI_E x PSI_T product ";
            if (PSI_E * PSI_T > 1.7)
            {
                msg += " is limited at max of 1.7 (computed as " + (PSI_E * PSI_T).ToString() + ") per ACI318-19 Table 25.4.2.5";
                return 1.7;
            } else
            {
                msg += "= " + (PSI_E * PSI_T).ToString() + " is less than 1.7 so no limit imposed per ACI318-19 Table 25.4.2.5";
                return (PSI_E * PSI_T);
            }
        }

        /// <summary>
        /// Determines the critical bond region per definition page 18 of ACI318-19
        /// "lesser of: (a) the distance from center of bar or wire to nearest concrete surface, and
        /// (b) one-half the center-to-center spacing of bars or wires being developed
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeCb(out string msg)
        {
            double cb = (new double[] { CC_Spacing / 2.0, SideCover, BottomCover }).Min();
            msg = "cb = " + cb.ToString() + " -- ";
            if (cb == CC_Spacing / 2.0)
            {
                msg += "clear spacing / 2.0 controls : ";
            }
            if (cb == SideCover)
            {
                msg += "side cover controls : ";
            }
            if(cb == BottomCover)
            {
                msg += "top cover controls";
            }

            return cb;
        }

        /// <summary>
        /// Computes the denominator (cb+Ktr)/db factor per ACI318-19 Section 25.4.2.4 and imposes
        /// the maximum limit of 2.5 for this quantity.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private double ComputeCb_Plus_Ktr(out string msg)
        {
            if (((C_B + K_TR) / BarDiameter) > 2.5)
            {
                msg = "(cb + Ktr) / db = 2.5 -- computed as " + ((C_B + K_TR) / BarDiameter).ToString() + " which exceeds limit of 2.5 from ACI318-19 25.4.2.4";
                return 2.5;
            } else
            {
                msg = "(cb + Ktr) / db is " + ((C_B + K_TR) / BarDiameter).ToString() +" per ACI318-19 25.4.2.4";
                return (C_B + K_TR) / BarDiameter;
            }
        }

        /// <summary>
        /// Provides a summary of necessary checks for choosing which development shortcut equation to be used
        /// in Table 25.4.2.3.  This checks the three different requirements necessary for the first row of equations in the table.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool CheckSpacingAndCoverReqs(out string msg)
        {
            bool spacing_check = CC_Spacing > BarDiameter ? true : false;
            bool cover_check = ((SideCover > BarDiameter) && (BottomCover > BarDiameter)) ? true : false;

            if(spacing_check)
            {
                msg = "ACI318-19 Table 25.4.2.3 spacing reqs okay\n";
            } else
            {
                msg = "ACI318-19 Table 25.4.2.3 spacing reqs not okay\n";
            }

            if(cover_check)
            {
                msg += "ACI318-19 Table 25.4.2.3 top and side cover reqs okay\n";
            }
            else
            {
                msg += "ACI318-19 Table 25.4.2.3 top / bot and side cover reqs not okay\n";
            }

            if(HasMinTransverseReinf)
            {
                msg += "ACI318-19 Table 25.4.2.3 transverse reinforcing reqs okay\n";
            }
            else
            {
                msg += "ACI318-19 Table 25.4.2.3 transverse reinforcing reqs not okay\n";
            }

            if (spacing_check && cover_check && HasMinTransverseReinf)
                msg += "All Table 25.4.2.3 spacing checks OKAY";

            return (spacing_check && cover_check && HasMinTransverseReinf);
        }

        #endregion

        #region Table 25.4.2.3 equations
        private double SmallBarDevLength_SpacingReqsOkay()
        {
            return Math.Max(12, (SteelYieldStrength * m_PSI_E_X_PSI_T_PRODUCT * PSI_G) / (25.0 * Lambda * Math.Sqrt(ConcreteCompStrength)) * BarDiameter);
        }

        private double SmallBarDevLength_SpacingReqsInvalid()
        {
            return Math.Max(12, (3.0 * SteelYieldStrength * m_PSI_E_X_PSI_T_PRODUCT * PSI_G) / (50.0 * Lambda * Math.Sqrt(ConcreteCompStrength)) * BarDiameter);
        }

        private double LargeBarDevLength_SpacingReqsOkay()
        {
            return Math.Max(12, ( SteelYieldStrength * m_PSI_E_X_PSI_T_PRODUCT * PSI_G) / (20.0 * Lambda * Math.Sqrt(ConcreteCompStrength)) * BarDiameter);
        }

        private double LargeBarDevLength_SpacingReqsInvalid()
        {
            return Math.Max(12, (3.0 * SteelYieldStrength * m_PSI_E_X_PSI_T_PRODUCT * PSI_G) / (40.0 * Lambda * Math.Sqrt(ConcreteCompStrength)) * BarDiameter);
        }
        #endregion

        /// <summary>
        /// Computes the development length characteristics of the object
        /// </summary>
        /// <returns></returns>
        public override double DevLength()
        {
            double val = StraightLength();
            return val;
        }
    }
}
