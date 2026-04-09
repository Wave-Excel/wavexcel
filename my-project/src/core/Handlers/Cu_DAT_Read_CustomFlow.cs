using System;
using System.Linq;
using Interfaces.ILogger;
using Models.TurbineData;
namespace Handlers.CustomDatReadCustomFlow;
using StartExecutionMain;
// using Excel = Microsoft.Office.Interop.Excel;

public class CustomReadDatFileHandler
{
    private TurbineDataModel turbineDataModel;
    ILogger logger;

    public CustomReadDatFileHandler()
    {
        turbineDataModel = TurbineDataModel.getInstance();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void ScanDATFile()
    {
        Logger("Scanning Reference DAT File..");

        GetParam_BEAUFSCHL();
        GetParam_RADKAMMER();
        GetParam_DRUCKZIFFERN();
        GetParam_INNENDURCHMESSER();
        GetParam_AUSGLEICHSKOLBENDURCHMESSER();
    }

    private void GetParam_BEAUFSCHL()
    {
        string parameter = "BEAUFSCHL";
        List<string> fileLines = turbineDataModel.DAT_DATA;
        
        turbineDataModel.BEAUFSCHL = 0;
        turbineDataModel.RADKAMMER = 0;
        turbineDataModel.DRUCK = 0;
        turbineDataModel.INNNEN =0;
        turbineDataModel.AUSGL = 0;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
                string paramVal = line.Substring(16, 10).Trim();
                turbineDataModel.BEAUFSCHL = Convert.ToDouble(paramVal);
                // datDataSheet.Cells[2, "E"].Value = paramVal;
                break;
            }
        }
    }

    private void GetParam_RADKAMMER()
    {
        string parameter = "RADKAMMER";
 //       int ergEoF = //datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
                string paramVal = line.Substring(6, 10).Trim();
                turbineDataModel.RADKAMMER = Convert.ToDouble(paramVal);
                break;
            }
        }
    }

    private void GetParam_DRUCKZIFFERN()
    {
        string parameter = "DRUCKZIFFERN";
        // int ergEoF = datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];// datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];// datDataSheet.Cells[lineNumber, "B"].Value.ToString();
                string paramVal = line.Substring(16, 9).Trim();
                // datDataSheet.Cells[4, "E"].Value = paramVal;
                turbineDataModel.DRUCK = Convert.ToDouble(paramVal);
                break;
            }
        }
    }

    private void GetParam_INNENDURCHMESSER()
    {
        string parameter = "INNENDURCHMESSER";
        // int ergEoF = datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
                string paramVal = line.Substring(16, 10).Trim();
                turbineDataModel.INNNEN = Convert.ToDouble(paramVal);
                break;
            }
        }
    }

    private void GetParam_AUSGLEICHSKOLBENDURCHMESSER()
    {
        string parameter = "AUSGLEICHSKOLBENDURCHMESSER";
        List<string> fileLines = turbineDataModel.DAT_DATA;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                string paramVal = line.Substring(16, 10).Trim();
                turbineDataModel.AUSGL = Convert.ToDouble(paramVal);
                break;
            }
        }
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
        // Console.WriteLine(message);
    }
}