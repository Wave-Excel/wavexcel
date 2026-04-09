namespace Checks.Exec_PowerMatch;
using System.DirectoryServices.AccountManagement;
using System.IO;
using Exec_HMBD_Configuration;
using Handlers.DAT_Handler;
using Ignite_X.src.core.Handlers;
using Ignite_X.src.core.Models;
using Ignite_X.src.core.Services;
using Ignite_X.src.core.Utilities;
using Ignite_x_wavexcel;
using Interfaces.IERGHandlerService;
using Interfaces.ILogger;
using Interfaces.IThermodynamicLibrary;
using Kreisl.KreislConfig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Models.AdditionalLoadPointModel;
using Models.LoadPointDataModel;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
using Newtonsoft.Json;
using OfficeOpenXml;
using Optimizers.Exec_ERG_PowerNoLoadOptimizer;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using StartExecutionMain;
using StartKreislExecution;
// using Ignite_x_wavexcel;
using Turba.Exec_TurbaConfig;
using Turba.TurbaConfiguration;

//using EventKitUI;

class ExecPowerMatch
    {
        private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";

        private TurbaOutputModel turbaOutputModel;
        private TurbineDataModel turbineDataModel;
        private IERGHandlerService eRGHandlerService;
        private IThermodynamicLibrary thermodynamicService;
        private IConfiguration configuration;
        private ILogger logger;
        int i_powermatch;
        int maxLoadPoints = 10;
        public static double[,] data = new double[,] {
            {12.5, 6.4, 10.7},
            {16.0, 7.4, 13.5},
            {20.0, 8.0, 17.0},
            {25.0, 8.8, 21.5},
            {32.0, 9.8, 27.0},
            {40.0, 11.5, 33.7},
            {50.0, 13.0, 42.6},
            {56.0, 14.4, 42.7},
            {63.0, 15.5, 53.8},
            {71.0, 16.7, 53.8},
            {80.0, 18.0, 63.0},
            {90.0, 19.5, 67.5},
            {100.0, 21.0, 85.0}
        };
    private bool isLP5Change = false;

    public ExecPowerMatch()
        {
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
            maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            eRGHandlerService = StartKreisl.GlobalHost.Services.GetRequiredService<IERGHandlerService>();
            thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
            logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
            turbaOutputModel = TurbaOutputModel.getInstance();
            turbineDataModel = TurbineDataModel.getInstance();
        isLP5Change = false;
        }

        public void HBDUpdateEffKriesl(double EffValue,int maxlp=0)
        {
        
        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 2 || turbineDataModel.OneLPMissing == true)
        {
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            KreislIntegration kreislIntegration = new KreislIntegration();
            kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", Convert.ToString(EffValue));
            kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "1", Convert.ToString(EffValue));
            TurbaAutomation turbaAutomation = new TurbaAutomation();
            //kreislIntegration.LaunchKreisL();
            
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();

            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            kreislDATHandler.DatFileInitParamsExceptLP();
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbineDataModel.AK25 = eRGHandlerService.ExtractPowerFromERG(CustomExecutedClass.ergFilePath);
            turbineDataModel.FinalPower = turbineDataModel.AK25;
        }
        else
        {
            //KreislDATHandler kreislDATHandler = new KreislDATHandler();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            if(turbineDataModel.DeaeratorOutletTemp > 0)
            {
                UpdateOutletTempAndEnth();
                if (turbineDataModel.DumpCondensor)
                {
                    if (turbineDataModel.PST < turbineDataModel.OutletTemperature)
                    {
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, 1);
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, 1);
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, 1);
                    }
                    else
                    {
                        kreislDATHandler.UpdateDesupratorOffClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, 1);
                        kreislDATHandler.UpdateDesupratorOffClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, 1);
                        kreislDATHandler.UpdateDesupratorOffClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, 1);
                    }
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    if (turbineDataModel.PST < turbineDataModel.OutletTemperature)
                    {
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, 1);
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, 1);
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, 1);
                    }
                    else
                    {
                        kreislDATHandler.UpdateOffDesupratorClosedPRV(StartKreisl.filePath, 5, 15);
                        kreislDATHandler.UpdateOffDesupratorClosedPRV(StartKreisl.filePath, 4, 16);
                        kreislDATHandler.UpdateOffDesupratorClosedPRV(StartKreisl.filePath, 3, 17);
                    }
                }
            }
            else if (turbineDataModel.PST > 0)
            {
                UpdateOutletTempAndEnth();
                if (turbineDataModel.PST < turbineDataModel.OutletTemperature)
                {
                    kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, 1);
                    kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, 1);

                }
                else
                {
                    kreislDATHandler.PSTONOFF(StartKreisl.filePath, 1);
                    kreislDATHandler.PSTONOFFSecond(StartKreisl.filePath, 1);
                }
            }

            kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", Convert.ToString(EffValue));
           
            KreislIntegration kreislIntegration = new KreislIntegration();
            

            kreislIntegration.LaunchKreisL();
            TurbaAutomation turbaAutomation = new TurbaAutomation();
            //kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();

            turbineDataModel.AK25 = eRGHandlerService.ExtractPowerFromERG(CustomExecutedClass.ergFilePath);
            
            UpdateGeneratorPowers();


            //turbineDataModel.AK25 = eRGHandlerService.ExtractPowerFromERG(CustomExecutedClass.ergFilePath);
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            turbineDataModel.AK25 = eRGHandlerService.ExtractPowerFromERG(CustomExecutedClass.ergFilePath);
            turbineDataModel.FinalPower = turbineDataModel.AK25;
        }

               
        }

        public void UpdateGeneratorPowers()
        {
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            kreislDATHandler.DatFileInitParamsExceptLP();
        }
        public void CheckPower(int maxlp = 0)
        {
            i_powermatch = 0;
            
            ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
            execHMBDConfiguration.HBDSetDefaultCustomerParams();
            // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            // hBDPowerCalculator.HBDSetDefaultCustomerParams();
            double Efficiency = turbaOutputModel.OutputDataList[1].Efficiency;
            // double Efficiency = Convert.ToDouble(turbaResults_ERG.Cells["R4"].Value);
            Logger("Updating turbine eff in HBD : " + Efficiency);
            if (StartKreisl.kreislKey)
                HBDUpdateEffKriesl(Efficiency,maxlp);
            else
                HBDupdateEff(Efficiency);
            Logger("Checking based ex_ERG_RsminHandler.load power with HBD..");
            // existingFile = new FileInfo(excelPath);
            // package = new ExcelPackage(existingFile);
            // HBDSheet = package.Workbook.Worksheets["OPEN_HBD"];
            turbineDataModel.AK25 = turbineDataModel.FinalPower;
            // HBDSheet.Cells["AK25"].Value = Turbine.finalPower;

            double targetPower = turbineDataModel.AK25;
            targetPower = Math.Floor(targetPower);
            double PowerLP1 = turbaOutputModel.OutputDataList[1].Power_KW;
            double deltaPower = targetPower - PowerLP1;
            bool PowerMatchedLP1 = Math.Abs(deltaPower) <= getPowerMargin() || targetPower <= PowerLP1;

            Logger("kW_LP1: " + PowerLP1 + ", HBDkW:" + targetPower + ", DeltakW:" + Math.Abs(deltaPower));

            if (!PowerMatchedLP1)
            {
                UpdateGeneratorinKriesl();
                KreislIntegration kreislIntegration = new KreislIntegration();
                kreislIntegration.LaunchKreisL();
                turbineDataModel.AK25 = eRGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath);
                turbineDataModel.FinalPower = turbineDataModel.AK25;
                //double targetPower = turbineDataModel.AK25;//Convert.ToDouble(HBDSheet.Cells["AK25"].Value);
                targetPower = Math.Floor(turbineDataModel.AK25);
                PowerLP1 = turbaOutputModel.OutputDataList[1].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q4"].Value);
                deltaPower = targetPower - PowerLP1;
                PowerMatchedLP1 = Math.Abs(deltaPower) <= getPowerMargin() || targetPower <= PowerLP1;

                if (!PowerMatchedLP1)
                {
                    Logger("Warning !! BaseLoad LP1 ex_Power_KNN.Power is outside 25kW delta...");
                    Logger("/\\/\\/\\/\\/\\/\\/\\/\\ TURBINE DESIGN: POWER COULDN'T NOT OPTIMIZED /\\/\\/\\/\\/\\/\\/\\/\\");
                    if (StartKreisl.kreislKey)
                        HBDUpdateEffKriesl(Efficiency,maxlp);
                    else
                        HBDupdateEff(Efficiency);

                    Logger("Output power (kW) : " + PowerLP1.ToString() + " Efficiency(%) : " + Efficiency.ToString());
                    TurbineDesignPage.finalToken.Cancel();
                }
                else
                {
                    Logger("LP1 power is within limits, Checking No load power and Bending..");
                    NoLoadPowerOptimize(maxlp);
                    checkFinalBending(maxlp);
                }
           
            }
            else
            {
                Logger("LP1 power is within limits, Checking No load power and Bending..");
                TurbaAutomation turbaAutomation = new TurbaAutomation();
                NoLoadPowerOptimize(maxlp);
                string bending = "";
                bending = turbaOutputModel.OutputDataList[5].Bending;
                if (!string.IsNullOrEmpty(bending))
                {
                    UpdateLP5Power(maxlp);
                    isLP5Change = true;
                }
                turbaAutomation.LaunchRsmin();
                bending = turbaOutputModel.OutputDataList[5].Bending;
                int count = 0;
                while (!string.IsNullOrEmpty(turbaOutputModel.OutputDataList[5].Bending))
                {
                    if (count == 7)
                    {
                        break;
                    }
                    turbineDataModel.IsScheneChange = true;
                    {
                        count++;
                    }
                    CorrectLP5Bending();
                    turbaAutomation.LaunchTurba(maxlp);
                }
                bending = turbaOutputModel.OutputDataList[5].Bending;
                if (!string.IsNullOrEmpty(bending))
                {
                    turbineDataModel.IsScheneChange = false;
                    CustomExecutedClass customExecutedClass = new CustomExecutedClass();
                    customExecutedClass.Main_CustomFlowPathTest(maxlp);
                    return;

                }

                
                while (turbaOutputModel.OutputDataList[5].Thrust > turbaOutputModel.ThrustLimit && getAUS() > 270)
                {
                        turbineDataModel.AK1OldValue = getAUS();
                        turbineDataModel.IsAK1Change = true;
                        UpdateDATSoftChecks(getAUS() + 1);
                        turbaAutomation.LaunchRsmin();
                        turbineDataModel.AK1NewValue = getAUS();
                }
                if(turbaOutputModel.OutputDataList[5].Thrust > turbaOutputModel.ThrustLimit)
                {
                    turbineDataModel.AK1OldValue = 0;
                    turbineDataModel.AK1NewValue = 0;
                    turbineDataModel.IsAK1Change = false;
                    turbineDataModel.IsScheneChange = false;
                    CustomExecutedClass customExecutedClass = new CustomExecutedClass();
                    customExecutedClass.Main_CustomFlowPathTest(maxlp);
                    return;
                }

                checkFinalBending(maxlp);
            }
        }
    public void ResetCleanUpExecutedNearest()
    {
        // Console.WriteLine("workinghggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg");
        List<PowerNearest> powerNearest = turbineDataModel.ListPower;
        // for(int i=1;i<=)
        int k = Convert.ToInt32(powerNearest[0].KNearest);
        // for(int i=1;i<=k;i++){
        //     //   if(powerNearest[i].KNearest=="Y"){
        //     //     powerNearest[i].KNearest="";
        //     //   }
        //     powerNearest[i].Efficiency=0;
        //     powerNearest[i].ProjectName=
        //     }
        powerNearest.Clear();
        turbineDataModel.fillPowerNearestData();
        turbineDataModel.ListPower[0].SteamPressure = turbineDataModel.InletPressure;
        turbineDataModel.ListPower[0].SteamTemperature = turbineDataModel.InletTemperature;
        turbineDataModel.ListPower[0].SteamMass = turbineDataModel.MassFlowRate;
        turbineDataModel.ListPower[0].ExhaustPressure = turbineDataModel.ExhaustPressure;

        turbineDataModel.ListPower[0].KNearest = turbineDataModel.NoOfExecuted.ToString();

    }
    private string UpdateLine(string line, int startIndex, int length, double value, string format = "00000.000")
        {
            if (string.IsNullOrEmpty(line) || startIndex < 1 || startIndex + length - 1 > line.Length)
            {
                Logger($"Warning: Invalid line update parameters");
                return line ?? "";
            }

            string formattedValue = value.ToString(format);
            if (formattedValue.Length > length)
            {
                formattedValue = formattedValue.Substring(0, length);
            }
            else if (formattedValue.Length < length)
            {
                formattedValue = formattedValue.PadLeft(length);
            }

            return line.Substring(0, startIndex - 1) + formattedValue + line.Substring(startIndex + length - 1);
        }
        private bool IsFileReadyForOpen(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
        public void UpdateDATSoftChecks(double variableA)
        {
            string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";

            if (!File.Exists(filePath))
            {
                Logger($"Error: File not found at {filePath}");
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(filePath);
                var fileLines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);


                string parameterA = "!               AUSGLEICHSKOLBENDURCHMESSER";

                for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
                {
                    if (fileLines[lineNumber].Contains(parameterA) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableA);
                    }
                }

                fileContent = string.Join(Environment.NewLine, fileLines);

                int retryCount = 0;
                while (!IsFileReadyForOpen(filePath) && retryCount < 20)
                {
                    System.Threading.Thread.Sleep(50);
                    retryCount++;
                }

                File.WriteAllText(filePath, fileContent);
                Logger($"DAT file updated with constraint-corrected parameters");
            }
            catch (Exception ex)
            {
                Logger($"Error updating DAT file: {ex.Message}");
            }
        }

        public double getAUS()
        {
            string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";

            if (!File.Exists(filePath))
            {
                Logger($"Error: File not found at {filePath}");
                return 0;
            }

            try
            {
                string fileContent = File.ReadAllText(filePath);
                var fileLines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // string parameterB = "!ND   DM REGELR";
                // string parameterR = "!     RADKAMMER";
                // string parameterD = "!               DRUCKZIFFERN";
                // string parameterI = "!               INNENDURCHMESSER";
                string parameterA = "!               AUSGLEICHSKOLBENDURCHMESSER";

                for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
                {
                    if (fileLines[lineNumber].Contains(parameterA) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        return Convert.ToDouble(fileLines[lineNumber].Substring(17, 9));
                        // fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableA);
                    }
                }
                return 0;

            }
            catch (Exception ex)
            {
                Logger($"Error updating DAT file: {ex.Message}");
                return 0;
            }
        }
    public static double getNextNB(double curr)
    {
        for (int i = 0; i < data.GetLength(0); i++)
        {
            if (data[i, 0] > curr)  // Check SE column
            {
                return data[i, 0];
            }
        }
        return 0;
    }

    public static void CorrectDatFileF(string FirstVal, string SecondVal)
    {
        string datFilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] datFile = File.ReadAllLines(datFilePath);
        bool fileModified = true;

        // string[] datFile = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.DAT");
        for (int i = 0; i < datFile.Length; i++)
        {
            if (datFile[i].Contains("!ST GI  DM    LANG BETA  SE SZ PR  RT RAD SP HD SAX ML D K D  DD  ANS  EXPA  DEP EXPI WSZ"))
            {
                for (int j = i + 1; j < datFile.Length; j++)
                {
                    string[] lineArray = datFile[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineArray[0] == FirstVal && lineArray[1] == SecondVal)
                    {
                        if (Convert.ToDouble(lineArray[4]) < 25)
                        {
                            double ans = Convert.ToInt64(Convert.ToDouble(lineArray[4]) * Convert.ToDouble(lineArray[5]) / 25);
                            if (Convert.ToDouble(SecondVal) % 2 == 0)
                            {
                                lineArray[4] = "25";
                                if (ans % 2 == 1)
                                {
                                    lineArray[5] = Convert.ToString(ans);
                                }
                                else
                                {
                                    lineArray[5] = Convert.ToString(ans + 1);
                                }
                                lineArray[10] = Convert.ToString(2);
                            }
                            else
                            {
                                lineArray[4] = "25";
                                if (ans % 2 == 0)
                                {
                                    lineArray[5] = Convert.ToString(ans);
                                }
                                else
                                {
                                    lineArray[5] = Convert.ToString(ans+1);
                                }
                                lineArray[10] = Convert.ToString(2);
                            }
                        }
                        else
                        {
                            lineArray[10] = Convert.ToString(2);
                        }
                        datFile[j] = ReconstructBladeLine(lineArray);
                    }
                }
            }
        }
        if (fileModified)
        {
            File.WriteAllLines(datFilePath, datFile);
            Console.WriteLine($"File {datFilePath} has been updated.");
        }
        else
        {
            Console.WriteLine("No modifications were needed.");
        }
    }
    public static string ReconstructBladeLine(string[] line)
    {
        if (Convert.ToInt64(line[1]) % 2 == 0)
        {
            string ans = (line[0].Length > 1 ? " " : "  ") + line[0] +
        " " + line[1] +
        "  " + line[2] +
        " " + line[3];
            if (line[4].Contains(".0")){
                ans+="  " + line[4];
            }
            else
            {
                ans+="  " + line[4] + ".0";
            }
        
            for (int i = 5; i <= 12; i++)
            {
                ans += " " + line[i];
            }
            ans += "  " + line[13];
            ans += " " + line[14];
            ans += " " + line[15];
            ans += "     " + line[16];
            ans += " " + line[17];
            return ans;
        }
        else
        {
            string ans = (line[0].Length > 1 ? " " : "  ") + line[0] +
       " " + line[1] +
       "  " + line[2] +
       " " + line[3];
       if (line[4].Contains(".0"))
            {
                ans += "  " + line[4];
            }
            else
            {
                ans += "  " + line[4] + ".0";
            }
            for (int i = 5; i <= 12; i++)
            {
                ans += " " + line[i];
            }
            ans += "  " + line[13];
            ans += " " + line[14];
            ans += "     " + line[15];
            ans += " " + line[16];
            // ans += " " + line[17];
            return ans;
        }
    }

    public static void CorrectDatFileB(string FirstVal, string SecondVal)
    {
        string datFilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string[] datFile = File.ReadAllLines(datFilePath);
        bool fileModified = false;
        for (int i = 0; i < datFile.Length; i++)
        {
            if (datFile[i].Contains("!ST GI  DM    LANG BETA  SE SZ PR  RT RAD SP HD SAX ML D K D  DD  ANS  EXPA  DEP EXPI WSZ"))
            {
                for (int j = i + 1; j < datFile.Length; j++)
                {

                    string[] lineArray = datFile[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineArray[0] == FirstVal && lineArray[1] == SecondVal)
                    {
                        fileModified = true;
                        double SENextNB = getNextNB(Convert.ToDouble(lineArray[4]));
                        double ans = Convert.ToInt64(Convert.ToDouble(lineArray[4]) * Convert.ToDouble(lineArray[5]) / SENextNB);
                        if (Convert.ToDouble(SecondVal) % 2 == 0)
                        {
                            lineArray[4] = Convert.ToString(SENextNB);
                            if (ans % 2 == 1)
                            {
                                lineArray[5] = Convert.ToString(ans);
                            }
                            else
                            {
                                lineArray[5] = Convert.ToString(ans + 1);
                            }

                        }
                        else
                        {
                            lineArray[4] = Convert.ToString(SENextNB);
                            if (ans % 2 == 0)
                            {
                                lineArray[5] = Convert.ToString(ans);
                            }
                            else
                            {
                                lineArray[5] = Convert.ToString(ans+1);
                            }

                        }
                    }
                    if (fileModified)
                    {
                        datFile[j] = ReconstructBladeLine(lineArray);
                        break;
                    }

                }
                if (fileModified)
                {
                    // datFile[j] = ReconstructBladeLine(lineArray);
                    break;
                }
            }
        }
        if (fileModified)
        {
            File.WriteAllLines(datFilePath, datFile);
            Console.WriteLine($"File {datFilePath} has been updated.");
        }
        else
        {
            Console.WriteLine("No modifications were needed.");
        }

    }
    public void CorrectLP5Bending()
    {
        string[] file = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.ERG");

        for (int i = 0; i < file.Length; i++)
        {
            if (file[i].Contains("#UST 5"))
            {
                for (int j = i + 1; j < file.Length; j++)
                {
                    if (file[j].Contains("STUFE SIGZV SIGAZS SIGAZF SIGVS SIGAS  GSS  HSS SIGVF SIGAF  HSF PRESZ PRESS GEF"))
                    {
                        j += 2;

                        for (int k = j; k < file.Length; k++)
                        {
                            if (string.IsNullOrWhiteSpace(file[k]))
                            {
                                break;
                            }

                            string[] lineArray = file[k].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string lstvalue = lineArray[lineArray.Length - 1];
                            string lstvalue1 = lineArray[lineArray.Length - 2];
                            string lstvalue2 = lineArray[lineArray.Length - 3];
                            string lstvalue3 = lineArray[lineArray.Length - 4];
                            Console.WriteLine($"{lstvalue}");
                            Console.WriteLine($"{lstvalue1}");
                            Console.WriteLine($"{lstvalue2}");
                            Console.WriteLine($"{lstvalue3}");

                            if (lstvalue == "F" || lstvalue1 == "F" || lstvalue2 == "F" || lstvalue3 == "F")
                            {
                                CorrectDatFileF(lineArray[0], lineArray[1]);
                            }
                            if (lstvalue == "B" || lstvalue1 == "B" || lstvalue2 == "B" || lstvalue3 == "B")
                            {
                                CorrectDatFileB(lineArray[0], lineArray[1]);
                            }
                        }
                        break;
                    }
                }
                break;
            }
        }
    }
    public string FormatWithDotZero(double value)
    {
        string str = value.ToString();
        return (str.Contains(".")) ? str : str + ".0";
    }
    public void UpdateLP5Power(int maxlp = 0)
    {
       
        string mainTemp = File.ReadAllText("C:\\testDir\\KREISL.DAT");
        updateMainTemplate(Convert.ToString(0.80) , turbaOutputModel.OutputDataList[1].Power_KW);
        KreislIntegration kreislIntegration = new KreislIntegration();
        TurbaAutomation turbaAutomation = new TurbaAutomation();
        //kreislIntegration.LaunchKreisL();
        if(turbineDataModel.DeaeratorOutletTemp > 0)
        {
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            Console.WriteLine("");
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
        }
        else if(turbineDataModel.PST > 0)
        {
            
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            Console.WriteLine("");
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            turbaAutomation.LaunchTurba(maxlp);
            Console.WriteLine("");
        }
        else
        {
            kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislIntegration.LaunchKreisL();
            
            turbaAutomation.LaunchTurba();
        }
    
        

    }
    
    public void updateMassFLow(double mass)
    {
        string[] file = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.DAT");
        
        for (int i = 0; i < file.Length; i++)
        {
            if (file[i].Contains("!LP5  DAMPFMENGE"))
            {
                string dataLine = new string(' ', 88);
                string[] lp5 = file[i+1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                InsertFormattedValues(ref dataLine, lp5 , Convert.ToString(mass));
                file[i + 1] = dataLine;
                break;
            }
        }
        TurbaAutomation turbaAutomation = new TurbaAutomation();
        turbaAutomation.LaunchTurba();

    }
    public string InsertFormattedValues(ref string dataLine, string[] loadPointData,string mass )
    {
        // Insert formatted values into the data line
        dataLine = InsertValue(dataLine, mass, 7, 10, "{0:00000.000}");
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
    public void updateMainTemplate(string eff, double pow)
    {
        string inputFilePath = "";
        KreislDATHandler datHandler = new KreislDATHandler();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor)
            {
                inputFilePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDumpLP5.DAT");
                datHandler.FillTurbineEff(inputFilePath, "7", eff);
                double pst = turbineDataModel.PST;
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(LoadPointDataModel.getInstance().LoadPoints[5].BackPress) + 5) : turbineDataModel.PST;

                datHandler.fillProcessSteamTemperatur(inputFilePath, 16, turbineDataModel.PST.ToString());
                if (pst == 0)
                {
                    turbineDataModel.PST = 0;
                }
                datHandler.FillPressureDesh(inputFilePath, 4, (1.2 * LoadPointDataModel.getInstance().LoadPoints[5].Pressure).ToString());
                datHandler.UpdateRPM2(inputFilePath, 1, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
                datHandler.FillMassFlow(inputFilePath, 18, LoadPointDataModel.getInstance().LoadPoints[5].MassFlow.ToString(), -1);
                datHandler.FillInletPressure(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[5].Pressure.ToString());
                datHandler.FillVariablePower(inputFilePath, 7, pow.ToString());
                datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[5].BackPress.ToString());
                datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[5].Temp.ToString());
                datHandler.MakeUpTemperature(inputFilePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(inputFilePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                //datHandler.ProcessMassFlow(inputFilePath, 9, turbineDataModel.OutletMassFlow.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(inputFilePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                else
                {
                    datHandler.UpdateTemplatePRVToWPRVInDumpCondensor(inputFilePath);
                }
                string content = File.ReadAllText(inputFilePath);
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                inputFilePath = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVLP5.DAT");
                datHandler.FillTurbineEff(inputFilePath, "7", eff);
                datHandler.fillProcessSteamTemperatur(inputFilePath, 16, turbineDataModel.PST.ToString());
                datHandler.FillPressureDesh(inputFilePath, 4, (1.2 * LoadPointDataModel.getInstance().LoadPoints[5].Pressure).ToString());
                datHandler.UpdateRPM2(inputFilePath, 1, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
                datHandler.FillMassFlow(inputFilePath, 18, LoadPointDataModel.getInstance().LoadPoints[5].MassFlow.ToString(), -1);
                datHandler.FillInletPressure(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[5].Pressure.ToString());
                datHandler.FillVariablePower(inputFilePath, 7, pow.ToString());
                datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[5].BackPress.ToString());
                datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[5].Temp.ToString());
                datHandler.MakeUpTemperature(inputFilePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(inputFilePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(inputFilePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                else
                {
                    datHandler.UpdateTemplatePRVToWPRV(inputFilePath);
                }
                string content = File.ReadAllText(inputFilePath);
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            inputFilePath = Path.Combine(AppContext.BaseDirectory, "KreislLp5D.DAT");

            datHandler.FillTurbineEff(inputFilePath, "1", eff);
            datHandler.fillProcessSteamTemperatur(inputFilePath, 3, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(inputFilePath, 8, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.UpdateRPM2(inputFilePath, 7, LoadPointDataModel.getInstance().LoadPoints[1].Rpm.ToString());
            datHandler.FillMassFlow(inputFilePath, 9, LoadPointDataModel.getInstance().LoadPoints[5].MassFlow.ToString(), -1);
            datHandler.FillVariablePower(inputFilePath, 7, pow.ToString()) ;
            datHandler.FillInletPressure(inputFilePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[5].BackPress.ToString());
            datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[5].Temp.ToString());
            string content = File.ReadAllText(inputFilePath);
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }
        else
        {
            inputFilePath = Path.Combine(AppContext.BaseDirectory, "kreislLp5.DAT");
            var replacements = new Dictionary<string, string>()
            {
            { "Eff", eff },
            { "EPres", FormatWithDotZero(LoadPointDataModel.getInstance().LoadPoints[5].BackPress) },
            { "Pre", FormatWithDotZero(TurbineDataModel.getInstance().InletPressure) },
            { "Temp","-"+FormatWithDotZero(LoadPointDataModel.getInstance().LoadPoints[5].Temp) },
            { "Flow", FormatWithDotZero(LoadPointDataModel.getInstance().LoadPoints[5].MassFlow)},
            { "RPM", FormatWithDotZero(LoadPointDataModel.getInstance().LoadPoints[1].Rpm)},
            {"Pow" , FormatWithDotZero(pow) }
            };
            string content = File.ReadAllText(inputFilePath);
            foreach (var kvp in replacements)
            {
                content = content.Replace(kvp.Key, kvp.Value);
            }
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }
            
        

    }
    public void UpdateGeneratorinKriesl()
    {
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        string[] datFile = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.DAT");
        for (int i = datFile.Length - 1; i >= 0; i--)
        {
            if (datFile[i].Contains("PN GENO       COS PHI"))
            {
                i++;
                string[] Points = datFile[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                kreislDATHandler.updateGeneratorSpecs(Convert.ToDouble(Points[0]), Convert.ToDouble(Points[2]), Convert.ToDouble(Points[3]), Convert.ToDouble(Points[4]), Convert.ToDouble(Points[5]));
                i--;
            }
            else if (datFile[i].Contains("PN GETR     PLL GETR"))
            {
                i++;
                string[] Points = datFile[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                kreislDATHandler.updateGearBoxPower(Convert.ToDouble(Points[0]));
                i--;
            }
            else if (datFile[i].Contains("!     PN TURB    NENNDREHZ   "))
            {
                i++;
                string[] Points = datFile[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                kreislDATHandler.updateTurbinePower(Convert.ToDouble(Points[0]));
                i--;
            }
        }
    }
    public double getPowerMargin()
    {
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Split the line by comma
                var values = line.Split(',');
                if (values[0] == "PowerMargin")
                {
                    return Convert.ToDouble(values[1]);
                }

            }
        }
        return 25;
    }

        void checkFinalBending(int maxlp =0)
            {
                

                string ending = "";
                bool bendingCheck = false;
                List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
        for (int lpNo = 1; lpNo < maxLoadPoints; ++lpNo)
        {
            Console.WriteLine("bending statusssssssssssssssssssssssssssss:" + lpList[0].Bending + ":");
            if (!string.IsNullOrEmpty(lpList[lpNo].Bending))
            {
                bendingCheck = true;
                // break;
            }
        }
        
            bool bendingStatus = !bendingCheck;
                //true;
                if (bendingCheck)
                {
                    Logger("Final Bending check Failed..");
                    turbaOutputModel.BendingCheck = "FALSE";
                }
                else
                {
                    Logger("Final Bending check Passed..");
                    turbaOutputModel.BendingCheck = "TRUE";
                }

                if (bendingStatus)
                {
                    DATFileProcessor dATFileProcessor = new DATFileProcessor();
                    double Na = dATFileProcessor.getCurrentNozzle(0);
                    double Nb = dATFileProcessor.getCurrentNozzle(1);
                    if (turbineDataModel.OldNa != 0 && (Na != turbineDataModel.OldNa || Nb != turbineDataModel.OldNb))
                    {
                        Logger("NozzleAdjusted");
                    }
                    TurbaAutomation turbaAutomation = new TurbaAutomation();
                    turbaAutomation.LaunchRsmin();
            
                    UpdateOutletTempAndEnth();
                    Logger("////////////// TURBINE IS GOOD To GO  //////////////");
                    Logger("BUILT IN EXECUTED FLOW PATH : " + turbineDataModel.TurbineStatus);
                    turbineDataModel.AK25 = turbaOutputModel.OutputDataList[1].Power_KW;
                    Logger("Output power (kW) : " + (turbaOutputModel.OutputDataList[1].Power_KW-25) + " Efficiency(%) : " + turbaOutputModel.OutputDataList[1].Efficiency.ToString());
                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 2)
                    {
                        int t = AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count;
                        getLoadPoint(10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count - 2);
                    }
                    else
                    {
                        getLoadPoint(10);
                    }
                    TurbineDesignPage.finalToken.Cancel();


                }
                else
                {

                turbineDataModel.AK25 = 0;

                CustomExecutedClass customExecutedClass = new CustomExecutedClass();
                customExecutedClass.Main_CustomFlowPathTest(maxlp);
                if (TurbineDesignPage.finalToken.IsCancellationRequested)
                {
                    return;
                }
                Logger("/\\/\\/\\/\\/\\/\\/\\/\\ TURBINE DESIGN FAILED /\\/\\/\\/\\/\\/\\/\\/\\");
                TurbineDesignPage.cts.Cancel();
            }
       

        }
    public void getLoadPoint(int loadPoint)
    {
        string[] lines = File.ReadAllLines(@"C:\testDir\Turman250\TURBATURBAE1.DAT.ERG");
        //string[] lines = File.ReadAllLines("yourfile.erg");
        List<double[]> pressuresList = new List<double[]>();
        List<double[]> tempsList = new List<double[]>();
        List<double[]> enthList = new List<double[]>();
        List<double[]> massList = new List<double[]>();
        var data = new LineSizeDataModel();
        int count = 1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("DRUECKE - bar - pressures"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    pressuresList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("TEMPERATUREN - grd C - temperatures"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    tempsList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("ENTHALPIEN - kJ/kg - enthalpies"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    enthList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("DAMPFMENGEN - kg/s - mass flow"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    massList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
        }

        double[,] pressures = new double[3, loadPoint];
        double[,] temps = new double[3, loadPoint];
        double[,] enthalpies = new double[3, loadPoint];
        double[,] massFlows = new double[3, loadPoint];

        for (int col = 0; col < loadPoint; col++)
        {
            pressures[0, col] = pressuresList[col][0]; // Inlet, Point col+1
            pressures[1, col] = pressuresList[col][1]; // Wheel
            pressures[2, col] = pressuresList[col][2]; // Exhaust

            temps[0, col] = tempsList[col][0];
            temps[1, col] = tempsList[col][1];
            temps[2, col] = tempsList[col][2];

            enthalpies[0, col] = enthList[col][0];
            enthalpies[1, col] = enthList[col][1];
            enthalpies[2, col] = enthList[col][2];

            massFlows[0, col] = massList[col][0];
            massFlows[1, col] = massList[col][1];
            massFlows[2, col] = massList[col][2];
        }
        double UpperLimit = 0;
        double LowerLimit = 0;
        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count == 0)
        {

            UpperLimit = Math.Min(1.1 * pressures[2, 0], pressures[2, 0] + 0.5);
            LowerLimit = Math.Max(0.9 * pressures[2, 0], pressures[2, 0] - 0.5);
        }
        else
        {
            UpperLimit = Math.Min(1.1 * pressures[2, 0], pressures[2, 0] + 0.5);
            LowerLimit = Math.Max(0.9 * pressures[2, 0], pressures[2, 0] - 0.5);
            for (int i = 10; i < loadPoint; i++)
            {
                double exhasutPressure = pressures[2, i];
                UpperLimit = Math.Min(UpperLimit, Math.Min(1.1 * pressures[2, i], pressures[2, i] + 0.5));
                LowerLimit = Math.Max(LowerLimit, Math.Max(0.9 * pressures[2, i], pressures[2, i] - 0.5));
            }


        }

        PrintPdf printPdf = new PrintPdf();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            int c = 3;
            int lp = 2;
            string firstLpname = "";
            int size = AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count;
            Console.WriteLine(size);
            double power = 0;
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count == 0)
            {
                firstLpname = turbineDataModel.LpName;
                power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
                turbineDataModel.AK25 = RoundToNearestTens(power);
            }
            else
            {
                firstLpname = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].LoadPoint;
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration) - 25;
                }
                else
                {
                    power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
                }
            }
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 1)
            {
                AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration = RoundToNearestTens(power);
            }
            turbineDataModel.AK25 = RoundToNearestTens(power);
            AddLP7InKriesl();


            if (turbineDataModel.DumpCondensor)
            {
                printPdf.GeneratePDFForClosedCycleDumpCondensor(pressures[0, 0], temps[0, 0], enthalpies[0, 0], massFlows[0, 0], pressures[2, 0], temps[2, 0], enthalpies[2, 0], massFlows[2, 0], power, "Output1.pdf", 1, loadPoint - 10 + 2, UpperLimit, LowerLimit, firstLpname, 1,data);
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                printPdf.GeneratePDFForClosedCycleWithOutDumpCondensor(pressures[0, 0], temps[0, 0], enthalpies[0, 0], massFlows[0, 0], pressures[2, 0], temps[2, 0], enthalpies[2, 0], massFlows[2, 0], power, "Output1.pdf", 1, loadPoint - 10 + 2, UpperLimit, LowerLimit, firstLpname, 1,data);
            }
            int lpno = 0;
            if (isLP5Change == true)
            {
                lpno = loadPoint + 2 - 10 + 1;
            }
            else
            {
                lpno = loadPoint + 2 - 10;
            }
            if (turbineDataModel.DumpCondensor)
            {
                printPdf.GeneratePDFForClosedCycleDumpCondensor(pressures[0, 6], temps[0, 6], enthalpies[0, 6], massFlows[0, 6], pressures[2, 0], temps[2, 6], enthalpies[2, 6], massFlows[2, 6], turbaOutputModel.OutputDataList[7].Power_KW - 25, "Output2.pdf", 2, loadPoint - 10 + 2, UpperLimit, LowerLimit, "MCR- Minimum Continuous Rating", lpno,data);
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                printPdf.GeneratePDFForClosedCycleWithOutDumpCondensor(pressures[0, 6], temps[0, 6], enthalpies[0, 6], massFlows[0, 6], pressures[2, 0], temps[2, 6], enthalpies[2, 6], massFlows[2, 6], turbaOutputModel.OutputDataList[7].Power_KW - 25, "Output2.pdf", 2, loadPoint - 10 + 2, UpperLimit, LowerLimit, "MCR- Minimum Continuous Rating", lpno,data);
            }
            if (loadPoint - 10 >= 1)
            {
                int tt = loadPoint;
                Console.WriteLine(tt);
                for (int i = 10; i < tt; i++)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration != 0) ? AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration : turbaOutputModel.OutputDataList[i + 1].Power_KW - 25;
                    if (turbineDataModel.DumpCondensor)
                    {
                        printPdf.GeneratePDFForClosedCycleDumpCondensor(pressures[0, i], temps[0, i], enthalpies[0, i], massFlows[0, i], pressures[2, i], temps[2, i], enthalpies[2, i], massFlows[2, i], power, ("Output" + (i + 3 - 10) + ".pdf"), c, loadPoint - 10 + 2, UpperLimit, LowerLimit, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].LoadPoint, lp,data);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        printPdf.GeneratePDFForClosedCycleWithOutDumpCondensor(pressures[0, i], temps[0, i], enthalpies[0, i], massFlows[0, i], pressures[2, i], temps[2, i], enthalpies[2, i], massFlows[2, i], power, ("Output" + (i + 3 - 10) + ".pdf"), c, loadPoint - 10 + 2, UpperLimit, LowerLimit, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].LoadPoint, lp,data);
                    }
                    c++;
                    lp++;
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration = RoundToNearestTens(power);
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamMass = massFlows[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamTemp = temps[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].ExhaustPressure = pressures[2, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamPressure = pressures[0, i];
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {

            int c = 3;
            int lp = 2;
            string firstLpname = "";
            double power = 0;
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count == 0)
            {
                firstLpname = turbineDataModel.LpName;
                power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
            }
            else
            {
                firstLpname = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].LoadPoint;
                
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration);
                }
                else
                {
                    power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
                }
            }
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 1)
            {
                AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration = RoundToNearestTens(power);
            }
            turbineDataModel.AK25 = RoundToNearestTens(power);
            AddLP7InKriesl();
            printPdf.GeneratePDFForDesuprator(pressures[0, 0], temps[0, 0], enthalpies[0, 0], massFlows[0, 0], pressures[2, 0], temps[2, 0], enthalpies[2, 0], massFlows[2, 0], power, "Output1.pdf", 1, loadPoint - 10 + 2, UpperLimit, LowerLimit, firstLpname, 1,data);
            int lpno = 0;
            if(isLP5Change == true)
            {
                lpno = loadPoint + 2 - 10 + 1;
            }
            else
            {
                lpno = loadPoint + 2 - 10;
            }
            printPdf.GeneratePDFForDesuprator(pressures[0, 6], temps[0, 6], enthalpies[0, 6], massFlows[0, 6], pressures[2, 0], temps[2, 6], enthalpies[2, 6], massFlows[2, 6], turbaOutputModel.OutputDataList[7].Power_KW - 25, "Output2.pdf", 2, loadPoint - 10 + 2, UpperLimit, LowerLimit, "MCR- Minimum Continuous Rating", lpno,data);
            if (loadPoint - 10 >= 1)
            {
                int tt = loadPoint;
                Console.WriteLine(tt);
                for (int i = 10; i < tt; i++)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration != 0) ? AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration : turbaOutputModel.OutputDataList[i + 1].Power_KW - 25;
                    printPdf.GeneratePDFForDesuprator(pressures[0, i], temps[0, i], enthalpies[0, i], massFlows[0, i], pressures[2, i], temps[2, i], enthalpies[2, i], massFlows[2, i], power, ("Output" + (i + 3 - 10) + ".pdf"), c, loadPoint - 10 + 2, UpperLimit, LowerLimit, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].LoadPoint, lp,data);
                    c++;
                    lp++;
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration = RoundToNearestTens(power);
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamMass = massFlows[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamTemp = temps[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].ExhaustPressure = pressures[2, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamPressure = pressures[0, i];
                }
            }
        }
        else
        {
            int c = 3;
            string firstLpname = "";
            int size = AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count;
            Console.WriteLine(size);
            double power = 0;
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count == 0)
            {
                firstLpname = turbineDataModel.LpName;
                power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
            }
            else
            {
                firstLpname = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].LoadPoint;
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration);
                }
                else
                {
                    power = (turbaOutputModel.OutputDataList[1].Power_KW - 25);
                }
            }
            if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 1)
            {
                AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration = RoundToNearestTens(power);
            }
            turbineDataModel.AK25 = RoundToNearestTens(power);
            printPdf.GeneratePDF(pressures[0, 0], temps[0, 0], enthalpies[0, 0], massFlows[0, 0], pressures[2, 0], temps[2, 0], enthalpies[2, 0], massFlows[2, 0], power, "Output1.pdf", 1, loadPoint - 10 + 2, UpperLimit, LowerLimit, firstLpname,data);
            printPdf.GeneratePDF(pressures[0, 6], temps[0, 6], enthalpies[0, 6], massFlows[0, 6], pressures[2, 0], temps[2, 6], enthalpies[2, 6], massFlows[2, 6], turbaOutputModel.OutputDataList[7].Power_KW - 25, "Output2.pdf", 2, loadPoint - 10 + 2, UpperLimit, LowerLimit, "MCR- Minimum Continuous Rating",data);
            if (loadPoint - 10 >= 1)
            {
                int tt = loadPoint;
                Console.WriteLine(tt);
                for (int i = 10; i < tt; i++)
                {
                    power = (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration != 0) ? AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration : turbaOutputModel.OutputDataList[i + 1].Power_KW - 25;
                    printPdf.GeneratePDF(pressures[0, i], temps[0, i], enthalpies[0, i], massFlows[0, i], pressures[2, i], temps[2, i], enthalpies[2, i], massFlows[2, i], power, ("Output" + (i + 3 - 10) + ".pdf"), c, loadPoint - 10 + 2, UpperLimit, LowerLimit, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].LoadPoint,data);
                    c++;
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].PowerGeneration = RoundToNearestTens(power);
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamMass = massFlows[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamTemp = temps[0, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].ExhaustPressure = pressures[2, i];
                    AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i - 10 + 2].SteamPressure = pressures[0, i];
                }
            }
        }
        ExportToFolder(loadPoint+1,data);


    }
    public double[,] getCordinate(double x, double y)
    {
        double[,] cordinate = new double[4, 2];

        // 1. X-34 , Y-24
        cordinate[0, 0] = x - 34;
        cordinate[0, 1] = y - 24;

        // 2. X+4 , Y-34
        cordinate[1, 0] = x + 4;
        cordinate[1, 1] = y - 24;

        // 3. X-34 , Y+4
        cordinate[2, 0] = x - 34;
        cordinate[2, 1] = y + 4;

        // 4. X+4 , Y+4
        cordinate[3, 0] = x + 4;
        cordinate[3, 1] = y + 4;

        return cordinate;
    }
    public void GeneratePDFForDesuprator(
        double pressure,
        double temperature,
        double enthalpy,
        double massFlow,
        double outletPres,
        double outletTemp,
        double outletEnth,
        double outletMassFlow,
        double power,
        string outputFile,
        double pageNo,
        double totalPage,
        double upperlimit,
        double lowerlimit,
        string LoadPointName,
        int loadPoint)
    {
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        double lkPressure = 1.01;
        double lkTemperature = (temperature + outletTemp) / 2;
        double lkMass = 0.2;
        double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
        string existingPdfPath = "Dinput.pdf";
        try
        {
            if (kreislDATHandler.checkDesuprator(StartKreisl.filePath, loadPoint) == -8)
            {
                existingPdfPath = "DWinput.pdf";
            }
            // Open existing PDF for modification
            PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
            PdfPage page = document.Pages[0];
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Arial", 7.91);
            double offsetX = page.MediaBox.X1;
            double offsetY = page.MediaBox.Y1;
            Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);

            // Draw turbine data values onto PDF at specified locations
            double[,] cordinates = getCordinate(251, 221);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 3, loadPoint) / 0.980665, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 3, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 3, loadPoint).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 3, loadPoint).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);



            gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(448, 264, 100, 100), XStringFormats.TopLeft);


            cordinates = getCordinate(432, 364);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 6, loadPoint) / 0.980665, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 6, loadPoint), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);


            cordinates = getCordinate(354, 419);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 5, loadPoint) / 0.980665, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 5, loadPoint), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

            if (kreislDATHandler.checkDesuprator(StartKreisl.filePath, loadPoint) == 8)
            {
                cordinates = getCordinate(432, 475);
                gfx.DrawString(Math.Round(1.2 * (turbineDataModel.InletPressure / 0.980552), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
                gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 10, loadPoint), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);
            }



            cordinates = getCordinate(354, 530);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractPressForDesuparator(StartKreisl.ergFilePath, 4, loadPoint) / 0.980552, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractEnthalphyForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round(kreislERGHandlerService.ExtractMassFlowForDesuparator(StartKreisl.ergFilePath, 4, loadPoint), 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);



            gfx.DrawString(turbineDataModel.ProjectName, font, XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString((Math.Round((upperlimit / 0.980665), 2).ToString("F1") + " ata").ToString(), new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(319, 656, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString((Math.Round((lowerlimit / 0.980665), 2)).ToString("F1") + " ata", new XFont("Arial", 7, XFontStyle.Bold), XBrushes.Black, new XRect(319, 667, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(328, 376, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(turbineDataModel.TSPID, font, XBrushes.Black, new XRect(446, 769, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(LoadPointName, font, XBrushes.Black, new XRect(391, 726, 160, 14), XStringFormats.Center);

            string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

            string domainName = "ad101";

            string userName = Environment.UserName;

            string fullName = "Siemens-User";


            double smallerFontSize = 7;// 6.5; // Adjust the size as needed

            // Create a new font with the smaller size

            XFont smallerFont = new XFont("Verdana", smallerFontSize);

            gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 695, 100, 100), XStringFormats.TopLeft);
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

            {

                UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                if (user != null)

                {

                    // Get the user's full name

                    fullName = user.DisplayName;

                    // Display the full name

                    Console.WriteLine("The current user's full name is: " + fullName);

                }

                else

                {

                    Console.WriteLine("User not found in the directory.");

                }

            }


            if (fullName.EndsWith("(ext)"))

            {

                fullName = fullName.Remove(fullName.Length - 6).Trim();

            }

            // Draw the string with the smaller font

            gfx.DrawString(fullName, smallerFont, XBrushes.Black, new XRect(337, 695, 100, 100), XStringFormats.TopLeft);

            document.Save(Path.Combine("C:\\testDir\\", outputFile));
            Console.WriteLine("PDF saved successfully at " + outputFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during PDF generation: " + ex.Message);
        }
    }
    public void AddLP7InKriesl()
    {
        KreislDATHandler datHandler = new KreislDATHandler();
        string inputFilePath1 = "";
        string inputFilePath = "";
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor)
            {
                inputFilePath1 = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVDDumpLP7.DAT");
                inputFilePath = Path.Combine(@"C:\testDir", "CloseCyclePRVLP7.DAT");
                File.Copy(inputFilePath1, inputFilePath, true);
                datHandler.FillTurbineEff(inputFilePath, "7", turbaOutputModel.OutputDataList[7].Efficiency.ToString());
                double pst = turbineDataModel.PST;
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(LoadPointDataModel.getInstance().LoadPoints[7].BackPress) + 5) : turbineDataModel.PST;

                datHandler.fillProcessSteamTemperatur(inputFilePath, 16, turbineDataModel.PST.ToString());
                if (pst == 0)
                {
                    turbineDataModel.PST = 0;
                }
                datHandler.FillPressureDesh(inputFilePath, 4, (1.2 * LoadPointDataModel.getInstance().LoadPoints[7].Pressure).ToString());
                datHandler.UpdateRPM2(inputFilePath, 1, LoadPointDataModel.getInstance().LoadPoints[7].Rpm.ToString());
                datHandler.FillInletPressure(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Pressure.ToString());
                //datHandler.FillVariablePower(inputFilePath, 7, pow.ToString());
                datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[7].BackPress.ToString());
                datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Temp.ToString());
                datHandler.MakeUpTemperature(inputFilePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(inputFilePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(inputFilePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                else
                {
                    datHandler.UpdateTemplatePRVToWPRVInDumpCondensor(inputFilePath);
                }
                
                if (turbineDataModel.MassFlowRate > 0)
                {
                    datHandler.FillMassFlow(inputFilePath, 19, LoadPointDataModel.getInstance().LoadPoints[7].MassFlow.ToString());
                }
                string content = File.ReadAllText(inputFilePath);
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                inputFilePath1 = Path.Combine(AppContext.BaseDirectory, "CloseCyclePRVLP7.DAT");
                inputFilePath = Path.Combine(@"C:\testDir", "CloseCyclePRVLP7.DAT");
                File.Copy(inputFilePath1, inputFilePath, true);
                datHandler.FillTurbineEff(inputFilePath, "7", turbaOutputModel.OutputDataList[7].Efficiency.ToString());
                datHandler.fillProcessSteamTemperatur(inputFilePath, 16, turbineDataModel.PST.ToString());
                datHandler.FillPressureDesh(inputFilePath, 4, (1.2 * LoadPointDataModel.getInstance().LoadPoints[7].Pressure).ToString());
                datHandler.UpdateRPM2(inputFilePath, 1, LoadPointDataModel.getInstance().LoadPoints[7].Rpm.ToString());
                datHandler.FillMassFlow(inputFilePath, 18, LoadPointDataModel.getInstance().LoadPoints[7].MassFlow.ToString());
                datHandler.FillInletPressure(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Pressure.ToString());
                //datHandler.FillVariablePower(inputFilePath, 7, pow.ToString());
                datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[7].BackPress.ToString());
                datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Temp.ToString());
                datHandler.MakeUpTemperature(inputFilePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(inputFilePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(inputFilePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(inputFilePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                else
                {
                    datHandler.UpdateTemplatePRVToWPRV(inputFilePath);
                }
                string content = File.ReadAllText(inputFilePath);
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            inputFilePath1 = Path.Combine(AppContext.BaseDirectory, "KreislLp7.DAT");
            inputFilePath = Path.Combine(@"C:\testDir", "KreislLp7.DAT");
            File.Copy(inputFilePath1, inputFilePath, true);

            datHandler.FillTurbineEff(inputFilePath, "1", turbaOutputModel.OutputDataList[7].Efficiency.ToString());
            datHandler.fillProcessSteamTemperatur(inputFilePath, 3, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(inputFilePath, 8, (1.2 * LoadPointDataModel.getInstance().LoadPoints[7].Pressure).ToString());
            datHandler.UpdateRPM2(inputFilePath, 7, LoadPointDataModel.getInstance().LoadPoints[7].Rpm.ToString());
            datHandler.FillMassFlow(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].MassFlow.ToString());
            datHandler.FillInletPressure(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Pressure.ToString());
            datHandler.FillExhaustPressure(inputFilePath, 2, LoadPointDataModel.getInstance().LoadPoints[7].BackPress.ToString());
            datHandler.FillInletTemperature(inputFilePath, 6, LoadPointDataModel.getInstance().LoadPoints[7].Temp.ToString());
            datHandler.UpdateDesupratorFirst(inputFilePath, 1);
            datHandler.UpdateDesupratorSecond(inputFilePath, 1);
            //datHandler.UpdateLpNo(inputFilePath, 7);

            string content = File.ReadAllText(inputFilePath);

            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
        }
        if (File.Exists(inputFilePath))
        {
            File.Delete(inputFilePath);
        }
        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
    }
    public static void MergePDFs(IEnumerable<string> pdfPaths, string outputMergedPdf)
    {
        try
        {
            PdfDocument outputDocument = new PdfDocument();

            foreach (var pdfPath in pdfPaths)
            {
                PdfDocument inputDocument = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);

                // Copy all pages from this document
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
            }

            outputDocument.Save(outputMergedPdf);
            Console.WriteLine($"Merged PDF saved successfully at {outputMergedPdf}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error merging PDFs: " + ex.Message);
        }
    }
    public void ExportToFolder(int loadPoint , LineSizeDataModel data)
    {
        string sourceDirectory = @"C:\testDir";
        string currDate = DateTime.Now.ToString("yyyyMMdd"); // Format date as yyyy-MM-dd
        string currTime = DateTime.Now.ToString("HHmmss"); // Format time as HH-mm-ss
        string destinationDirectory = $@"C:\testdir\turbine_files_{currDate}-{currTime}";
        string curr = AppContext.BaseDirectory;
        string destFile1 = Path.Combine(destinationDirectory, "output1.pdf");
        string file1 = @"C:\testDir\Output1.pdf";
        string logPath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");
        try
        {
            if (Directory.Exists(sourceDirectory))
            {
                string[] files = Directory.GetFiles(sourceDirectory);
                if (files.Length > 0)
                {
                    Directory.CreateDirectory(destinationDirectory);
                    TurbineDesignPage.outputPath = destinationDirectory;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName == "TURBAE1.BSP" || fileName == "TURBAE1.CAD"
                        || fileName == "TURBAE1.DAT" || fileName == "TURBAE1.ERG"
                        || fileName == "TURBAE1.PUN")
                        {
                            string destFile = Path.Combine(destinationDirectory, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                            Console.WriteLine($"Copied: {fileName} to {destFile}");
                        }
                    }
                }
            }
            List<string> pdfFiles = new List<string>()
                {
                     @"C:\testDir\Output1.pdf",
                };


            //File.Copy(file1, destFile1, true);

            for (int i = 10; i < loadPoint; i++)
            {
                file1 = Path.Combine(@"C:\testDir\", "Output" + (i + 2 - 10) + ".pdf");
                //destFile1 =  Path.Combine(destinationDirectory, "Output" + (i + 2 - 10) + ".pdf");
                //File.Copy(file1, destFile1, true);
                pdfFiles.Add(file1);
            }
            MergePDFs(pdfFiles, Path.Combine(destinationDirectory, "output.pdf"));

            string destFile2 = Path.Combine(destinationDirectory, Path.GetFileName(logPath));
            File.Copy(logPath, destFile2, true);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(Path.Combine(destinationDirectory, "linesize.json"), json);

            string[] batchFiles = new string[]
            {
                "TURBATURBAE1.DAT.DAT",
                "TURBATURBAE1.DAT.ERG",
                //"TURBAE1.BSP",
                //"TURBAE1.CAD",
                "TURBAE1.PUN"
            };

            foreach (string batchFile in batchFiles)
            {
                string sourceFile = Path.Combine(sourceDirectory, batchFile);
                string destFile = Path.Combine(destinationDirectory, batchFile);
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destFile, true);
                }
            }

            string turman250SourceDir = Path.Combine(sourceDirectory, "Turman250");
            string turman250DestDir = Path.Combine(destinationDirectory, "Turman250");
            if (Directory.Exists(turman250SourceDir))
            {
                CopyDirectory(turman250SourceDir, turman250DestDir);
            }

            string datFile = Path.Combine(destinationDirectory, "TURBATURBAE1.DAT.DAT");
            if (File.Exists(datFile))
            {
                File.Move(datFile, Path.Combine(destinationDirectory, "TURBAE1.DAT"));
            }

            string ergFile = Path.Combine(destinationDirectory, "TURBATURBAE1.DAT.ERG");
            if (File.Exists(ergFile))
            {
                File.Move(ergFile, Path.Combine(destinationDirectory, "TURBAE1.ERG"));
            }
            //string turman250DestDir1 = Path.Combine(destinationDirectory, "Turman250");
            File.Copy(Path.Combine(turman250DestDir, "TURBAE1.BSP"), Path.Combine(destinationDirectory, "TURBAE1.BSP"), true);
            File.Copy(Path.Combine(turman250DestDir, "TURBAE1.CAD"), Path.Combine(destinationDirectory, "TURBAE1.CAD"), true);
            File.Copy(Path.Combine(destinationDirectory, "TURBAE1.DAT"), Path.Combine(turman250DestDir, "TURBAE1.DAT"), true);
            File.Copy(Path.Combine(destinationDirectory, "TURBAE1.PUN"), Path.Combine(turman250DestDir, "TURBAE1.PUN"), true);


        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

    }
    public void GeneratePDF(
       double pressure,
       double temperature,
       double enthalpy,
       double massFlow,
       double outletPres,
       double outletTemp,
       double outletEnth,
       double outletMassFlow,
       double power,
       string outputFile,
       double pageNo,
       double totalPage,
       double upperlimit,
       double lowerlimit,
       string LoadPointName)
    {

        double lkPressure = 1.01;
        double lkTemperature = (temperature + outletTemp) / 2;
        double lkMass = 0.2;
        double lkEnthalpy = thermodynamicService.getOutletEnthalpy(outletPres, enthalpy, turbineDataModel.TurbineEfficiency, pressure);
        string existingPdfPath = "input.pdf";
        try
        {
            // Open existing PDF for modification
            PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
            PdfPage page = document.Pages[0];
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Arial", 7.91);
            double offsetX = page.MediaBox.X1;
            double offsetY = page.MediaBox.Y1;
            Console.WriteLine("cjhvbevh-->" + offsetX + offsetY);

            // Draw turbine data values onto PDF at specified locations
            double[,] cordinates = getCordinate(251, 221);
            // Draw turbine data values onto PDF at specified locations
            gfx.DrawString(Math.Round(pressure / 0.980665, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(enthalpy, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(temperature.ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round(massFlow * 3.60, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);


            gfx.DrawString(RoundToNearestTens(power).ToString() + " kW", new XFont("Arial", 10.28, XFontStyle.Bold), XBrushes.Black, new XRect(448, 264, 100, 100), XStringFormats.TopLeft);


            cordinates = getCordinate(432, 364);

            gfx.DrawString(Math.Round(lkPressure, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(lkEnthalpy, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(Math.Round(lkTemperature, 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round(lkMass, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

            cordinates = getCordinate(354, 467);
            gfx.DrawString(Math.Round(outletPres / 0.980665, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[0, 0], cordinates[0, 1], 30, 20), XStringFormats.BottomRight);
            gfx.DrawString(Math.Round(outletEnth, 2).ToString("F2"), font, XBrushes.Blue, new XRect(cordinates[1, 0], cordinates[1, 1], 30, 20), XStringFormats.BottomLeft);
            gfx.DrawString(Math.Round(outletTemp, 2).ToString("F1"), font, XBrushes.Blue, new XRect(cordinates[2, 0], cordinates[2, 1], 30, 20), XStringFormats.TopRight);
            gfx.DrawString(Math.Round((outletMassFlow * 3.60) - 0.2, 2).ToString("F3"), font, XBrushes.Blue, new XRect(cordinates[3, 0], cordinates[3, 1], 30, 20), XStringFormats.TopLeft);

            gfx.DrawString(turbineDataModel.ProjectName, font, XBrushes.Black, new XRect(440, 748, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(pageNo.ToString(), font, XBrushes.Black, new XRect(574, 770, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(totalPage.ToString(), font, XBrushes.Black, new XRect(574, 721, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString((Math.Round((upperlimit / 0.980665), 2).ToString("F1") + " ata").ToString(), font, XBrushes.Black, new XRect(332, 663, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString((Math.Round((lowerlimit / 0.980665), 2)).ToString("F1") + " ata", font, XBrushes.Black, new XRect(332, 677, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString("STG".ToString(), font, XBrushes.Black, new XRect(337, 730, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(turbineDataModel.TSPID, font, XBrushes.Black, new XRect(440, 776, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(LoadPointName, font, XBrushes.Black, new XRect(500, 730, 100, 100), XStringFormats.TopLeft);

            string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

            string domainName = "ad101";

            string userName = Environment.UserName;

            string fullName = "Siemens-User";


            double smallerFontSize = 7;// 6.5; // Adjust the size as needed

            // Create a new font with the smaller size

            XFont smallerFont = new XFont("Verdana", smallerFontSize);

            gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(217, 695, 100, 100), XStringFormats.TopLeft);
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

            {

                UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

                if (user != null)

                {

                    // Get the user's full name

                    fullName = user.DisplayName;

                    // Display the full name

                    Console.WriteLine("The current user's full name is: " + fullName);

                }

                else

                {

                    Console.WriteLine("User not found in the directory.");

                }

            }


            if (fullName.EndsWith("(ext)"))

            {

                fullName = fullName.Remove(fullName.Length - 6).Trim();

            }

            // Draw the string with the smaller font

            gfx.DrawString(fullName, smallerFont, XBrushes.Black, new XRect(337, 695, 100, 100), XStringFormats.TopLeft);

            document.Save(Path.Combine("C:\\testDir\\", outputFile));
            Console.WriteLine("PDF saved successfully at " + outputFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during PDF generation: " + ex.Message);
        }
    }
    public void UpdateOutletTempAndEnth()
    {
        string[] fileLines = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.ERG");
        for (int i = 0; i < fileLines.Length; i++)
        {
            if (fileLines[i].Contains("TEMPERATUREN - grd C - temperatures"))
            {
                i += 2;
                string[] Points = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                turbineDataModel.OutletTemperature = Convert.ToDouble(Points[2]);
            }
            else if (fileLines[i].Contains("ENTHALPIEN - kJ/kg - enthalpies"))
            {
                i += 2;
                string[] Points = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                turbineDataModel.OutletEnthalphy = Convert.ToDouble(Points[2]);
            }
        }
    }
    void Logger(string message)
        {
            logger.LogInformation(message);
            // Console.WriteLine(message);
        }

        void HBDsetDefaultCustomerParamas()
        {
            // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            // hBDPowerCalculator.HBDSetDefaultCustomerParams();
            // use from HMBD_Interface.cs
            ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
            execHMBDConfiguration.HBDSetDefaultCustomerParams();            // HMBD_Interface ik = new HMBD_Interface();
            // ik.HBDsetDefaultCustomerParamas();
        }
        void HBDupdateEff(double efficiency)
        {
          ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
          execHMBDConfiguration.HBDUpdateEff(efficiency);
            // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
            // hBDPowerCalculator.HBDUpdateEff(efficiency);
            /* Implementation */
            //use from HMBD_Interface.cs
        }
    public int RoundToNearestTens(double number)
    {
        return (int)Math.Floor(number / 10.0) * 10;
    }
    public void exportToPDFandTurbineFiles(string sheetNameToPrint)
    {

        FileInfo existingFile = new FileInfo(excelPath);



        double pressure = Math.Round(turbineDataModel.InletPressure / 0.980665, 2);

        double temperature = Math.Round(turbineDataModel.InletTemperature, 2);

        double flow = Math.Round(turbineDataModel.MassFlowRate * 3.600, 2);

        double h1 = Math.Round(turbineDataModel.InletEnthalphy, 2);

        double pressure2 = Math.Round(turbineDataModel.ExhaustPressure / 0.980665, 2);

        double temperature2 = Math.Round(turbineDataModel.OutletTemperature, 2);

        double flow2 = Math.Round(turbineDataModel.OutletMassFlow * 3.600, 2);

        double h2 = Math.Round(turbineDataModel.OutletEnthalphy, 2);

        double lpressure = Math.Round(turbineDataModel.LeakagePressure, 2);

        double ltemperature = Math.Round(turbineDataModel.LeakageTemperature, 2);

        double lFlow = Math.Round(turbineDataModel.LeakageMassFlow, 2);

        double lh = Math.Round(turbineDataModel.LeakageEnthalphy, 2);

        double power = Math.Round(turbineDataModel.AK25-25, 0);

        String existingPdfPath = "input.pdf";

        String pdfFilePath = "output.pdf";

        PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);

        // // Access the first page

        PdfPage page = document.Pages[0];

        // // Get an XGraphics object for drawing on the page

        XGraphics gfx = XGraphics.FromPdfPage(page);

        XFont font = new XFont("Verdana", 11);

        gfx.DrawString(pressure.ToString(), font, XBrushes.Black,

            new XRect(179, 171, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(h1.ToString(), font, XBrushes.Black,

            new XRect(240, 171, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString($"{temperature:F2}", font, XBrushes.Black,

            new XRect(179, 195, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(flow.ToString(), font, XBrushes.Black,

            new XRect(240, 195, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(RoundToNearestTens(power).ToString() + " KW", font, XBrushes.Black,

            new XRect(460, 220, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(lpressure.ToString(), font, XBrushes.Black,

            new XRect(368, 291, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(lh.ToString(), font, XBrushes.Black,

            new XRect(425, 291, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(ltemperature.ToString(), font, XBrushes.Black,

           new XRect(368, 313, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(lFlow.ToString(), font, XBrushes.Black,

           new XRect(425, 313, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(pressure2.ToString(), font, XBrushes.Black,

           new XRect(300, 354, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(h2.ToString(), font, XBrushes.Black,

           new XRect(360, 354, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(temperature2.ToString(), font, XBrushes.Black,

           new XRect(300, 378, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(flow2.ToString(), font, XBrushes.Black,

           new XRect(360, 378, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(Math.Round((1.1 * pressure2), 2).ToString() + " ata", font, XBrushes.Black,

           new XRect(273, 514, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(Math.Round((0.9 * pressure2), 2).ToString() + " ata", font, XBrushes.Black,

          new XRect(273, 531, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(RoundToNearestTens(power).ToString() + " KW", font, XBrushes.Black,

          new XRect(410, 647, 100, 100), XStringFormats.TopLeft);

        gfx.DrawString(turbineDataModel.ProjectName, font, XBrushes.Black,

          new XRect(410, 670, 100, 100), XStringFormats.TopLeft);

        string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd

        string domainName = "ad101";

        string userName = Environment.UserName;

        string fullName = "Siemens-User";

        using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))

        {

            UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

            if (user != null)

            {

                // Get the user's full name

                fullName = user.DisplayName;

                // Display the full name

                Console.WriteLine("The current user's full name is: " + fullName);

            }

            else

            {

                Console.WriteLine("User not found in the directory.");

            }

        }


        //gfx.DrawString(fullName, font, XBrushes.Black, new XRect(291, 616, 100, 100), XStringFormats.TopLeft);

        double smallerFontSize = 7;// 6.5; // Adjust the size as needed

        // Create a new font with the smaller size

        XFont smallerFont = new XFont("Verdana", smallerFontSize);

        gfx.DrawString(currDate1, smallerFont, XBrushes.Black, new XRect(163, 613, 100, 100), XStringFormats.TopLeft);

        if (fullName.EndsWith("(ext)"))

        {

            fullName = fullName.Remove(fullName.Length - 6).Trim();

        }

        // Draw the string with the smaller font

        gfx.DrawString(fullName, smallerFont, XBrushes.Black, new XRect(280, 614, 100, 100), XStringFormats.TopLeft);


        document.Save(pdfFilePath);


        string sourceDirectory = @"C:\testDir";
        string currDate = DateTime.Now.ToString("yyyyMMdd"); // Format date as yyyy-MM-dd
        string currTime = DateTime.Now.ToString("HHmmss"); // Format time as HH-mm-ss
        string destinationDirectory = $@"C:\testdir\turbine_files_{currDate}-{currTime}";
        string curr = AppContext.BaseDirectory;
        string destFile1 = Path.Combine(destinationDirectory, "output.pdf");
        string file1 = curr + "\\output.pdf";
        string logPath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");
        try
        {
            if (Directory.Exists(sourceDirectory))
            {
                string[] files = Directory.GetFiles(sourceDirectory);
                if (files.Length > 0)
                {
                    Directory.CreateDirectory(destinationDirectory);
                     TurbineDesignPage.outputPath = destinationDirectory;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName == "TURBAE1.BSP" || fileName == "TURBAE1.CAD"
                        || fileName == "TURBAE1.DAT" || fileName == "TURBAE1.ERG"
                        || fileName == "TURBAE1.PUN")
                        {
                            string destFile = Path.Combine(destinationDirectory, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                            Console.WriteLine($"Copied: {fileName} to {destFile}");
                        }
                    }
                }
            }

            File.Copy(file1, destFile1, true);
            string destFile2 = Path.Combine(destinationDirectory, Path.GetFileName(logPath));
            File.Copy(logPath, destFile2, true);
            //string turbineFilesDir = Path.Combine(sourceDirectory, "turbine_files");
            //if (Directory.Exists(turbineFilesDir))
            //{
            //    Directory.Delete(turbineFilesDir, true);
            //}
            //Directory.CreateDirectory(turbineFilesDir);

            string[] batchFiles = new string[]
            {
    "TURBATURBAE1.DAT.DAT",
    "TURBATURBAE1.DAT.ERG",
    //"TURBAE1.BSP",
    //"TURBAE1.CAD",
    "TURBAE1.PUN"
            };

            foreach (string batchFile in batchFiles)
            {
                string sourceFile = Path.Combine(sourceDirectory, batchFile);
                string destFile = Path.Combine(destinationDirectory, batchFile);
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destFile, true);
                }
            }

            string turman250SourceDir = Path.Combine(sourceDirectory, "Turman250");
            string turman250DestDir = Path.Combine(destinationDirectory, "Turman250");
            if (Directory.Exists(turman250SourceDir))
            {
                CopyDirectory(turman250SourceDir, turman250DestDir);
            }

            string datFile = Path.Combine(destinationDirectory, "TURBATURBAE1.DAT.DAT");
            if (File.Exists(datFile))
            {
                File.Move(datFile, Path.Combine(destinationDirectory, "TURBAE1.DAT"));
            }

            string ergFile = Path.Combine(destinationDirectory, "TURBATURBAE1.DAT.ERG");
            if (File.Exists(ergFile))
            {
                File.Move(ergFile, Path.Combine(destinationDirectory, "TURBAE1.ERG"));
            }
            //string turman250DestDir1 = Path.Combine(destinationDirectory, "Turman250");
            File.Copy(Path.Combine(turman250DestDir, "TURBAE1.BSP"),Path.Combine(destinationDirectory, "TURBAE1.BSP"), true);
            File.Copy(Path.Combine(turman250DestDir, "TURBAE1.CAD"), Path.Combine(destinationDirectory, "TURBAE1.CAD"), true);
            File.Copy(Path.Combine(destinationDirectory, "TURBAE1.DAT"), Path.Combine(turman250DestDir,"TURBAE1.DAT"),true);
            File.Copy(Path.Combine(destinationDirectory, "TURBAE1.PUN"), Path.Combine(turman250DestDir, "TURBAE1.PUN"), true);

        }


        //string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        //string newTurbineFilesDir = Path.Combine(sourceDirectory, $"turbine_files_{timestamp}");
        //Directory.Move(turbineFilesDir, newTurbineFilesDir);

        //File.Copy(logPath, destFile1, true);

        // string sourceDirectory = @"C:\testDir";

        // string currDate = DateTime.Now.ToString("yyyyMMdd");

        // string currTime = DateTime.Now.ToString("HHmmss"); // Format time as HH-mm-ss

        // string destinationDirectory = @"C:\testDir\turbine_files";// _{currDate}-{currTime}";

        // string destinationDirectoryFinal = $@"C:\testDir\turbine_files_{currDate}-{currTime}";

        // string curr = AppContext.BaseDirectory;

        // string destFile1 = Path.Combine(destinationDirectory, "output.pdf");

        // string file1 = curr + "\\output.pdf";

        // string logPath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");

        // try

        // {

        //     if (Directory.Exists(sourceDirectory))

        //     {

        //         string[] files = Directory.GetFiles(sourceDirectory);

        //         if (files.Length > 0)

        //         {

        //             Directory.CreateDirectory(destinationDirectory);

        //             // TurbineDesignPage.outputPath = destinationDirectoryFinal;

        //             foreach (string file in files)

        //             {

        //                 string fileName = Path.GetFileName(file);

        //                 if (fileName == "TURBAE1.BSP" || fileName == "TURBAE1.CAD"

        //                 || fileName == "TURBAE1.DAT" || fileName == "TURBAE1.ERG"

        //                 || fileName == "TURBAE1.PUN")

        //                 {

        //                     string destFile = Path.Combine(destinationDirectory, Path.GetFileName(file));

        //                     File.Copy(file, destFile, true);

        //                     Console.WriteLine($"Copied: {fileName} to {destFile}");

        //                 }

        //             }

        //         }

        //     }
        //     File.Copy(file1, destFile1, true);
        //     string destFile2 = Path.Combine(destinationDirectory, Path.GetFileName(logPath));
        //     File.Copy(logPath, destFile2, true);
        //     Directory.Move(destinationDirectory, destinationDirectoryFinal);
        // }
        catch (Exception ex)
        {
            logger.LogError("exportToPDFandTurbineFiles", ex.Message);
        }
    }


    public void exportToPDFandTurbineFiles1(string sheetNameToPrint)
    {

        // string sheetNameToPrint = "OPEN_HBD";
        FileInfo existingFile = new FileInfo(excelPath);
        ExcelPackage package = new ExcelPackage(existingFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetNameToPrint];
        double pressure = Math.Round(turbineDataModel.InletPressure / 0.980665, 2);//Math.Round(worksheet.Cells["C6"].GetValue<double>(),2);
        double temperature = Math.Round(turbineDataModel.InletTemperature, 2);//Math.Round(worksheet.Cells["C7"].GetValue<double>(), 2);
        double flow = Math.Round(turbineDataModel.MassFlowRate * 3.600, 2);//Math.Round(worksheet.Cells["D7"].GetValue<double>(), 2);
        double h1 = Math.Round(turbineDataModel.InletEnthalphy, 2);//Math.Round(worksheet.Cells["D6"].GetValue<double>(), 2);
        double pressure2 = Math.Round(turbineDataModel.ExhaustPressure / 0.980665, 2);//Math.Round(worksheet.Cells["E17"].GetValue<double>(), 2);
        double temperature2 = Math.Round(turbineDataModel.OutletTemperature, 2);//Math.Round(worksheet.Cells["E18"].GetValue<double>(), 2);
        double flow2 = Math.Round(turbineDataModel.OutletMassFlow * 3.600, 2);//Math.Round(worksheet.Cells["F18"].GetValue<double>(), 2);
        double h2 = Math.Round(turbineDataModel.OutletEnthalphy, 2);//Math.Round(worksheet.Cells["F17"].GetValue<double>(), 2);
        double lpressure = Math.Round(turbineDataModel.LeakagePressure, 2);//Math.Round(worksheet.Cells["F13"].GetValue<double>(), 2);
        double ltemperature = Math.Round(turbineDataModel.LeakageTemperature, 2);//Math.Round(worksheet.Cells["F14"].GetValue<double>(), 2);
        double lFlow = Math.Round(turbineDataModel.LeakageMassFlow, 2);//Math.Round(worksheet.Cells["G14"].GetValue<double>(),2);
        double lh = Math.Round(turbineDataModel.LeakageEnthalphy, 2);//Math.Round(worksheet.Cells["G13"].GetValue<double>(), 2);
        double power = Math.Round(turbineDataModel.AK25, 2);//Math.Round(worksheet.Cells["AK25"].GetValue<double>(), 2);
                                                            // Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        String existingPdfPath = "input.pdf";
        String pdfFilePath = "output.pdf";
        PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
        // // Access the first page
        PdfPage page = document.Pages[0];
        // // Get an XGraphics object for drawing on the page
        XGraphics gfx = XGraphics.FromPdfPage(page);
        // // Create a regular font (16pt Verdana)
        // double leakageMassFlow = 1.03;
        // double outMassFlow = massFlowRate - leakageMassFlow;
        XFont font = new XFont("Verdana", 11);
        gfx.DrawString(pressure.ToString(), font, XBrushes.Black,
            new XRect(179, 171, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(h1.ToString(), font, XBrushes.Black,
            new XRect(240, 171, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(temperature.ToString(), font, XBrushes.Black,
            new XRect(179, 195, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(flow.ToString(), font, XBrushes.Black,
            new XRect(240, 195, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(power.ToString(), font, XBrushes.Black,
            new XRect(460, 220, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(lpressure.ToString(), font, XBrushes.Black,
            new XRect(368, 291, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(lh.ToString(), font, XBrushes.Black,
            new XRect(425, 291, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(ltemperature.ToString(), font, XBrushes.Black,
           new XRect(368, 313, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(lFlow.ToString(), font, XBrushes.Black,
           new XRect(425, 313, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(pressure2.ToString(), font, XBrushes.Black,
           new XRect(300, 354, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(h2.ToString(), font, XBrushes.Black,
           new XRect(360, 354, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(temperature2.ToString(), font, XBrushes.Black,
           new XRect(300, 378, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(flow2.ToString(), font, XBrushes.Black,
           new XRect(360, 378, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(Math.Round((1.1 * pressure2), 2).ToString(), font, XBrushes.Black,
           new XRect(273, 514, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(Math.Round((0.9 * pressure2), 2).ToString(), font, XBrushes.Black,
          new XRect(273, 531, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(power.ToString(), font, XBrushes.Black,
          new XRect(410, 647, 100, 100), XStringFormats.TopLeft);
        gfx.DrawString(turbineDataModel.ProjectName, font, XBrushes.Black,
          new XRect(410, 665, 100, 100), XStringFormats.TopLeft);
        string currDate1 = DateTime.Now.ToString("dd-MM-yyyy"); // Format date as yyyy-MM-dd
        string domainName = "ad101";
        string userName = Environment.UserName;
        string fullName = "Siemens-User";
        // Get the current domain contextContextType.Domain, domainName
        using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))
        {
            // Find the user in the directory
            UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);

            if (user != null)
            {
                // Get the user's full name
                fullName = user.DisplayName;

                // Display the full name
                Console.WriteLine("The current user's full name is: " + fullName);
            }
            else
            {
                Console.WriteLine("User not found in the directory.");
            }
        }


        gfx.DrawString(currDate1, font, XBrushes.Black, new XRect(163, 611, 100, 100), XStringFormats.TopLeft);
        //gfx.DrawString(fullName, font, XBrushes.Black, new XRect(291, 616, 100, 100), XStringFormats.TopLeft);
        double smallerFontSize = 6.5; // Adjust the size as needed

        // Create a new font with the smaller size
        XFont smallerFont = new XFont("Arial", smallerFontSize);

        // Draw the string with the smaller font
        gfx.DrawString(fullName, smallerFont, XBrushes.Black, new XRect(280, 614, 100, 100), XStringFormats.TopLeft);


        document.Save(pdfFilePath);

        string sourceDirectory = @"C:\testDir";
        string currDate = DateTime.Now.ToString("yyyyMMdd");
        string currTime = DateTime.Now.ToString("HHmmss"); // Format time as HH-mm-ss
        string destinationDirectory = @"C:\testDir\turbine_files";// _{currDate}-{currTime}";
        string destinationDirectoryFinal = $@"C:\testDir\turbine_files_{currDate}-{currTime}";
        string curr = AppContext.BaseDirectory;
        string destFile1 = Path.Combine(destinationDirectory, "output.pdf");
        string file1 = curr + "\\output.pdf";
        string logPath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");

        try
        {
            if (Directory.Exists(sourceDirectory))
            {
                string[] files = Directory.GetFiles(sourceDirectory);
                if (files.Length > 0)
                {
                    Directory.CreateDirectory(destinationDirectory);
                    TurbineDesignPage.outputPath = destinationDirectoryFinal;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName == "TURBAE1.BSP" || fileName == "TURBAE1.CAD"
                        || fileName == "TURBAE1.DAT" || fileName == "TURBAE1.ERG"
                        || fileName == "TURBAE1.PUN")
                        {
                            string destFile = Path.Combine(destinationDirectory, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                            Console.WriteLine($"Copied: {fileName} to {destFile}");
                        }
                    }
                }
            }

            File.Copy(file1, destFile1, true);
            string destFile2 = Path.Combine(destinationDirectory, Path.GetFileName(logPath));
            File.Copy(logPath, destFile2, true);
            Directory.Move(destinationDirectory, destinationDirectoryFinal);
            //Logger("OVERRR");
            //Directory.CreateDirectory(destinationDirectoryFinal)
            //if (Directory.Exists(sourceDirectory))
            //{
            //    // Ensure the destination directory does not already exist
            //    if (!Directory.Exists(destinationDirectory))
            //    {
            //        // Rename the directory by moving it to the new path
            //        Directory.Move(sourceDirectory, destinationDirectory);
            //        Console.WriteLine($"Directory renamed successfully from '{sourceDirectory}' to '{destinationDirectory}'.");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"The destination directory '{destinationDirectory}' already exists.");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"The source directory '{sourceDirectory}' does not exist.");
            //}
            //copyDirectory(destinationDirectory, destinationDirectoryFinal);
            //File.Copy(logPath, destFile1, true);

        }

        catch (Exception ex)
        {
            logger.LogError("exportToPDFandTurbineFiles", ex.Message);
        }
    }

    public void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
            CopyDirectory(directory, destDirectory);
        }
    }

    public void exportToPDFandTurbineFilesOld(string sheetNameToPrint)
        {
            // string sheetNameToPrint = "OPEN_HBD";
            // FileInfo existingFile = new FileInfo(excelPath);
            // ExcelPackage package = new ExcelPackage(existingFile);
            // ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetNameToPrint];
            double pressure = Math.Round(turbineDataModel.InletPressure, 2);//Math.Round(worksheet.Cells["C6"].GetValue<double>(),2);
            double temperature = Math.Round(turbineDataModel.InletTemperature, 2);//Math.Round(worksheet.Cells["C7"].GetValue<double>(), 2);
            double flow = Math.Round(turbineDataModel.VolumetricFlow, 2);//Math.Round(worksheet.Cells["D7"].GetValue<double>(), 2);
            double h1 = Math.Round(turbineDataModel.InletEnthalphy, 2);//Math.Round(worksheet.Cells["D6"].GetValue<double>(), 2);
            double pressure2 = Math.Round(turbineDataModel.ExhaustPressure, 2);//Math.Round(worksheet.Cells["E17"].GetValue<double>(), 2);
            double temperature2 = Math.Round(turbineDataModel.OutletTemperature, 2);//Math.Round(worksheet.Cells["E18"].GetValue<double>(), 2);
            double flow2 = Math.Round(turbineDataModel.OutletMassFlow, 2);//Math.Round(worksheet.Cells["F18"].GetValue<double>(), 2);
            double h2 = Math.Round(turbineDataModel.OutletEnthalphy, 2);//Math.Round(worksheet.Cells["F17"].GetValue<double>(), 2);
            double lpressure = Math.Round(turbineDataModel.LeakagePressure, 2);//Math.Round(worksheet.Cells["F13"].GetValue<double>(), 2);
            double ltemperature = Math.Round(turbineDataModel.LeakageTemperature, 2);//Math.Round(worksheet.Cells["F14"].GetValue<double>(), 2);
            double lFlow = Math.Round(turbineDataModel.LeakageMassFlow, 2);//Math.Round(worksheet.Cells["G14"].GetValue<double>(),2);
            double lh = Math.Round(turbineDataModel.LeakageEnthalphy, 2);//Math.Round(worksheet.Cells["G13"].GetValue<double>(), 2);
            double power = Math.Round(turbineDataModel.AK25, 2);//Math.Round(worksheet.Cells["AK25"].GetValue<double>(), 2);
            // Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            String existingPdfPath = "input.pdf";
            String pdfFilePath = "output.pdf";
            PdfDocument document = PdfReader.Open(existingPdfPath, PdfDocumentOpenMode.Modify);
            // // Access the first page
            PdfPage page = document.Pages[0];
            // // Get an XGraphics object for drawing on the page
            XGraphics gfx = XGraphics.FromPdfPage(page);
            // // Create a regular font (16pt Verdana)
            // double leakageMassFlow = 1.03;
            // double outMassFlow = massFlowRate - leakageMassFlow;
            XFont font = new XFont("Verdana", 11);
            gfx.DrawString(pressure.ToString(), font, XBrushes.Black,
                new XRect(179, 171, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(h1.ToString(), font, XBrushes.Black,
                new XRect(240, 171, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(temperature.ToString(), font, XBrushes.Black,
                new XRect(179, 195, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(flow.ToString(), font, XBrushes.Black,
                new XRect(240, 195, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(power.ToString(), font, XBrushes.Black,
                new XRect(460, 220, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(lpressure.ToString(), font, XBrushes.Black,
                new XRect(368, 291, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(lh.ToString(), font, XBrushes.Black,
                new XRect(425, 291, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(ltemperature.ToString(), font, XBrushes.Black,
               new XRect(368, 313, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(lFlow.ToString(), font, XBrushes.Black,
               new XRect(425, 313, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(pressure2.ToString(), font, XBrushes.Black,
               new XRect(300, 354, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(h2.ToString(), font, XBrushes.Black,
               new XRect(360, 354, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(temperature2.ToString(), font, XBrushes.Black,
               new XRect(300, 378, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(flow2.ToString(), font, XBrushes.Black,
               new XRect(360, 378, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(Math.Round((1.1 * pressure2), 2).ToString(), font, XBrushes.Black,
               new XRect(273, 514, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(Math.Round((0.9 * pressure2), 2).ToString(), font, XBrushes.Black,
              new XRect(273, 531, 100, 100), XStringFormats.TopLeft);
            gfx.DrawString(power.ToString(), font, XBrushes.Black,
              new XRect(410, 647, 100, 100), XStringFormats.TopLeft);
            document.Save(pdfFilePath);
            string sourceDirectory = @"C:\testDir";
            string currDate = DateTime.Now.ToString("yyyyMMdd"); // Format date as yyyy-MM-dd
            string currTime = DateTime.Now.ToString("HHmmss"); // Format time as HH-mm-ss
            string destinationDirectory = @"C:\testdir\turbine_files";//_{currDate}-{currTime}";
            string destinationDirectoryFinal = $@"C:\testdir\turbine_files_{currDate}-{currTime}";
            string curr = Directory.GetCurrentDirectory();
            string destFile1 = Path.Combine(destinationDirectory, "output.pdf");
            string file1 = curr + "\\output.pdf";
            string logPath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");
            try
            {
                if (Directory.Exists(sourceDirectory))
                {
                    string[] files = Directory.GetFiles(sourceDirectory);
                    if (files.Length > 0)
                    {
                        // Check if a file with the same name exists
                        // if (File.Exists(destinationDirectory))
                        // {
                        //     // Delete the file
                        //     File.Delete(destinationDirectory);
                        //     Console.WriteLine($"File deleted: {destinationDirectory}");
                        // }
                        if (!Directory.Exists(destinationDirectory))
                            Directory.CreateDirectory(destinationDirectory);
                         TurbineDesignPage.outputPath = destinationDirectoryFinal;
                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            if (fileName == "TURBAE1.BSP" || fileName == "TURBAE1.CAD"
                            || fileName == "TURBAE1.DAT" || fileName == "TURBAE1.ERG"
                            || fileName == "TURBAE1.PUN")
                            {
                                string destFile = Path.Combine(destinationDirectory, Path.GetFileName(file));
                                File.Copy(file, destFile, true);
                                Console.WriteLine($"Copied: {fileName} to {destFile}");
                            }
                        }
                        
                    }
                }

                File.Copy(file1, destFile1, true);
                string destFile2 = Path.Combine(destinationDirectory, Path.GetFileName(logPath));
                File.Copy(logPath, destFile2, true);
                Directory.Move(destinationDirectory, destinationDirectoryFinal);
                //File.Copy(logPath, destFile1, true);

            }
            catch (Exception ex)
            {
                logger.LogError("exportToPDFandTurbineFiles", ex.Message);
            }
        }

        void prepareTurbineFiles()
        {
            // TurbaConfig turbaConfig = new TurbaConfig();
            // turbaConfig.PrepareTurbineFiles();
            // use from TURBA_Interface
            /* Implementation */
            TurbaAutomation turbaAutomation = new TurbaAutomation();
            turbaAutomation.PrepareTurbineFiles();

        }
        void NoLoadPowerOptimize(int maxlp = 0)
        {
            ExecNoLoadPowerOptimizer execNoLoadPowerOptimizer = new ExecNoLoadPowerOptimizer();
            execNoLoadPowerOptimizer.NoLoadPowerOptimize(maxlp);
            //use from PowerNoLoadOptimizer function
        }
    }