using System;
// using Microsoft.Office.Interop.Excel;
// using Turba_Interface;
// using DAT_Handler;
using OfficeOpenXml;
using System.IO.Packaging;
using OfficeOpenXml.Packaging;
using Models.LoadPointDataModel;
using Models.TurbaOutputDataModel;
using Interfaces.ILogger;
using Handlers.DAT_Handler;
using Turba.TurbaConfiguration;
using StartExecutionMain;

namespace ERG_PowerNoLoadOptimizer{
public class NoLoadPowerOptimizer
{
    private string excelPath =@"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
       
    private static int iteration;
    LoadPointDataModel loadPointDataModel;
    TurbaOutputModel turbaOutputModel;


        private ILogger logger;
        public NoLoadPowerOptimizer(){
            this.loadPointDataModel = LoadPointDataModel.getInstance();
            this.turbaOutputModel=TurbaOutputModel.getInstance();
            logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        }

    public void NoLoadPowerOptimize(int maxLP = 0)
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
            CalculatePwrSub1(maxLP);
            // init();
            // Outputs = package.Workbook.Worksheets["Output"];
            // TurbaOutputModel turbaOutputModel = TurbaOutputModel.getInstance();
            Pwr1 = turbaOutputModel.OutputDataList[9].Power_KW;
            Pwr2 = turbaOutputModel.OutputDataList[10].Power_KW;
            // Pwr1 = (double)Outputs.Cells["Q12"].GetValue<double>();
            // Pwr2 = (double)Outputs.Cells["Q13"].GetValue<double>();
            Logger($"Power LP9: {Pwr1} Power LP10: {Pwr2}");

            msflow1 = AdjustMsflow(msflow1, Pwr1, ref isOptimized1, maxLP);
            msflow2 = AdjustMsflow(msflow2, Pwr2, ref isOptimized2, maxLP);

            Logger($"MassFlow LP9: {msflow1} MassFlow LP10: {msflow2}");
        } while (!isOptimized1 || !isOptimized2);

        Logger($"Final Power LP9: {Pwr1} Power LP10: {Pwr2}");
    }

    private double AdjustMsflow(double msflow, double Pwr, ref bool isOptimized, int maxLP = 0)
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
            CalculatePwrSub1(maxLP);
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

    private void CalculatePwrSub1(int maxLP = 0)
    { 
      // init();
        Logger("Updating MassFlow and checking no load power.....");
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.PrepareDATFile_OnlyLPUpdate(maxLP);
        LaunchTurba(maxLP);
    }

    private void Logger(string message)
    {
            logger.LogInformation(message);
        //Console.WriteLine(message);
    }
    void LaunchTurba(int maxlP=0)

    {
        // init();
        TurbaConfig turbaConfig  = new TurbaConfig();
        turbaConfig.LaunchTurba(maxlP);
        // Implement the logic to launch Turba
    }

     void PrepareDATFile_OnlyLPUpdate()
    {
        // init();
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.PrepareDATFile_OnlyLPUpdate();
        //use the function from prepareDATFile
        // Implement the logic to prepare DAT file
    }
    

    
}
}