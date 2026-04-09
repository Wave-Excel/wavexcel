using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HMBD.Custom_LoadPointGenerator;
using Handlers.Custom_DAT_Handler;
using Turba.Cu_TurbaConfig;
using Interfaces.ILogger;
using StartExecutionMain;
using Optimizers.CustomNozzleOptimizer;
using Ignite_x_wavexcel;
namespace Checks.CustomBCD1120;
// using Microsoft.Office.Interop.Excel;
public class CustomERGCheck1120
{

        TurbaOutputModel turbaOutputModel;
        PreFeasibilityDataModel preFeasibilityDataModel;
        IConfiguration configuration;
        ILogger logger;
        
        int AbweichungLowerLimit;
        int AbweichungUpperLimit;
        int FMIN2MAX;
        int DusenLowerLimit;
        double ExhaustVolumetricFlowUpperLimit;
        double LAUFZAHLUpperLimit;
        double BIEGESPANNUNGMaxValue;
        int maxLoadPoints=10;
        public static bool isCheckingLP5 = false;

        public CustomERGCheck1120()
        {
            turbaOutputModel = TurbaOutputModel.getInstance();
            preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            AbweichungLowerLimit = 2;
            AbweichungUpperLimit = configuration.GetValue<int>("AppSettings:Abweichung_UpperLimit");
            FMIN2MAX =  configuration.GetValue<int>("AppSettings:FMIN2_MAX");
            DusenLowerLimit = configuration.GetValue<int>("AppSettings:DUESEN_Lower_Limit");
            ExhaustVolumetricFlowUpperLimit = configuration.GetValue<double>("AppSettings:Exhaust_Volumetric_Flow_Upper_Limit");
            LAUFZAHLUpperLimit = configuration.GetValue<double>("AppSettings:LAUFZAHL_Upper_Limit");
            BIEGESPANNUNGMaxValue = configuration.GetValue<double>("AppSettings:BIEGESPANNUNG_Max_Value");
            logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        }

        

        public void ErgResultsCheckBCD1120_Custom(int mxlp=0)
        {
            if (ErgCheckExhaustVolumetricFlowBCD1120_Custom(mxlp))
            {
                if (ErgCheckThrustValue1120_Custom(mxlp))
                {
                    if (ErgCheckDetaTGBCWheelChamberPTBendingBCD1120_Custom(mxlp))
                    {
                        if (ErgCheckNozzlesSectionBCD1120_Custom(mxlp))
                        {
                            if (ErgCheckLoadPointsBCD1120_Custom(mxlp))
                            {
                                Console.WriteLine("Turbine looks great !! Let's Compare cu_Power_KNN.Power With HBD..");
                                Logger("Comparing cu_Power_KNN.Power With HBD..");
                            }
                        }
                    }
                }
            }
        }

        private bool ErgCheckNozzlesSectionBCD1120_Custom(int maxlp = 0)
        {
            return RuleEngineAlgorithmForNozzles();
        }

        

        private bool ErgCheckLoadPointsBCD1120_Custom(int maxlp = 0)
        {
            bool ergCheckLoadPointsBCD1120_Custom = false;
            bool Turbarun = false;

            if ((turbaOutputModel.Stage_Pressure_Check=="TRUE")?true:false)
            {
                Logger("Stages pressure are in limit For all cu_ERG_RsminHandler.load points..");
                goto CheckPower;
            }

            string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;
            string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;
            string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;

            if (mcr2Status == "False" || mcr1Status == "False")
            {
                ergCheckLoadPointsBCD1120_Custom = false;
                Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                LoadPointGenerator_IncMassFlow(7, 5);
                LoadPointGenerator_IncMassFlow(8, 5);
                Turbarun = true;
            }

            if (highBpStatus == "False")
            {
                ergCheckLoadPointsBCD1120_Custom = false;
                Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                LoadPointGenerator_ReduceBP(2, 2);
                Turbarun = true;
            }

            if (Turbarun)
            {
                PrepareDATFile_OnlyLPUpdate(maxlp);
                LaunchTurba(maxlp);
                ErgResultsCheckBCD1120_Custom(maxlp);
                return false;
            }

        CheckPower:
            double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;
            double basePower = turbaOutputModel.OutputDataList[0].Power_KW;
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;

             for (int i = 1; i <= 5; ++i)
            {
                 double loadPointPower = lpList[i].Power_KW;
                 if(loadPointPower < mcrPower)
                 {
                    ergCheckLoadPointsBCD1120_Custom = false;
                    Console.WriteLine("Base power is less than MCR Case.. Going to Custom Path");
                    goto ExitSub;
                }
                else
                {
                    Console.WriteLine("Base power greater than MCR Case..");
                    ergCheckLoadPointsBCD1120_Custom = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                }
            }

        ExitSub:
            return ergCheckLoadPointsBCD1120_Custom;
        }



