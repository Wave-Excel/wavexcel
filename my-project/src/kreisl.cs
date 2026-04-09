using System;
using Interfaces.IERGHandlerService;
using Ignite_X.src.core.Services;
using StartExecutionMain;
using ERG_PowerMatch;
using ERG_ValvePointOptimizer;
using HMBD.LoadPointGenerator;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Turba.TurbaConfiguration;


using System;
using Models.NozzleTurbaData;
using Models.PowerEfficiencyData;
using Models.PreFeasibility;

using Microsoft.Extensions.Hosting;
using Interfaces.ILogger;
using ERG_Handler;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.IThermodynamicLibrary;
using Handlers.DAT_Handler;
using Services.ThermodynamicService;
using Utilities.Logger;
using HMBD.Ref_DAT_selector;
using HMBD.HMBDInformation;
using OfficeOpenXml;
using Models.LoadPointDataModel;
using ERG_Verification;
using Models.TurbineData;
using Models.TurbaOutputDataModel;
using Microsoft.Maui.ApplicationModel;
using Ignite_x_wavexcel;
using Ignite_X.src.core.Handlers;
using Ignite_X.src.core.Optimizers;
using Kreisl.KreislConfig;
using ExtraLoadPoints;
using Optimizers.ERG_NozzleOptimizer;

namespace StartKreislExecution;
public class StartKreisl
{
    public static IHost GlobalHost { get; set; }
    public static bool kreislKey = false;
    public static string excelPath;
    private static ILogger logger;
    private static IThermodynamicLibrary thermodynamicService;

    IERGHandlerService ergHandlerService;
    public static string filePath = "C:\\testDir\\kreisl.dat", ergFilePath = "C:\\testDir\\KREISL.erg";//"C:\\Users\\z00528mr\\Downloads\\checking_again\\KREISLa1.ERG";
        
    public StartKreisl()
    {
        ergHandlerService = StartKreisl.GlobalHost.Services.GetRequiredService<IERGHandlerService>();
        logger = StartKreisl.GlobalHost.Services.GetRequiredService<ILogger>();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
    }

    public static void FillGlobalHost()
    {
        GlobalHost = CreateHostBuilder(null).Build();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        StartExec.GlobalHost = GlobalHost;
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

        if(!Directory.Exists(directoryPath))
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
    public static void MainKreisL(string[] args)
    {
        try
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ResetNozzleCounter();
            GlobalHost = CreateHostBuilder(args).Build();
            IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            excelPath = configuration["AppSettings:ExcelFilePath"];
            StartExec.GlobalHost = GlobalHost;
            Console.WriteLine("Started Execution Using KreisL");

            logger = GlobalHost.Services.GetRequiredService<ILogger>();
            thermodynamicService = GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
            kreislKey = true;

            DeleteCONFiles();
            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            kreislDATHandler.RefreshKreislDAT();
            HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();

            thermodynamicService.FillClosestTurbineEfficiency();
            hBDPowerCalculator.GetTurbaCON(TurbineDataModel.getInstance().ClosestProjectID);
            InitConfig();

            KreislIntegration kreislIntegration = new KreislIntegration();
            kreislIntegration.LaunchKreisL();

            kreislDATHandler.RefreshKreislDAT();
            InitConfig();







            logger.clear();
            Logger("STARTING THE TURBINE DESIGN");
            Logger("PressIn:" + TurbineDataModel.getInstance().InletPressure + ", " + "TempIn:" + TurbineDataModel.getInstance().InletTemperature + ", " + "FlowIn:" + TurbineDataModel.getInstance().MassFlowRate + ", " + "PressEx: " + TurbineDataModel.getInstance().ExhaustPressure);
            Logger("Searching efficiency from last projects..");
            //Logger("Searching efficiency from last projects..");

            hBDPowerCalculator.HBDSetDefaultCustomerParamsKreisL();
            //hBDPowerCalculator.HBDUpdateEffGeneratorInit();
            //hBDPowerCalculator.HBDPersistInitialPower();//remove this no need to calculate gear losses

            Logger("Selecting DAT file..");
            DatFileSelector datFileSelector = new DatFileSelector(excelPath);
            datFileSelector.ReferenceDATSelector();
            //KreislDATHandler kreislDATHandler = new KreislDATHandler();

            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }

            Logger("Generating Load Points..");
            LoadPointGen loadPoint = new LoadPointGen();
            loadPoint.GenerateLoadPoints();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }

