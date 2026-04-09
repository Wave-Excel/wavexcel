using System;
using System.Runtime.InteropServices;
using CsvHelper;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
using PdfSharp.Pdf.Filters;
using Turba.Exec_TurbaConfig;
using StartExecutionMain;
using Turba.Cu_TurbaConfig;
namespace HMBD.PSO_PenalityFunctionNozzle;

using System.Diagnostics;
using HMBD.Cu_CW_Curve;
using Ignite_x_wavexcel;

// using Microsoft.Office.Interop.Excel;

public class PenaltyScoreCalculator
{
    // private Worksheet powerNearest;
    private TurbineDataModel turbineDataModel;
    // private Worksheet output;
    private TurbaOutputModel turbaOutputModel;
    // private Worksheet prefeas;
    private PreFeasibilityDataModel preFeasibilityDataModel;

    private double deltaTupperLim;

    double nozzleHeightLowerLim;
    double nozzleHeightUpperLim;

    int GBCLengthLowerLimit;
    int GBCLengthUpperLimit;

    int lpCount;
    IConfiguration configuration;
    int nozzleAreaUpperLim;
    ILogger logger;
    public PenaltyScoreCalculator(){
        turbaOutputModel = TurbaOutputModel.getInstance();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        turbineDataModel = TurbineDataModel.getInstance();
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        lpCount = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        nozzleHeightLowerLim = configuration.GetValue<double>("AppSettings:Nozzle_height_Limits_Lower");
        nozzleHeightUpperLim = configuration.GetValue<double>("AppSettings:Nozzle_height_Limits_Upper");
        nozzleAreaUpperLim = configuration.GetValue<int>("AppSettings:Nozzle_Area_UpperLim");
        deltaTupperLim = configuration.GetValue<double>("AppSettings:DeltaT_Upper_Lim"); 
        GBCLengthLowerLimit = configuration.GetValue<int>("AppSettings:GBCLength_lower_Limit");
        GBCLengthUpperLimit = configuration.GetValue<int>("AppSettings:GBCLength_Upper_Limit");
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    private string BCD;
    private string checkType;
    private object output;

    public double GetPenaltyScore()
    {
        bool G14 = (preFeasibilityDataModel.Decision == "TRUE") ? true:false;
        bool G26 = (preFeasibilityDataModel.Decision_2 == "TRUE") ? true:false;
        bool G46 = true;//(preFeasibilityDataModel.Decision_3 == "TRUE")?true:false;

        if (G14)
        {
            BCD = "1120";
        }
        else if (!G14 && G26)
        {
            BCD = "1190";
        }
        else if (!G26 && G46)
        {
            BCD = "Throttle";
        }
        else
        {
            Logger("Prefeasibility check failed...Go To 2 GBC");
            TerminateIgniteX("Prefeasibility_Check");
        }

        if (int.Parse(BCD) >= 1120 && int.Parse(BCD) <= 1130)
        {
            checkType = "1120";
        }
        else if (int.Parse(BCD) >= 1190 && int.Parse(BCD) <= 1210)
        {
            checkType = "1190";
        }
        else
        {
            Logger("Invalid cu_SAXA_SAXI.BCD Type");
            TerminateIgniteX("getPenaltyScore");
        }

        bool allFilled = false;
        CheckDeltaErrorERG(checkType);

        Debug.WriteLine("DELTA T:" + turbaOutputModel.OutputDataList[0].DELTA_T);
        Debug.WriteLine("Wheel Chamber Temp:" + turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature);
        Debug.WriteLine("FMIN1:" + turbaOutputModel.OutputDataList[0].FMIN1);
        if (turbaOutputModel.OutputDataList[0].DELTA_T!=0 && turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature!=0 && turbaOutputModel.OutputDataList[0].FMIN1!=0 && turbaOutputModel.OutputDataList[0].Thrust!=0 && turbaOutputModel.OutputDataList[0].HOEHE!=0 && turbaOutputModel.OutputDataList[0].GBC_Length!=0 && !String.IsNullOrEmpty(turbaOutputModel.OutputDataList[0].PSI) && !String.IsNullOrEmpty(turbaOutputModel.OutputDataList[0].Lang)){
            allFilled = true;
        }

        try
        {
            if (allFilled)
            {
                turbaOutputModel.H41 = 1;
                turbaOutputModel.AA41 = 0.1;
                double pen = (turbaOutputModel.E40*20.00)/(turbaOutputModel.E41)+(turbaOutputModel.G40*20.00)/(turbaOutputModel.G41)+(turbaOutputModel.H40*20.00)/(turbaOutputModel.H41)+(turbaOutputModel.P40*20.00)/(turbaOutputModel.P41)+(turbaOutputModel.AA40*20.00)/(turbaOutputModel.AA41)+(turbaOutputModel.AC40*20.00)/(turbaOutputModel.AC41)+(turbaOutputModel.AE40*20.00)/(turbaOutputModel.AE41)+(turbaOutputModel.AF40*20.00)/(turbaOutputModel.AF41);
                return pen;

            }
            else
            {
                // output.Range["H41"].Value = 1;
                turbaOutputModel.H41 = 1;
                turbaOutputModel.AA41 = 0.1;
                return 50000;
            }
        }
        catch (Exception)
        {
            TerminateIgniteX("getPenaltyScore");
            return 0;
        }
    }

    private void CheckDeltaErrorERG(string checkType)
    {
        if (checkType == "1190")
        {
            Logger("Conducting checks for BCD1190");
            HardCheckWheelChamberTemp1190();
            HardCheckThrustValue1190();
            HardCheckNozzlesSection1190();
            HardCheckGBCLength1190();
            HardCheckPSI1190();
            HardCheckLang1190();
        }
        else if (checkType == "1120")
        {
            Logger("Conducting checks for BCD1120");
            HardCheckWheelChamberTemp1120();
            HardCheckThrustValue1120();
            HardCheckNozzlesSection1120();
            HardCheckGBCLength1120();
            HardCheckPSI1120();
            HardCheckLang1120();
        }
        else
        {
            Logger("Invalid cu_PSO_PenalityFunc_Throttle.Check Type");
            TerminateIgniteX("CheckDeltaErrorERG");
        }
    }

    private void HardCheckNozzlesSection1190()
    {
        double nozzleHeight = turbaOutputModel.OutputDataList[0].HOEHE;//output.Range["AA2"].Value;
        double nozzleHeightLowerLim = 10.5;// output.Range["AA30"].Value;
        double nozzleHeightUpperLim = 27;// output.Range["AA31"].Value;

        Logger($"Nozzle height: {nozzleHeight} Limits: {nozzleHeightLowerLim} | {nozzleHeightUpperLim}");
        if (nozzleHeight < nozzleHeightLowerLim)
        {
            Logger("Nozzle Height check failed...");
            turbaOutputModel.Check_HOEHE = "FALSE";
            // output.Range["AA3"].Value = false;
            turbaOutputModel.AA40 = Math.Abs(nozzleHeight - nozzleHeightLowerLim);
            // output.Range["AA40"].Value = Math.Abs(nozzleHeight - nozzleHeightLowerLim);
        }
        else if (nozzleHeight <= nozzleHeightUpperLim)
        {
            Logger("Nozzle Height check passed...");
            turbaOutputModel.Check_HOEHE = "TRUE";
            // output.Range["AA3"].Value = true;
            turbaOutputModel.AA40 = 0;
        }
        else
        {
            Logger("Nozzle Height check failed...");
            // output.Range["AA3"].Value = false;
            turbaOutputModel.Check_HOEHE = "FALSE";
            turbaOutputModel.AA40 = Math.Abs(nozzleHeight - nozzleHeightUpperLim);
        }

        double nozzleAreaGroup1 = turbaOutputModel.OutputDataList[0].FMIN1;//output.Range["H2"].Value;
        double nozzleAreaUpperLim = 1430;// output.Range["I31"].Value;
        Logger($"Nozzle area G1: {nozzleAreaGroup1} Limits: {nozzleAreaUpperLim}");
        if (nozzleAreaGroup1 > nozzleAreaUpperLim || nozzleAreaGroup1 <= 0)
        {
            Logger("Nozzle Area Group1 check Failed..");
            turbaOutputModel.Check_FMIN1 = "FALSE";
            // output.Range["H3"].Value = false;
            turbaOutputModel.H40 = Math.Abs(nozzleHeight - nozzleAreaUpperLim);
            // output.Range["H40"].Value = Math.Abs(nozzleHeight - nozzleAreaUpperLim);
        }
        else
        {
            Logger("nozzle Area Group1 check Passed..");
            turbaOutputModel.Check_FMIN1 = "TRUE";
            // output.Range["H3"].Value = true;
            turbaOutputModel.H40 = 0;

            // output.Range["H40"].Value = 0;
        }
    }

    private void HardCheckNozzlesSection1120()
    {
        double nozzleHeight =  turbaOutputModel.OutputDataList[0].HOEHE;//output.Range["AA2"].Value;
         double nozzleHeightLowerLim = 10.5;
         double nozzleHeightUpperLim = 27;

        Logger($"Nozzle height: {nozzleHeight} Limits: {nozzleHeightLowerLim} | {nozzleHeightUpperLim}");
        if (nozzleHeight < nozzleHeightLowerLim)
        {
            Logger("Nozzle Height check failed...");
            turbaOutputModel.Check_HOEHE = "FALSE";
            // output.Range["AA3"].Value = false;
            turbaOutputModel.AA40 = Math.Abs(nozzleHeight - nozzleHeightLowerLim);
            // output.Range["AA40"].Value = Math.Abs(nozzleHeight - nozzleHeightLowerLim);
        }
        else if (nozzleHeight <= nozzleHeightUpperLim)
        {
            Logger("Nozzle Height check passed...");
            turbaOutputModel.Check_HOEHE = "TRUE";
            // output.Range["AA3"].Value = true;
            turbaOutputModel.AA40 = 0;
            // output.Range["AA40"].Value = 0;
        }
        else
        {
            Logger("Nozzle Height check failed...");
            turbaOutputModel.Check_HOEHE  = "FALSE";
            // output.Range["AA3"].Value = false;
            turbaOutputModel.AA40 = Math.Abs(nozzleHeight - nozzleHeightUpperLim);
            // output.Range["AA40"].Value = Math.Abs(nozzleHeight - nozzleHeightUpperLim);
        }

        double nozzleAreaGroup1 = turbaOutputModel.OutputDataList[0].FMIN1;// output.Range["H2"].Value;
        double nozzleAreaUpperLim = 1430;//output.Range["I31"].Value;
        Logger($"Nozzle area G1: {nozzleAreaGroup1} Limits: {nozzleAreaUpperLim}");
        if (nozzleAreaGroup1 > nozzleAreaUpperLim || nozzleAreaGroup1 <= 0)
        {
            Logger("Nozzle Area Group1 check Failed..");
            turbaOutputModel.Check_FMIN1 = "FALSE";
            // output.Range["H3"].Value = false;
            turbaOutputModel.H40 = Math.Abs(nozzleHeight - nozzleAreaUpperLim);
            // output.Range["H40"].Value = Math.Abs(nozzleHeight - nozzleAreaUpperLim);
        }
        else
        {
            Logger("nozzle Area Group1 check Passed..");
            turbaOutputModel.Check_FMIN1 = "TRUE";
            // output.Range["H3"].Value = true;
            turbaOutputModel.H40 = 0;
            // output.Range["H40"].Value = 0;
        }
    }

    private bool HardCheckThrustValue1190()
    {
        LaunchRsmin();
        double thrustUpperLim = 0.8;
        bool thrustResult = false;

        // Range loadPointThrust = output.Range["P4:P13"];
        List<OutputLoadPoint> lplist = turbaOutputModel.OutputDataList;
        for(int i=0;i<lpCount;i++)
        {
            double thrust = lplist[i].Thrust;//Math.Abs(cell.Value);
            if (thrust > thrustUpperLim || thrust < -0.8)
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
                // output.Range["P3"].Value = false;
                turbaOutputModel.P40 = 1;
                // output.Range["P40"].Value = 1;
                return false;
            }
            else
            {
                thrustResult = true;
                turbaOutputModel.P40 =0;
                // output.Range["P40"].Value = 0;
            }
        }

        if (thrustResult)
        {
            Logger("Thrust check Passed....");
            // output.Range["P3"].Value = true;
            turbaOutputModel.Check_Thrust = "TRUE";
            turbaOutputModel.OutputDataList[0].Thrust = 1;
            // output.Range["P2"].Value = true;
        }
        else
        {
            Logger("Thrust check Failed....");
            turbaOutputModel.Check_Thrust = "FALSE";
            // output.Range["P3"].Value = false;
            turbaOutputModel.OutputDataList[0].Thrust = 1;
            // output.Range["P2"].Value = true;
        }

        return thrustResult;
    }