        private bool ErgCheckThrustValue1120_Custom(int maxlp =0)
        {
            bool ergCheckThrustValue1120_Custom = false;
            LaunchRsmin(maxlp);
            double thrustUpperLim = 0.8;
            bool ThrustResult = false;
            // Range LoadPointThrust = turbaResults_ERG.Range["P4:P13"];
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= maxLoadPoints; ++i)
            {
            
                double thrust = lpList[i].Thrust;

                if (thrust > thrustUpperLim || thrust < -0.8)
                {
                    ergCheckThrustValue1120_Custom = false;
                    Logger("Thrust check Failed....");
                    turbaOutputModel.Check_Thrust = "FALSE";
                    Logger("Thrust cu_Customer_Input_Handler.Check FAILED...");
                    return false;
                }
                else
                {
                    ergCheckThrustValue1120_Custom = true;
                    ThrustResult = true;
                }
            }

            if (ThrustResult)
            {
                Logger("Thrust check Passed....");
                turbaOutputModel.Check_Thrust="TRUE";
                // turbaResults_ERG.Range["P3"].Value = true;
            }
            else
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
                // turbaResults_ERG.Range["P3"].Value = false;
            }

            return ergCheckThrustValue1120_Custom;
        }

        

        private bool ErgCheckDetaTGBCWheelChamberPTBendingBCD1120_Custom(int maxlp = 0)
        {
            bool ergCheckDetaTGBCWheelChamberPTBendingBCD1120_Custom = false;
            bool datChanged = false;

            double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;
            double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;

            double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;
            double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit;
            double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;

            double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;
            double wheelchamberTlowerLim = 474.9089588;
            double wheelchamberTupperLim = 474.9089588;
            double wheelchamberTupperLim_2 = 412;
            string bending = "";
            bool bendingStatus = false;
            for (int lp = 1; lp <= 4; ++lp)
                {
                    bending += turbaOutputModel.OutputDataList[lp].Bending;
                }
                if (!string.IsNullOrEmpty(bending))
                {
                    bendingStatus = false;
                    // break;
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
            bool deltaTStatus = false;
            if (deltaT > deltaTupperLim)
            {
                Logger("deltaT GBC check Failed..");
                // Logger("Delta ex_Power_KNN.ex_Power_KNN.T Limit: " + deltaTupperLim + " Delta T Used: " + deltaT);
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
                Logger("Trying Another Project................");
                // Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                // MainExecuted("BCD1190");
                // Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
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
                Logger("Trying Another Project................");
                // Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                // MainExecuted("BCD1190");
                // Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                return false;
            }
        }
            bool wheelchamberPStatus;
            bool wheelchamberP_executedPath_1120_1130=false;
            if (wheelchamberP > wheelchamberPupperLim_2)
            {
                wheelchamberP_executedPath_1120_1130=true;
                Logger("wheelchamber P check Failed..");
                Console.WriteLine("wheelchamber P check Failed..");
                turbaOutputModel.Check_Wheel_Chamber_Pressure="FALSE";
                // turbaResults_ERG.Range["F3"].Value = false;
                wheelchamberPStatus = false;
            }
            else
            {
                Logger("wheelchamber P check Passed..");
                Console.WriteLine("wheelchamber P check Passed..");
                // turbaResults_ERG.Range["F3"].Value = true;
                turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
                wheelchamberPStatus = true;
            }

            bool wheelchamberTStatus;
            bool wheelchamberT_executedPath_1120_1130=false;
            if (wheelchamberT > wheelchamberTupperLim_2)
            {
                wheelchamberT_executedPath_1120_1130=true;
                Logger("wheelchamber Temp check Failed..");
                Console.WriteLine("wheelchamber Temp check Failed..");
                turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
                // turbaResults_ERG.Range["G3"].Value = false;
                wheelchamberTStatus = false;
            }
            else
            {
                Logger("wheelchamber Temp check Passed..");
                Console.WriteLine("wheelchamber Temp check Passed..");
                turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
                // turbaResults_ERG.Range["G3"].Value = true;
                wheelchamberTStatus = true;
            }

            if (wheelchamberP_executedPath_1120_1130 || wheelchamberT_executedPath_1120_1130)
            {
                Logger("Selecting New flow path from executed projects...");
                Console.WriteLine("Selecting New flow path from executed projects...");
                return false;
            }

           
                ergCheckDetaTGBCWheelChamberPTBendingBCD1120_Custom = true;
            

            return ergCheckDetaTGBCWheelChamberPTBendingBCD1120_Custom;
        }

        
        private bool ErgCheckExhaustVolumetricFlowBCD1120_Custom(int maxLp = 0)
        {
            double VolFlow = turbaOutputModel.OutputDataList[0].Vol_Flow;// turbaResults_ERG.Range["M2"].Value;
            int currentFlowPathNo = preFeasibilityDataModel.Variant;//Convert.ToInt32(currentFlowPath.Range["G15"].Value);
            double volFlowUpperLimit = 7.7;//turbaResults_ERG.Range["N31"].Value;

            if (VolFlow <= volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow ="TRUE";
                // turbaResults_ERG.Range["M3"].Value = true;
                Logger("volFlow is in acceptable range...continuing with 1120");
                Console.WriteLine("volFlow is in acceptable range...continuing with 1120");
                return true;
            }
            else
            {
                Console.WriteLine("Go to 2GBC path");
                return false;
            }
        }

        public void ERG_CUSTOM_BASE_CHECKS()
        {
            Logger("Checking ABWEICHUNG...");
            if ((turbaOutputModel.OutputDataList[1].ABWEICHUNG  >= AbweichungLowerLimit) && (turbaOutputModel.OutputDataList[1].ABWEICHUNG  <= AbweichungUpperLimit))
            {
                Logger("ABWEICHUNG is within Range");
            }
            else
            {
                Logger("ABWEICHUNG Failed at " + turbaOutputModel.OutputDataList[1].ABWEICHUNG);
            }

            Logger("Checking FMIN2...");
            if (turbaOutputModel.OutputDataList[0].FMIN2 <= FMIN2MAX)
            {
                Logger("FMIN2 is within Range");
            }
            else
            {
                Logger("FMIN2 Failed at " + turbaOutputModel.OutputDataList[0].FMIN2);
            }

            Logger("Checking DUESEN...");
            if (turbaOutputModel.OutputDataList[0].DUESEN >= DusenLowerLimit)
            {
                Logger("DUESEN is within Range");
            }
            else
            {
                Logger("DUESEN Failed at " + turbaOutputModel.OutputDataList[0].DUESEN);
            }

            Logger("Checking FMIN1 and FMIN2 DUESEN...");
            if (turbaOutputModel.OutputDataList[0].FMIN1_DUESEN >= turbaOutputModel.OutputDataList[0].FMIN2_DUESEN)
            {
                Logger("FMIN1 and FMIN2 DUESEN passed");
            }
            else
            {
                Logger("FMIN1 and FMIN2 DUESEN Failed...");
            }

            Logger("Checking cu_Exhaust_Curve.Exhaust Volumetric Flow...");
            if (turbaOutputModel.OutputDataList[0].Vol_Flow <= ExhaustVolumetricFlowUpperLimit){
                Logger("Exhaust Volumetric Flow is within Range");
            }
            else
            {
                Logger("Exhaust Volumetric Flow Failed at " + turbaOutputModel.OutputDataList[0].Vol_Flow);
                Logger("Go to 2 GBC...");
                TerminateIgniteX("Exhaust_Volumetric_Flow");
            }

            Logger("Checking Stage Pressure...");
            bool check = (turbaOutputModel.Stage_Pressure_Check == "TRUE")? true: false;
            if (check)
            {
                Logger("Stage Pressure is within Range");
            }
            else
            {
                Logger("Stage Pressure Failed at " + turbaOutputModel.OutputDataList[0].Stage_Pressure);
            }

            Logger("Checking LAUFZAHL U/CO...");
            if (turbaOutputModel.OutputDataList[0].LAUFZAHL <= LAUFZAHLUpperLimit)
            {
                Logger("LAUFZAHL U/CO is within Range");
            }
            else
            {
                Logger("LAUFZAHL U/CO Failed at " + turbaOutputModel.OutputDataList[0].LAUFZAHL);
            }

            Logger("Checking BIEGESPANNUNG 1...");
            if (turbaOutputModel.OutputDataList[0].BIEGESPANNUNG < BIEGESPANNUNGMaxValue)
            {
                Logger("BIEGESPANNUNG 1 is within Range");
            }
            else
            {
                Logger("BIEGESPANNUNG 1 Failed at " + turbaOutputModel.OutputDataList[0].BIEGESPANNUNG);
            }

            Logger("Checking HSTAT - HGES...");
            bool check1 = (turbaOutputModel.Check_BIEGESPANNUNG_TRUE_FALSE=="TRUE")? true: false;
            if (check1)
            {
                Logger("HSTAT - HGES Passed");
            }
            else
            {
                Logger("HSTAT - HGES Failed");
            }

            Logger("Checking LANG...");
            bool check2 = (turbaOutputModel.Check_Lang=="TRUE")? true: false;
            if (check2)
            {
                Logger("LANG Passed");
            }
            else
            {
                Logger("LANG Failed");
            }
        }

        public void ERG_CUSTOM_TURNA_CHECKS()
        {
            Logger("Checking DAMPFMENGE UND LEISTUNG....");
            bool check = (turbaOutputModel.Check_AK_LECKDVERDICT=="TRUE")?true:false;
            if (check)
            {
                Logger("DAMPFMENGE UND LEISTUNG Passed, proceeding with ERG Checks...");
            }
            else
            {
                Logger("DAMPFMENGE UND LEISTUNG Failed");
            }
        }

        private void Logger(string message)
        {
            // Implement your logging mechanism here
            
            // Console.WriteLine(message);
            logger.LogInformation(message);
        }

        private void MainExecuted(string projectCode)
        {
            // Implement your main executed logic here
            Console.WriteLine($"Executing main project: {projectCode}");
        }

        private void LaunchRsmin(int maxlp=0)
        {
            CuTurbaAutomation cuTurbaAutomation= new CuTurbaAutomation();   
            cuTurbaAutomation.LaunchRsmin(maxlp);
            // Implement your LaunchRsmin logic here
            Console.WriteLine("Launching Rsmin...");
        }

        private void LoadPointGenerator_IncMassFlow(int cell, int percentage)
        {
            CustomLoadPointGenerator customLoadPointGenerator = new CustomLoadPointGenerator();
            customLoadPointGenerator.LoadPointGenerator_IncMassFlow(cell, percentage);
            // Implement your LoadPointGenerator_IncMassFlow logic here
            Console.WriteLine($"Increasing mass flow by {percentage}% at {cell}...");
        }

        private void LoadPointGenerator_ReduceBP(int cell, int percentage)
        {
            CustomLoadPointGenerator customLoadPointGenerator = new CustomLoadPointGenerator();
            customLoadPointGenerator.LoadPointGenerator_ReduceBP(cell, percentage);
            // Implement your LoadPointGenerator_ReduceBP logic here
            Console.WriteLine($"Reducing back pressure by {percentage}% at {cell}...");
        }

        private void PrepareDATFile_OnlyLPUpdate(int maxlp = 0)
        {
            CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
            customDATFileProcessor.PrepareDatFileOnlyLPUpdate(maxlp);
            // Implement your PrepareDATFile_OnlyLPUpdate logic here
            Console.WriteLine("Preparing DAT file with only load point update...");
        }

        private void LaunchTurba(int maxlp =0)
        {
            CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
            // Implement your LaunchTurba logic here
            cuTurbaAutomation.LaunchTurba(maxlp);
            Console.WriteLine("Launching Turba...");
        }

        private void PrepareDATFile()
        {
            CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
            customDATFileProcessor.PrepareDatFile();
            // Implement your PrepareDATFile logic here
            Console.WriteLine("Preparing DAT file...");
        }

        private void TerminateIgniteX(string reason)
        {
        // Implement your TerminateIgniteX logic here
        TurbineDesignPage.cts.Cancel();
        
        Console.WriteLine($"Terminating IgniteX due to {reason}...");
        }

        private bool RuleEngineAlgorithmForNozzles()
        {
            CustomNozzleOptimizer customNozzleOptimizer= new CustomNozzleOptimizer();
            return customNozzleOptimizer.RuleEngineAlgorithmForNozzles();
            // Implement your RuleEngineAlgorithmForNozzles logic here
            // Console.WriteLine("Executing Rule Engine Algorithm for Nozzles...");
            // return true;
        }
}