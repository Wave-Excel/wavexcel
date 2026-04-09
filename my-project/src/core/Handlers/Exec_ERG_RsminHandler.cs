using System;
using System.IO;
using System.IO.Packaging;
using Microsoft.Extensions.Configuration;
using Interfaces.ILogger;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;

// using Microsoft.Office.Interop.Excel;
using Models.TurbaOutputDataModel;

namespace Handlers.Exec_ERG_RsminHandler;

public class ExecERGFileHandler
{  
   private IConfiguration configuration;
    TurbaOutputModel turbaOutputModel;

    ILogger logger;
    public ExecERGFileHandler(){
      turbaOutputModel = TurbaOutputModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }
       public void GetThrust()
    {
        string filePath = @"C:\testDir\RSMINE1.ERG";
        string fileContent;
        string[] fileLines;
        int lineNumber;
        Console.WriteLine("Opening the file.." + filePath);
        // try{
            fileContent = File.ReadAllText(filePath);
            fileLines = fileContent.Split(new[] { "\r\n" }, StringSplitOptions.None);

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
                        string thrust = subLine;
                        turbaOutputModel.OutputDataList[lpNumber].Thrust = Convert.ToDouble(thrust);
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

    public void Logger(string message){
        logger.LogInformation(message);
    }
}

