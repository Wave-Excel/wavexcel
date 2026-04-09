using System;
using System.IO;
using System.IO.Packaging;
using Ignite_x_wavexcel.Utilities;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Models.PowerEfficiencyData;
using Models.TurbineData;
using OfficeOpenXml;
using StartExecutionMain;
// using TurbineUtils;

namespace HMBD.HMBD_GeneratorEfficiency;
public class GeneratorEfficiencyCalculator
{
    private string filePath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";

    private TurbineDataModel turbineDataModel;
    private PowerEfficiencyModel powerEfficiencyModel;

    private IConfiguration configuration;
    public ILogger logger;
    public GeneratorEfficiencyCalculator()
    {
        turbineDataModel = TurbineDataModel.getInstance();
        powerEfficiencyModel = PowerEfficiencyModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        filePath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        // worksheet = excelPackage.Workbook.Worksheets["OPEN_HBD"];
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    public void GetGeneratorEfficiency(double eff)
    {
        int closestNumber = FindClosestNumber();
        CalcGeneratorEfficiency(closestNumber, eff);
    }
    public void Logger(string message)
    {
        logger.LogInformation(message);
    }

    private int FindClosestNumber()
    {
        List<PowerEfficiencyDataPoint> powEffList = powerEfficiencyModel.PowerEfficiencyPoints;
        double targetValue = turbineDataModel.AK25;//Turbine.AK25;//worksheet.Cells["K11"].GetValue<double>();
        double closestValue = powEffList[0].Power * 1000;//rng["AI30"].GetValue<double>();
        double minDifference = Math.Abs(targetValue - closestValue);
        
        foreach(PowerEfficiencyDataPoint powEffDataPoint in powEffList){
            double val = powEffDataPoint.Power * 1000;
            double difference = Math.Abs(targetValue - val);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestValue = val;//cell.GetValue<double>();
            }
        }
        // worksheet.Cells["L11"].Value = closestValue;
        return (int)closestValue;
    }

    private void CalcGeneratorEfficiency(int power, double efficiency)
    {
        double outputCell = 0;
        switch (power)
        {
            case 1000:
                outputCell = LagrangeInterpolation1000(efficiency) / 100;
                break;
            case 1500:
                outputCell = LagrangeInterpolation1500(efficiency) / 100;
                break;
            case 2000:
                outputCell = LagrangeInterpolation2000(efficiency) / 100;
                break;
            case 2500:
                outputCell = LagrangeInterpolation2500(efficiency) / 100;
                break;
            case 3000:
                outputCell = LagrangeInterpolation3000(efficiency) / 100;
                break;
            case 3400:
                outputCell = LagrangeInterpolation3400(efficiency) / 100;
                break;
            case 4000:
                outputCell = LagrangeInterpolation4000(efficiency) / 100;
                break;
            case 4500:
                outputCell = LagrangeInterpolation4500(efficiency) / 100;
                break;
            case 5000:
                outputCell = LagrangeInterpolation5000(efficiency) / 100;
                break;
            case 6000:
                outputCell = LagrangeInterpolation6000(efficiency) / 100;
                break;
            case 7000:
                outputCell = LagrangeInterpolation7000(efficiency) / 100;
                break;
            case 7500:
                outputCell = LagrangeInterpolation7500(efficiency) / 100;
                break;
            case 8000:
                outputCell = LagrangeInterpolation8000(efficiency) / 100;
                break;
            case 8650:
                outputCell = LagrangeInterpolation8650(efficiency) / 100;
                break;
            case 9000:
                outputCell = LagrangeInterpolation9000(efficiency) / 100;
                break;
            case 10000:
                outputCell = LagrangeInterpolation10000(efficiency) / 100;
                break;
        }
        
        turbineDataModel.GeneratorEfficiency = outputCell;
        // worksheet.Cells["O10"].Value = outputCell;
        Logger("Generator Efficiency is :" + outputCell*100 +"%");
    }
    

