// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Models.PreFeasibility;
// using Models.TurbaOutputDataModel;
// using HMBD.Exec_Ref_DAT_Selector;
// using Handlers.Exec_DAT_Handler;
// namespace Checks.Exec_ERG_Verification;
// using Microsoft.Extensions.Configuration;

// class ExecERGVerification
// {     
//     TurbaOutputModel turbaOutputModel;
//     PreFeasibilityDataModel preFeasibilityDataModel;
//     int maxLoadPoints = 10;
//             private IConfiguration configuration;

//     public ExecERGVerification(){
//         turbaOutputModel = TurbaOutputModel.getInstance();
//         preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
//         configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
//         maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
//     }
//          void ergResultsCheck()
//         {
//             if (ergCheck_ExhaustVolumetricFlow())
//             {
//                 if (ergCheck_detaTGBCwheelchamberPTbending())
//                 {
//                     if (ergCheck_thrustValue())
//                     {
//                         if (ergCheck_LoadPoints())
//                         {
//                             if (ergCheck_NozzlesSection())
//                             {
//                                 Console.WriteLine("Turbine looks great !! Let's Compare ex_Power_KNN.Power With HBD..");
//                             }
//                         }
//                     }
//                 }
//             }
//         }

//          bool ergCheck_NozzlesSection()
//         {
//             // Consider NOzzle looks fine
//             bool ergCheck_NozzlesSection = true;
//             Excel.Worksheet turbaResults_ERG = Globals.ThisWorkbook.Worksheets["Output"];
//             int LpRange = turbaResults_ERG.Cells[turbaResults_ERG.Rows.Count, "D"].End(Excel.XlDirection.xlUp).Row;
//             string LpRangeStr = "D4:D" + LpRange;
//             double nozzleAreaDeviationLowerLim = turbaResults_ERG.Range["E30"].Value;
//             double nozzleAreaDeviationUpperLim = turbaResults_ERG.Range["E31"].Value;

//             double nozzleAreaDeviation = turbaResults_ERG.Range["D4"].Value * 100;
//             Logger("ABWEICHUNG: " + nozzleAreaDeviation + "%");
//             if (nozzleAreaDeviation >= nozzleAreaDeviationLowerLim && nozzleAreaDeviation <= nozzleAreaDeviationUpperLim)
//             {
//                 Logger("Nozzle area within Range...");
//                 turbaResults_ERG.Range["D3"].Value = true;
//             }
//             else
//             {
//                 Logger("Nozzle area deviation Not in Range...");
//                 turbaResults_ERG.Range["D3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }

//             double nozzleAreaGroup1 = turbaResults_ERG.Range["H2"].Value;
//             double nozzleAreaUpperLim = turbaResults_ERG.Range["I31"].Value;
//             Logger("Nozzle are G1: " + nozzleAreaGroup1 + " Limit:" + nozzleAreaUpperLim);
//             if (nozzleAreaGroup1 > nozzleAreaUpperLim)
//             {
//                 Logger("nozzle Area Group1 ex_ERG_PowerMatch.check Failed..");
//                 turbaResults_ERG.Range["H3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 Logger("nozzle Area Group1 ex_ERG_PowerMatch.check Passed..");
//                 turbaResults_ERG.Range["H3"].Value = true;
//             }

//             double nozzleAreaGroup2 = turbaResults_ERG.Range["I2"].Value;
//             nozzleAreaUpperLim = turbaResults_ERG.Range["J31"].Value;
//             Logger("Nozzle are G1: " + nozzleAreaGroup2 + " Limit:" + nozzleAreaUpperLim);
//             if (nozzleAreaGroup2 > nozzleAreaUpperLim)
//             {
//                 Logger("nozzle Area Group2 ex_ERG_PowerMatch.check Failed..");
//                 turbaResults_ERG.Range["I3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 Logger("nozzle Area Group2 ex_ERG_PowerMatch.check Passed..");
//                 turbaResults_ERG.Range["I3"].Value = true;
//             }

