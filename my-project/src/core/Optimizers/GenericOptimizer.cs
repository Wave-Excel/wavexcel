using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2013.Excel;
//using GameController;
using Interfaces.ILogger;
using StartExecutionMain;
using StartKreislExecution;
using System.Text.RegularExpressions;


namespace Ignite_X.src.core.Optimizers;

public class GenericOptimizer
{
    ILogger logger;
    public GenericOptimizer()
    {
        logger = StartKreisl.GlobalHost.Services.GetRequiredService<ILogger>();
        logger.clear();
    }
    public double GetPenaltyScore()
    {
        return 50000;
    }

    private string EnsureMinimumLength(string line, int minLength)
    {
        return line.PadRight(minLength);
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

    private string Format(double value, string format)
    {
        return value.ToString(format);
    }

    public void testing()
    {
        UpdateDAT_DependentVariables(1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6);
    }

    public void GetDatFiles(string path, List<string> datFiles)
    {
        try
        {
            logger.LogInformation("Checking :" + path);
            // Get all .DAT files in the current directory
            datFiles.AddRange(Directory.GetFiles(path, "*.ERG"));

            // Get all subdirectories and recursively call this method
            foreach (var directory in Directory.GetDirectories(path))
            {
                GetDatFiles(directory, datFiles);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Log the exception or handle it as needed
            Debug.WriteLine($"Access denied to folder: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            // Handle the case where the directory is not found
            Debug.WriteLine($"Directory not found: {path}");
        }
        catch (Exception ex)
        {
            // Handle other potential exceptions
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    public void fillDataSet()
    {
        string prodisPath = "\\\\invadi7fla.ad101.siemens-energy.net\\ai@stg\\Database for Executed Turbine\\prodis";
        string csvFilePath = "C:\\testDir\\DataSetMLfromERG.csv";

        // Check if the file exists and delete it if it does
        if (File.Exists(csvFilePath))
        {
            File.Delete(csvFilePath);
        }


        // Check if the directory exists
        if (!Directory.Exists(prodisPath))
        {
            Debug.WriteLine("The specified directory does not exist.");
            return;
        }

        using (StreamWriter writer = new StreamWriter(csvFilePath))
        {
            // Write the header row
            writer.WriteLine("Input Pressure, Temperature, Mass Flow, Exhaust Pressure, Admission Factor Total,Wheel Chamber Pressure,Pressure after 1st GBC,Pressure after 2ND GBC,No. of Stages in Stage Group 1," +
                "No. of Stages in Stage Group 2,No. of Stages in Stage Group 3,Shaft Diameter 1,Shaft Diameter 2,Shaft Diameter 3,Balance Piston Diameter 1," +
                "Balance Piston Diameter 2,Angle 1,Angle 2,Angle 3, ProjectName");
        }
            // Traverse through each folder in the specified directory
            List<TurbineDataSet> turbineDataSets = new List<TurbineDataSet>();
        try
        {
            // Get all .DAT files in the directory and its subdirectories
            List<string> datFiles = new List<string>();
            //string[] datFiles = Directory.GetFiles(prodisPath, "*.DAT", SearchOption.AllDirectories);
            GetDatFiles(prodisPath, datFiles);
            string[] parameters0 = new string[]
            {
                "!ND   DM REGELR",
                "!     RADKAMMER- UND",
                "!     RADKAMMER- UND",
                "!     RADKAMMER- UND",
                "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "!               INNENDURCHMESSER DER UE-TEILE",
                "!               INNENDURCHMESSER DER UE-TEILE",
                "!               INNENDURCHMESSER DER UE-TEILE",
                "!               AUSGLEICHSKOLBENDURCHMESSER",
                "!               AUSGLEICHSKOLBENDURCHMESSER",
                "!     SCHAUFELWINKEL FUER UE-TEILE",
                "!     SCHAUFELWINKEL FUER UE-TEILE",
                "!     SCHAUFELWINKEL FUER UE-TEILE"
            };
            string[] parameters = new string[]
            {
                "ND   DM REGELR",
                "     RADKAMMER- UND",
                "     RADKAMMER- UND",
                "     RADKAMMER- UND",
                "               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "               DRUCKZIFFERN BZW. STUFENZAHLEN",
                "               INNENDURCHMESSER DER UE-TEILE",
                "               INNENDURCHMESSER DER UE-TEILE",
                "               INNENDURCHMESSER DER UE-TEILE",
                "               AUSGLEICHSKOLBENDURCHMESSER",
                "               AUSGLEICHSKOLBENDURCHMESSER",
                "     SCHAUFELWINKEL FUER UE-TEILE",
                "     SCHAUFELWINKEL FUER UE-TEILE",
                "     SCHAUFELWINKEL FUER UE-TEILE"
            };
            string inputParam = "DAMPFMENGE DREHZAHL P";
            int c = 0;
            foreach (string filePath in datFiles)
            {
                if (filePath.IndexOf("WELLE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                string pattern = @"prodis\\(.*?)\\E\d", projectName = "";
                Match match = Regex.Match(filePath, pattern);
                if (match.Success)
                {
                    projectName = match.Groups[1].Value;
                    Debug.WriteLine("project: " + projectName);
                    //Console.WriteLine(result);
                }
                
                ++c;
                Debug.WriteLine("FILE: " + c + "::" + filePath);
                string[] fileLines = File.ReadAllLines(filePath);
                string[] variable = new string[16];
                bool foundInput = false;
                for (int i = 0; i < variable.Length; i++)
                {
                    variable[i] = "";
                }
                int varCount = 0;
                string inputPressure = "";
                string inputTemperature = "";
                string inputMassFlow = "";
                string exhaustPressure = "";
                for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
                {
                    string line = fileLines[lineNumber];
                    if (!foundInput)
                    {
                        if (line.Contains(inputParam))
                        {
                            foundInput = true;
                            ++lineNumber;
                            line = fileLines[lineNumber];
                            var values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            inputPressure = values[2]; // 42.981
                            inputTemperature = values[3]; // 440.000
                            inputMassFlow = values[0]; // 8.93
                            exhaustPressure = values[5]; // 4.59
                        }
                    }
                    // Admission factor
                    if (line.Contains(parameters[0]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 25);
                        variable[1] = line.Substring(16, 9).Trim();
                        varCount++;
                    }

                    // Wheel Chamber Pressure, Pressure after 1st GBC, Pressure after 2nd GBC
                    if (line.Contains(parameters[1]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 35);
                        variable[2] = line.Substring(6, 9).Trim();  // Fill variable2
                        variable[3] = line.Substring(16, 9).Trim(); // Fill variable3
                        variable[4] = line.Substring(26, 9).Trim(); // Fill variable4
                        varCount += 3;
                    }

                    // Stages in Group 1, Group 2, Group 3
                    if (line.Contains(parameters[4]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 45);
                        variable[5] = line.Substring(16, 9).Trim(); // Fill variable5
                        variable[6] = line.Substring(26, 9).Trim(); // Fill variable6
                        variable[7] = line.Substring(36, 9).Trim(); // Fill variable7
                        varCount += 3;
                    }

                    // Shaft Diameter 1, 2, 3
                    if (line.Contains(parameters[7]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 45);
                        variable[8] = line.Substring(16, 9).Trim(); // Fill variable8
                        variable[9] = line.Substring(26, 9).Trim(); // Fill variable9
                        variable[10] = line.Substring(36, 9).Trim(); // Fill variable10
                        varCount += 3;
                    }

                    // Balance Piston Diameter 1, 2
                    if (line.Contains(parameters[10]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 55);
                        variable[11] = line.Substring(26, 9).Trim(); // Fill variable11
                        variable[12] = line.Substring(46, 9).Trim(); // Fill variable12
                        varCount += 2;
                    }

                    // Angle 1, 2, 3
                    if (line.Contains(parameters[12]))
                    {
                        lineNumber++;
                        line = fileLines[lineNumber];
                        line = EnsureMinimumLength(line, 45);
                        variable[13] = line.Substring(16, 9).Trim(); // Fill variable13
                        variable[14] = line.Substring(26, 9).Trim(); // Fill variable14
                        variable[15] = line.Substring(36, 9).Trim(); // Fill variable15
                        varCount += 3;
                    }
                    if (varCount >= 15)
                        break;
                }
                using (StreamWriter writer = new StreamWriter(csvFilePath, true))
                {
                    writer.WriteLine(string.Join(",",inputPressure, inputTemperature,inputMassFlow, exhaustPressure, variable[1], variable[2], variable[3], variable[4], variable[5], variable[6], variable[7], variable[8], variable[9], variable[10], variable[11], variable[12], variable[13], variable[14], variable[15], projectName));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    public void UpdateDAT_DependentVariables(double variable1, double variable2, double variable3, double variable4, double variable5, double variable6, double variable7, double variable8,
                                        double variable9, double variable10, double variable11, double variable12, double variable13, double variable14, double variable15)
    {
        string filepath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] fileLines;

        // Retry reading the file until it's ready
        while (!IsFileReadyForOpen(filepath))
        {
            System.Threading.Thread.Sleep(100); // Wait before retrying
        }

        // Read the entire file content into an array of lines
        fileLines = File.ReadAllLines(filepath);

        // Set the parameters
        string[] parameters = new string[]
        {
            "!ND   DM REGELR",
            "!     RADKAMMER- UND",
            "!     RADKAMMER- UND",
            "!     RADKAMMER- UND",
            "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
            "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
            "!               DRUCKZIFFERN BZW. STUFENZAHLEN",
            "!               INNENDURCHMESSER DER UE-TEILE",
            "!               INNENDURCHMESSER DER UE-TEILE",
            "!               INNENDURCHMESSER DER UE-TEILE",
            "!               AUSGLEICHSKOLBENDURCHMESSER",
            "!               AUSGLEICHSKOLBENDURCHMESSER",
            "!     SCHAUFELWINKEL FUER UE-TEILE",
            "!     SCHAUFELWINKEL FUER UE-TEILE",
            "!     SCHAUFELWINKEL FUER UE-TEILE"
        };

        // Variable Changes
        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];

            // Admission factor
            if (line.Contains(parameters[0]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(16, 9).Insert(16, Format(variable1, "00000.000"));
                fileLines[lineNumber] = line;
            }
            // Wheel Chamber Pressure, Pressure after 1st GBC, Pressure after 2nd GBC
            if (line.Contains(parameters[1]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(6, 9).Insert(6, Format(variable2, "00000.000"));
                line = line.Remove(16, 9).Insert(16, Format(variable3, "00000.000"));
                line = line.Remove(26, 9).Insert(26, Format(variable4, "00000.000"));
                fileLines[lineNumber] = line;
            }
            // Stages in Group 1, Group 2, Group 3
            if (line.Contains(parameters[4]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(16, 9).Insert(16, Format(variable5, "0000.000"));
                line = line.Remove(26, 9).Insert(26, Format(variable6, "0000.000"));
                line = line.Remove(36, 9).Insert(36, Format(variable7, "0000.000"));
                fileLines[lineNumber] = line;
            }
            // Shaft Diameter 1, 2, 3
            if (line.Contains(parameters[7]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(16, 9).Insert(16, Format(variable8, "00000.000"));
                line = line.Remove(26, 9).Insert(26, Format(variable9, "00000.000"));
                line = line.Remove(36, 9).Insert(36, Format(variable10, "00000.000"));
                fileLines[lineNumber] = line;
            }
            // Balance Piston Diameter 1, 2
            if (line.Contains(parameters[10]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(26, 9).Insert(26, Format(variable11, "00000.000"));
                line = line.Remove(46, 9).Insert(46, Format(variable12, "00000.000"));
                fileLines[lineNumber] = line;
            }
            // Angle 1, 2, 3
            if (line.Contains(parameters[12]))
            {
                lineNumber++;
                line = fileLines[lineNumber];
                line = line.Remove(16, 9).Insert(16, Format(variable13, "00000.000"));
                line = line.Remove(26, 9).Insert(26, Format(variable14, "00000.000"));
                line = line.Remove(36, 9).Insert(36, Format(variable15, "00000.000"));
                fileLines[lineNumber] = line;
            }
        }

        // Write the new content back to the file
        while (!IsFileReadyForOpen(filepath))
        {
            System.Threading.Thread.Sleep(100); // Wait before retrying
        }

        File.WriteAllLines(filepath, fileLines);

        Logger("Dat file updated with following parameters-");
        Logger($"Variable1  |  Variable2  |  Variable3  |  Variable4  |  Variable5  |  Variable6  |  Variable7  |  Variable8  |  Variable9  |  Variable10  |  Variable11  |  Variable12  |  Variable13  |  Variable14  |  Variable15");
        Logger($"{variable1} | {variable2} | {variable3} | {variable4} | {variable5} | {variable6} | {variable7} | {variable8} | {variable9} | {variable10} | {variable11} | {variable12} | {variable13} | {variable14} | {variable15}");
    }
    public void Logger(string message)
    {
        logger.LogInformation(message);
    }
}

//'--------------------- Variable Description ----------------------------------
//    '|  1. | BEAUFSCHL          |  Admission Factor Total
//    '|  2. | RADKAMMER          |  Wheel Chamber Pressure
//    '|  3. | ----------------   |  Pressure after 1st GBC
//    '|  4. | ----------------   |  Pressure after 2ND GBC
//    '|  5. | DRUCKZIFFERN       |  No. of Stages in Stage Group 1
//    '|  6. | DRUCKZIFFERN       |  No. of Stages in Stage Group 2
//    '|  7. | DRUCKZIFFERN       |  No. of Stages in Stage Group 3
//    '|  8. | INNENDURCHMESSER   |  Shaft Diameter 1
//    '|  9. | INNENDURCHMESSER   |  Shaft Diameter 2
//    '|  10.| INNENDURCHMESSER   |  Shaft Diameter 3
//    '|  11.| AUSGLEICHSKOLBEND  |  Balance Piston Diameter 1
//    '|  12.| AUSGLEICHSKOLBEND  |  Balance Piston Diameter 2
//    '|  13.| SCHAUFELWINKEL     |  Angle 1
//    '|  14.| SCHAUFELWINKEL     |  Angle 2
//    '|  15.| SCHAUFELWINKEL     |  Angle 3

public class TurbineDataSet
{
    public string AdmissionFactorTotal { get; set; } // 1. BEAUFSCHL
    public string WheelChamberPressure { get; set; } // 2. RADKAMMER
    public string PressureAfter1stGBC { get; set; } // 3. Pressure after 1st GBC
    public string PressureAfter2ndGBC { get; set; } // 4. Pressure after 2ND GBC
    public string NoOfStagesInStageGroup1 { get; set; } // 5. DRUCKZIFFERN
    public string NoOfStagesInStageGroup2 { get; set; } // 6. DRUCKZIFFERN
    public string NoOfStagesInStageGroup3 { get; set; } // 7. DRUCKZIFFERN
    public string ShaftDiameter1 { get; set; } // 8. INNENDURCHMESSER
    public string ShaftDiameter2 { get; set; } // 9. INNENDURCHMESSER
    public string ShaftDiameter3 { get; set; } // 10. INNENDURCHMESSER
    public string BalancePistonDiameter1 { get; set; } // 11. AUSGLEICHSKOLBEND
    public string BalancePistonDiameter2 { get; set; } // 12. AUSGLEICHSKOLBEND
    public string Angle1 { get; set; } // 13. SCHAUFELWINKEL
    public string Angle2 { get; set; } // 14. SCHAUFELWINKEL
    public string Angle3 { get; set; } // 15. SCHAUFELWINKEL
    public TurbineDataSet()
    {

    }

}