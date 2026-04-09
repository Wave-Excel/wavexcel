// using DocumentFormat.OpenXml.Packaging;
using OfficeOpenXml;
using System;
using System.IO.Packaging;
// using HMBD_Interface;
// using TurbineUtils;
// using HMBD_GeneratorEfficiency;
using HMBD.HMBDInformation;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using HMBD.HMBD_GeneratorEfficiency;
using Interfaces.IThermodynamicLibrary;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Models.PreFeasibility;
using Models.LoadPointDataModel;
using ExtraLoadPoints;
using System.Diagnostics;
using Ignite_X.src.core.Handlers;
using StartKreislExecution;
using Kreisl.KreislConfig;
using Ignite_X.src.core.Services;
using System.Runtime.Intrinsics.Arm;
using DocumentFormat.OpenXml.Spreadsheet;

namespace HMBD.LoadPointGenerator
{
    class LoadPointGen
    {
        private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
        private double HBD_O6 = 87.02;

        private IConfiguration configuration;
        private IThermodynamicLibrary thermodynamicService;
        private ILogger logger;

        private TurbineDataModel turbineDataModel;
        private LoadPointDataModel lpDataModel;
        private PreFeasibilityDataModel preFeasibilityDataModel;

        private int maxLoadPoints = 10;
        private double genEfficiency;

        private string mainTemp = "";
        public LoadPointGen()
        {
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
            logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
            turbineDataModel = TurbineDataModel.getInstance();
            preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
            lpDataModel = LoadPointDataModel.getInstance();
        }


