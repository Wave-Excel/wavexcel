using System;
using System.Linq;
using OfficeOpenXml;
using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Turba.TurbaConfiguration;
using HMBD.HMBDInformation;
using Models.NozzleTurbaData;
using Interfaces.ILogger;
using Ignite_x_wavexcel;
// using System.Security.Cryptography;

namespace HMBD.Power_KNN;
public class PowerKNN
{
    // private Application excelApp;
    // private Workbook workbook;
    ExcelPackage package;
    TurbineDataModel turbineDataModel;
    private ILogger logger;
    private IConfiguration configuration;
    string excelPath;
    public PowerKNN()
    {
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        package = new ExcelPackage(new FileInfo(excelPath));
        logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        turbineDataModel = TurbineDataModel.getInstance();

    }

    public void ExecutePowerKNN(string criteria)
    {
        ExcelWorksheet wsPowerDB = package.Workbook.Worksheets["PowerDB"];
        ExcelWorksheet wsPowerNormDB = package.Workbook.Worksheets["PowerNormDB"];
        ExcelWorksheet wsPowerNearest = package.Workbook.Worksheets["PowerNearest"];

        // int lastRow = wsPowerDB.Cells[wsPowerDB.Rows.Count, 1].End(XlDirection.xlUp).Row;
        int totalRows = wsPowerDB.Dimension.End.Row;
        // Console.WriteLine("LASTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT :"+ totalRows);
        double[] normNewTurbine = new double[4];
        //double[] normNewTurbine = new double[5];
        double[] distances = new double[totalRows - 1];
        double[] filteredDistances = new double[totalRows - 1];
        int[] filteredIndexes = new int[totalRows - 1];
        int filteredCount = 0;
        double[] minList = new double[4];
        double[] maxList = new double[4];

        for (int i = 0; i <= 3; ++i)
        {
            minList[i] = double.MaxValue;
            maxList[i] = double.MinValue;
        }
        // double maxValue = double.MinValue;
        // double minValue = double.MaxValue;
        int startCol = 4;
        int endCol = 7;
        List<PowerNearest> powerNearestList = turbineDataModel.ListPower;
        for (int col = startCol; col <= endCol; col++)
        {
            // maxValue = double.MinValue;
            // minValue = double.MaxValue;
            for (int row = 2; row <= totalRows; row++)
            {
                var cellValue = wsPowerDB.Cells[row, col].Value;

                if (cellValue != null && double.TryParse(cellValue.ToString(), out double numericValue))
                {
                    maxList[col - startCol] = Math.Max(maxList[col - startCol], numericValue);
                    minList[col - startCol] = Math.Min(minList[col - startCol], numericValue);
                }
            }
        }

        Console.WriteLine("Powerrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr:" + powerNearestList[0].Power + ", " + turbineDataModel.AK25);
        normNewTurbine[0] = (powerNearestList[0].SteamPressure - minList[0]) / (maxList[0] - minList[0]);
        normNewTurbine[1] = (powerNearestList[0].SteamTemperature - minList[1]) / (maxList[1] - minList[1]);
        normNewTurbine[2] = (powerNearestList[0].SteamMass - minList[2]) / (maxList[2] - minList[2]);
        normNewTurbine[3] = (powerNearestList[0].ExhaustPressure - minList[3]) / (maxList[3] - minList[3]);

        // Console.WriteLine("IDOSONFJSBNFJNFSKJFNSKJFBSKJFBFJKSBFKS");
        // for(int i=0;i<=4;++i){
        //     Console.WriteLine("MAX:"+ maxList[i]+", MIN:"+minList[i]);
        //     Console.WriteLine(normNewTurbine[i]+", ");
        // }
        for (int i = 2; i <= totalRows; i++)
        {
            distances[i - 2] = 0;
            for (int j = 0; j < 4; j++)
            {
                distances[i - 2] += Math.Pow((double)wsPowerNormDB.Cells[i, j + startCol].Value - normNewTurbine[j], 2);
            }
            distances[i - 2] = Math.Sqrt(distances[i - 2]);
        }
        // Filter distances based on criteria
        for (int i = 2; i <= totalRows; i++)
        {
            bool criteriaMet = false;
            switch (criteria)
            {
                case "BCD1120":
                    if ((double)wsPowerNormDB.Cells[i, 8].Value >= 1120 && (double)wsPowerNormDB.Cells[i, 8].Value <= 1130)
                    {
                        criteriaMet = true;
                    }
                    break;
                case "BCD1190":
                    if ((double)wsPowerNormDB.Cells[i, 8].Value >= 1190 && (double)wsPowerNormDB.Cells[i, 8].Value <= 1210)
                    {
                        criteriaMet = true;
                    }
                    break;
                case "Throttle":
                    if (wsPowerNormDB.Cells[i, 9].Value.ToString() == "Throttle")
                    {
                        criteriaMet = true;
                    }
                    break;
                case "CustomLoadCases":
                    criteriaMet = true;
                    break;
            }

            if (criteriaMet)
            {
                filteredCount++;
                filteredDistances[filteredCount - 1] = distances[i - 2];
                filteredIndexes[filteredCount - 1] = i;
            }
        }

        // If no valid indexes, exit the subroutine
        if (filteredCount == 0)
        {
            Logger("No valid turbines found for the given criteria.");
            return;
        }

        // Resize arrays to match the number of filtered turbines
        Array.Resize(ref filteredDistances, filteredCount);
        Array.Resize(ref filteredIndexes, filteredCount);

        // Get the value of k from Customer_Inputs cell G2
        int k = Convert.ToInt32(powerNearestList[0].KNearest);//(int)wsPowerNearest.Cells[2, "H"].Value;
        // Adjust k if criteria is "Throttle"
        if (criteria == "Throttle" && k > 2)
        {
            k = 2;
        }

        // Sort distances and get the k-nearest neighbors
        SortWithIndexes(filteredDistances, filteredIndexes, filteredCount);
        // Clear previous results
        // wsPowerNearest.Range["A5:G" + wsPowerNearest.Rows.Count].ClearContents();

        // Print the k-nearest neighbors' details back after denormalizing
        int outputRow = 1;
        for (int i = 0; i < k; i++)
        {
            powerNearestList[outputRow].Efficiency = Convert.ToDouble(wsPowerNormDB.Cells[filteredIndexes[i], 1].Value);
            powerNearestList[outputRow].ProjectName = wsPowerNormDB.Cells[filteredIndexes[i], 2].Value.ToString();
            powerNearestList[outputRow].ProjectID = wsPowerNormDB.Cells[filteredIndexes[i], 10].Value.ToString();
            Console.WriteLine(powerNearestList[outputRow].ProjectName);
            // Copy columns A and B from NormDB
            // wsPowerNearest.Cells[outputRow, 1].Value = wsPowerNormDB.Cells[filteredIndexes[i], 1].Value;
            // wsPowerNearest.Cells[outputRow, 2].Value = wsPowerNormDB.Cells[filteredIndexes[i], 2].Value;
            // for (int col = 3; col <= 7; col++)
            // {
            //     // Range valRange = wsPowerDB.Range[wsPowerDB.Cells[2, col], wsPowerDB.Cells[lastRow, col]];
            //     double minVal = minList[col - 3];//excelApp.WorksheetFunction.Min(valRange);
            //     double maxVal = maxList[col - 3];//excelApp.WorksheetFunction.Max(valRange);
            //     wsPowerNearest.Cells[outputRow, col].Value = (double)wsPowerNormDB.Cells[filteredIndexes[i], col].Value * (maxVal - minVal) + minVal;
            // }
            //powerNearestList[outputRow].Power = (double)wsPowerNormDB.Cells[filteredIndexes[i], 3].Value * (maxList[0] - minList[0]) + minList[0];
            powerNearestList[outputRow].SteamPressure = (double)wsPowerNormDB.Cells[filteredIndexes[i], 4].Value * (maxList[0] - minList[0]) + minList[0];
            powerNearestList[outputRow].SteamTemperature = (double)wsPowerNormDB.Cells[filteredIndexes[i], 5].Value * (maxList[1] - minList[1]) + minList[1];
            powerNearestList[outputRow].SteamMass = (double)wsPowerNormDB.Cells[filteredIndexes[i], 6].Value * (maxList[2] - minList[2]) + minList[2];
            powerNearestList[outputRow].ExhaustPressure = (double)wsPowerNormDB.Cells[filteredIndexes[i], 7].Value * (maxList[3] - minList[3]) + minList[3];

            outputRow++;
        }
        if (TurbineDesignPage.solveForHigherEfficiencyFlag)
        {
            for (int i = 1; i < k; i++)
            {
                powerNearestList[i].KNearest = "";
            }
            if (powerNearestList.Count > 1)
            {
                // Create a sublist starting from index 1 to the end of the list
                List<PowerNearest> sublist = powerNearestList.GetRange(1, powerNearestList.Count - 1);

                // Sort the sublist based on efficiency in descending order
                sublist.Sort((p1, p2) => p2.Efficiency.CompareTo(p1.Efficiency));

                // Update the original list with the sorted sublist
                for (int i = 1; i < powerNearestList.Count; i++)
                {
                    powerNearestList[i] = sublist[i - 1];
                }
            }
            if (MainExecutedClass.row >= 1)
            {
                powerNearestList[MainExecutedClass.row].KNearest = "Y";
            }
        }


        Logger("K-nearest neighbors found and denormalized details printed!");
    }

    private void Logger(String message)
    {
        logger.LogInformation(message);
    }
    private void SortWithIndexes(double[] arr, int[] indexes, int N)
    {
        var combined = new (double value, int index)[N];
        for (int i = 0; i < N; i++)
        {
            combined[i] = (arr[i], indexes[i]);
        }
        Array.Sort(combined, (a, b) => a.value.CompareTo(b.value));
        for (int i = 0; i < N; i++)
        {
            arr[i] = combined[i].value;
            indexes[i] = combined[i].index;
        }
    }

}