//             double nozzleFmin1 = turbaResults_ERG.Range["K2"].Value;
//             double nozzleFmin2 = turbaResults_ERG.Range["L2"].Value;
//             Logger("Nozzle numbers: " + nozzleFmin1 + " " + nozzleFmin2);
//             if (nozzleFmin1 < nozzleFmin2)
//             {
//                 Logger("Nozzles: Failed (FMIN1 <> FMIN2) ..");
//                 turbaResults_ERG.Range["K3"].Value = false;
//                 turbaResults_ERG.Range["L3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 Logger("Nozzles: OK (FMIN1 <> FMIN2) ..");
//                 turbaResults_ERG.Range["K3"].Value = true;
//                 turbaResults_ERG.Range["L3"].Value = true;
//             }

//             double admissionFactor = turbaResults_ERG.Range["S2"].Value;
//             double admissionFactorLim = turbaResults_ERG.Range["Q30"].Value;
//             Logger("Nozzle admission factor1: " + admissionFactor + " Limit:" + admissionFactorLim);
//             if (admissionFactor > admissionFactorLim)
//             {
//                 Logger("Nozzles admission factor Failed - Group 1 ..");
//                 turbaResults_ERG.Range["S3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 turbaResults_ERG.Range["S3"].Value = true;
//             }

//             admissionFactor = turbaResults_ERG.Range["T2"].Value;
//             admissionFactorLim = turbaResults_ERG.Range["R30"].Value;
//             Logger("Nozzle admission factor2: " + admissionFactor + " Limit:" + admissionFactorLim);
//             if (admissionFactor > admissionFactorLim)
//             {
//                 Logger("Nozzles admission factor Failed - Group 2 ..");
//                 turbaResults_ERG.Range["T3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 turbaResults_ERG.Range["T3"].Value = true;
//             }

//             admissionFactor = turbaResults_ERG.Range["U2"].Value;
//             admissionFactorLim = turbaResults_ERG.Range["S30"].Value;
//             Logger("Nozzle admission factor3: " + admissionFactor + " Limit:" + admissionFactorLim);
//             if (admissionFactor > admissionFactorLim)
//             {
//                 Logger("Nozzles admission factor Failed - Total ..");
//                 turbaResults_ERG.Range["U3"].Value = false;
//                 ergCheck_NozzlesSection = false;
//                 goto NozzleCorrector;
//             }
//             else
//             {
//                 turbaResults_ERG.Range["U3"].Value = true;
//             }

//         NozzleCorrector:
//             if (!ergCheck_NozzlesSection)
//             {
//                 Logger("START NOZZLE OPTIMIZER ");
//                 // Call ex_ERG_NozzleOptimizer.GeneticAlgorithmForNozzles
//                 ex_ERG_NozzleOptimizer.RuleEngineAlgorithmForNozzles();
//             }
//             else
//             {
//                 Logger("Nozzles checks are passed...");
//                 Logger("=========================================");
//                 Logger("START Comparing ex_Power_KNN.Power With HBD..");
//             }

//             return ergCheck_NozzlesSection;
//         }

//          bool ergCheck_LoadPoints()
//         {
//             Excel.Worksheet turbaResults_ERG = Globals.ThisWorkbook.Worksheets["Output"];
//             bool ergCheck_LoadPoints = false;
//             bool Turbarun = false;

//             bool overallPressureStatus = turbaResults_ERG.Range["O3"].Value;
//             if (overallPressureStatus)
//             {
//                 Logger("Stages pressure are in limit For all ex_ERG_RsminHandler.load points..");
//                 goto CheckPower;
//             }

//             string mcr1Status = turbaResults_ERG.Range["O10"].Value.ToString();
//             string mcr2Status = turbaResults_ERG.Range["O11"].Value.ToString();
//             string highBpStatus = turbaResults_ERG.Range["O5"].Value.ToString();

//             if (mcr2Status == "False" || mcr1Status == "False")
//             {
//                 Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
//                 loadPointGenerator_IncMassFlow("B11", 5);
//                 loadPointGenerator_IncMassFlow("B12", 5);
//                 Turbarun = true;
//             }

//             if (highBpStatus == "False")
//             {
//                 Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
//                 loadPointGenerator_ReduceBP("B6", 2);
//                 Turbarun = true;
//             }

