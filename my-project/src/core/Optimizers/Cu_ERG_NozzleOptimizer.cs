using System;
using Interfaces.ILogger;
using Models.NozzleDataModel;
using Models.TurbaOutputDataModel;
using Optimizers.Exec_ERG_NozzleOptimizer;
using Turba.Cu_TurbaConfig;
// using Microsoft.Office.Interop.Excel;
namespace Optimizers.CustomNozzleOptimizer;

using Handlers.Custom_DAT_Handler;
using Handlers.CustomERGHandler;
using Ignite_x_wavexcel;
using Models.PreFeasibility;
using StartExecutionMain;
public class CustomNozzleOptimizer : ExecutedNozzleOptimizer
{

    private TurbaOutputModel turbaOutputModel;
    private NozzleCalculationsData nozzleCalculationsData;
    private PreFeasibilityDataModel preFeasibilityDataModel;
    public static int GNozzleCount = 0;
    public static double Na = 0;
    public static double Nb= 0;
    ILogger logger;
    public CustomNozzleOptimizer()
    {
        turbaOutputModel = TurbaOutputModel.getInstance();
        nozzleCalculationsData = NozzleCalculationsData.getInstance();
        logger=CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        this.preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();

    }

    public override bool RuleEngineAlgorithmForNozzles()
    {
        GNozzleCount++;
        bool result = false;

        LoadERGFile();

        // ClearContents(NozzleCalc.Range["B5:J5"]);
        nozzleCalculationsData.NA = turbaOutputModel.OutputDataList[0].FMIN1_DUESEN; // NA
        nozzleCalculationsData.nozzle_Total = nozzleCalculationsData.NA+ turbaOutputModel.OutputDataList[0].FMIN2_DUESEN;
        nozzleCalculationsData.HOEHE=turbaOutputModel.OutputDataList[0].HOEHE;
        nozzleCalculationsData.TEILUNG=turbaOutputModel.OutputDataList[0].TEILUNG;
        nozzleCalculationsData.WIWINKEL_ALFAA = turbaOutputModel.OutputDataList[0].WINKEL_ALFAA;
        nozzleCalculationsData.FMIN_IST_V1_V2 = turbaOutputModel.OutputDataList[1].FMIN_IST;
        nozzleCalculationsData.FMIN_IST_V1= turbaOutputModel.OutputDataList[6].FMIN_IST;
        nozzleCalculationsData.FMIN_SOLL_LP1= turbaOutputModel.OutputDataList[1].FMIN_SOLL;
        nozzleCalculationsData.FMIN_SOLL_LP6= turbaOutputModel.OutputDataList[6].FMIN_SOLL;
        nozzleCalculationsData.fillNozzleData();

        double NA = nozzleCalculationsData.Cal_NA;
        double NB = nozzleCalculationsData.Cal_NB; 

        double NozzleFitness = EvaluateFitness(nozzleCalculationsData.Cal_NA, nozzleCalculationsData.Cal_NB);
        Console.WriteLine("FITNESSS: "+NozzleFitness);
        if (NozzleFitness == 0)
        {
            Logger("Best Nozzles A: " + NA + ", Best Nozzles B: " + NB);
            result = true;
            Logger("------ Nozzles Best fit found !! -----");
        }
        else
        {
           // This should be defined and managed appropriately
            if (GNozzleCount <= 3)
            {
                return RuleEngineAlgorithmForNozzles();
                //return result;
            }

            Logger("---- WARNING !! Can not find the best nozzle match...");
            result = false;
            TerminateIgniteX("Nozzle optimizer");
        }

        Logger("***********************************************************");
        return result;
    }
    public override double EvaluateFitness(double NA, double NB)
    {
        double ABWEICHUNG_lowerLimit, ABWEICHUNG_upperLimit;
        int flowPathVariant = preFeasibilityDataModel.Variant;

        if (flowPathVariant < 3)
        {
            ABWEICHUNG_lowerLimit = 2;
            ABWEICHUNG_upperLimit = 11;
        }
        else
        {
            ABWEICHUNG_lowerLimit = 2;
            ABWEICHUNG_upperLimit = 11;
        }
        double[] outputs;
        if (GNozzleCount == 1)
        {
            outputs = BlackBoxFunction(NA, NB);
        }
        else
        {
            if(Na > turbaOutputModel.Fmin1_UpperLimit)
            {
                outputs=  BlackBoxFunction(NA-1, NB+1);
            }
            else
            {
                outputs=  BlackBoxFunction(NA+1, NB - 1);
            }
        }

        //outputs = BlackBoxFunction(NA, NB);

        double fitness = 0;
        Na = outputs[1];
        Nb = outputs[2];
        if (outputs[0] < ABWEICHUNG_lowerLimit)
        {
            fitness += (ABWEICHUNG_lowerLimit - outputs[0]) * 10;
        }
        else if (outputs[0] > ABWEICHUNG_upperLimit)
        {
            fitness += (outputs[0] - ABWEICHUNG_upperLimit) * 10;
        }

        if (outputs[1] > turbaOutputModel.Fmin1_UpperLimit)
        {
            fitness += (outputs[1] - turbaOutputModel.Fmin1_UpperLimit) * 10;
        }

        if (outputs[2] > turbaOutputModel.Fmin2_UpperLimit)
        {
            fitness += (outputs[2] - turbaOutputModel.Fmin2_UpperLimit) * 10;
        }

        if (outputs[3] > 0.43)
        {
            fitness += (outputs[3] - 0.43) * 10;
        }

        if (outputs[4] > 0.38)
        {
            fitness += (outputs[4] - 0.38) * 10;
        }

        if (outputs[5] > 0.88)
        {
            fitness += (0.88 - outputs[5]) * 10;
        }
        return fitness;
    }

