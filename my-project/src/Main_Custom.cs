using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using Checks.ERG_BCD1120;
using Checks.ERG_BCD1190;
using Interfaces.ILogger;
using StartExecutionMain;
using HMBD.Power_KNN;
using Turba.TurbaConfiguration;
using Models.TurbineData;
using Optimizers.Executed_ERG_ValvePointOptimizer;
using Microsoft.Extensions.Configuration;
using HMBD.Exec_Ref_DAT_Selector;
using Handlers.Exec_DAT_Handler;
using HMBD.Exec_LoadPointGenerator;
using Exec_HMBD_Configuration;
using Turba.Exec_TurbaConfig;
using Checks.Exec_PowerMatch;
using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Hosting;
using Exec_ERG_Throttle;
using Services.ThermodynamicService;
using Utilities.Logger;
using HMBD.LoadPointGenerator;
using HMBD.Ref_DAT_selector;
using ERG_ValvePointOptimizer;
using HMBD.HMBDInformation;
using ERG_PowerMatch;
using Models.LoadPointDataModel;
using ERG_Verification;
using Models.NozzleTurbaData;
using Models.PowerEfficiencyData;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Models.ExecutedProjectDB;
using Cu_HMBD_Configuration;
using HMBD.Custom_LoadPointGenerator;
using Handlers.CustomPathDeletePrepare;
using HMBD.Cu_Pun_Convertor;
using HMBD.Cu_Ref_DAT_Selector;
using Handlers.Custom_DAT_Handler;
using Checks.SAXA_SAXI;
using HMBD.PSO_PenalityFunctionNozzle;
using Optimizers.PSOFlowPathNozzle;
using Checks.CustomBCD1120;
using Turba.Cu_TurbaConfig;
using Checks.Custom_PowerMatch;
using Optimizers.CustomValvePointOptimizer;
using Checks.CustomERGCheck1190;
using Ignite_X.src.core.Handlers;
using Kreisl.KreislConfig;
using Interfaces.IERGHandlerService;
using Ignite_X.src.core.Services;
using StartKreislExecution;
using DocumentFormat.OpenXml.Vml.Office;
using Ignite_x_wavexcel;
using Optimizers.PSOFlowPathNozzle;
using Models.AdditionalLoadPointModel;
using ExtraLoadPoints;
using Optimizers.CustomNozzleOptimizer;
namespace StartExecutionMain;
public class CustomExecutedClass
{
    public static IHost GlobalHost { get;  set; }

    private static Dictionary<string, int> mainCallCounters;
    private static Dictionary<string, int> throttleCounters;
    private const int MAX_THROTTLE_CALLS = 2;
    private static bool countersInitialized = false;
    public static string filePath = "C:\\testDir\\kreisl.dat", ergFilePath = "C:\\testDir\\KREISL.erg";

    private ILogger logger;
    public static bool kreislKey = false;
    private AdditionalLoadPoint additionalLoadPoint;
    private TurbineDataModel turbineDataModel;
    private FileInfo excelFile;
    private IConfiguration configuration;
    //private string filePath;
    private ExcelPackage package;
    private PreFeasibilityDataModel preFeasibilityDataModel;
    private static IThermodynamicLibrary thermodynamicService;
    private string MainTemp = "";

    public CustomExecutedClass()
    {
        turbineDataModel = TurbineDataModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        additionalLoadPoint = AdditionalLoadPoint.GetInstance();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();

    }
    public static void DeleteCONFiles()
    {
        string directoryPath = @"C:\testDir";

        try
        {
            // Get all .CON files in the specified directory
            string[] conFiles = Directory.GetFiles(directoryPath, "*.CON");
            string[] ergFiles = Directory.GetFiles(directoryPath, "*.ERG");

            // Loop through each file and delete it
            foreach (string file in conFiles)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted: {file}");
            }
            foreach (string file in ergFiles)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted: {file}");
            }

            Console.WriteLine("All .CON files have been deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        directoryPath = "C:\\testDir\\Turman250";

        if (!Directory.Exists(directoryPath))
        {
            return;
        }
        try
        {
            // Get all .CON files in the specified directory
            string[] conFiles = Directory.GetFiles(directoryPath, "*.CON");
            string[] ergFiles = Directory.GetFiles(directoryPath, "*.ERG");

            // Loop through each file and delete it
            foreach (string file in conFiles)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted: {file}");
            }
            foreach (string file in ergFiles)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted: {file}");
            }