//             if (Turbarun)
//             {
//                 prepareDATFile_OnlyLPUpdate();
//                 LaunchTurba();
//                 ergResultsCheck();
//                 return ergCheck_LoadPoints;
//             }

//         CheckPower:
//             double mcrPower = turbaResults_ERG.Range["Q10"].Value;
//             double basePower = turbaResults_ERG.Range["Q2"].Value;
//             Excel.Range cellRange = turbaResults_ERG.Range["Q4:Q8"];
//             foreach (Excel.Range rRow in cellRange.Cells)
//             {
//                 double loadPointPower = rRow.Value;
//                 if (loadPointPower < mcrPower)
//                 {
//                     Logger("MCR Failed. MCR kW > Base LP Power");
//                     TerminateIgniteX("ergCheck_LoadPoints");
//                     goto ExitSub;
//                 }
//                 else
//                 {
//                     Logger("MCR passed. MCR kW < Base LP Power");
//                     ergCheck_LoadPoints = true;
//                     turbaResults_ERG.Range["Q3"].Value = true;
//                 }
//             }

//         ExitSub:
//             return ergCheck_LoadPoints;
//         }

//         bool ergCheck_thrustValue()
//         {
//             bool ergCheck_thrustValue = true;
//             LaunchRsmin();
//             double thrustUpperLim = 1.4;
//             // Excel.Worksheet turbaResults_ERG = Globals.ThisWorkbook.Worksheets["Output"];
//             // Excel.Range LoadPointThrust = turbaResults_ERG.Range["P4:P13"];
//             string LogThrust = "";
//             for(int i=0;i<maxLoadPoints;i++)
//             {
//                 double Thrust = turbaOutputModel.OutputDataList[i].Thrust;
//                 LogThrust += " | " + Thrust;
//                 if (Thrust > thrustUpperLim)
//                 {
//                     ergCheck_thrustValue = false;
//                     Logger("Thrust: " + LogThrust);
//                     Logger("Thrust ex_ERG_PowerMatch.check Failed....");
//                     turbaOutputModel.Check_Thrust= "FALSE";
//                     // turbaResults_ERG.Range["P3"].Value = false;
//                     return ergCheck_thrustValue;
//                 }
//                 else
//                 {
//                     ergCheck_thrustValue = true;
//                 }
//             }
//             Logger("Thrust: " + LogThrust);

//             if (ergCheck_thrustValue)
//             {
//                 Logger("Thrust ex_ERG_PowerMatch.check Passed....");
//                 turbaOutputModel.Check_Thrust="TRUE";
//                 // turbaResults_ERG.Range["P3"].Value = true;
//             }
//             else
//             {
//                 Logger("Thrust ex_ERG_PowerMatch.check Failed....");
//                 turbaOutputModel.Check_Thrust = "FALSE";
//                 // turbaResults_ERG.Range["P3"].Value = false;
//                 TerminateIgniteX("ergCheck_thrustValue");
//             }

//             return ergCheck_thrustValue;
//         }

//         bool ergCheck_detaTGBCwheelchamberPTbending()
//         {
//             bool ergCheck_detaTGBCwheelchamberPTbending = false;
//             bool datChanged = false;
//             // Excel.Worksheet turbaResults_ERG = Globals.ThisWorkbook.Worksheets["Output"];

//             double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//Convert.ToDouble(turbaResults_ERG.Cells["E2"].Value);
//         double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["F31"].Value);
//         Logger("Delta T: " + deltaT + " UpperLimit: " + deltaTupperLim);

//         double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure; //Convert.ToDouble(turbaResults_ERG.Cells["F2"].Value);
//         double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit; //Convert.ToDouble(turbaResults_ERG.Cells["G31"].Value);
//         double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["G32"].Value);
//         Logger("Wheel Chamber Pressure: " + wheelchamberP + ", Limits: " + wheelchamberPupperLim + " " + wheelchamberPupperLim_2);