    private bool HardCheckThrustValue1120()
    {
        LaunchRsmin();
        double thrustUpperLim = 0.8;
        bool thrustResult = false;

        // Range loadPointThrust = output.Range["P4:P13"];
        List<OutputLoadPoint> lplist = turbaOutputModel.OutputDataList;
        for(int i=0;i<lpCount;i++)
        // foreach (Range cell in loadPointThrust.Cells)
        {
            double thrust = lplist[i].Thrust;//Math.Abs(cell.Value);
            if (thrust > thrustUpperLim || thrust < -0.8)
            {
                Logger("Thrust check Failed....");
                turbaOutputModel.Check_Thrust = "FALSE";
                // output.Range["P3"].Value = false;
                turbaOutputModel.P40 = 1;
                // output.Range["P40"].Value = 1;
                return false;
            }
            else
            {
                thrustResult = true;
                turbaOutputModel.P40 = 0;
                // output.Range["P40"].Value = 0;
            }
        }

        if (thrustResult)
        {
            Logger("Thrust check Passed....");
            turbaOutputModel.Check_Thrust = "TRUE";
            // output.Range["P3"].Value = true;
            turbaOutputModel.OutputDataList[0].Thrust= 1;
            // output.Range["P2"].Value = true;
        }
        else
        {
            Logger("Thrust check Failed....");
            turbaOutputModel.Check_Thrust = "FALSE";
            // output.Range["P3"].Value = false;
            turbaOutputModel.OutputDataList[0].Thrust = 1;
            // output.Range["P2"].Value = true;
        }

        return thrustResult;
    }

