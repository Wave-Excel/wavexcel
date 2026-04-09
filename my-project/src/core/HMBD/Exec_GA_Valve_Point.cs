using System;
using System.Runtime.InteropServices;
// using Excel = Microsoft.Office.Interop.Excel;
namespace HMBD.Exec_GA_Valve_Point;
using Models.TurbaOutputDataModel;
using Models.LoadPointDataModel;
using Turba.Exec_TurbaConfig;
using Handlers.Exec_DAT_Handler;

public class GAValvePoint
    {
        TurbaOutputModel turbaOutputModel;
        LoadPointDataModel loadPointDataModel;
        public GAValvePoint(){
            turbaOutputModel = TurbaOutputModel.getInstance();
            loadPointDataModel = LoadPointDataModel.getInstance();
        }
        
        
        // void Main(string[] args)
        // {
        //     OptimizeValue();
        // }
        public void OptimizeValue()
        {
            // Excel.Application excelApp = new Excel.Application();
            // Excel.Workbook workbook = excelApp.Workbooks.Open(@"C:\path\to\your\workbook.xlsx");
            // Excel.Worksheet loadPointsSheet = workbook.Sheets["LOAD_POINTS"];
            // Excel.Worksheet outputSheet = workbook.Sheets["Output"];
            List<LoadPoint>  lpPoint = loadPointDataModel.LoadPoints;
            double maxValue = 0.6 *  lpPoint[1].MassFlow ;// loadPointsSheet.Range["B4"].Value;
            double currentValue = maxValue;
            double minValue = 0.001;
            double targetMin = 0.00000000001;
            double targetMax = 0.01;
            double outputValue;
            do
            {
                // Set the input value
                lpPoint[6].MassFlow = currentValue;
                // Call the calculateAB function
                CalculateAB();
                // Get the output value
                outputValue = turbaOutputModel.OutputDataList[6].ABWEICHUNG;//   outputSheet.Range["D9"].Value;
                // Debug print the current input and output values
                Console.WriteLine($"Input: {currentValue} Output: {outputValue}");
                // Check if the output value is within the desired range
                if (outputValue >= targetMin && outputValue <= targetMax)
                {
                    Console.WriteLine("Optimal value found!");
                    Console.WriteLine($"Input: {currentValue} Output: {outputValue}");
                    break;
                }
                // Adjust the current value based on the output
                if (outputValue > targetMax)
                {
                    currentValue *= 0.9;  // Decrease by 10%
                }
                else if (outputValue < targetMin)
                {
                    currentValue *= 1.1;  // Increase by 10%
                }
                // Ensure the current value does not go below the minimum allowed value
                if (currentValue < minValue)
                {
                    currentValue = minValue;
                }
            } while (currentValue >= minValue);
        }
        public void CalculateAB()
        {
            PrepareDATFileOnlyLPUpdate();
            LaunchTurba();
        }
        public void PrepareDATFileOnlyLPUpdate()
        {
            ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
            datFileHandler.PrepareDatFileOnlyLPUpdate();
            // Implement the logic for PrepareDATFileOnlyLPUpdate
        }
        public void LaunchTurba()
        {
            TurbaAutomation turbaAutomation = new TurbaAutomation();
            turbaAutomation.LaunchTurba();
            // Implement the logic for LaunchTurba
        }
    }