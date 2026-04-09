using System;
using System.IO;
using System.IO.Packaging;
using Microsoft.Extensions.Configuration;
using Interfaces.ILogger;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;

// using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using Models.TurbaOutputDataModel;

namespace Handlers.CU_ERG_RsminHandler;
class CustomThrustCalculator
{
    private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
    private IConfiguration configuration;
    TurbaOutputModel turbaOutputModel;

    ILogger logger;
    public CustomThrustCalculator(){
        turbaOutputModel = TurbaOutputModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }    
    public void LoadRsminERGFile(int maxlp= 0)
    {
        GetThrust(maxlp);
    }

    public void GetThrust(int maxlp = 0)
    {
        string filePath = @"C:\testDir\RSMINE1.ERG";
        string fileContent;
        string[] fileLines;
        int lineNumber;
        // Console.WriteLine("Opening the file.." + filePath);
        // try{
            // fileContent = File.ReadAllText(filePath);
            // fileLines = fileContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (IsFileReadyForOpen(filePath))
        {
            fileLines = File.ReadAllLines(filePath);
        }
        else
        {
            // In case of RSMIN.EXE fails then forcefully write bigger value
            // ErgResult.SetValue("P4", 1.5);
            turbaOutputModel.OutputDataList[1].Thrust = 1.5;
            return;
        }

        string parameter = "RESTSCHUB TOTAL (N) REMAINING THRUST TOTAL";
        for (lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            if (fileLines[lineNumber].Contains(parameter))
            {
                lineNumber += 2;
                int lpNumber = 1;
                for (int lineNo = lineNumber; lineNo < fileLines.Length; lineNo++)
                {
                    string line = fileLines[lineNo];
                    if (line.Length < 20)
                    {
                        goto ReturnSub;
                    }
                    string subLine = line.Substring(25).Trim();
                    string[] thrust = subLine.Split(" ");
                    turbaOutputModel.OutputDataList[lpNumber].Thrust = Convert.ToDouble(thrust[0]);
                    lpNumber++;
                }
                goto ReturnSub;
            }
        }
    ReturnSub:
        Logger("Closed the file.." + filePath);
        // }
        // catch(Exception ex){
        //     logger.LogError("GetThrust", ex.Message);
        // }
    }
    private bool IsFileReadyForOpen(string filePath)
    {
        // Implement the logic to check if the file is ready to be opened
        return File.Exists(filePath);
    }


    public void Logger(string message){
        logger.LogInformation(message);
    }
}