using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
// using Microsoft.Office.Interop.Excel;
using HMBD.HMBDInformation;
using Turba.TurbaConfiguration;
using Models.TurbineData;
using ERG_PowerNoLoadOptimizer;
using Interfaces.ILogger;
using OfficeOpenXml;
using System.IO.Packaging;
using ERG_PowerMatch;
// using TurbineUtils;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.IO;
using Handlers.DAT_Handler;
using PdfSharp.Pdf.IO;
using Models.TurbaOutputDataModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StartExecutionMain;
using Ignite_x_wavexcel;
using System.DirectoryServices.AccountManagement;
using Ignite_X.src.core.Handlers;
using Kreisl.KreislConfig;
using Interfaces.IERGHandlerService;
using StartKreislExecution;
using Turba.Exec_TurbaConfig;
using DocumentFormat.OpenXml.Bibliography;
using Models.AdditionalLoadPointModel;
using Models.LoadPointDataModel;
using Services.ThermodynamicService;
using Interfaces.IThermodynamicLibrary;
using Ignite_X.src.core.Services;
using DocumentFormat.OpenXml.Spreadsheet;
//using PdfSharpCore.Drawing;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;
using Ignite_X.src.core.Models;
namespace Ignite_X.src.core.Utilities
{
    
    public class PrintPdf
    {
        private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";

        private TurbaOutputModel turbaOutputModel;
        private TurbineDataModel turbineDataModel;
        private IERGHandlerService eRGHandlerService;
        private IConfiguration configuration;
        private ILogger logger;
        private IThermodynamicLibrary thermodynamicService;
        int i_powermatch;
        int maxLoadPoints = 10;
        private bool isLP5Change = false;
        public PrintPdf()
        {
            configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            eRGHandlerService = StartKreisl.GlobalHost.Services.GetRequiredService<IERGHandlerService>();
            thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
            logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
            turbaOutputModel = TurbaOutputModel.getInstance();
            turbineDataModel = TurbineDataModel.getInstance();
            isLP5Change = false;
        }
        public double[,] getCordinate(double x, double y)
        {
            double[,] cordinate = new double[4, 2];

            // 1. X-34 , Y-24
            cordinate[0, 0] = x - 34;
            cordinate[0, 1] = y - 24;

            // 2. X+4 , Y-34
            cordinate[1, 0] = x + 6;
            cordinate[1, 1] = y - 24;

            // 3. X-34 , Y+4
            cordinate[2, 0] = x - 34;
            cordinate[2, 1] = y + 4;

            // 4. X+4 , Y+4
            cordinate[3, 0] = x + 6;
            cordinate[3, 1] = y + 4;

            return cordinate;
        }
        public int RoundToNearestTens(double number)
        {
            return (int)Math.Floor(number / 10.0) * 10;
        }
        double[,] dataTable = new double[,]
        {
            { 1, 424,   277 },
            { 2, 424,   460 },
            { 3, 355,   332.6 },
            { 4, 104,   459.4 },
            { 5, 104,   332.6 },
            { 6, 303,   87 },
            { 7, 104,   237.6 },
            { 8, 242,   332.6 },
            { 9, 43.3,  570.2 },
            { 10, 164.5, 570.2 },
            { 11, 484.8, 205.9 },
            { 15, 233.7, 514.7 },
            { 16, 303,   277.2 }
        };
        double[,] dataTableDump = new double[,]
        {
            { 1, 424.21, 273.08 },
            { 2, 424.19, 431.50 },
            { 3, 363.60, 328.54 },
            { 4, 77.87, 433.54 },
            { 5, 77.88, 330.60 },
            { 6, 77.88, 219.71 },
            { 7, 302.98, 90.96 },
            { 8, 199.07, 328.06 },
            { 9, 311.62, 550.29 },
            {10, 199.08, 550.31 },
            {11, 484.74, 193.90 },
            {15, 194.07, 494.85 },
            {17, 311.63, 273.11 },
            {18, 328.91, 605.75 },
            {19, 476.04, 605.78 },
            {20, 502.09, 494.87 },
            {21, 502.08, 384.00 }
        };

