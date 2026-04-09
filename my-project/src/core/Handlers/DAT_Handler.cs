using System;
// using Microsoft.Office.Interop.Excel;
// using Excel = Microsoft.Office.Interop.Excel;
// using HMBD_Interface;
using OfficeOpenXml;
// using TurbineUtils;
using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Turba.TurbaConfiguration;
using HMBD.HMBDInformation;
using Models.NozzleTurbaData;
using Interfaces.ILogger;
using Models.LoadPointDataModel;
using Models.PowerEfficiencyData;
using DocumentFormat.OpenXml.Spreadsheet;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace Handlers.DAT_Handler;
public class DATFileProcessor
{
    private ExcelPackage package;
    private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
    IConfiguration configuration;
    IThermodynamicLibrary thermodynamicService;
    ILogger logger;

    TurbineDataModel turbineDataModel;
    PowerEfficiencyModel powerEfficiencyModel;
    NozzleTurbaDataModel nozzleTurbaDataModel;
    LoadPointDataModel loadPointDataModel;

    int maxLoadPoints = 10;
    public  DATFileProcessor()
    {        
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        thermodynamicService =  StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        turbineDataModel = TurbineDataModel.getInstance();
        powerEfficiencyModel = PowerEfficiencyModel.getInstance();
        nozzleTurbaDataModel = NozzleTurbaDataModel.getInstance();
        loadPointDataModel = LoadPointDataModel.getInstance();
        maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
    }
    public void PrepareDATFile(int mxLPs = 0)
    {
        try{
            LoadLP1FromDAT();
            DeleteRowAfterFirstLoadPoint();
            InsertDataLineUnderFirstLPFixed();
            DeleteLoadPoints();
            InsertLoadPointsWithExactFormattingUsingMid(mxLPs);
            //DeleteRowAfterND(); // Commented out as in VBA
            int totalLps = (mxLPs >= 1) ? (mxLPs - 1): 0;
            InsertDataLineUnderND(totalLps);
            Logger("Load points written into DAT file...");
            DatFileInitParamsExceptLP();
            Logger("Updated the DAT file...");
        }
        catch(Exception ex){
            logger.LogError("PrepareDATFile", ex.Message);
        }
    }

    public void PrepareDATFile_OnlyLPUpdate(int maxLps = 0)
    {
        try{
            LoadLP1FromDAT();
            DeleteRowAfterFirstLoadPoint();
            InsertDataLineUnderFirstLPFixed();
            DeleteLoadPoints();
            InsertLoadPointsWithExactFormattingUsingMid(maxLps);
            //if(maxLps!=0)
            int totalLps = (maxLps >= 1) ? (maxLps - 1) : 0;
            InsertDataLineUnderND(totalLps);
        }
        catch(Exception ex){
            logger.LogError("PrepareDATFile_OnlyLPUpdate", ex.Message);
        }
    }

    // Placeholder methods for those called in the original VBA code.
    public void DatFileInitParamsExceptLP()
    {
        try{
            // Values from HBD (Power Cycle) calculations
            turbineDataModel.InletVelocity = thermodynamicService.GetInletVelocity(turbineDataModel.MassFlowRate);
            double inletVelocityFromHBD = turbineDataModel.InletVelocity;//36.900;
            turbineDataModel.VolumetricFlow = thermodynamicService.getVolumetricFlow();
            double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;
            double estimatedPowerFromHBD =  turbineDataModel.AK25;
            if (estimatedPowerFromHBD < 1000)
            {
                estimatedPowerFromHBD = 1001;
            }
            // Variables for calculations
            double nozzleCount, nozzleFront;
            double genPower = 1000, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
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

            Logger($"Generator Power kW / Eff : {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
            updateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);

            HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            hBDPowerCalculator.HBDUpdateEffGenerator(genEff4_4);

            // Gearbox Power calculation
            gearboxPower = genPower / (genEff4_4 / 100);
            Logger($"Gearbox Power kW : {gearboxPower}");
            updateGearboxSpecs(gearboxPower);

            // Turbine Power calculation
            turbinePower = gearboxPower / 0.86;
            Logger($"Turbine Shaft Power kW: {gearboxPower}");
            updateTurbineSpecs(turbinePower);

            Logger($"Inlet flow velocity from HBD: {inletVelocityFromHBD}");
            updateVari27(inletVelocityFromHBD);

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
            updateNozzleSpecs(nozzleCount, nozzleFront);
            Logger($"Initial Nozzle in G1: {nozzleFront} Total: {nozzleCount}");
        }
        catch(Exception ex){
            logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
        }
    }

