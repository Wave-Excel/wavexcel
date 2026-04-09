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
using Models.LoadPointDataModel;
using HMBD.Exec_Exhaust_Curve;
using Optimizers.Exec_ERG_NozzleOptimizer;
using HMBD.Exec_LoadPointGenerator;
using Handlers.Exec_DAT_Handler;
using Turba.Exec_TurbaConfig;
using HMBD.Exec_CW_Curve;
using StartExecutionMain;
using Ignite_x_wavexcel;
//using CoreMotion;


namespace Checks.ERG_BCD1190
{
    class ERG_BCD1190
    {
        public static string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
        private IConfiguration configuration;
        private TurbaOutputModel turbaOutputModel;
        private LoadPointDataModel loadPointDataModel;
        private PreFeasibilityDataModel preFeasibilityModel;
        private ILogger logger;
        int maxLoadPoints = 10;
        public static bool isLP5Update = false;
        public ERG_BCD1190(){
            turbaOutputModel = TurbaOutputModel.getInstance();
            preFeasibilityModel = PreFeasibilityDataModel.getInstance();
            loadPointDataModel = LoadPointDataModel.getInstance();
            logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath"); 
        }
        public void ErgResultsCheckBCD1190(int maxlp=0)
        {
            if (ErgCheckExhaust1190(maxlp) &&
                ErgCheckNozzlesSectionBCD1190(maxlp) &&
                ErgCheckThrustValue1190(maxlp) &&
                ErgCheckDetaTGBCWheelChamberPTBendingBCD1190(maxlp) &&
                ErgResultsCheckBCD1190New(maxlp))
            {
                Logger("Turbine looks great !! Let's Compare ex_Power_KNN.Power With HBD..");
                Logger("Comparing ex_Power_KNN.Power With HBD..");
            }
            //else
            //{
            //    Logger("Loadpoints ex_Customer_Input_Handler.Check Failed... Terminating IgniteX");
            //    TerminateIgniteX("Loadpoints");
            //}
        }

        public bool ErgCheckExhaust1190(int maxlp = 0)
        {
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet turbaResults_ERG = workbook.Sheets["Output"];
            // Excel.Worksheet currentFlowPath = workbook.Sheets["Pre-Feasibility checks"];
            // Excel.Worksheet LP = workbook.Sheets["LOAD_POINTS"];
            bool ergCheckExhaust1190 = false;

            double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//turbaResults_ERG.Range["E2"].Value;
            double pressureForCurve = loadPointDataModel.LoadPoints[2].BackPress;//LP.Range["G6"].Value;
            double tempForCurve = turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature;//turbaResults_ERG.Range["AB2"].Value;

            if (deltaT <= 100)
            {
                // ExhaustFunctions
                if (ExhaustFunctions.Exhaust1(tempForCurve, pressureForCurve))
                {
                    ergCheckExhaust1190 = true;
                    Logger("Exhaust ex_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Logger("Check Failed, ex_Exhaust_Curve.Exhaust not optimal. Curve 1 Used...");
                    Logger("Permissable Limit: " + ExhaustFunctions.Exhaust1_GetUpperLimit(tempForCurve) + " Our Value: " + pressureForCurve);
                }
            }
            else if (deltaT <= 210 && deltaT > 100)
            {
                if (ExhaustFunctions.Exhaust2(tempForCurve, pressureForCurve))
                {
                    ergCheckExhaust1190 = true;
                    Logger("Exhaust ex_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Logger("Check Failed, ex_Exhaust_Curve.Exhaust not optimal. Curve 2 Used...");
                    Logger("Permissable Limit: " + ExhaustFunctions.Exhaust2_GetUpperLimit(tempForCurve) + " Our Value: " + pressureForCurve);
                    MainExecuted("BCD1190",maxlp);
                    //TurbineDesignPage.finalToken.Cancel();
                }
            }
            else
            {
                Logger("Delta T is out of Range (>210)" + deltaT);
                Logger("Trying Next nearest neighbor...");
                MainExecuted("BCD1190",maxlp);
                //TurbineDesignPage.finalToken.Cancel();
            }
            return ergCheckExhaust1190;
        }

        public bool ErgCheckNozzlesSectionBCD1190(int maxlp = 0)
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
                MainExecuted("BCD1190", maxlp);
                return false;
            }
        }

        public bool ErgCheckLoadPointsBCD1190()
        {
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet turbaResults_ERG = workbook.Sheets["Output"];
            bool ergCheckLoadPointsBCD1190 = false;
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
                ergCheckLoadPointsBCD1190 = false;
                Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                // ExecLoadPointGenerator
                ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                loadPoint.LoadPointGenerator_IncMassFlow(7, 5); // B11 = LP7
                loadPoint.LoadPointGenerator_IncMassFlow(8, 5); // B12 = LP8
                turbaRun = true;
            }
            if (highBpStatus == "FALSE")
            {
                ergCheckLoadPointsBCD1190 = false;
                Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                loadPoint.LoadPointGenerator_ReduceBP(2, 2);
                turbaRun = true;
            }

