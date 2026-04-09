namespace Optimizers.Exec_ERG_PowerNoLoadOptimizer;
using OfficeOpenXml;
using System.IO.Packaging;
using OfficeOpenXml.Packaging;
using Models.LoadPointDataModel;
using Models.TurbaOutputDataModel;
using Handlers.Exec_DAT_Handler;
using Turba.Exec_TurbaConfig;
using Interfaces.ILogger;
using StartExecutionMain;

public class ExecNoLoadPowerOptimizer
{
    // private string excelPath =@"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
       
    private static int iteration;
    LoadPointDataModel loadPointDataModel;
    TurbaOutputModel turbaOutputModel;
    ILogger logger;
    public ExecNoLoadPowerOptimizer(){
      this.loadPointDataModel = LoadPointDataModel.getInstance();
      this.turbaOutputModel=TurbaOutputModel.getInstance();
        if (logger == null)
        {
            logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        }

    }

    public void NoLoadPowerOptimize(int maxlp = 0)
    {  
      //  init();
        // LoadPointDataModel loadPointDataModel = LoadPointDataModel.getInstance();
        List<LoadPoint> loadPoints = loadPointDataModel.LoadPoints;
        double msflow1 = loadPoints[9].MassFlow;
        double msflow2 = loadPoints[10].MassFlow;
        // double msflow1 = LPSheet.Cells["B13"].GetValue<double>();
        // double msflow2 = (double)LPSheet.Cells["B14"].GetValue<double>();
        double Pwr1, Pwr2;
        bool isOptimized1 = false, isOptimized2 = false;

        iteration = 0;
        Logger("Attempting to adjust NLP MassFlow ....");
        Logger("---------------------------------------");

        do
        {
            // init();
            iteration++;
            loadPoints[9].MassFlow=msflow1;
            loadPoints[10].MassFlow=msflow2;
            // LPSheet.Cells["B13"].Value = msflow1;
            // LPSheet.Cells["B14"].Value = msflow2;
            // package.Save();
            CalculatePwrSub1(maxlp);
            // init();
            // Outputs = package.Workbook.Worksheets["Output"];
            // TurbaOutputModel turbaOutputModel = TurbaOutputModel.getInstance();
            Pwr1 = turbaOutputModel.OutputDataList[9].Power_KW;
            Pwr2 = turbaOutputModel.OutputDataList[10].Power_KW;
            // Pwr1 = (double)Outputs.Cells["Q12"].GetValue<double>();
            // Pwr2 = (double)Outputs.Cells["Q13"].GetValue<double>();
            Logger($"ex_Power_KNN.Power LP9: {Pwr1} Power LP10: {Pwr2}");

            msflow1 = AdjustMsflow(msflow1, Pwr1, ref isOptimized1);
            msflow2 = AdjustMsflow(msflow2, Pwr2, ref isOptimized2);

            Logger($"MassFlow LP9: {msflow1} MassFlow LP10: {msflow2}");
        } while (!isOptimized1 || !isOptimized2);

        Logger($"Final ex_ex_Power_KNN.Power_KNN.Power LP9:  {Pwr1} Power LP10: {Pwr2}");
    }
    public void LP5LoadPowerOptimize()
    {
        
        List<LoadPoint> loadPoints = loadPointDataModel.LoadPoints;
        //double msflow1 = loadPoints[9].MassFlow;
        double msflow = loadPoints[5].MassFlow;
        // double msflow1 = LPSheet.Cells["B13"].GetValue<double>();
        // double msflow2 = (double)LPSheet.Cells["B14"].GetValue<double>();
        double Pwr;
        bool isOptimized1 = false, isOptimized2 = false;

        iteration = 0;
        Logger("Attempting to adjust NLP MassFlow ....");
        Logger("---------------------------------------");

        //do
        //{
        //    // init();
        //    iteration++;
        loadPoints[5].MassFlow = msflow * 0.80;
            
            CalculatePwrSub1();
            
            Pwr = turbaOutputModel.OutputDataList[5].Power_KW;
            
        //    Logger($"ex_Power_KNN.Power LP5: {Pwr}");

        //    msflow = AdjustMsflowlp5(msflow, Pwr, ref isOptimized1);
        //    //msflow2 = AdjustMsflow(msflow2, Pwr2, ref isOptimized2);

        //    Logger($"MassFlow LP5: {msflow}");
        //} while (!isOptimized1);

        Logger($"Final ex_ex_Power_KNN.Power_KNN.Power LP5:  {Pwr}");
    }
    private double AdjustMsflowlp5(double msflow, double Pwr, ref bool isOptimized)
    {
        if (Pwr >= turbaOutputModel.OutputDataList[1].Power_KW - 30 && Pwr <= turbaOutputModel.OutputDataList[1].Power_KW + 20)
        {
            isOptimized = true;
        }
        else
        {
            if (Pwr > turbaOutputModel.OutputDataList[1].Power_KW + 20)
            {
                msflow *= 0.80; // Decrease msflow by 20%
            }
            else if (Pwr < turbaOutputModel.OutputDataList[1].Power_KW - 30)
            {
                msflow *= 1.1; // Increase msflow by 20%
            }
            // LoadPointDataModel loadPointDataModel = LoadPointDataModel.getInstance();
            List<LoadPoint> loadPoints = loadPointDataModel.LoadPoints;
            loadPoints[5].MassFlow = msflow;
            //loadPoints[10].MassFlow = msflow;
            // LPSheet.Cells["B13"].Value = msflow;
            // LPSheet.Cells["B14"].Value = msflow;
            // package.Save();
            CalculatePwrSub1();
            // init();
            // TurbaOutputModel turbaOutputModel = TurbaOutputModel.getInstance();
            Pwr = turbaOutputModel.OutputDataList[5].Power_KW;
            // Pwr = (double)Outputs.Cells["Q12"].GetValue<double>();

            // Or Outputs.Cells["Q13"].Value depending on context
            if (Pwr < turbaOutputModel.OutputDataList[1].Power_KW - 30)
            {
                msflow *= 1.1; // Increase msflow by 10%
            }
            else if (Pwr > turbaOutputModel.OutputDataList[1].Power_KW + 20)
            {
                msflow *= 0.7; // Decrease msflow by 10%
            }
        }
        return msflow;
    }
    private double AdjustMsflow(double msflow, double Pwr, ref bool isOptimized)
    {
        if (Pwr >= 50 && Pwr <= 100)
        {
            isOptimized = true;
        }
        else
        {
            if (Pwr > 100)
            {
                msflow *= 0.8; // Decrease msflow by 20%
            }
            else if (Pwr < 50)
            {
                msflow *= 1.2; // Increase msflow by 20%
            }
            // LoadPointDataModel loadPointDataModel = LoadPointDataModel.getInstance();
            List<LoadPoint> loadPoints = loadPointDataModel.LoadPoints;
            loadPoints[9].MassFlow = msflow;
            loadPoints[10].MassFlow = msflow;
            // LPSheet.Cells["B13"].Value = msflow;
            // LPSheet.Cells["B14"].Value = msflow;
            // package.Save();
            CalculatePwrSub1();
            // init();
            // TurbaOutputModel turbaOutputModel = TurbaOutputModel.getInstance();
            Pwr = turbaOutputModel.OutputDataList[9].Power_KW;
            // Pwr = (double)Outputs.Cells["Q12"].GetValue<double>();

            // Or Outputs.Cells["Q13"].Value depending on context
            if (Pwr < 50)
            {
                msflow *= 1.1; // Increase msflow by 10%
            }
            else if (Pwr > 100)
            {
                msflow *= 0.9; // Decrease msflow by 10%
            }
        }
        return msflow;
    }