    private void HardCheckWheelChamberTemp1190()
    {
        double deltaT = turbaOutputModel.OutputDataList[0].DELTA_T;//Convert.ToDouble(output.Range["E2"].Value);
        // double deltaTupperLim = Convert.ToDouble(output.Range["F31"].Value);
        Logger($"GBC delta Temperature: {deltaT}");

        double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;//Convert.ToDouble(output.Range["F2"].Value);
        double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(output.Range["G2"].Value);
        Logger($"Wheel Chamber Temperature: {wheelchamberT}");

        if (deltaT > 210 || deltaT <= 0)
        {
            Logger("deltaT GBC check Failed..");
            turbaOutputModel.Check_DELTA_T = "FALSE";
            // output.Range["E3"].Value = false;
            turbaOutputModel.E40 = Math.Abs(deltaT - deltaTupperLim);
            // output.Range["E40"].Value = Math.Abs(deltaT - deltaTupperLim);
        }
        else
        {
            Logger("deltaT GBC check Passed..");
            turbaOutputModel.Check_DELTA_T = "TRUE";
                        // output.Range["E3"].Value = true;
            turbaOutputModel.E40 = 0;
            // output.Range["E40"].Value = 0;
        }

        double inletTemperature =  preFeasibilityDataModel.TemperatureActualValue;//Convert.ToDouble(prefeas.Range["F8"].Value);
        double wheelchamberPupperLim;
        if (inletTemperature <= 500)
        {
            wheelchamberPupperLim = WheelChamberTempCurveWithCW1GetUpperLimit(wheelchamberP);
        }
        else
        {
            wheelchamberPupperLim = WheelChamberTempCurveWithCW2GetUpperLimit(wheelchamberP);
        }
        turbaOutputModel.H30 = wheelchamberPupperLim;
        turbaOutputModel.H31 = wheelchamberPupperLim;
        // output.Range["H30"].Value = wheelchamberPupperLim;
        // output.Range["H31"].Value = wheelchamberPupperLim;

        if (wheelchamberT > wheelchamberPupperLim || wheelchamberT <= 0)
        {
            Logger("wheelchamber Temp check Failed..");
            // output.Range["G3"].Value = false;
            
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
            turbaOutputModel.G40 = Math.Abs(wheelchamberT - wheelchamberPupperLim);
            // output.Range["G40"].Value = Math.Abs(wheelchamberT - wheelchamberPupperLim);
        }
        else
        {
            Logger("wheelchamber Temp check Passed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "TRUE";
            // output.Range["G3"].Value = true;
            turbaOutputModel.G40 = 0;
        }
    }

    private void HardCheckWheelChamberTemp1120()
    {
        double deltaT =  turbaOutputModel.OutputDataList[0].DELTA_T;//Convert.ToDouble(output.Range["E2"].Value);
        // double deltaTupperLim = Convert.ToDouble(output.Range["F31"].Value);
        Logger($"GBC delta Temperature: {deltaT}");

        double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;//Convert.ToDouble(output.Range["F2"].Value);
        double wheelchamberT = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;//Convert.ToDouble(output.Range["G2"].Value);
        Logger($"Wheel Chamber Temperature: {wheelchamberT}");

        if (deltaT > 240 || deltaT <= 0)
        {
            Logger("deltaT GBC check Failed..");
            // output.Range["E3"].Value = false;
            turbaOutputModel.Check_DELTA_T = "FALSE";
            turbaOutputModel.E40 = Math.Abs(deltaT - deltaTupperLim);
            // output.Range["E40"].Value = Math.Abs(deltaT - deltaTupperLim);
        }
        else
        {
            Logger("deltaT GBC check Passed..");
            turbaOutputModel.Check_DELTA_T = "TRUE";
            // output.Range["E3"].Value = true;
            turbaOutputModel.E40 = 0;
            // output.Range["E40"].Value = 0;
        }

        if (wheelchamberT > 410 || wheelchamberT <= 0)
        {
            Logger("wheelchamber Temp check Failed..");
            // output.Range["G3"].Value = false;
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "FALSE";
            turbaOutputModel.G40  = Math.Abs(wheelchamberT - 410);
            // output.Range["G40"].Value = Math.Abs(wheelchamberT - 410);
        }
        else
        {
            Logger("wheelchamber Temp check Passed..");
            turbaOutputModel.Check_Wheel_Chamber_Temperature = "TRUE";
            // output.Range["G3"].Value = true;
            turbaOutputModel.G40 = 0;
            // output.Range["G40"].Value = 0;
        }
    }

    private void HardCheckGBCLength1190()
    {
        double GBCLength =  turbaOutputModel.OutputDataList[0].GBC_Length; //Convert.ToDouble(output.Range["AC2"].Value);
        // double GBCLengthLimit = Convert.ToBoolean(output.Range["AC30"].Value) ? Convert.ToDouble(output.Range["AC30"].Value) : Convert.ToDouble(output.Range["AC31"].Value);
        bool varicode7 = false;
        double GBCLengthLimit = 0;
        if(varicode7){
            GBCLengthLimit = 356;
        }else{
            GBCLengthLimit = 370;
        }
        if (GBCLength > GBCLengthLimit || GBCLength <= 0)
        {
            turbaOutputModel.Check_GBC_Length = "FALSE";
            // output.Range["AC3"].Value = false;
            Logger("GBC Length failed..");
            turbaOutputModel.AC40 = Math.Abs(GBCLength - GBCLengthLimit);
            // output.Range["AC40"].Value = Math.Abs(GBCLength - GBCLengthLimit);
        }
        else
        {
            turbaOutputModel.Check_GBC_Length = "TRUE";
            // output.Range["AC3"].Value = true;
            Logger("GBC Length passed..");
            turbaOutputModel.AC40 = 0;
            // output.Range["AC40"].Value = 0;
        }
    }

    private void HardCheckGBCLength1120()
    {
        double GBCLength = turbaOutputModel.OutputDataList[0].GBC_Length;//Convert.ToDouble(output.Range["AC2"].Value);
        double GBCLengthLimit = 318;

        if (GBCLength > GBCLengthLimit || GBCLength <= 0)
        {
            turbaOutputModel.Check_GBC_Length = "FALSE";
            // output.Range["AC3"].Value = false;
            Logger("GBC Length failed..");
            turbaOutputModel.AC40 = Math.Abs(GBCLength - GBCLengthLimit);
            // output.Range["AC40"].Value = Math.Abs(GBCLength - GBCLengthLimit);
        }
        else
        {
            turbaOutputModel.Check_GBC_Length = "TRUE";
            // output.Range["AC3"].Value = true;
            Logger("GBC Length passed..");
            turbaOutputModel.AC40 = 0;
            // output.Range["AC40"].Value1w = 0;
        }
    }

    private bool HardCheckPSI1190()
    {
        bool PSIStatus = (turbaOutputModel.Check_PSI=="TRUE")?true:false;

        if (PSIStatus)
        {
            Logger("PSI check Passed....");
            turbaOutputModel.AE40 =0;
            return true;
        }
        else
        {
            Logger("PSI check Failed....");
             turbaOutputModel.AE40 =1;
            return false;
        }
    }

    private bool HardCheckPSI1120()
    {
        bool PSIStatus = (turbaOutputModel.Check_PSI=="TRUE")?true:false; //Convert.ToBoolean(output.Range["AE3"].Value);

        if (PSIStatus)
        {
            Logger("PSI check Passed....");
            turbaOutputModel.AE40 =0;
            // output.Range["AE40"].Value = 0;
            return true;
        }
        else
        {
            Logger("PSI check Failed....");
            turbaOutputModel.AE40 =1;
            // output.Range["AE40"].Value = 1;
            return false;
        }
    }

    private bool HardCheckLang1190()
    {
        bool LANGStatus = (turbaOutputModel.Check_Lang=="TRUE")?true:false;//Convert.ToBoolean(output.Range["AF3"].Value);

        if (LANGStatus)
        {
            Logger("LANG check Passed....");
             turbaOutputModel.AF40 = 0;
            // output.Range["AF40"].Value = 0;
            return true;
        }
        else
        {
            Logger("LANG check Failed....");
            turbaOutputModel.AF40 = 1;
            // output.Range["AF40"].Value = 1;
            return false;
        }
    }

    private bool HardCheckLang1120()
    {
        bool LANGStatus = (turbaOutputModel.OutputDataList[0].Lang=="TRUE")?true:false; //Convert.ToBoolean(output.Range["AF3"].Value);

        if (LANGStatus)
        {
            Logger("LANG check Passed....");
            turbaOutputModel.AF40 = 0;
            // output.Range["AF40"].Value = 0;
            return true;
        }
        else
        {
            Logger("LANG check Failed....");
            turbaOutputModel.AF40 = 1;
            // output.Range["AF40"].Value = 1;
            return false;
        }
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
        // Implement your logging logic here
        Console.WriteLine(message);
    }

    private void TerminateIgniteX(string functionName)
    {
        // Implement your termination logic here
        TurbineDesignPage.cts.Cancel();
      
        Console.WriteLine($"Terminating function: {functionName}");
    }

    private void LaunchRsmin()
    {
        CuTurbaAutomation cuTurbaAutomation     = new CuTurbaAutomation();
        cuTurbaAutomation.LaunchRsmin();
        // Implement your LaunchRsmin logic here
        Console.WriteLine("Launching Rsmin...");
    }

    private double WheelChamberTempCurveWithCW1GetUpperLimit(double wheelchamberP)
    {
        return WheelChamber.WheelChamberTemp_CurveWithCW1_GetUpperLimit(wheelchamberP);
        // Implement your logic to get the upper limit for Wheel Chamber Temp Curve With CW1
        // return 0;
    }

    private double WheelChamberTempCurveWithCW2GetUpperLimit(double wheelchamberP)
    {
        return WheelChamber.WheelChamberTemp_CurveWithCW2_GetUpperLimit(wheelchamberP);
        // Implement your logic to get the upper limit for Wheel Chamber Temp Curve With CW2
        // return 0;
    }
}
