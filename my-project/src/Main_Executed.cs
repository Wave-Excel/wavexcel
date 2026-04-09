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
using Ignite_x_wavexcel;
using StartKreislExecution;
using Kreisl.KreislConfig;
using Ignite_X.src.core.Handlers;
using Optimizers.ERG_NozzleOptimizer;
using Optimizers.Exec_ERG_NozzleOptimizer;
using Models.AdditionalLoadPointModel;
using ExtraLoadPoints;
using static Microsoft.Maui.Controls.Internals.Profile;


namespace StartExecutionMain;
public class MainExecutedClass
{
    public static IHost GlobalHost { get;  set; }
    public static int row = 0;

    private static Dictionary<string, int> mainCallCounters;
    private static Dictionary<string, int> throttleCounters;
    private const int MAX_THROTTLE_CALLS = 2;
    private static bool countersInitialized = false;

    private ILogger logger;


    private TurbineDataModel turbineDataModel;
    private FileInfo excelFile;
    private static IThermodynamicLibrary thermodynamicService;
    private AdditionalLoadPoint additionalLoadPoint;
    private IConfiguration configuration;
    private string filePath;
    private ExcelPackage package;
    private string MainTemp = "";

    public MainExecutedClass()
    {
        turbineDataModel = TurbineDataModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        // logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        filePath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        additionalLoadPoint = AdditionalLoadPoint.GetInstance();
        excelFile = new FileInfo(filePath);
        package = new ExcelPackage(excelFile);
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        //InitializeCallCounters();

    }

    public void Workbook_Open()
    {
        InitializeCallCounters();
        //ResetCleanUpExecutedNearest();
    }

