using System;
using Handlers.DAT_Handler;
using Optimizers.ERG_NozzleOptimizer;
using OfficeOpenXml;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Models.TurbaOutputDataModel;
using Interfaces.ILogger;
using Turba.TurbaConfiguration;
using HMBD.Ref_DAT_selector;
using Models.PreFeasibility;
using DocumentFormat.OpenXml.VariantTypes;
using HMBD.LoadPointGenerator;
using DocumentFormat.OpenXml.Spreadsheet;
using Ignite_x_wavexcel;
using System.Diagnostics;
using static Microsoft.Maui.ApplicationModel.Permissions;
using StartKreislExecution;
namespace ERG_Verification;
public class ERGVerification
{
    public static string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
    private IConfiguration configuration;
    private TurbaOutputModel turbaOutputModel;
    private PreFeasibilityDataModel preFeasibilityModel;
    private ILogger logger;
    int maxLoadPoints = 10;
    public static bool isCheckingLP5;
    public ERGVerification()
    {
        turbaOutputModel = TurbaOutputModel.getInstance();
        preFeasibilityModel = PreFeasibilityDataModel.getInstance();
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        maxLoadPoints = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
       
    }

    public void ErgResultsCheck(int maxlPS = 0)
    {
        try
        {
            if (ErgCheckExhaustVolumetricFlow(maxlPS))
            {
                if (ErgCheck_NozzlesSection(maxlPS))
                {
                    if (ErgCheckDetaTGBCWheelChamberPTBending(maxlPS))
                    {
                        if (ErgCheckThrustValue(maxlPS))
                        {
                            if (ErgCheck_LoadPoints(maxlPS))
                            {
                                Console.WriteLine("Turbine looks great !! Let's Compare Power With HBD..");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TurbineDesignPage.cts.Cancel();
            logger.LogError("ERGResultsCheck", ex.StackTrace);
        }
    }
    private bool ErgCheck_NozzlesSection(int maxlPS = 0)
    {
        NozzleOptimizer nozzleOptimizer = new NozzleOptimizer();
        if (nozzleOptimizer.RuleEngineAlgorithmForNozzles(maxlPS))// access from another file
        {
            Console.WriteLine("Nozzles Optimized...");
            return true;
        }
        else
        {
            Console.WriteLine("Nozzles Couldn't be Optimized...");
            return false;
        }
    }
    private bool ErgCheck_LoadPoints(int maxLps = 0)
    {
        bool overallPressureStatus = (turbaOutputModel.Stage_Pressure_Check == "TRUE") ? true : false;//(bool)turbaResults_ERG.Cells["O3"].Value;
        bool ergCheck_LoadPoints = false;
        bool turbaRun = false;
        if (overallPressureStatus)
        {
            Logger("Stages pressure are in limit For all load points..");
            goto CheckPower;
        }

        string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;//turbaResults_ERG.Cells["O10"].Value.ToString();
        // Console.WriteLine("mcrrrrrrrrrrrrrrrrrrstatsus" + mcr1Status);
        string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;//turbaResults_ERG.Cells["O11"].Value.ToString();
        string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;
        // Console.WriteLine("highBpStatusssssssssssssssssssssssssssssss" + highBpStatus);//turbaResults_ERG.Cells["O5"].Value.ToString();
        if (mcr2Status == "FALSE" || mcr1Status == "FALSE")
        {
            Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
            LoadPointGen loadPoint = new LoadPointGen();
            loadPoint.LoadPointGenerator_IncMassFlow(7, 5); // B11 = LP7
            loadPoint.LoadPointGenerator_IncMassFlow(8, 5); // B12 = LP8
            turbaRun = true;
            ergCheck_LoadPoints = false;
        }
        if (highBpStatus == "FALSE")
        {
            ergCheck_LoadPoints = false;
            LoadPointGen loadPoint = new LoadPointGen();
            Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
            // Console.WriteLine("Pressure Failing at High Back Pressure ... Need To Reduce BP");
            loadPoint.LoadPointGenerator_ReduceBP(2, 2);
            turbaRun = true;

        }
        if (turbaRun)
        {
            DATFileProcessor dATFileProcessor = new DATFileProcessor();
            dATFileProcessor.PrepareDATFile_OnlyLPUpdate(maxLps);
            TurbaConfig turbaConfig = new TurbaConfig();
            turbaConfig.LaunchTurba(maxLps);
            StartKreisl.ResetNozzleCounter();
            ErgResultsCheck(maxLps);
            return ergCheck_LoadPoints;
        }
    CheckPower:

        double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q10"].Value);
        double basePower = turbaOutputModel.OutputDataList[0].Power_KW;//Convert.ToDouble(turbaResults_ERG.Cells["Q2"].Value);
        List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
        for (int i = 1; i <= 5; ++i)
        {
            double loadPointPower = lpList[i].Power_KW;
            if (loadPointPower < mcrPower)
            {
                Logger("MCR Failed. MCR kW > Base LP Power");
                TerminateIgniteX("ergCheck_LoadPoints");
                return false;
            }
            else
            {
                Logger("MCR passed. MCR kW < Base LP Power");
                ergCheck_LoadPoints = true;
                turbaOutputModel.Check_Power_KW = "TRUE";
            }
        }
        return ergCheck_LoadPoints;
    }
    public void TerminateIgniteX(string name)
    {
        TurbineDesignPage.cts.Cancel();
        Console.Write(name + " End");
    }
    public bool ErgCheckThrustValue(int maxlPS = 0)
    {
        bool ergCheckThrustValue = true;
        TurbaConfig turbaConfig = new TurbaConfig();
        turbaConfig.LaunchRsmin();
        // it was informed by anam 1.4 to 0.8 
        double thrustUpperLim = turbaOutputModel.ThrustLimit;
        Debug.WriteLine("thru" + thrustUpperLim);
        
        // var loadPointThrust = turbaResults_ERG.Cells["P4:P13"];
        string logThrust = "";
        List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
        for (int i = 1; i <= maxLoadPoints; ++i)
        {
            double thrust = lpList[i].Thrust;//lpList[Convert.ToDouble(turbaResults_ERG.Cells[rowCount, 16].Value);
            logThrust += " | " + thrust;
            if (thrust >  thrustUpperLim)
            {
                ergCheckThrustValue = false;
                Logger("Thrust: " + logThrust);
                Logger("Thrust check Failed....");
                // turbaResults_ERG.Cells["P3"].Value = false;
                turbaOutputModel.Check_Thrust = "FALSE";
                return ergCheckThrustValue;
            }
        }

        Logger("Thrust: " + logThrust);
        if (ergCheckThrustValue)
        {
            Logger("Thrust check Passed....");
            // turbaResults_ERG.Cells["P3"].Value = true;
            turbaOutputModel.Check_Thrust = "TRUE";
        }
        else
        {
            Logger("Thrust check Failed....");
            // turbaResults_ERG.Cells["P3"].Value = false;
            turbaOutputModel.Check_Thrust = "FALSE";
            TerminateIgniteX("ergCheck_thrustValue");
        }
        return ergCheckThrustValue;
    }
    public void Logger(string message)
    {
        logger.LogInformation(message);
    }
    public bool ErgCheckDetaTGBCWheelChamberPTBending(int maxlPS = 0)
    {
        bool ergCheckDetaTGBCWheelChamberPTBending = false;
        bool datChanged = false;
        double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//Convert.ToDouble(turbaResults_ERG.Cells["E2"].Value);
        double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["F31"].Value);
        Logger("Delta T: " + deltaT + " UpperLimit: " + deltaTupperLim);
        double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure; //Convert.ToDouble(turbaResults_ERG.Cells["F2"].Value);
        double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit; //Convert.ToDouble(turbaResults_ERG.Cells["G31"].Value);
        double wheelchamberPupperLim_2 = turbaOutputModel.WheelchamberP_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["G32"].Value);
        Logger("Wheel Chamber Pressure: " + wheelchamberP + ", Limits: " + wheelchamberPupperLim + " " + wheelchamberPupperLim_2);
        double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(turbaResults_ERG.Cells["G2"].Value);
        double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H30"].Value);
        double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["H31"].Value);
        double wheelchamberTupperLim_2 = turbaOutputModel.WheelchamberT_UpperLimit2;//Convert.ToDouble(turbaResults_ERG.Cells["H32"].Value);
        Logger("Wheel Chamber Temp: " + wheelchamberT + ", Limits: " + wheelchamberTlowerLim + " " + wheelchamberTupperLim + " " + wheelchamberTupperLim_2);
        List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
        // var loadPointBendings = turbaResults_ERG.Cells["N4:N8"];
        string bending = "";
        bool bendingStatus = true;
        // Console.WriteLine("BSDJISBNDJSKBFKJSBFJKSBFasdhjfbshjdbfgajhsbdfgjdbfgjbsdfjgbajdfgb :" + lpList[0].Bending);
        // for(int i = 1; i <= 5; ++i){
        for (int lp = 1; lp <= 5; ++lp)
        {
            bending += turbaOutputModel.OutputDataList[lp].Bending;
        }
        if (!string.IsNullOrEmpty(bending))
        {
            bendingStatus = false;
            // break;
        }
        // }
        // bendingStatus = bending == ""; // we removed if else here
        Logger(bendingStatus ? "Bending check Passed.." : "Bending check Failed..");
        turbaOutputModel.BendingCheck = (bendingStatus == true) ? "TRUE" : "FALSE";
        // turbaResults_ERG.Cells["N3"].Value = bendingStatus;
        bool deltaTStatus = deltaT <= deltaTupperLim;
        Logger(deltaTStatus ? "deltaT GBC check Passed.." : "deltaT GBC check Failed..");
        turbaOutputModel.Check_DELTA_T = (deltaTStatus == true) ? "TRUE" : "FALSE";
        if (!isCheckingLP5)
        {
            if (!bendingStatus || !deltaTStatus)
            {
                DatFileSelector datFileSelector = new DatFileSelector(excelPath);
                string path =  datFileSelector.findNextVariant(preFeasibilityModel.Variant,maxlPS);
                if (!string.IsNullOrEmpty(path))
                {
                    TurbaConfig turbaConfig = new TurbaConfig();
                    datFileSelector.CopyRefDATFile(path);
                    DATFileProcessor dATFileProcessor = new DATFileProcessor();
                    dATFileProcessor.PrepareDATFile(maxlPS);
                    // PrepareDATFile();
                    turbaConfig.LaunchTurba(maxlPS);
                    // LaunchTurba();
                    ErgResultsCheck(maxlPS);
                }
                else if(string.IsNullOrEmpty(path))
                {
                    Logger("Selecting New flow path from executed projects..");
                    Logger("stopping Execution : WIP Executed FLow path");
                    MainExecutedClass executedFlow = new MainExecutedClass();
                    executedFlow.GotoBCD1120(maxlPS);
                    // TerminateIgniteX("ergCheck_detaTGBCwheelchamberPTbending");
                    
                }
                return false;

            }
        }else
        {
            if (!bendingStatus)
            {
                Logger("Selecting New flow path from executed projects..");
                Logger("stopping Execution : WIP Executed FLow path");
                MainExecutedClass executedFlow = new MainExecutedClass();
                executedFlow.GotoBCD1120(maxlPS);
                // TerminateIgniteX("ergCheck_detaTGBCwheelchamberPTbending");
                return false;
            }
        }
        
        bool P_executedPath = false, P_standardPath = false;
        bool T_executedPath = false, T_standardPath = false;
        bool wheelchamberPStatus = CheckWheelChamberPressure(wheelchamberP, wheelchamberPupperLim, wheelchamberPupperLim_2, ref P_executedPath, ref P_standardPath);
        bool wheelchamberTStatus = CheckWheelChamberTemperature(wheelchamberT, wheelchamberTlowerLim, wheelchamberTupperLim, wheelchamberTupperLim_2, ref T_executedPath, ref T_standardPath);
        if (P_executedPath || T_executedPath)
        {
            DatFileSelector datFileSelector = new DatFileSelector(excelPath);
            string path = datFileSelector.findNextVariant(preFeasibilityModel.Variant,maxlPS);
            if (!string.IsNullOrEmpty(path))
            {
                TurbaConfig turbaConfig = new TurbaConfig();
                datFileSelector.CopyRefDATFile(path);
                DATFileProcessor dATFileProcessor = new DATFileProcessor();
                dATFileProcessor.PrepareDATFile(maxlPS);
                // PrepareDATFile();
                StartKreisl.ResetNozzleCounter();
                turbaConfig.LaunchTurba(maxlPS);
                // LaunchTurba();

                ErgResultsCheck(maxlPS);
            }
            else if (string.IsNullOrEmpty(path))
            {
                Logger("Selecting New flow path from executed projects..");
                Logger("stopping Execution : WIP Executed FLow path");
                MainExecutedClass executedFlow = new MainExecutedClass();
                executedFlow.GotoBCD1120(maxlPS);
            }
            return false;
        }
        if (P_standardPath || T_standardPath)
        {
            Logger("Selecting New flow path from standard variants..");
            DatFileSelector datFileSelector = new DatFileSelector(excelPath);
            string path = datFileSelector.SelectStandard("NextLarger");
            if (path.ToLower().Contains("no dat found"))
            {
                Logger("Stopping Execution : End of higher variant selection");
                MainExecutedClass executedFlow = new MainExecutedClass();
                executedFlow.GotoBCD1120(maxlPS);
                // TerminateIgniteX("ergCheck_detaTGBCwheelchamberPTbending");
                return false;
            }
            datFileSelector.CopyRefDATFile(path);
            datChanged = true;
        }
        if (datChanged)
        {
            DATFileProcessor dATFileProcessor = new DATFileProcessor();
            dATFileProcessor.PrepareDATFile(maxlPS);
            TurbaConfig turbaConfig = new TurbaConfig();
            turbaConfig.LaunchTurba(maxlPS);
            ErgResultsCheck(maxlPS);
            return false;
        }
        return true;
    }
    
    private bool CheckWheelChamberPressure(double wheelchamberP, double wheelchamberPupperLim, double wheelchamberPupperLim_2, ref bool P_executedPath, ref bool P_standardPath)
    {
        bool wheelchamberPStatus = true;
        bool wheelchamberP_executedPath = false;
        bool wheelchamberP_standardPath = false;
        if (wheelchamberP > wheelchamberPupperLim_2)
        {
            wheelchamberP_executedPath = true;
            Logger("wheelchamber P check Failed..");
            turbaOutputModel.Check_Wheel_Chamber_Pressure = "FALSE";
            // turbaResults_ERG.Cells["F3"].Value = false;
            wheelchamberPStatus = false;
        }
        else if (wheelchamberP > wheelchamberPupperLim)
        {
            wheelchamberP_standardPath = true;
            Logger("wheelchamber P check Failed..");
            turbaOutputModel.Check_Wheel_Chamber_Pressure = "FALSE";
            // turbaResults_ERG.Cells["F3"].Value = false;
            wheelchamberPStatus = false;
        }
        else
        {
            Logger("wheelchamber P check Passed..");
            turbaOutputModel.Check_Wheel_Chamber_Pressure = "TRUE";
            // turbaResults_ERG.Cells["F3"].Value = true;
        }
        P_executedPath = wheelchamberP_executedPath;
        P_standardPath = wheelchamberP_standardPath;
        return wheelchamberPStatus;
    }
    private bool CheckWheelChamberTemperature(double wheelchamberT, double wheelchamberTlowerLim, double wheelchamberTupperLim, double wheelchamberTupperLim_2, ref bool T_executedPath, ref bool T_standardPath)
    {
        bool wheelchamberTStatus = true;
        bool wheelchamberT_executedPath = false;
        bool wheelchamberT_standardPath = false;
        if (wheelchamberT > wheelchamberTupperLim_2)
        {
            wheelchamberT_executedPath = true;
            Logger("wheelchamber Temp check Failed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";

            // turbaResults_ERG.Cells["G3"].Value = false;
            wheelchamberTStatus = false;
        }
        else if (wheelchamberT > wheelchamberTupperLim)
        {
            wheelchamberT_standardPath = true;
            Logger("wheelchamber Temp check Failed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
            // turbaResults_ERG.Cells["G3"].Value = false;
            wheelchamberTStatus = false;
        }
        else if (wheelchamberT > wheelchamberTlowerLim)
        {
            Logger("wheelchamber Temp check Passed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "TRUE";
            // turbaResults_ERG.Cells["G3"].Value = true;
        }
        else
        {
            wheelchamberT_standardPath = true;
            Logger("wheelchamber Temp check Failed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
            // turbaResults_ERG.Cells["G3"].Value = false;
            wheelchamberTStatus = false;
        }
        T_executedPath = wheelchamberT_executedPath;
        T_standardPath = wheelchamberT_standardPath;
        return wheelchamberTStatus;
    }
    public bool ErgCheckExhaustVolumetricFlow(int maxlPS = 0)
    {
        bool ergCheckExhaustVolumetricFlow = false;
        double volFlow = turbaOutputModel.OutputDataList[0].Vol_Flow; //Convert.ToDouble(turbaResults_ERG.Cells["M2"].Value);
        Logger("Volumetric Flow: " + volFlow);
        int currentFlowPathNo = preFeasibilityModel.Variant; //Convert.ToInt32(currentFlowPath.Cells["G15"].Value);
        double volFlowUpperLimit;
        if (currentFlowPathNo < 3)
        {
            volFlowUpperLimit = turbaOutputModel.VolFlow_LowerLimit; //Convert.ToDouble(turbaResults_ERG.Cells["N30"].Value);
            Logger("Volumetric Flow limit: " + volFlowUpperLimit);
            if (volFlow > volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow = "FALSE";
                // turbaResults_ERG.Cells["M3"].Value = false;
                Logger("Opt2 VolFlow failed, Selecting project from Opt1..");
                DatFileSelector datFileSelector = new DatFileSelector(excelPath);
                string path = datFileSelector.SelectStandard("Opt2");
                Logger("Variant selected: " + preFeasibilityModel.Variant.ToString() + ", path: " + path);
                TurbaConfig turbaConfig = new TurbaConfig();
                //Ref_DAT_selector ref_DAT_Selector = new Ref_DAT_selector();
                //DatFileSelector datFileSelector = new DatFileSelector(excelPath);
                datFileSelector.CopyRefDATFile(path);
                DATFileProcessor dATFileProcessor = new DATFileProcessor();
                dATFileProcessor.PrepareDATFile(maxlPS);
                // PrepareDATFile();
                turbaConfig.LaunchTurba(maxlPS);
                // LaunchTurba();
                ErgResultsCheck(maxlPS);
                return false;
            }
            else
            {
                turbaOutputModel.CheckVolFlow = "TRUE";
                // turbaResults_ERG.Cells["M3"].Value = true;
                Logger("volFlow is in acceptable Cells..");
                return true;
            }
        }
        else //>=3
        {
            volFlowUpperLimit = turbaOutputModel.VolFlow_UpperLimit;//Convert.ToDouble(turbaResults_ERG.Cells["N31"].Value);
            Logger("Volumetric Flow limit: " + volFlowUpperLimit);
            if (volFlow > volFlowUpperLimit)
            {
                turbaOutputModel.CheckVolFlow = "FALSE";
                // turbaResults_ERG.Cells["M3"].Value = false;
                Logger("Opt1 VolFlow failed, Design requires at least 2GBC..");

                Console.WriteLine("Terminating execution, Opt1 VolFlow failed, Design requires at least 2GBC..");
                TurbineDesignPage.cts.Cancel();

                return false;

            }
            else
            {
                turbaOutputModel.CheckVolFlow = "TRUE";
                // turbaResults_ERG.Cells["M3"].Value = true;
                Logger("volFlow is in acceptable Cells..");
                return true;
            }
        }
    }
}