            if (turbaRun)
            {
                ExecutedDATFileProcessor  dATFileProcessor = new ExecutedDATFileProcessor();
                // Need to see if this function is from Exec_DAT_handler for now im using Standard one
                dATFileProcessor.PrepareDatFileOnlyLPUpdate(); 
                TurbaAutomation turbaConfig = new TurbaAutomation();
                turbaConfig.LaunchTurba();
                ErgResultsCheckBCD1190();
                return ergCheckLoadPointsBCD1190;
            }

        CheckPower:
            double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q10"].Value);
            double basePower = turbaOutputModel.OutputDataList[0].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q2"].Value);
            // Excel.Range cellRange = turbaResults_ERG.Range["Q4:Q8"];
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= 5; ++i){
                double loadPointPower = lpList[i].Power_KW;
                if (loadPointPower < mcrPower)
                {
                    ergCheckLoadPointsBCD1190 = false;
                    Logger("Base power is less than MCR Case.. Going to Custom Path");
                    TerminateIgniteX("ergCheck_LoadPointsBCD1190");
                    return ergCheckLoadPointsBCD1190;
                }
                else
                {
                    Logger("Base power greater than MCR Case..");
                    ergCheckLoadPointsBCD1190 = true;
                    // turbaResults_ERG.Range["Q3"].Value = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                }
            }
            return ergCheckLoadPointsBCD1190;
        }

        public bool ErgResultsCheckBCD1190New(int maxlp = 0)
        {
            bool ergResultsCheckBCD1190New = false;
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
                    ergResultsCheckBCD1190New = false;
                    Logger("MCR 1 or 2 Pressure Failed ... Increasing mass flow");
                    ExecLoadPointGenerator loadPoint = new ExecLoadPointGenerator();
                    loadPoint.LoadPointGenerator_IncMassFlow(7, 5); // B11 = LP7
                    loadPoint.LoadPointGenerator_IncMassFlow(8, 5); // B12 = LP8
                    // LoadPointGeneratorIncMassFlow("B11", 5);
                    // LoadPointGeneratorIncMassFlow("B12", 5);
                    turbaRun = true;
                }