//         double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(turbaResults_ERG.Cells["G2"].Value);
//         double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H30"].Value);
//         double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H31"].Value);
//         double wheelchamberTupperLim_2 = turbaOutputModel.WheelchamberT_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["H32"].Value);
//         Logger("Wheel Chamber Temp: " + wheelchamberT + ", Limits: " + wheelchamberTlowerLim + " " + wheelchamberTupperLim + " " + wheelchamberTupperLim_2);

//             string Bending = turbaOutputModel.OutputDataList[0].Bending;//turbaResults_ERG.Range["N2"].Value.ToString();
//             bool bendingStatus = true;
//             bool deltaTStatus = true;
//             bool wheelchamberPStatus = true;
//             bool wheelchamberTStatus = true;
//             bool wheelchamberP_executedPath = false;
//             bool wheelchamberT_executedPath = false;
//             bool wheelchamberP_standardPath = false;
//             bool wheelchamberT_standardPath = false;

//             if (!string.IsNullOrEmpty(Bending))
//             {
//                 Logger("Bending ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.BendingCheck="FALSE";
//                 // turbaResults_ERG.Range["N3"].Value = false;
//                 bendingStatus = false;
//             }
//             else
//             {
//                 Logger("Bending ex_ERG_PowerMatch.check Passed..");
//                 turbaOutputModel.BendingCheck = "TRUE";
//                 // turbaResults_ERG.Range["N3"].Value = true;
//             }

//             if (deltaT > deltaTupperLim)
//             {
//                 Logger("deltaT GBC ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_DELTA_T="FALSE";
//                 // turbaResults_ERG.Range["E3"].Value = false;
//                 deltaTStatus = false;
//             }
//             else
//             {
//                 Logger("deltaT GBC ex_ERG_PowerMatch.check Passed..");
//                 turbaOutputModel.Check_DELTA_T = "TRUE";
//                 // turbaResults_ERG.Range["E3"].Value = true;
//             }

//             if (!(bendingStatus && deltaTStatus))
//             {
//                 Logger("Selecting New flow path from ex_Ref_PathExecutedProjects.executed projects..");
//                 ex_Main_Executed.MainExecuted("BCD1120");
//                 return ergCheck_detaTGBCwheelchamberPTbending;
//             }

//             if (wheelchamberP > wheelchamberPupperLim_2)
//             {
//                 wheelchamberP_executedPath = true;
//                 Logger("wheelchamber ex_Power_KNN.P ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Pressure="FALSE";
//                 // turbaResults_ERG.Range["F3"].Value = false;
//                 wheelchamberPStatus = false;
//             }
//             else if (wheelchamberP > wheelchamberPupperLim)
//             {
//                 wheelchamberP_standardPath = true;
//                 Logger("wheelchamber ex_Power_KNN.P ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Pressure = "FALSE";
//                 // turbaResults_ERG.Range["F3"].Value = false;
//                 wheelchamberPStatus = false;
//             }
//             else
//             {
//                 Logger("wheelchamber ex_Power_KNN.P ex_ERG_PowerMatch.check Passed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
//                 // turbaResults_ERG.Range["F3"].Value = true;
//             }

//             if (wheelchamberT > wheelchamberTupperLim_2)
//             {
//                 wheelchamberT_executedPath = true;
//                 Logger("wheelchamber Temp ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
//                 // turbaResults_ERG.Range["G3"].Value = false;
//                 wheelchamberTStatus = false;
//             }
//             else if (wheelchamberT > wheelchamberTupperLim)
//             {
//                 wheelchamberT_standardPath = true;
//                 Logger("wheelchamber Temp ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
//                 // turbaResults_ERG.Range["G3"].Value = false;
//                 wheelchamberTStatus = false;
//             }
//             else if (wheelchamberT > wheelchamberTlowerLim)
//             {
//                 Logger("wheelchamber Temp ex_ERG_PowerMatch.check Passed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Temperature = "TRUE";
//                 // turbaResults_ERG.Range["G3"].Value = true;
//             }
//             else
//             {
//                 wheelchamberT_standardPath = true;
//                 Logger("wheelchamber Temp ex_ERG_PowerMatch.check Failed..");
//                 turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
//                 // turbaResults_ERG.Range["G3"].Value = false;
//                 wheelchamberTStatus = false;
//             }

