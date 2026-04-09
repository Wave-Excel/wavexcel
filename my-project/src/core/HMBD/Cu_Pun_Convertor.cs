using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Interfaces.ILogger;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
// using Microsoft.Office.Interop.Excel;
namespace HMBD.Cu_Pun_Convertor;

using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using Handlers.Custom_DAT_Handler;
using Handlers.CustomPathDeletePrepare;
using Ignite_x_wavexcel;
using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using static Microsoft.Maui.ApplicationModel.Permissions;

// namespace DatFileProcessor
// {

// remove range in LP11 CHECK 
public class CuPunConvertor
{
    TurbineDataModel turbineDataModel;
    TurbaOutputModel turbaOutputModel;
    IConfiguration configuration;
    ILogger logger;
    int lpCount;

    public CuPunConvertor()
    {
        turbineDataModel = TurbineDataModel.getInstance();
        turbaOutputModel = TurbaOutputModel.getInstance();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
    .Build();
        lpCount = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
    }

    //  void Main(string[] args)
    // {
    //     TurnaConvert();
    // }

    public void TurnaConvert(int maxlp = 0)
    {
        RenameOLDDat();
        ConvertPUN();
        LoadDatFile();
        InsertVARICODE52();
        InsertVARICODE54();
        RemoveSwallow(maxlp);
        WriteDatFile();
        int totalLps = (maxlp >= 1) ? (maxlp - 1) : 0;
        InsertDataLineUnderND(totalLps);
    }

