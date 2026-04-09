namespace HMBD.Exec_CW_Curve;
public class ExeCwCurve{
  public static  bool WheelChamberWithoutCW_1(double x, double y)
        {
            double polynomialValue = -3.05295391318078E-05 * Math.Pow(x, 3) + 3.66629394185078E-02 * Math.Pow(x, 2) - 14.6721814082593 * x + 2009.11098177129;
            return y < polynomialValue;
        }
  public static bool WheelChamberWithoutCW_2(double x, double y)
        {
            double polynomialValue = 1.45721407444395E-10 * Math.Pow(x, 7) - 3.76334758558274E-07 * Math.Pow(x, 5) + 4.03348295014795E-04 * Math.Pow(x, 4) - 0.229660269750966 * Math.Pow(x, 3) + 73.2728098215907 * Math.Pow(x, 2) - 12420.8436313972 * x + 874102.841812403;
            return y < polynomialValue;
        }
        public static bool WheelChamberWithCW_1(double x, double y)
        {
            double polynomialValue = -0.0019 * Math.Pow(x, 2) + 1.309 * x - 169.49;
            return y < polynomialValue;
        }
        public static bool WheelChamberWithCW_2(double x, double y)
        {
            double polynomialValue = -2.11341112074546E-05 * Math.Pow(x, 3) + 2.51275247729883E-02 * Math.Pow(x, 2) - 9.9409285193903 * x + 1371.27722982352;
            return y < polynomialValue;
        }

}