        public void GenerateLoadPoints(int extraLPs = 0)
        {
            KreislIntegration kreislIntegration = new KreislIntegration();
            kreislIntegration.RenameTurbaCON();
            // double efficiency = (double)powerCalc.Cells["O6"].Value;
            double massAX13 = 0;
            // Console.WriteLine(efficiency);
            genEfficiency = turbineDataModel.GeneratorEfficiency / 100.000;
            Logger("Load point calculation started..");
            mainTemp = File.ReadAllText("C:\\testDir\\KREISL.DAT");
            List<LoadPoint> lpList = lpDataModel.LoadPoints;
            lpList[1].Pressure = turbineDataModel.InletPressure;
            lpList[1].Temp = turbineDataModel.InletTemperature;
            lpList[1].MassFlow = turbineDataModel.MassFlowRate;
            lpList[1].BackPress = turbineDataModel.ExhaustPressure;
            lpList[1].Rpm = 12000;
            lpList[1].InFlow = 0.0;
            lpList[1].BYP = 0.0;
            lpList[1].EIN = 0;
            lpList[1].WANZ = 1;
            lpList[1].RSMIN = 0;
            lpList[1].LPName = "Base Load Case";


            lpList[2].Pressure = turbineDataModel.InletPressure;
            lpList[2].Temp = turbineDataModel.InletTemperature;
            lpList[2].MassFlow = turbineDataModel.MassFlowRate;
            lpList[2].BackPress = 1.1 * turbineDataModel.ExhaustPressure;
            lpList[2].Rpm = 12000;
            lpList[2].InFlow = 0;
            lpList[2].BYP = -1;
            lpList[2].EIN = 0;
            lpList[2].WANZ = 0;
            lpList[2].RSMIN = 0;
            lpList[2].LPName = "10% Higher Backpressure";



            lpList[3].Pressure = turbineDataModel.InletPressure;
            lpList[3].Temp = turbineDataModel.InletTemperature;
            lpList[3].MassFlow = 0.85 * turbineDataModel.MassFlowRate;
            lpList[3].BackPress = 0.8 * turbineDataModel.ExhaustPressure;
            lpList[3].Rpm = 12000;
            lpList[3].InFlow = 0;
            lpList[3].BYP = -1;
            lpList[3].EIN = 0;
            lpList[3].WANZ = 0;
            lpList[3].RSMIN = 0;
            lpList[3].LPName = "20% Lower Backpressure";


            lpList[4].Pressure = turbineDataModel.InletPressure;
            lpList[4].Temp = turbineDataModel.InletTemperature - 30;
            lpList[4].MassFlow = turbineDataModel.MassFlowRate;
            lpList[4].BackPress = turbineDataModel.ExhaustPressure;
            lpList[4].Rpm = 12000;
            lpList[4].InFlow = 0;
            lpList[4].BYP = -1;
            lpList[4].EIN = 0;
            lpList[4].WANZ = 0;
            lpList[4].RSMIN = 0;
            lpList[4].LPName = "Temp -30 Degrees";



            lpList[5].Pressure = turbineDataModel.InletPressure;
            lpList[5].Temp = turbineDataModel.InletTemperature - 30;
            lpList[5].MassFlow = turbineDataModel.MassFlowRate;
            lpList[5].BackPress = 0.8 * turbineDataModel.ExhaustPressure;
            lpList[5].Rpm = 12000;
            lpList[5].InFlow = 0;
            lpList[5].BYP = -1;
            lpList[5].EIN = 0;
            lpList[5].WANZ = 0;
            lpList[5].RSMIN = 0;
            lpList[5].LPName = "Temp -30 Degrees and -20% Backpressure";


            lpList[6].Pressure = turbineDataModel.InletPressure;
            lpList[6].Temp = turbineDataModel.InletTemperature; ;
            lpList[6].MassFlow = getLP6Multipler() * turbineDataModel.MassFlowRate;
            lpList[6].BackPress = turbineDataModel.ExhaustPressure;
            lpList[6].Rpm = 12000;
            lpList[6].InFlow = 0;
            lpList[6].BYP = 0;
            lpList[6].EIN = 0;
            lpList[6].WANZ = 0;
            lpList[6].RSMIN = 0;
            lpList[6].LPName = "Valve Point";
            //return;
            //7,8 -> 30% massflow

            // Loadcase 7: MCR
           HBD_O6 = 60;//powerCalc.Cells["O6"].Value = 60;
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
            updateMainTemplate("0.600", TurbineDataModel.getInstance().ExhaustPressure, 12000, preFeasibilityDataModel.PowerActualValue * 0.100);
            //kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "60");
            kreislIntegration.LaunchKreisL();
            KreislERGHandlerService eRGHandlerService = new KreislERGHandlerService();

            massAX13 = eRGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);
            //massAX13 = thermodynamicService.GetMassFlowUsingKreislPower(preFeasibilityDataModel.PowerActualValue * 0.200, turbineDataModel.MassFlowRate * 0.10, turbineDataModel.MassFlowRate * 0.80);// 0.3000 * turbineDataModel.MassFlowRate;// GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, preFeasibilityDataModel.PowerActualValue * 0.2000);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);

            lpList[7].Pressure = turbineDataModel.InletPressure;
            lpList[7].Temp = turbineDataModel.InletTemperature; ;
            lpList[7].MassFlow = massAX13;//0.6 * turbineDataModel.MassFlowRate;
            lpList[7].BackPress = turbineDataModel.ExhaustPressure;
            lpList[7].Rpm = 12000;
            lpList[7].InFlow = 0;
            lpList[7].BYP = -1;
            lpList[7].EIN = 0;
            lpList[7].WANZ = 0;
            lpList[7].RSMIN = 0;
            lpList[7].LPName = "MCR- Minimum Continuous Rating";



            // Loadcase 8: MCR with - 30 Temp
            HBD_O6 = 60;//powerCalc.Cells["O6"].Value = 60;
            kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "60");
            lpList[8].Pressure = turbineDataModel.InletPressure;
            lpList[8].Temp = turbineDataModel.InletTemperature - 30;
            lpList[8].MassFlow = massAX13;//0.6 * turbineDataModel.MassFlowRate;
            lpList[8].BackPress = turbineDataModel.ExhaustPressure;
            lpList[8].Rpm = 12000;
            lpList[8].InFlow = 0;
            lpList[8].BYP = -1;
            lpList[8].EIN = 0;
            lpList[8].WANZ = 0;
            lpList[8].RSMIN = 0;
            lpList[8].LPName = "MCR with Temp -30 Degrees";
            double prevLP_MassFlow = massAX13;

            

