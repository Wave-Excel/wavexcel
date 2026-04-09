using System;
using System.Linq;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Handlers.Custom_DAT_Handler;
using Handlers.CustomPathDeletePrepare;
using Interfaces.ILogger;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
// using OfficeOpenXml;
using Turba.Cu_TurbaConfig;
using StartExecutionMain;
namespace Checks.SAXA_SAXI;

public class CustomSaxaSaxi
{
    // private ExcelPackage _package;
    // private ExcelWorksheet _datDataSheet;
    // private ExcelWorksheet _preFeasibilityChecksSheet;
    // private ExcelWorksheet _outputSheet;

    private TurbineDataModel turbineDataModel;

    private PreFeasibilityDataModel preFeasibilityDataModel;
    
    private TurbaOutputModel turbaOutputModel;
    ILogger logger;

    public CustomSaxaSaxi()
    {
        // _package = new ExcelPackage(new System.IO.FileInfo(filePath));
        // _datDataSheet = _package.Workbook.Worksheets["DAT_DATA"];
        // _preFeasibilityChecksSheet = _package.Workbook.Worksheets["Pre-Feasibility checks"];
        // _outputSheet = _package.Workbook.Worksheets["Output"];
        turbineDataModel = TurbineDataModel.getInstance();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        turbaOutputModel = TurbaOutputModel.getInstance();
        logger  = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void BCD_UPDATE(int maxLp =0)
    {
        LoadDatFile();
        BCD_Change();
        WriteDatFile();
    }

    public void SAXA_SAXI_Handler()
    {
        LoadDatFile();
        InsertABSTAND();
        Varicode24Edit();
        BCD_Change();
        Cross_Section_Insert();
        WriteDatFile();
    }

    public void InsertABSTAND()
    {
        List<string> fileLines = turbineDataModel.DAT_DATA.ToList();
        string searchString = "!     ABSTAND AXIALLAGER";
        bool lineFound = false;

        for (int i = 0; i < fileLines.Count; i++)
        {
            if (fileLines[i].StartsWith(searchString))
            {
            lineFound = true;
            string lineX = fileLines[i];
            string lineXPlus1 = fileLines[i + 1];

            // Insert two new lines after line X + 1
            fileLines.Insert(i + 2, lineX);
            fileLines.Insert(i + 3, lineXPlus1);

            break;
            }
        }

        if (!lineFound)
        {
        Console.WriteLine($"The line starting with '{searchString}' was not found.");
        }

        // Update the original array with the modified list
        turbineDataModel.DAT_DATA = fileLines;
    }

    public void Varicode24Edit()
    {
            string searchString = "5        24.000     1.000";
            bool lineFound = false;
            List<string> fileLines = turbineDataModel.DAT_DATA.ToList();

            for (int i = 0; i < fileLines.Count; i++)
            {
                if (fileLines[i].StartsWith(searchString.Substring(0, searchString.Length - 5)))
                {
                    lineFound = true;
                    fileLines[i] = fileLines[i].Replace("1.000", "2.000");
                    break;
                }
            }

            if (!lineFound)
            {
                Console.WriteLine($"The line starting with '{searchString}' was not found.");
            }

            // Update the original array with the modified list
            turbineDataModel.DAT_DATA = fileLines; 

    }

    public void BCD_Change()
    {
    string searchString1 = "!     ABSTAND AXIALLAGER";
    string searchString2 = "          0.000     0.000";
    bool lineFound = false;
    List<string> fileLines = turbineDataModel.DAT_DATA.ToList();

    for (int i = 0; i < fileLines.Count; i++)
    {
        if (fileLines[i].StartsWith(searchString1))
        {
            if (fileLines[i + 1].StartsWith(searchString2))
            {
                string newValue = (preFeasibilityDataModel.Decision == "TRUE") ? "0.000  1124.000" : "0.000  1198.000";
                fileLines[i + 1] = fileLines[i + 1].Replace("0.000     0.000", newValue);
                lineFound = true;
                break;
            }
        }
    }

    if (!lineFound)
    {
        Console.WriteLine($"The line starting with '{searchString1}' followed by '{searchString2}' was not found.");
    }

    // Update the original array with the modified list
    turbineDataModel.DAT_DATA = fileLines;

    }

    public void Cross_Section_Insert()
    {
    string searchString1 = "!     ABSTAND AXIALLAGER";
    string searchString2 = "          0.000     0.000";
    int occurrenceCount = 0;
    List<string> fileLines = turbineDataModel.DAT_DATA.ToList();

    for (int i = 0; i < fileLines.Count; i++)
    {
        if (fileLines[i].StartsWith(searchString1))
        {
            occurrenceCount++;
            if (occurrenceCount == 2)
            {
                if (fileLines[i + 1].StartsWith(searchString2))
                {
                    fileLines[i + 1] = fileLines[i + 1].Replace("0.000     0.000", "0.000   720.000");
                    break;
                }
            }
        }
    }

    if (occurrenceCount < 2)
    {
        Console.WriteLine($"The second occurrence of '{searchString1}' was not found.");
    }

    // Update the original array with the modified list
    turbineDataModel.DAT_DATA = fileLines;
    }

    public void SAXA_FIX()
    {
        LaunchTurba();
        double saxa = turbaOutputModel.OutputDataList[0].SAXA_SAXI;//_outputSheet.Cells["AR2"].GetValue<double>();

        if (saxa < 2.2)
        {
            IncreasePosition();
        }
        else
        {
            Logger("SAXA - SAXI is within Range");
        }
    }

    public void IncreasePosition()
    {
        string searchString1 = "!     POSITION DER";
        string searchString2 = "                  796.000";
        List<string> fileLines = turbineDataModel.DAT_DATA;
        LoadDatFile();

        for (int i = 0; i < fileLines.Count; i++)
        {
            if (fileLines[i].StartsWith(searchString1))
            {
                if (fileLines[i+1].StartsWith(searchString2))
                {
                    fileLines[i+1] = fileLines[i+1].Replace("                  796.000", "                  816.000");
                    // _datDataSheet.Cells[i + 1, 2].Value = _datDataSheet.Cells[i + 1, 2].Text.Replace("                  796.000", "                  816.000");
                    WriteDatFile();
                    break;
                }
            }
        }

        LaunchTurba();

        if (turbaOutputModel.OutputDataList[0].SAXA_SAXI < 2.2)
        {
            LoadDatFile();

            for (int i = 0; i <fileLines.Count; i++)
            {
                if (fileLines[i].StartsWith(searchString1))
                {
                    if (fileLines[i+1].StartsWith(searchString2))
                    {
                        fileLines[i+1]=fileLines[i+1].Replace("                  816.000", "                  836.000");
                        // _datDataSheet.Cells[i + 1, 2].Value = _datDataSheet.Cells[i + 1, 2].Text.Replace("                  816.000", "                  836.000");
                        WriteDatFile();
                        break;
                    }
                }
            }

            LaunchTurba();

            if (turbaOutputModel.OutputDataList[0].SAXA_SAXI < 2.2)
            {
                Logger("SAXA - SAXI Couldnt be Optimized...");
            }
            else
            {
                Logger("SAXA - SAXI Optimized...");
            }
        }
        else
        {
            Logger("SAXA - SAXI Optimized...");
        }
    }

    private void LoadDatFile()
    {
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        customDATFileProcessor.LoadDatFile();
        // Implement the logic to load the DAT file
    }

    private void WriteDatFile()
    {
        // CustomReadDatFileHandler customReadDatFileHandler= new CustomReadDatFileHandler();
        CustomDatFileHandler customDatFileHandler = new CustomDatFileHandler();
        customDatFileHandler.WriteDatFile();
        // Implement the logic to write the DAT file
    }

    private void LaunchTurba()
    {
        CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
        cuTurbaAutomation.LaunchTurba();
        // Implement the logic to launch Turba
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
        // Implement the logic to log the message
    }
}
