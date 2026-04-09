// using System;
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// // using Excel = Microsoft.Office.Interop.Excel;
// using OfficeOpenXml;
// using StartExecutionMain;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Models.TurbineData;
// using Interfaces.IThermodynamicLibrary;
// using Services.ThermodynamicService;
// using Budoom;

// namespace Handlers.Executed_Custom_Input_Handle;

//     public class ExecutedCustomInputHandler
//     {
//         string filePath;
//         TurbineDataModel turbineDataModel;
//         IThermodynamicLibrary thermodynamicService;

//         IConfiguration configuration;

//         public ExecutedCustomInputHandler(){
//             configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
//             filePath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
//             thermodynamicService =  StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
//             turbineDataModel = TurbineDataModel.getInstance();
//         }
//         public void UpdateCustomParams()
//         {
//             ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
//             ExcelPackage package = new ExcelPackage(new FileInfo(filePath));

//             // Excel.Application excelApp = new Excel.Application();
//             // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
//             ExcelWorksheet normDB_CI = package.Workbook.Worksheets["PowerNormDB_CI"];
//             // Excel.Worksheet hmbd = workbook.Sheets["OPEN_HBD"];
//             ExcelWorksheet wsPowerDB = package.Workbook.Worksheets["PowerDB"];
//             ExcelWorksheet ci = package.Workbook.Worksheets["Customer_Inputs_New"];

//             int totalRows = wsPowerDB.Dimension.End.Row;
            

//             // double[] newTurbine = new double[5];
//             double[] normNewTurbine = new double[5];
//             double maxValue = double.MinValue;
//             double minValue = double.MaxValue;
//             int startCol = 3;
//             int endCol = 7;
//             for (int col = startCol; col <= endCol; col++)
//             {
//                 maxValue = double.MinValue;
//                 minValue = double.MaxValue;
//                 for (int row = 1; row <= totalRows; row++)
//                 {
//                     var cellValue = wsPowerDB.Cells[row, col].Value;

//                     if (cellValue != null && double.TryParse(cellValue.ToString(), out double numericValue))
//                     {
//                         maxValue = Math.Max(maxValue, numericValue);
//                         minValue = Math.Min(minValue, numericValue);
//                     }
//                 }
//                 // if (ci.Cells[2, col].Value == null)
//                 // {
//                 //     normNewTurbine[col - 3] = 0;//ci.Cells[2, col].Value;
//                 // }
//                 // else 
//                 if ((maxValue - minValue) != 0)
//                 {
//                     normNewTurbine[col - 3] = ((double)ci.Cells[2, col].Value - minValue) / (maxValue - minValue);
//                 }
//                 else
//                 {
//                     normNewTurbine[col - 3] = 0;
//                 }
//             }
            
//             normDB_CI.Cells["C4"].Value = normNewTurbine[0];
//             normDB_CI.Cells["D4"].Value = normNewTurbine[1];
//             normDB_CI.Cells["E4"].Value = normNewTurbine[2];
//             normDB_CI.Cells["F4"].Value = normNewTurbine[3];
//             normDB_CI.Cells["G4"].Value = normNewTurbine[4];

//             // SortCI(workbook);
//             thermodynamicService.PerformCalculations();
//             List<DataPoint> dataList = thermodynamicService.LoadDataFromExcel(filePath, "PowerNormDB_CI");
//             turbineDataModel.TurbineEfficiency = thermodynamicService.getClosestEfficiency(dataList, normNewTurbine[1], normNewTurbine[4], normNewTurbine[3], normNewTurbine[2]);
//             // normDB_CI.Range["A4"].Value = normDB_CI.Range["A7"].Value;
//             // turbineDataModel.TurbineEfficiency = (double)normDB_CI.Range["A7"].Value;

//             // workbook.Save();
//             // workbook.Close();
//             // excelApp.Quit();

//             package.Save();
//             Console.WriteLine("over!!!!!!!!");
//         }

//         // public void SortCI(Excel.Workbook workbook)
//         // {
//         //     Excel.Worksheet ws = workbook.Sheets["PowerNormDB_CI"];
//         //     int lastRow = ws.Cells[ws.Rows.Count, 11].End(Excel.XlDirection.xlUp).Row;

//         //     Excel.Range sortRange = ws.Range["A7:K" + lastRow];
//         //     sortRange.Sort(sortRange.Columns[11], Excel.XlSortOrder.xlAscending);
//         // }

//         public void CheckAndGoalSeek()
//         {
//             Excel.Application excelApp = new Excel.Application();
//             Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
//             Excel.Worksheet ci = workbook.Sheets["Customer_Inputs_New"];
//             List<Excel.Range> emptyCells = new List<Excel.Range>();

//             for (int col = 3; col <= 7; col++)
//             {
//                 if (ci.Cells[2, col].Value == null)
//                 {
//                     emptyCells.Add(ci.Cells[2, col]);
//                 }
//             }

//             if (emptyCells.Count > 1)
//             {
//                 Console.WriteLine("Minimum input criteria not met");
//                 return;
//             }

//             if (emptyCells.Count == 1)
//             {
//                 Excel.Range emptyCell = emptyCells[0];

//                 switch (emptyCell.Address)
//                 {
//                     case "$C$2":
//                         GetPower(workbook);
//                         break;
//                     case "$D$2":
//                         GoalSeekSteamPressure(workbook);
//                         break;
//                     case "$E$2":
//                         GoalSeekSteamTemp(workbook);
//                         break;
//                     case "$F$2":
//                         GoalSeekSteamMass(workbook);
//                         break;
//                     case "$G$2":
//                         MissingExhaustPressure();
//                         break;
//                 }
//             }

