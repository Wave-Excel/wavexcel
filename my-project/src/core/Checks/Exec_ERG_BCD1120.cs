using System;
using System.Runtime.InteropServices;
// using Excel = Microsoft.Office.Interop.Excel;
using Services.ThermodynamicService;
using System;
using System.Collections.Generic;
using OfficeOpenXml;
using StartExecutionMain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Interfaces.ILogger;
using Turba.TurbaConfiguration;
using Models.TurbaOutputDataModel;
using Optimizers.ERG_NozzleOptimizer;
using HMBD.LoadPointGenerator;
using Handlers.DAT_Handler;
using Interfaces.IThermodynamicLibrary;
using Models.PreFeasibility;
using Optimizers.Exec_ERG_NozzleOptimizer;
using HMBD.Exec_LoadPointGenerator;
using Handlers.Exec_DAT_Handler;
using Turba.Exec_TurbaConfig;
using StartExecutionMain;
using Ignite_x_wavexcel;

namespace Checks.ERG_BCD1120
{
    class ERG_BCD1120
    {
        public static string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
        private IConfiguration configuration;
        private TurbaOutputModel turbaOutputModel;
        private PreFeasibilityDataModel preFeasibilityModel;
        private ILogger logger;
        private TurbineDataModel turbineDataModel;
        int maxLoadPoints = 10;
        public static bool isCheckingLP5 = false;
        public ERG_BCD1120(){
            turbaOutputModel = TurbaOutputModel.getInstance();
            turbineDataModel= TurbineDataModel.getInstance();
            preFeasibilityModel = PreFeasibilityDataModel.getInstance();
            logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        }
        public void ErgResultsCheckBCD1120(int maxlp=0)
        {
            if (ErgCheckExhaustVolumetricFlowBCD1120(maxlp))
            {
                if (ErgCheckNozzlesSectionBCD1120(maxlp))
                {
                    if (ErgCheckThrustValue1120(maxlp))
                    {
                        if (ErgCheckDetaTGBCWheelChamberPTBendingBCD1120(maxlp))
                        {
                            if (ErgResultsCheckBCD1120New(maxlp))
                            {
                                Console.WriteLine("Turbine looks great !! Let's Compare ex_Power_KNN.Power With HBD..");
                                Logger("Comparing ex_Power_KNN.Power With HBD..");
                            }
                        }
                    }
                }
            }
            
        }
        public bool ErgCheckNozzlesSectionBCD1120(int maxlp = 0)
        {
            ExecutedNozzleOptimizer nozzleOptimizer = new ExecutedNozzleOptimizer();
            // NozzleOptimizer nozzleOptimizer = new NozzleOptimizer();
            if (nozzleOptimizer.RuleEngineAlgorithmForNozzles())
            {
                Logger("Nozzles Optimized...");
                return true;
            }
            else
            {
                Logger("Nozzles Couldn't be Optimized...");
                Logger("Going to Next Neighbor...");
                ResetCleanUpExecutedNearest();
                MainExecuted("BCD1190",maxlp);
                return false;
            }
        }
        public bool ErgCheckLoadPointsBCD1120()
        {
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet turbaResults_ERG = workbook.Sheets["Output"];
            bool ergCheckLoadPointsBCD1120 = false;
            bool turbaRun = false;
            bool overallPressureStatus = (turbaOutputModel.Stage_Pressure_Check == "TRUE") ? true : false;//turbaResults_ERG.Range["O3"].Value;
            if (overallPressureStatus)
            {
                Logger("Stages pressure are in limit For all ex_ERG_RsminHandler.load points..");
                goto CheckPower;
            }
            string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;;//turbaResults_ERG.Range["O10"].Value.ToString();
            string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;;//turbaResults_ERG.Range["O11"].Value.ToString();
            string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;//turbaResults_ERG.Range["O5"].Value.ToString();
            if (mcr2Status == "FALSE" || mcr1Status == "FALSE")
            {
                ergCheckLoadPointsBCD1120 = false;
                Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                // LoadPointGen loadPoint = new LoadPointGen();
                loadPoint.LoadPointGenerator_IncMassFlow(7, 5); // B11 = LP7
                loadPoint.LoadPointGenerator_IncMassFlow(8, 5); // B12 = LP8
                turbaRun = true;
            }
            if (highBpStatus == "FALSE")
            {
                ergCheckLoadPointsBCD1120 = false;
                Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                loadPoint.LoadPointGenerator_ReduceBP(2, 2);
                turbaRun = true;
            }
            if (turbaRun)
            {
                ExecutedDATFileProcessor dATFileProcessor = new ExecutedDATFileProcessor();
                // Need to see if this function is from Exec_DAT_handler for now im using Standard one
                dATFileProcessor.PrepareDatFileOnlyLPUpdate();//repareDATFileOnlyLPUpdate(); 
                TurbaAutomation turbaConfig = new TurbaAutomation();
                turbaConfig.LaunchTurba();
                ErgResultsCheckBCD1120();
                return ergCheckLoadPointsBCD1120;
            }
        CheckPower:
            double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q10"].Value);
            double basePower = turbaOutputModel.OutputDataList[0].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q2"].Value);
            // Excel.Range cellRange = turbaResults_ERG.Range["Q4:Q8"];
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= 5; ++i){
                 double loadPointPower = lpList[i].Power_KW;
                 if(loadPointPower < mcrPower)
                 {
                    Logger("Base power is less than MCR Case.. Going to Custom Path");
                    TerminateIgniteX("ergCheck_LoadPointsBCD1120");
                    return false;
                 }
                 else
                 {
                    Logger("Base power greater than MCR Case..");
                    ergCheckLoadPointsBCD1120 = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                 }
            }
            return ergCheckLoadPointsBCD1120;
        }
        public bool ErgResultsCheckBCD1120New(int maxlp = 0)
        {
            bool ergResultsCheckBCD1120New = false;
            bool turbaRun = false;
            int iterationCount = 0;
            do
            {
                iterationCount++;
                bool overallPressureStatus = (turbaOutputModel.Stage_Pressure_Check == "TRUE") ? true : false;//turbaResults_ERG.Range["O3"].Value;
                string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;;//turbaResults_ERG.Range["O10"].Value.ToString();
                string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;;//turbaResults_ERG.Range["O11"].Value.ToString();
                string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;//turbaResults_ERG.Range["O5"].Value.ToString();
                Logger("Iteration: " + iterationCount);
                Logger("MCR1 Status: " + mcr1Status);
                Logger("MCR2 Status: " + mcr2Status);
                Logger("High BP Status: " + highBpStatus);
                if (overallPressureStatus)
                {
                    Logger("Stages pressure are in limit for all ex_ERG_RsminHandler.load points..");
                    goto CheckPower;
                }
                if (mcr1Status == "FALSE" || mcr2Status == "FALSE")
                {
                    ergResultsCheckBCD1120New = false;
                    Logger("MCR 1 or 2 Pressure Failed ... Increasing mass flow");
                    ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                    loadPoint.LoadPointGenerator_IncMassFlow(7, 5); // B11 = LP7
                    loadPoint.LoadPointGenerator_IncMassFlow(8, 5); // B12 = LP8
                    turbaRun = true;
                }
                if (highBpStatus == "FALSE")
                {
                    ergResultsCheckBCD1120New = false;
                    Logger("Pressure failing at high back pressure ... Need to reduce BP");
                    ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                    loadPoint.LoadPointGenerator_ReduceBP(2, 2);
                    turbaRun = true;
                }
                if (turbaRun)
                {
                    ExecutedDATFileProcessor dATFileProcessor = new ExecutedDATFileProcessor();
                    dATFileProcessor.PrepareDatFileOnlyLPUpdate(maxlp);
                    TurbaAutomation turbaConfig = new TurbaAutomation();
                    turbaConfig.LaunchTurba(maxlp);
                    // PrepareDATFileOnlyLPUpdate();
                    // LaunchTurba();
                    turbaRun = false;
                }
                if (iterationCount > 100)
                {
                    Logger("Max iterations reached. Exiting loop.");
                    break;
                }
            } while (turbaOutputModel.OutputDataList[7].Stage_Pressure == "FALSE" ||
                     turbaOutputModel.OutputDataList[8].Stage_Pressure == "FALSE" ||
                     turbaOutputModel.OutputDataList[2].Stage_Pressure == "FALSE");
        CheckPower:
            double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q10"].Value);
            double basePower = turbaOutputModel.OutputDataList[0].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q2"].Value);
            // double mcrPower = turbaResults_ERG.Range["Q10"].Value;
            // double basePower = turbaResults_ERG.Range["Q2"].Value;
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= 5; ++i)
            {
                 double loadPointPower = lpList[i].Power_KW;
                 if(loadPointPower < mcrPower)
                 {
                    Logger("Base power is less than MCR Case.. Going to Custom Path");
                    TerminateIgniteX("ergCheck_LoadPointsBCD1120_new");
                    return false;
                 }
                 else
                 {
                    Logger("Base power greater than MCR Case..");
                    ergResultsCheckBCD1120New = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                 }
            }
            return ergResultsCheckBCD1120New;
        }
        public bool ErgCheckThrustValue1120(int maxlp = 0)
        {
            bool ergCheckThrustValue1120 = false;
            LaunchRsmin();
            
            double thrustUpperLim = turbaOutputModel.ThrustLimit;
            bool thrustResult = false;
            string logThrust = "";
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= maxLoadPoints; ++i)
            {
                double thrust = lpList[i].Thrust;//lpList[Convert.ToDouble(turbaResults_ERG.Cells[rowCount, 16].Value);
                logThrust += " | " + thrust;
                if (thrust > thrustUpperLim)
                {
                    ergCheckThrustValue1120 = false;
                    // Logger("Thrust: " + logThrust);
                    Logger("Thrust check Failed....");
                    Logger(" Thrust Permissible Limit " + thrustUpperLim + " Thrust Found: " + thrust);
                    // turbaResults_ERG.Cells["P3"].Value = false;
                    turbaOutputModel.Check_Thrust = "FALSE";
                    MainExecuted("BCD1190",maxlp);
                }
                else{
                    ergCheckThrustValue1120 = true;
                    thrustResult = true;
                }
            }
            
            if (thrustResult)
            {
                Logger("Thrust check Passed....");
                // turbaResults_ERG.Range["P3"].Value = true;
                 turbaOutputModel.Check_Thrust = "TRUE";
            }
            else
            {
                Logger("Thrust check Failed....");
                // turbaResults_ERG.Range["P3"].Value = false;
                 turbaOutputModel.Check_Thrust = "FALSE";
            }
            
            return ergCheckThrustValue1120;
        }
        public bool ErgCheckDetaTGBCWheelChamberPTBendingBCD1120(int maxlp = 0)
        {
            bool ergCheckDetaTGBCWheelChamberPTBendingBCD1120 = false;
            bool datChanged = false;
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet turbaResults_ERG = workbook.Sheets["Output"];
            double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//turbaResults_ERG.Range["E2"].Value;
            double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;//turbaResults_ERG.Range["F31"].Value;
            double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure; //Convert.ToDouble(turbaResults_ERG.Cells["F2"].Value);
            double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit; //Convert.ToDouble(turbaResults_ERG.Cells["G31"].Value);
            double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["G32"].Value);
            double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(turbaResults_ERG.Cells["G2"].Value);
            double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H30"].Value);
            double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H31"].Value);
            double wheelchamberTupperLim_2 = turbaOutputModel.WheelchamberT_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["H32"].Value);
            string bending = "";//turbaOutputModel.OutputDataList[0].Bending;//turbaResults_ERG.Range["N2"].Value.ToString();
            bool bendingStatus = false;
            bool deltaTStatus = false;
            bool wheelchamberPStatus = false;
            bool wheelchamberTStatus = false;
            for(int lp = 1;lp<=4;++lp){
                bending += turbaOutputModel.OutputDataList[lp].Bending;
            }
            if (!isCheckingLP5)
            {
                if (!string.IsNullOrEmpty(bending))
                {
                    Logger("Bending check Failed..");
                    turbaOutputModel.BendingCheck = "FALSE";
                    // turbaResults_ERG.Range["N3"].Value = false;
                    bendingStatus = false;
                }
                else
                {
                    Logger("Bending check Passed..");
                    turbaOutputModel.BendingCheck = "TRUE";
                    // turbaResults_ERG.Range["N3"].Value = true;
                    bendingStatus = true;
                }
                if (deltaT > deltaTupperLim)
                {
                    Logger("deltaT GBC check Failed..");
                    Logger("Delta ex_Power_KNN.ex_Power_KNN.T Limit: " + deltaTupperLim + " Delta T Used: " + deltaT);
                    turbaOutputModel.Check_DELTA_T = "FALSE";
                    // turbaResults_ERG.Range["E3"].Value = false;
                    deltaTStatus = false;
                }
                else
                {
                    Logger("deltaT GBC check Passed..");
                    turbaOutputModel.Check_DELTA_T = "TRUE";
                    // turbaResults_ERG.Range["E3"].Value = true;
                    deltaTStatus = true;
                }
                if (!(bendingStatus && deltaTStatus))
                {
                    Logger("Trying Another Project...");
                    MainExecuted("BCD1120");
                    return false;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(bending))
                {
                    Logger("Bending check Failed..");
                    turbaOutputModel.BendingCheck = "FALSE";
                    // turbaResults_ERG.Range["N3"].Value = false;
                    bendingStatus = false;
                }
                else
                {
                    Logger("Bending check Passed..");
                    turbaOutputModel.BendingCheck = "TRUE";
                    // turbaResults_ERG.Range["N3"].Value = true;
                    bendingStatus = true;
                }
                if (!(bendingStatus))
                {
                    Logger("Trying Another Project...");
                    MainExecuted("BCD1120",maxlp);
                    return false;
                }
            }

           
            if (wheelchamberP > wheelchamberPupperLim_2)
            {
                Logger("wheelchamber ex_Power_KNN.P check Failed..");
                // turbaResults_ERG.Range["F3"].Value = false;
                turbaOutputModel.Check_Wheel_Chamber_Pressure = "FALSE";
                wheelchamberPStatus = false;
            }
            else
            {
                Logger("wheelchamber ex_Power_KNN.P check Passed..");
                // turbaResults_ERG.Range["F3"].Value = true;
                turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
                wheelchamberPStatus = true;
            }
            if (wheelchamberT > wheelchamberTupperLim_2)
            {
                Logger("wheelchamber Temp check Failed.." + wheelchamberT);
                // turbaResults_ERG.Range["G3"].Value = false;
                turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
                wheelchamberTStatus = false;
            }
            else
            {
                Logger("wheelchamber Temp check Passed..");
                // turbaResults_ERG.Range["G3"].Value = true;
                turbaOutputModel.Check_Wheel_Chamber_Temperature = "TRUE";
                wheelchamberTStatus = true;
            }
            if (!wheelchamberPStatus || !wheelchamberTStatus)
            {
                Logger("Selecting New flow path from executed projects...");
                MainExecuted("BCD1120",maxlp);
                TurbineDesignPage.finalToken.Cancel();
                return false;
            }
            if(datChanged){
                ExecutedDATFileProcessor dATFileProcessor = new ExecutedDATFileProcessor();
                dATFileProcessor.PrepareDatFile(maxlp);
                TurbaAutomation turbaConfig = new TurbaAutomation();
                turbaConfig.LaunchTurba(maxlp);
                ErgResultsCheckBCD1120(maxlp);
                // ergCheckDetaTGBCWheelChamberPTBendingBCD1120 = false;
                return false;
                    
            }
            else{
                ergCheckDetaTGBCWheelChamberPTBendingBCD1120 = true;
                Logger("DeltaT, Bending and Wheelchamber checks passed, checking Thrust...");
            }
            return ergCheckDetaTGBCWheelChamberPTBendingBCD1120;
        }
        public bool ErgCheckExhaustVolumetricFlowBCD1120(int maxlp = 0)
        {
            double volFlow = turbaOutputModel.OutputDataList[0].Vol_Flow;//turbaResults_ERG.Range["M2"].Value;
            int currentFlowPathNo = preFeasibilityModel.Variant;
            // int currentFlowPathNo = Convert.ToInt32(currentFlowPath.Range["G15"].Value);
            double volFlowUpperLimit = turbaOutputModel.VolFlow_UpperLimit;//turbaResults_ERG.Range["N31"].Value;
            if (volFlow <= volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow = "TRUE";
                // turbaResults_ERG.Range["M3"].Value = true;
                Logger("volFlow is in acceptable range...continuing with 1120");
                
                return true;
            }
            Logger("Go to 2GBC path");
            return false;
        }
        public void Logger(string message)
        {
            logger.LogInformation(message);
        }
        public void ResetCleanUpExecutedNearest()
        {
             Console.WriteLine("workinghggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg");
         List<PowerNearest> powerNearest = turbineDataModel.ListPower;
        
        int k = Convert.ToInt32(powerNearest[0].KNearest);
       
        powerNearest.Clear();
        turbineDataModel.fillPowerNearestData();
        turbineDataModel.ListPower[0].SteamPressure = turbineDataModel.InletPressure;
        turbineDataModel.ListPower[0].SteamTemperature = turbineDataModel.InletTemperature;
        turbineDataModel.ListPower[0].SteamMass = turbineDataModel.MassFlowRate;
        turbineDataModel.ListPower[0].ExhaustPressure = turbineDataModel.ExhaustPressure;
        
        turbineDataModel.ListPower[0].KNearest = turbineDataModel.NoOfExecuted.ToString();
            // Implement the logic for ResetCleanUpExecutedNearest
        }
        public void MainExecuted(string projectCode,int maxlp = 0)
        {
            MainExecutedClass mainExecutedClass = new MainExecutedClass();
            mainExecutedClass.MainExecuted(projectCode,maxlp);
            // Implement the logic for MainExecuted
        }
        public void TerminateIgniteX(string functionName)
        {
            // Implement the logic for TerminateIgniteX
        }
        public void LaunchRsmin()
        {
            TurbaAutomation turbaConfig = new TurbaAutomation();
            turbaConfig.LaunchRsmin();
        }
    }
}