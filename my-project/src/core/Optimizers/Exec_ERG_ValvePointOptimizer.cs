using System;
using Models.TurbaOutputDataModel;
using Optimizers.ERG_NozzleOptimizer;
using OfficeOpenXml;
using System.IO.Packaging;
using Models.LoadPointDataModel;
using StartExecutionMain;
using Turba.TurbaConfiguration;
using Handlers.DAT_Handler;
using Interfaces.ILogger;
using Optimizers.Exec_ERG_NozzleOptimizer;
using HMBD.Ref_DAT_selector;
using Handlers.Exec_DAT_Handler;
using Turba.Exec_TurbaConfig;

namespace Optimizers.Executed_ERG_ValvePointOptimizer;
class ExecValvePointOptimizer
{
        private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
        TurbaOutputModel turbaOutputModel; 
        LoadPointDataModel loadPointDataModel; 
        ILogger logger;
    int maxlp = 0;
    public ExecValvePointOptimizer(){
          this.turbaOutputModel = TurbaOutputModel.getInstance();
          this.loadPointDataModel = LoadPointDataModel.getInstance();
          logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        }
    

    public void ValvePointOptimize(int maxlp = 0)
    {
        this.maxlp = maxlp;
        double BaseLoadPointAbweichung = turbaOutputModel.OutputDataList[1].ABWEICHUNG;
        // Convert.ToDouble(turbaResults_ERG.Cells["D4"].GetValue<string>().Replace("%", "").Trim());//* 100;
        double ValvePointAbweichung = turbaOutputModel.OutputDataList[6].ABWEICHUNG;
        // Convert.ToDouble(turbaResults_ERG.Cells["D9"].GetValue<string>().Replace("%", "").Trim());// * 100;
        string NozzleGroupValveStatus = turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST;
        // turbaResults_ERG.Cells["V9"].Value.ToString();

        Logger("---------------------------------------");
        Logger("Base ex_ERG_RsminHandler.load ABWEICHUNG: " + BaseLoadPointAbweichung + "%");
        Logger("Valve point ABWEICHUNG: " + ValvePointAbweichung + "%");
        Logger("Valve point Nozzle group state: " + NozzleGroupValveStatus);

        if (ValvePointAbweichung >= 0 && ValvePointAbweichung <= 0.5)
        {
            Logger("ValvePointOptimize: It's already within Cells. Exiting..");
            return;
        }
        // turbaResults_ERG.Cells["K2"].Value
        int NozzleGroup1 = Convert.ToInt32(turbaOutputModel.OutputDataList[0].FMIN1_DUESEN);
        int NozzleGroup2 = Convert.ToInt32(turbaOutputModel.OutputDataList[0].FMIN2_DUESEN);

        if (BaseLoadPointAbweichung >= 2 && BaseLoadPointAbweichung <= 7)
        {
            Logger("Nozzles are in range... ex_Customer_Input_Handler.Check LP6");
            if (NozzleGroupValveStatus == "1 - 2")
            {
                Logger("Nozzle Group 2 Open: Checking LP6 ABWEICHUNG");
                if (ValvePointAbweichung <= 50)
                {
                    AdjustNozzlePair(1, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else if (ValvePointAbweichung > 50)
                {
                    AdjustNozzlePair(2, NozzleGroup1, NozzleGroup2);
                    return;
                }
            }
            else if (NozzleGroupValveStatus == "1 - 1")
            {
                Logger("Nozzle Group 2 Closed: Checking LP6 ABWEICHUNG");
                if (ValvePointAbweichung <= 25)
                {
                    AdjustValvePointMassFlow(maxlp);
                    return;
                }
                else if (ValvePointAbweichung > 25 && ValvePointAbweichung <= 50)
                {
                    AdjustNozzlePair(1, NozzleGroup1, NozzleGroup2);
                    return;
                }
                else if (ValvePointAbweichung > 50)
                {
                    AdjustNozzlePair(2, NozzleGroup1, NozzleGroup2);
                    return;
                }
            }
        }
    }

    void AdjustNozzlePair(int AdjustNum, int NA, int NB)
    {
        int newNA = NA + AdjustNum;
        int newNB = NB - AdjustNum;

        if (newNA > 25 || newNB < 4)
        {
            Logger("New Nozzles are out of permissible Cells...");
            Logger("Exiting. Adjusting mass flow..");
            AdjustValvePointMassFlow(maxlp);
            return;
        }
        else
        {
            int NozzleCount = newNA + newNB;

            Logger("----- Adjusting Nozzles -----");
            Logger("Writing NA: " + newNA + ", NB: " + newNB);
            UpdateNozzleSpecs(NozzleCount, newNA);
            LaunchTurba(maxlp);
            ValvePointOptimize(maxlp);
        }
    }
     void AdjustValvePointMassFlow(int maxlp = 0)
    {
        double MassFlow = loadPointDataModel.LoadPoints[6].MassFlow;
        double ValvePointAbweichung = turbaOutputModel.OutputDataList[6].ABWEICHUNG;
        // Convert.ToDouble(turbaResults_ERG.Cells["D9"].GetValue<string>().Replace("%", "").Trim());// * 100;
        double x = MassFlow;
        double fxPast = ValvePointAbweichung;
        double tolerance = 0.0001;
        long maxIterations = 1000;
        long iteration = 0;
        double stepSize = 0.1;//0.03;
        if (ValvePointAbweichung <= 1)
        {
            stepSize = 0.01;
        }

        do
        {
            Logger("Calculating valve point mass flow...");
            if (fxPast >= 0 && fxPast <= 0.5)
            {
                break;
            }
            else if (fxPast > 0.01 && turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST == "1 - 1")
            {
                x += stepSize;
            }
            else if (fxPast < 0 && turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST == "1 - 1")
            {
                x -= stepSize;
            }
            else if (fxPast > 0.01 && turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST == "1 - 2")
            {
                x -= stepSize;
            }
            else if (fxPast < 0 && turbaOutputModel.OutputDataList[6].DUESEN_GRUPPE_AUSGEST == "1 - 2")
            {
                x += stepSize;
            }
            loadPointDataModel.LoadPoints[6].MassFlow = x;
            // LoadPoints.Cells["B10"].Value = x;
            CalculateAbweichung(maxlp);
            double fxPresent = Convert.ToDouble(turbaOutputModel.OutputDataList[6].ABWEICHUNG);
            // Convert.ToDouble(turbaResults_ERG.Cells["D9"].GetValue<string>().Replace("%", "").Trim());
            if (iteration > 0 && Math.Sign(fxPresent * fxPast) == -1)
            {
                stepSize /= 2;
            }
            iteration++;
            Logger("Present Mass Flow: " + x + ", LP6 ABWEICHUNG Previous: " + fxPast + " Present: " + fxPresent);
            Logger("Performing MassFlow Correction by : " + stepSize);

            fxPast = fxPresent;

        } while (iteration < maxIterations);

        if (iteration < maxIterations)
        {
            Logger("Convergence achieved: x = " + x + ", f(x) = " + fxPast);
            Logger("-----------  Valve point corrected ------------");
            Logger("***********************************************************");
        }
        else
        {
            Logger("Maximum iterations reached without convergence.");
        }
    }

     void CalculateAbweichung(int maxlp=0)
    {
        Logger("Updating massflow...");
        PrepareDATFile_OnlyLPUpdate(maxlp);
        LaunchTurba(maxlp);
    }

    void Logger(string message)
    {
        logger.LogInformation(message);
    }

    void UpdateNozzleSpecs(int NozzleCount, int newNA)
    {
        ExecutedNozzleOptimizer nozzleOptimizer = new ExecutedNozzleOptimizer();
        nozzleOptimizer.UpdateNozzleSpecs(NozzleCount,newNA);
    }

    void LaunchTurba(int maxlp = 0)
    {
        TurbaAutomation turbaConfig  = new TurbaAutomation();
        turbaConfig.LaunchTurba(maxlp);
    }

    void PrepareDATFile_OnlyLPUpdate(int maxlp = 0)
    {
        // DatFileSelector datFileSelector = new DatFileSelector();
        ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        datFileHandler.PrepareDatFileOnlyLPUpdate(maxlp);
        // ExecutedDATFileProcessor dATFileProcessor = new ExecutedDATFileProcessor();
        // dATFileProcessor.PrepareDATFile_OnlyLPUpdate();
    }
}