//             workbook.Save();
//             workbook.Close();
//             excelApp.Quit();

//             Marshal.ReleaseComObject(ci);
//             Marshal.ReleaseComObject(workbook);
//             Marshal.ReleaseComObject(excelApp);
//         }

//         public void GetPower(ExcelWorkbook workbook)
//         {
//             // Excel.Worksheet ci = workbook.Sheets["Customer_Inputs_New"];
//             // Excel.Worksheet hmbd = workbook.Sheets["OPEN_HBD"];
//             // hmbd.Range["E17"].Value = ci.Range["G3"].Value;
//             // hmbd.Range["C7"].Value = ci.Range["E3"].Value;
//             // hmbd.Range["C6"].Value = ci.Range["D3"].Value;
//             // hmbd.Range["D7"].Value = ci.Range["F3"].Value;
//             // thermodynamicService.PerformCalculations();

//             // thermodynamicService.findPowerOfTurbine();
//             // ci.Range["C2"].Value = hmbd.Range["AK25"].Value;
            
//             double inletEnthalpy = thermodynamicService.getInletEnthalpy(turbineDataModel.InletPressure, turbineDataModel.InletTemperature);
//             turbineDataModel.InletEnthalphy = inletEnthalpy;
//             double outletEnthalpy = thermodynamicService.getOutletEnthalpy(turbineDataModel.ExhaustPressure, inletEnthalpy, turbineDataModel.TurbineEfficiency, turbineDataModel.InletPressure);//getOutletEnthalpy(exhaustPressure, inletEnthalpy, efficiency, inletPressure);
//             turbineDataModel.OutletEnthalphy = outletEnthalpy;
//             turbineDataModel.AK25 = thermodynamicService.findPowerOfTurbine(inletEnthalpy, outletEnthalpy, turbineDataModel.MassFlowRate, turbineDataModel.OilLosses, turbineDataModel.GearLosses);
//         }

//         public void GoalSeekSteamMass(Excel.Workbook workbook)
//         {
//             Excel.Worksheet ci = workbook.Sheets["Customer_Inputs_New"];
//             Excel.Worksheet hmbd = workbook.Sheets["OPEN_HBD"];

//             hmbd.Range["E17"].Value = ci.Range["G3"].Value;
//             hmbd.Range["C7"].Value = ci.Range["E3"].Value;
//             hmbd.Range["C6"].Value = ci.Range["D3"].Value;
//             double powerReq = (double)hmbd.Range["AK25"].Value;
//             hmbd.Range["AK25"].GoalSeek(powerReq, hmbd.Range["D7"]);
//             ci.Range["F2"].Value = (double)hmbd.Range["D7"].Value / 3.6;
//         }

//         public void GoalSeekSteamPressure(Excel.Workbook workbook)
//         {
//             Excel.Worksheet ci = workbook.Sheets["Customer_Inputs_New"];
//             Excel.Worksheet hmbd = workbook.Sheets["OPEN_HBD"];

//             hmbd.Range["E17"].Value = ci.Range["G3"].Value;
//             hmbd.Range["C7"].Value = ci.Range["E3"].Value;
//             hmbd.Range["D7"].Value = ci.Range["F3"].Value;
//             double powerReq = (double)hmbd.Range["AK25"].Value;
//             hmbd.Range["AK25"].GoalSeek(powerReq, hmbd.Range["C6"]);
//             ci.Range["D2"].Value = (double)hmbd.Range["C6"].Value * 0.980665;
            
//             var sc = new GoalInletPressure();
//             sc.massFlow = (decimal)turbineDataModel.MassFlowRate;
//             var goalSeekResult = GoalSeek.TrySeek(sc.Calculate, new List<decimal> { 0.15m }, 3536.215579m, 3m, 1000, false); 
//             goalSeekResult.ClosestValue
//         }

//         public void GoalSeekSteamTemp(Excel.Workbook workbook)
//         {
//             Excel.Worksheet ci = workbook.Sheets["Customer_Inputs_New"];
//             Excel.Worksheet hmbd = workbook.Sheets["OPEN_HBD"];

//             hmbd.Range["E17"].Value = ci.Range["G3"].Value;
//             hmbd.Range["C6"].Value = ci.Range["D3"].Value;
//             hmbd.Range["D7"].Value = ci.Range["F3"].Value;
//             double powerReq = (double)hmbd.Range["AK25"].Value;
//             hmbd.Range["AK25"].GoalSeek(powerReq, hmbd.Range["C7"]);
//             ci.Range["E2"].Value = hmbd.Range["C7"].Value;
//         }

//         public void MissingExhaustPressure()
//         {
//             Console.WriteLine("Error: Exhaust Pressure Must be Provided!");
//         }
//     }

// public class GoalInletPressure : IGoalSeek
// {
//     [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
//     static extern double hVon_p_t(double P, double T, double unknown);
//     public decimal massFlow;//8.93m;
//     public double temp;
//     public TurbineDataModel turbineDataModel;
//     public GoalInletPressure(){
//         turbineDataModel = TurbineDataModel.getInstance();
//         massFlow = (decimal)turbineDataModel.MassFlowRate;
//     }
//     public decimal Calculate(decimal x)
//     {
//         decimal sign = 1m;
//         if(x < 0){
//             sign = -1m;
//             x = -x;
//         }
//         return sign * massFlow * (getInletEnthalpy((double)x, temp) - 2781.10m);
//     }
//     public decimal getInletEnthalpy(double pressure, double temperature)
//     {
//         double enthalpy = hVon_p_t(pressure, temperature, 0);
//         return Convert.ToDecimal(enthalpy);
//     }
// }