namespace ACI318_19Library
{ 
    /// <summary>
    /// Represents a single reinforcing bar size.
    /// </summary>
    public class Rebar
    {
        public string Designation { get; private set; }
        public double Diameter { get; private set; } // inches
        public double Area { get; private set; }     // in^2

        public Rebar(string designation, double diameter, double area)
        {
            Designation = designation;
            Diameter = diameter;
            Area = area;
        }
    }
}
