// using System;
// using System.Linq;
// using Models.PreFeasibility;
// using Models.TurbaOutputDataModel;
// using Models.TurbineData;
// // using Microsoft.Office.Interop.Excel;

// public class PenaltyScoreCalculator
// {
//     // private Worksheet powerNearest;
//     // private Worksheet output;
//     // private Worksheet prefeas;
//     private TurbineDataModel turbineDataModel;
//     private TurbaOutputModel turbaOutputModel;
//     private PreFeasibilityDataModel preFeasibilityDataModel;
//     private string BCD;
//     private string checkType;
//     private bool allFilled;
//     private int lpCount = 10;
//     public PenaltyScoreCalculator(){
//         turbineDataModel = TurbineDataModel.getInstance(); 
//         turbaOutputModel = TurbaOutputModel.getInstance();
//         preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
//     }

//     public double GetPenaltyScore_th()
//     {
//         // powerNearest = (Worksheet)ThisWorkbook.Worksheets["PowerNearest"];
//         // output = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         // prefeas = (Worksheet)ThisWorkbook.Worksheets["Pre-Feasibility checks"];
//         bool G14 = (preFeasibilityDataModel.Decision=="TRUE")?true:false;
//         bool G26 = (preFeasibilityDataModel.Decision_2=="TRUE")?true:false;
//         bool G46 = (preFeasibilityDataModel.Decision_3=="TRUE")?true:false;
//         if (G14)
//         {
//             BCD = "1120";
//         }
//         else if (!G14 && G26)
//         {
//             BCD = "1190";
//         }
//         else if (!G26 && G46)
//         {
//             BCD = "5000";
//         }
//         else
//         {
//             Logger("Prefeasibility check failed...Go To 2 GBC");
//             TerminateIgniteX("Prefeasibility_Check");
//         }

//         if (int.Parse(BCD) >= 1120 && int.Parse(BCD) <= 1130)
//         {
//             checkType = "1120";
//         }
//         else if (int.Parse(BCD) >= 1190 && int.Parse(BCD) <= 1210)
//         {
//             checkType = "1190";
//         }
//         else if (BCD == "5000")
//         {
//             checkType = "1190";
//         }
//         else
//         {
//             Logger("Invalid BCD Type");
//             TerminateIgniteX("getPenaltyScore_th");
//         }

//         allFilled = true;
//         CheckDeltaErrorERG_th(checkType);

//     //     Range checkRange = output.Range["E2,G2,P2,AC2,AE2:AF2"];
//     //     foreach (Range cell in checkRange.Cells)
//     //     {
//     //         if (cell.Value == null)
//     //         {
//     //             allFilled = false;
//     //             break;
//     //         }
//     //     }

//     //     try
//     //     {
//     //         if (allFilled)
//     //         {
//     //             output.Range["H40"].Value = 0;
//     //             output.Range["AA40"].Value = 0;
//     //             return Application.WorksheetFunction.SumProduct(output.Range["D40:AF40"], output.Range["D42:AF42"]);
//     //         }
//     //         else
//     //         {
//     //             output.Range["H40"].Value = 0;
//     //             output.Range["AA40"].Value = 0;
//     //             return 50000;
//     //         }
//     //     }
//     //     catch (Exception)
//     //     {
//     //         TerminateIgniteX("getPenaltyScore_th");
//     //         return 0;
//     //     }
//     }

//     private void CheckDeltaErrorERG_th(string checkType)
//     {
//         if (checkType == "1190")
//         {
//             Logger("Conducting checks for BCD1190");
//             HardCheck_GBCLength1190_th();
//             HardCheck_WheelChamberTemp1190_th();
//             HardCheck_ThrustValue1190_th();
//             HardCheck_Lang1190_th();
//             HardCheck_PSI1190_th();
//         }
//         else if (checkType == "1120")
//         {
//             Logger("Conducting checks for BCD1120");
//             HardCheck_GBCLength1120_th();
//             HardCheck_WheelChamberTemp1120_th();
//             HardCheck_ThrustValue1120_th();
//             HardCheck_Lang1120_th();
//             HardCheck_PSI1120_th();
//         }
//         else
//         {
//             Logger("Invalid Check Type");
//             TerminateIgniteX("CheckDeltaErrorERG_th");
//         }
//     }
//     public void LaunchRsmin(){