            Console.WriteLine("All .CON files have been deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    public void Main_CustomFlowPathTest(int mxlp = 0)
    {
        kreislKey = true;
        fillDependencies();
        DeleteCONFiles();
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        kreislDATHandler.RefreshKreislDAT();
        thermodynamicService.FillClosestTurbineEfficiency();
        CustomHMBDConfiguration customHMBDConfiguration = new CustomHMBDConfiguration();

        customHMBDConfiguration.GetTurbaCON(TurbineDataModel.getInstance().ClosestProjectID);
        FillInputValues();
        kreislDATHandler.RefreshKreislDAT();
        FillInputValues();
        Logger("PressIn:" + TurbineDataModel.getInstance().InletPressure + ", " + "TempIn:" + TurbineDataModel.getInstance().InletTemperature + ", " + "FlowIn:" + TurbineDataModel.getInstance().MassFlowRate + ", " + "PressEx: " + TurbineDataModel.getInstance().ExhaustPressure);
        Logger("Starting Custom Turbine Design...");
        ResetCleanUpExecutedNearest();
        Logger("------CUSTOM FLOW PATH TEST------");
        Logger("Searching efficiency from last projects...");
        customHMBDConfiguration.HBDSetDefaultCustomerParamsKreisL();
        customHMBDConfiguration.HBDUpdateEffGeneratorInit();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        Logger("Generating Load Points...");
        CustomLoadPointGenerator customLoadPointGenerator= new CustomLoadPointGenerator();
        customLoadPointGenerator.GenerateLoadPoints(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        customHMBDConfiguration.HBDSetDefaultCustomerParamsKreisL();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        Logger("Selecting custom path template DAT file...");
        preFeasibilityDataModel.fillPrefeasibilityDecisionChecks();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        CustomDatFileHandler customDatFileHandler = new CustomDatFileHandler();
        customDatFileHandler.GetNearestParams_Custom();
        // HelperFunctions.GetNearestParamsCustom();
        Logger("WAVEXCEL: Deleting EXECUTED DAT");
        CuPunConvertor cuPunConvertor= new CuPunConvertor();
        cuPunConvertor.DeleteExecutedDat();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        // HelperFunctions.DeleteExecutedDat();
        Logger("WAVEXCEL: COPY REF DAT FILE");
        CuFlowPathSelector cuFlowPathSelector  = new CuFlowPathSelector();
        string sourceFilePath = Path.Combine(AppContext.BaseDirectory, "10LP_TURBATURBAE1.DAT.DAT");

        // Define the destination directory and file path
        string destinationDirectory = @"C:\testDir\projects_repository\custom_flowPaths";
        string destinationFilePath = Path.Combine(destinationDirectory, "10LP_TURBATURBAE1.DAT.DAT");

        // Check if the destination directory exists, if not, create it
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Copy the file to the destination directory
        try
        {
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            Console.WriteLine("File copied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        cuFlowPathSelector.CopyRefDATFile(@"C:\testDir\projects_repository\custom_flowPaths\10LP_TURBATURBAE1.DAT.DAT");
        Logger("Start preparing DAT file...");
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        customDATFileProcessor.PrepareDatFile(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        CustomSaxaSaxi customSaxaSaxi = new CustomSaxaSaxi();
        Logger("WAVEXCEL: BCD UPDATE");
        customSaxaSaxi.BCD_UPDATE(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        // HelperFunctions.BCDUpdate();
        PenaltyScoreCalculator penaltyScoreCalculator = new PenaltyScoreCalculator();
        

        RelationshipAwarePSOOptimizer pSOFlowPathOptimizerNozzle = new RelationshipAwarePSOOptimizer();

        if (configuration.GetValue<bool>("AppSettings:UseOllamaGuidedNozzle"))
            Logger("WAVEXCEL: Nozzle optimizer mode = Ollama-guided (local Ollama must be running; see appsettings Ollama section).");
        else
            Logger("WAVEXCEL: Nozzle optimizer mode = PSO (set AppSettings:UseOllamaGuidedNozzle=true for Ollama).");

        Logger("WAVEXCEL: PSO FLOW OPTIMISZER");
        RelationshipAwarePSOOptimizer.mxlp = mxlp;
        pSOFlowPathOptimizerNozzle.InvokeTurbineDesigner();
        
        
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        CustomERGCheck1120 customERGCheck1120 = new CustomERGCheck1120();
        
        
        Logger("WAVEXCEL: CUSTOM BASE CHECKS");
        customERGCheck1120.ERG_CUSTOM_BASE_CHECKS();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        // HelperFunctions.ERGCustomBaseChecks();
        Logger("WAVEXCEL:TURNA CONVERT");
        cuPunConvertor.TurnaConvert(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        // HelperFunctions.TurnaConvert();
        CuTurbaAutomation cuTurbaAutomation  = new CuTurbaAutomation();

        cuTurbaAutomation.LaunchTurba(mxlp);
        Logger("WAVEXCEL: UPDATE STEAM PATH");
        cuPunConvertor.UpdatePunConvertor();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }


        cuTurbaAutomation.LaunchTurba(mxlp);
        bool preFeasibilityFlowPathBCD1120Check  = (preFeasibilityDataModel.Decision == "TRUE")?true:false;
        bool preFeasibilityFlowPathBCD1190Check  = (preFeasibilityDataModel.Decision_2 == "TRUE")?true:false;

        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();

        if (preFeasibilityFlowPathBCD1120Check)
        {
            CustomERGCheck1120.isCheckingLP5 = false;
            customERGCheck1120.ErgResultsCheckBCD1120_Custom(mxlp);
            turbineDataModel.TurbineStatus = "BCD1120";
        }
        else if (preFeasibilityFlowPathBCD1190Check)
        {
            CustomERGCheck1190 customERGCheck1190 = new CustomERGCheck1190();
            CustomERGCheck1190.isCheckingLP5 = false;
            turbineDataModel.TurbineStatus = "BCD1190";
            customERGCheck1190.ErgResultsCheckBCD1190_Custom(mxlp);
        }
        ResetNozzleCounter();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        UpdateLP5(mxlp);
        if (preFeasibilityFlowPathBCD1120Check)
        {
            CustomERGCheck1120.isCheckingLP5 = true;

            customERGCheck1120.ErgResultsCheckBCD1120_Custom(mxlp);
            turbineDataModel.TurbineStatus = "BCD1120";
        }
        else if (preFeasibilityFlowPathBCD1190Check)
        {
            CustomERGCheck1190.isCheckingLP5 = true;

            CustomERGCheck1190 customERGCheck1190 = new CustomERGCheck1190();
            turbineDataModel.TurbineStatus = "BCD1190";
            customERGCheck1190.ErgResultsCheckBCD1190_Custom(mxlp);
        }
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        ResetNozzleCounter();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        Logger("WAVEXCEL: CUSTOM VALVE POINT OPITMIZER");
        
        CustomValvePointOptimizer customValvePointOptimizer = new CustomValvePointOptimizer();
        customValvePointOptimizer.ValvePointOptimize(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        kreislDATHandler.FillVari40();
        cuTurbaAutomation.LaunchTurba(mxlp);
        
        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 2)
        {
            if (!File.Exists("C:\\testDir\\TURBA.CON"))
                kreislIntegration.RenameTurbaCON("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislDATHandler.FillVari40();
            MainTemp = "";
            RemoveErg();
            kreislDATHandler.RefreshKreislDAT();
            double wheelChamberPressu = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
            kreislDATHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", Convert.ToString(wheelChamberPressu));
            //KreislDATHandler kreislDATHandler = new KreislDATHandler();
            int count = 11;
            for (int i = 1; i < additionalLoadPoint.CustomerLoadPoints.Count; i++)
            {
                if (i == 1)
                {
                    
                    int lpNo = additionalLoadPoint.CustomerLoadPoints[i].LPNumber;
                    int index = CustomLoadPointHandler.lpNumberToIndexMap[lpNo];
                    fillAGainDat(index, CustomLoadPointHandler.initList);
                }
                else
                {
                    int lpNo = additionalLoadPoint.CustomerLoadPoints[i].LPNumber;
                    int index = CustomLoadPointHandler.lpNumberToIndexMap[lpNo];
                    double press = CustomLoadPointHandler.initList[index].SteamPressure;
                    double Temp = CustomLoadPointHandler.initList[index].SteamTemp;
                    double mass = CustomLoadPointHandler.initList[index].SteamMass;
                    double exPres = CustomLoadPointHandler.initList[index].ExhaustPressure;
                    double Power = CustomLoadPointHandler.initList[index].PowerGeneration;
                    double exMass = CustomLoadPointHandler.initList[index].ExhaustMassFlow;
                    if(turbineDataModel.DeaeratorOutletTemp == 0 && turbineDataModel.PST == 0)
                    {
                        if (mass == 0 && exMass > 0)
                        {
                            CustomLoadPointHandler.initList[index].SteamMass = 0.055 + CustomLoadPointHandler.initList[index].ExhaustMassFlow;
                        }
                        else if (mass > 0 && exMass == 0)
                        {
                            CustomLoadPointHandler.initList[index].ExhaustMassFlow = CustomLoadPointHandler.initList[index].SteamMass - 0.055;
                        }
                    }
                   

                    if (CustomLoadPointHandler.initList[index].SteamPressure == 0)
                    {
                        fillLPAgain(index, "Pr", count, CustomLoadPointHandler.initList);
                    }
                    else if (CustomLoadPointHandler.initList[index].SteamTemp == 0)
                    {
                        fillLPAgain(index, "T", count, CustomLoadPointHandler.initList);
                    }
                    else if (CustomLoadPointHandler.initList[index].SteamMass == 0)
                    {
                        fillLPAgain(index, "M", count, CustomLoadPointHandler.initList);
                    }
                    else if (CustomLoadPointHandler.initList[index].PowerGeneration == 0)
                    {
                        fillLPAgain(index, "P", count, CustomLoadPointHandler.initList);
                    }
                    else if (CustomLoadPointHandler.initList[index].ExhaustPressure == 0)
                    {
                        fillLPAgain(index, "E", count, CustomLoadPointHandler.initList);
                    }
                    count++;
                }
            }
            File.WriteAllText("C:\\testDir\\KREISL.DAT", MainTemp);
            if (turbineDataModel.DeaeratorOutletTemp>0 ||  turbineDataModel.PST > 0)
            {
                UpdateDesupratorWithTurba(10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count - 2);
            }
            kreislIntegration.LaunchKreisL();
        }


        double wheelChamberPressure = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
        kreislDATHandler.FillWheelChamberPressure(filePath, "1 0", Convert.ToString(wheelChamberPressure));
        CustomPowerMatch customPowerMatch = new CustomPowerMatch();
        customPowerMatch.CheckPower(mxlp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        customERGCheck1120.ERG_CUSTOM_TURNA_CHECKS();//.ERGCustomTurnaChecks();
        customSaxaSaxi.SAXA_FIX();// SAXAFix();
        
        cuTurbaAutomation.LaunchTurba(mxlp);
        
        customPowerMatch.checkFinalTurbine();
    }
    public void RemoveErg()
    {
        string file = @"C:\testDir\KREISL.ERG";
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }
    public void UpdateDesupratorWithTurba(int loadPoint)
    {
        string[] lines = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.ERG");
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        //string[] lines = File.ReadAllLines("yourfile.erg");
        List<double[]> pressuresList = new List<double[]>();
        List<double[]> tempsList = new List<double[]>();
        List<double[]> enthList = new List<double[]>();
        List<double[]> massList = new List<double[]>();
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
        double exhaustTemp = temps[2, 0];
        if (exhaustTemp > turbineDataModel.PST)
        {
            kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, 1);
            kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, 1);
        }
        count = 2;
        if (loadPoint - 10 >= 1)
        {
            int tt = loadPoint;
            for (int i = 10; i < tt; i++)
            {
                exhaustTemp = temps[2, i];
                if (exhaustTemp > turbineDataModel.PST)
                {
                    kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, count);
                    kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, count);

                }
                count++;
            }
        }
    }
    public static void UpdateLP5(int maxlp =0)
    {
        double temp = TurbineDataModel.getInstance().InletTemperature - thermodynamicService.tsatvonp(TurbineDataModel.getInstance().InletPressure);
        if (temp >= 110)
        {
            temp = 60;
        }
        else
        {
            temp += 50;
        }
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        LoadPointDataModel lpDataModel = LoadPointDataModel.getInstance();
        List<LoadPoint> lpList = lpDataModel.LoadPoints;
        lpList[5].Pressure = TurbineDataModel.getInstance().InletPressure;
        lpList[5].Temp = TurbineDataModel.getInstance().InletTemperature - temp;
        lpList[5].MassFlow = TurbineDataModel.getInstance().MassFlowRate;
        lpList[5].BackPress = 0.5 * TurbineDataModel.getInstance().ExhaustPressure;
        lpList[5].Rpm = LoadPointDataModel.getInstance().LoadPoints[0].Rpm;
        lpList[5].InFlow = 0;
        lpList[5].BYP = -1;
        lpList[5].EIN = 0;
        lpList[5].WANZ = 0;
        lpList[5].RSMIN = 0;
        customDATFileProcessor.PrepareDatFileOnlyLPUpdate(maxlp);
    }
    public static void ResetNozzleCounter()
    {
        
        CustomNozzleOptimizer.Na = 0;
        CustomNozzleOptimizer.GNozzleCount = 0;
        CustomNozzleOptimizer.Nb = 0;
        CustomNozzleOptimizer.A1 = 0;
        CustomNozzleOptimizer.A2 = 0;
    }
    public void fillAGainDat(int i, List<CustomerLoadPoint> initList)
    {
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        //List<CustomerLoadPoint> input = AdditionalLoadPoint.GetInstance().CustomerLoadPoints;
        string filePath = StartKreisl.filePath;
        double press = initList[i].SteamPressure;
        double Temp = initList[i].SteamTemp;
        double mass = initList[i].SteamMass;
        double exPres = initList[i].ExhaustPressure;
        double Power = initList[i].PowerGeneration;
        double exMass = initList[i].ExhaustMassFlow;
        if (initList[i].DeaeratorOutletTemp > 0)
        {
            kreislDATHandler.MakeUpTemperature(filePath, 9, initList[i].MakeUpTempe.ToString());
            kreislDATHandler.Processcondensatetemperature(filePath, 12, initList[i].CondRetTemp.ToString());
            kreislDATHandler.FillCondensateReturn(filePath, "14", initList[i].ProcessCondReturn.ToString());
            turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(initList[i].ExhaustPressure) + 5) : turbineDataModel.PST;
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(initList[i].ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                if (kreislDATHandler.InPrvMultipleBackPressure(initList[i].ExhaustPressure))
                {
                    kreislDATHandler.fillPsatvont_t(filePath, 13, initList[i].DeaeratorOutletTemp.ToString());
                }
                else
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultipleinDumpCondensor(filePath);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultiple(filePath);
                    }
                }
            }
            if (initList[i].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            if (initList[i].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        else if (turbineDataModel.DeaeratorOutletTemp == 0 && turbineDataModel.PST == 0)
        {
            if (mass == 0 && exMass > 0)
            {
                initList[i].SteamMass = 0.055 + initList[i].ExhaustMassFlow;
            }
            else if (mass > 0 && exMass == 0)
            {
                initList[i].ExhaustMassFlow = initList[i].SteamMass - 0.055;
            }
        }
        

        if (initList[i].SteamPressure == 0)
        {

            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
            kreislDATHandler.FillInletPressure(filePath, 5, "42.981", -1);
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.PST > 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, "80");
            }
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].SteamTemp == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
            kreislDATHandler.FillInletTemperature(filePath, 5, "440", -1);
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].SteamMass == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
            kreislDATHandler.FillMassFlow(filePath, 5, "8.93", -1);
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            //kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (initList[i].PowerGeneration != 0)
            {
                kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            }
            else if ((turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0) && initList[i].ExhaustMassFlow != 0)
            {
                kreislDATHandler.ProcessMassFlow(filePath, 9, initList[i].ExhaustMassFlow.ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].PowerGeneration == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].ExhaustPressure == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
            kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59", -1);
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (initList[i].PST == 0)
            {
                turbineDataModel.PST = 0;
            }
        }
    }
    public void fillLPAgain(int i, string unk, int count, List<CustomerLoadPoint> initList)
    {

        string filePath = StartKreisl.filePath;
        string dat = Path.Combine(AppContext.BaseDirectory, "loadPoint.dat");
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor)
            {
                dat = Path.Combine(AppContext.BaseDirectory, "LoadPointDumpCondenPRV.DAT");
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                dat = Path.Combine(AppContext.BaseDirectory, "loadpointclosecyclePRV.DAT");
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            dat = Path.Combine(AppContext.BaseDirectory, "loadPointD.dat");
        }
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        if (unk == "Pr")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
            kreislDATHandler.FillInletPressure(filePath, 5, "42.981", -1);
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.PST > 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, "80".ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        else if (unk == "M")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
            kreislDATHandler.FillMassFlow(filePath, 5, "8.93", -1);
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            //kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (initList[i].PowerGeneration != 0)
            {
                kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            }
            else if ((turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0) && initList[i].ExhaustMassFlow != 0)
            {
                kreislDATHandler.ProcessMassFlow(filePath, 9, initList[i].ExhaustMassFlow.ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            //MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (unk == "T")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
            kreislDATHandler.FillInletTemperature(filePath, 5, "440", -1);
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            //MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (unk == "P")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);

                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            //MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (unk == "E")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
            kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59", -1);
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            kreislDATHandler.UpdateRPM2(StartKreisl.filePath, 6, LoadPointDataModel.getInstance().LoadPoints[0].Rpm.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            //MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0 && !turbineDataModel.IsPRVTemplate)
        {
            if (turbineDataModel.DumpCondensor)
            {

                kreislDATHandler.UpdateTemplatePRVToWPRVInDumpCondensor(filePath);

            }
            else if (!turbineDataModel.DumpCondensor)
            {

                kreislDATHandler.UpdateTemplatePRVToWPRV(filePath);

            }
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            kreislDATHandler.MakeUpTemperature(filePath, 9, initList[i].MakeUpTempe.ToString());
            kreislDATHandler.Processcondensatetemperature(filePath, 12, initList[i].CondRetTemp.ToString());
            kreislDATHandler.FillCondensateReturn(filePath, "14", initList[i].ProcessCondReturn.ToString());
            turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                if (kreislDATHandler.InPrvMultipleBackPressure(initList[i].ExhaustPressure))
                {
                    kreislDATHandler.fillPsatvont_t(filePath, 13, initList[i].DeaeratorOutletTemp.ToString());
                }
                else
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultipleinDumpCondensor(filePath);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultiple(filePath);

                    }
                }
            }
            if (initList[i].SteamPressure != 0)
            {
                if (unk != "Pr")
                {
                    kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            if (unk != "Pr")
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (initList[i].PST == 0)
            {
                turbineDataModel.PST = 0;
            }
        }
        MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");

    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services with the DI container
                    services.AddSingleton<IThermodynamicLibrary, ThermodynamicService>();
                    services.AddSingleton<ILogger, Logger>();
                    
                    services.AddSingleton<IERGHandlerService, KreislERGHandlerService>();
                });

