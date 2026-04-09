using System;
// using ERG_Verification;
// using ERG_ValvePointOptimizer;
// using ERG_PowerMatch;
// using TurbineUtils;
// using ERG_NozzleOptimizer;
// using ERG_RsminHandler;
using Models.NozzleTurbaData;
using Models.PowerEfficiencyData;
using Models.PreFeasibility;
using Microsoft.Extensions.Configuration;

// using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Turba.TurbaConfiguration;
using Interfaces.ILogger;
using ERG_Handler;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.IThermodynamicLibrary;
// using Microsoft.Extensions.Logging;
using Handlers.DAT_Handler;
using Services.ThermodynamicService;
using Utilities.Logger;
using HMBD.LoadPointGenerator;
using HMBD.Ref_DAT_selector;
using ERG_ValvePointOptimizer;
using HMBD.HMBDInformation;
using OfficeOpenXml;
using ERG_PowerMatch;
using Models.LoadPointDataModel;
using ERG_Verification;
using Models.TurbineData;
using Models.TurbaOutputDataModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Ignite_x_wavexcel;
using Interfaces.IERGHandlerService;
using Ignite_X.src.core.Services;
using StartKreislExecution;
using Ignite_X.src.core.Handlers;

namespace StartExecutionMain;
class StartExec
{
    public static IHost GlobalHost { get;  set; }
    public static string excelPath;
    private static ILogger logger;


    public static void MainKreisl()
    {

    }
    public static void Main1()  
    {
        
        // InitConfig();


        // CustomerInputHandler();
        // Console.WriteLine(excelPath);
        // //return;
        // Logger("Searching efficiency from last projects..");
        // NormalizeData();


        // HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();

        
        // hBDPowerCalculator.HBDSetDefaultCustomerParams();
        // // return;
        // hBDPowerCalculator.HBDUpdateEffGeneratorInit();
        // hBDPowerCalculator.HBDPersistInitialPower();

        // Logger("Selecting DAT file..");
        // DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        // datFileSelector.ReferenceDATSelector();

        // Logger("Generating Load Points..");
        // LoadPoint loadPoint = new LoadPoint();
        // loadPoint.GenerateLoadPoints();
        // //If Ref DAT is not found then Don't attempt to write LPs. As of now..

        // Logger("Start preparing DAT file...");
        // DATFileProcessor dATFileProcessor = new DATFileProcessor();
        // dATFileProcessor.PrepareDATFile();
        // Logger("Launching Turba");
        // TurbaConfig turbaConfig = new TurbaConfig();
        // turbaConfig.LaunchTurba();
        // Logger("Ending Turba");
        // // return;
        // ERGVerification ergVerification = new ERGVerification(excelPath);
        // ergVerification.ErgResultsCheck();

        // Logger("---------------------------");
        // Logger("Checking valve point.....");
        // ValvePointOptimizer valvePointOptimizer = new ValvePointOptimizer();
        // valvePointOptimizer.ValvePointOptimize();
        // PowerMatch powerMatch = new PowerMatch();
        
        // Logger("checkkkkkkkkkkkk POWER START!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        // powerMatch.CheckPower();

        // Logger("checkkkkkkkkkkkk POWER END!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    }

    public static void Main4(string[] args)
    {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            Console.WriteLine("started execution");
            
            
            GlobalHost = CreateHostBuilder(args).Build();
            IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            excelPath = configuration["AppSettings:ExcelFilePath"];
            

            if (StartKreisl.GlobalHost == null)
            {
                StartKreisl.FillGlobalHost();
            }
            StartKreisl.DeleteCONFiles();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            kreislDATHandler.RefreshKreislDAT();

        InitConfig();
        Console.WriteLine(excelPath);
            logger = GlobalHost.Services.GetRequiredService<ILogger>();
            logger.clear();
        Logger("STARTING THE TURBINE DESIGN");
        Logger("PressIn:" + TurbineDataModel.getInstance().InletPressure + ", " + "TempIn:" + TurbineDataModel.getInstance().InletTemperature + ", " + "FlowIn:" + TurbineDataModel.getInstance().MassFlowRate + ", " + "PressEx: " + TurbineDataModel.getInstance().ExhaustPressure);
        Logger("Searching efficiency from last projects..");
       

        //NormalizeData();


            HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();


                hBDPowerCalculator.HBDSetDefaultCustomerParams();
        // return;
                Logger("Nearest project is: "+ TurbineDataModel.getInstance().ClosestProjectName + " having Efficiency :" + TurbineDataModel.getInstance().TurbineEfficiency);
                hBDPowerCalculator.HBDUpdateEffGeneratorInit();
                hBDPowerCalculator.HBDPersistInitialPower();

                Logger("Selecting DAT file..");
                DatFileSelector datFileSelector = new DatFileSelector(excelPath);
                datFileSelector.ReferenceDATSelector();
              if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested )
              {
                 logger.moveLogs();
                return;
              }

            Logger("Generating Load Points..");
                LoadPointGen loadPoint = new LoadPointGen();
                loadPoint.GenerateLoadPoints();
                //If Ref DAT is not found then Don't attempt to write LPs. As of now..

            Logger("Start preparing DAT file...");
            DATFileProcessor dATFileProcessor = new DATFileProcessor();
            dATFileProcessor.PrepareDATFile();
            Logger("Launching Turba");
        //Logger("begfpre praneeetyhhhhhhhhhhhhhhhhhhh:" + Thread.CurrentThread.ManagedThreadId);
            TurbaConfig turbaConfig = new TurbaConfig();
            turbaConfig.LaunchTurba();
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
        {
            logger.moveLogs();
            return;
        }

        Logger("Ending Turba");
            // return;
            ERGVerification ergVerification = new ERGVerification();
            ergVerification.ErgResultsCheck();
            if (TurbineDesignPage.cts.IsCancellationRequested  || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return ;
            }
            Logger("---------------------------");
            Logger("Checking valve point.....");
            ValvePointOptimizer valvePointOptimizer = new ValvePointOptimizer();
            valvePointOptimizer.ValvePointOptimize();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested )
            {
                // logger.moveLogs();
                logger.moveLogs();
                return;
            }
            PowerMatch powerMatch = new PowerMatch();
            //Logger("aftererrerewwrewfwrgre praneeetyhhhhhhhhhhhhhhhhhhh:" + Thread.CurrentThread.ManagedThreadId);
            //Logger("checkkkkkkkkkkkk POWER START!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            powerMatch.CheckPower();
       