    public void DeleteExecutedDat()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Logger("File deleted successfully.");
        }
        else
        {
            Logger("File not found.");
        }
    }

    public void RenameOLDDat()//next step 
    {
        string folderPath = @"C:\testDir\";
        string filename = "TURBATURBAE1.DAT.DAT";
        string newFileName = "TURBA_BASE.DAT";

        string oldFilePath = Path.Combine(folderPath, filename);
        string newFilePath = Path.Combine(folderPath, newFileName);

        if (File.Exists(oldFilePath))
        {
            if (File.Exists(newFilePath))
            {
                // Delete the existing file
                File.Delete(newFilePath);
            }
            File.Move(oldFilePath, newFilePath);
            Logger($"{filename} has been renamed to {newFileName}");
        }
        else
        {
            Logger("The .dat file to rename does not exist!");
        }

        //Thread.Sleep(500);
    }

    public void ConvertPUN()
    {
        string folderPath = @"C:\testDir\";
        string filename = "TURBAE1.PUN";
        string newFileName = "TURBAE1.DAT";

        string oldFilePath = Path.Combine(folderPath, filename);
        string newFilePath = Path.Combine(folderPath, newFileName);

        if (File.Exists(oldFilePath))
        {
            File.Move(oldFilePath, newFilePath);
            Console.WriteLine($"{filename} has been changed to {newFileName}");
        }
        else
        {
            Console.WriteLine("The .pun file does not exist!");
        }

        Thread.Sleep(1000);

        filename = "TURBAE1.DAT";
        newFileName = "TURBATURBAE1.DAT.DAT";

        oldFilePath = Path.Combine(folderPath, filename);
        newFilePath = Path.Combine(folderPath, newFileName);

        if (File.Exists(oldFilePath))
        {
            File.Move(oldFilePath, newFilePath);
            Console.WriteLine($"{filename} has been renamed to {newFileName}");
        }
        else
        {
            Console.WriteLine("The .dat file to rename does not exist!");
        }

        Thread.Sleep(1000);
    }

    public void InsertVARICODE52()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
        // Worksheet ws = workbook.Sheets["DAT_DATA"];

        string searchString = "5        49.000";
        bool lineFound = false;
        List<string> fileLines = turbineDataModel.DAT_DATA.ToList();

        // Range lastCell = ws.Cells[ws.Rows.Count, "B"].End(XlDirection.xlUp);
        // int lastRow = lastCell.Row;

        for (int i = 0; i < fileLines.Count; i++)
        {
            string originalText = fileLines[i];//ws.Cells[i, "B"].Value.ToString();

            if (originalText.Trim().StartsWith(searchString))
            {
                lineFound = true;
                string modifiedText = originalText.Substring(0, 9) + "52.000" + originalText.Substring(15);

                // ws.Rows[i + 1].Insert();
                fileLines.Insert(i + 1, modifiedText);
                // ws.Cells[i + 1, "B"].Value = modifiedText;

                // Console.WriteLine($"Original Line: {originalText}");
                // Console.WriteLine($"Modified Line: {modifiedText}");

                break;
            }
        }

        if (!lineFound)
        {
            Console.WriteLine($"The line starting with '{searchString}' was not found.");
        }
        turbineDataModel.DAT_DATA = fileLines;

        // workbook.Save();
        // workbook.Close();
        // excelApp.Quit();
    }

    public void InsertVARICODE54()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
        // Worksheet ws = workbook.Sheets["DAT_DATA"];

        string searchString = "5        52.000     1.000";
        bool lineFound = false;
        List<string> fileLines = turbineDataModel.DAT_DATA.ToList();

        // Range lastCell = ws.Cells[ws.Rows.Count, "B"].End(XlDirection.xlUp);
        // int lastRow = lastCell.Row;

        for (int i = 0; i < fileLines.Count; i++)
        {
            string originalText = fileLines[i];// ws.Cells[i, "B"].Value.ToString();

            if (originalText.StartsWith(searchString))
            {
                lineFound = true;
                string modifiedText = originalText.Substring(0, 9) + "54.000" + " " + "13200.000" + " " + originalText.Substring(28);

                // ws.Rows[i + 1].Insert();
                fileLines.Insert(i + 1, modifiedText);
                // ws.Cells[i + 1, "B"].Value = modifiedText;

                Console.WriteLine($"Original Line: {originalText}");
                Console.WriteLine($"Modified Line: {modifiedText}");

                break;
            }
        }

        if (!lineFound)
        {
            Console.WriteLine($"The line starting with '{searchString}' was not found.");
        }
        turbineDataModel.DAT_DATA = fileLines;
        // workbook.Save();
        // workbook.Close();
        // excelApp.Quit();
    }

    public void RemoveSwallow(int maxlp =0)
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
        // Worksheet ws = workbook.Sheets["DAT_DATA"];
        List<string> fileLines = turbineDataModel.DAT_DATA.ToList();
        Console.WriteLine("checkng");
        string searchString = "";
        if (maxlp > 0)
        {
            searchString = "!LP" + (maxlp);
        }
        else
        {
            searchString = "!LP11";
        }
        bool lineFound = false;

        // Range lastCell = ws.Cells[ws.Rows.Count, "B"].End(XlDirection.xlUp);
        // int lastRow = lastCell.Row;

        for (int i = 0; i < fileLines.Count; i++)
        {
            string cellValue = fileLines[i];//ws.Cells[i, "B"].Value.ToString();

            if (cellValue.StartsWith(searchString))
            {
                lineFound = true;
                for (int j = 1; j <= 4; j++)
                {
                    fileLines[i] = "";
                    i++;
                }
                // ws.Rows[$"{i}:{i + 3}"].Delete();
                //    int linesToDelete = Math.Min(4, fileLines.Count - i); // Ensure we don't go out of bounds
                //Console.WriteLine(linesToDelete);    
                //fileLines.RemoveRange(i, linesToDelete);
                Console.WriteLine("w");


                break;
            }
        }

        if (!lineFound)
        {
            Console.WriteLine($"The line starting with '{searchString}' was not found.");
        }
        List<string> filteredLines = new List<string>();

        // Loop through fileLines and add only non-empty strings
        for (int i = 0; i < fileLines.Count; i++)
        {
            if (!string.IsNullOrEmpty(fileLines[i]))
            {
                filteredLines.Add(fileLines[i]);
            }
        }

        // Assign the filtered list
        turbineDataModel.DAT_DATA = filteredLines;

        //turbineDataModel.DAT_DATA = fileLines;

        // workbook.Save();
        // workbook.Close();
        // excelApp.Quit();
    }

    public Dictionary<double, double> GetStageAndRDHEN()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
        // Worksheet ws = workbook.Sheets["Output"];

        Dictionary<double, double> dict = new Dictionary<double, double>();

        // // Range lastCell = ws.Cells[ws.Rows.Count, "AY"].End(XlDirection.xlUp);
        // // int lastRow = lastCell.Row;

        for (int i = 1; i <= lpCount; i++)
        {
            double key = turbaOutputModel.OutputDataList[i].AY; //ws.Cells[i, "AY"].Value.ToString();
            double value = turbaOutputModel.OutputDataList[i].AZ;//ws.Cells[i, "AZ"].Value.ToString();

            if (key != 0 && value != 0)
            {
                dict[key] = value;
            }
        }

        // workbook.Close();
        // excelApp.Quit();

        return dict;
    }
    public void UpdatePunConvertor()
    {
        string ergPath = @"C:\testDir\TURBATURBAE1.DAT.ERG";
        string[] fileContent = File.ReadAllLines(ergPath);
        for (int i = 0; i < fileContent.Length; i++)
        {
            if (fileContent[i].Contains("STUFE  RSPALT  RDZENT   RDEHN  GEF"))
            {
                i += 3;
                for (int j = i; j < fileContent.Length; j++)
                {
                    if (String.IsNullOrEmpty(fileContent[j]) || fileContent[j].Contains("") || fileContent[j].Contains("DATE"))
                    {
                        break;
                    }
                    if (fileContent[j].Contains("*"))
                    {
                        if (String.IsNullOrEmpty(fileContent[j]) || fileContent[j].Contains("") || fileContent[j].Contains("DATE"))
                        {
                            break;
                        }
                        string[] Params = fileContent[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        //double Rdehn = Convert.ToDouble(Params[3]);
                        string nRdehn = Next005(Params[3]);
                        UpdateValueinDat(Convert.ToDouble(Params[0]), Convert.ToDouble(nRdehn));


                    }
                }
            }
        }
    }
    public void UpdateValueinDat(double find, double replace)
    {
        string DatfilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] filePathDat = File.ReadAllLines(DatfilePath);
        for (int i = 0; i < filePathDat.Length; i++)
        {
            if (filePathDat[i].Contains("!ST"))
            {
                for (int j = i + 1; j < filePathDat.Length; j++)
                {
                    if (filePathDat[j].Contains("!LP2"))
                    {
                        break;
                    }
                    string[] fields = filePathDat[j].Split('|');
                    if (Convert.ToDouble(fields[0].Trim()) == find && Convert.ToDouble(fields[1].Trim()) == 2)
                    {
                        fields[8] = replace.ToString("0.00");
                        filePathDat[j] = string.Join("|", fields);
                    }
                }
            }
        }
        File.WriteAllLines(DatfilePath, filePathDat);
    }
    public string Next005(string input)
    {
        double value = Convert.ToDouble(input);
        // Always get the next higher multiple of 0.05
        double next = Math.Ceiling((value / 0.05) + 1e-9) * 0.05;
        // If "next" equals the input, add one more 0.05
        if (Math.Abs(next - value) < 1e-9)
            next += 0.05;
        return next.ToString("0.00"); // Always 3 decimal digits
    }

    public void UpdateSteamPathInDat()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        Dictionary<double, double> ERG_StageAndRDHEN = GetStageAndRDHEN();

        string fileContent = File.ReadAllText(filePath);
        string[] fileLines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        string parameter1 = "!ST GI  DM    LANG BETA  SE SZ PR  RT RAD";

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            if (fileLines[lineNumber].Contains(parameter1))
            {
                for (int lineNumberNew = lineNumber + 1; lineNumberNew < fileLines.Length; lineNumberNew += 2)
                {
                    if (fileLines[lineNumberNew].Contains("!LP"))
                    {
                        goto ReturnSub;
                    }

                    string line = fileLines[lineNumberNew + 1];
                    string currentStageNo = line.Substring(1, 2).Trim();
                    string radValueCurrent = line.Substring(38, 3).Trim();

                    if (ERG_StageAndRDHEN.ContainsKey(Convert.ToDouble(currentStageNo)))
                    {
                        double suggestedRAD = ERG_StageAndRDHEN[Convert.ToDouble(currentStageNo)];
                        double radValueNew = Math.Ceiling((suggestedRAD) / 0.05) * 0.05;

                        line = line.Substring(0, 38) + radValueNew.ToString("0.00") + line.Substring(41);
                        fileLines[lineNumberNew + 1] = line;

                        Console.WriteLine($"{currentStageNo}, {suggestedRAD}");
                    }
                }

                break;
            }
        }

    ReturnSub:
        fileContent = string.Join(Environment.NewLine, fileLines);
        Logger("Attempting to update WELLE DAT File..");
        Console.WriteLine(fileContent);

        File.WriteAllText(filePath, fileContent);
        Console.WriteLine("Dat File SteamPath RAD Written..");
    }

    void Logger(string message)
    {
        logger.LogInformation(message);
        Console.WriteLine(message);
    }

    void LoadDatFile()
    {
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        customDATFileProcessor.LoadDatFile();
        // Implement the logic to load the .dat file
    }

    void WriteDatFile()
    {
        CustomDatFileHandler customDatFileHandler = new CustomDatFileHandler();
        customDatFileHandler.WriteDatFile();

        // Implement the logic to write the .dat file
    }

    void InsertDataLineUnderND(int maxlp=0)
    {
        CustomDATFileProcessor customDATFileProcessor1 = new CustomDATFileProcessor();
        customDATFileProcessor1.InsertDataLineUnderND(maxlp);
        // Implement the logic to insert data line under ND
    }

    bool IsFileReadyForOpen(string filePath)
    {
        try
        {
            using (FileStream inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                if (inputStream.Length > 0)
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }

    void TerminateIgniteX(string functionName)
    {
        TurbineDesignPage.cts.Cancel();
        Console.WriteLine($"Terminating function: {functionName}");
    }
}
// }