            Logger("Start preparing DAT file...");
            DATFileProcessor dATFileProcessor = new DATFileProcessor();
            dATFileProcessor.PrepareDATFile();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            Logger("Launching Turba");
            TurbaConfig turbaConfig = new TurbaConfig();
            turbaConfig.LaunchTurba();
            //double wheelChamberPress = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
            Logger("Ending Turba");
            ERGVerification ergVerification = new ERGVerification();
            ERGVerification.isCheckingLP5 = false;
            ergVerification.ErgResultsCheck();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            ResetNozzleCounter();
            UpdateLP5();
            ERGVerification.isCheckingLP5 = true;
            ergVerification.ErgResultsCheck();
            ResetNozzleCounter();


            //LoadPointDataModel loadPoint = LoadPointDataModel.getInstance();

            double wheelChamberPressure = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;

            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            Logger("---------------------------");
            Logger("Checking valve point.....");
            ValvePointOptimizer valvePointOptimizer = new ValvePointOptimizer();
            valvePointOptimizer.ValvePointOptimize();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            PowerMatch powerMatch = new PowerMatch();
            //double w = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;


            kreislDATHandler.FillVari40();
            turbaConfig.LaunchTurba();

            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            //double w1 = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
            kreislIntegration.RenameTurbaCON("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");


            kreislDATHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", Convert.ToString(wheelChamberPressure));

            powerMatch.CheckPower();
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            Logger("Execution Time: " + elapsedTime.TotalMilliseconds + " ms");

        }catch(Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    public static void ResetNozzleCounter()
    {
        NozzleOptimizer.Na = 0;
        NozzleOptimizer.nozzleOptimizeCount = 0;
        NozzleOptimizer.Nb = 0;
        NozzleOptimizer.A1 = 0;
        NozzleOptimizer.A2 = 0;
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

    public static void UpdateLP5()
    {
        double temp = TurbineDataModel.getInstance().InletTemperature - thermodynamicService.tsatvonp(TurbineDataModel.getInstance().InletPressure);
        if(temp>= 110)
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
        lpList[5].Rpm = 12000;
        lpList[5].InFlow = 0;
        lpList[5].BYP = -1;
        lpList[5].EIN = 0;
        lpList[5].WANZ = 0;
        lpList[5].RSMIN = 0;
    }
    public static void InitConfig()
    {
        NozzleTurbaDataModel.getInstance().fillNozzleTurbaDataModel();
        PowerEfficiencyModel.getInstance().fillPowerEfficiencyDataModel();
        PreFeasibilityDataModel.getInstance().fillPreFeasibilityData();
        LoadPointDataModel.getInstance().fillLoadPoints();
        TurbaOutputModel.getInstance().fillTurbaOutputDataList();


        


        logger = StartKreisl.GlobalHost.Services.GetRequiredService<ILogger>();

        TurbineDataModel turbineDataModel = TurbineDataModel.getInstance();
        //turbineDataModel.InletPressure = 41.91;// 66;// 65.7;// 66;// 41.19; //66.69;// 42.981;
        //turbineDataModel.ExhaustPressure = 4.903;// 6;// 5.3936575;// 6;// 4.903; //4.90;// 4.59;
        //turbineDataModel.MassFlowRate = 6.83;// 5.666667;// 8.33333333333333;// 5.666667;// 6.83; //10.36;// 8.93;
        //turbineDataModel.InletTemperature = 495.01;// 490;// 495;// 490;// 495.01; //485;// 440;
        //turbineDataModel.InletPressure = 42.981;
        //turbineDataModel.ExhaustPressure = 4.59;
        //turbineDataModel.MassFlowRate = 8.93;
        //turbineDataModel.InletTemperature = 440;
        turbineDataModel.GeneratorEfficiency = 95.8;
        turbineDataModel.LeakagePressure = 1.015;// 2.015;
        
        logger.LogInformation(Convert.ToString(turbineDataModel.InletPressure));
        FillInputValues();

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
        
        
        if(turbineDataModel.DeaeratorOutletTemp > 0)
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
                //datHandler.fillCapacity(filePath, 9, turbineDataModel.Capacity.ToString());
                if (turbineDataModel.MassFlowRate > 0)
                {
                    datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                }else if(turbineDataModel.AK25 > 0)
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
            if(turbineDataModel.PST < 120)
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
            datHandler.FillMassFlow(filePath, 5, Math.Round(turbineDataModel.MassFlowRate,3).ToString());
            datHandler.FillInletPressure(filePath, 5, Math.Round(turbineDataModel.InletPressure,3).ToString());
            datHandler.FillExhaustPressure(filePath, 4, Math.Round(turbineDataModel.ExhaustPressure,3).ToString());
            datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
        }
        using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var values = line.Split(',');
                Logger(values[0] + "=" + values[1]);
            }
        }
    }

    

    public static void moveRequiredLogs()
    {
        if (logger == null)
        {
            logger = new Logger();
        }
        logger.moveLogs();
    }
    public static void Logger(string message)
    {
        logger = StartKreisl.GlobalHost.Services.GetRequiredService<ILogger>();
        logger.LogInformation(message);
    }
}