            //Logger("checkkkkkkkkkkkk POWER END!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            stopwatch.Stop();

                // Get the elapsed time as a TimeSpan value
                TimeSpan elapsedTime = stopwatch.Elapsed;

                // Display the elapsed time
                Console.WriteLine("Execution Time: " + elapsedTime.TotalMilliseconds + " ms");
            //}
            //catch (Exception ex)
            //{
            //    Logger(ex.ToString());
            //}

       


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

    public static void InitConfig(){
        NozzleTurbaDataModel.getInstance().fillNozzleTurbaDataModel();
        PowerEfficiencyModel.getInstance().fillPowerEfficiencyDataModel();
        PreFeasibilityDataModel.getInstance().fillPreFeasibilityData();
        LoadPointDataModel.getInstance().fillLoadPoints();
        TurbaOutputModel.getInstance().fillTurbaOutputDataList();

            
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();

        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        ////(66.69, 4.9, 10.36, 485, "wavexcel");
        //turbineDataModel.InletPressure = 66.69;//42.981;
        //turbineDataModel.ExhaustPressure = 4.9;//4.59;
        //turbineDataModel.MassFlowRate = 10.36;//8.93;
        //turbineDataModel.InletTemperature = 485;//440;
        turbineDataModel.GeneratorEfficiency = getGeneratorEff();
        turbineDataModel.LeakagePressure = 1.015;// 2.015;
        logger.LogInformation(Convert.ToString(turbineDataModel.InletPressure));
        FillInputValues();
    }

    public static void FillInputValues()
    {
        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        KreislDATHandler datHandler = new KreislDATHandler();
        string filePath = StartKreisl.filePath;
        datHandler.FillMassFlow(filePath, 5, turbineDataModel.MassFlowRate.ToString());
        datHandler.FillInletPressure(filePath, 5, turbineDataModel.InletPressure.ToString());
        datHandler.FillExhaustPressure(filePath, 4, turbineDataModel.ExhaustPressure.ToString());
        datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
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

    public static void moveRequiredLogs()
    {
        if(logger == null)
        {
            logger = new Logger();
        }
        logger.moveLogs();
    }
    public static void Logger(string message){
        logger.LogInformation(message);
    }
}


