using System;
using System.IO;
// using Microsoft.Office.Interop.Excel;
using Models.LoadPointDataModel;

using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.ILogger;
using Models.PreFeasibility;
using Interfaces.IThermodynamicLibrary;
using Models.TurbineData;
using Turba.TurbaConfiguration;
using HMBD.HMBDInformation;
using Models.NozzleTurbaData;
// using Interfaces.ILogger;
// using Models.LoadPointDataModel;
using Models.PowerEfficiencyData;
using Exec_HMBD_Configuration;
using HMBD.Exec_LoadPointGenerator;
namespace Handlers.Exec_DAT_Handler;
// update turbinespec check rpm 
// ex_power_knn check
// check datfileintin also
// const_RADKAMMER
public class ExecutedDATFileProcessor
{
    // private Application excelApp;
    LoadPointDataModel loadPointDataModel;
    TurbineDataModel turbineDataModel;
    PowerEfficiencyModel powerEfficiencyModel;
    NozzleTurbaDataModel nozzleTurbaDataModel;
    IThermodynamicLibrary thermodynamicService;
    PreFeasibilityDataModel preFeasibilityDataModel;
    IConfiguration configuration;
    ILogger logger;
    int maxLoadPoints;
    // private Workbook workbook;
    // private Worksheet mySheet;
    // check executed one for sure

    public ExecutedDATFileProcessor()
    {
      loadPointDataModel = LoadPointDataModel.getInstance();
      configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
      maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
      logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
      thermodynamicService =  MainExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
      preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
      turbineDataModel = TurbineDataModel.getInstance();
      powerEfficiencyModel = PowerEfficiencyModel.getInstance();
      nozzleTurbaDataModel = NozzleTurbaDataModel.getInstance();
        // excelApp = new Application();
        // workbook = excelApp.Workbooks.Open("path_to_your_excel_file.xlsx");
    }

    public void LoadDatFile()
    {
       string filePath = "C:\\testDir\\TURBATURBAE1.DAT.DAT";
    //    string [] file = File.ReadAllLines(filePath);
       
       string[] dat_data = File.ReadAllLines(filePath);//turbineDataModel.DAT_DATA;
       

       turbineDataModel.DAT_DATA=dat_data.ToList();
    }

    public void PrepareDatFile(int mxLPs = 0)
    {
        LoadDatFile();
        LoadLP1FromDat();
        DeleteRowAfterFirstLoadPoint();
        InsertDataLineUnderFirstLPFixed();
        DeleteLoadPoints();
        InsertLoadPointsWithExactFormattingUsingMid(mxLPs);
        int totalLps = (mxLPs >= 1) ? (mxLPs - 1) : 0;
        InsertDataLineUnderND(totalLps);
        Logger("Load points written into DAT file...");
        DatFileInitParamsExceptLP();
        Logger("Updated the DAT file...");
    }
    public void PrepareDatFileExecuted(int mxLPs = 0)
        {
            LoadDatFile();
            LoadLP1FromDat();
            DeleteRowAfterFirstLoadPoint();
            InsertDataLineUnderFirstLPFixed();
            DeleteLoadPoints();
            InsertLoadPointsWithExactFormattingUsingMid(mxLPs);
            int totalLps = (mxLPs >= 1) ? (mxLPs - 1) : 0;
            InsertDataLineUnderND(totalLps);
            Logger("Load points written into DAT file...");
            DatFileInitParamsExceptLPExecuted();
            Logger("Updated the DAT file...");
        }

      public void PrepareDatFileOnlyLPUpdate(int mxLPs = 0)
        {
            LoadDatFile();
            LoadLP1FromDat();
            DeleteRowAfterFirstLoadPoint();
            InsertDataLineUnderFirstLPFixed();
            DeleteLoadPoints();
            InsertLoadPointsWithExactFormattingUsingMid(mxLPs);
            int totalLps = (mxLPs >= 1) ? (mxLPs - 1) : 0;
            InsertDataLineUnderND(totalLps);
        }



