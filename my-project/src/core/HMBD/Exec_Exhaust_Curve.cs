namespace HMBD.Exec_Exhaust_Curve;
public class ExhaustFunctions
{
    public static bool Exhaust1(double x, double y)
    {
        return y < (-4.87163299663345E-06 * Math.Pow(x, 3) 
                    + 4.06240981241024E-03 * Math.Pow(x, 2) 
                    - 1.12687890812905 * x 
                    + 121.916666666684);
    }

    public static bool Exhaust2(double x, double y)
    {
        return y < (-5.17676767676804E-06 * Math.Pow(x, 3) 
                    + 4.38284632034667E-03 * Math.Pow(x, 2) 
                    - 1.23618145743157 * x 
                    + 129.350000000013);
    }

    public static double Exhaust1_GetUpperLimit(double x)
    {
        return (-4.87163299663345E-06 * Math.Pow(x, 3) 
                + 4.06240981241024E-03 * Math.Pow(x, 2) 
                - 1.12687890812905 * x 
                + 121.916666666684);
    }

    public static double Exhaust2_GetUpperLimit(double x)
    {
        return (-5.17676767676804E-06 * Math.Pow(x, 3) 
                + 4.38284632034667E-03 * Math.Pow(x, 2) 
                - 1.23618145743157 * x 
                + 129.350000000013);
    }
}