    public override double[] BlackBoxFunction(double NA, double NB)
    {
        double[] Output = new double[6];
        double NozzleFront = NA;
        double NozzleCount = NA + NB;

        Logger("----- Algorithm For Nozzles -----");
        Logger($"Writing NA: {NA}, NB: {NB}");
        Logger("-----------------------------------------");

        UpdateNozzleSpecs(NozzleCount, NozzleFront);

        // TurbaConfig turbaConfig = new TurbaConfig();
        CuTurbaAutomation turbaAutomation = new CuTurbaAutomation();
        turbaAutomation.LaunchTurba();
        Output[0] = turbaOutputModel.OutputDataList[1].ABWEICHUNG;//Replace("%",""));
        Output[1] = turbaOutputModel.OutputDataList[0].FMIN1;
        Output[2] = turbaOutputModel.OutputDataList[0].FMIN2;
        Output[3] = turbaOutputModel.OutputDataList[0].Admission_Factor_Group1;
        Output[4] = turbaOutputModel.OutputDataList[0].Admission_Factor_Group2;
        Output[5] = turbaOutputModel.OutputDataList[0].Admission_Factor;
        return Output;
    }
    public override void UpdateNozzleSpecs(double NozzleCount, double NozzleFront)
    {
        CustomDATFileProcessor customDATFileProcessor= new CustomDATFileProcessor();
        customDATFileProcessor.UpdateNozzleSpecs(NozzleCount,NozzleFront);
        // CustomNozzleOptimizer customNozzleOptimizer = new CustomNozzleOptimizer();
        // customNozzleOptimizer.UpdateNozzleSpecs(NozzleCount,NozzleFront);
    }
    private void LoadERGFile()
    {

        CustomERGFileReader customERGFileReader = new CustomERGFileReader();
        customERGFileReader.LoadERGFile();
        // Implement the logic to load the ERG file
    }

    private void ClearContents(Range range)
    {
        // range.ClearContents();
    }

    

    public override void Logger(string message)
    {
        // Implement the logging logic
        // Console.WriteLine(message);
        logger.LogInformation(message);
    }

    public override void TerminateIgniteX(string reason)
    {
        // Implement the logic to terminate IgniteX
        TurbineDesignPage.cts.Cancel();
        Console.WriteLine("Terminating IgniteX: " + reason);
    }
}