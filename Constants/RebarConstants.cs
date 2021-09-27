namespace ACI319_19Library.Constants
{
    public static class RebarConstants
    {
        /// <summary>
        /// Helper class to hold standard ACI bar sizes.
        /// </summary>
        public static class RebarData
        {
            /// <summary>
            /// ACI318_19 Rebar Constants in order 3, 4, 5 ,6 7 , 8 ,9 10, 11, 14, 18
            /// </summary>
            ///                           
            
            // Rebar numeric sizes
            public static int[] Sizes =       {    3,       4,       5,       6,       7,       8,       9,      10,      11,      14,      18 };

            // Rebar cross sectional areas in sq, in.
            public static double[] Areas =   {  0.11,    0.20,    0.31,    0.44,    0.60,    0.79,    1.00,    1.27,    1.56,    2.25,    4.00 };

            // Rebar weight per foot in plf
            public static double[] Weights = { 0.376,   0.668,   1.043,   1.502,   2.044,   2.670,   3.400,   4.303,   5.313,   7.650,   13.60 };
        }


    }
}
