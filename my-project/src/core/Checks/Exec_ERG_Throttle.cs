using System;
using System.Collections.Generic;
using System;
using StartExecutionMain;
using Models.TurbaOutputDataModel;
using Models.PreFeasibility;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Turba.Exec_TurbaConfig;
using HMBD.Exec_LoadPointGenerator;
using Handlers.DAT_Handler;
using HMBD.Exec_CW_Curve;
using HMBD.Exec_Exhaust_Curve;
using Handlers.Exec_DAT_Handler;

namespace Exec_ERG_Throttle;
public class ERGResultsChecker
{
    // private Application excelApp;
    // private Workbook workbook;
    private TurbaOutputModel turbaOutputModel;
    private IConfiguration configuration;
    private PreFeasibilityDataModel preFeasibilityModel;
    // private volFlowUpperLimit;
    ILogger logger;

    private int lpCount;
    public ERGResultsChecker()
    {
        // excelApp = new Application();
        // workbook = excelApp.Workbooks.Open(workbookPath);
        turbaOutputModel = TurbaOutputModel.getInstance();
        preFeasibilityModel = PreFeasibilityDataModel.getInstance();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
         .Build();
        //  volFlowUpperLimit =  configuration.GetValue<double>("AppSettings:volFlowUpperLimit");
         logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
         lpCount = configuration.GetValue<Int32>("AppSettings:LP_COUNT");
    }

    public void ERGResultsCheckThrottle()
    {
        if (ERGCheckExhaustVolumetricFlowThrottle())
        {
            if (ERGCheckDeltaTGBCWheelChamberPTBendingThrottle())
            {
                if (ERGCheckThrustValueThrottle())
                {
                    if (ERGCheckLoadPointsThrottle())
                    {
                        if (ERGCheckExhaustThrottle())
                        {
                            Console.WriteLine("Turbine looks great !! Let's Compare Power With HBD..");
                            Logger("Comparing Power With HBD..");
                        }
                    }
                }
            }
        }
        else
        {
            return;
        }
    }

    private bool ERGCheckExhaustThrottle()
    {
        // Worksheet turbaResultsERG = workbook.Sheets["Output"];
        // Worksheet currentFlowPath = workbook.Sheets["Pre-Feasibility checks"];
        bool result = false;

        if ((double)turbaOutputModel.OutputDataList[0].DELTA_T <= 100)
        {
            if (Exhaust1((double)preFeasibilityModel.TemperatureActualValue, (double)preFeasibilityModel.BackpressureActualValue))
            {
                result = true;
                Logger("Exhaust Check Passed...");
                Logger("Exhaust Check Passed...");
            }
            else
            {
                Logger("Check Failed, Exhaust not optimal...");
                Logger("Check Failed, Exhaust not optimal...");
            }
        }
        else if ((double)turbaOutputModel.OutputDataList[0].DELTA_T<= 200 && (double)turbaOutputModel.OutputDataList[0].DELTA_T > 100)
        {
            if (Exhaust2((double)preFeasibilityModel.TemperatureActualValue, (double)preFeasibilityModel.BackpressureActualValue))
            {
                result = true;
                Logger("Exhaust Check Passed...");
                Logger("Exhaust Check Passed...");
            }
            else
            {
                Logger("Check Failed, Exhaust not optimal...");
                Logger("Check Failed, Exhaust not optimal...");
            }
        }
        else
        {
            Logger("Delta T is out of Range (>200)");
            Logger("Delta T is out of Range (>200)");
        }

        return result;
    }

        private bool ERGCheckLoadPointsThrottle()
        {
            // Worksheet turbaResultsERG = workbook.Sheets["Output"];
            bool result = false;
            bool turbaRun = false;

            bool overallPressureStatus = (turbaOutputModel.Stage_Pressure_Check=="TRUE")?true:false;//(bool)turbaResultsERG.Range["O3"].Value;
            if (overallPressureStatus)
            {
                Logger("Stages pressure are in limit for all load points..");
                goto CheckPower;
            }

            string mcr1Status = turbaOutputModel.OutputDataList[7].Stage_Pressure;//turbaResultsERG.Range["O10"].Value.ToString();
            string mcr2Status = turbaOutputModel.OutputDataList[8].Stage_Pressure;//turbaResultsERG.Range["O11"].Value.ToString();
            string highBpStatus = turbaOutputModel.OutputDataList[2].Stage_Pressure;//turbaResultsERG.Range["O5"].Value.ToString();

            if (mcr2Status == "False" || mcr1Status == "False")
            {
                result = false;
                Logger("MCR 1 2 Pressure Failed ... Increasing mass flow");
                LoadPointGeneratorIncMassFlow(7, 5);
                LoadPointGeneratorIncMassFlow(8, 5);
                turbaRun = true;
            }

            if (highBpStatus == "False")
            {
                result = false;
                Logger("Pressure Failing at High Back Pressure ... Need To Reduce BP");
                LoadPointGeneratorReduceBP(2, 2);
                turbaRun = true;
            }

            if (turbaRun)
            {
                PrepareDATFileOnlyLPUpdate();
                LaunchTurba();
                ERGResultsCheckThrottle();
                return result;
            }

        CheckPower:
            double mcrPower = turbaOutputModel.OutputDataList[7].Power_KW;//(double)turbaResultsERG.Range["Q10"].Value;
            double basePower = turbaOutputModel.OutputDataList[0].Power_KW;//(double)turbaResultsERG.Range["Q2"].Value;
            // Range cellRange = turbaResultsERG.Range["Q4:Q8"];
            List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;

            for(int i = 1; i <= 5; ++i)
            {
                double loadPointPower = lpList[i].Power_KW;

                if (loadPointPower < mcrPower)
                {
                    result = false;
                    Logger("Base power is less than MCR Case.. Going to Custom Path");
                    return result;
                }
                else
                {
                    Logger("Base power greater than MCR Case..");
                    result = true;
                    turbaOutputModel.Check_Power_KW = "TRUE";
                    return result;
                }
            }
            return result;
            }
        
        
    

