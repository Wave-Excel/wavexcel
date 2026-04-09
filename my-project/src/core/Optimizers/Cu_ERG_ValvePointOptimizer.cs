using System;
using System.Runtime.InteropServices;
using Handlers.Custom_DAT_Handler;
using Interfaces.ILogger;
using Models.LoadPointDataModel;
using Models.TurbaOutputDataModel;
using StartExecutionMain;
using Turba.Cu_TurbaConfig;
// using Microsoft.Office.Interop.Excel;
namespace Optimizers.CustomValvePointOptimizer;
public class CustomValvePointOptimizer
{
    private TurbaOutputModel turbaOutputModel;
    private LoadPointDataModel loadPointDataModel;
    private ILogger logger;
    int maxlp = 0;
    public CustomValvePointOptimizer()
    {
        turbaOutputModel = TurbaOutputModel.getInstance();
        loadPointDataModel = LoadPointDataModel.getInstance();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }

    public void ValvePointOptimize(int maxlp = 0)
    {
        this.maxlp = maxlp;
        double BaseLoadPointAbweichung = turbaOutputModel.OutputDataList[1].ABWEICHUNG;//turbaResults_ERG.Range["D4"].Value * 100;
        double ValvePointAbweichung = turbaOutputModel.OutputDataList[6].ABWEICHUNG;//turbaResults_ERG.Range["D9"].Value * 100;
        string NozzleGroupValveStatus = turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST;//turbaResults_ERG.Range["V9"].Value;

        Logger("---------------------------------------");
        Logger("Base cu_ERG_RsminHandler.load ABWEICHUNG: " + BaseLoadPointAbweichung + "%");
        Logger("Valve point ABWEICHUNG: " + ValvePointAbweichung + "%");
        Logger("Valve point cu_DAT_CustomPath_DeletePrepare.Nozzle group state: " + NozzleGroupValveStatus);

        if (ValvePointAbweichung >= 0 && ValvePointAbweichung <= 0.5)
        {
            Logger("ValvePointOptimize: It's already within range. Exiting..");
            return;
        }

        int NozzleGroup1 = Convert.ToInt32(turbaOutputModel.OutputDataList[0].FMIN1_DUESEN);//(int)turbaResults_ERG.Range["K2"].Value;
        int NozzleGroup2 = Convert.ToInt32(turbaOutputModel.OutputDataList[0].FMIN2_DUESEN);//(int)turbaResults_ERG.Range["L2"].Value;

        if (BaseLoadPointAbweichung >= 2 && BaseLoadPointAbweichung <= 7)
        {
            Logger("Nozzles are in range... cu_Customer_Input_Handler.Check LP6");
            if (NozzleGroupValveStatus == "1 - 2")
            {
                Logger("Nozzle Group 2 Open: Checking LP6 ABWEICHUNG");
                if (ValvePointAbweichung <= 25)
                {
                    //------ INCREASE 1 NOZZLE IN GP1 TOTAL SAME
                    AdjustNozzlePair(1, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else if (ValvePointAbweichung > 25 && ValvePointAbweichung <= 50)
                {
                    //------ INCREASE 1 NOZZLE IN GP1 TOTAL SAME
                    
                    AdjustNozzlePair(1, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else if (ValvePointAbweichung > 50)
                {
                    //------ INCREASE 2 NOZZLE IN GP1 TOTAL SAME
                    AdjustNozzlePair(2, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else
                {
                    //----------EXCEPTION HANDLERS--------------
                }
            }
            else if (NozzleGroupValveStatus == "1 - 1")
            {
                Logger("Nozzle Group 2 Closed: Checking LP6 ABWEICHUNG");

                if (ValvePointAbweichung <= 25)
                {
                    //------ CHANGE IN LP MASSFLOW
                    AdjustValvePointMassFlow(maxlp);
                    return;
                }
                else if (ValvePointAbweichung > 25 && ValvePointAbweichung <= 50)
                {
                    //------ INCREASE 1 NOZZLE IN GP1 TOTAL SAME
                    AdjustNozzlePair(1, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else if (ValvePointAbweichung > 50)
                {
                    //------ INCREASE 2 NOZZLE IN GP1 TOTAL SAME
                    AdjustNozzlePair(2, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else
                {
                    //----------EXCEPTION HANDLERS--------------
                }
            }
        }
    }

    public void AdjustNozzlePair(int AdjustNum, int NA, int NB)
    {
        int newNA = NA + AdjustNum;
        int newNB = NB - AdjustNum;

        if (newNA > 25 || newNB < 4)
        {
            Logger("New Nozzles are out of permissable range...");
            Logger("Exiting. Adjusting mass flow..");
            AdjustValvePointMassFlow(maxlp);
            return;
        }
        else
        {
            int NozzleCount = newNA + newNB;

            // Write cu_DAT_CustomPath_DeletePrepare.Nozzle numbers in DAT file
            Logger("----- Adjusting Nozzles -----____VALVE POINT");
            Logger("Writing NA: " + newNA + ", NB: " + newNB);
            updateNozzleSpecs(NozzleCount, newNA);
            // cu_PSO_FlowPathOptimizer_Nozzle.Run the Turba
            LaunchTurba(maxlp);
            ValvePointOptimize(maxlp);
        }
    }

    public void AdjustValvePointMassFlow(int maxlp =0)
    {
        double MassFlow = loadPointDataModel.LoadPoints[6].MassFlow;//LoadPoints.Range["B10"].Value;
        double ValvePointAbweichung = turbaOutputModel.OutputDataList[6].ABWEICHUNG; // turbaResults_ERG.Range["D9"].Value * 100;

        double x = MassFlow;
        double fxPast, fxPresent;
        double tolerance = 0.0001;
        long maxIterations = 1000;
        long iteration = 0;
        double stepSize = 0.1;

        fxPast = turbaOutputModel.OutputDataList[6].ABWEICHUNG;//turbaResults_ERG.Range["D9"].Value * 100;

        while (iteration < maxIterations)
        {
            Logger("Calculating valve point mass flow...");
            if (fxPast >= 0 && fxPast <= 0.5)
            {
                break; 
            }
            else if (fxPast > 0.5)
            {
                x += stepSize;
            }
            else if (fxPast < 0)
            {
                x -= stepSize;
            }
            loadPointDataModel.LoadPoints[6].MassFlow = x;
            // LoadPoints.Range["B10"].Value = x;
            CalculateAbweichung(maxlp);
            fxPresent = turbaOutputModel.OutputDataList[6].ABWEICHUNG;//turbaResults_ERG.Range["D9"].Value * 100;

            // cu_ERG_ValvePointOptimizer.Adjust step size to prevent oscillation
            if (iteration > 0)
            {
                if (Math.Sign(fxPresent * fxPast) == -1)
                {
                    stepSize /= 2;
                }
            }

            iteration++;

            Logger("Mass Flow: " + x + ", LAST LP6 ABWEICHUNG: " + fxPast + ", LAST LP6 ABWEICHUNG: " + fxPresent);
            Logger("MassFlow Correction: " + stepSize);

            fxPast = fxPresent;
        }

        // Output the result
        if (iteration < maxIterations)
        {
            Logger("Convergence achieved: x = " + x + ", f(x) = " + fxPast);
            Logger("Valve point is corrected...");
        }
        else
        {
            Logger("Maximum iterations reached without convergence.");
        }
    }

    public void CalculateAbweichung(int maxlp =0)
    {
        // Call the necessary subroutines to perform calculations
        Logger("Updating massflow...");
        prepareDATFile_OnlyLPUpdate(maxlp);
        LaunchTurba(maxlp);
    }
    public void Logger(string message)
    {
        logger.LogInformation(message);
        // Console.WriteLine(message);
    }
    public void updateNozzleSpecs(int NozzleCount, int newNA)
    {
        CustomDATFileProcessor customDATFileProcessor= new CustomDATFileProcessor();
        customDATFileProcessor.UpdateNozzleSpecs(NozzleCount, newNA);
    

        // Implementation for updating nozzle specs in DAT file
    }

    public void prepareDATFile_OnlyLPUpdate(int maxlp =0)
    {
        CustomDATFileProcessor customDATFileProcessor= new CustomDATFileProcessor();
        customDATFileProcessor.PrepareDatFileOnlyLPUpdate(maxlp);

        // Implementation for preparing DAT file with only LP update
    }
    public void LaunchTurba(int maxlp =0)
    {
        CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
        cuTurbaAutomation.LaunchTurba(maxlp);


        // Implementation for launching Turba
    }
}



