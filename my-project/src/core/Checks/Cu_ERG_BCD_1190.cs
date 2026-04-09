using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Handlers.Custom_DAT_Handler;
using HMBD.Cu_CW_Curve;
using HMBD.Custom_LoadPointGenerator;
using HMBD.Exec_Exhaust_Curve;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Models.LoadPointDataModel;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Optimizers.CustomNozzleOptimizer;
using StartExecutionMain;
using Turba.Cu_TurbaConfig;
namespace Checks.CustomERGCheck1190;

public class CustomERGCheck1190
    {
        
        private TurbaOutputModel turbaOutputModel;
        private LoadPointDataModel loadPointDataModel;
        private PreFeasibilityDataModel preFeasibilityDataModel;
        public static bool isCheckingLP5 = false;
    IConfiguration configuration;
        ILogger logger;
        int maxLoadPoints = 10;
        public CustomERGCheck1190()
        {
            // turbaResults_ERG = outputSheet;
            // currentFlowPath = preFeasibilitySheet;
            // LP = loadPointsSheet;
            turbaOutputModel = TurbaOutputModel.getInstance();
            loadPointDataModel = LoadPointDataModel.getInstance(); 
            preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        }

        public void ErgResultsCheckBCD1190()
        {
            if (ErgCheckExhaust1190() && ErgCheckDetaTGBCWheelChamberPTBendingBCD1190() && ErgCheckNozzlesSectionBCD1190() && ErgCheckLoadPointsBCD1190())
            {
                Console.WriteLine("Turbine looks great !! Let's Compare cu_Power_KNN.Power With HBD..");
                Logger("Comparing cu_Power_KNN.Power With HBD..");
            }
        }

        public void ErgResultsCheckBCD1190_Custom(int maxlp  = 0)
        {
            if (ErgCheckExhaust1190_Custom(maxlp) && ErgCheckDetaTGBCWheelChamberPTBendingBCD1190_Custom(maxlp) && ErgCheckThrustValue1190_Custom(maxlp) && ErgCheckNozzlesSectionBCD1190_Custom(maxlp) && ErgCheckLoadPointsBCD1190_Custom(maxlp))
            {
                Console.WriteLine("Turbine looks great !! Let's Compare cu_Power_KNN.Power With HBD..");
                Logger("Comparing cu_Power_KNN.Power With HBD..");
            }
        }

        public bool ErgCheckExhaust1190()
        {
            bool result = false;

            if (Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) <= 100)
            {
                if (Exhaust_1(Convert.ToDouble(turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature), Convert.ToDouble(loadPointDataModel.LoadPoints[2].BackPress)))
                {
                    result = true;
                    Console.WriteLine("Exhaust cu_Customer_Input_Handler.Check Passed...");
                    Logger("Exhaust cu_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Console.WriteLine("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                    Logger("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                }
            }
            else if (Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) <= 210 && Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) > 100)
            {
                if (Exhaust_2(Convert.ToDouble(turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature), Convert.ToDouble(loadPointDataModel.LoadPoints[2].BackPress)))
                {
                    result = true;
                    Console.WriteLine("Exhaust cu_Customer_Input_Handler.Check Passed...");
                    Logger("Exhaust cu_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Console.WriteLine("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                    Logger("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                    MainExecuted("BCD1190");
                }
            }
            else
            {
                Console.WriteLine("Delta T is out of Range (>200)");
                Logger("Delta T is out of Range (>200)");
            }

            return result;
        }

        public bool ErgCheckExhaust1190_Custom(int maxlp = 0)
        {
            bool result = false;

            if (Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) <= 100)
            {
                if (Exhaust_1(Convert.ToDouble(turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature), Convert.ToDouble(loadPointDataModel.LoadPoints[2].BackPress)))
                {
                    result = true;
                    Console.WriteLine("Exhaust cu_Customer_Input_Handler.Check Passed...");
                    Logger("Exhaust cu_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Console.WriteLine("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                    Logger("WARNING !! Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                }
            }
            else if (Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) <= 210 && Convert.ToDouble(turbaOutputModel.OutputDataList[0].DELTA_T) > 100)
            {
                if (Exhaust_2(Convert.ToDouble(turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature), Convert.ToDouble(loadPointDataModel.LoadPoints[2].BackPress)))
                {
                    result = true;
                    Console.WriteLine("Exhaust cu_Customer_Input_Handler.Check Passed...");
                    Logger("Exhaust cu_Customer_Input_Handler.Check Passed...");
                }
                else
                {
                    Console.WriteLine("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                    Logger("Check Failed, cu_Exhaust_Curve.Exhaust not optimal...");
                }
            }
            else
            {
                Console.WriteLine("Delta T is out of Range (>200)");
                Logger("Delta T is out of Range (>200)");
            }

            return result;
        }

        public bool ErgCheckNozzlesSectionBCD1190()
        {
            return RuleEngineAlgorithmForNozzles();
        }

        public bool ErgCheckNozzlesSectionBCD1190_Custom(int maxlp = 0)
        {
            return RuleEngineAlgorithmForNozzles();
        }

        public bool ErgCheckLoadPointsBCD1190()
        {
            bool result = false;
            bool turbaRun = false;

            if ((turbaOutputModel.Stage_Pressure_Check == "TRUE") ? true : false)
            {
                Logger("Stages pressure are in limit For all cu_ERG_RsminHandler.load points..");
                result = true;
            }
            else
            {
                 string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;;//turbaResults_ERG.Range["O10"].Value.ToString();
                 string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;;//turbaResults_ERG.Range["O11"].Value.ToString();
                 string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;//turbaResults_ERG.Range["O5"].Value.ToString(); 
                if (mcr2Status == "FALSE" || mcr1Status == "FALSE")
                {
                    Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                    IncMassFlow(7, 5);
                    IncMassFlow(8, 5);
                    turbaRun = true;
                }

                if (highBpStatus == "FALSE")
                {
                    Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                    ReduceBP(2, 2);
                    turbaRun = true;
                }

                if (turbaRun)
                {
                    PrepareDATFile_OnlyLPUpdate();
                    LaunchTurba();
                    ErgResultsCheckBCD1190();
                    return result;
                }

                double mcrPower = Convert.ToDouble(turbaOutputModel.OutputDataList[7].Power_KW);
                double basePower = Convert.ToDouble(turbaOutputModel.OutputDataList[0].Power_KW);//Convert.ToDouble(turbaResults_ERG.Cells["Q2"].Value);
            // Excel.Range cellRange = turbaResults_ERG.Range["Q4:Q8"];
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;;

                for (int i = 1; i <= 5; ++i){
                double loadPointPower = lpList[i].Power_KW;
                if (loadPointPower < mcrPower)
                {
                        Logger("Base power is less than MCR Case.. Going to Custom Path");
                        return result;
                    }
                    else
                    {
                        Logger("Base power greater than MCR Case..");
                        result = true;
                        turbaOutputModel.Check_Power_KW = "TRUE";
                    }
                }
            }

            return result;
        }

        public bool ErgCheckLoadPointsBCD1190_Custom(int maxlp =0)
        {
            bool result = false;
            bool turbaRun = false;

            if ( (turbaOutputModel.Stage_Pressure_Check == "TRUE") ? true : false)
            {
                Logger("Stages pressure are in limit For all cu_ERG_RsminHandler.load points..");
                result = true;
            }
            else
            {
                string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;
                string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;
                string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;

                if (mcr2Status == "FALSE" || mcr1Status == "FALSE")
                {
                    Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                    IncMassFlow(7, 5);
                    IncMassFlow(8, 5);
                    turbaRun = true;
                }

                if (highBpStatus == "False")
                {
                    Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                    ReduceBP(2, 2);
                    turbaRun = true;
                }

                if (turbaRun)
                {
                    PrepareDATFile_OnlyLPUpdate(maxlp);
                    LaunchTurba(maxlp);
                CustomNozzleOptimizer.GNozzleCount = 0;
                CustomNozzleOptimizer.Na = 0;
                CustomNozzleOptimizer.Nb = 0;
                ErgResultsCheckBCD1190_Custom(maxlp);
                    return result;
                }

                double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;
                double basePower = turbaOutputModel.OutputDataList[0].Power_KW;

                List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
            for (int i = 1; i <= 5; ++i){
                double loadPointPower = lpList[i].Power_KW;
                if (loadPointPower < mcrPower)
                {
                        Logger("Base power is less than MCR Case.. Going to Custom Path");
                        return result;
                    }
                    else
                    {
                        Logger("Base power greater than MCR Case..");
                        result = true;
                        turbaOutputModel.Check_Power_KW = "TRUE";
                    }
                }
            }

            return result;
        }

        public bool ErgCheckThrustValue1190()
        {
            bool result = false;
            LaunchRsmin();

            double thrustUpperLim = 1.4;
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;

             for (int i = 1; i <= maxLoadPoints; ++i)
            {
            
                double thrust = lpList[i].Thrust;

                if (thrust > thrustUpperLim)
                {
                    Logger("Thrust check Failed....");
                    turbaOutputModel.Check_Thrust = "FALSE";
                    return result;
                }
                else
                {
                    result = true;
                }
            }

            if (result)
            {
                Logger("Thrust check Passed....");
                turbaOutputModel.Check_Thrust = "TRUE";
            }
            else
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
            }

            return result;
        }

        public bool ErgCheckThrustValue1190_Custom(int maxlp =0)
        {
            bool result = false;
            LaunchRsmin();

            double thrustUpperLim = 1.4;
            bool thrustResult = false;

           List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;

             for (int i = 1; i <= maxLoadPoints; ++i)
            {
            
                double thrust = lpList[i].Thrust;

                if (thrust > thrustUpperLim)
                {
                    Logger("Thrust check Failed....");
                    turbaOutputModel.Check_Thrust = "FALSE";
                    return result;
                }
                else
                {
                    result = true;
                    thrustResult = true;
                }
            }

            if (thrustResult)
            {
                Logger("Thrust check Passed....");
turbaOutputModel.Check_Thrust = "TRUE";          
              }
            else
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
            }

            return result;
        }

        public bool ErgCheckDetaTGBCWheelChamberPTBendingBCD1190()
        {
            bool result = false;
            bool datChanged = false;

            double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;
            double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;

            double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;
            double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit;
            double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;

            double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;
            double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;
            double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;
            double wheelchamberTupperLim_2 = turbaOutputModel.WheelchamberT_UpperLimit2;

            string bending = Convert.ToString(turbaOutputModel.OutputDataList[0].Bending);

            bool bendingStatus = false;
            if (!string.IsNullOrEmpty(bending))
            {
                Logger("Bending check Failed..");
                turbaOutputModel.BendingCheck  = "FALSE";
                // turbaResults_ERG.Range["N3"].Value = false;
                bendingStatus = false;
            }
            else
            {
                Logger("Bending check Passed..");
                turbaOutputModel.BendingCheck  = "TRUE";
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
                Logger("Going to Executed/Custom Path");
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                // MainExecuted("BCD1190");
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                return false;
            }
            // bool deltaTStatus = deltaT <= deltaTupperLim;


//             if (!bendingStatus || !deltaTStatus)
//             {
//                 Logger("Going to Executed/Custom Path due to Bending and/or Delta failure");
//                 return result;
//             }
            bool wheelchamberPStatus = false;
            bool wheelchamberTStatus = false;
            if (preFeasibilityDataModel.TemperatureActualValue < 500)
            {
                // ExeCwCurve
                // ExeCwCurve
                if (!WheelChamberWithCW_1(wheelchamberT, wheelchamberP))
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
                if (!WheelChamberWithCW_2(wheelchamberT, wheelchamberP))
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
                MainExecuted("BCD1190");
                return false;
            }    

            return result;
        }

        public bool ErgCheckDetaTGBCWheelChamberPTBendingBCD1190_Custom(int maxlp =0)
        {
            bool result = true;
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
                Logger("Going to Executed/Custom Path");
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                // MainExecuted("BCD1190");
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
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
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                // MainExecuted("BCD1190");
                Logger("Going to Executed/Custom Path due to Bending and/or Delta failiure");
                return false;
            }
        }
            bool wheelchamberPStatus = false;
            bool wheelchamberTStatus = false;
            if (preFeasibilityDataModel.TemperatureActualValue < 500)
            {
                // ExeCwCurve
                // ExeCwCurve
                if (!WheelChamberWithCW_1(wheelchamberT, wheelchamberP))
                {
                    wheelchamberPStatus = true;
                }
                else
                {
                    wheelchamberPStatus = false;
                turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
                    Logger("Wheel Chamber Temp and Pressure check Passed...");
                }
            }
            else
            {
                if (!WheelChamberWithCW_2(wheelchamberT, wheelchamberP))
                {
                    wheelchamberPStatus = true;
                }
                else
                {
                    wheelchamberPStatus = false;
                turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
                Logger("Wheel Chamber Temp and Pressure check Passed...");
                }
            }

            return result;
        }
        public  void IncMassFlow(int cell, int percentage)
        {
            CustomLoadPointGenerator customLoadPointGenerator = new CustomLoadPointGenerator();
            customLoadPointGenerator.LoadPointGenerator_IncMassFlow(cell, percentage);
            // Implement your IncMassFlow logic here
            Console.WriteLine($"Increasing mass flow by {percentage}% at {cell}");
        }

        public  void ReduceBP(int cell, int percentage)
        {
            CustomLoadPointGenerator customLoadPointGenerator = new CustomLoadPointGenerator();
            customLoadPointGenerator.LoadPointGenerator_ReduceBP(cell, percentage);
            // Implement your ReduceBP logic here
            Console.WriteLine($"Reducing back pressure by {percentage}% at {cell}");
        }
        public bool ErgCheckExhaustVolumetricFlowBCD1190()
        {
            bool result = false;

            double volFlow = turbaOutputModel.OutputDataList[0].Vol_Flow;
            int currentFlowPathNo = preFeasibilityDataModel.Variant;
            double volFlowUpperLimit = turbaOutputModel.VolFlow_UpperLimit;

            if (volFlow <= volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow = "TRUE";
                Logger("volFlow is in acceptable range...continuing with 1190");
                result = true;
            }
            else
            {
                Logger("Go to 2GBC path");
            }

            return result;
        }
        public  bool Exhaust_1(double ab2, double g6)
        {
            // Implement your Exhaust_1 logic here
            // return true;
            return ExhaustFunctions.Exhaust1(ab2,g6);
        }

        public  bool Exhaust_2(double ab2, double g6)
        {
            // Implement your Exhaust_2 logic here
            // return true;
            return ExhaustFunctions.Exhaust2(ab2,g6);
        }

        private void Logger(string message)
        {
            // Implement your logging mechanism here
            logger.LogInformation(message);
            Console.WriteLine(message);
        }

        private void MainExecuted(string path)
        {
            // Implement your main executed logic here
            Console.WriteLine($"Executing main path: {path}");
        }

        private void PrepareDATFile()
        {
            CustomDATFileProcessor   customDATFileProcessor= new CustomDATFileProcessor();
            customDATFileProcessor.PrepareDatFile();
            // Implement your DAT file preparation logic here
            Console.WriteLine("Preparing DAT file...");
        }

        private void PrepareDATFile_OnlyLPUpdate(int maxlp =0)
        {
            CustomDATFileProcessor customDATFileProcessor= new CustomDATFileProcessor();
            customDATFileProcessor.PrepareDatFileOnlyLPUpdate(maxlp);
            // Implement your DAT file preparation logic for only LP update here
            Console.WriteLine("Preparing DAT file for only LP update...");
        }

        private void LaunchTurba(int maxlp = 0)
        {
            CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
            cuTurbaAutomation.LaunchTurba(maxlp);
            // Implement your Turba launch logic here
            Console.WriteLine("Launching Turba...");
        }

        private void LaunchRsmin()
        {
            CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
            cuTurbaAutomation.LaunchRsmin();
            // Implement your Rsmin launch logic here
            Console.WriteLine("Launching Rsmin...");
        }
        public  bool WheelChamberWithCW_1(double g2, double f2)
        {
            // Implement your WheelChamberWithCW_1 logic here
            // return true;
            return WheelChamber.WheelChamberWithCW_1(g2, f2);
        }

        public  bool WheelChamberWithCW_2(double g2, double f2)
        {
            // Implement your WheelChamberWithCW_2 logic here
            return WheelChamber.WheelChamberWithCW_2(g2, f2);
            // return true;
        }
        public  bool RuleEngineAlgorithmForNozzles()
        {
            // Implement your RuleEngineAlgorithmForNozzles logic here
            // return true;
            CustomNozzleOptimizer customNozzleOptimizer = new CustomNozzleOptimizer();
            return customNozzleOptimizer.RuleEngineAlgorithmForNozzles();
        }
    }


    