/*
import pandas as pd
from sklearn.linear_model import Lasso
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_squared_error
from sklearn.preprocessing import StandardScaler, MinMaxScaler
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.ensemble import RandomForestRegressor
from sklearn.svm import SVR


def trainWithLasso(X_train, y_train, X_test):
    predictions = pd.DataFrame()
    scaler = MinMaxScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    for i in range(y_train.shape[1]):
        lasso = Lasso(alpha = 0.25)  
        lasso.fit(X_train_scaled, y_train.iloc[:, i])  
        y_pred = lasso.predict(X_test_scaled)  
        predictions[f'variable_{i + 5}'] = y_pred
    return predictions


def trainWithGradientBoosting(X_train, y_train, X_test, n_estimators=100, learning_rate=0.1, max_depth=3):
    predictions = pd.DataFrame()  
    scaler = MinMaxScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    for i in range(y_train.shape[1]):
        gb = GradientBoostingRegressor(n_estimators=n_estimators, learning_rate=learning_rate, max_depth=max_depth, random_state=42)  
        gb.fit(X_train_scaled, y_train.iloc[:, i]) 
        y_pred = gb.predict(X_test_scaled)  
        predictions[f'variable_{i + 5}'] = y_pred 
    return predictions  


def trainWithRandomForest(X_train, y_train, X_test, treeCount = 200):
    predictions = pd.DataFrame()
    for i in range(y_train.shape[1]):
        rf = RandomForestRegressor(n_estimators = treeCount, random_state = 42) 
        rf.fit(X_train, y_train.iloc[:, i])
        y_pred = rf.predict(X_test)
        predictions[f'variable_{i + 5}'] = y_pred 
    return predictions


def trainWithSVR(X_train, y_train, X_test):
    predictions = pd.DataFrame() 
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    for i in range(y_train.shape[1]):
        svr = SVR(kernel='rbf', C=1.0, epsilon=0.1)  
        svr.fit(X_train_scaled, y_train.iloc[:, i])
        y_pred = svr.predict(X_test_scaled)
        predictions[f'variable_{i + 5}'] = y_pred

    return predictions


def printPredictions(mlMethod, y_test, predictions):
  print(f'Method:  {mlMethod}')
  for i in range(y_test.shape[1]):
    mse = mean_squared_error(y_test.iloc[:, i], predictions.iloc[:, i])
    print(f'Mean Squared Error for variable {i + 5}: {mse}', end = '\n')

data = pd.read_csv('DataSetMLfromERG.csv').dropna(how='all')
for column in data.columns:
    data[column] = pd.to_numeric(data[column], errors='coerce') 

data.fillna(data.mean(), inplace=True)
data.to_csv('cleaned_dataset.csv', index=False)

X = data.iloc[:, 0:4]  
y = data.iloc[:, 4:19]  

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.15)#, random_state=42)

predictions = trainWithLasso(X_train, y_train, X_test) 
printPredictions('Lasso', y_test, predictions)

predictions = trainWithSVR(X_train, y_train, X_test) 
printPredictions('SVR', y_test, predictions)

predictions = trainWithRandomForest(X_train, y_train, X_test)
# for t in range(50,600,50):
t1 =300
predictions = trainWithRandomForest(X_train, y_train, X_test, 300)
printPredictions(f'Random Forest with {t1} trees', y_test, predictions)

for t in range(50,600,50):
  predictions = trainWithGradientBoosting(X_train, y_train, X_test, t)
  printPredictions(f'Gradient Boosting with {t} trees', y_test, predictions)
# predictions.to_csv('predictions.csv', index=False)
 */