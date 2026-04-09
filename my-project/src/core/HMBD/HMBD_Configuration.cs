using System.Runtime.InteropServices;
using Interfaces.ILogger;
using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Services.ThermodynamicService;
using Models.PreFeasibility;
using Kreisl.KreislConfig;
using Interfaces.IERGHandlerService;
// using TurbineUtils;
using StartKreislExecution;
using Ignite_X.src.core.Handlers;
using Models.ExecutedProjectDB;
using Ignite_x_wavexcel.Utilities;

namespace HMBD.HMBDInformation;

public class HBDPowerCalculator
{
    public static string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
    ExcelPackage package;
    private IThermodynamicLibrary thermodynamicService;
    private IConfiguration configuration;
    private IERGHandlerService eRGHandlerService;
    private ILogger logger;

    TurbineDataModel turbineDataModel;
    PreFeasibilityDataModel preFeasibilityDataModel;

    public HBDPowerCalculator(){
        configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        FileInfo existingFile = new FileInfo(excelPath);
        package = new ExcelPackage(existingFile);
        if(StartKreisl.GlobalHost == null)
        {
            StartKreisl.FillGlobalHost();
        }
        thermodynamicService = StartKreisl.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        eRGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        turbineDataModel = TurbineDataModel.getInstance();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
    }
    public ExcelWorksheet GetWorksheet(string sheetName)
    {
        var worksheet = package.Workbook.Worksheets[sheetName]; 
        if (worksheet != null) // Check if the worksheet exists
        {
            return worksheet;
        }
        else
        {
            Console.WriteLine("The 'Turbine' sheet does not exist.");
            return null; 
        }
        return null;
    }
    public void GetHMBDPower(double csInletPressure, double csInletTemp, double csInletMassFlow, double csBackPressure, double turEfficiency)
    {
        
    }
    public string GetTurbaCON(string projID)
    {
        string projectDatFile = "";
        ExecutedDB executedDB = ExecutedDB.getInstance();
        executedDB.fillExecutedDB();
        List<ExecutedProject> db = executedDB.ExecutedProjectDB;
        for (int row = 0; row < db.Count; ++row)
        {
            if (db[row].ProjectID.Contains(projID))
            {
                projectDatFile = db[row].DatFilePath; //projectMaster.Cells[row, "BJ"].Value.ToString();
                break;
            }
        }

        string directoryPath = Path.GetDirectoryName(projectDatFile);
        string[] conFiles = Directory.GetFiles(directoryPath, "*.CON");
        if (conFiles.Length > 0)
        {
            if (IsFileReadyForOpen(conFiles[0]))
            {
                logger.LogInformation("Server is up and files are available...");
                File.Copy(conFiles[0], "C:\\testDir\\TURBA.CON", true);
            }
            else
            {
                logger.LogInformation("!Server is down files are unavailable...");
            }
        }
        else
        {
            logger.LogInformation("No .CON files found in the specified directory.");
        }
        return "";
    }
    public bool IsFileReadyForOpen(string path)
    {
        try
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
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

    public void HBDSetDefaultCustomerParamsKreisL()
    {
        // fill kreisl.dat file-> DONE BEFORE ONLY

        thermodynamicService.FillClosestTurbineEfficiency();
        //logger.LogInformation("Closest Project: " + turbineDataModel.ClosestProjectName +" , ID: "+ turbineDataModel.ClosestProjectID);
        GetTurbaCON(turbineDataModel.ClosestProjectID);
        
        KreislDATHandler kreislDATHandler = new KreislDATHandler();

        kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", turbineDataModel.TurbineEfficiency.ToString());
        //kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "1", turbineDataModel.TurbineEfficiency.ToString());

        KreislIntegration kreislConfig = new KreislIntegration();
        kreislConfig.LaunchKreisL();
        
        //read erg fill power, volflow
        preFeasibilityDataModel.PowerActualValue = eRGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath);
        turbineDataModel.AK25 = preFeasibilityDataModel.PowerActualValue;
        //Fill Efficiencies based on AK25
        if(turbineDataModel.DeaeratorOutletTemp > 0)
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERGForPRV(StartKreisl.ergFilePath);
        }
        else if (turbineDataModel.PST > 0)
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERGForDesuparator(StartKreisl.ergFilePath);
        }
        else
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERG(StartKreisl.ergFilePath);

        }
        turbineDataModel.VolumetricFlow = preFeasibilityDataModel.InletVolumetricFlowActualValue;
        logger.LogInformation("PreFeasibility Power Value: " + preFeasibilityDataModel.PowerActualValue);
        logger.LogInformation("PreFeasibility Vol Flow Value: " + preFeasibilityDataModel.InletVolumetricFlowActualValue);
        HBDFUpdatePreFeasibility("KreisL");
    }

    public void HBDSetDefaultCustomerParams()
    {

        
        //Need tp remove
        thermodynamicService.PerformCalculations();
        // Turbine.Main1();//write it in ThermoService
        turbineDataModel.AK25 = thermodynamicService.getPowerFromClosestEfficiency(turbineDataModel.AK25, turbineDataModel.GeneratorEfficiency);
        HBDFUpdatePreFeasibility();
    }
    public void HBDUpdateEff(double EffValue)
    {
        turbineDataModel.AK25 = thermodynamicService.getPowerFromTurbineEfficiency(EffValue);
        turbineDataModel.FinalPower = turbineDataModel.AK25;//Convert.ToDouble(HBD.Cells["AK25"].Value);
    }
    public void HBDUpdateEffGenerator(double EffValue)
    {
        turbineDataModel.GeneratorEfficiency = EffValue / 100;
    }
    public void HBDUpdateEffGeneratorInit()
    {
        
    }
    public void HBDPersistInitialPower()
    {
        turbineDataModel.GearLosses = thermodynamicService.CalculateGearLosses();
    }
    public void HBDFUpdatePreFeasibility(string kreislCheck = "")
    {
        // Pre-Feasibility 1
        preFeasibilityDataModel.InletPressureActualValue = turbineDataModel.InletPressure;
        
        preFeasibilityDataModel.TemperatureActualValue = turbineDataModel.InletTemperature;
        preFeasibilityDataModel.BackpressureActualValue = turbineDataModel.ExhaustPressure;
        if (kreislCheck != "")
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = turbineDataModel.VolumetricFlow;
        }
        else
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = thermodynamicService.getVolumetricFlow();
        }
        preFeasibilityDataModel.PowerActualValue = turbineDataModel.AK25;

        // Pre-Feasibility 2
        preFeasibilityDataModel.InletPressureActualValue_2 = turbineDataModel.InletPressure;
        preFeasibilityDataModel.TemperatureActualValue_2 = turbineDataModel.InletTemperature;
        preFeasibilityDataModel.BackpressureActualValue_2 = turbineDataModel.ExhaustPressure;
        preFeasibilityDataModel.InletVolumetricFlowActualValue_2 = turbineDataModel.VolumetricFlow;
        preFeasibilityDataModel.PowerActualValue_2 = turbineDataModel.AK25;
        
    }
}