    private double LagrangeInterpolation1000(double x)
    {
        double x0 = 25, y0 = 90.23;
        double x1 = 50, y1 = 93.1;
        double x2 = 75, y2 = 93.22;
        double x3 = 100, y3 = 94.2;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation1500(double x)
    {
        double x0 = 25, y0 = 92.3;
        double x1 = 50, y1 = 93.33;
        double x2 = 75, y2 = 93.45;
        double x3 = 100, y3 = 94.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation2000(double x)
    {
        double x0 = 25, y0 = 92.1;
        double x1 = 50, y1 = 93.91;
        double x2 = 75, y2 = 94.02;
        double x3 = 100, y3 = 94.9;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation2500(double x)
    {
        double x0 = 25, y0 = 91.72;
        double x1 = 50, y1 = 94.14;
        double x2 = 75, y2 = 94.3;
        double x3 = 100, y3 = 95;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation3000(double x)
    {
        double x0 = 25, y0 = 92.3;
        double x1 = 50, y1 = 94.5;
        double x2 = 75, y2 = 94.6;
        double x3 = 100, y3 = 95.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation3400(double x)
    {
        double x0 = 25, y0 = 93;
        double x1 = 50, y1 = 95.1;
        double x2 = 75, y2 = 95.2;
        double x3 = 100, y3 = 95.8;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation4000(double x)
    {
        double x0 = 25, y0 = 93.1;
        double x1 = 50, y1 = 95.6;
        double x2 = 75, y2 = 96;
        double x3 = 100, y3 = 96.1;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation4500(double x)
    {
        double x0 = 25, y0 = 93.7;
        double x1 = 50, y1 = 96.1;
        double x2 = 75, y2 = 96.3;
        double x3 = 100, y3 = 96.4;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation5000(double x)
    {
        double x0 = 25, y0 = 93.7;
        double x1 = 50, y1 = 96.1;
        double x2 = 75, y2 = 96.4;
        double x3 = 100, y3 = 96.5;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation6000(double x)
    {
        double x0 = 25, y0 = 93.5;
        double x1 = 50, y1 = 96;
        double x2 = 75, y2 = 96.5;
        double x3 = 100, y3 = 96.7;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation7000(double x)
    {
        double x0 = 25, y0 = 94.2;
        double x1 = 50, y1 = 96.4;
        double x2 = 75, y2 = 96.9;
        double x3 = 100, y3 = 97;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));
        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }
    private double LagrangeInterpolation7500(double x)
    {
        double x0 = 25, y0 = 93.8;
        double x1 = 50, y1 = 96.4;
        double x2 = 75, y2 = 97;
        double x3 = 100, y3 = 97.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation8000(double x)
    {
        double x0 = 25, y0 = 93.8;
        double x1 = 50, y1 = 96.4;
        double x2 = 75, y2 = 97;
        double x3 = 100, y3 = 97.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation8650(double x)
    {
        double x0 = 25, y0 = 94.5;
        double x1 = 50, y1 = 96.7;
        double x2 = 75, y2 = 97.2;
        double x3 = 100, y3 = 97.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation9000(double x)
    {
        double x0 = 25, y0 = 94.5;
        double x1 = 50, y1 = 96.7;
        double x2 = 75, y2 = 97.2;
        double x3 = 100, y3 = 97.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }

    private double LagrangeInterpolation10000(double x)
    {
        double x0 = 25, y0 = 94.3;
        double x1 = 50, y1 = 96.3;
        double x2 = 75, y2 = 96.9;
        double x3 = 100, y3 = 97.3;

        double L0 = ((x - x1) * (x - x2) * (x - x3)) / ((x0 - x1) * (x0 - x2) * (x0 - x3));
        double L1 = ((x - x0) * (x - x2) * (x - x3)) / ((x1 - x0) * (x1 - x2) * (x1 - x3));
        double L2 = ((x - x0) * (x - x1) * (x - x3)) / ((x2 - x0) * (x2 - x1) * (x2 - x3));
        double L3 = ((x - x0) * (x - x1) * (x - x2)) / ((x3 - x0) * (x3 - x1) * (x3 - x2));

        return y0 * L0 + y1 * L1 + y2 * L2 + y3 * L3;
    }
}