//             if (wheelchamberP_executedPath || wheelchamberT_executedPath)
//             {
//                 Logger("Selecting New flow path from ex_Ref_PathExecutedProjects.executed projects..");
//                 ex_Main_Executed.MainExecuted("BCD1120");
//                 return ergCheck_detaTGBCwheelchamberPTbending;
//             }

//             if (wheelchamberP_standardPath || wheelchamberT_standardPath)
//             {
//                 Logger("Selecting New flow path from standard variants..");
//                 FlowPathSelector flowPathSelector = new FlowPathSelector();
//                 string path = flowPathSelector.SelectStandard("NextLarger");
//                 if (path == "No Dat Found")
//                 {
//                     Logger("Stopping Execution : End of higher variant selection");
//                     ex_HELPER_FUNC.TerminateIgniteX("ergCheck_detaTGBCwheelchamberPTbending");
//                     return ergCheck_detaTGBCwheelchamberPTbending;
//                 }
//                 flowPathSelector.CopyRefDATFile(path);
//                 datChanged = true;
//                 goto ExitSub;
//             }

//         ExitSub:
//             if (datChanged)
//             {
                
//                 ex_DAT_Handler.prepareDATFile();
//                 ex_TURBA_Interface.LaunchTurba();
//                 ergResultsCheck();
//                 ergCheck_detaTGBCwheelchamberPTbending = false;
//             }
//             else
//             {
//                 ergCheck_detaTGBCwheelchamberPTbending = true;
//             }
//             ergCheck_detaTGBCwheelchamberPTbending=true;
//             return ergCheck_detaTGBCwheelchamberPTbending;
//         }

//         public bool ergCheck_ExhaustVolumetricFlow()
//          {
//             bool ergCheck_ExhaustVolumetricFlow = false;
//             // Excel.Worksheet turbaResults_ERG = Globals.ThisWorkbook.Worksheets["Output"];
//             // Excel.Worksheet currentFlowPath = Globals.ThisWorkbook.Worksheets["Pre-Feasibility checks"];
           
//             double VolFlow= turbaOutputModel.OutputDataList[0].Vol_Flow;
//             // double VolFlow = turbaResults_ERG.Range["M2"].Value;
//             Logger("Volumetric Flow: " + VolFlow);
//             int currentFlowPathNo = (int) preFeasibilityDataModel.Variant;//currentFlowPath.Range["G15"].Value;

//             switch (currentFlowPathNo)
//             {
//                 case int n when (n < 3):
//                     double volFlowUpperLimit = turbaOutputModel.VolFlow_LowerLimit; //turbaResults_ERG.Range["N30"].Value;
//                     Logger("Volumetric Flow limit: " + volFlowUpperLimit);
//                     if (VolFlow > volFlowUpperLimit)
//                     {
//                         turbaOutputModel.CheckVolFlow="FALSE";
//                         // turbaResults_ERG.Range["M3"].Value = false;
//                         Logger("Opt2 VolFlow failed, Selecting project from Opt1..");
//                         FlowPathSelector flowPathSelector = new FlowPathSelector();
//                         // flowPathSelector.selectStandard()
//                         string path = flowPathSelector.SelectStandard("Opt2");// ex_Ref_DAT_Selector.SelectStandard("Opt2");
//                         Logger("Variant selected: " + preFeasibilityDataModel.Variant + ",path:" + path);
//                         flowPathSelector.CopyRefDATFile(path);
//                         // ex_Ref_DAT_Selector.copyRefDATFile(path);
//                         DatFileHandler datFileHandler = new DatFileHandler();
//                         datFileHandler.PrepareDatFile();
//                         // calling turba
//                         ex_TURBA_Interface.LaunchTurba();
//                         ergResultsCheck();
//                         return ergCheck_ExhaustVolumetricFlow;
//                     }
//                     else
//                     {
//                         turbaOutputModel.CheckVolFlow = "TRUE";
//                         // turbaResults_ERG.Range["M3"].Value = true;
//                         Logger("volFlow is in acceptable range..");
//                         ergCheck_ExhaustVolumetricFlow = true;
//                         return ergCheck_ExhaustVolumetricFlow;
//                     }