    public void fillCounters()
    {
        //if (mainCallCounters == null)
        mainCallCounters = new Dictionary<string, int>();
        //if (throttleCounters == null)
        throttleCounters = new Dictionary<string, int>();
        turbineDataModel.ListPower = new List<PowerNearest>();
        countersInitialized = false;
    }
    private void InitializeCallCounters()
    {
         if(mainCallCounters == null)
            mainCallCounters = new Dictionary<string, int>();
        if(throttleCounters == null)
            throttleCounters = new Dictionary<string, int>();
        // mainCallCounters = new Dictionary<string, int>();
        // throttleCounters = new Dictionary<string, int>();
    }
    public void RemoveVari40()
    {
        string filePath = "C:\\testDir\\TURBATURBAE1.DAT.DAT";
        string[] line = File.ReadAllLines(filePath);
        string newContent = "";
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i].Contains("5        40.000     2.000 KOPPLUNG MIT PROGRAMM KREISL"))
            {
                continue;
            }
            else
            {
                newContent += (line[i]+Environment.NewLine);
            }
        }
        File.WriteAllText(filePath, newContent);

    }

    public void MainExecuted(string criteria , int maxLp = 0)
    {
        Logger("MainExecuted called with argument: " + criteria);
        int maxCalls;

        var powerNearest = package.Workbook.Worksheets["PowerNearest"];
        maxCalls = turbineDataModel.NoOfExecuted;

        if (!countersInitialized)
        {
            InitializeCallCounters();
            countersInitialized = true;
        }

        if (!mainCallCounters.ContainsKey(criteria))
        {
            mainCallCounters[criteria] = 0;
        }

        if (criteria == "Throttle" && !throttleCounters.ContainsKey(criteria))
        {
            throttleCounters[criteria] = 0;
        }

        if (criteria == "Throttle")
        {
            if (throttleCounters[criteria] < MAX_THROTTLE_CALLS)
            {
                throttleCounters[criteria]++;
                Console.WriteLine("Throttle subroutine executed with argument: " + criteria);
            }
            else
            {
                Console.WriteLine("going to custom path for Throttle");
                return;
            }
        }
        else
        {
            if (mainCallCounters[criteria] < maxCalls)
            {
                
                mainCallCounters[criteria]++;
                Logger("Flowpath executed with argument: " + criteria);
                ResetNozzleCounter();
                TurbineDataModel.getInstance().OldNa = 0;
                TurbineDataModel.getInstance().OldNb = 0;

            }
            else
            {
                if (criteria == "BCD1120")
                {
                    ResetNozzleCounter();
                    TurbineDataModel.getInstance().OldNa = 0;
                    TurbineDataModel.getInstance().OldNb = 0;
                    row = 0;
                    
                    ResetCleanUpExecutedNearest();
                    MainExecuted("BCD1190",maxLp);
                }
                else if (criteria == "BCD1190")
                {
                    Logger("Going to Custom Path...");
                    Main_CustomFlowPathTest(maxLp);
                    return;
                }
            }
        }
        if (TurbineDesignPage.finalToken.IsCancellationRequested || TurbineDesignPage.cts.IsCancellationRequested)
        {
            return;
        }
        Console.WriteLine("MainExecuted function called with argument: " + criteria);
        CustomerInputHandler();
        ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
        if (StartKreisl.kreislKey)
            execHMBDConfiguration.HBDsetDefaultCustomerParamas_Executed_Kreisl();
        else
            execHMBDConfiguration.HBDsetDefaultCustomerParamas_Executed();

        turbineDataModel.ListPower[0].Power = turbineDataModel.AK25;
        PowerKNN(criteria);
        MoveYAndSetParams();

        Logger("Selecting DAT file..");
        ReferenceDATSelectorExecuted(criteria);

        // Adding a check for var 40;
        //RemoveVari40();
        
       
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        LoadDatFile();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }


        Logger("Generating Load Points..");
        GenerateLoadPoints(maxLp);
        HBDsetDefaultCustomerParamsExecuted(StartKreisl.kreislKey);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        double efficiency = turbineDataModel.ListPower[0].Efficiency; 
        HBDupdateEfficiency(efficiency);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        Logger("Writing LoadPoints in DAT file..");
        PrepareDATFileExecuted(maxLp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        
        if (!IsWheelChamberPressureValid())
        {
            Logger("Wheelchamber pressure is too low in selected project...");
            Logger("Selecting other project...");
            MainExecuted(criteria,maxLp);
        }
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        LaunchTurba(maxLp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }


        ErgResultsCheckExecuted(criteria , false , maxLp);
        if (TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            return;
        }
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        ResetNozzleCounter();
        UpdateLP5();
        ErgResultsCheckExecuted(criteria , true,maxLp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        ResetNozzleCounter();
        if (TurbineDesignPage.cts.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }
        ValvePointOptimize(maxLp);
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        PowerMatch powerMatch = new PowerMatch();

        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        kreislDATHandler.FillVari40();
        TurbaAutomation turbaAutomation = new TurbaAutomation();
        turbaAutomation.LaunchTurba(maxLp);
        KreislIntegration kreislIntegration = new KreislIntegration();

        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 2)
        {
            if (!File.Exists("C:\\testDir\\TURBA.CON"))
                kreislIntegration.RenameTurbaCON("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislDATHandler.FillVari40();
            MainTemp = "";
            RemoveErg();
            kreislDATHandler.RefreshKreislDAT();
            double wheelChamberPressure = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
            kreislDATHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", Convert.ToString(wheelChamberPressure));
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
                    if(turbineDataModel.DeaeratorOutletTemp == 0&& turbineDataModel.PST == 0)
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
            if (turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0)
            {
                UpdateDesupratorWithTurba(10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count - 2);
            }

            kreislIntegration.LaunchKreisL();
        }
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        double wheelChamberP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
        kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");

        kreislDATHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", Convert.ToString(wheelChamberP));
        if (TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            return;
        }
        CheckPower(maxLp);
        if (TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            return;
        }
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
        //double pst = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PST;
        turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure) + 5) : turbineDataModel.PST;
        if (exhaustTemp > turbineDataModel.PST)
        {
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, 1);
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, 1);

                }
            }
            else
            {
                kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, 1);
                kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, 1);
            }
            //kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath,1);
            //kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath,1);
        }
        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PST == 0)
        {
            turbineDataModel.PST = 0;
        }
        count = 2;
        if (loadPoint - 10 >= 1)
        {
            int tt = loadPoint;
            for (int i = 10; i < tt; i++)
            {
                exhaustTemp = temps[2, i];
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[count].ExhaustPressure) + 5) : turbineDataModel.PST;
                if (exhaustTemp > turbineDataModel.PST)
                {
                    if (turbineDataModel.DeaeratorOutletTemp > 0)
                    {
                        if (turbineDataModel.DumpCondensor)
                        {
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, count);
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, count);
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, count);
                        }
                        else if (!turbineDataModel.DumpCondensor)
                        {
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, count);
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, count);
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, count);

                        }
                    }
                    else
                    {
                        kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, count);
                        kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, count);
                    }
                    //kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, count);
                    //kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, count);

                }
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[count].PST == 0)
                {
                    turbineDataModel.PST = 0;
                }
                count++;
            }
        }
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
        else if (turbineDataModel.DeaeratorOutletTemp == 0 &&  turbineDataModel.PST == 0)
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
            //MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
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
            else if ((turbineDataModel.DeaeratorOutletTemp> 0 || turbineDataModel.PST > 0) && initList[i].ExhaustMassFlow != 0)
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
    public static void UpdateLP5()
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
    }
    public void ErgResultsCheckExecuted(string criteria , bool isLP5Update,int maxLp=0)
    {
        switch (criteria)
        {
            case "BCD1120":
                Console.WriteLine("Performing ERG checks for BCD1120...");
                Logger("Performing ERG checks for BCD1120...");
                ErgResultsCheckBCD1120(isLP5Update,maxLp);
                break;

            case "BCD1190":
                Console.WriteLine("Performing ERG checks for BCD1190...");
                Logger("Performing ERG checks for BCD1190...");
                ErgResultsCheckBCD1190(isLP5Update,maxLp);
                break;

            case "Throttle":
                Console.WriteLine("Performing ERG checks for Throttle...");
                Logger("Performing ERG checks for Throttle...");
                ErgResultsCheckThrottle();
                break;

            default:
                Logger("Invalid ERG Check");
                Console.WriteLine("Invalid ERG Check");
                break;
        }
    }

    
    public void GotoBCD1120(int maxLp = 0){
        // logger.clear();
        fillDependencies();
        InitializeCallCounters();
        MainExecuted("BCD1120",maxLp);
    }

    public void GotoBCD1190(int maxLp = 0)
    {
        fillDependencies();
        InitializeCallCounters();
        MainExecuted("BCD1190",maxLp);
    }
    private void Logger(string message)
    {
        if(logger == null){
            logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        }
        logger.LogInformation(message);
    }

    public void fillDependencies(){
        GlobalHost = CreateHostBuilder(null).Build();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        StartExec.GlobalHost = GlobalHost;
        //  excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");


        NozzleTurbaDataModel.getInstance().fillNozzleTurbaDataModel();
        ExecutedDB.getInstance().fillExecutedDB();
        PowerEfficiencyModel.getInstance().fillPowerEfficiencyDataModel();
        PreFeasibilityDataModel.getInstance().fillPreFeasibilityData();
        LoadPointDataModel.getInstance().fillLoadPoints();
        TurbaOutputModel.getInstance().fillTurbaOutputDataList();




        logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        logger.clear();
        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        KreislDATHandler datHandler = new KreislDATHandler();


        turbineDataModel.GeneratorEfficiency = getGeneratorEff();
        turbineDataModel.NoOfExecuted = getNoNearestProject();
        turbineDataModel.LeakagePressure = 1.02;
        turbineDataModel.fillPowerNearestData();
        turbineDataModel.ListPower[0].SteamPressure = turbineDataModel.InletPressure;
        turbineDataModel.ListPower[0].SteamTemperature = turbineDataModel.InletTemperature;
        turbineDataModel.ListPower[0].SteamMass = turbineDataModel.MassFlowRate;
        turbineDataModel.ListPower[0].ExhaustPressure = turbineDataModel.ExhaustPressure;
        turbineDataModel.ListPower[0].KNearest = turbineDataModel.NoOfExecuted.ToString();
        logger.LogInformation(Convert.ToString(turbineDataModel.InletPressure));

    }
    public int getNoNearestProject()
    {
        int k = 3;
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Split the line by comma
                var values = line.Split(',');

                // Process the values (for example, print them)
                //foreach (var value in values)
                //{
                //    Console.Write(value + " ");
                //}
                if (values[0] == "NoOfExecutedProject")
                {
                    return Convert.ToInt32(values[1]);
                }

            }
        }
        return k;
    }
    public static void ResetNozzleCounter()
    {
        ExecutedNozzleOptimizer.Na = 0;
        ExecutedNozzleOptimizer.nozzleOptimizeCount = 0;
        ExecutedNozzleOptimizer.Nb = 0;
        ExecutedNozzleOptimizer.A1 = 0;
        ExecutedNozzleOptimizer.A2 = 0;
        //TurbineDataModel.getInstance().OldNa = 0;
        //TurbineDataModel.getInstance().OldNb = 0;
    }
    public static double getGeneratorEff()
    {
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Split the line by comma
                var values = line.Split(',');
                if (values[0] == "GeneratorEff")
                {
                    return Convert.ToDouble(values[1]);
                }

            }
        }
        return 95.8;
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services with the DI container
                    services.AddSingleton<IThermodynamicLibrary, ThermodynamicService>();
                    services.AddSingleton<ILogger, Logger>();
                });
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

    private void Main_CustomFlowPathTest(int maxlp = 0)
    {
        Logger("Moving to Custom Flow Path------------------------------------");
        CustomExecutedClass customFlow = new CustomExecutedClass();
        customFlow.Main_CustomFlowPathTest(maxlp);
        //TurbineDesignPage.cts.Cancel();
        // Implementation for Main_CustomFlowPathTest
    }

    private void CustomerInputHandler()
    {
        // Implementation for CustomerInputHandler
    }

    private void PowerKNN(string criteria)
    {
        // Implementation for PowerKNN
        PowerKNN powerKNN = new PowerKNN();
        powerKNN.ExecutePowerKNN(criteria);
    }

    private void MoveYAndSetParams()
    {
        // Implementation for MoveYAndSetParam
        FlowPathSelector execRefDATSelector = new FlowPathSelector();
        execRefDATSelector.MoveYAndSetParams();
    }

    private void ReferenceDATSelectorExecuted(string criteria)
    {
        // Implementation for ReferenceDATSelectorExecuted
        FlowPathSelector execRefDATSelector = new FlowPathSelector();
        // Exec_Ref_DAT_Selector execRefDATSelector = new Exec_Ref_DAT_Selector();
        execRefDATSelector.ReferenceDATSelectorExecuted(criteria);
    }

    private void LoadDatFile()
    {
        // Implementation for LoadDatFile
        // ExecutedDATFileProcessor
        ExecutedDATFileProcessor execDATHandler = new ExecutedDATFileProcessor();
        execDATHandler.LoadDatFile();
    }

    private void GenerateLoadPoints(int maxLp=0)
    {
        // Implementation for GenerateLoadPoints
        ExecLoadPointGenerator execLoadPointGenerator = new ExecLoadPointGenerator();
        execLoadPointGenerator.GenerateLoadPoints(maxLp);
    }

    private void HBDsetDefaultCustomerParamsExecuted(bool kreislKey = false)
    {
        // Implementation for HBDsetDefaultCustomerParamas_Executed
        ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
        if(kreislKey)
        {
            execHMBDConfiguration.HBDsetDefaultCustomerParamas_Executed_Kreisl();
        }
        else
            execHMBDConfiguration.HBDsetDefaultCustomerParamas_Executed();  //   HBDsetDefaultCustomerParamas_Executed();
    }

    private void HBDupdateEfficiency(double efficiency)
    {
        TurbineDataModel.getInstance().TurbineEfficiency = efficiency;
    }

    private void PrepareDATFileExecuted(int maxLp = 0)
    {
        // Implementation for PrepareDATFile_Executed
        ExecutedDATFileProcessor execDATHandler = new ExecutedDATFileProcessor();
        execDATHandler.PrepareDatFileExecuted(maxLp);
    }

    private bool IsWheelChamberPressureValid()
    {
        // Implementation for IsWheelChamberPresssureValid
        ExecutedDATFileProcessor execDATHandler = new ExecutedDATFileProcessor();
        return execDATHandler.IsWheelChamberPressureValid();
    }

    private void LaunchTurba(int mxLPs = 0)
    {
        TurbaAutomation turbaConfig = new TurbaAutomation();
        turbaConfig.LaunchTurba(mxLPs);
        // Implementation for LaunchTurba
    }

    private void ValvePointOptimize(int maxlp = 0)
    {
        // Implementation for ValvePointOptimize
        ExecValvePointOptimizer valvePointOptimizer = new ExecValvePointOptimizer();
        valvePointOptimizer.ValvePointOptimize(maxlp);
    }

    private void CheckPower(int maxlp = 0)
    {
        // Implementation for CheckPower
        ExecPowerMatch  powerMatch = new ExecPowerMatch();
        powerMatch.CheckPower(maxlp);
    }

    private void ErgResultsCheckBCD1120(bool isLP5Update,int maxLP = 0)
    {
        ERG_BCD1120 bcd1120 = new ERG_BCD1120();
        ERG_BCD1120.isCheckingLP5 = isLP5Update;
        bcd1120.ErgResultsCheckBCD1120(maxLP);
        // Implementation for ErgResultsCheckBCD1120
    }

    private void ErgResultsCheckBCD1190(bool isLP5Update, int maxLP = 0)
    {
        ERG_BCD1190 bcd1190 = new ERG_BCD1190();
        ERG_BCD1190.isLP5Update = isLP5Update;
        bcd1190.ErgResultsCheckBCD1190(maxLP);
        // Implementation for ErgResultsCheckBCD1190
    }

    private void ErgResultsCheckThrottle()
    {
        // ERGResultsChecker
        ERGResultsChecker ergThrottle = new ERGResultsChecker();
        ergThrottle.ERGResultsCheckThrottle();
        // Implementation for ErgResultsCheckThrottle
    }
}