    private bool ERGCheckThrustValueThrottle()
    {
        bool result = false;
        LaunchRsmin();
         // it was informed by anam 1.4 to 0.8 
        double thrustUpperLim = 0.8;
        List<OutputLoadPoint> lpList = turbaOutputModel.OutputDataList;
        // Worksheet turbaResultsERG = workbook.Sheets["Output"];
        // Range loadPointThrust = turbaResultsERG.Range["P4:P13"];
        
        for(int i=1;i<=lpCount;i++)
        // foreach (Range cell in loadPointThrust.Cells)
        {
            double thrust = (double)lpList[i].Thrust;
            if (thrust > thrustUpperLim)
            {
                result = false;
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust="FALSE";//turbaResultsERG.Range["P3"].Value = false;
                return result;
            }
            else
            {
                result = true;
            }
        }

        if (result)
        {
            Logger("Thrust check Passed....");
            turbaOutputModel.Check_Thrust = "TRUE";
        }
        else
        {
            Logger("Thrust check Failed....");
            turbaOutputModel.Check_Thrust = "FALSE";
        }

        return result;
    }

    private bool ERGCheckDeltaTGBCWheelChamberPTBendingThrottle()
    {
        bool result = false;
        bool datChanged = false;
        // Worksheet turbaResultsERG = workbook.Sheets["Output"];
        // Worksheet currentFlowPath = workbook.Sheets["Pre-Feasibility checks"];
        int currentFlowPathNo = preFeasibilityModel.Variant;

        double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//(double)turbaResultsERG.Range["E2"].Value;
        double deltaTupperLim = turbaOutputModel.DeltaT_UpperLimit;//(double)turbaResultsERG.Range["F31"].Value;

        double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;//(double)turbaResultsERG.Range["F2"].Value;
        double wheelchamberPupperLim = turbaOutputModel.WheelchamberP_UpperLimit;//(double)turbaResultsERG.Range["G31"].Value;
        double wheelchamberPupperLim2 = turbaOutputModel.WheelchamberP_UpperLimit2;//(double)turbaResultsERG.Range["G32"].Value;

        double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//(double)turbaResultsERG.Range["G2"].Value;
        double wheelchamberTlowerLim = turbaOutputModel.WheelchamberT_LowerLimit;//(double)turbaResultsERG.Range["H30"].Value;
        double wheelchamberTupperLim = turbaOutputModel.WheelchamberT_UpperLimit;//(double)turbaResultsERG.Range["H31"].Value;
        double wheelchamberTupperLim2 = turbaOutputModel.WheelchamberT_UpperLimit2;//(double)turbaResultsERG.Range["H32"].Value;

        string bending = turbaOutputModel.OutputDataList[0].Bending;//turbaResultsERG.Range["N2"].Value.ToString();

        bool bendingStatus = string.IsNullOrEmpty(bending);
        turbaOutputModel.BendingCheck=(bendingStatus==true)?"TRUE":"FALSE";//turbaResultsERG.Range["N3"].Value = bendingStatus;
        Logger(bendingStatus ? "Bending check Passed.." : "Bending check Failed..");

        bool deltaTStatus = deltaT <= 500;
        turbaOutputModel.Check_DELTA_T=(deltaTStatus==true)?"TRUE":"FALSE";//turbaResultsERG.Range["E3"].Value = deltaTStatus;
        Logger(deltaTStatus ? "deltaT GBC check Passed.." : "deltaT GBC check Failed..");

        if (!bendingStatus || !deltaTStatus)
        {
            Logger("Going to Executed/Custom Path");
            Logger("Throttle!! Going to Executed/Custom Path due to Bending and/or Delta failure");
            Logger("Going to Executed/Custom Path due to Bending and/or Delta failure");
            return result;
        }

        bool wheelchamberThrottle = false;
        
        if ((double)preFeasibilityModel.TemperatureActualValue < 500)
        {
            wheelchamberThrottle = !WheelChamberWithCW1((double)turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature, (double)turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure);
        }
        else
        {
            wheelchamberThrottle = !WheelChamberWithCW2((double)turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature, (double)turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure);
        }

        if (wheelchamberThrottle)
        {
            Logger("Throttle !! Going to Executed/Custom Path...");
            Logger("Going to Executed/Custom Path...");
            return result;
        }

        if (datChanged)
        {
            PrepareDATFile();
            LaunchTurba();
            ERGResultsCheckThrottle();
            return result;
        }
        else
        {
            result = true;
            Logger("Bending and Wheelchamber checks passed, checking Loadpoints...");
            Logger("Bending and Wheelchamber checks passed, checking Thrust...");
        }

        return result;
    }