//                 case int n when (n >= 3):
//                     volFlowUpperLimit =  turbaOutputModel.VolFlow_UpperLimit;//turbaResults_ERG.Range["N31"].Value;
//                     Logger("Volumetric Flow limit: " + volFlowUpperLimit);
//                     if (VolFlow > volFlowUpperLimit)
//                     {
//                         turbaOutputModel.CheckVolFlow="FALSE";
//                         // turbaResults_ERG.Range["M3"].Value = false;
//                         Logger("Opt1 VolFlow failed, Design requires atleast 2GBC..");
//                         Console.WriteLine("Terminating execution, Opt1 VolFlow failed, Design requires atleast 2GBC..");
//                         return ergCheck_ExhaustVolumetricFlow;
//                     }
//                     else
//                     {
//                         turbaOutputModel.CheckVolFlow="TRUE";
//                         // turbaResults_ERG.Range["M3"].Value = true;
//                         Logger("volFlow is in acceptable range..");
//                         ergCheck_ExhaustVolumetricFlow = true;
//                         return ergCheck_ExhaustVolumetricFlow;
//                     // default:
//                     // Logger("Invalid flow path number.");
//                     // return ergCheck_ExhaustVolumetricFlow;
//                     }
//                     default:
//                          Logger("Invalid flow path number.");
//                          return false;
//                     // return ergCheck_ExhaustVolumetricFlow;
//             }
//         }
//         static void Logger(string message)
//         {
//             Console.WriteLine(message);
//         }

//         static void loadPointGenerator_IncMassFlow(string cell, int percentage)
//         {
//             // Implement the logic to increase mass flow by the given percentage
//         }

//         static void loadPointGenerator_ReduceBP(string cell, int percentage)
//         {
//             // Implement the logic to reduce back pressure by the given percentage
//         }

//         static void prepareDATFile_OnlyLPUpdate()
//         {
//             // Implement the logic to prepare DAT file with only load point updates
//         }

//         static void LaunchTurba()
//         {
//             // Implement the logic to launch Turba
//         }

//         static void TerminateIgniteX(string functionName)
//         {
//             // Implement the logic to terminate IgniteX with the given function name
//         }

//         static void LaunchRsmin()
//         {
//             // Implement the logic to launch Rsmin
//         }

//     static class ex_ERG_NozzleOptimizer
//     {
//         public static void RuleEngineAlgorithmForNozzles()
//         {
//             // Implement the logic for the rule engine algorithm for nozzles
//         }
//     }

//     static class ex_Main_Executed
//     {
//         public static void MainExecuted(string parameter)
//         {
//             // Implement the logic for the main executed function with the given parameter
//         }
//     }

//     static class ex_Ref_DAT_Selector
//     {
//         public static string SelectStandard(string option)
//         {
//             // Implement the logic to select the standard DAT file based on the given option
//             return "path_to_standard_dat_file";
//         }

//         public static void copyRefDATFile(string path)
//         {
//             // Implement the logic to copy the reference DAT file from the given path
//         }
//     }

//     static class ex_DAT_Handler
//     {
//         public static void prepareDATFile()
//         {
//             DatFileHandler datFileHandler = new DatFileHandler();
//             datFileHandler.PrepareDatFile();
//             // Implement the logic to prepare the DAT file
//         }
//     }

//     static class ex_TURBA_Interface
//     {
//         public static void LaunchTurba()
//         {
//             // Implement the logic to launch Turba
//         }
//     }

//     static class ex_HELPER_FUNC
//     {
//         public static void Logger(string message)
//         {
//             Console.WriteLine(message);
//         }

//         public static void TerminateIgniteX(string functionName)
//         {
//             // Implement the logic to terminate IgniteX with the given function name
//         }
//     }

//     // static class ex_ERG_Verification
//     // {
//     //     public static void ergResultsCheck()
//     //     {
//     //         Program.ergResultsCheck();
//     //     }
//     // }
// }