    public void LoadLP1FromDAT()
    {
        string datFilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        List<LoadPoint> loadPointsList = loadPointDataModel.LoadPoints;
        try
        {
            if (!File.Exists(datFilePath))
            {
                logger.LogInformation("File not found: " + datFilePath);
                return;
            }

            if (loadPointsList.Count == 0)
            {
                logger.LogInformation("Load Points List is Empty");
                return;
            }

            // Variables to track the process
            bool previousLineWasLP1 = false;
            int numberCount = 0;
            int col = 2;  // Start at column B in Excel (index 2)

            // Open and read the .dat file
            using (StreamReader sr = new StreamReader(datFilePath))
            {
                string line;
                
                // Loop through each line in the file
                while ((line = sr.ReadLine()) != null)
                {
                    // Check if the previous line was "!LP1"
                    if (previousLineWasLP1)
                    {
                        // Split the line into elements based on spaces
                        string[] dataArray = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        List<double> loadPointInfo = new List<double>();
                        foreach (string item in dataArray)
                        {
                            // Try to parse the item as a number
                            
                            if (double.TryParse(item, out double number))
                            {
                                loadPointInfo.Add(number);
                                // worksheet.Cells[2, col].Value = number;  // Write to row 2, starting at column B
                                col++;
                                numberCount++;

                                // Stop after writing 10 numbers
                                if (numberCount == 10)
                                {
                                    break;
                                }
                            }
                        }
                        
                        loadPointsList[0].MassFlow = loadPointInfo[0];
                        loadPointsList[0].Rpm = loadPointInfo[1];
                        loadPointsList[0].Pressure = loadPointInfo[2];
                        loadPointsList[0].Temp = loadPointInfo[3];
                        loadPointsList[0].InFlow = loadPointInfo[4];
                        loadPointsList[0].BackPress = loadPointInfo[5];
                        loadPointsList[0].BYP = loadPointInfo[6];
                        loadPointsList[0].EIN = loadPointInfo[7];
                        loadPointsList[0].WANZ = loadPointInfo[8];
                        loadPointsList[0].RSMIN = loadPointInfo[9];
                        // Stop processing after writing the first 10 numbers
                        break;
                    }

                    // Check if the current line starts with "!LP1"
                    if (line.StartsWith("!LP1"))
                    {
                        previousLineWasLP1 = true;
                    }
                }
            }
            logger.LogInformation("First 10 numbers after '!LP1' written to LoadPointDataModel.");
        }
        catch (Exception ex)
        {
            logger.LogError("LoadLP1FromDAT", ex.Message);
        }
    }
    public void DeleteLoadPoints()
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
    public void DeleteRowAfterFirstLoadPoint()
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
    public void InsertDataLineUnderFirstLPFixed()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string newDataLine = "";
        int insertIndex = -1;

        if (!File.Exists(filePath))
        {
            logger.LogInformation("File not found: " + filePath);
            return;
        }
        try
        {
            // Read all lines from the file
            // maybe later try-catch is needed, later we ll see
            string[] lines = File.ReadAllLines(filePath);
            // Find the index to insert new data line under the first LP line
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
                logger.LogInformation("Line starting with '!LP' not found.");
                return;
            }
            // Prepare the new data line with spaces and formatted values
            string[] loadPointData = new string[10];