    private bool ERGCheckExhaustVolumetricFlowThrottle()
    {
       
        // Worksheet turbaResultsERG = workbook.Sheets["Output"];
        // Worksheet currentFlowPath = workbook.Sheets["Pre-Feasibility checks"];
        bool result = false;

        double volFlow = turbaOutputModel.OutputDataList[0].Vol_Flow;//(double)turbaResultsERG.Range["M2"].Value;
        int currentFlowPathNo = preFeasibilityModel.Variant;//(int)currentFlowPath.Range["G15"].Value;
        double volFlowUpperLimit = turbaOutputModel.VolFlow_UpperLimit;//(double)turbaResultsERG.Range["N31"].Value;

        if (volFlow <= volFlowUpperLimit)
        {
          turbaOutputModel.CheckVolFlow = "TRUE";
            // turbaResultsERG.Range["M3"].Value = true;
            Logger("volFlow is in acceptable range...continuing with Throttle");
            Logger("volFlow is in acceptable range...continuing with Throttle");
            result = true;
        }
        else
        {
            Logger("Go to 2GBC path");
        }

        return result;
    }

    private void Logger(string message)
    {
        // Implement the logic to log messages
        Console.WriteLine(message);
        logger.LogInformation(message);
    }

    private void LaunchRsmin()
    {
        TurbaAutomation turbaAutomation = new TurbaAutomation();
        turbaAutomation.LaunchRsmin();
        // Implement the logic to launch Rsmin
    }

    private void LoadPointGeneratorIncMassFlow(int cell, int percentage)
    {
        ExecLoadPointGenerator execLoadPointGenerator = new ExecLoadPointGenerator();
        execLoadPointGenerator.LoadPointGenerator_IncMassFlow(cell,percentage);
        // Implement the logic to increase mass flow
    }

    private void LoadPointGeneratorReduceBP(int cell, int percentage)
    {
        ExecLoadPointGenerator execLoadPointGenerator = new ExecLoadPointGenerator();
        execLoadPointGenerator.LoadPointGenerator_ReduceBP(cell, percentage);
        // Implement the logic to reduce back pressure
    }

    private void PrepareDATFileOnlyLPUpdate()
    {
        // ExecDATFileProcessor
        // ExecDATFileProcessor execDATFileProcessor = new ExecDATFileProcessor();
        ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        datFileHandler.PrepareDatFileOnlyLPUpdate();//repareDATFile_OnlyLPUpdate();//repareDATFileOnlyLPUpdate();
        // Implement the logic to prepare DAT file with only load point update
    }

    private void LaunchTurba()
    {
        TurbaAutomation turbaAutomation = new TurbaAutomation();
        turbaAutomation.LaunchTurba();
        // Implement the logic to launch Turba
    }

    private void PrepareDATFile()
    {
        ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        // ExecDATFileProcessor execDATFileProcessor = new ExecDATFileProcessor();
        datFileHandler.PrepareDatFile();//repareDATFile();
        // Implement the logic to prepare DAT file
    }

    private bool Exhaust1(double f8, double f9)
    {
        // Implement the logic for Exhaust1 check
        return ExhaustFunctions.Exhaust1(f8,f9);
        // return true;
    }

    private bool Exhaust2(double f8, double f9)
    {
        return ExhaustFunctions.Exhaust2(f8,f9);
        // Implement the logic for Exhaust2 check
        // return true;
    }

    private bool WheelChamberWithCW1(double g2, double f2)
    {
        // ExeCwCurve exeCwCurve = new ExeCwCurve();
        // exeCwCurve
        return ExeCwCurve.WheelChamberWithCW_1(g2,f2);
        // Implement the logic for WheelChamberWithCW1 check
        // return true;
    }

    private bool WheelChamberWithCW2(double g2, double f2)
    {
        return ExeCwCurve.WheelChamberWithCW_2(g2,f2);
        // Implement the logic for WheelChamberWithCW2 check
        // return true;
    }

    public void Close()
    {
        // workbook.Close(false);
        // excelApp.Quit();
    }
}