    private void CalculatePwrSub1(int maxlp = 0)
    { 
      // init();
        Logger("Updating massflow and checking no ex_ERG_RsminHandler.load power.....");
        // DATFileProcessor dATFileProcessor = new DATFileProcessor();
        ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        datFileHandler.PrepareDatFileOnlyLPUpdate(maxlp);
            // dATFileProcessor.PrepareDATFile_OnlyLPUpdate();
        LaunchTurba(maxlp);
    }

    private void Logger(string message)
    {
        Console.WriteLine(message);
        logger.LogInformation(message);
        
    }
    void LaunchTurba(int maxlp = 0)

    {
      TurbaAutomation turbaAutomation = new TurbaAutomation();
      turbaAutomation.LaunchTurba(maxlp);
        // init();
        // TurbaConfig turbaConfig  = new TurbaConfig();
        // turbaConfig.LaunchTurba();
        // Implement the logic to launch Turba
    }

     void PrepareDATFile_OnlyLPUpdate()
    {
      ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        datFileHandler.PrepareDatFileOnlyLPUpdate();
        // // init();
        // DATFileProcessor dATFileProcessor = new DATFileProcessor();
        // dATFileProcessor.PrepareDATFile_OnlyLPUpdate();
        // //use the function from prepareDATFile
        // // Implement the logic to prepare DAT file
    }
    

    
}