//     }

//     private void HardCheck_ThrustValue1190_th()
//     {
//         LaunchRsmin();
//         double thrustUpperLim = 1.4;
//         // Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         // Range loadPointThrust = turbaResults_ERG.Range["P4:P13"];
//         bool thrustCheckPassed = true;
//         double deltaErr_thrust = 0;
//         List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
//         for (int i =1 ;i< lpCount;i++)
//         {
//             double thrust = Math.Abs((double)lpList[i].Thrust);
//             if (thrust > thrustUpperLim)
//             {
//                 Logger("Thrust check Failed....");
//                 // turbaResults_ERG.Range["P3"].Value = false;
//                 turbaOutputModel.Check_Thrust="FALSE";
//                 thrustCheckPassed = false;
//                 deltaErr_thrust = 1;
//                 break;
//             }
//         }

//         if (thrustCheckPassed)
//         {
//             Logger("Thrust check Passed....");
//             turbaOutputModel.Check_Thrust ="TRUE";
//             // turbaResults_ERG.Range["P3"].Value = true;
//             turbaResults_ERG.Range["P2"].Value = true;
//         }
//         else
//         {
//             Logger("Thrust check Failed....");
//             turbaResults_ERG.Range["P3"].Value = false;
//             turbaResults_ERG.Range["P2"].Value = true;
//         }

//         turbaResults_ERG.Range["P40"].Value = deltaErr_thrust;
//     }

//     private void HardCheck_ThrustValue1120_th()
//     {
//         double thrustUpperLim = 1.4;
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         Range loadPointThrust = turbaResults_ERG.Range["P4:P13"];
//         bool thrustCheckPassed = true;
//         double deltaErr_thrust = 0;

//         foreach (Range cell in loadPointThrust.Cells)
//         {
//             double thrust = Math.Abs((double)cell.Value);
//             if (thrust > thrustUpperLim)
//             {
//                 Logger("Thrust check Failed....");
//                 turbaResults_ERG.Range["P3"].Value = false;
//                 thrustCheckPassed = false;
//                 deltaErr_thrust = 1;
//                 break;
//             }
//         }

//         if (thrustCheckPassed)
//         {
//             Logger("Thrust check Passed....");
//             turbaResults_ERG.Range["P3"].Value = true;
//             turbaResults_ERG.Range["P2"].Value = true;
//         }
//         else
//         {
//             Logger("Thrust check Failed....");
//             turbaResults_ERG.Range["P3"].Value = false;
//             turbaResults_ERG.Range["P2"].Value = true;
//         }

//         turbaResults_ERG.Range["P40"].Value = deltaErr_thrust;
//     }

//     private void HardCheck_WheelChamberTemp1190_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         Worksheet currentFlowPath = (Worksheet)ThisWorkbook.Worksheets["Pre-Feasibility checks"];

//         double deltaT = Convert.ToDouble(turbaResults_ERG.Range["E2"].Value);
//         double deltaTupperLim = Convert.ToDouble(turbaResults_ERG.Range["F31"].Value);
//         Logger("GBC delta Temperature: " + deltaT);

//         double wheelchamberP = Convert.ToDouble(turbaResults_ERG.Range["F2"].Value);
//         double wheelchamberT = Convert.ToDouble(turbaResults_ERG.Range["G2"].Value);
//         Logger("Wheel Chamber Temperature: " + wheelchamberT);

//         bool deltaTStatus;
//         double deltaErr_deltaT;
//         if (deltaT > deltaTupperLim || deltaT <= 0)
//         {
//             Logger("deltaT GBC check Failed..");
//             turbaResults_ERG.Range["E3"].Value = false;
//             deltaTStatus = false;
//             deltaErr_deltaT = Math.Abs(deltaT - deltaTupperLim);
//         }
//         else
//         {
//             Logger("deltaT GBC check Passed..");
//             turbaResults_ERG.Range["E3"].Value = true;
//             deltaTStatus = true;
//             deltaErr_deltaT = 0;
//         }

