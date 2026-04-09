using System;
namespace HMBD.Cu_CW_Curve;

public class WheelChamber
{
    public static bool WheelChamberWithCW_1(double x, double y)
    {
        double result = -0.0019 * Math.Pow(x, 2) + 1.309 * x - 169.49;
        return y < result;
    }

    public static bool WheelChamberWithCW_2(double x, double y)
    {
        double result = -2.11341112074546E-05 * Math.Pow(x, 3) 
                        + 2.51275247729883E-02 * Math.Pow(x, 2) 
                        - 9.9409285193903 * x 
                        + 1371.27722982352;
        return y < result;
    }

    public static double WheelChamberWithCW_1_GetUpperLimit(double x)
    {
        return -0.0019 * Math.Pow(x, 2) + 1.309 * x - 169.49;
    }

    public static double WheelChamberWithCW_2_GetUpperLimit(double x)
    {
        return -2.11341112074546E-05 * Math.Pow(x, 3) 
               + 2.51275247729883E-02 * Math.Pow(x, 2) 
               - 9.9409285193903 * x 
               + 1371.27722982352;
    }

    public static double WheelChamberTemp_CurveWithCW1_GetUpperLimit(double wheelChamberPressure)
    {
        double x = wheelChamberPressure;
        return -0.0019 * Math.Pow(x, 3) 
               + 0.1196 * Math.Pow(x, 2) 
               - 3.7148 * x 
               + 522.26;
    }

    public static double WheelChamberTemp_CurveWithCW2_GetUpperLimit(double wheelChamberPressure)
    {
        double x = wheelChamberPressure;
        if (x < 60)
        {
            return (-1.6792 * x) + 564.31;
        }
        else
        {
            return (-26 * x) + 2026;
        }
    }
    public static bool WheelChamberWithoutCW_2(double x, double y)
    {
        double result = 1.45721407444395E-10 * Math.Pow(x, 7) 
                        - 3.76334758558274E-07 * Math.Pow(x, 5) 
                        + 4.03348295014795E-04 * Math.Pow(x, 4) 
                        - 0.229660269750966 * Math.Pow(x, 3) 
                        + 73.2728098215907 * Math.Pow(x, 2) 
                        - 12420.8436313972 * x 
                        + 874102.841812403;

        return y <= result;
    }

    public static bool WheelChamberWithoutCW_1(double x, double y)
    {
        double result = -3.05295391318078E-05 * Math.Pow(x, 3) 
                        + 3.66629394185078E-02 * Math.Pow(x, 2) 
                        - 14.6721814082593 * x 
                        + 2009.11098177129;

        return y <= result;
    }


    public static void TestCurves()
    {
        Console.WriteLine(WheelChamberTemp_CurveWithCW2_GetUpperLimit(62));
    }
}






