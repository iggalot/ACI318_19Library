using System.Collections.Generic;

namespace ACI318_19Library
{
    public static class RebarCatalog
    {
        public static readonly Dictionary<string, Rebar> RebarTable = new Dictionary<string, Rebar>
        {
            { "#3", new Rebar("#3", 0.375, 0.11) },
            { "#4", new Rebar("#4", 0.500, 0.20) },
            { "#5", new Rebar("#5", 0.625, 0.31) },
            { "#6", new Rebar("#6", 0.750, 0.44) },
            { "#7", new Rebar("#7", 0.875, 0.60) },
            { "#8", new Rebar("#8", 1.000, 0.79) },
            { "#9", new Rebar("#9", 1.128, 1.00) },
            { "#10", new Rebar("#10", 1.270, 1.27) },
            { "#11", new Rebar("#11", 1.410, 1.56) },
            { "#14", new Rebar("#14", 1.693, 2.25) },
            { "#18", new Rebar("#18", 2.257, 4.00) }
        };
    }
}