//         double inletTemperature = Convert.ToDouble(currentFlowPath.Range["F8"].Value);
//         double wheelchamberPupperLim;
//         if (inletTemperature <= 500)
//         {
//             wheelchamberPupperLim = WheelChamberTemp_CurveWithCW1_GetUpperLimit(wheelchamberP);
//         }
//         else
//         {
//             wheelchamberPupperLim = WheelChamberTemp_CurveWithCW2_GetUpperLimit(wheelchamberP);
//         }
//         turbaResults_ERG.Range["H30"].Value = wheelchamberPupperLim;
//         turbaResults_ERG.Range["H31"].Value = wheelchamberPupperLim;

//         bool wheelchamberTStatus;
//         double deltaErr_wheelchamberT;
//         if (wheelchamberT > wheelchamberPupperLim || wheelchamberT <= 0)
//         {
//             Logger("wheelchamber Temp check Failed..");
//             turbaResults_ERG.Range["G3"].Value = false;
//             wheelchamberTStatus = false;
//             deltaErr_wheelchamberT = Math.Abs(wheelchamberT - wheelchamberPupperLim);
//         }
//         else
//         {
//             Logger("wheelchamber Temp check Passed..");
//             turbaResults_ERG.Range["G3"].Value = true;
//             wheelchamberTStatus = true;
//             deltaErr_wheelchamberT = 0;
//         }

//         turbaResults_ERG.Range["E40"].Value = deltaErr_deltaT;
//         turbaResults_ERG.Range["G40"].Value = deltaErr_wheelchamberT;
//     }

//     private void HardCheck_WheelChamberTemp1120_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         Worksheet currentFlowPath = (Worksheet)ThisWorkbook.Worksheets["Pre-Feasibility checks"];

//         double deltaT = Convert.ToDouble(turbaResults_ERG.Range["E2"].Value);
//         double deltaTupperLim = Convert.ToDouble(turbaResults_ERG.Range["F31"].Value);
//         Logger("GBC delta Temperature: " + deltaT);

//         double wheelchamberP = Convert.ToDouble(turbaResults_ERG.Range["F2"].Value);
//         double wheelchamberT = Convert.ToDouble(turbaResults_ERG.Range["G2"].Value);
//         Logger("Wheel Chamber Temperature: " + wheelchamberT);

//         bool deltaTStatus;
//         double deltaErr_deltaT;
//         if (deltaT > 240 || deltaT <= 0)
//         {
//             Logger("deltaT GBC check Failed..");
//             turbaResults_ERG.Range["E3"].Value = false;
//             deltaTStatus = false;
//             deltaErr_deltaT = Math.Abs(deltaT - deltaTupperLim);
//         }
//         else
//         {
//             Logger("deltaT GBC check Passed..");
//             turbaResults_ERG.Range["E3"].Value = true;
//             deltaTStatus = true;
//             deltaErr_deltaT = 0;
//         }

//         bool wheelchamberTStatus;
//         double deltaErr_wheelchamberT;
//         if (wheelchamberT > 410 || wheelchamberT <= 0)
//         {
//             Logger("wheelchamber Temp check Failed..");
//             turbaResults_ERG.Range["G3"].Value = false;
//             wheelchamberTStatus = false;
//             deltaErr_wheelchamberT = Math.Abs(wheelchamberT - 410);
//         }
//         else
//         {
//             Logger("wheelchamber Temp check Passed..");
//             turbaResults_ERG.Range["G3"].Value = true;
//             wheelchamberTStatus = true;
//             deltaErr_wheelchamberT = 0;
//         }

//         turbaResults_ERG.Range["E40"].Value = deltaErr_deltaT;
//         turbaResults_ERG.Range["G40"].Value = deltaErr_wheelchamberT;
//     }

//     private void HardCheck_GBCLength1190_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         double GBCLength = Convert.ToDouble(turbaResults_ERG.Range["AC2"].Value);
//         bool varicode7 = false; // Assume cu_SAXA_SAXI.Varicode 7 is absent
//         double GBCLengthLimit = 360;

//         bool GBCLengthStatus;
//         double deltaErr_GBCLength;
//         if (GBCLength > 360 || GBCLength <= 0)
//         {
//             turbaResults_ERG.Range["AC3"].Value = false;
//             Logger("GBC Length failed..");
//             deltaErr_GBCLength = Math.Abs(GBCLength - GBCLengthLimit);
//         }
//         else
//         {
//             turbaResults_ERG.Range["AC3"].Value = true;
//             Logger("GBC Length passed..");
//             deltaErr_GBCLength = 0;
//         }