        public void WriteInpdf(XGraphics gfx,int loadPoint,double x,double y, int elementNumber, LineSizeDataModel data,string LoadPointName)
        {
            KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
            XFont font = new XFont("Arial", 7.91);
            double[,] cordinates = getCordinate(x, y);
            if(turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    if(elementNumber != 18 && elementNumber != 19)
                    {
                        gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                    }
                   
                }else if (!turbineDataModel.DumpCondensor)
                {
                    if (elementNumber != 9 && elementNumber != 10)
                    {
                        gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                    }
                    
                }
            }
            else
            {
                gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint) ,"bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            }
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            string formatted = ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit).ToString("F3");
            string result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
            gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);
            if(turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    if (elementNumber == 7)
                    {
                        UpdateLoadPoint(data, "Main Steam at TG Inlet", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), Math.Round(kreislERGHandlerService.ExtractEnthalphyForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), Math.Round(kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint) / 3.6, 3) );
                    }
                    else if (elementNumber == 8)
                    {
                        UpdateLoadPoint(data, "Exhuast Line", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), Math.Round(kreislERGHandlerService.ExtractEnthalphyForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), Math.Round(kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint) / 3.6, 3) );
                    }
                }
                else if(!turbineDataModel.DumpCondensor)
                {
                    if(elementNumber == 6)
                    {
                        UpdateLoadPoint(data, "Main Steam at TG Inlet", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint),2), kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), Math.Round(kreislERGHandlerService.ExtractEnthalphyForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint),2), Math.Round(kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint)/3.6,3) );
                    }
                    else if(elementNumber == 8)
                    {
                        UpdateLoadPoint(data, "Exhuast Line", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressureForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), Math.Round(kreislERGHandlerService.ExtractEnthalphyForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint), 2), Math.Round(kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, elementNumber, loadPoint)/3.6, 3) );
                    }
                }
            }
        }

        public void GeneratePDFForClosedCycleDumpCondensor(
        double pressure,
        double temperature,
        double enthalpy,
        double massFlow,
        double outletPres,
        double outletTemp,
        double outletEnth,
        double outletMassFlow,
        double power,
        string outputFile,
        double pageNo,
        double totalPage,
        double upperlimit,
        double lowerlimit,
        string LoadPointName,
        int loadPoint,
        LineSizeDataModel data)
        {
            KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            double lkPressure = 1.01;
            double lkTemperature = (temperature + outletTemp) / 2;
            double lkMass = 0.2;
            double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
            double capacityValue = turbineDataModel.CheckForCapacity * 3.6;
            double massFlowValue = kreislERGHandlerService.ExtractMassFlowForClosedCycle(StartKreisl.ergFilePath, 20, loadPoint);
            string existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpOFFDeOFFPRV.pdf" : "ClosedCycleDumpOFFDeOFF.pdf";
            try
            { 
                if (kreislDATHandler.checkDesupratorForClosedCycleDumpCondensor(StartKreisl.filePath, loadPoint) == -17 && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == -21)
                {
                    existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpOFFDeOFFPRV.pdf" : "ClosedCycleDumpOFFDeOFF.pdf";
                } else if (kreislDATHandler.checkDesupratorForClosedCycleDumpCondensor(StartKreisl.filePath, loadPoint) == 17 && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == -21)
                {
                    existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpOFFDeONPRV.pdf" : "ClosedCycleDumpOFFDeON.pdf";
                } else if (kreislDATHandler.checkDesupratorForClosedCycleDumpCondensor(StartKreisl.filePath, loadPoint) == -17 && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == 21)
                {
                    if (turbineDataModel.CheckForCapacity != 0 && (Math.Abs(capacityValue - massFlowValue) <= 0.5 || capacityValue >= massFlowValue))

                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpONDeOFFPRV.pdf" : "ClosedCycleDumpONDeOFF.pdf";
                    }
                    else if (turbineDataModel.CheckForCapacity != 0 && (Math.Abs(capacityValue - massFlowValue) <= 0.5 || capacityValue >= massFlowValue))
                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpOFFDeOFFPRV.pdf" : "ClosedCycleDumpOFFDeOFF.pdf";
                    }
                    else if (turbineDataModel.CheckForCapacity == 0)
                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpONDeOFFPRV.pdf" : "ClosedCycleDumpONDeOFF.pdf";
                    }
                }
                else if (kreislDATHandler.checkDesupratorForClosedCycleDumpCondensor(StartKreisl.filePath, loadPoint) == 17 && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == 21)
                {

                    if (turbineDataModel.CheckForCapacity != 0 && (Math.Abs(capacityValue - massFlowValue) <= 0.5 || capacityValue >= massFlowValue))
                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpONDeONPRV.pdf" : "ClosedCycleDumpONDeON.pdf";
                    }
                    else if (turbineDataModel.CheckForCapacity != 0 && (Math.Abs(capacityValue - massFlowValue) <= 0.5 || capacityValue >= massFlowValue))
                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpOFFDeONPRV.pdf" : "ClosedCycleDumpOFFDeON.pdf";

                    }
                    else if (turbineDataModel.CheckForCapacity == 0)
                    {
                        existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleDumpONDeONPRV.pdf" : "ClosedCycleDumpONDeON.pdf";
                    }
                }
                
                // Open existing PDF for modification
                PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
                PdfPage page = document.Pages[0];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 7.91);
                double offsetX = page.MediaBox.X1;
                double offsetY = page.MediaBox.Y1;
                Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);
                
                for (int row = 0; row < dataTableDump.GetLength(0); row++)
                {
                    int elementNumber = Convert.ToInt32(dataTableDump[row, 0]);

                    if (elementNumber != 17 && elementNumber !=20 && elementNumber !=21)
                    {
                        double x = dataTableDump[row, 1];
                        double y = dataTableDump[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber,data,LoadPointName);
                    }
                    else if (elementNumber == 17 && kreislDATHandler.checkDesupratorForClosedCycleDumpCondensor(StartKreisl.filePath, loadPoint) == 17)
                    {
                        double x = dataTableDump[row, 1];
                        double y = dataTableDump[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber, data, LoadPointName);
                    }else if((elementNumber == 21 || elementNumber == 20)  && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == 21 && turbineDataModel.CheckForCapacity != 0 && (Math.Abs(capacityValue - massFlowValue) <= 0.5 || capacityValue >= massFlowValue))
                    {
                        double x = dataTableDump[row, 1];
                        double y = dataTableDump[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber, data, LoadPointName);
                    }
                    else if ((elementNumber == 21 || elementNumber == 20) && kreislDATHandler.checkDumpCondensorForClosedCycle(StartKreisl.filePath, loadPoint) == 21 && turbineDataModel.CheckForCapacity == 0)
                    {
                        double x = dataTableDump[row, 1];
                        double y = dataTableDump[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber, data, LoadPointName);
                    }
                }

                gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(550, 103, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(turbineDataModel.ProjectName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(upperlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3).ToString("F2") + " " + TurbineDesignPage.Selectedenquiry.SteamPressureUnit).ToString(), new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 657.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(lowerlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3)).ToString("F2") + " " + TurbineDesignPage.Selectedenquiry.SteamPressureUnit, new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 668.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(337, 729, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(turbineDataModel.TSPID, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(446, 768, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(LoadPointName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(391, 726, 160, 14), XStringFormats.Center);

                double[,] cordinates = getCordinate(543, 658);

                XSize textSize1 = gfx.MeasureString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[0, 0], cordinates[0, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomRight);
                XSize textSize2 = gfx.MeasureString("h [kJ/,kg]", font);
                gfx.DrawString("h [kJ/kg]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[1, 0], cordinates[1, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomLeft);
                XSize textSize3 = gfx.MeasureString("t [°C]", font);
                gfx.DrawString("t [°C]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[2, 0], cordinates[2, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopRight);
                XSize textSize4 = gfx.MeasureString("m [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("m [" + TurbineDesignPage.Selectedenquiry.SteamMassUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[3, 0], cordinates[3, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopLeft);
                // turbine data model.process condensed return temp
                //gfx.DrawString(turbineDataModel.ProcessCondReturn.ToString(), new XFont("Arial", 9, XFontStyle.Bold), XBrushes.Black, new XRect(200, 620, 100, 100), XStringFormats.TopLeft);
                string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

                string domainName = "ad101";

                string userName = Environment.UserName;

                string fullName = "Siemens-User";
                string firstName = "Siemens-User";

                double smallerFontSize = 7;// 6.5; // Adjust the size as needed

                // Create a new font with the smaller size

                XFont smallerFont = new XFont("Arial", smallerFontSize);

                gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 697, 100, 100), XStringFormats.TopLeft);
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

                {

                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                    if (user != null)

                    {

                        // Get the user's full name

                        fullName = user.DisplayName;

                        // Display the full name

                        Console.WriteLine("The current user's full name is: " + fullName);

                    }

                    else

                    {

                        Console.WriteLine("User not found in the directory.");

                    }

                }


                if (fullName.EndsWith("(ext)"))

                {

                    fullName = fullName.Remove(fullName.Length - 6).Trim();

                    firstName = fullName.Split(",")[^1];

                }

                // Draw the string with the smaller font

                gfx.DrawString(firstName, smallerFont, XBrushes.Black, new XRect(337, 697, 100, 100), XStringFormats.TopLeft);

                document.Save(Path.Combine("C:\\testDir\\", outputFile));
                Console.WriteLine("PDF saved successfully at " + outputFile);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during PDF generation: " + ex.Message);
            }
        }
        public void GeneratePDFForClosedCycleWithOutDumpCondensor(
        double pressure,
        double temperature,
        double enthalpy,
        double massFlow,
        double outletPres,
        double outletTemp,
        double outletEnth,
        double outletMassFlow,
        double power,
        string outputFile,
        double pageNo,
        double totalPage,
        double upperlimit,
        double lowerlimit,
        string LoadPointName,
        int loadPoint,
        LineSizeDataModel data)
        {
            KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            double lkPressure = 1.01;
            double lkTemperature = (temperature + outletTemp) / 2;
            double lkMass = 0.2;
            double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
            string existingPdfPath = (turbineDataModel.IsPRVTemplate == true)? "ClosedCycleInputPRVD.pdf" : "ClosedCycleInputWPRVD.pdf";
            try
            {
                if (kreislDATHandler.checkDesupratorForClosedCycle(StartKreisl.filePath, loadPoint) == -16)
                {
                    existingPdfPath = (turbineDataModel.IsPRVTemplate == true) ? "ClosedCycleInputPRVWD.pdf" : "ClosedCycleInputWPRVWD.pdf"; ;
                }
                // Open existing PDF for modification
                PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
                PdfPage page = document.Pages[0];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 7.91);
                double offsetX = page.MediaBox.X1;
                double offsetY = page.MediaBox.Y1;
                Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);

                for (int row = 0; row < dataTable.GetLength(0); row++)
                {
                    int elementNumber = Convert.ToInt32(dataTable[row, 0]);

                    if(elementNumber != 16)
                    {
                        double x = dataTable[row, 1];
                        double y = dataTable[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber, data, LoadPointName);
                    }else if(elementNumber == 16 && kreislDATHandler.checkDesupratorForClosedCycle(StartKreisl.filePath, loadPoint) == 16) 
                    {
                        double x = dataTable[row, 1];
                        double y = dataTable[row, 2];
                        WriteInpdf(gfx, loadPoint, x, y, elementNumber, data, LoadPointName);
                    }
                }


                


                gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(550, 103, 100, 100), XStringFormats.TopLeft);

                double [,] cordinates = getCordinate(528, 658);

                XSize textSize1 = gfx.MeasureString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[0, 0], cordinates[0, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomRight);
                XSize textSize2 = gfx.MeasureString("h [kJ/,kg]", font);
                gfx.DrawString("h [kJ/kg]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[1, 0], cordinates[1, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomLeft);
                XSize textSize3 = gfx.MeasureString("t [°C]", font);
                gfx.DrawString("t [°C]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[2, 0], cordinates[2, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopRight);
                XSize textSize4 = gfx.MeasureString("m [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("m [" + TurbineDesignPage.Selectedenquiry.SteamMassUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[3, 0], cordinates[3, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopLeft);
                

                gfx.DrawString(turbineDataModel.ProjectName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(upperlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3).ToString("F2") + " " + TurbineDesignPage.Selectedenquiry.SteamPressureUnit).ToString(), new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 657.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(lowerlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3)).ToString("F2") + " " + TurbineDesignPage.Selectedenquiry.SteamPressureUnit, new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 668.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(337, 729, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(turbineDataModel.TSPID, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(446, 768, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(LoadPointName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(391, 726, 160, 14), XStringFormats.Center);
                // turbine data model.process condensed return temp
                gfx.DrawString(turbineDataModel.ProcessCondReturn.ToString()+ " %", new XFont("Arial", 9, XFontStyle.Bold), XBrushes.Black, new XRect(200, 620, 100, 100), XStringFormats.TopLeft);
                string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

                string domainName = "ad101";

                string userName = Environment.UserName;

                string fullName = "Siemens-User";
                string firstName = "Siemens-User";

                double smallerFontSize = 7;// 6.5; // Adjust the size as needed

                // Create a new font with the smaller size

                XFont smallerFont = new XFont("Arial", smallerFontSize);

                gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 697, 100, 100), XStringFormats.TopLeft);
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

                {

                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                    if (user != null)

                    {

                        // Get the user's full name

                        fullName = user.DisplayName;

                        // Display the full name

                        Console.WriteLine("The current user's full name is: " + fullName);

                    }

                    else

                    {

                        Console.WriteLine("User not found in the directory.");

                    }

                }

                
                if (fullName.EndsWith("(ext)"))

                {

                    fullName = fullName.Remove(fullName.Length - 6).Trim();

                    firstName = fullName.Split(",")[^1];

                }

                // Draw the string with the smaller font

                gfx.DrawString(firstName, smallerFont, XBrushes.Black, new XRect(337, 697, 100, 100), XStringFormats.TopLeft);

                document.Save(Path.Combine("C:\\testDir\\", outputFile));
                Console.WriteLine("PDF saved successfully at " + outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during PDF generation: " + ex.Message);
            }
        }
        public void GeneratePDFForDesuprator(
        double pressure,
        double temperature,
        double enthalpy,
        double massFlow,
        double outletPres,
        double outletTemp,
        double outletEnth,
        double outletMassFlow,
        double power,
        string outputFile,
        double pageNo,
        double totalPage,
        double upperlimit,
        double lowerlimit,
        string LoadPointName,
        int loadPoint,
        LineSizeDataModel data)
        {
            KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            double lkPressure = 1.01;
            double lkTemperature = (temperature + outletTemp) / 2;
            double lkMass = 0.2;
            double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
            string existingPdfPath = "Dinput.pdf";
            try
            {
                if (kreislDATHandler.checkDesuprator(StartKreisl.filePath, loadPoint) == -8)
                {
                    existingPdfPath = "DWinput.pdf";
                }
                // Open existing PDF for modification
                PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
                PdfPage page = document.Pages[0];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 7.91);
                double offsetX = page.MediaBox.X1;
                double offsetY = page.MediaBox.Y1;
                Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);

                // Draw turbine data values onto PDF at specified locations
                double[,] cordinates = getCordinate(251, 221);
                gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 3, loadPoint),"bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 3, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, loadPoint).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                string formatted = ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 3, loadPoint),"t/h",TurbineDesignPage.Selectedenquiry.SteamMassUnit).ToString("F3");
                string result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

                UpdateLoadPoint(data, "Main Steam at TG Inlet", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 3, loadPoint),2), kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, loadPoint), Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 3, loadPoint),2), Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 3, loadPoint)/3.6,3));

                gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(448, 264, 100, 100), XStringFormats.TopLeft);


                cordinates = getCordinate(432, 364);
                gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                formatted = Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit), 2).ToString("F3");
                result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);


                cordinates = getCordinate(354, 419);
                gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                formatted = (ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit)).ToString("F3");
                result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

                if (kreislDATHandler.checkDesuprator(StartKreisl.filePath, loadPoint) == 8)
                {
                    cordinates = getCordinate(432, 475);
                    gfx.DrawString(Math.Round(ConvertingToInputUnit(1.2 * (turbineDataModel.InletPressure), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                    gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                    gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                    formatted = Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit), 2).ToString("F3");
                    result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                    gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);
                }



                cordinates = getCordinate(354, 530);
                gfx.DrawString(Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                formatted = Math.Round(ConvertingToInputUnit(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit), 2).ToString("F3");
                result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);
                UpdateLoadPoint(data, "Exhuast Line", LoadPointName, Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 4, loadPoint),2), Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 4, loadPoint),2), Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 4, loadPoint),2), Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 4, loadPoint)/3.6,3));


                gfx.DrawString(turbineDataModel.ProjectName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(upperlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3).ToString("F2") + " " + TurbineDesignPage.Selectedenquiry.SteamPressureUnit).ToString(), new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 657.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(lowerlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3)).ToString("F2") + " "+ TurbineDesignPage.Selectedenquiry.SteamPressureUnit, new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(324, 668.2, 160, 14), XStringFormats.TopLeft);
                gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(337, 729, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(turbineDataModel.TSPID, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(446, 768, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(LoadPointName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(391, 726, 160, 14), XStringFormats.Center);
                cordinates = getCordinate(528, 658);

                XSize textSize1 = gfx.MeasureString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("p [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[0, 0], cordinates[0, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomRight);
                XSize textSize2 = gfx.MeasureString("h [kJ/,kg]", font);
                gfx.DrawString("h [kJ/kg]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[1, 0], cordinates[1, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomLeft);
                XSize textSize3 = gfx.MeasureString("t [°C]", font);
                gfx.DrawString("t [°C]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[2, 0], cordinates[2, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopRight);
                XSize textSize4 = gfx.MeasureString("m [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("m [" + TurbineDesignPage.Selectedenquiry.SteamMassUnit + "]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[3, 0], cordinates[3, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopLeft);


                string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

                string domainName = "ad101";

                string userName = Environment.UserName;

                string fullName = "Siemens-User";

                string firstName = "Siemens-User";
                double smallerFontSize = 7;// 6.5; // Adjust the size as needed

                // Create a new font with the smaller size

                XFont smallerFont = new XFont("Arial", smallerFontSize);

                gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 697, 100, 100), XStringFormats.TopLeft);
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

                {

                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                    if (user != null)

                    {

                        // Get the user's full name

                        fullName = user.DisplayName;

                        // Display the full name

                        Console.WriteLine("The current user's full name is: " + fullName);

                    }

                    else

                    {

                        Console.WriteLine("User not found in the directory.");

                    }

                }


                if (fullName.EndsWith("(ext)"))

                {

                    fullName = fullName.Remove(fullName.Length - 6).Trim();
                    firstName = fullName.Split(",")[^1];
                }

                // Draw the string with the smaller font

                gfx.DrawString(firstName, smallerFont, XBrushes.Black, new XRect(337, 697, 100, 100), XStringFormats.TopLeft);

                document.Save(Path.Combine("C:\\testDir\\", outputFile));
                Console.WriteLine("PDF saved successfully at " + outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during PDF generation: " + ex.Message);
            }
        }
        public double ConvertingToInputUnit(double val , string currentUnit , string givenUnit)
        {
            if (currentUnit != givenUnit) { 
             
                if(givenUnit == "ata" && currentUnit == "bar")
                {
                    return val / 0.980665;
                }else if(givenUnit == "t/h" && currentUnit == "kg/s")
                {
                    return val * 3.6;
                }else if(givenUnit == "bar" && currentUnit == "ata")
                {
                    return val * 0.980665;
                }else if(givenUnit == "kg/s" && currentUnit == "t/h")
                {
                    return val / 3.6;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return val;
            }
        }
        public static void UpdateLoadPoint(LineSizeDataModel data, string pipeName, string loadPointName, double pressure, double temperature, double enthalpy, double massFlow)
        {
            // Find the pipe by name
            var pipe = data.LineSize["Pipes"]
                .FirstOrDefault(p => p.ContainsKey("PipeName") && (string)p["PipeName"] == pipeName);

            if (pipe == null)
            {
                // If pipe doesn't exist, create it and add the load point
                var newPipe = new Dictionary<string, object>
                {
                    { "PipeIndex", data.LineSize["Pipes"].Count + 1 },
                    { "PipeName", pipeName },
                    { "LoadPoints", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                { "LoadPointName", loadPointName },
                                { "Pressure", pressure },
                                { "Temperature", temperature },
                                { "Enthalpy", enthalpy },
                                { "MassFlow", massFlow }
                            }
                        }
                    }
                };
                data.LineSize["Pipes"].Add(newPipe);
            }
            else
            {
                // Pipe exists, check for the load point
                var loadPoints = pipe["LoadPoints"] as List<Dictionary<string, object>>;
                var loadPoint = loadPoints
                    .FirstOrDefault(lp => lp.ContainsKey("LoadPointName") && (string)lp["LoadPointName"] == loadPointName);

                if (loadPoint == null)
                {
                    // Add new load point
                    loadPoints.Add(new Dictionary<string, object>
                    {
                        { "LoadPointName", loadPointName },
                        { "Pressure", pressure },
                        { "Temperature", temperature },
                        { "Enthalpy", enthalpy },
                        { "MassFlow", massFlow }
                    });
                }
                else
                {
                    // Update existing load point
                    loadPoint["Pressure"] = pressure;
                    loadPoint["Temperature"] = temperature;
                    loadPoint["Enthalpy"] = enthalpy;
                    loadPoint["MassFlow"] = massFlow;
                }
            }
        }


        public void GeneratePDF(
        double pressure,
        double temperature,
        double enthalpy,
        double massFlow,
        double outletPres,
        double outletTemp,
        double outletEnth,
        double outletMassFlow,
        double power,
        string outputFile,
        double pageNo,
        double totalPage,
        double upperlimit,
        double lowerlimit,
        string LoadPointName,
        LineSizeDataModel data)
        {

            double lkPressure = 1.01;
            double lkTemperature = (temperature + outletTemp) / 2;
            double lkMass = 0.2;
            double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
            string existingPdfPath = "input.pdf";
            try
            {
                // Open existing PDF for modification
                PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
                PdfPage page = document.Pages[0];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 7.91);
                double offsetX = page.MediaBox.X1;
                double offsetY = page.MediaBox.Y1;
                Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);

                double[,] cordinates = getCordinate(251, 221);
                // Draw turbine data values onto PDF at specified locations

                gfx.DrawString(Math.Round(ConvertingToInputUnit(pressure,"bar",TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(enthalpy, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(temperature.ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                string formatted = Math.Round(ConvertingToInputUnit(massFlow,"kg/s",TurbineDesignPage.Selectedenquiry.SteamMassUnit), 2).ToString("F3");
                string result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

                gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(448, 264, 100, 100), XStringFormats.TopLeft);


                cordinates = getCordinate(432, 364);

                gfx.DrawString(Math.Round(ConvertingToInputUnit(lkPressure, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(lkEnthalpy, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(lkTemperature, 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                formatted = Math.Round(ConvertingToInputUnit(lkMass, "t/h", TurbineDesignPage.Selectedenquiry.SteamMassUnit), 2).ToString("F3");
                result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

                cordinates = getCordinate(354, 467);
                gfx.DrawString(Math.Round(ConvertingToInputUnit(outletPres, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(outletEnth, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(outletTemp, 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                formatted = Math.Round((ConvertingToInputUnit(outletMassFlow, "kg/s", TurbineDesignPage.Selectedenquiry.SteamMassUnit)) - 0.2, 2).ToString("F3");
                result = formatted.Length > 5 ? formatted.Substring(0, 5) : formatted;
                gfx.DrawString(result, font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);



                gfx.DrawString(turbineDataModel.ProjectName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(upperlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3).ToString("F2") + " "+ TurbineDesignPage.Selectedenquiry.SteamPressureUnit).ToString(), new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(321, 663, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString((Math.Round((ConvertingToInputUnit(lowerlimit, "bar", TurbineDesignPage.Selectedenquiry.SteamPressureUnit)), 3)).ToString("F2") + " "+ TurbineDesignPage.Selectedenquiry.SteamPressureUnit, new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(321, 679, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(337, 729, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(turbineDataModel.TSPID, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(446, 768, 100, 100), XStringFormats.TopLeft);
                gfx.DrawString(LoadPointName, new XFont("Arial", 7.91, XFontStyle.Bold), XBrushes.Black, new XRect(391, 726, 160, 14), XStringFormats.Center);

                cordinates = getCordinate(528, 658); 

                XSize textSize1 = gfx.MeasureString("p ["+TurbineDesignPage.Selectedenquiry.SteamPressureUnit+"]", font);
                gfx.DrawString("p ["+TurbineDesignPage.Selectedenquiry.SteamPressureUnit+"]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[0,0], cordinates[0,1], textSize1.Width+5, textSize1.Height+5), XStringFormats.BottomRight);
                XSize textSize2 = gfx.MeasureString("h [kJ/,kg]",font);
                gfx.DrawString("h [kJ/kg]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[1, 0], cordinates[1, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.BottomLeft);
                XSize textSize3 = gfx.MeasureString("t [°C]", font);
                gfx.DrawString("t [°C]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[2, 0], cordinates[2, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopRight);
                XSize textSize4 = gfx.MeasureString("m [" + TurbineDesignPage.Selectedenquiry.SteamPressureUnit + "]", font);
                gfx.DrawString("m ["+TurbineDesignPage.Selectedenquiry.SteamMassUnit+"]", new XFont("Arial", 9.49), XBrushes.Black, new XRect(cordinates[3, 0], cordinates[3, 1], textSize1.Width + 5, textSize1.Height + 5), XStringFormats.TopLeft);


                UpdateLoadPoint(data, "Main Steam at TG Inlet", LoadPointName, pressure, temperature, enthalpy, massFlow);
                UpdateLoadPoint(data, "Exhuast Line", LoadPointName, outletPres, outletTemp, outletEnth, outletMassFlow);
                string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

                string domainName = "ad101";

                string userName = Environment.UserName;

                string fullName = "Siemens-User";

                string firstName = "Siemens-User";
                double smallerFontSize = 7;// 6.5; // Adjust the size as needed

                // Create a new font with the smaller size

                XFont smallerFont = new XFont("Arial", smallerFontSize);

                gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 697, 100, 100), XStringFormats.TopLeft);
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

                {

                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                    if (user != null)

                    {

                        // Get the user's full name

                        fullName = user.DisplayName;

                        // Display the full name

                        Console.WriteLine("The current user's full name is: " + fullName);

                    }

                    else

                    {

                        Console.WriteLine("User not found in the directory.");

                    }

                }


                if (fullName.EndsWith("(ext)"))

                {

                    fullName = fullName.Remove(fullName.Length - 6).Trim();
                    firstName = fullName.Split(",")[^1];
                }

                // Draw the string with the smaller font

                gfx.DrawString(firstName, smallerFont, XBrushes.Black, new XRect(337, 697, 100, 100), XStringFormats.TopLeft);


                document.Save(Path.Combine("C:\\testDir\\", outputFile));
                Console.WriteLine("PDF saved successfully at " + outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during PDF generation: " + ex.Message);
            }
        }
    }
}