            HBD_O6 = 30;
            File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);
            //kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "30");

            updateMainTemplate("0.300", TurbineDataModel.getInstance().ExhaustPressure, 12000);
            kreislIntegration.LaunchKreisL();
            //massAX13 = thermodynamicService.GetMassFlowUsingKreislPower(100.00, 0.001, turbineDataModel.MassFlowRate);// GoalSeekMass(turbineDataModel.ExhaustPressure, turbineDataModel.InletPressure,turbineDataModel.InletTemperature, 100.00, prevLP_MassFlow);
            massAX13 = eRGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);

            lpList[9].Pressure = turbineDataModel.InletPressure;
            lpList[9].Temp = turbineDataModel.InletTemperature;
            lpList[9].MassFlow = massAX13;//0.6 * turbineDataModel.MassFlowRate;
            lpList[9].BackPress = turbineDataModel.ExhaustPressure;
            lpList[9].Rpm = 12000;
            lpList[9].InFlow = 0;
            lpList[9].BYP = -1;
            lpList[9].EIN = 0;
            lpList[9].WANZ = 0;
            lpList[9].RSMIN = 0;
            lpList[9].LPName = "No Load (100 kW)";
            prevLP_MassFlow = massAX13;

            File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);

            // Loadcase 10: No Load (100 kW) and 1.013 Backpressure
            HBD_O6 = 40;
            updateMainTemplate("0.400", 1.013,12000);
            // massAX13 = GoalSeekMass(1.013, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 100.00, prevLP_MassFlow);
            //KreislDATHandler kreislDATHandler = new KreislDATHandler();
            //kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", "40");
            //kreislDATHandler.FillExhaustPressure(StartKreisl.filePath, 4, "1.013");


            //massAX13 = eRGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath); ;// GoalSeekMass(1.013, turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 100.00);
            kreislIntegration.LaunchKreisL();
            massAX13 = eRGHandlerService.ExtractMassFlowFromERGLP9(StartKreisl.ergFilePath);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", mainTemp);

            kreislDATHandler.FillExhaustPressure(StartKreisl.filePath, 4, turbineDataModel.ExhaustPressure.ToString());
            kreislDATHandler.FillMassFlow(StartKreisl.filePath, 5, turbineDataModel.MassFlowRate.ToString());
            //KreislIntegration kreislIntegration = new KreislIntegration();
            kreislIntegration.LaunchKreisL();

            lpList[10].Pressure = turbineDataModel.InletPressure;
            lpList[10].Temp = turbineDataModel.InletTemperature;
            lpList[10].MassFlow = massAX13; //0.6 * turbineDataModel.MassFlowRate;
            lpList[10].BackPress = 1.013; //turbineDataModel.ExhaustPressure;
            lpList[10].Rpm = 12000;
            lpList[10].InFlow = 0;
            lpList[10].BYP = -1;
            lpList[10].EIN = 0;
            lpList[10].WANZ = 0;
            lpList[10].RSMIN = 0;
            lpList[10].LPName = "No Load (100 kW) and 1.013 Backpressure";




            HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            //Use from HMBD_Interface.cs
            hBDPowerCalculator.HBDSetDefaultCustomerParams();

            Logger("Load points calculation completed...");

            string LPxMassFlow = "| ";
            string LPxMassTemp = "| ";
            string LPxMassBP = "| ";

            for (int lp = 1; lp <= maxLoadPoints; ++lp)
            {
                LPxMassFlow += lpList[lp].MassFlow + " | ";
                LPxMassTemp += lpList[lp].Temp + " | ";
                LPxMassBP += lpList[lp].BackPress + " | ";
            }

            for (int i = 2; i <= extraLPs; ++i)
            {
                lpList[9 + i].Pressure = CustomLoadPointHandler.extraLP[i].Pressure;
                lpList[9 + i].Temp = CustomLoadPointHandler.extraLP[i].Temp;
                lpList[9 + i].MassFlow = CustomLoadPointHandler.extraLP[i].MassFlow;
                lpList[9 + i].BackPress = CustomLoadPointHandler.extraLP[i].BackPress;
                lpList[9 + i].Rpm = CustomLoadPointHandler.extraLP[i].Rpm;
                lpList[9 + i].InFlow = CustomLoadPointHandler.extraLP[i].InFlow;
                lpList[9 + i].BYP = CustomLoadPointHandler.extraLP[i].BYP;
                lpList[9 + i].EIN = CustomLoadPointHandler.extraLP[i].EIN;
                lpList[9 + i].WANZ = CustomLoadPointHandler.extraLP[i].WANZ;
                lpList[9 + i].RSMIN = CustomLoadPointHandler.extraLP[i].RSMIN;
                lpList[9 + i].LPName = "Custom Load Case";
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

        public void updateMainTemplate(string eff , double Epres, double RPM , double power = 90)
        {
            string inputFilePath = "";
            KreislDATHandler datHandler = new KreislDATHandler();

            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    inputFilePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDumpLP9.DAT");
                }
                else if(!turbineDataModel.DumpCondensor)
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
                //datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
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
                datHandler.FillMassFlow(inputFilePath, 6, turbineDataModel.MassFlowRate.ToString(),-1);
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
                { "Pre", FormatWithDotZero(Math.Round(TurbineDataModel.getInstance().InletPressure,3)) },
                { "Temp","-"+FormatWithDotZero(TurbineDataModel.getInstance().InletTemperature) },
                { "RPM",FormatWithDotZero(RPM) },
                { "Flow", FormatWithDotZero(TurbineDataModel.getInstance().MassFlowRate) }
                };
                inputFilePath = Path.Combine(AppContext.BaseDirectory, "kreislLp9.DAT");
                datHandler.FillVariablePower(inputFilePath,6, power.ToString()) ;
                datHandler.UpdateRPM2(inputFilePath,6, "12000");
                string content = File.ReadAllText(inputFilePath);
                foreach (var kvp in replacements)
                {
                    content = content.Replace(kvp.Key, kvp.Value);
                }
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            }
            
            
            

        }
        public void LoadPointGenerator_IncMassFlow(int lpNumber, int perCentInc)
        {
            lpDataModel.LoadPoints[lpNumber].MassFlow *= (1 + (perCentInc / 100.00));
        }

        public void LoadPointGenerator_ReduceBP(int lpNumber, int perCentDec)
        {
            lpDataModel.LoadPoints[lpNumber].BackPress *= (1 - (perCentDec / 100.0));
        }

        public double GoalSeekMass(double backpressureValue, double pressureValue, double tempValue, double powerValue, double prevMassFlow = 0)
        {
            FileInfo existingFile = new FileInfo(excelPath);
            ExcelPackage package = new ExcelPackage(existingFile);
            var powerCalc = package.Workbook.Worksheets["OPEN_HBD"];
            var sheet1 = package.Workbook.Worksheets["Sheet1"];
            Debug.WriteLine("Generatorr Efficiency :" + turbineDataModel.GeneratorEfficiency + ", stored value: " + genEfficiency);
            double massinKg = 0;
            if (prevMassFlow > 0)
            {
                return thermodynamicService.getMassFlowFromPower(powerValue, pressureValue, backpressureValue, tempValue, prevMassFlow, HBD_O6, turbineDataModel.GeneratorEfficiency * 100.000);
            }
            return thermodynamicService.getMassFlowFromPower(powerValue, pressureValue, backpressureValue, tempValue, turbineDataModel.MassFlowRate, HBD_O6, turbineDataModel.GeneratorEfficiency * 100.000);
        }
        public double GetGeneratorEfficiency(double input)
        {

            GeneratorEfficiencyCalculator genCalc = new GeneratorEfficiencyCalculator();
            genCalc.GetGeneratorEfficiency(input);
            return turbineDataModel.GeneratorEfficiency;
        }
        void Logger(string message)
        {
            logger.LogInformation(message);
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
}