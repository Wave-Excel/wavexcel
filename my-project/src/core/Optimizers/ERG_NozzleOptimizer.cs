using System;
// using Microsoft.Office.Interop.Excel;
// using ERG_Verification;
// using DAT_Handler;
using OfficeOpenXml;
using System.IO.Packaging;
// using DAT_Handler;
// using Turba_Interface;
using System.Security.Cryptography;
using Models.NozzleDataModel;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Models.NozzleTurbaData;
using Interfaces.ILogger;
using ERG_Verification;
using Turba.TurbaConfiguration;
using Handlers.DAT_Handler;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Ignite_x_wavexcel;
using Models.TurbineData;
//using Android.Support.CustomTabs;
// using Models.PreFeasibility;
namespace Optimizers.ERG_NozzleOptimizer{
public class NozzleOptimizer
{
    public static int nozzleOptimizeCount = 0;
    public static double Na = 0;
    public static double A1 = 0;
    public static double Nb = 0;
    public static double A2 = 0;
    NozzleCalculationsData nozzleData;
    TurbaOutputModel turbaOutputModel;
    NozzleTurbaDataModel nozzleTurbaDataModel;
    ILogger logger;
    PreFeasibilityDataModel preFeasibilityDataModel;
    public NozzleOptimizer(){
        this.nozzleData = NozzleCalculationsData.getInstance();
        this.turbaOutputModel = TurbaOutputModel.getInstance();
        this.nozzleTurbaDataModel = NozzleTurbaDataModel.getInstance();
        this.preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        this.logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    public bool RuleEngineAlgorithmForNozzles(int maxlPS = 0)
    {   
        nozzleOptimizeCount++;
        nozzleData.NA = turbaOutputModel.OutputDataList[0].FMIN1_DUESEN;
        nozzleData.nozzle_Total = nozzleData.NA + turbaOutputModel.OutputDataList[0].FMIN2_DUESEN;
        nozzleData.HOEHE = turbaOutputModel.OutputDataList[0].HOEHE;
        nozzleData.TEILUNG=turbaOutputModel.OutputDataList[0].TEILUNG;
        nozzleData.WIWINKEL_ALFAA = turbaOutputModel.OutputDataList[0].WINKEL_ALFAA;
        nozzleData.FMIN_IST_V1_V2 = turbaOutputModel.OutputDataList[1].FMIN_IST;
        nozzleData.FMIN_IST_V1 = turbaOutputModel.OutputDataList[6].FMIN_IST;
        nozzleData.FMIN_SOLL_LP1 = turbaOutputModel.OutputDataList[1].FMIN_SOLL;
        nozzleData.FMIN_SOLL_LP6 = turbaOutputModel.OutputDataList[6].FMIN_SOLL;
        nozzleData.fillNozzleData();
        double NA = nozzleData.Cal_NA;
        double NB = nozzleData.Cal_NB;    
        double NozzleFitness = EvaluateFitness(nozzleData.Cal_NA, nozzleData.Cal_NB);
        Console.WriteLine("FITNESSS: "+NozzleFitness);
        if (NozzleFitness == 0)
        {
            
            return true;
        }
        else
        {
            if (nozzleOptimizeCount <= 3)
            {
                    return this.RuleEngineAlgorithmForNozzles(maxlPS);
               
                //return false;
            }

            Logger("---- WARNING !! Can not find the best nozzle match...");
            TerminateIgniteX("Nozzle optimizer GA");
            Logger("********************************************************");
           
            return false;
        }
    }
    public void Logger(string message)
    {
        logger.LogInformation(message);
        Console.WriteLine(message);
    }
    public void TerminateIgniteX(string message)
    {
            TurbineDesignPage.cts.Cancel();
            Console.WriteLine(message+" End");
            //Environment.Exit(0);
    }
    public void ErgResultsCheck()
    {   
        //use from ERG_Verfifcation
        ERGVerification eRGVerification = new ERGVerification();
        eRGVerification.ErgResultsCheck();
    }
    private double[] BlackBoxFunction(double NA, double NB)
    {   
        double[] Output = new double[6];
        double NozzleFront = NA;
        double NozzleCount = NA + NB;

        if(TurbineDataModel.getInstance().OldNa!= 0 && TurbineDataModel.getInstance().OldNa != NA)
            {
                //Logger("NozzleAdjusted");
            }

        Logger("----- Algorithm For Nozzles -----");
        Logger($"Writing NA: {NA}, NB: {NB}");
        Logger("-----------------------------------------");

        UpdateNozzleSpecs(NozzleCount, NozzleFront);

        TurbaConfig turbaConfig = new TurbaConfig();
        turbaConfig.LaunchTurba();
        Output[0] = turbaOutputModel.OutputDataList[1].ABWEICHUNG;//Replace("%",""));
        Output[1] = turbaOutputModel.OutputDataList[0].FMIN1; // ADMIN_CONTROL
        Output[2] = turbaOutputModel.OutputDataList[0].FMIN2;
        Output[3] = turbaOutputModel.OutputDataList[0].Admission_Factor_Group1;
        Output[4] = turbaOutputModel.OutputDataList[0].Admission_Factor_Group2;
        Output[5] = turbaOutputModel.OutputDataList[0].Admission_Factor;
        return Output;
    }

    public void UpdateNozzleSpecs(double NozzleCount, double NozzleFront){
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.updateNozzleSpecs(NozzleCount, NozzleFront);

    }
    

    public void InitializePopulation(double maxNozzle, int popSize, double minN, double maxN, double initNA, double initNB, ref double[,] population)
    {  
        Random rnd = new Random();
        for (int i = 0; i < popSize; i++)
        {
            do
            {
                population[i, 0] = minN + rnd.NextDouble() * (maxN - minN);
                population[i, 1] = minN + rnd.NextDouble() * (population[i, 0] - minN);
                population[i, 0] = Math.Round(population[i, 0], 0);
                population[i, 1] = Math.Round(population[i, 1], 0);
            }
            while (population[i, 0] + population[i, 1] > maxNozzle);
        }
    }

    public double EvaluateFitness(double NA, double NB)
    {    
        double ABWEICHUNG_lowerLimit, ABWEICHUNG_upperLimit;
        int flowPathVariant = preFeasibilityDataModel.Variant;

        if (flowPathVariant < 3)
        {
            ABWEICHUNG_lowerLimit = turbaOutputModel.Abweichung_LowerLimit;
            ABWEICHUNG_upperLimit = turbaOutputModel.Abweichung_UpperLimit;
        }
        else
        {
            ABWEICHUNG_lowerLimit = turbaOutputModel.Abweichung_LowerLimit;
            ABWEICHUNG_upperLimit = turbaOutputModel.Abweichung_UpperLimit;
        }
        double[] outputs;
            double nozzle1 = 0;
            double nozzle2 = 0;
        if (nozzleOptimizeCount == 1)
        {
                nozzle1 = NA;
                nozzle2 = NB;
                outputs = BlackBoxFunction(NA, NB);
        }
        else
        {
         if (Na > turbaOutputModel.Fmin1_UpperLimit || A1 > 0.43 )
          {
                    nozzle1 = NA - nozzleOptimizeCount + 1;
                    nozzle2 = NB + nozzleOptimizeCount - 1;
                    outputs = BlackBoxFunction(nozzle1, nozzle2);
          }
          else
          {
                    nozzle1 = NA + nozzleOptimizeCount - 1;
                    nozzle2 = NB - nozzleOptimizeCount + 1;
                    outputs = BlackBoxFunction(nozzle1, nozzle2);
         }
        }


            double fitness = 0;
            Na = outputs[1];
            Nb = outputs[2];
            A1 = outputs[3];
            A2 = outputs[4];
        if (outputs[0] < ABWEICHUNG_lowerLimit)
        {
            fitness += (ABWEICHUNG_lowerLimit - outputs[0]) * 10;
        }
        else if (outputs[0] > ABWEICHUNG_upperLimit)
        {
            fitness += (outputs[0] - ABWEICHUNG_upperLimit) * 10;
        }

        if (outputs[1] > turbaOutputModel.Fmin1_UpperLimit) // CHECK
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
        if (fitness == 0)
        {
                if (nozzleOptimizeCount > 1)
                {
                    //Logger("NozzleAdjusted");
                }
                Logger($"Best Nozzles A: {nozzle1}, Best Nozzles B: {nozzle2}");
                Logger("------ Nozzles Best fit found !! -----");


         }
            return fitness;
    }



    public void SelectParents(double[,] population, double[] fitness, ref double[,] parents)
    {  
        int best1 = 0;
        int best2 = 1;

        for (int i = 0; i < fitness.Length; i++)
        {
            if (fitness[i] < fitness[best1])
            {
                best2 = best1;
                best1 = i;
            }
            else if (fitness[i] < fitness[best2])
            {
                best2 = i;
            }
        }

        parents[0, 0] = population[best1, 0];
        parents[0, 1] = population[best1, 1];
        parents[1, 0] = population[best2, 0];
        parents[1, 1] = population[best2, 1];
    }

    public void Crossover(double[] parent1, double[] parent2, ref double[] child1, ref double[] child2)
    {  
        Random rnd = new Random();
        double alpha = rnd.NextDouble();

        child1[0] = alpha * parent1[0] + (1 - alpha) * parent2[0];
        child1[1] = alpha * parent1[1] + (1 - alpha) * parent2[1];

        child2[0] = alpha * parent2[0] + (1 - alpha) * parent1[0];
        child2[1] = alpha * parent2[1] + (1 - alpha) * parent1[1];

        child1[0] = Math.Round(child1[0], 0);
        child1[1] = Math.Round(child1[1], 0);
        child2[0] = Math.Round(child2[0], 0);
        child2[1] = Math.Round(child2[1], 0);
    }

    public void Mutate(double maxNozzle, ref double[] individual, double minN, double maxN)
    {   
        Random rnd = new Random();
        double mutationRate = 0.1; // Set a mutation rate
        do
        {
            individual[0] = minN + rnd.NextDouble() * mutationRate * (maxN - minN);
            individual[0] = Math.Round(individual[0], 0);
            individual[1] = minN + rnd.NextDouble() * mutationRate * (individual[0] - minN); // Ensure NA > NB
            individual[1] = Math.Round(individual[1], 0);
        }
        while (individual[0] + individual[1] > maxNozzle);
    }
    public bool GeneticAlgorithmForNozzles()
    {  
        int popSize = 8;
        double minN = 4;
        double maxNozzle = GetMaxNozzleByVolFlow();
        double maxN = 25;
        double initNA = GetFMIN1ByVolFlow();
        double initNB = GetFMINGESByVolFlow() - initNA;
        int generations = 10;

        Logger($"Initializing GA parameters...");
        Logger($"PopSize: {popSize} Gen: {generations} Nozzle Min: {minN} Nozzle Max: {maxN}");
        Logger($"Nozzle totLim: {maxNozzle} Nozzle initA: {initNA} Nozzle initB: {initNB}");

        double[,] population = new double[popSize, 2];
        double[] fitness = new double[popSize];
        double[,] newPopulation = new double[popSize, 2];
        double[,] parents = new double[2, 2];

        InitializePopulation(maxNozzle, popSize, minN, maxN, initNA, initNB, ref population);

        for (int i = 0; i < generations; i++)
        {
            for (int j = 0; j < popSize; j++)
            {
                fitness[j] = EvaluateFitness(population[j, 0], population[j, 1]);
                Console.WriteLine($"_______________________________");
                Console.WriteLine($"Generation-{i + 1}, Fitness score: {fitness[j]}");
                Console.WriteLine($"_______________________________");

                Logger($"Nozzle optimization.....");
                Logger($"Gen-{i + 1} Finished, Fitness score: {fitness[j]}");
                if (fitness[j] == 0)
                {
                    goto StopGenerations;
                }
            }

            SelectParents(population, fitness, ref parents);

            for (int j = 0; j < popSize; j += 2)
            {
                double[] parent1 = { parents[0, 0], parents[0, 1] };
                double[] parent2 = { parents[1, 0], parents[1, 1] };
                double[] child1 = new double[2];
                double[] child2 = new double[2];

                Crossover(parent1, parent2, ref child1, ref child2);

                Mutate(maxNozzle, ref child1, minN, maxN);
                Mutate(maxNozzle, ref child2, minN, maxN);

                if (child1[0] <= child1[1])
                {
                    child1[1] = child1[0] - new Random().NextDouble() * (child1[0] - minN);
                    child1[1] = Math.Round(child1[1], 0);
                }
                if (child2[0] <= child2[1])
                {
                    child2[1] = child2[0] - new Random().NextDouble() * (child2[0] - minN);
                    child2[1] = Math.Round(child2[1], 0);
                }

                newPopulation[j, 0] = child1[0];
                newPopulation[j, 1] = child1[1];
                newPopulation[j + 1, 0] = child2[0];
                newPopulation[j + 1, 1] = child2[1];
            }

            for (int j = 0; j < popSize; j++)
            {
                population[j, 0] = newPopulation[j, 0];
                population[j, 1] = newPopulation[j, 1];
            }
        }

    StopGenerations:
        double bestFitness = fitness[0];
        int bestIndex = 0;

        for (int j = 1; j < popSize; j++)
        {
            if (fitness[j] < bestFitness)
            {
                bestFitness = fitness[j];
                bestIndex = j;
            }
        }

        if (fitness[bestIndex] == 0)
        {
            Console.WriteLine($"Best NA: {population[bestIndex, 0]}, Best NB: {population[bestIndex, 1]}");
            Logger($"Best Nozzles A: {population[bestIndex, 0]}, Best Nozzles B: {population[bestIndex, 1]}");
            return true;
        }
        else
        {
            Logger("---- WARNING !! Can not find the best nozzle match...");
            TerminateIgniteX("Nozzle optimizer GA");
            return false;
        }
    }

    private double GetFMINGESByVolFlow()
    {   
        double nozzleMax = 21;
        foreach(NozzleTurbaData nozzleTurbaData in nozzleTurbaDataModel.NozzleTurbaDataList){
            if(nozzleTurbaData.Remark.Equals("Selected", StringComparison.Ordinal)){
                nozzleMax = nozzleTurbaData.FminGes;
                break;
            }
        }
        return nozzleMax;
    }

    private double GetFMIN1ByVolFlow()
    { 
        double nozzleMax = 25;
        foreach(NozzleTurbaData nozzleTurbaData in nozzleTurbaDataModel.NozzleTurbaDataList){
            if(nozzleTurbaData.Remark.Equals("Selected", StringComparison.Ordinal)){
                nozzleMax = nozzleTurbaData.Fmin1;
                break;
            }
        }
        return nozzleMax;
    }

    private double GetMaxNozzleByVolFlow()
    {  
        double nozzleMax = 51;
        foreach (NozzleTurbaData nozzleTurbaData in nozzleTurbaDataModel.NozzleTurbaDataList)
        {
            if (nozzleTurbaData.Remark.Equals("Selected", StringComparison.Ordinal))
            {
                 nozzleMax = nozzleTurbaData.InitLimit;
                 break;
            }
        }
        return nozzleMax;
    }
}
}