    public void LoadLP1FromDat()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        bool previousLineWasLP1 = false;
        int numberCount = 0;
        List<LoadPoint> lpPoint = loadPointDataModel.LoadPoints;
        using (StreamReader sr = new StreamReader(filePath))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (previousLineWasLP1)
                {
                    string[] dataArray = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    // int col = 2;
                    lpPoint[0].MassFlow = Convert.ToDouble(dataArray[0]);
                    lpPoint[0].Rpm = Convert.ToDouble(dataArray[1]);
                    lpPoint[0].Pressure = Convert.ToDouble(dataArray[2]);
                    lpPoint[0].Temp = Convert.ToDouble(dataArray[3]);
                    lpPoint[0].InFlow = Convert.ToDouble(dataArray[4]);
                    lpPoint[0].BackPress = Convert.ToDouble(dataArray[5]);
                    lpPoint[0].BYP = Convert.ToDouble(dataArray[6]);
                    lpPoint[0].EIN = Convert.ToDouble(dataArray[7]);
                    lpPoint[0].WANZ = Convert.ToDouble(dataArray[8]);
                    lpPoint[0].RSMIN = Convert.ToDouble(dataArray[9]);
                    // foreach (string data in dataArray)
                    // {
                    //     if (!string.IsNullOrWhiteSpace(data))
                    //     {
                            
                    //         workbook.Worksheets["LOAD_POINTS"].Cells[2, col].Value = data;
                    //         col++;
                    //         numberCount++;

                    //         if (numberCount == 10)
                    //             break;
                    //     }
                    // }

                    break;
                }

