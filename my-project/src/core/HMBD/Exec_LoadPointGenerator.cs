namespace HMBD.Exec_LoadPointGenerator;
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
using Ignite_X.src.core.Handlers;
using StartKreislExecution;
using Kreisl.KreislConfig;
using System.Diagnostics;
using Ignite_X.src.core.Services;
using Models.AdditionalLoadPointModel;
using ExtraLoadPoints;

public class ExecLoadPointGenerator
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
    private string mainTemp = "";
    public ExecLoadPointGenerator()
    {
         configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
         turbineDataModel = TurbineDataModel.getInstance();
         loadPointDataModel = LoadPointDataModel.getInstance();
         preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
         thermodynamicService =  MainExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
         maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
         logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void GenerateLoadPoints(int maxLp = 0)
    {
        // double efficiency = powerCalc.Cells["O6"].GetValue<double>();
        double initialGenEff = turbineDataModel.GeneratorEfficiency;
        double initialPower = turbineDataModel.AK25;
        if (File.Exists(@"C:\testDir\TURBA.CON"))
        {
            File.Delete(@"C:\testDir\TURBA.CON");
        }
        double datRPM = RPM();
        
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        if(StartKreisl.kreislKey)
        {
            kreislDATHandler.DatFileInitParamsExceptLPKriesl();
            kreislDATHandler.UpdateRPM(StartKreisl.filePath,6,datRPM.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath,6,datRPM.ToString());

        }
        mainTemp = File.ReadAllText("C:\\testDir\\KREISL.DAT");
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
        SetLoadPoint(6, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, getLP6Multipler() * turbineDataModel.MassFlowRate, turbineDataModel.ExhaustPressure, datRPM, 0, 0, 0, 0, 0, "Valve Point");

        // Loadcase 7: MCR
        HBD_O6 = 60;
        GetGeneratorEfficiency(20);
        KreislIntegration kreislIntegration = new KreislIntegration();
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();

        //kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "60");
        //kreislDATHandler.FillExhaustPressure(StartKreisl.filePath, 4, "1.013");
        //massAX13 = thermodynamicService.GetMassFlowUsingKreislPower(preFeasibilityDataModel.PowerActualValue * 0.280, turbineDataModel.MassFlowRate * 0.15, turbineDataModel.MassFlowRate * 0.60);// 0.3000 * turbineDataModel.MassFlowRate;// GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, preFeasibilityDataModel.PowerActualValue * 0.2000);
        File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
        updateMainTemplate("0.600", TurbineDataModel.getInstance().ExhaustPressure, 12000, preFeasibilityDataModel.PowerActualValue * 0.200);
        kreislIntegration.LaunchKreisL();
        massAX13 = kreislERGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);
        File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
        SetLoadPoint(7, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "MCR- Minimum Continuous Rating");

        // Loadcase 8: MCR with -30 Temp
        HBD_O6  = 60;
        kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "60");
        GetGeneratorEfficiency(20);
        // destinationSheet.Cells["B12"].Value = destinationSheet.Cells["B11"].GetValue<double>();
        SetLoadPoint(8, turbineDataModel.InletPressure, turbineDataModel.InletTemperature - 30, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "MCR with Temp -30 Degrees");

        // Loadcase 9: No Load (100 kW)
        HBD_O6 = 30;
        double noLoadTargetPower = 100;
        File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
        updateMainTemplate("0.300", TurbineDataModel.getInstance().ExhaustPressure, datRPM);
        kreislIntegration.LaunchKreisL();
        massAX13 = kreislERGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);

        SetLoadPoint(9, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, turbineDataModel.ExhaustPressure, datRPM, 0, -1, 0, 0, 0, "No Load (100 kW)");

        // Loadcase 10: No Load (100 kW) and 1.013 Backpressure
        File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);

        HBD_O6 = 40;
        updateMainTemplate("0.400", 1.013, datRPM);
        kreislIntegration.LaunchKreisL();
        massAX13 = kreislERGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);
        File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
        //massAX13 = GoalSeekMass(1.013, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, noLoadTargetPower);
        // destinationSheet.Cells["B14"].Value = powerCalc.Cells["AX13"].GetValue<double>();
        SetLoadPoint(10, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, massAX13, 1.013, datRPM, 0, -1, 0, 0, 0, "No Load (100 kW) and 1.013 Backpressure");

        kreislDATHandler.UpdateGeneratorEff(initialGenEff/100.000);
        kreislDATHandler.FillExhaustPressure(StartKreisl.filePath, 4, turbineDataModel.ExhaustPressure.ToString());
        kreislDATHandler.FillMassFlow(StartKreisl.filePath, 5, turbineDataModel.MassFlowRate.ToString());
        kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, datRPM.ToString());
        //KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
        Logger("Load points calculation completed...");

        string LPxMassFlow = "| ";
        string LPxMassTemp = "| ";
        string LPxMassBP = "| ";
        for(int lp = 1;lp <= maxLoadPoints; ++lp){
              LPxMassFlow += loadPointDataModel.LoadPoints[lp].MassFlow + " | ";
              LPxMassTemp += loadPointDataModel.LoadPoints[lp].Temp + " | ";
              LPxMassBP += loadPointDataModel.LoadPoints[lp].BackPress + " | ";
        }
        for (int i = 2; i <= maxLp-10; ++i)
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
    string FormatWithDotZero(double value)
    {
        string str = value.ToString();
        return (str.Contains(".")) ? str : str + ".0";
    }
    public void updateMainTemplate(string eff, double Epres ,double RPM , double power = 90)
    {
        string inputFilePath = "";
        KreislDATHandler datHandler = new KreislDATHandler();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor)
            {
                inputFilePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDumpLP9.DAT");
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                inputFilePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVLP9.DAT");
            }
            datHandler.FillTurbineEff(inputFilePath, "7", eff);
            datHandler.fillProcessSteamTemperatur(inputFilePath, 16, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(inputFilePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.UpdateRPM2(inputFilePath, 1, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
            datHandler.FillMassFlow(inputFilePath, 18, "1.012", -1);
            datHandler.FillInletPressure(inputFilePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillVariablePower(inputFilePath, 7, power.ToString());
            datHandler.FillExhaustPressure(inputFilePath, 2, Epres.ToString());
            datHandler.FillInletTemperature(inputFilePath, 6, turbineDataModel.InletTemperature.ToString());
            datHandler.MakeUpTemperature(inputFilePath, 9, turbineDataModel.MakeUpTempe.ToString());
            datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
            datHandler.FillCondensateReturn(inputFilePath, "14", turbineDataModel.ProcessCondReturn.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                datHandler.fillPsatvont_t(inputFilePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
            }
            else
            {
                if (turbineDataModel.DumpCondensor)
                {
                    datHandler.UpdateTemplatePRVToWPRVInDumpCondensor(inputFilePath);

                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    datHandler.UpdateTemplatePRVToWPRV(inputFilePath);

                }
            }
            string content = File.ReadAllText(inputFilePath);
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }
        else if (turbineDataModel.PST > 0)
        {
            inputFilePath = Path.Combine(AppContext.BaseDirectory, "KreislLp9D.DAT");

            datHandler.FillTurbineEff(inputFilePath, "1", eff);
            datHandler.FillWheelChamberEff(inputFilePath, "2", eff);
            datHandler.fillProcessSteamTemperatur(inputFilePath, 3, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(inputFilePath, 8, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.UpdateRPM2(inputFilePath, 7, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
            datHandler.FillMassFlow(inputFilePath, 6, turbineDataModel.MassFlowRate.ToString(), -1);
            datHandler.FillInletPressure(inputFilePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillVariablePower(inputFilePath, 7, power.ToString());
            datHandler.FillExhaustPressure(inputFilePath, 2, Epres.ToString());
            datHandler.FillInletTemperature(inputFilePath, 6, turbineDataModel.InletTemperature.ToString());
            string content = File.ReadAllText(inputFilePath);
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }
        else
        {
            var replacements = new Dictionary<string, string>()
                {
                { "Eff", eff },
                { "EPres", FormatWithDotZero(Epres) },
                { "Pre", FormatWithDotZero(TurbineDataModel.getInstance().InletPressure) },
                { "Temp","-"+FormatWithDotZero(TurbineDataModel.getInstance().InletTemperature) },
                { "RPM",FormatWithDotZero(RPM) },
                { "Flow", FormatWithDotZero(TurbineDataModel.getInstance().MassFlowRate-4) }
                };
            inputFilePath = Path.Combine(AppContext.BaseDirectory, "kreislLp9.DAT");
            datHandler.FillVariablePower(inputFilePath, 6, power.ToString());
            datHandler.UpdateRPM2(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
            string content = File.ReadAllText(inputFilePath);
            foreach (var kvp in replacements)
            {
                content = content.Replace(kvp.Key, kvp.Value);
            }
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }


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

    public void LoadPointGenerator_IncMassFlow(int lpNumber, int perCentInc)
    {
        // Logger("Massflow " + lpNumber + " " + destinationSheet.Cells[lpNumber].GetValue<double>());
        // destinationSheet.Cells[LPxCell].Value = destinationSheet.Cells[LPxCell].GetValue<double>() * (1 + (perCentInc / 100.0));
        loadPointDataModel.LoadPoints[lpNumber].MassFlow *= (1 + (perCentInc / 100.00));
        // Logger("Massflow " + lpNumber + " " + destinationSheet.Cells[lpNumber].GetValue<double>());
    }

    public void LoadPointGenerator_ReduceBP(int lpNumber, int perCentDec)
    {
      loadPointDataModel.LoadPoints[lpNumber].BackPress *= (1 - (perCentDec / 100.0));
        // Logger("Back Pressure " + LPxCell + " " + destinationSheet.Cells[LPxCell].GetValue<double>());
        // destinationSheet.Cells[LPxCell].Value = destinationSheet.Cells[LPxCell].GetValue<double>() * (1 - (perCentDec / 100.0));
        // Logger("Back Pressure " + LPxCell + " " + destinationSheet.Cells[LPxCell].GetValue<double>());
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
        List<string> fileLines = turbineDataModel.DAT_DATA;
        string parameter = "!LP1";
        int parameterPos = 5;
        int parameterLen = parameter.Length;

        int ergEoF = fileLines.Count;

        for (int lineNumber = 0; lineNumber < ergEoF; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                line = fileLines[lineNumber+1];//datData.Cells[lineNumber + 1, 2].GetValue<string>();
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
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            kreislDATHandler.UpdateGeneratorEff(turbineDataModel.GeneratorEfficiency);
            return turbineDataModel.GeneratorEfficiency;//Convert.ToDouble(powerCalc.Cells["O10"].Value);
            // return 95.8;
    }
    public double getLP6Multipler()
    {
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {

                var values = line.Split(',');



                if (values[0] == "LP6 mass flow factor multiplicator")
                {
                    Debug.WriteLine("LP 6 MULti " + values[1]);
                    return Convert.ToDouble(values[1]);

                    //Debug.WriteLine("ABWEICHUNG LOWER" + Convert.ToDouble(values[1]));
                    //abweichung_LowerLimit = Convert.ToDouble(values[1]);
                }
            }
        }
        return -1;
    }

}