//         turbaResults_ERG.Range["AC20"].Value = deltaErr_GBCLength;
//     }

//     private void HardCheck_GBCLength1120_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         double GBCLength = Convert.ToDouble(turbaResults_ERG.Range["AC2"].Value);
//         bool varicode7 = false; // Assume cu_SAXA_SAXI.Varicode 7 is absent
//         double GBCLengthLimit = 285;

//         bool GBCLengthStatus;
//         double deltaErr_GBCLength;
//         if (GBCLength > 295 || GBCLength <= 0)
//         {
//             turbaResults_ERG.Range["AC3"].Value = false;
//             Logger("GBC Length failed..");
//             deltaErr_GBCLength = Math.Abs(GBCLength - GBCLengthLimit);
//         }
//         else
//         {
//             turbaResults_ERG.Range["AC3"].Value = true;
//             Logger("GBC Length passed..");
//             deltaErr_GBCLength = 0;
//         }

//         turbaResults_ERG.Range["AC40"].Value = deltaErr_GBCLength;
//     }

//     private void HardCheck_PSI1190_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         bool PSIStatus = (bool)turbaResults_ERG.Range["AE3"].Value;
//         double deltaErr_PSI;

//         if (PSIStatus)
//         {
//             Logger("PSI check Passed....");
//             deltaErr_PSI = 0;
//         }
//         else
//         {
//             Logger("PSI check Failed....");
//             deltaErr_PSI = 1;
//         }

//         turbaResults_ERG.Range["AE40"].Value = deltaErr_PSI;
//     }

//     private void HardCheck_PSI1120_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         bool PSIStatus = (bool)turbaResults_ERG.Range["AE3"].Value;
//         double deltaErr_PSI;

//         if (PSIStatus)
//         {
//             Logger("PSI check Passed....");
//             deltaErr_PSI = 0;
//         }
//         else
//         {
//             Logger("PSI check Failed....");
//             deltaErr_PSI = 1;
//         }

//         turbaResults_ERG.Range["AE40"].Value = deltaErr_PSI;
//     }

//     private void HardCheck_Lang1190_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         bool LANGStatus = (bool)turbaResults_ERG.Range["AF3"].Value;
//         double deltaErr_LANG;

//         if (LANGStatus)
//         {
//             Logger("LANG check Passed....");
//             deltaErr_LANG = 0;
//         }
//         else
//         {
//             Logger("LANG check Failed....");
//             deltaErr_LANG = 1;
//         }

//         turbaResults_ERG.Range["AF40"].Value = deltaErr_LANG;
//     }

//     private void HardCheck_Lang1120_th()
//     {
//         Worksheet turbaResults_ERG = (Worksheet)ThisWorkbook.Worksheets["Output"];
//         bool LANGStatus = (bool)turbaResults_ERG.Range["AF3"].Value;
//         double deltaErr_LANG;

//         if (LANGStatus)
//         {
//             Logger("LANG check Passed....");
//             deltaErr_LANG = 0;
//         }
//         else
//         {
//             Logger("LANG check Failed....");
//             deltaErr_LANG = 1;
//         }

//         turbaResults_ERG.Range["AF40"].Value = deltaErr_LANG;
//     }

//     private void Logger(string message)
//     {
//         // Implement logging functionality here
//         Console.WriteLine(message);
//     }

//     private void TerminateIgniteX(string functionName)
//     {
//         // Implement termination functionality here
//         throw new Exception($"Terminating function: {functionName}");
//     }

//     private double WheelChamberTemp_CurveWithCW1_GetUpperLimit(double wheelchamberP)
//     {
//         // Implement the logic to get the upper limit for Wheel Chamber pressure using Curve 1
//         return 0;
//     }

//     private double WheelChamberTemp_CurveWithCW2_GetUpperLimit(double wheelchamberP)
//     {
//         // Implement the logic to get the upper limit for Wheel Chamber pressure using Curve 2
//         return 0;
//     }
// }