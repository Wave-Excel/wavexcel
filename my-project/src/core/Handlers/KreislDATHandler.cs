using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignite_X.src.core.Models;
using Interfaces.IThermodynamicLibrary;
using Models.PowerEfficiencyData;
using Models.TurbineData;
using StartExecutionMain;
using StartKreislExecution;
using HMBD.HMBDInformation;
using Interfaces.ILogger;
using ExtraLoadPoints;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;
using Models.LoadPointDataModel;
using Ignite_X.src.core.Services;
using Models.AdditionalLoadPointModel;
using System.Diagnostics;
using Handlers.DAT_Handler;

namespace Ignite_X.src.core.Handlers
{
    public class KreislDATHandler
    {
        TurbineDataModel turbineDataModel;
        PowerEfficiencyModel powerEfficiencyModel;
        KGraphDataModel kGraphDataModel;
        IThermodynamicLibrary thermodynamicService;
        KreislERGHandlerService kreislERGHandlerService;
        ILogger logger;


        public KreislDATHandler() {
            turbineDataModel = TurbineDataModel.getInstance(); 
            kGraphDataModel = KGraphDataModel.getInstance();
            powerEfficiencyModel = PowerEfficiencyModel.getInstance();
            thermodynamicService = StartKreisl.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
            logger = StartKreisl.GlobalHost.Services.GetRequiredService<ILogger>();
            kreislERGHandlerService = new KreislERGHandlerService();
        }