            loadPointData[0] = loadPointDataModel.LoadPoints[1].MassFlow.ToString();
            loadPointData[1] = loadPointDataModel.LoadPoints[1].Rpm.ToString();
            loadPointData[2] = loadPointDataModel.LoadPoints[1].Pressure.ToString();
            loadPointData[3] = loadPointDataModel.LoadPoints[1].Temp.ToString();
            loadPointData[4] = loadPointDataModel.LoadPoints[1].InFlow.ToString();
            loadPointData[5] = loadPointDataModel.LoadPoints[1].BackPress.ToString();
            loadPointData[6] = loadPointDataModel.LoadPoints[1].BYP.ToString();
            loadPointData[7] = loadPointDataModel.LoadPoints[1].EIN.ToString();
            loadPointData[8] = loadPointDataModel.LoadPoints[1].WANZ.ToString();
            loadPointData[9] = loadPointDataModel.LoadPoints[1].RSMIN.ToString();
            // Create the load point data line
            newDataLine = new string(' ', 88); // Initialize a line with 88 spaces
            newDataLine = InsertFormattedValues(ref newDataLine, loadPointData);
            // Prepare the new content with the inserted line
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
            logger.LogInformation("First data line inserted successfully under the first LP line.");
        }
        catch (Exception ex)
        {
            logger.LogError("InsertDataLineUnderFirstLPFixed", ex.Message);
        }
    }

    public void InsertLoadPointsWithExactFormattingUsingMid(int mxLPs = 0)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        if(mxLPs != 0){
            maxLoadPoints = mxLPs;
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
                logger.LogInformation("Line starting with '!     PN' not found.");
                return;
            }
            // Prepare new content with new load points
            for (int i = 0; i < insertIndex; i++)
            {
                newContent += lines[i] + Environment.NewLine;
            }
        for(int lp = 2; lp <= maxLoadPoints; ++lp){
            if(loadPointDataModel.LoadPoints[lp].MassFlow == 0)continue;
            string[] loadPointData = new string[10];
            loadPointData[0] = loadPointDataModel.LoadPoints[lp].MassFlow.ToString();
            loadPointData[1] = loadPointDataModel.LoadPoints[lp].Rpm.ToString();
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
            logger.LogInformation("New load points inserted successfully.");
        }
        catch(Exception ex){
            logger.LogError("InsertLoadPointsWithExactFormattingUsingMid", ex.Message);
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
            //we changed if wrong check again

            //Console.WriteLine("Before"+ line+ "After");
            //Console.WriteLine("596 " + start + ",line:" + line + ",len:" + length + "for " + formattedValue);
            // we have changes from length -1 to length;
            line = line.Remove(start, formattedValue.Length).Insert(start, formattedValue);
            

        }
        return line;
    }
    string InsertValue(string line, string value, int start, int length, string format)
    {
        string formattedValue = string.Format(format, Convert.ToDouble(value));
        return line.Remove(start - 1, formattedValue.Length).Insert(start - 1, formattedValue);
    }

    public void DatFileInitParamsExceptLP1(double power)
    {
        try
        {
            if(power <= 0)
            {
                DatFileInitParamsExceptLP();
            }
            else
            {
                // Values from HBD (Power Cycle) calculations
                turbineDataModel.InletVelocity = thermodynamicService.GetInletVelocity(turbineDataModel.MassFlowRate);
                double inletVelocityFromHBD = turbineDataModel.InletVelocity;//36.900;
                turbineDataModel.VolumetricFlow = thermodynamicService.getVolumetricFlow();
                double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;

                double estimatedPowerFromHBD = power;
                if (estimatedPowerFromHBD < 1000)
                {
                    estimatedPowerFromHBD = 1001;
                }


                // Variables for calculations
                double nozzleCount, nozzleFront;
                double genPower = 1000, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
                double turbinePower, gearboxPower;
    
                Console.WriteLine("Calculating nozzles and powertrain specs to write into DAT file...");
    
                // int rowIndex = 0;
                PowerEfficiencyDataPoint reqPowDataPoint = new PowerEfficiencyDataPoint();
                foreach(PowerEfficiencyDataPoint powerEffPoint in powerEfficiencyModel.PowerEfficiencyPoints){
                    double pow_value = powerEffPoint.Power;
                    if (pow_value * 1000 <= estimatedPowerFromHBD)
                    {
                        reqPowDataPoint = powerEffPoint;
                        genPower = pow_value * 1000;
                    }
                    else
                    {
                        // genPower = pow_value * 1000;//estimatedPowerFromHBD;
                        break;
                    }
                }
                // genPower = estimatedPowerFromHBD;
                genEff4_4 = reqPowDataPoint.Eff100;
                genEff3_4 = reqPowDataPoint.Eff75;
                genEff1_2 = reqPowDataPoint.Eff50;
                genEff1_4 = reqPowDataPoint.Eff25;
    
                Console.WriteLine($"Generator Power kW / Eff : {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
                updateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);
    
                HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
                hBDPowerCalculator.HBDUpdateEffGenerator(genEff4_4);
    
                // Gearbox Power calculation
                gearboxPower = genPower / (genEff4_4 / 100);
                Console.WriteLine($"Gearbox Power kW : {gearboxPower}");
                updateGearboxSpecs(gearboxPower);
    
                // Turbine Power calculation
                turbinePower = gearboxPower / 0.86;
                Console.WriteLine($"Turbine Shaft Power kW: {gearboxPower}");
                updateTurbineSpecs(turbinePower);
    
                Console.WriteLine($"Inlet flow velocity from HBD: {inletVelocityFromHBD}");
                updateVari27(inletVelocityFromHBD);
    
                NozzleTurbaData reqNozzleTurbaData = new NozzleTurbaData();
                Console.WriteLine("Volumeeeeeeeeeeeeeeeee FFFFFFFFFFFFLOWWWWWWWWWWWWWWWWWWWWWWWWWWWW:"+ volumetricFlowFromHBD);
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
                updateNozzleSpecs(nozzleCount, nozzleFront);
                Logger($"Initial Nozzle in G1: {nozzleFront} Total: {nozzleCount}");
            
            }
        }
        catch(Exception ex){
            logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
        }
       
    }

    public void InsertDataLineUnderND(int maxLoadPoints = 0)
    {
        int lastLoadPointNumber = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        if(maxLoadPoints != 0){
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
    void updateVari27(double paramNewValue)
    {
        // Set the path to your .dat file
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
    }

    public void updateGeneratorSpecs(double paramGenPower, double paramGenEff4_4, double paramGenEff3_4, double paramGenEff1_2, double paramGenEff1_4)
    {
        // Set the path to your .dat file
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = "";
        string[] fileLines;

        try{
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
                    // If we catch an IOException, retry after a short delay
                    logger.LogError("updateGeneratorSpecs", ex.Message);
                }
            }

            // Split the file content into an array of lines
            fileLines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string parameter = "!     PN GENO";

            for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
            {
                string line = fileLines[lineNumber];
            
                
                if (line.Contains(parameter))
                {
                    lineNumber++; // Move to the next line
                    line = fileLines[lineNumber];

                    // Update generator power
                    string powerValue = paramGenPower.ToString("00000.000");
                    line = line.Substring(0, 6) + powerValue + line.Substring(15); // Update the line

                    // Update generator efficiency values
                    line = line.Substring(0, 26) + paramGenEff4_4.ToString("00000.000") + line.Substring(35);
                    line = line.Substring(0, 36) + paramGenEff3_4.ToString("00000.000") + line.Substring(45);
                    line = line.Substring(0, 46) + paramGenEff1_2.ToString("00000.000") + line.Substring(55);
                    line = line.Substring(0, 56) + paramGenEff1_4.ToString("00000.000") + line.Substring(65);
                    
                    fileLines[lineNumber] = line; // Save the updated line
                }
            }
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
            logger.LogInformation("Successfully updated generator specs.");
        }
        catch(Exception ex){
            logger.LogError("updateGeneratorSpecs", ex.Message);
        }
    }


     public void updateGearboxSpecs(double paramGearboxPower)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines = new string[0];
        string parameter = "!     PN GETR";
        bool fileReady = false;
        while (!fileReady)
        {
            try
            {
                fileLines = File.ReadAllLines(filePath);
                fileReady = true; 
            }
            catch (IOException ex)
            {
                logger.LogError("updateGearboxSpecs", ex.Message);
            }
        }
        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            if (fileLines[lineNumber].Contains(parameter))
            {
                lineNumber++;
                string line = fileLines[lineNumber];
                // Gearbox power
                line = line.Remove(6, 9).Insert(6, paramGearboxPower.ToString("00000.000"));
                fileLines[lineNumber] = line;
            }
        }
        File.WriteAllLines(filePath, fileLines);
        logger.LogInformation("Gearbox specs updated.");
    }

    public void updateTurbineSpecs(double paramTurbinePower)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines = new string[0];
        string parameter = "!     PN TURB";
        bool fileReady = false;
        try{
            while (!fileReady)
            {
                try
                {
                    fileLines = File.ReadAllLines(filePath);
                    fileReady = true; 
                }
                catch (IOException ex)
                {
                    logger.LogError("updateTurbineSpecs", ex.Message);
                }
            }
            for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
            {
                if (fileLines[lineNumber].Contains(parameter))
                {
                    lineNumber++;
                    string line = fileLines[lineNumber];
                    // Turbine power
                    line = line.Remove(6, 9).Insert(6, paramTurbinePower.ToString("00000.000"));
                    fileLines[lineNumber] = line;
                }
            }
            File.WriteAllLines(filePath, fileLines);
            logger.LogInformation("Turbine specs updated.");
        }
        catch(Exception ex){
            logger.LogError("updateTurbineSpecs", ex.Message);
        }
    }
    public double getCurrentNozzle(int val)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines = new string[0];//= File.ReadAllLines(filePath);
        string parameter = "!     ANZAHL    FLAECHE BZW DUESENZAHL";
        bool fileReady = false;
        try
        {
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
                    return -1;
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
                    if(val == 0)
                    {
                        if (double.TryParse(naString, out double oldNaValue))
                        {
                            return oldNaValue;
                        }
                        else
                        {
                            return -1;
                        }
                    }else if(val == 1)
                    {
                        if (double.TryParse(totalString, out double totalStringValue))
                        {
                            return totalStringValue - Convert.ToDouble(naString);
                        }
                        else
                        {
                            return -1; ;  // Default value
                        }
                    }
                    
                }
                
            }
            return -1;
        }
        catch (Exception ex)
        {
            logger.LogError("updateNozzleSpecs", ex.Message);
            return -1;
        }
    }
    public void updateNozzleSpecs(double paramNozzleCount, double paramNozzleFront)
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines = new string[0];//= File.ReadAllLines(filePath);
        string parameter = "!     ANZAHL    FLAECHE BZW DUESENZAHL";
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
                        }
                        else
                        {
                            turbineDataModel.OldNa = 0.0;  // Default value
                        }

                        if (double.TryParse(totalString, out double totalStringValue))
                        {
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

    string FormatAsLeadingSpaces(double number, int fixWidth)
    {
        string formattedString = number.ToString("0.000");
        formattedString = new string(' ', fixWidth - formattedString.Length) + formattedString;
        Console.WriteLine("New string: " + formattedString);
        return formattedString;
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
    }
}
