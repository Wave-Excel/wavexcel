namespace HMBD.Custom_LoadPointGenerator;
using System;
using System.IO;
using Models.LoadPointDataModel;
using Models.PreFeasibility;
using Models.TurbineData;
using OfficeOpenXml;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.IThermodynamicLibrary;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using HMBD.HMBD_GeneratorEfficiency;
using ExtraLoadPoints;

public class CustomLoadPointGenerator
{
    private ExcelPackage package;
    private ExcelWorksheet sourceSheet, destinationSheet, powerCalc, prefeasibility1;

    private double HBD_O6 = 87.02, massAX13 = 0.0;
    TurbineDataModel turbineDataModel;
    private IThermodynamicLibrary thermodynamicService;
    private IConfiguration configuration;
    private ILogger logger;
    private int maxLoadPoints = 10;
       
    LoadPointDataModel loadPointDataModel;
    PreFeasibilityDataModel preFeasibilityDataModel;
    public CustomLoadPointGenerator()
    {
         configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
         turbineDataModel = TurbineDataModel.getInstance();
         loadPointDataModel = LoadPointDataModel.getInstance();
         preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
         thermodynamicService =  CustomExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
         maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
         logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void GenerateLoadPoints(int maxlp = 0){
        // double efficiency = powerCalc.Cells["O6"].GetValue<double>();
        double datRPM = 12000;//RPM();

        Logger("Load point calculation started..");
        // Loadcase 1: Base Loadcase
        SetLoadPoint(1, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, 0, 0, 1, 0, "Base Load Case");


        // Loadcase 2: 10% Higher Backpressure
        SetLoadPoint(2, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, turbineDataModel.MassFlowRate, 1.1 * turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "10% Higher Backpressure");

        // Loadcase 3: -20% Lower Backpressure
        SetLoadPoint(3, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 0.85 * turbineDataModel.MassFlowRate, 0.8 * turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "20% Lower Backpressure");

        // Loadcase 4: -30 Degrees
        SetLoadPoint(4, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "Temp -30 Degrees");

        // Loadcase 5: -30 Degrees and -20% Backpressure
        SetLoadPoint(5, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, turbineDataModel.MassFlowRate, 0.8 * turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "Temp -30 Degrees and -20% Backpressure");

        // Loadcase 6: Valve Point
        SetLoadPoint(6, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 0.6 * turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, 0, 0, 0, 0, "Valve Point");

        // Loadcase 7: MCR
        HBD_O6 = 60;
        // double massAX13 = 0;
        // powerCalc.Cells["O6"].Value = 60;
        GetGeneratorEfficiency(20);
        

        // Console.WriteLine("PREFESIOIFLISLYJRKNGKJFNGDJKGNDKJGNK:"+ preFeasibilityDataModel.PowerActualValue);
        // Console.WriteLine("ajbdfojadbfja:"+thermodynamicService.getPowerFromTurbineEfficiencyWithoutUpdate(60));
        massAX13 = GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, preFeasibilityDataModel.PowerActualValue * 0.2000);
        // destinationSheet.Cells["B11"].Value = powerCalc.Cells["AX13"].GetValue<double>();
        Console.WriteLine("LLLLLLLLLLLLLLPPPPPPP77777 mass:"+massAX13);
        SetLoadPoint(7, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "MCR- Minimum Continuous Rating");

        // Loadcase 8: MCR with -30 Temp
        HBD_O6  = 60;
        GetGeneratorEfficiency(20);
        // destinationSheet.Cells["B12"].Value = destinationSheet.Cells["B11"].GetValue<double>();
        SetLoadPoint(8, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "MCR with Temp -30 Degrees");

        // Loadcase 9: No Load (100 kW)
        Console.WriteLine("*********************************************************gtrhtyhTHNTNTHN");
        HBD_O6 = 30;
        double noLoadTargetPower = 100 + GetGeneratorNoLoadLossPower(turbineDataModel.AK25);
        // GetGeneratorEfficiency(100.00 * (100.000 / preFeasibilityDataModel.PowerActualValue));
        massAX13 = GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure,turbineDataModel.InletTemperature, noLoadTargetPower);
      
