using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces.IERGHandlerService;
using Microsoft.Maui.Controls;
using Models.TurbineData;

namespace Ignite_X.src.core.Services
{
    public class KreislERGHandlerService : IERGHandlerService
    {
        public KreislERGHandlerService() { }

        public double ExtractMassFlowFromERG(string filePath)
        {
            string search = "LTG.  MENGE";
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];
                if (line.Trim().Contains(search))
                {
                   
                  
                        for (int j = i + 1; j < fileLines.Length; ++j)
                        {
                            string[] newLine = fileLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (newLine.Length > 0 && newLine[0] == "4")
                            {
                                return (Convert.ToDouble(newLine[1])) / 3.6000;
                            }
                        }
                    }
                    
                
            }
            return 0;
        }
        public double ExtractMassFlowFromERGLP9(string filePath)
        {
            string search = "LTG.  MENGE";
            int count = 0;
            string searchNo = "4";
            if(TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    searchNo = "7";
                }
                else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    searchNo = "6";
                }

            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                searchNo = "1";
            }
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];
                if (line.Trim().Contains(search))
                {
                    count++;
                    if (count == 2)
                    {
                        
                        for (int j = i + 1; j < fileLines.Length; ++j)
                        {
                            string[] newLine = fileLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (newLine.Length > 0 && newLine[0] == searchNo)
                            {
                                //if (TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
                                //{
                                //    return (Convert.ToDouble(newLine[1]));
                                //}
                                //else
                                //{
                                    return (Convert.ToDouble(newLine[1])) / 3.6000;
                                //}
                            }
                        }
                    }

                    
                }


            }
            return 0;
        }

        public double ExtractEnthalphyForDesuparator(string filePath,int count,int lp)
        {
            int lpno = 0;
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    lpno++;
                    if (lpno == lp)
                    {
                        string[] newLine = fileLines[i + count].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[2]);
                    }
                }
            }
            return 0;
        }
        public double ExtractMassFlowForDesuparator(string filePath, int count,int lp)
        {
            int lpno = 0;
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    lpno++;
                    if (lpno == lp)
                    {
                        string[] newLine = fileLines[i + count].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[1]);
                    }
                }
            }
            return 0;
        }
        public double ExtractTempForDesuparator(string filePath, int count,int lp)
        {
            int lpno = 0;
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    lpno++;
                    if (lpno == lp)
                    {
                        string[] newLine = fileLines[i + count].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[4]);
                    }
                }
            }
            return 0;
        }
        public double ExtractPressureForClosedCycle(string filePath, int elementNo, int loadPoint)
        {
            int currentLpCount = 0;
            string header = "LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL";

            string[] fileLines = File.ReadAllLines(filePath);

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains(header))
                {
                    currentLpCount++;
                    if (currentLpCount == loadPoint)
                    {
                        // Loop through lines up to maxCount after startOffset
                        for (int offset = i+1; offset < fileLines.Length; offset++)
                        {
                            
                                string[] parts = fileLines[offset]
                                    .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 3 &&
                                    double.TryParse(parts[3], out double pressure) && parts[0] == elementNo.ToString())
                                {
                                    return pressure;
                                }
                            
                        }
                        return 0; // No valid line found within count range
                    }
                }
            }
            return 0; // Load point not found
        }
        public double ExtractTempForClosedCycle(string filePath, int elementNo, int loadPoint)
        {
            int currentLpCount = 0;
            string header = "LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL";

            string[] fileLines = File.ReadAllLines(filePath);

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains(header))
                {
                    currentLpCount++;
                    if (currentLpCount == loadPoint)
                    {
                        // Loop through lines up to maxCount after startOffset
                        for (int offset = i + 1; offset < fileLines.Length; offset++)
                        {

                            string[] parts = fileLines[offset]
                                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length > 3 &&
                                double.TryParse(parts[4], out double Temp) && parts[0] == elementNo.ToString())
                            {
                                return Temp;
                            }

                        }
                        return 0; // No valid line found within count range
                    }
                }
            }
            return 0; // Load point not found
        }
        public double ExtractEnthalphyForClosedCycle(string filePath, int elementNo, int loadPoint)
        {
            int currentLpCount = 0;
            string header = "LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL";

            string[] fileLines = File.ReadAllLines(filePath);

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains(header))
                {
                    currentLpCount++;
                    if (currentLpCount == loadPoint)
                    {
                        // Loop through lines up to maxCount after startOffset
                        for (int offset = i + 1; offset < fileLines.Length; offset++)
                        {

                            string[] parts = fileLines[offset]
                                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length > 1 &&
                                double.TryParse(parts[2], out double Enthalphy) && parts[0] == elementNo.ToString())
                            {
                                return Enthalphy;
                            }

                        }
                        return 0; // No valid line found within count range
                    }
                }
            }
            return 0; // Load point not found
        }
        public double ExtractMassFlowForClosedCycle(string filePath, int elementNo, int loadPoint)
        {
            int currentLpCount = 0;
            string header = "LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL";

            string[] fileLines = File.ReadAllLines(filePath);

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains(header))
                {
                    currentLpCount++;
                    if (currentLpCount == loadPoint)
                    {
                        // Loop through lines up to maxCount after startOffset
                        for (int offset = i + 1; offset < fileLines.Length; offset++)
                        {

                            string[] parts = fileLines[offset]
                                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length > 1 &&
                                double.TryParse(parts[1], out double MassFlow) && parts[0]== elementNo.ToString())
                            {
                                return MassFlow;
                            }

                        }
                        return 0; // No valid line found within count range
                    }
                }
            }
            return 0; // Load point not found
        }
        public double ExtractPressForDesuparator(string filePath, int count, int lp)
        {
            int lpno = 0;
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    lpno++;
                    if (lpno == lp)
                    {
                        string[] newLine = fileLines[i + count].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[3]);
                    }
                }
            }
            return 0;
        }
        public double ExtractPowerFromERG(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file does not exist.", filePath);
            }
            double power = 0.0;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                bool foundGenerator = false;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("GENERATOR"))
                    {
                        foundGenerator = true;
                        continue;
                    }
                    if (foundGenerator)
                    {
                        foundGenerator = false;

                        int startIndex = line.IndexOf("Klemmenlstg.:") + "Klemmenlstg.:".Length;
                        int endIndex = line.IndexOf(".kW", startIndex);

                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            string powerString = line.Substring(startIndex, endIndex - startIndex).Trim();

                            if (double.TryParse(powerString, out power))
                            {
                                return power; // Return the extracted power value
                            }
                            else
                            {
                                throw new FormatException("The extracted power value is not in a valid format.");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("The expected format was not found in the line after GENERATOR.");
                        }
                    }
                }
            }
            throw new InvalidOperationException("The power section was not found in Kreisl erg file. Please verify the DAT files are in the correct format and all inputs are filled in the DAT files correctly.");

        }
        public double ExtractVolFlowFromERG(string filePath)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("VMAX-->LP"))
                {
                    string[] newLine = fileLines[i + 5].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return Convert.ToDouble(newLine[1]);
                }
            }
            return 0;
        }
        public double ExtractVolFlowFromERGForDesuparator(string filePath)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    string[] newLine = fileLines[i + 3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return Convert.ToDouble(newLine[6]);
                }
            }
            return 0;
        }
        public double ExtractVolFlowFromERGForPRV(string filePath)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Contains("LTG.  MENGE   ENTHALPIE  DRUCK   TEMP.    DP       VOL"))
                {
                    if (TurbineDataModel.getInstance().DumpCondensor)
                    {
                        string[] newLine = fileLines[i + 9].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[6]);
                    }
                    else if (!TurbineDataModel.getInstance().DumpCondensor)
                    {
                        string[] newLine = fileLines[i + 8].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return Convert.ToDouble(newLine[6]);
                    }
                    
                }
            }
            return 0;
        }
        public double ExtractVolFlowForLoadPoint(string filePath, int loadPoint, int lineNumber)
        {
            if (TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 7;
                }
                else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 6;
                }
            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                lineNumber = 1;
            }
            string[] fileLines = File.ReadAllLines(filePath);
            string searchString = "";
            bool foundLoadPoint = false;

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (loadPoint > 9)
                {
                    searchString = $"Lastpunkt {loadPoint}";
                }
                else
                {
                    searchString = $"Lastpunkt  {loadPoint}";
                }
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("LTG.  MENGE   ENTHALPIE"))
                {
                    i += 2; // Skip headers
                    while (i < fileLines.Length)
                    {
                        string[] parts = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4 && int.TryParse(parts[0], out int lineNum) && lineNum == lineNumber)
                        {
                            return Convert.ToDouble(parts[6]); // Pressure is at index 3
                        }
                        i++;
                        if (fileLines[i].Contains("ELEMENT-NR.")) break;
                    }
                    break;
                }
            }
            //return 0;
            return 0;
        }
        public double ExtractGeneratorPower(string filePath, int loadPoint)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            bool foundLoadPoint = false;
            string searchString = "";

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i].Trim();

                
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("GENERATOR/VERDICHTER"))
                {
                    // Check the next few lines for power information
                    for (int j = i + 1; j < Math.Min(fileLines.Length, i + 10); j++)
                    {
                        string powerLine = fileLines[j].Trim();

                        if (powerLine.Contains("Klemmenlstg.:") && powerLine.Contains("kW"))
                        {
                            // Use regex to extract the number before "kW"
                            var match = System.Text.RegularExpressions.Regex.Match(powerLine, @"(\d+(?:\.\d+)?)\.?kW");
                            if (match.Success)
                            {
                                if (double.TryParse(match.Groups[1].Value, out double power))
                                {
                                    return power;
                                }
                            }
                        }

                        // Stop if we hit another element or section
                        if (powerLine.Contains("ELEMENT-NR.") && !powerLine.Contains("6"))
                            break;
                    }
                    break;
                }
            }
            return 0;
        }

        public double ExtractPressure(string filePath, int loadPoint, int lineNumber)
        {
            if(TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 7;
                }else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 6;
                }
            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                lineNumber = 1;
            }
            string[] fileLines = File.ReadAllLines(filePath);
            bool foundLoadPoint = false;
            string searchString = "";

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (loadPoint > 9)
                {
                    searchString = $"Lastpunkt {loadPoint}";
                }
                else
                {
                    searchString = $"Lastpunkt  {loadPoint}";
                }
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("LTG.  MENGE   ENTHALPIE"))
                {
                    i += 2; // Skip headers
                    while (i < fileLines.Length)
                    {
                        string[] parts = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4 && int.TryParse(parts[0], out int lineNum) && lineNum == lineNumber)
                        {
                            return Convert.ToDouble(parts[3]); // Pressure is at index 3
                        }
                        i++;
                        if (fileLines[i].Contains("ELEMENT-NR.")) break;
                    }
                    break;
                }
            }
            return 0;
        }

        public double ExtractTemperature(string filePath, int loadPoint, int lineNumber)
        {
            if (TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 7;
                }
                else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 6;
                }
            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                lineNumber = 1;
            }
            // Similar logic but return parts[4] for temperature
            string[] fileLines = File.ReadAllLines(filePath);
            string searchString = "";
            bool foundLoadPoint = false;

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (loadPoint > 9)
                {
                    searchString = $"Lastpunkt {loadPoint}";
                }
                else
                {
                    searchString = $"Lastpunkt  {loadPoint}";
                }
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("LTG.  MENGE   ENTHALPIE"))
                {
                    i += 2;
                    while (i < fileLines.Length)
                    {
                        string[] parts = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[0], out int lineNum) && lineNum == lineNumber)
                        {
                            return Convert.ToDouble(parts[4]); // Temperature is at index 4
                        }
                        i++;
                        if (fileLines[i].Contains("ELEMENT-NR.")) break;
                    }
                    break;
                }
            }
            return 0;
        }
        public double ExtractBackPressure(string filePath, int loadPoint, int lineNumber)
        {
            if (TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 1;
                }
                else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 1;
                }
            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                lineNumber = 3;
            }
            string[] fileLines = File.ReadAllLines(filePath);
            string searchString = "";
            bool foundLoadPoint = false;

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];

                if (loadPoint > 9)
                {
                    searchString = $"Lastpunkt {loadPoint}";
                }
                else
                {
                    searchString = $"Lastpunkt  {loadPoint}";
                }
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("LTG.  MENGE   ENTHALPIE"))
                {
                    i += 2; // Skip headers
                    while (i < fileLines.Length && !fileLines[i].Contains("ELEMENT-NR."))
                    {
                        string[] parts = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4 && int.TryParse(parts[0], out int lineNum) && lineNum == lineNumber)
                        {
                            return Convert.ToDouble(parts[3]); // Back pressure is at index 3
                        }
                        i++;
                    }
                    break;
                }
            }
            return 0;
        }

        public double ExtractMassFlow(string filePath, int loadPoint, int lineNumber)
        {
            // Similar logic but return parts[1] for mass flow
            string[] fileLines = File.ReadAllLines(filePath);
            bool foundLoadPoint = false;
            string searchString = "";
            if (TurbineDataModel.getInstance().DeaeratorOutletTemp > 0)
            {
                if (TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 7;
                }
                else if (!TurbineDataModel.getInstance().DumpCondensor)
                {
                    lineNumber = 6;
                }
            }
            else if (TurbineDataModel.getInstance().PST > 0)
            {
                lineNumber = 1;
            }

            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];

                if(loadPoint > 9)
                {
                    searchString = $"Lastpunkt {loadPoint}";
                }
                else
                {
                    searchString = $"Lastpunkt  {loadPoint}";
                }
                if (line.Contains(searchString))
                    foundLoadPoint = true;

                if (foundLoadPoint && line.Contains("LTG.  MENGE   ENTHALPIE"))
                {
                    i += 2;
                    while (i < fileLines.Length)
                    {
                        string[] parts = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && int.TryParse(parts[0], out int lineNum) && lineNum == lineNumber)
                        {
                            return Convert.ToDouble(parts[1])/3.6; // Mass flow is at index 1
                        }
                        i++;
                        if (fileLines[i].Contains("ELEMENT-NR.")) break;
                    }
                    break;
                }
            }
            return 0;
        }




        public double ExtractExhaustPressureFromERG(string filePath)
        {
            string search = "LTG.  PMAX    T";
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];
                if (line.Trim().Contains(search))
                {
                    for (int j = i + 1; j < fileLines.Length; ++j)
                    {
                        string[] newLine = fileLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (newLine[0] == "5")
                        {
                            return Convert.ToDouble(newLine[1]);
                        }
                    }
                }
            }
            return 0;
        }
        public double ExtractInletTemperatureFromERG(string filePath) 
        {
            string search = "LTG.  PMAX    T";
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];
                if (line.Trim().Contains(search))
                {
                    for(int j = i + 1; j< fileLines.Length; ++j)
                    {
                        string[] newLine = fileLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if(newLine[0] == "4")
                        {
                            return Convert.ToDouble(newLine[2]);
                        }
                    }
                }
            }

            return 0;
        }

        public double ExtractInletPressureFromERG(string filePath)
        {
            string search = "LTG.  PMAX    T";
            string[] fileLines = File.ReadAllLines(filePath);
            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];
                if (line.Trim().Contains(search))
                {
                    for (int j = i + 1; j < fileLines.Length; ++j)
                    {
                        string[] newLine = fileLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (newLine[0] == "4")
                        {
                            return Convert.ToDouble(newLine[1]);
                        }
                    }
                }
            }

            return 0;
        }
    }
}