                if (highBpStatus == "FALSE")
                {
                    ergResultsCheckBCD1190New = false;
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


            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= 5; ++i)
            {
                double loadPointPower = lpList[i].Power_KW;
                 if(loadPointPower < mcrPower)
                 {
                    Logger("Base power is less than MCR Case.. Going to Custom Path");
                    TerminateIgniteX("ergResultsCheckBCD1190_new");
                    return false;
                 }
                 else
                 {
                    Logger("Base power greater than MCR Case..");
                    ergResultsCheckBCD1190New = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                 }
            }
            return ergResultsCheckBCD1190New;
        }

        public bool ErgCheckThrustValue1190(int maxlp = 0)
        {
            bool ergCheckThrustValue1190 = false;
            TurbaAutomation turbaAutomation = new TurbaAutomation();
            turbaAutomation.LaunchRsmin();
            double thrustUpperLim = turbaOutputModel.ThrustLimit ;
            bool thrustResult = false;
            string logThrust = "";
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= maxLoadPoints; ++i)
            {
                double thrust = lpList[i].Thrust;//lpList[Convert.ToDouble(turbaResults_ERG.Cells[rowCount, 16].Value);
                logThrust += " | " + thrust;
                if (thrust > thrustUpperLim)
                {
                    ergCheckThrustValue1190 = false;
                    Logger("Thrust check Failed....");
                    Logger(" Thrust Permissible Limit " + thrustUpperLim + " Thrust Found: " + thrust);
                    turbaOutputModel.Check_Thrust = "FALSE";
                    return false;
                }
                else
                {
                    ergCheckThrustValue1190 = true;
                    thrustResult = true;   
                }
            }
           

            if (thrustResult)
            {
                Logger("Thrust check Passed....");
                turbaOutputModel.Check_Thrust = "TRUE";
                // turbaResults_ERG.Range["P3"].Value = true;
            }
            else
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
                // turbaResults_ERG.Range["P3"].Value = false;
                Logger("Going to Next Neighbor...");
                MainExecuted("BCD1190", maxlp);
            }

            return ergCheckThrustValue1190;
        }

        public bool ErgCheckDetaTGBCWheelChamberPTBendingBCD1190(int maxlp = 0)
        {
            bool ergCheckDetaTGBCWheelChamberPTBendingBCD1190 = true;
            bool datChanged = false;
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet turbaResults_ERG = workbook.Sheets["Output"];
            // Excel.Worksheet currentFlowPath = workbook.Sheets["Pre-Feasibility checks"];

            double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//turbaResults_ERG.Range["E2"].Value;
            double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;//turbaResults_ERG.Range["F31"].Value;
            double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure; //Convert.ToDouble(turbaResults_ERG.Cells["F2"].Value);
            double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit; //Convert.ToDouble(turbaResults_ERG.Cells["G31"].Value);
            double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["G32"].Value);
            double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(turbaResults_ERG.Cells["G2"].Value);
            double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H30"].Value);
            double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H31"].Value);
            double wheelchamberTupperLim_2 = turbaOutputModel.WheelchamberT_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["H32"].Value);
            string bending = "";// turbaOutputModel.OutputDataList[0].Bending;//turbaResults_ERG.Range["N2"].Value.ToString();
            bool bendingStatus = false;
            bool deltaTStatus = false;
            bool wheelchamberPStatus = false;
            bool wheelchamberTStatus = false;
            for (int lp = 1; lp <= 4; ++lp)
            {
                bending += turbaOutputModel.OutputDataList[lp].Bending;
            }
            if (!isLP5Update)
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
                    // turbaResults_ERG.Range["E3"].Value = false;
                    turbaOutputModel.Check_DELTA_T = "FALSE";
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
                    Logger("Going to Executed/Custom Path");
                    Logger("Going to Next Nearest Neighbor due to Bending and/or Delta failure");
                    MainExecuted("BCD1190", maxlp);
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
                    Logger("Going to Executed/Custom Path");
                    Logger("Going to Next Nearest Neighbor due to Bending and/or Delta failure");
                    MainExecuted("BCD1190",maxlp);
                    return false;
                }
            }
            

            
            
            if (preFeasibilityModel.TemperatureActualValue < 500)
            {
                // ExeCwCurve
                // ExeCwCurve
                if (!ExeCwCurve.WheelChamberWithCW_1(wheelchamberT, wheelchamberP))
                {
                    wheelchamberPStatus = true;
                }
                else
                {
                    wheelchamberPStatus = false;
                    Logger("Wheel Chamber Temp and Pressure check Passed...");
                }
            }
            else
            {
                if (!ExeCwCurve.WheelChamberWithCW_2(wheelchamberT, wheelchamberP))
                {
                    wheelchamberPStatus = true;
                }
                else
                {
                    wheelchamberPStatus = false;
                    Logger("Wheel Chamber Temp and Pressure check Passed...");
                }
            }

            if (wheelchamberPStatus)
            {
                Logger("Going to Next Nearest Neighbor due to Failure of ex_With_CW_Curve.Wheel Chamber Temp/pressure...");
                MainExecuted("BCD1190", maxlp);
                return false;
            }

            return ergCheckDetaTGBCWheelChamberPTBendingBCD1190;
        }

        public bool ErgCheckExhaustVolumetricFlowBCD1190()
        {
            double volFlow = turbaOutputModel.OutputDataList[0].Vol_Flow;
            int currentFlowPathNo = preFeasibilityModel.Variant;//Convert.ToInt32(currentFlowPath.Range["G15"].Value);
            double volFlowUpperLimit = turbaOutputModel.VolFlow_UpperLimit;//turbaResults_ERG.Range["N31"].Value;

            if (volFlow <= volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow = "TRUE";
                // turbaResults_ERG.Range["M3"].Value = true;
                Logger("volFlow is in acceptable range...continuing with 1190");
                return true;
            }
            else
            {
                Logger("Go to 2GBC path");
                return false;
            }
        }

        public void Logger(string message)
        {
            logger.LogInformation(message);
        }

        public void TerminateIgniteX(string reason)
        {
            
            // Implement the logic for TerminateIgniteX
        }

        public void MainExecuted(string projectCode,int maxLp =0)
        {
            // Implement the logic for MainExecuted
            MainExecutedClass mainExecutedClass = new MainExecutedClass();
            mainExecutedClass.MainExecuted(projectCode,maxLp);
        }

        // public bool RuleEngineAlgorithmForNozzles()
        // {
        //     // Implement the logic for RuleEngineAlgorithmForNozzles
        //     return true;
        // }

        // public void LoadPointGeneratorIncMassFlow(string cell, int percentage)
        // {
        //     // Implement the logic for LoadPointGeneratorIncMassFlow
        // }

        // public void LoadPointGeneratorReduceBP(string cell, int percentage)
        // {
        //     // Implement the logic for LoadPointGeneratorReduceBP
        // }

        // public void PrepareDATFileOnlyLPUpdate()
        // {
        //     // Implement the logic for PrepareDATFileOnlyLPUpdate
        // }

        // public void LaunchTurba()
        // {
        //     // Implement the logic for LaunchTurba
        // }

        // public void LaunchRsmin()
        // {
        //     // Implement the logic for LaunchRsmin
        // }

        // public bool ExhaustCurveExhaust1(double temp, double pressure)
        // {
        //     // Implement the logic for ExhaustCurveExhaust1
        //     return true;
        // }

        // public bool ExhaustCurveExhaust2(double temp, double pressure)
        // {
        //     // Implement the logic for ExhaustCurveExhaust2
        //     return true;
        // }

        // public double ExhaustCurveExhaust1GetUpperLimit(double temp)
        // {
        //     // Implement the logic for ExhaustCurveExhaust1GetUpperLimit
        //     return 0;
        // }
}
}