        SetLoadPoint(9, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "No Load (100 kW)");
        // Console.WriteLine("*********************************************************gtrhtyhTHNTNTHN");
        // Loadcase 10: No Load (100 kW) and 1.013 Backpressure
        HBD_O6 = 40;

        massAX13 = GoalSeekMass(1.013, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, noLoadTargetPower);
        // destinationSheet.Cells["B14"].Value = powerCalc.Cells["AX13"].GetValue<double>();
        SetLoadPoint(10, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, 1.013, datRPM, 0, -1, 0, 0, 0, "No Load (100 kW) and 1.013 Backpressure");

        Logger("Load points calculation completed...");

        string LPxMassFlow = "| ";
        string LPxMassTemp = "| ";
        string LPxMassBP = "| ";
        for(int lp = 1;lp <= maxLoadPoints; ++lp){
              LPxMassFlow += loadPointDataModel.LoadPoints[lp].MassFlow + " | ";
              LPxMassTemp += loadPointDataModel.LoadPoints[lp].Temp + " | ";
              LPxMassBP += loadPointDataModel.LoadPoints[lp].BackPress + " | ";
        }
        for (int i = 2; i <= maxlp - 10; ++i)
        {
            loadPointDataModel.LoadPoints[9 + i].Pressure = CustomLoadPointHandler.extraLP[i].Pressure;
            loadPointDataModel.LoadPoints[9 + i].Temp = CustomLoadPointHandler.extraLP[i].Temp;
            loadPointDataModel.LoadPoints[9 + i].MassFlow = CustomLoadPointHandler.extraLP[i].MassFlow;
            loadPointDataModel.LoadPoints[9 + i].BackPress = CustomLoadPointHandler.extraLP[i].BackPress;
            loadPointDataModel.LoadPoints[9 + i].Rpm = CustomLoadPointHandler.extraLP[i].Rpm;
            loadPointDataModel.LoadPoints[9 + i].InFlow = CustomLoadPointHandler.extraLP[i].InFlow;
            loadPointDataModel.LoadPoints[9 + i].BYP = CustomLoadPointHandler.extraLP[i].BYP;
            loadPointDataModel.LoadPoints[9 + i].EIN = CustomLoadPointHandler.extraLP[i].EIN;
            loadPointDataModel.LoadPoints[9 + i].WANZ = CustomLoadPointHandler.extraLP[i].WANZ;
            loadPointDataModel.LoadPoints[9 + i].RSMIN = CustomLoadPointHandler.extraLP[i].RSMIN;
            loadPointDataModel.LoadPoints[9 + i].LPName = "Custom Load Case";
        }
        Logger("Mass Flow: " + LPxMassFlow);
        Logger("Temperature: " + LPxMassTemp);
        Logger("Back Pressure: " + LPxMassBP);
    }
    public void GenerateLoadPoints_Throttle()
    {
        // double efficiency = powerCalc.Cells["O6"].GetValue<double>();
        double datRPM = 12000;

        Logger("Load point calculation started..");
        // Console.WriteLine("RRRRRRRRRRRRRRRRRRRRRRRRRRRRRPPPPPPPPPPPPPPPP:"+datRPM);
        // Loadcase 1: Base Loadcase
        SetLoadPoint(1, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 1, 0, "Base Load Case");


        // Loadcase 2: 10% Higher Backpressure
        SetLoadPoint(2, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, turbineDataModel.MassFlowRate, 1.1 * turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "10% Higher Backpressure");

        // Loadcase 3: -20% Lower Backpressure
        SetLoadPoint(3, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 0.85 * turbineDataModel.MassFlowRate, 0.8 * turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "20% Lower Backpressure");

        // Loadcase 4: -30 Degrees
        SetLoadPoint(4, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "Temp -30 Degrees");

        // Loadcase 5: -30 Degrees and -20% Backpressure
        SetLoadPoint(5, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, turbineDataModel.MassFlowRate, 0.8 * turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "Temp -30 Degrees and -20% Backpressure");

        // Loadcase 6: Valve Point
        SetLoadPoint(6, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, turbineDataModel.MassFlowRate, 1.1 * turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "Valve Point Dummy");

        // Loadcase 7: MCR
        HBD_O6 = 60;
        // double massAX13 = 0;
        // powerCalc.Cells["O6"].Value = 60;
        GetGeneratorEfficiency(20);

        // Console.WriteLine("PREFESIOIFLISLYJRKNGKJFNGDJKGNDKJGNK:"+ preFeasibilityDataModel.PowerActualValue);
        // Console.WriteLine("ajbdfojadbfja:"+thermodynamicService.getPowerFromTurbineEfficiencyWithoutUpdate(60));
        massAX13 = GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, preFeasibilityDataModel.PowerActualValue * 0.2000);
        // destinationSheet.Cells["B11"].Value = powerCalc.Cells["AX13"].GetValue<double>();
        Console.WriteLine("LLLLLLLLLLLLLLPPPPPPP77777 mass:"+massAX13);
        SetLoadPoint(7, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "MCR- Minimum Continuous Rating");

        // Loadcase 8: MCR with -30 Temp
        HBD_O6  = 60;
        GetGeneratorEfficiency(20);
        // destinationSheet.Cells["B12"].Value = destinationSheet.Cells["B11"].GetValue<double>();
        SetLoadPoint(8, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "MCR with Temp -30 Degrees");

        // Loadcase 9: No Load (100 kW)
        Console.WriteLine("*********************************************************gtrhtyhTHNTNTHN");
        HBD_O6 = 30;
        double noLoadTargetPower = 100;// + GetGeneratorNoLoadLossPower(turbineDataModel.AK25);
        GetGeneratorEfficiency(100.00 * (100.000 / preFeasibilityDataModel.PowerActualValue));
        massAX13 = GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure,turbineDataModel.InletTemperature, noLoadTargetPower);
      
        SetLoadPoint(9, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, 1, 0, 0, 0, "No Load (100 kW)");
        // Console.WriteLine("*********************************************************gtrhtyhTHNTNTHN");
        // Loadcase 10: No Load (100 kW) and 1.013 Backpressure
        HBD_O6 = 40;
        GetGeneratorEfficiency(100.00 * (100.000 / preFeasibilityDataModel.PowerActualValue));
        massAX13 = GoalSeekMass(1.013, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, noLoadTargetPower);
        // destinationSheet.Cells["B14"].Value = powerCalc.Cells["AX13"].GetValue<double>();
        
        SetLoadPoint(10, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, 1.013, datRPM, 0, 1, 0, 0, 0, "No Load (100 kW) and 1.013 Backpressure");

        Logger("Load points calculation completed...");

        string LPxMassFlow = "| ";
        string LPxMassTemp = "| ";
        string LPxMassBP = "| ";
        for(int lp = 1;lp <= maxLoadPoints; ++lp){
              LPxMassFlow += loadPointDataModel.LoadPoints[lp].MassFlow + " | ";
              LPxMassTemp += loadPointDataModel.LoadPoints[lp].Temp + " | ";
              LPxMassBP += loadPointDataModel.LoadPoints[lp].BackPress + " | ";
        }
        Logger("Mass Flow: " + LPxMassFlow);
        Logger("Temperature: " + LPxMassTemp);
        Logger("Back Pressure: " + LPxMassBP);
    }

    public void SetLoadPoint(int row,double pressure, double temperature, double steamMassflow, double backpressure, double rpm, double f, double h, double i, double j, double k, string description)
    {
       List<LoadPoint> lpList = loadPointDataModel.LoadPoints;
       lpList[row].Pressure=pressure;
       lpList[row].Temp=temperature;
       lpList[row].MassFlow=steamMassflow;
       lpList[row].BackPress=backpressure;
       lpList[row].Rpm=rpm;
       lpList[row].InFlow = f;
       lpList[row].BYP = h;
       lpList[row].EIN = i;
       lpList[row].WANZ = j;
       lpList[row].RSMIN = k;
       lpList[row].LPName = description;
    }

    public double GetGeneratorNoLoadLossPower(double ratedPower_kW)
    {
        Logger("Searching Generator No load loss power ...");
        int rowIndex = 0;
        FileInfo fileInfo = new FileInfo("RunTurbaCycle_V5.9.xlsm");
        ExcelPackage package = new ExcelPackage(fileInfo);
        ExcelWorksheet sheetDatFileParams = package.Workbook.Worksheets["DAT_FILE_PARAMS"];
        for (int row = 48; row <= 57; ++row)
        {
            if (Convert.ToDouble(sheetDatFileParams.Cells[row, 3].Value) < ratedPower_kW)
            {
                rowIndex = row;
            }
            else
            {
                break;
            }
        }

        double genLoss = Convert.ToDouble(sheetDatFileParams.Cells[rowIndex + 1, 4].Value);

        if (genLoss <= 0)
        {
            genLoss = 48;
        }

        Logger($"Generator Loss Power kW  : {genLoss}");
        return genLoss;
    }
    public void LoadPointGenerator_IncMassFlow(int lpNumber, int perCentInc)
    {
        loadPointDataModel.LoadPoints[lpNumber].MassFlow *= (1 + (perCentInc / 100.00));
    }

    public void LoadPointGenerator_ReduceBP(int lpNumber, int perCentDec)
    {
      loadPointDataModel.LoadPoints[lpNumber].BackPress *= (1 - (perCentDec / 100.0));
    }

    public double GoalSeekMass(double backpressureValue, double pressureValue, double tempValue, double powerValue)
    {
        backpressureValue /= 0.980665;
        pressureValue /= 0.980665;
            // Console.WriteLine("WAVEXCEL EFFICIENCY:::"+HBD_O6+", O10:"+Convert.ToDouble(powerCalc.Cells["O10"].Value));
            Console.WriteLine("Generatorr Efficiency :"+ turbineDataModel.GeneratorEfficiency);
            double massinKg = thermodynamicService.getMassFlowFromPower(powerValue, pressureValue, backpressureValue, tempValue, massAX13, HBD_O6, turbineDataModel.GeneratorEfficiency * 100.000);
            return massinKg;
    }

    public double RPM()
    {
        // ExcelWorksheet datData = package.Workbook.Worksheets["DAT_DATA"];
        string [] fileLines = turbineDataModel.DAT_DATA.ToArray<string>();
        string parameter = "!LP1";
        int parameterPos = 5;
        int parameterLen = parameter.Length;

        int ergEoF = fileLines.Length;

        for (int lineNumber = 0; lineNumber < ergEoF; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                line = fileLines[lineNumber + 1];//datData.Cells[lineNumber + 1, 2].GetValue<string>();
                string paramVal = ExtractSecondValue(line);
                return double.Parse(paramVal);
            }
        }

        return 0;
    }

    public string ExtractSecondValue(string stringToRead)
    {
        string[] values = stringToRead.Split(' ');
        int decimalCount = 0;

        foreach (string value in values)
        {
            if (value.Contains("."))
            {
                decimalCount++;
            }

            if (decimalCount == 2)
            {
                return value;
            }
        }

        return string.Empty;
    }

    private void Logger(string message)
    {
        // Implement your logging mechanism here
        // Console.WriteLine(message);
        logger.LogInformation(message);
    }

    public double GetGeneratorEfficiency(double input){
            
            // powerCalc.Cells["O10"].Value = 0.958;
            GeneratorEfficiencyCalculator genCalc = new GeneratorEfficiencyCalculator();
            genCalc.GetGeneratorEfficiency(input);
            // var powerCalc = package.Workbook.Worksheets["OPEN_HBD"];
            // Console.WriteLine("Dummy Check: "+powerCalc.Cells["O10"].Value);
            return turbineDataModel.GeneratorEfficiency;//Convert.ToDouble(powerCalc.Cells["O10"].Value);
            // return 95.8;
    }

}