        public void MakeUpTemperature(string filePath, int serialNumber, string temperature, int offset = 0)
        {
            try
            {
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 22;
                    }
                    else
                    {
                        serialNumber = 9;
                    }
                }

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber+" ")))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string sign = "-";
                            double tmp = -1;
                            if (Double.TryParse(temperature.Trim(), out tmp))
                            {
                                if (tmp == 0)
                                    sign = "";
                            }


                            newLine[3] = sign + ConvertToKreislInputFormat(temperature.ToString());

                            string joinedLine = "";
                            if(turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                if (turbineDataModel.DumpCondensor)
                                {
                                    joinedLine = "  " + newLine[0] + " " + newLine[1];
                                }
                                else
                                {
                                    joinedLine = "   " + newLine[0] + " " + newLine[1];
                                }
                            }
                            
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }

                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                // Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }

        public void MakePRVToWPRVMultiple(string filePath)
        {
            try
            {

                // Read the entire file content into an array of lines
                
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("13 0")))
                    {
                        
                            newContent += ("  13 0     0.000     0.000     0.000     0.000     0.500     0.000     0.000" + Environment.NewLine);
                            newContent += ("  13 0     3.500     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                            i= i + 1;
                            continue;
                        
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public bool checkIfDumpcondensorON(int i, List<CustomerLoadPoint> initList)
        {
            int count = 0;
            if (initList[i].SteamPressure > 0)
            {
                count++;
            }
            if (initList[i].SteamTemp > 0)
            {
                count++;

            }
            if (initList[i].SteamMass > 0)
            {
                count++;
            }
            if (initList[i].ExhaustPressure > 0)
            {
                count++;
            }
            if (initList[i].ExhaustMassFlow > 0)
            {
                count++;
            }
            if (initList[i].PowerGeneration > 0)
            {
                count++;
            }
            if (count == 4)
            {
                return false;
            }
            else if (count == 5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void MakePRVToWPRVMultipleinDumpCondensor(string filePath)
        {
            try
            {

                // Read the entire file content into an array of lines

                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("12 0")))
                    {

                        newContent += ("  12 0     0.000     0.000     0.000     0.000     0.500     0.000     0.000" + Environment.NewLine);
                        newContent += ("  12 0     2.690     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                        i = i + 1;
                        continue;

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public bool InPrvMultipleBackPressure(double backPressure)
        {
            if(thermodynamicService.tsatvonp(backPressure * 0.92 - 0.25) - turbineDataModel.DeaeratorOutletTemp > 0)
            {
                return true;
            }else
            {
                return false;
            }
        }
        public void fillPsatvont_t(string filePath, int serialNumber, string temperature, int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 12;
                    }
                    else if(!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 13;
                    }
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            // Console.WriteLine(newLine[2]);
                            double pressure = thermodynamicService.psatvont(turbineDataModel.DeaeratorOutletTemp) + 0.25;
                            newLine[2] = ConvertToKreislInputFormat(pressure.ToString());
                            string joinedLine = "  " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    File.WriteAllText(filePath, newContent);
                    Console.WriteLine(newContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillInletPressure", ex.Message);
            }
        }
        public void FillCondensateReturn(string filePath, string serialNumber, string eff)
        {
            if(turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    serialNumber = "20";
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    serialNumber = "14";
                }
            }
            else if (turbineDataModel.PST > 0)
            {
                serialNumber = "1";
            }
            else
            {
                serialNumber = "4";
            }
            if (Double.TryParse(eff, out double efficiency_))
            {
                if (efficiency_ > 1) efficiency_ /= 100.000;
                eff = efficiency_.ToString();
            }
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == serialNumber)
                    {
                        count++;
                        if (count == 2)
                        {
                            subpart[5] = ConvertToKreislInputFormat(eff.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                joinedLine = "  " + subpart[0] + " " + subpart[1];

                            }
                            else
                            {
                                joinedLine = "   " + subpart[0] + " " + subpart[1];

                            }
                            for (int st = 2; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }

                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }
        public void Processcondensatetemperature(string filePath, int serialNumber, string temperature, int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {

                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 21;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 12;
                    }
                }
                
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string sign = "-";
                            double tmp = -1;
                            if (Double.TryParse(temperature.Trim(), out tmp))
                            {
                                if (tmp == 0)
                                    sign = "";
                            }


                            newLine[3] = sign + ConvertToKreislInputFormat(temperature.ToString());
                            string joinedLine = "  " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                // Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }
        public void RefreshKreislDAT()
        {
            string ergFilePath = "C:\\testDir\\KREISL.ERG";



            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor == true)
                {
                    if (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure * 0.92 - 0.25) - turbineDataModel.DeaeratorOutletTemp > 0)
                    {
                        if (File.Exists(ergFilePath))
                        {
                            double exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, 1);
                            if (exhaustTemp < turbineDataModel.PST)
                            {
                                string krieslwde = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWDDump.DAT");
                                File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);

                            }
                            else
                            {
                                string filePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDump.DAT");
                                File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
                            }
                        }
                        else
                        {
                            string krieslDWD = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWDDump.DAT");
                            File.Copy(krieslDWD, "C:\\testDir\\KREISL.DAT", true);
                        }
                        turbineDataModel.IsPRVTemplate = true;
                    }
                    else if (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure * 0.92 - 0.25) - turbineDataModel.DeaeratorOutletTemp < 0)
                    {
                        

                        if (File.Exists(ergFilePath))
                        {
                            double exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, 1);
                            if (exhaustTemp < turbineDataModel.PST)
                            {
                                string krieslwde = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWDDump.DAT");
                                File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);

                            }
                            else
                            {
                                string filePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDump.DAT");
                                File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
                            }
                        }
                        else
                        {
                            string krieslDWD = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWDDump.DAT");
                            File.Copy(krieslDWD, "C:\\testDir\\KREISL.DAT", true);
                        }
                        turbineDataModel.IsPRVTemplate = false;
                        UpdateTemplatePRVToWPRVInDumpCondensor(StartKreisl.filePath);


                    }
                }
                else if (turbineDataModel.DumpCondensor == false)
                {
                    Debug.WriteLine(thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure * 0.92 - 0.25)+"--1213213213123");
                    if (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure * 0.92 - 0.25) - turbineDataModel.DeaeratorOutletTemp > 0)
                    {
                        if (File.Exists(ergFilePath))
                        {
                            double exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, 1);
                            if (exhaustTemp < turbineDataModel.PST)
                            {
                                string krieslwde = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWD.DAT");
                                File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);

                            }
                            else
                            {
                                string filePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVD.DAT");
                                File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
                            }
                        }
                        else
                        {
                            string krieslDWD = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWD.DAT");
                            File.Copy(krieslDWD, "C:\\testDir\\KREISL.DAT", true);
                        }
                        turbineDataModel.IsPRVTemplate = true;
                    }
                    else if (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure * 0.92 - 0.25) - turbineDataModel.DeaeratorOutletTemp < 0)
                    {
                        if (File.Exists(ergFilePath))
                        {
                            double exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, 1);
                            if (exhaustTemp < turbineDataModel.PST)
                            {
                                string krieslwde = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWD.DAT");
                                File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);

                            }
                            else
                            {
                                string filePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVD.DAT");
                                File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
                            }
                        }
                        else
                        {
                            string krieslDWD = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVWD.DAT");
                            File.Copy(krieslDWD, "C:\\testDir\\KREISL.DAT", true);
                        }
                        turbineDataModel.IsPRVTemplate = false;
                        UpdateTemplatePRVToWPRV(StartKreisl.filePath);


                    }
                }
            }
            else if(turbineDataModel.PST > 0)
            {
                if (File.Exists(ergFilePath))
                {
                    double exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath,5,1);
                    if(exhaustTemp < turbineDataModel.PST)
                    {
                        string krieslwde = Path.Combine(AppContext.BaseDirectory, "kreislwdesuprator.dat");
                        File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);

                    }
                    else
                    {
                        string filePath = Path.Combine(AppContext.BaseDirectory, "kreisldesuprator.dat");
                        File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
                    }
                }
                else
                {
                    string krieslwde = Path.Combine(AppContext.BaseDirectory, "kreislwdesuprator.dat");
                    File.Copy(krieslwde, "C:\\testDir\\KREISL.DAT", true);
                }
                
            }
            else
            {
                String filePath = Path.Combine(AppContext.BaseDirectory, "kreislp1.dat");
                File.Copy(filePath, "C:\\testDir\\KREISL.DAT", true);
            }
            
        }
        public double checkDesupratorForClosedCycle(string filePath, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("15  13   5   7   0")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return Convert.ToDouble(newLine[5]);

                        }
                    }

                }
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
            return 0;

        }
        public double checkDesupratorForClosedCycleDumpCondensor(string filePath, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("8  15   0")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return Convert.ToDouble(newLine[3]);

                        }
                    }

                }
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
            return 0;

        }
        public double checkDumpCondensorForClosedCycle(string filePath, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("9  15   0")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return Convert.ToDouble(newLine[3]);

                        }
                    }

                }
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
            return 0;

        }
        public double checkDesuprator(string filePath,int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("3   4   3   2")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return Convert.ToDouble(newLine[4]);
                            
                        }
                    }
                    
                }
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
            return 0;

        }
        public void TurnOffCondensor(string filePath, int position, int start, int secondval, int lpCount, int fillValue)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                bool isChange = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (FirstLine.Length > 1 && FirstLine[0].Trim() == (Convert.ToString(start)) && !isChange && FirstLine[1].Trim() == (Convert.ToString(secondval)))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[position] = fillValue.ToString();
                            string joinedLine = "";
                            //string joinedLine = GetSpaces(newLine[0]) + newLine[0] + GetSpaces(newLine[1]) + newLine[1] + "   " + newLine[2] + "   " + newLine[3] + "   " + newLine[4];

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            isChange = true;
                            continue;
                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }


                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void FillWheelChamberEff(string filePath, string serialNumber, string eff)
        {
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    serialNumber = "6";
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    serialNumber = "7";
                }
            }
            else if (turbineDataModel.PST > 0)
            {
                serialNumber = "2";
            }
            else
            {
                serialNumber = "2";
            }
            if (Double.TryParse(eff, out double efficiency_))
            {
                if (efficiency_ > 1) efficiency_ /= 100.000;
                eff = efficiency_.ToString();
            }
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == serialNumber)
                    {
                        count++;
                        if (count == 2)
                        {
                            subpart[3] = ConvertToKreislInputFormat(eff.ToString());
                            string joinedLine = "   " + subpart[0] + " " + subpart[1];
                            for (int st = 2; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }

                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }
        public void FillTurbineEff(string filePath, string serialNumber, string eff)
        {
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                serialNumber = "7";
            }else if (turbineDataModel.PST > 0)
            {
                serialNumber = "1";
            }
            else
            {
                serialNumber = "4";
            }
            if (Double.TryParse(eff, out double efficiency_))
            {
                if (efficiency_ > 1) efficiency_ /= 100.000;
                eff = efficiency_.ToString();
            }
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == serialNumber)
                    {
                        count++;
                        if (count == 2)
                        {
                            subpart[3] = ConvertToKreislInputFormat(eff.ToString());
                            string joinedLine = "   " + subpart[0] + " " + subpart[1];
                            for (int st = 2; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }

                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }

        public void UpdateDesupratorFirst(string filePath , int lpCount)
        {
            try
            {
                
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("3   4   3   2")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[4] = "8";
                            string joinedLine = "   " + newLine[0] + "   " + newLine[1] + "   " + newLine[2]+ "   " + newLine[3]+"   " + newLine[4];
                            
                            for (int st = 5; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public string GetSpaces(string s)
        {
            if (s.Length == 2)
                return "  "; // 3 spaces
            if (s.Length < 2)
                return "   "; // 2 spaces
            return " "; // default 1 space
        }
        public void UpdateTemplatePRVToWPRV(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("13 0")))
                    {
                        count++;
                        if (count == 1)
                        {

                            newContent += ("  13 1     0.000     0.000     0.000     0.000    -0.080     0.000     0.000" + Environment.NewLine);
                            continue;

                        }
                        else if (count == 2)
                        {
                            newContent += ("  13 1     0.000     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                            continue;
                        }
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
                SecondLineClosedCycle(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateTemplatePRVToWPRVInDumpCondensor(string filePath) {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("12 0")))
                    {
                        count++;
                        if (count == 1)
                        {
                           
                            newContent += ("  12 1     0.000     0.000     0.000     0.000    -0.080     0.000     0.000" + Environment.NewLine);
                            continue;
                            
                        }else if(count == 2)
                        {
                            newContent += ("  12 1     0.000     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                            continue;
                        }
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
                SecondLineClosedCycleInDumpCondensor(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }

        public void UpdateTemplatePRVToWPRVLP9(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("13 0")))
                    {
                        count++;
                        if (count == 1)
                        {

                            newContent += ("  13 1     0.000     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                            continue;

                        }
                        else if (count == 2)
                        {
                            newContent += ("  13 1     0.000     0.000     0.000     0.000     0.000     0.000     0.000" + Environment.NewLine);
                            continue;
                        }
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
                SecondLineClosedCycle(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void SecondLineClosedCycle(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("13  19")))
                    {
                        count++;
                        if (count == 1)
                        {

                            newContent += ("  13  35   2  15   0   0   0   0   0   0   0   0   0   0   0   0   0   0" + Environment.NewLine);
                            continue;

                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }
                        
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void SecondLineClosedCycleInDumpCondensor(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("12  19")))
                    {
                        count++;
                        if (count == 1)
                        {

                            newContent += ("  12  35   2  15   0   0   0   0   0   0   0   0   0   0   0   0   0   0" + Environment.NewLine);
                            continue;

                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateDesupratorOffClosedPRVDumpCondensor(string filePath, int position, int start, int secondval, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                bool isChange = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (FirstLine.Length > 1 && FirstLine[0].Trim() == (Convert.ToString(start)) && !isChange && FirstLine[1].Trim() == (Convert.ToString(secondval)))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[position] = "-17";
                            string joinedLine = "";
                            //string joinedLine = GetSpaces(newLine[0]) + newLine[0] + GetSpaces(newLine[1]) + newLine[1] + "   " + newLine[2] + "   " + newLine[3] + "   " + newLine[4];

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            isChange = true;
                            continue;
                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }


                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateDesupratorClosedPRVDumpCondensor(string filePath, int position, int start, int secondval, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                bool isChange = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (FirstLine.Length > 1 && FirstLine[0].Trim() == (Convert.ToString(start)) && !isChange && FirstLine[1].Trim() == (Convert.ToString(secondval)))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[position] = "17";
                            string joinedLine = "";
                            //string joinedLine = GetSpaces(newLine[0]) + newLine[0] + GetSpaces(newLine[1]) + newLine[1] + "   " + newLine[2] + "   " + newLine[3] + "   " + newLine[4];

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            isChange = true;
                            continue;
                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }


                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateDesupratorClosedPRV(string filePath, int position, int start , int secondval, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                bool isChange = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (FirstLine.Length > 1 && FirstLine[0].Trim() == (Convert.ToString(start)) && !isChange && FirstLine[1].Trim() == (Convert.ToString(secondval)))
                    {
                        count++;
                        if(count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[position] = "16";
                            string joinedLine = "";
                            //string joinedLine = GetSpaces(newLine[0]) + newLine[0] + GetSpaces(newLine[1]) + newLine[1] + "   " + newLine[2] + "   " + newLine[3] + "   " + newLine[4];

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            isChange = true;
                            continue;
                        }
                        else
                        {
                            newContent += (lines[i] + Environment.NewLine);
                        }
                        

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateOffDesupratorClosedPRV(string filePath, int position , int start)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                bool isChange = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (FirstLine.Length > 1 && FirstLine[0].Trim() == (Convert.ToString(start)) && !isChange)
                    {

                        string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[position] = "-16";
                            string joinedLine = "";
                            //string joinedLine = GetSpaces(newLine[0]) + newLine[0] + GetSpaces(newLine[1]) + newLine[1] + "   " + newLine[2] + "   " + newLine[3] + "   " + newLine[4];

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            isChange = true;
                            continue;
                        
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateLpNo(string filePath , int lpNo)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    
                        if (i == 1)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[2] = Convert.ToString(lpNo);
                            string joinedLine = "";

                            for (int st = 0; st < newLine.Length; st++)
                            {

                                if (st == newLine.Length - 1)
                                {
                                    joinedLine += " " + newLine[st];
                                }
                                else
                                {
                                    joinedLine += "   " + newLine[st];
                                }

                             }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateDesupratorSecond(string filePath, int lpCount)
        {
            try
            {

                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;


                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("8  15   0")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[3] = "8";
                            string joinedLine = "   " + newLine[0] + "  " + newLine[1] + "   " + newLine[2] + "   " + newLine[3];

                            for (int st = 4; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void fillProcessSteamTemperatur(string filePath , int serialNumber , string pst , int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 17;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 16;
                    }
                }
                else if(turbineDataModel.PST > 0)
                {
                    serialNumber = 3;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string sign = "-";
                            double tmp = -1;
                            if (Double.TryParse(pst.Trim(), out tmp))
                            {
                                if (tmp == 0)
                                    sign = "";
                            }


                            newLine[3] = sign + ConvertToKreislInputFormat(pst.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                joinedLine = "  " + newLine[0] + " " + newLine[1];
                            }
                            else
                            {
                                joinedLine = "   " + newLine[0] + " " + newLine[1];

                            }
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                // Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }

        public  void PSTONOFF(string filePath, int lpCount)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("3   4   3   2")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            double val = 8;
                            if (val == -8)
                            {
                                newLine[4] = "8";
                            }
                            else
                            {
                                newLine[4] = "-8";
                            }
                            string joinedLine = "   " + newLine[0] + "   " + newLine[1] + "   " + newLine[2] + "   " + newLine[3];
                            if (newLine[4].Length > 1)
                            {
                                joinedLine += "  " + newLine[4];
                            }
                            else
                            {
                                joinedLine += "   " + newLine[4];
                            }

                            for (int st = 5; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void PSTONOFFSecond(string filePath, int lpCount)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString("8  15   0")))
                    {
                        count++;
                        if (count == lpCount)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            double val = 8;
                            if (val == -8)
                            {
                                newLine[3] = "8";
                            }
                            else
                            {
                                newLine[3] = "-8";
                            }
                            string joinedLine = "   " + newLine[0] + "  " + newLine[1] + "   " + newLine[2];
                            if (newLine[3].Length > 1)
                            {
                                joinedLine += "  " + newLine[3];
                            }
                            else
                            {
                                joinedLine += "   " + newLine[3];
                            }

                            for (int st = 4; st < newLine.Length; st++)
                            {

                                int spaceCount = 4 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }

        public void FillPressureDesh(string filePath, int serialNumber, string Pressure, int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 4;
                    }
                    else if(!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 4;
                    }
                }
                else if(turbineDataModel.PST > 0)
                {
                    serialNumber = 8;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            // Console.WriteLine(newLine[2]);
                            newLine[2] = ConvertToKreislInputFormat(Pressure.ToString());
                            string joinedLine = "   " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    File.WriteAllText(filePath, newContent);
                    Console.WriteLine(newContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillInletPressure", ex.Message);
            }
        }
        public int getSerialNumber(string equipmentNumber, string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(equipmentNumber))
                {
                    string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return Convert.ToInt32(newLine[0]);
                }
            }
            return 0;
        }
        public string ConvertToKreislInputFormat(string inputString)
        {
            if (double.TryParse(inputString, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                // Round the number to 3 decimal places
                double roundedNumber = Math.Round(number, 3);

                // Format the number to always show 3 decimal places
                string formattedNumber = roundedNumber.ToString("F3", CultureInfo.InvariantCulture);

                return formattedNumber;
            }
            else
            {
                return "Invalid input";
            }
        }
        public void FillVariablePower(string filePath, int serialNumber, string power)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = 1;
                }
                else if(turbineDataModel.PST > 0)
                {
                    serialNumber = 7;
                }
                else
                {
                    serialNumber = 6;
                }
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] FirstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (FirstLine.Length > 1 && FirstLine[0] == (Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            //Console.WriteLine(newLine[5]);
                            newLine[2] = ConvertToKreislInputFormat(power.ToString());
                            string joinedLine = "   " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                //Console.WriteLine("Load points deleted successfully.");
                //Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillExhaustPressure", ex.Message);
            }
        }
        public void FillExhaustPressure(string filePath, int serialNumber, string pressure2, int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = 7;
                }
                else if (turbineDataModel.PST > 0)
                {
                    serialNumber = 2;
                }
                else
                {
                    serialNumber = 4;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            //Console.WriteLine(newLine[5]);
                            newLine[2] = ConvertToKreislInputFormat(pressure2.ToString());
                            string joinedLine = "   " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                //Console.WriteLine("Load points deleted successfully.");
                //Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillExhaustPressure", ex.Message);
            }

        }
        public void FillInletPressure(string filePath, int serialNumber, string Pressure, int offset = 0)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 23;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 18;
                    }
                }
                else if(turbineDataModel.PST > 0)
                {
                    serialNumber = 6;
                }
                else
                {
                    serialNumber = 5;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            // Console.WriteLine(newLine[2]);
                            newLine[2] = ConvertToKreislInputFormat(Pressure.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                joinedLine = "  " + newLine[0] + " " + newLine[1];
                            }
                            else
                            {
                                joinedLine = "   " + newLine[0] + " " + newLine[1];
                            }
                            
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    File.WriteAllText(filePath, newContent);
                    Console.WriteLine(newContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillInletPressure", ex.Message);
            }
        }
        public void FillInletTemperature(string filePath, int serialNumber, string temperature, int offset = 0)
        {
            try
            {
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 23;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 18;
                    }
                }
                else if(turbineDataModel.PST > 0)
                {
                    serialNumber = 6;
                }
                else
                {
                    serialNumber = 5;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string sign = "-";
                            double tmp = -1;
                            if (Double.TryParse(temperature.Trim(), out tmp))
                            {
                                if (tmp == 0)
                                    sign = "";
                            }
                            

                            newLine[3] = sign + ConvertToKreislInputFormat(temperature.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                joinedLine = "  " + newLine[0] + " " + newLine[1];
                            }
                            else
                            {
                                joinedLine = "   " + newLine[0] + " " + newLine[1];
                            }
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                // Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }
        public void FillMassFlow(string filePath, int serialNumber, string massflow, int offset = 0)
        {
            try
            {
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 23;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 18;
                    }
                }
                else if (turbineDataModel.PST > 0)
                {
                    serialNumber = 6;
                }
                else
                {
                    serialNumber = 5;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[5] = ConvertToKreislInputFormat(massflow.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                joinedLine = "  " + newLine[0] + " " + newLine[1];
                            }
                            else
                            {
                                joinedLine = "   " + newLine[0] + " " + newLine[1];
                            }
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void fillCapacity(string filePath, int serialNumber, string massflow , int offset = 0)
        {
            try
            {
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 9;
                    }
                }
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(serialNumber.ToString() + " "))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[5] = ConvertToKreislInputFormat(massflow.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                if (turbineDataModel.DumpCondensor)
                                {
                                    joinedLine = "   " + newLine[0] + " " + newLine[1];
                                }
                                else if (!turbineDataModel.DumpCondensor)
                                {
                                    joinedLine = "   " + newLine[0] + " " + newLine[1];
                                }
                            }
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void ProcessMassFlow(string filePath, int serialNumber, string massflow, int offset = 0)
        {
            try
            {
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 16;
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        serialNumber = 8;
                    }
                }
                else if (turbineDataModel.PST > 0)
                {
                    serialNumber = 9;
                }
                
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = offset;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(serialNumber.ToString()+" "))
                    {
                        count++;
                        if (count == 2)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[5]);
                            newLine[5] = ConvertToKreislInputFormat(massflow.ToString());
                            string joinedLine = "";
                            if (turbineDataModel.DeaeratorOutletTemp > 0)
                            {
                                if (turbineDataModel.DumpCondensor)
                                {
                                    joinedLine = "  " + newLine[0] + " " + newLine[1];
                                }
                                else if (!turbineDataModel.DumpCondensor)
                                {
                                    joinedLine = "   " + newLine[0] + " " + newLine[1];
                                }
                            }else if(turbineDataModel.PST > 0)
                            {
                                joinedLine = "   " + newLine[0] + " " + newLine[1];
                            }
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void FillEfficiencies()
        {
            double pow = turbineDataModel.AK25;
            string filePath = "C:\\Users\\z00528mr\\Downloads\\checking_again\\KREISLa1.ERG";
            double generatorElementNo = GetEquipmentElementNumber(filePath, kGraphDataModel.GeneratorNumber);
            string searchString = "!PN TURB"; // The string to search for

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                int foundIndex = -1;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchString))
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex != -1)
                {
                    string[] resultLines = new string[3];
                    int count = 0;

                    for (int offset = 1; offset <= 5; offset += 2)
                    {
                        int lineIndex = foundIndex + offset;
                        if (lineIndex < lines.Length)
                        {
                            resultLines[count] = lines[lineIndex];
                            count++;
                        }
                    }
                    for(int j = 0; j < 3; ++j)
                    {
                        string[] noList = resultLines[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // to be filled here
                    }
                }
                else
                {
                    Console.WriteLine($"The string '{searchString}' was not found in the file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
        public void FillVari40()
        {
            string vari40 = "5        40.000     2.000   KreisL";
            string datFilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";

            string[] lines = File.ReadAllLines(datFilePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    if (double.TryParse(parts[0], out double first) &&
                        double.TryParse(parts[1], out double second) &&
                        double.TryParse(parts[2], out double third))
                    {
                        if (second >= 40)
                        {
                            lines[i] = vari40 + Environment.NewLine + lines[i];
                            break;
                        }
                    }
                }
            }
            File.WriteAllLines(datFilePath, lines);
        }
        public void FillWheelChamberPressure(string filePath, string serialNumber, string wheelChamberTemp)
        {
            try
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = "6 0";
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {

                    
                    if (lines[i].Trim().StartsWith(serialNumber))
                    {
                        count++;
                        if (count == 1)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            newLine[2] = ConvertToKreislInputFormat(wheelChamberTemp.ToString());
                            string joinedLine = "   " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; ++st)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);

                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    File.WriteAllText(filePath, newContent);
                    // Console.WriteLine("Load points deleted successfully.");
                    Console.WriteLine(newContent);

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("fillInletPressure", ex.Message);
            }
        }
        
        public void AddLoadPoint(int loadPointNumber, LoadPoint loadPoint, string filePath)
        {
            string wheelChamberPress = ConvertToKreislInputFormat((loadPoint.Pressure / 2.00).ToString());
            string inletPressure = ConvertToKreislInputFormat(loadPoint.Pressure.ToString());
            string exhaustPress = ConvertToKreislInputFormat(loadPoint.BackPress.ToString());
            string inletTemperature = ConvertToKreislInputFormat(loadPoint.Temp.ToString());
            string massFlow = ConvertToKreislInputFormat(loadPoint.MassFlow.ToString());
            string lpNo = "";
            if (loadPointNumber > 0 && loadPointNumber < 10)
            {
                lpNo = " " + loadPointNumber.ToString();
            }
            else
            {
                lpNo = loadPointNumber.ToString();
            }
            string newLoadPoint = "\n0.1 0" + lpNo + " 1 0 0 3 0 0 0   0";
            for (int i = 1; i <= 5; ++i)
                newLoadPoint += Environment.NewLine;
            newLoadPoint += "   1   8   4   1   0   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n   3  13   3   5   0   6   0   0   0   0   0   0   0   0   0   0   0   0\r\n   4   7   1   3   0   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n   5  15   0   4   0   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n   6   9   0   0   0   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n   7  19   6   7   0   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n 150  17   6  -4  -1   0   0   0   0   0   0   0   0   0   0   0   0   0\r\n 999   0   0   0   0   0   0   0   0   0   0   0   0   0   0   0   0   0\n";
            
            //first 2 lines
            newLoadPoint += "   1 0";
            newLoadPoint += new String(' ', 10 - wheelChamberPress.Length);
            newLoadPoint += (wheelChamberPress);
            newLoadPoint += "     0.833     0.000     0.000     0.000     0.000     0.000\n";
            newLoadPoint += "   1 0     0.000     0.000     0.000     0.000     0.000     0.000     0.000\r\n";
            newLoadPoint += "   3 0     0.000     0.000     0.000     0.000     0.000     0.000     0.000\r\n   3 0     0.000     0.000     0.000     0.000     0.000     0.000     0.000\r\n";
            
            //
            newLoadPoint += "   4 1";
            newLoadPoint += new String(' ', 10 - exhaustPress.Length);
            newLoadPoint += (exhaustPress);
            newLoadPoint += "     0.833     0.000     0.000     0.000     0.000     0.000\n";
            newLoadPoint += "   4 1     0.000     0.000     0.000     0.000     0.000     0.000     0.000\n";

            //5th
            newLoadPoint += "   5 0";
            newLoadPoint += new string(' ', 10 - inletPressure.Length);
            newLoadPoint += (inletPressure);
            newLoadPoint += new string(' ', 9 - inletTemperature.Length);
            newLoadPoint += "-" + inletTemperature + "     0.000";
            newLoadPoint += new string(' ', 10 - massFlow.Length);
            newLoadPoint += (massFlow);
            newLoadPoint += "     0.000     0.000     0.000\r\n";

            newLoadPoint += "   5 0     0.000     0.000     0.000     0.000     0.000     0.000     0.000\r\n   6 2     0.000     0.000 12000.000     0.000     0.000     0.000     0.000\r\n   6 2     0.000     0.000     0.000     0.000     0.000     0.000     0.000\r\n";


            newLoadPoint += "   7 0     0.995  -240.000     0.000     0.042    -1.000     0.000     0.000\r\n   7 0     0.000     0.000     0.000     0.000     0.000     0.000     0.000";
            string lineToAdd = newLoadPoint;

            try
            {
                // Append the line to the file
                File.AppendAllText(filePath, lineToAdd + Environment.NewLine);
                Console.WriteLine("Line added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        public void UpdateRPM(string filePath, int serialNumber, string Rpm)
        {
            try
            {
                int lineNumber = 0;
                if( turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = 1;
                    if (turbineDataModel.DumpCondensor)
                    {
                        lineNumber = 15;
                    }else if (!turbineDataModel.DumpCondensor)
                    {
                        lineNumber = 14;
                    }
                }
                else if (turbineDataModel.PST > 0)
                {
                    serialNumber = 7;
                    lineNumber = 4;
                }
                else
                {
                    serialNumber = 6;
                    lineNumber = 4;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith(Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == lineNumber)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[4]);
                            newLine[2] = ConvertToKreislInputFormat(Rpm.ToString());
                            string joinedLine = "   " + newLine[0] + "  ";
                            for (int st = 1; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateRPM2(string filePath, int serialNumber, string Rpm)
        {
            try
            {
                int lineNumber = 0;
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = 1;
                    if (turbineDataModel.DumpCondensor)
                    {
                        lineNumber = 13;
                    }else if(!turbineDataModel.DumpCondensor)
                    {
                        lineNumber = 12;
                    }
                }
                else if (turbineDataModel.PST > 0)
                {
                    serialNumber = 7;
                    lineNumber = 2;
                }
                else
                {
                    serialNumber = 6;
                    lineNumber = 2;
                }
                // Read the entire file content into an array of lines
                string[] lines = File.ReadAllLines(filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;

                // Loop through the lines and delete the load points after the first occurrence
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] firstLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (firstLine.Length > 1 && firstLine[0] == (Convert.ToString(serialNumber)))
                    {
                        count++;
                        if (count == lineNumber)
                        {
                            string[] newLine = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(newLine[4]);
                            newLine[4] = ConvertToKreislInputFormat(Rpm.ToString());
                            string joinedLine = "   " + newLine[0] + " " + newLine[1];
                            for (int st = 2; st < newLine.Length; st++)
                            {

                                int spaceCount = 10 - newLine[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += newLine[st];

                            }
                            // string joinedLine = string.Join(" ", newLine);
                            // Add a space at the beginning of the joined string
                            // joinedLine = " " + joinedLine;
                            newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(filePath, newContent);
                Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("fillMassFlow:", ex.Message);
            }
        }
        public void UpdateGeneratorEff(double eff)
        {
            try
            {
                string serialNumber = "6";
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    serialNumber = "1";
                }
                if (turbineDataModel.PST > 0)
                {
                    serialNumber = "7";
                }
                else
                {
                    serialNumber = "6";
                }

                string[] lines = File.ReadAllLines(StartKreisl.filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == serialNumber)
                    {
                        count++;
                        if (count == 6)
                        {
                            subpart[3] = ConvertToKreislInputFormat(eff.ToString());
                            string joinedLine = "   " + subpart[0] + "  ";// + subpart[1];
                            for (int st = 1; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }

                            newContent = newContent+ joinedLine + Environment.NewLine;
                            //newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine) ;
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }
                }
                File.WriteAllText(StartKreisl.filePath, newContent);
            }
            catch (Exception ex)
            {
                logger.LogError("Kreisl Update Generator Efficiency", ex.Message);
            }
        }
        public void updateGeneratorSpecs(double genPower, double eff4, double eff3, double eff2, double eff1)
        {
            try
            {
                string subpartg = "";
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    subpartg = "1";
                }
                else if (turbineDataModel.PST > 0)
                {
                    subpartg = "7";
                }
                else
                {
                    subpartg = "6";
                }
                string[] lines = File.ReadAllLines(StartKreisl.filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0 ; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == subpartg)
                    {
                        count++;
                        if (count == 6)
                        {
                            subpart[1] = ConvertToKreislInputFormat(genPower.ToString());
                            subpart[3] = ConvertToKreislInputFormat(eff4.ToString());


                            subpart[4] = ConvertToKreislInputFormat(eff3.ToString());
                            subpart[5] = ConvertToKreislInputFormat(eff2.ToString());
                            subpart[6] = ConvertToKreislInputFormat(eff1.ToString());
                            string joinedLine = "   " + subpart[0] + "  ";// + subpart[1];
                            for (int st = 1; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }

                            newContent = (newContent  + joinedLine + Environment.NewLine);
                            //newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(StartKreisl.filePath, newContent);
                //Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                logger.LogError("Kreisl UpdateGeneratorSpecs", ex.Message);
                //Console.WriteLine("DeleteLoadPoints", ex.Message);
            }
        }
        public double getPowerFromErg(string parameter)
        {
            string[] files = File.ReadAllLines("C:\\testDir\\TURBATURBAE1.DAT.ERG");
            for(int i = 0; i< files.Length; i++)
            {
                if (files[i].Contains(parameter))
                {
                    i+=2;
                    string[] subpart = files[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return Convert.ToDouble(subpart[0]+'0');
                }
            }
            return 0;
        }
        
        public void DatFileInitParamsExceptLP()
        {
            try
            {
                DATFileProcessor dATFileProcessor = new DATFileProcessor();
                double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;
                double estimatedPowerFromHBD = getPowerFromErg("KLEMMENLEISTUNGEN - KW - output at terminals");

                // Variables for calculations
                double nozzleCount, nozzleFront;
                double genPower = 1000, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
                double turbinePower, gearboxPower;

                Console.WriteLine("Calculating nozzles and powertrain specs to write into DAT file...");

                PowerEfficiencyDataPoint reqPowDataPoint = new PowerEfficiencyDataPoint();
                //reqPowDataPoint.fillPowerEfficiencyDataModel();
                foreach (PowerEfficiencyDataPoint powerEffPoint in powerEfficiencyModel.PowerEfficiencyPoints)
                {
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
                genPower = estimatedPowerFromHBD;
                genEff4_4 = reqPowDataPoint.Eff100 / 100.000;
                genEff3_4 = reqPowDataPoint.Eff75 / 100.000;
                genEff1_2 = reqPowDataPoint.Eff50 / 100.000;
                genEff1_4 = reqPowDataPoint.Eff25 / 100.000;

                Console.WriteLine($"Generator Power kW / Eff : {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
                updateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);
                dATFileProcessor.updateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);


                HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
                hBDPowerCalculator.HBDUpdateEffGenerator(genEff4_4);

                // Gearbox Power calculation
                gearboxPower = getPowerFromErg("KUPPLUNGSLEISTUNGEN - KW - output at coupling");
                Console.WriteLine($"Gearbox Power kW : {gearboxPower}");
                //updateGearboxSpecs(gearboxPower);

                // Turbine Power calculation
                turbinePower = getPowerFromErg("INNERE LEISTUNGEN - KW - inner power");
                Console.WriteLine($"Turbine Shaft Power kW: {gearboxPower}");
                //updateTurbineSpecs(turbinePower);
                updateGearBoxPower(gearboxPower);
                dATFileProcessor.updateGearboxSpecs(gearboxPower);
                updateTurbinePower(turbinePower);
                dATFileProcessor.updateTurbineSpecs(turbinePower);

            }
            catch (Exception ex)
            {
                logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
            }
        }
        public void DatFileInitParamsExceptLPKriesl()
        {
            try
            {
                double volumetricFlowFromHBD = turbineDataModel.VolumetricFlow;
                double estimatedPowerFromHBD = turbineDataModel.AK25;

                // Variables for calculations
                double nozzleCount, nozzleFront;
                double genPower = 1000, genEff4_4, genEff3_4, genEff1_2, genEff1_4;
                double turbinePower, gearboxPower;

                Console.WriteLine("Calculating nozzles and powertrain specs to write into DAT file...");

                PowerEfficiencyDataPoint reqPowDataPoint = new PowerEfficiencyDataPoint();
                //reqPowDataPoint.fillPowerEfficiencyDataModel();
                foreach (PowerEfficiencyDataPoint powerEffPoint in powerEfficiencyModel.PowerEfficiencyPoints)
                {
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
                genEff4_4 = reqPowDataPoint.Eff100 / 100.000;
                genEff3_4 = reqPowDataPoint.Eff75 / 100.000;
                genEff1_2 = reqPowDataPoint.Eff50 / 100.000;
                genEff1_4 = reqPowDataPoint.Eff25 / 100.000;

                Console.WriteLine($"Generator Power kW / Eff : {genPower} {genEff4_4} {genEff3_4} {genEff1_2} {genEff1_4}");
                updateGeneratorSpecs(genPower, genEff4_4, genEff3_4, genEff1_2, genEff1_4);

                HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
                hBDPowerCalculator.HBDUpdateEffGenerator(genEff4_4);

                // Gearbox Power calculation
                gearboxPower = genPower / (genEff4_4);
                Console.WriteLine($"Gearbox Power kW : {gearboxPower}");
                //updateGearboxSpecs(gearboxPower);

                // Turbine Power calculation
                turbinePower = gearboxPower / 0.86;
                Console.WriteLine($"Turbine Shaft Power kW: {gearboxPower}");
                //updateTurbineSpecs(turbinePower);
                updateGearBoxPower(gearboxPower);
                updateTurbinePower(turbinePower);

            }
            catch (Exception ex)
            {
                logger.LogError("DatFileInitParamsExceptLP", ex.StackTrace);
            }
        }
        public void updateGearBoxPower(double gearBoxPower)
        {
            try
            {
                string subpartg = "";
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    subpartg = "1";
                }
                else if (turbineDataModel.PST > 0)
                {
                    subpartg = "7";
                }
                else
                {
                    subpartg = "6";
                }
                string[] lines = File.ReadAllLines(StartKreisl.filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i <  lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == subpartg)
                    {
                        count++;
                        if (count == 5)
                        {
                            subpart[1] = ConvertToKreislInputFormat(gearBoxPower.ToString());


                            string joinedLine = "   " + subpart[0] + "  ";// + subpart[1];
                            for (int st = 1; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }
                            newContent += (joinedLine + Environment.NewLine);
                            //newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(StartKreisl.filePath, newContent);
                //Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateGearBoxPower", ex.Message);
            }
        }
        public void updateTurbinePower(double turbinePower)
        {
            try
            {
                string subpartg = "";
                if (turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    subpartg = "1";
                }
                else if(turbineDataModel.PST > 0)
                {
                    subpartg = "7";
                }
                else
                {
                    subpartg = "6";
                }
                string[] lines = File.ReadAllLines(StartKreisl.filePath);
                bool firstLPFound = false;
                string newContent = "";
                int count = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] subpart = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subpart.Length > 1 && (subpart[0]) == subpartg)
                    {
                        count++;
                        if (count == 4)
                        {
                            subpart[1] = ConvertToKreislInputFormat(turbinePower.ToString());


                            string joinedLine = "   " + subpart[0] + "  ";// + subpart[1];
                            for (int st = 1; st < subpart.Length; st++)
                            {
                                int spaceCount = 10 - subpart[st].Length;
                                for (int sp = 1; sp <= spaceCount; sp++)
                                {
                                    joinedLine += " ";
                                }
                                joinedLine += subpart[st];
                            }
                            newContent +=(joinedLine + Environment.NewLine) ;
                            //newContent += (joinedLine + Environment.NewLine);
                            continue;
                        }
                        newContent += (lines[i] + Environment.NewLine);
                    }
                    else
                    {
                        newContent += (lines[i] + Environment.NewLine);
                    }

                }

                File.WriteAllText(StartKreisl.filePath, newContent);
                //Console.WriteLine("Load points deleted successfully.");
                Console.WriteLine(newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateTurbinePower", ex.Message);
            }
        }
        public double GetEquipmentElementNumber(string filePath, double equipmentNo)
        {
            //string filePath = "C:\\Users\\z00528mr\\Downloads\\checking_again\\KREISLa1.ERG";
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 2)
                    {
                        if (double.TryParse(parts[1], out double secondValue))
                        {
                            if (secondValue == kGraphDataModel.GeneratorNumber)
                            {
                                if (double.TryParse(parts[0], out double firstValue))
                                {
                                    return firstValue; 
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("No matching lines found for equipment : " + equipmentNo);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return 0;
        }
    }
}
