using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Handlers.Custom_DAT_Handler;
using Handlers.CustomDatReadCustomFlow;
using Handlers.DAT_Handler;
using HMBD.Cu_Ref_DAT_Selector;
using HMBD.CustomPower_KNN;
using Interfaces.ILogger;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
namespace Handlers.CustomPathDeletePrepare;

using Optimizers.PSOFlowPathNozzle;
using StartExecutionMain;
// using Excel = Microsoft.Office.Interop.Excel;

public class CustomDatFileHandler
{
    TurbaOutputModel turbaOutputModel;
    TurbineDataModel turbineDataModel;
    ILogger logger;
    public CustomDatFileHandler()
    {
        turbaOutputModel = TurbaOutputModel.getInstance();
        turbineDataModel = TurbineDataModel.getInstance();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void GetNearestParams_Custom()
    {

        PowerKNN("Nozzle");
        //for (int i = 0; i < 3; i++)
        //{
            MoveYAndSetParamsCustom();
            SelectExecutedFlowPath("Nozzle");
            GetFlowPathExecuted("Nozzle");
            LoadDatFile();
            ScanDATFile();
            //double constBeaufschl = turbineDataModel.BEAUFSCHL;//nvert.ToDouble(datData.Range("E2").Value);
            //double constRadkammer = turbineDataModel.RADKAMMER;//Convert.ToDouble(datData.Range("E3").Value);
            //double constDruckziff = turbineDataModel.DRUCK;//Convert.ToDouble(datData.Range("E4").Value);
            //double constInnendurch = turbineDataModel.INNNEN;//Convert.ToDouble(datData.Range("E5").Value);
            //double constAusgleich = turbineDataModel.AUSGL;//Convert.ToDouble(datData.Range("E6").Value);
            //RelationshipAwarePSOOptimizer.Position[i, 0] = constBeaufschl;
            //RelationshipAwarePSOOptimizer.Position[i, 1] = constRadkammer;
            //RelationshipAwarePSOOptimizer.Position[i, 2] = constDruckziff * -1;
            //RelationshipAwarePSOOptimizer.Position[i, 3] = constInnendurch;
            //RelationshipAwarePSOOptimizer.Position[i, 4] = constAusgleich;
        //}

    }

    private void PowerKNN(string parameter)
    {
        CustomPowerKNN customPowerKNN = new CustomPowerKNN();
        customPowerKNN.ExecutePowerKNN(parameter);
        // Implement the PowerKNN logic here
    }

    private void MoveYAndSetParamsCustom()
    {
        CuFlowPathSelector cuFlowPathSelector = new CuFlowPathSelector();
        cuFlowPathSelector.MoveYAndSetParamsCustom();
        // Implement the MoveYAndSetParamsCustom logic here
    }

    private void SelectExecutedFlowPath(string parameter)
    {
        CuFlowPathSelector cuFlowPathSelector = new CuFlowPathSelector();
        cuFlowPathSelector.SelectExecutedFlowPath(parameter);
        // Implement the SelectExecutedFlowPath logic here
    }

    private void GetFlowPathExecuted(string parameter)
    {
        CuFlowPathSelector cuFlowPathSelector = new CuFlowPathSelector();
        cuFlowPathSelector.GetFlowPathExecuted(parameter);

        // Implement the GetFlowPathExecuted logic here
    }

    private void LoadDatFile()
    {
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        customDATFileProcessor.LoadDatFile();
        // Implement the LoadDatFile logic here
    }

    private void ScanDATFile()
    {
        CustomReadDatFileHandler customReadDatFileHandler = new CustomReadDatFileHandler();
        customReadDatFileHandler.ScanDATFile();


        // Implement the ScanDATFile logic here
    }

    public void VaricodeDelete(string parameter)
    {
        int parameterPos = 17;
        int parameterLen = parameter.Length;
        // int datEoF = datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                fileLines[lineNumber] = "!" + line;
                break;
            }
        }
    }

    public void ABSTANDDelete()
    {
        string parameter = "!     ABSTAND AXIALLAGER";
        int parameterPos = 17;
        int parameterLen = parameter.Length;
        // int datEoF = datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber]; //datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                fileLines[lineNumber] = "!" + line;
                break;
            }
        }
    }

    public void PositionDelete()
    {
        string parameter = "!     POSITION DER";
        int parameterPos = 17;
        int parameterLen = parameter.Length;
        List<string> fileLines = turbineDataModel.DAT_DATA;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                fileLines[lineNumber] = "!" + line;
                break;
            }
        }
    }

    public void Nozzle_Steampath()
    {
        string parameter1 = "!     ANZAHL";
        string parameter2 = "!LP2  DAMPFMENGE";
        List<string> fileLines = turbineDataModel.DAT_DATA;

        int startLine = 0;
        int endLine = 0;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter1))
            {
                startLine = lineNumber;
            }
            else if (line.Contains(parameter2))
            {
                endLine = lineNumber;
                break;
            }
        }

        if (startLine > 0 && endLine > 0)
        {
            for (int lineNumber = startLine + 1; lineNumber < endLine; lineNumber++)
            {
                string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
                fileLines[lineNumber] = "!" + line;
            }
        }
    }

    public void Set0Stages()
    {
        string parameter = "!ND   DM REGELR";
        int parameterPos = 17;
        List<string> fileLines = turbineDataModel.DAT_DATA;

        int parameterLen = parameter.Length;
        // int datEoF = datDataSheet.Cells[datDataSheet.Rows.Count, "B"].End(Excel.XlDirection.xlUp).Row;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                line = fileLines[lineNumber + 1]; //datDataSheet.Cells[lineNumber + 1, "B"].Value.ToString();
                if (line.Length >= 65)
                {
                    line = line.Substring(0, 63) + "00" + line.Substring(65);
                }
                fileLines[lineNumber + 1] = line;
                // datDataSheet.Cells[lineNumber + 1, "B"].Value = line;
                break;
            }
        }
    }

    public void WriteDatFile()
    {
        Logger("Writing back to DAT File..");
        string filePath = "C:\\testDir\\TURBATURBAE1.DAT.DAT";
        string[] file = File.ReadAllLines(filePath);

        List<string> fileLine = turbineDataModel.DAT_DATA;
    
        try
        {
            File.WriteAllLines(filePath, fileLine);
            Logger("Finished writing to DAT File.");
        }
        catch (Exception ex)
        {
            Logger($"Error writing to DAT File: {ex.Message}");
        }

        Logger("Finished writing to DAT File.");
    }

    public double GetParamWheelChamberPressure()
    {
        LoadDatFile();
        string parameter = "!     RADKAMMER- UND ZWISCHENDRUECKE";
        List<string> fileLines = turbineDataModel.DAT_DATA;

        for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
        {
            string line = fileLines[lineNumber];//datDataSheet.Cells[lineNumber, "B"].Value.ToString();
            if (line.Contains(parameter))
            {
                line = fileLines[lineNumber + 1];//datDataSheet.Cells[lineNumber + 1, "B"].Value.ToString();
                string[] values = line.Trim().Split(' ');
                double paramVal = Convert.ToDouble(values[0]);
                return paramVal;
            }
        }

        return 0;
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
        Console.WriteLine(message);
    }
}