                if (line.StartsWith("!LP1"))
                {
                    previousLineWasLP1 = true;
                }
            }
        }
    }

    void DeleteLoadPoints()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        // Check if the file exists
        if (!File.Exists(filePath))
        {
            logger.LogInformation("File not found: " + filePath);
            return;
        }
        try
        {
            // Read the entire file content into an array of lines
            string[] lines = File.ReadAllLines(filePath);
            bool firstLPFound = false;
            string newContent ="";

            // Loop through the lines and delete the load points after the first occurrence
            for (int i = 0; i < lines.Length ; i++)
            {
                if (lines[i].Trim().StartsWith("!LP"))
                {
                    if (firstLPFound)
                    {
                        // Clear the current line and the next one
                        lines[i] = string.Empty;
                        if (i + 1 < lines.Length)
                            lines[i + 1] = string.Empty;
                        i++;
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                        
                        // Mark that the first load point has been found
                        firstLPFound = true;
                    }
                }
                else{
                    newContent += (lines[i] + Environment.NewLine);
                }
            }
            File.WriteAllText(filePath, newContent);
            logger.LogInformation("Load points deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("DeleteLoadPoints", ex.Message);
        }
    }
    // public void DeleteLoadPoints()
    // {
    //     string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
    //     string fileContent = File.ReadAllText(filePath);
    //     string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
    //     bool firstLPFound = false;

    //     for (int i = 0; i < lines.Length; i++)
    //     {
    //         if (lines[i].Trim().StartsWith("!LP"))
    //         {
    //             if (firstLPFound)
    //             {
    //                 lines[i] = string.Empty;
    //                 lines[i + 1] = string.Empty;
    //             }
    //             else
    //             {
    //                 firstLPFound = true;
    //             }
    //         }
    //     }

    //     File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
    //     Console.WriteLine("Load points deleted successfully.");
    // }

    void DeleteRowAfterFirstLoadPoint()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        if (!File.Exists(filePath))
        {
            logger.LogInformation("File not found: " + filePath);
            return;
        }
        try
        {
            // Read the entire file content into an array of lines
            string[] lines = File.ReadAllLines(filePath);
            bool lpFound = false;

            // Loop through the lines and delete the line after the first occurrence of "!LP"
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (!lpFound && lines[i].Trim().StartsWith("!LP"))
                {
                    lpFound = true; // Mark the first LP found
                }
                else if (lpFound)
                {
                    // Delete the line immediately after the first LP row
                    lines[i] = string.Empty;
                    break; // Exit the loop after deleting the line
                }
            }
            string newContent = string.Join(Environment.NewLine, lines);
            // Write the new content back to the file
            File.WriteAllText(filePath, newContent);
            logger.LogInformation("Row after first load point deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("DeleteRowAfterFirstLoadPoint", ex.Message);
        }
    }
    // public void DeleteRowAfterFirstLoadPoint()
    // {
    //     string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
    //     string fileContent = File.ReadAllText(filePath);
    //     string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
    //     bool lpFound = false;

    //     for (int i = 0; i < lines.Length; i++)
    //     {   
    //         if (!lpFound && lines[i].Trim().StartsWith("!LP"))
    //         {
    //             lpFound = true;
    //         }
    //         else if (lpFound)
    //         {
    //             lines[i] = string.Empty;
    //             break;
    //         }
    //     }

    //     File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
    //     Console.WriteLine("Row after first load point deleted successfully.");
    // }

    public void InsertDataLineUnderFirstLPFixed()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = File.ReadAllText(filePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        int insertIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("!LP"))
            {
                insertIndex = i + 1;
                break;
            }
        }

        if (insertIndex == -1)
        {
            Console.WriteLine("Line starting with 'LP' not found.");
            return;
        }

        
        
        List<LoadPoint> lpPoint = loadPointDataModel.LoadPoints;
        string [] loadPoints = new string[10];
        loadPoints[0]=lpPoint[1].MassFlow.ToString();
        loadPoints[1]=lpPoint[1].Rpm.ToString();
        loadPoints[2]=lpPoint[1].Pressure.ToString();
        loadPoints[3]=lpPoint[1].Temp.ToString();
        loadPoints[4]=lpPoint[1].EIN.ToString();
        loadPoints[5]=lpPoint[1].BackPress.ToString();
        loadPoints[6]=lpPoint[1].BYP.ToString();
        loadPoints[7]=lpPoint[1].EIN.ToString();
        loadPoints[8]=lpPoint[1].WANZ.ToString();
        loadPoints[9]=lpPoint[1].RSMIN.ToString();
        string newDataLine = new string(' ', 88);
        newDataLine = InsertFormattedValues(ref newDataLine, loadPoints);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
                for (int i = 0; i < insertIndex; i++)
                {
                    writer.WriteLine(lines[i]);
                }

                writer.WriteLine(newDataLine);
                for (int i = insertIndex + 1; i < lines.Length; i++)
                {
                    writer.WriteLine(lines[i]);
                }
        }

        Console.WriteLine("First data line inserted successfully under the first LP line.");
    }
     string InsertFormattedValues(ref string dataLine, string[] loadPointData)
    {
        // Insert formatted values into the data line
        dataLine = InsertValue(dataLine, loadPointData[0], 7, 10, "{0:00000.000}");
        dataLine = InsertValue(dataLine, loadPointData[1], 17, 10, "{0:00000.000}");
        dataLine = InsertValue(dataLine, loadPointData[2], 27, 10, "{0:00000.000}");
        dataLine = InsertValue(dataLine, loadPointData[3], 37, 10, "{0:00000.000}");
        dataLine = InsertValue(dataLine, loadPointData[4], 47, 10, "{0:00000.000}");
        dataLine = InsertValue(dataLine, loadPointData[5], 57, 10, "{0:000.00000}");
        dataLine = InsertNumberValue(dataLine, loadPointData[6], 69, 5);
        dataLine = InsertNumberValue(dataLine, loadPointData[7], 74, 5);
        dataLine = InsertNumberValue(dataLine, loadPointData[8], 79, 5);
        dataLine = InsertNumberValue(dataLine, loadPointData[9], 84, 4);

        return dataLine;
    }
    string InsertNumberValue(string line, string value, int start, int length)
    {
        string formattedValue = string.Format("{0:0}", Convert.ToDouble(value));
        //Console.WriteLine(value + " 586");
        if (Convert.ToDouble(value) < 0)
        {
            line = line.Remove(start - 1, formattedValue.Length).Insert(start - 1, formattedValue);
        }
        else
        {
            line = line.Remove(start, formattedValue.Length).Insert(start, formattedValue);
        }
        return line;
    }
    string InsertValue(string line, string value, int start, int length, string format)
    {
        string formattedValue = string.Format(format, Convert.ToDouble(value));
        return line.Remove(start - 1, formattedValue.Length).Insert(start - 1, formattedValue);
    }

   void InsertLoadPointsWithExactFormattingUsingMid(int maxLp =0)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        if (maxLp != 0)
        {
            maxLoadPoints = maxLp;
        }
        string[] lines;
        string newContent = "";
        int insertIndex = -1;
        int rowNum = 6; // Assuming the data starts from row 6
        int lpCounter = 2; // Starting from LP2
        try{
            // Read the entire file content into an array of lines
            RetryFile:
            if (IsFileReadyForOpen(filePath))
            {
                lines = File.ReadAllLines(filePath);
            }
            else
            {
                goto RetryFile;
            }

            // Find the index to insert new load points before the line starting with "!     PN"
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("!") && lines[i].Trim().IndexOf("PN", 1) > 0)
                {
                    insertIndex = i;
                    break;
                }
            }
            if (insertIndex == -1)
            {
                Console.WriteLine("Line starting with '!     PN' not found.");
                return;
            }
            // Prepare new content with new load points
            for (int i = 0; i < insertIndex; i++)
            {
                newContent += lines[i] + Environment.NewLine;
            }
        for(int lp = 2; lp <= maxLoadPoints; ++lp){
                if (loadPointDataModel.LoadPoints[lp].MassFlow == 0) continue;
                string[] loadPointData = new string[10];
            loadPointData[0] = loadPointDataModel.LoadPoints[lp].MassFlow.ToString();
            loadPointData[1] = loadPointDataModel.LoadPoints[1].Rpm.ToString() ;
            loadPointData[2] = loadPointDataModel.LoadPoints[lp].Pressure.ToString();
            loadPointData[3] = loadPointDataModel.LoadPoints[lp].Temp.ToString();
            loadPointData[4] = loadPointDataModel.LoadPoints[lp].InFlow.ToString();
            loadPointData[5] = loadPointDataModel.LoadPoints[lp].BackPress.ToString();
            loadPointData[6] = loadPointDataModel.LoadPoints[lp].BYP.ToString();
            loadPointData[7] = loadPointDataModel.LoadPoints[lp].EIN.ToString();
            loadPointData[8] = loadPointDataModel.LoadPoints[lp].WANZ.ToString();
            loadPointData[9] = loadPointDataModel.LoadPoints[lp].RSMIN.ToString();
            // Create the load point title line
            string lpLine = "!LP" + lpCounter;
            lpLine = lpLine.PadRight(6) + "DAMPFMENGE";
            lpLine = lpLine.PadRight(17) + "DREHZAHL";
            lpLine = lpLine.PadRight(26) + "P FRISCHD";
            lpLine = lpLine.PadRight(36) + "T FRISCHD";
            lpLine = lpLine.PadRight(46) + "EINSTROEM";
            lpLine = lpLine.PadRight(56) + "GEGENDRUCK";
            lpLine = lpLine.PadRight(68) + "BYP";
            lpLine = lpLine.PadRight(73) + "EIN";
            lpLine = lpLine.PadRight(77) + "WANZ";
            lpLine = lpLine.PadRight(82) + "RSMIN";
            lpLine = lpLine.PadRight(87);
            
            // Create the load point data line using exact positioning
            string dataLine = new string(' ', 88);
            InsertFormattedValues(ref dataLine, loadPointData);

            // Add the new load point title and data lines to the content
            newContent += lpLine + Environment.NewLine;
            newContent += dataLine + Environment.NewLine;

            rowNum++;
            lpCounter++;
        }
            // Append the remaining lines from the original file
            for (int i = insertIndex; i < lines.Length; i++)
            {
                newContent += lines[i] + Environment.NewLine;
            }

            // Write the new content back to the file
            RetryFile2:
            if (IsFileReadyForOpen(filePath))
            {
                File.WriteAllText(filePath, newContent);
            }
            else
            {
                goto RetryFile2;
            }
            //Console.WriteLine("New load points inserted successfully.");
             logger.LogInformation("New load points inserted successfully.");
        }
        catch(Exception ex){
            // logger.LogError("InsertLoadPointsWithExactFormattingUsingMid", ex.Message);
        }
    }
    bool IsFileReadyForOpen(string filePath)
    {
        try
        {
            using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            return false;
        }

        return true;
    }

    public void InsertDataLineUnderND(int maxLoadPoints = 0)
    {
       int lastLoadPointNumber = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        if (maxLoadPoints != 0)
        {
            lastLoadPointNumber = maxLoadPoints;
        }
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = "";
        string[] fileLines;

        // Read the entire file content into a string
        bool fileReady = false;
        while (!fileReady)
        {
            try
            {
                fileContent = File.ReadAllText(filePath);  // Attempt to read the file
                fileReady = true;  // If we successfully read the file, exit the loop
            }
            catch (IOException ex)
            {
                // logger.LogError("InsertDataLineUnderND", ex.Message);
                // If we catch an IOException, retry after a short delay
                Console.WriteLine("File is not ready, retrying.. Error : "+ ex.Message);
            }
        }

        // Split the file content into an array of lines
        fileLines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string parameter = "!ND   DM REGELR BEAUFSCHL";

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                lineNumber++; // Move to the next line
                line = fileLines[lineNumber];
                
                // Update the last load point number
                line = line.Substring(0, 53) + lastLoadPointNumber.ToString("00") + line.Substring(55);
                fileLines[lineNumber] = line; // Save the updated line
            }
        }

        // Join the lines back into a single string
        fileContent = string.Join(Environment.NewLine, fileLines);

        // Write the new content back to the file
        RetryFile2:
        try
        {
            File.WriteAllText(filePath, fileContent);
        }
        catch (IOException)
        {
            goto RetryFile2; 
        }
        logger.LogInformation("Successfully updated last load point number.");
    }
    public void DatFileInitParamsExceptLPExecuted()
        {
            try{
            // Values from HBD (Power Cycle) calculations
            Console.WriteLine("MASSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS"+thermodynamicService.GetInletVelocity(turbineDataModel.MassFlowRate));
            turbineDataModel.InletVelocity = thermodynamicService.GetInletVelocity(turbineDataModel.MassFlowRate);
            double inletVelocityFromHBD = turbineDataModel.InletVelocity;//36.900;
            turbineDataModel.VolumetricFlow = thermodynamicService.getVolumetricFlow();
            double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;
            double estimatedPowerFromHBD = turbineDataModel.AK25;
            if (estimatedPowerFromHBD < 1000)
            {
                estimatedPowerFromHBD = 1001;
            }

            // Variables for calculations
            double nozzleCount, nozzleFront;
            double genPower  = 1000, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
            double turbinePower, gearboxPower;

            Logger("Calculating nozzles and powertrain specs to write into DAT file...");

            // int rowIndex = 0;
            PowerEfficiencyDataPoint reqPowDataPoint = new PowerEfficiencyDataPoint();
            foreach(PowerEfficiencyDataPoint powerEffPoint in powerEfficiencyModel.PowerEfficiencyPoints){
                double pow_value = powerEffPoint.Power;
                if (pow_value * 1000 <= estimatedPowerFromHBD)
                {
                    reqPowDataPoint = powerEffPoint;
                }
                else
                {
                    genPower = pow_value * 1000;//estimatedPowerFromHBD;
                    break;
                }
            }
            // genPower = estimatedPowerFromHBD;
            genEff4_4 = reqPowDataPoint.Eff100;
            genEff3_4 = reqPowDataPoint.Eff75;
            genEff1_2 = reqPowDataPoint.Eff50;
            genEff1_4 = reqPowDataPoint.Eff25;

            Logger($"Generator ex_Power_KNN.Power kW / Eff :  {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
            UpdateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);

            // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
            execHMBDConfiguration.HBDUpdateEffGenerator(genEff4_4);

            // Gearbox Power calculation
            gearboxPower = genPower / (genEff4_4 / 100);
            Logger($"GearBox ex_Power_KNN.Power kW {gearboxPower}");
            UpdateGearboxSpecs(gearboxPower);

            // Turbine Power calculation
            turbinePower = gearboxPower / 0.86;
            Logger($"Turbine Shaft ex_Power_KNN.Power kW: {gearboxPower}");
            UpdateTurbineSpecs(turbinePower);

            Logger($"Inlet flow velocity from HBD: {inletVelocityFromHBD}");
            UpdateVari27(inletVelocityFromHBD);

            NozzleTurbaData reqNozzleTurbaData = new NozzleTurbaData();
            foreach(NozzleTurbaData nozzleTurbaData in nozzleTurbaDataModel.NozzleTurbaDataList){
                string[] flowCells = nozzleTurbaData.InletVolumetricFlowRange.Split('-');
                double lowerVolFlow = Convert.ToDouble(flowCells[0]);
                double higherVolFlow = Convert.ToDouble(flowCells[1]);

                if (volumetricFlowFromHBD >= lowerVolFlow)
                {
                    reqNozzleTurbaData = nozzleTurbaData;
                }
                else
                {
                    break;
                }
            }
            
            nozzleCount = reqNozzleTurbaData.FminGes;
            nozzleFront = reqNozzleTurbaData.Fmin1;
            reqNozzleTurbaData.Remark = "Selected";
            UpdateNozzleSpecs(0,0);
            // Logger($"Volumetric Flow from HBD: {volumetricFlowFromHBD}");
            // Logger($"Selected Nozzle VolFlow Cells: {reqNozzleTurbaData.InletVolumetricFlowRange}");
            // UpdateNozzleSpecs(nozzleCount, nozzleFront);
            // Logger($"Initial Nozzle in G1: {nozzleFront} Total: {nozzleCount}");
        }
        catch(Exception ex){
            logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
        }
        }
    public void DatFileInitParamsExceptLP()
    {
         try{
            // Values from HBD (Power Cycle) calculations
            turbineDataModel.InletVelocity = thermodynamicService.GetInletVelocity(turbineDataModel.MassFlowRate);
            double inletVelocityFromHBD = turbineDataModel.InletVelocity;//36.900;
            turbineDataModel.VolumetricFlow = thermodynamicService.getVolumetricFlow();
            double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;
            double estimatedPowerFromHBD = turbineDataModel.AK25;
            if (estimatedPowerFromHBD < 1000)
            {
                estimatedPowerFromHBD = 1001;
            }

            // Variables for calculations
            double nozzleCount, nozzleFront;
            double genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
            double turbinePower, gearboxPower;

            Console.WriteLine("Calculating nozzles and powertrain specs to write into DAT file...");

            // int rowIndex = 0;
            PowerEfficiencyDataPoint reqPowDataPoint = new PowerEfficiencyDataPoint();
            foreach(PowerEfficiencyDataPoint powerEffPoint in powerEfficiencyModel.PowerEfficiencyPoints){
                double pow_value = powerEffPoint.Power;
                if (pow_value * 1000 <= estimatedPowerFromHBD)
                {
                    reqPowDataPoint = powerEffPoint;
                }
                else
                {
                    break;
                }
            }
            genPower = estimatedPowerFromHBD;
            genEff4_4 = reqPowDataPoint.Eff100;
            genEff3_4 = reqPowDataPoint.Eff75;
            genEff1_2 = reqPowDataPoint.Eff50;
            genEff1_4 = reqPowDataPoint.Eff25;

            Logger($"Generator ex_Power_KNN.Power kW / Eff : {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
            UpdateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);
            ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
            // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            execHMBDConfiguration.HBDUpdateEffGenerator(genEff4_4);

            // Gearbox Power calculation
            gearboxPower = genPower / (genEff4_4 / 100);
            Logger($"GearBox ex_Power_KNN.Power kW :  {gearboxPower}");
            UpdateGearboxSpecs(gearboxPower);

            // Turbine Power calculation
            turbinePower = gearboxPower / 0.86;
            Logger($"Turbine Shaft ex_Power_KNN.Power kW:  {gearboxPower}");
            UpdateTurbineSpecs(turbinePower);

            Logger($"Inlet flow velocity from HBD: {inletVelocityFromHBD}");
            UpdateVari27(inletVelocityFromHBD);

            NozzleTurbaData reqNozzleTurbaData = new NozzleTurbaData();
            foreach(NozzleTurbaData nozzleTurbaData in nozzleTurbaDataModel.NozzleTurbaDataList){
                string[] flowCells = nozzleTurbaData.InletVolumetricFlowRange.Split('-');
                double lowerVolFlow = Convert.ToDouble(flowCells[0]);
                double higherVolFlow = Convert.ToDouble(flowCells[1]);

                if (volumetricFlowFromHBD >= lowerVolFlow)
                {
                    reqNozzleTurbaData = nozzleTurbaData;
                }
                else
                {
                    break;
                }
            }
            
            nozzleCount = reqNozzleTurbaData.FminGes;
            nozzleFront = reqNozzleTurbaData.Fmin1;
            reqNozzleTurbaData.Remark = "Selected";

            Logger($"Volumetric Flow from HBD: {volumetricFlowFromHBD}");
            Logger($"Selected Nozzle VolFlow Cells: {reqNozzleTurbaData.InletVolumetricFlowRange}");
            UpdateNozzleSpecs(nozzleCount, nozzleFront);
            Logger($"Initial Nozzle in G1: {nozzleFront} Total: {nozzleCount}");
        }
        catch(Exception ex){
            // logger.LogError("")
            logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
        }
    }

     public void Logger(string message){

      logger.LogInformation(message);
     }
    public void UpdateGeneratorSpecs(double paramGenPower, double paramGenEff4_4, double paramGenEff3_4, double paramGenEff1_2, double paramGenEff1_4)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = File.ReadAllText(filePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        string Parameter = "!     PN GENO";

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(Parameter))
            {
                i++;
                string line = lines[i];
                line = line.Remove(6, 9).Insert(6, paramGenPower.ToString("00000.000"));
                line = line.Remove(26, 9).Insert(26, paramGenEff4_4.ToString("00000.000"));
                line = line.Remove(36, 9).Insert(36, paramGenEff3_4.ToString("00000.000"));
                line = line.Remove(46, 9).Insert(46, paramGenEff1_2.ToString("00000.000"));
                line = line.Remove(56, 9).Insert(56, paramGenEff1_4.ToString("00000.000"));
                lines[i] = line;
            }
        }

        File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
        Console.WriteLine("Generator specs updated successfully.");
    }

    public void UpdateGearboxSpecs(double paramGearboxPower)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = File.ReadAllText(filePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        string Parameter = "!     PN GETR";

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(Parameter))
            {
                i++;
                string line = lines[i];
                line = line.Remove(6, 9).Insert(6, paramGearboxPower.ToString("00000.000"));
                lines[i] = line;
            }
        }

        File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
        Console.WriteLine("Gearbox specs updated successfully.");
    }

    public void UpdateTurbineSpecs(double paramTurbinePower)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = File.ReadAllText(filePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        string Parameter = "!     PN TURB";
        ExecLoadPointGenerator execLoadPointGenerator = new ExecLoadPointGenerator();

        double finalRpm = execLoadPointGenerator.RPM(); // Replace with actual RPM value

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(Parameter))
            {
                i++;
                string line = lines[i];
                line = line.Remove(6, 9).Insert(6, paramTurbinePower.ToString("00000.000"));
                line = line.Remove(16, 9).Insert(16, finalRpm.ToString("00000.000"));
                lines[i] = line;
            }
        }

        File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
        Console.WriteLine("Turbine specs updated successfully.");
    }

    public void UpdateNozzleSpecs(double paramNozzleCount, double paramNozzleFront)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines = new string[0];//= File.ReadAllLines(filePath);
        string parameter = "!     ANZAHL";
        bool fileReady = false;
        try{
            while (!fileReady)
            {
                try
                {
                    fileLines = File.ReadAllLines(filePath);
                    fileReady = true;  // If we successfully read the file, exit the loop
                }
                catch (IOException ex)
                {
                    logger.LogError("udpateNozzleSpecs", ex.Message);
                }
            }
            for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
            {
                if (fileLines[lineNumber].Contains(parameter))
                {
                    lineNumber++;
                    string line = fileLines[lineNumber];

                    string totalString = line.Substring(26, 9).Trim();
                    string naString = line.Substring(16, 9).Trim();

                    // Convert to double with error handling
                    if (turbineDataModel.OldNa == 0 && turbineDataModel.OldNb == 0)
                    {
                        if (double.TryParse(naString, out double oldNaValue))
                        {
                            turbineDataModel.OldNa = oldNaValue;
                            paramNozzleFront = oldNaValue;
                        }
                        else
                        {
                            turbineDataModel.OldNa = 0.0;  // Default value
                        }

                        if (double.TryParse(totalString, out double totalStringValue))
                        {
                            paramNozzleCount = totalStringValue;
                            turbineDataModel.OldNb = totalStringValue - turbineDataModel.OldNa;
                        }
                        else
                        {
                            turbineDataModel.OldNb = 0.0;  // Default value
                        }
                    }
                    // Nozzle count and front
                    line = line.Remove(26, 9).Insert(26, paramNozzleCount.ToString("00000.000"));
                    line = line.Remove(16, 9).Insert(16, paramNozzleFront.ToString("00000.000"));
                    
                    fileLines[lineNumber] = line;
                }
            }
            File.WriteAllLines(filePath, fileLines);
            logger.LogInformation("Nozzle specs updated.");
        }
        catch(Exception ex){
            logger.LogError("updateNozzleSpecs", ex.Message);
        }
    }

    public void UpdateVari27(double paramNewValue)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = "";
        string[] fileLines;


        bool fileReady = false;
        while (!fileReady)
        {
            try
            {
                fileContent = File.ReadAllText(filePath);  // Attempt to read the file
                fileReady = true;  // If we successfully read the file, exit the loop
            }
            catch (IOException ex)
            {
                logger.LogError("updateVari27", ex.Message);
            }
        }
        // Split the file content into an array of lines
        fileLines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string parameter = "!     SCHLUESSEL VARI";
        int parameterPos = 43; // Position based on original code

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line.Contains(parameter))
            {
                for (int lineNumberNested = lineNumber; lineNumberNested < fileLines.Length; lineNumberNested++)
                {
                    lineNumber = lineNumberNested; // Update the outer line number
                    line = fileLines[lineNumber];

                    // If the end of section is detected, jump to the next section
                    if (line.Contains("!ND   DM REGELR"))
                    {
                        break; // Exit the nested loop
                    }

                    // Check for the specific value to update
                    if (line.Substring(0,16).Substring(9).Trim() == "27.000")
                    {
                        // Update the parameter value
                        string newValue = paramNewValue.ToString("00000.000");
                        line = line.Substring(0, 16) + newValue + line.Substring(25); // Update the line
                        fileLines[lineNumber] = line;
                        goto ReturnSub;//break; // Exit the nested loop after updating
                    }
                }
            }
        }

        ReturnSub:
        // Join the lines back into a single string
        fileContent = string.Join(Environment.NewLine, fileLines);
        
        // Write the new content back to the file
        RetryFile2:
        try
        {
            File.WriteAllText(filePath, fileContent);
        }
        catch (IOException)
        {
            goto RetryFile2; // Retry if file is not ready
        }
        logger.LogInformation("Successfully updated varicode 27 Value to: " + paramNewValue);
    
        Console.WriteLine($"Vari27 value updated to: {paramNewValue}");
    }

    public bool IsWheelChamberPressureValid()
    {
        // Worksheet datSheet = workbook.Worksheets["DAT_DATA"];
        // Worksheet prefeas = workbook.Worksheets["Pre-Feasibility checks"];
        
        double const_RADKAMMER = Convert.ToDouble(GetParam_RADKAMMER());
        double inletPressure = preFeasibilityDataModel.InletPressureActualValue;
        double backPressure = preFeasibilityDataModel.BackpressureActualValue; ///double.Parse(prefeas.Range["F9"].Value.ToString());

        return !(const_RADKAMMER < backPressure || const_RADKAMMER > 0.8 * inletPressure);
    }
    public string GetParam_RADKAMMER()
    {
        // Create Excel Application instance
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open(@"Path\To\Your\Workbook.xlsx");
        // Worksheet datData = workbook.Sheets["DAT_DATA"] as Worksheet;
        List<string> fileLines = turbineDataModel.DAT_DATA;
        string parameter = "RADKAMMER";
        int parameterPos, parameterLen;

        // Get the last used row in column B
        // Range lastCell = datData.Columns["B"].Cells[datData.Rows.Count];
        // int lastRow = lastCell.End[XlDirection.xlUp].Row;
        int lastRow = fileLines.Count;

        for (int lineNumber = 0; lineNumber < lastRow; lineNumber++)
        {
            // Range currentCell = datData.Cells[lineNumber, "B"];
            // string cellValue = currentCell.Value?.ToString() ?? "";
            string line = fileLines[lineNumber];  

            if (line.Contains(parameter))
            {
                lineNumber++;

                // Range nextCell = datData.Cells[lineNumber, "B"];
                // string line = nextCell.Value?.ToString() ?? "";
                string line1 = fileLines[lineNumber];

                // Extract the parameter value
                string paramVal = line1.Length >= 16 ? line1.Substring(6, 10).Trim() : "";

                // Set the value in cell E3
                return paramVal;

                // Exit the loop after first occurrence
                // break;
            }
        }
        return "0";
        
    }
}