    public void fillDependencies(){
        GlobalHost = CreateHostBuilder(null).Build();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        //  excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");

        MainExecutedClass.GlobalHost = GlobalHost;
        StartKreisl.GlobalHost = GlobalHost;
        StartExec.GlobalHost = GlobalHost;
        NozzleTurbaDataModel.getInstance().fillNozzleTurbaDataModel();
        ExecutedDB.getInstance().fillExecutedDB();
        PowerEfficiencyModel.getInstance().fillPowerEfficiencyDataModel();
        PreFeasibilityDataModel.getInstance().fillPreFeasibilityData();
        LoadPointDataModel.getInstance().fillLoadPoints();
        TurbaOutputModel.getInstance().fillTurbaOutputDataList();

            
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        logger.clear();
        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();

        ////(66.69, 4.9, 10.36, 485, "wavexcel");
        //turbineDataModel.InletPressure = 44.13;// 53.93;// 62.76;// 46.09;// 63.74;// 62.763;// 65.705;//64.724;//44.13;//62.76;//66.69;//42.981;
        //turbineDataModel.ExhaustPressure = 4.903;// 5.66;// 5.39;// 5.394;// 6.865;//4.413;//4.90;//5.39;//4.9;//4.59;
        //turbineDataModel.MassFlowRate = 7.222;// 7.22;// 8.33;// 12.5;// 8.333;//13.889;//11.2;//12.5;//10.36;//8.93;
        //turbineDataModel.InletTemperature = 450;// 480;// 485;// 485;// 450;// 485.016;// 500;//482;//485;//485;//440;
        turbineDataModel.GeneratorEfficiency = 96.7;
        turbineDataModel.NoOfExecuted = getNoNearestProject();
        turbineDataModel.LeakagePressure = 1.02;
        turbineDataModel.fillPowerNearestData();
        turbineDataModel.ListPower[0].SteamPressure = turbineDataModel.InletPressure;
        turbineDataModel.ListPower[0].SteamTemperature = turbineDataModel.InletTemperature;
        turbineDataModel.ListPower[0].SteamMass = turbineDataModel.MassFlowRate;
        turbineDataModel.ListPower[0].ExhaustPressure = turbineDataModel.ExhaustPressure;
        turbineDataModel.ListPower[0].KNearest = turbineDataModel.NoOfExecuted.ToString();
        
        logger.LogInformation(Convert.ToString(turbineDataModel.InletPressure));
        //FillInputValues();

    }
    public int getNoNearestProject()
    {
        int k = 3;
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var values = line.Split(',');
                if (values[0] == "NoOfExecutedProject")
                {
                    return Convert.ToInt32(values[1]);
                }

            }
        }
        return k;
    }
    public static bool checkIfDumpcondensorON()
    {
        int count = 0;
        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        if (turbineDataModel.InletPressure > 0)
        {
            count++;
        }
        if (turbineDataModel.InletTemperature > 0)
        {
            count++;

        }
        if (turbineDataModel.MassFlowRate > 0)
        {
            count++;
        }
        if (turbineDataModel.ExhaustPressure > 0)
        {
            count++;
        }
        if (turbineDataModel.OutletMassFlow > 0)
        {
            count++;
        }
        if (turbineDataModel.AK25 > 0)
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
    public static void FillInputValues()
    {
        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        KreislDATHandler datHandler = new KreislDATHandler();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor == true)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, turbineDataModel.ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                datHandler.FillInletPressure(filePath, 18, turbineDataModel.InletPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, turbineDataModel.InletTemperature.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                datHandler.ProcessMassFlow(filePath, 9, turbineDataModel.OutletMassFlow.ToString());
                if (turbineDataModel.Capacity == 0 && checkIfDumpcondensorON() == false)
                {
                    datHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    datHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                datHandler.fillCapacity(filePath, 9, turbineDataModel.Capacity.ToString());
                if (turbineDataModel.MassFlowRate > 0)
                {
                    datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                }
                else if (turbineDataModel.AK25 > 0)
                {
                    datHandler.FillVariablePower(filePath, 19, turbineDataModel.AK25.ToString());
                }
            }
            else if (turbineDataModel.DumpCondensor == false)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, turbineDataModel.ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                datHandler.FillInletPressure(filePath, 18, turbineDataModel.InletPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, turbineDataModel.InletTemperature.ToString());
                datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            if (turbineDataModel.PST < 120)
            {
                turbineDataModel.PST -= 10;
            }
            datHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(filePath, 8, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.FillMassFlow(filePath, 9, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 2, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 6, turbineDataModel.InletTemperature.ToString());

        }
        else
        {
            datHandler.FillMassFlow(filePath, 5, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 5, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 4, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
        }
        //using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        //{
        //    string line;
        //    while ((line = reader.ReadLine()) != null)
        //    {
        //        var values = line.Split(',');
        //        Logger(values[0] + "=" + values[1]);
        //    }
        //}

    }
    public void Logger(string message){
        logger.LogInformation(message);
    }
    private void ResetCleanUpExecutedNearest()
    {
         List<PowerNearest> powerNearest = turbineDataModel.ListPower;
         int k = Convert.ToInt32(powerNearest[0].KNearest);
        
        powerNearest.Clear();
        turbineDataModel.fillPowerNearestData();
        turbineDataModel.ListPower[0].SteamPressure = turbineDataModel.InletPressure;
        turbineDataModel.ListPower[0].SteamTemperature = turbineDataModel.InletTemperature;
        turbineDataModel.ListPower[0].SteamMass = turbineDataModel.MassFlowRate;
        turbineDataModel.ListPower[0].ExhaustPressure = turbineDataModel.ExhaustPressure;
        
        turbineDataModel.ListPower[0].KNearest = turbineDataModel.NoOfExecuted.ToString();
        
    }
}
 
