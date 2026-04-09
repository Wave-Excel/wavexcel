using System;
using System.IO;
// using Excel = Microsoft.Office.Interop.Excel; 
// using Microsoft.Office.Interop.Excel;
using System.IO.Packaging;
using OfficeOpenXml;
using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.IThermodynamicLibrary;
using Interfaces.ILogger;
using Models.PreFeasibility;
using Ignite_x_wavexcel;
using Models.TurbineData;
//using SensorKit;
// using OfficeOpenXml;

namespace HMBD.Ref_DAT_selector{ 
public class DatFileSelector
{
    // private Application excelApp;

    private ExcelPackage package;
    private IConfiguration configuration;
    
    private IThermodynamicLibrary thermodynamicService;
    private ILogger logger;

    private PreFeasibilityDataModel preFeasibilityDataModel;
        private TurbineDataModel turbineDataModel;
    // private ExcelPackage package;
     
    public DatFileSelector(string workbookPath)
    {
        configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        // excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        thermodynamicService =  StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        FileInfo existingFile = new FileInfo(workbookPath);
        package = new ExcelPackage(existingFile);
        turbineDataModel = TurbineDataModel.getInstance();
    }

    public void ReferenceDATSelector(int maxLp = 0)
    {
        getFlowPath(maxLp);
    }

        
    private void getFlowPath(int maxLp = 0)
    {
            // var sheetFeasibility = package.Workbook.Worksheets["Pre-Feasibility checks"];
        preFeasibilityDataModel.fillPrefeasibilityDecisionChecks();
        bool preFeasibilty_1 = (preFeasibilityDataModel.Decision == "TRUE")? true : false;  //Convert.ToBoolean(sheetFeasibility.Cells["G14"].Value);
        bool preFeasibilty_2 = (preFeasibilityDataModel.Decision_2 == "TRUE")? true: false; //Convert.ToBoolean(sheetFeasibility.Cells["G26"].Value);

        Logger("Pre feasibility criteria: " + preFeasibilty_1);
        Logger("HBD VolFlow: " + preFeasibilityDataModel.InletVolumetricFlowActualValue +
                ", HBD Power: " + preFeasibilityDataModel.PowerActualValue);

        string path = string.Empty;

        if (preFeasibilty_1)
        {
            Logger("Selecting from existing flow paths..");
            path = SelectStandard("Straight",maxLp);
            //turbineDataModel.DatFilePath = path;
        if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
                {
                    logger.moveLogs();
                    return;
        }
        if (preFeasibilityDataModel.Variant == -1)
        {
                TerminateIgniteX("CopyRefDATFile");
                return;
        }
                Logger($"Variant selected: {preFeasibilityDataModel.Variant}, path: {path}");
                CopyRefDATFile(path);
        }
        else
        {
            if (preFeasibilty_2)
            {
                Logger("Initiate Executed Flow Path search");
                MainExecutedClass executedFlow = new MainExecutedClass();
                executedFlow.GotoBCD1190(maxLp);
            }
            else
            {
                Logger("Initiate 2 GBC Design.."); 
                TurbineDesignPage.cts.Cancel();
            }
        }
    }

    private string SelectExecutedFlowPath()
    {
        var projectMaster = package.Workbook.Worksheets["ExecutedProjects"];
        var selectedProjects = package.Workbook.Worksheets["PowerNearest"];


        int filteredProjectsCount = (int)selectedProjects.Cells["H2"].Value;
        for (int project = 5; project <= filteredProjectsCount + 5; project++)
        {//H column
            if (selectedProjects.Cells[project, 8].Value == "Y")
            {
                string projectName = (string)(selectedProjects.Cells[project, 2]).Value;
                    int lastRow = projectMaster.Dimension.End.Row;
                    //int lastRow = projectMaster.Cells[projectMaster.Rows.Count, 3].End[Excel.XlDirection.xlUp].Row;

                    for (int row = 5; row <= lastRow; row++)
                {
                    if (((string)projectMaster.Cells[row,3].Value).Contains(projectName))
                    {
                        string projectID = projectMaster.Cells[row, 2].Value.ToString();
                            //accessing BJ
                        string projectDatFile = projectMaster.Cells[row, 62].Value.ToString(); 
                        Logger($"Selected Project id: {projectID}, Dat Path: {projectDatFile}");
                        return projectDatFile;
                    }
                }
            }
        }
        return string.Empty;
    }

    public string SelectStandard(string inputPath,int maxlp=0)
    {
        // var ws =  package.Workbook.Worksheets["Pre-Feasibility checks"];
        string cellValue = preFeasibilityDataModel.Variant.ToString();//.ws.Cells["G15"].Value.ToString();
        string sourceFilePath = AppContext.BaseDirectory;
        string destinationFolderPath = @"C:\testDir\";
            turbineDataModel.TurbineStatus = inputPath;
        string refDATfileName = "";
        List<FlowPathData> flowPathList = preFeasibilityDataModel.FlowPathDataList;
        double valF7 = preFeasibilityDataModel.InletPressureActualValue;//(double)ws.Cells["F7"].Value;
        double valF10 = preFeasibilityDataModel.InletVolumetricFlowActualValue;//(double)ws.Cells["F10"].Value;
        double valF11 = preFeasibilityDataModel.PowerActualValue;//(double)ws.Cells["F11"].Value;
        //logger.LogInformation(Convert.ToString(valF7));
        //logger.LogInformation(Convert.ToString(valF10));
        //logger.LogInformation(Convert.ToString(valF11));

        try{
            switch (inputPath)
            {
                case "NextLarger":
                    switch (cellValue)
                    {
                        case "3":
                            // preFeasibilityDataModel.Variant = 5;
                            preFeasibilityDataModel.Variant = 5;
                            refDATfileName = "66_5TURBAE1.DAT";
                            // package.Save();
                            break;
                        case "4":
                            preFeasibilityDataModel.Variant = 6;
                            refDATfileName = "46_5TURBAE2.DAT";
                            // package.Save();
                            break;
                        case "5":
                        case "6":
                            preFeasibilityDataModel.Variant = 7;
                            refDATfileName = "No Dat Found";
                            // package.Save();
                            break;
                    }
                    break;
                case "Straight":
                    if (valF10 < flowPathList[1].H && valF11 < flowPathList[1].G)
                    {
                        preFeasibilityDataModel.Variant = 1;
                        refDATfileName = "2_TURBAE1.DAT";
                        // package.Save();
                        Logger("Condition 1 met: F10 < 0.35 And F11 < 2000");
                    }
                    else if (valF10 < flowPathList[2].H && valF11 < flowPathList[2].G)
                    {
                        preFeasibilityDataModel.Variant = 2;
                        refDATfileName = "3_TURBAE1.DAT";
                        // package.Save();
                        Logger("Condition 2 met: F10 < 0.51 And F11 < 3500");
                    }
                    else if (valF7 > flowPathList[3].F)
                    {
                        if (valF10 < flowPathList[3].H && valF11 < flowPathList[3].G)
                        {
                            preFeasibilityDataModel.Variant = 3;
                            refDATfileName = "66_2_TURBAE1.DAT";
                            // package.Save();
                            Logger("Condition 3 met: F7 > 46 And F10 < 0.45 And F11 < 4000");
                        }
                        else if (valF10 < flowPathList[5].H && valF11 < flowPathList[5].G)
                        {
                            preFeasibilityDataModel.Variant = 5;
                            refDATfileName = "66_5TURBAE1.DAT";
                            // package.Save();
                            Logger("Condition 5 met: F7 > 46 And F10 < 0.52 And F11 < 7000");
                        }
                        else
                        {
                            
                            Logger("No conditions met in Pre feasibility ");
                                MainExecutedClass executedFlow = new MainExecutedClass();
                                executedFlow.GotoBCD1120(maxlp);
                                if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
                                {
                                    logger.moveLogs();
                                    return "";
                                }

                            }
                    }
                    else if (valF7 <= flowPathList[4].F)
                    {
                        if (valF10 < flowPathList[4].H && valF11 < flowPathList[4].G)
                        {
                            preFeasibilityDataModel.Variant = 4;
                            refDATfileName = "46_2_TURBAE2.DAT";
                            // package.Save();
                            Logger("Condition 4 met: F7 <= 46 And F10 < 0.7 And F11 < 4000");
                        }
                        else if (valF10 < flowPathList[6].H && valF11 < flowPathList[6].G)
                        {
                            preFeasibilityDataModel.Variant = 6;
                            refDATfileName = "46_5TURBAE2.DAT";
                            // package.Save();
                            Logger("Condition 6 met: F7 <= 46 And F10 < 0.7 And F11 < 7000");
                        }
                        else
                        {
                            preFeasibilityDataModel.Variant = 7;
                            refDATfileName = "No Dat Found";
                            // package.Save();
                            Logger("Condition 7 met: F7 <= 46 And F10 >= 0.7 Or F11 >= 7000");
                                //MainExecutedClass executedFlow = new MainExecutedClass();
                                //executedFlow.GotoBCD1120();
                            }
                    }
                    break;
                case "Opt2":
                    if (valF7 > flowPathList[4].F)
                    {
                        if (valF10 < flowPathList[3].H && valF11 < flowPathList[3].G)
                        {
                            preFeasibilityDataModel.Variant = 3;
                            refDATfileName = "66_2_TURBAE1.DAT";
                            // package.Save();
                            Logger("Condition 3 met: F7 > 46 And F10 < 0.45 And F11 < 4000");
                        }
                        else if (valF10 < flowPathList[5].H && valF11 < flowPathList[5].G)
                        {
                            preFeasibilityDataModel.Variant = 5;
                            refDATfileName = "66_5TURBAE1.DAT";
                            // package.Save();
                            Logger("Condition 5 met: F7 > 46 And F10 < 0.52 And F11 < 7000");
                        }
                        else
                        {
                            Logger("No conditions met in Pre feasibility ");
                                MainExecutedClass executedFlow = new MainExecutedClass();
                                executedFlow.GotoBCD1120(maxlp);
                        }
                    }
                    else if (valF7 <= flowPathList[4].F)
                    {
                        if (valF10 < flowPathList[4].H && valF11 < flowPathList[4].G)
                        {
                            preFeasibilityDataModel.Variant = 4;
                            refDATfileName = "46_2_TURBAE2.DAT";
                            // package.Save();
                            Logger("Condition 4 met: F7 <= 46 And F10 < 0.7 And F11 < 4000");
                        }
                        else if (valF10 < flowPathList[6].H && valF11 < flowPathList[6].G)
                        {
                            preFeasibilityDataModel.Variant = 6;
                            refDATfileName = "46_5TURBAE2.DAT";
                            // package.Save();
                            Logger("Condition 6 met: F7 <= 46 And F10 < 0.7 And F11 < 7000");
                        }
                        else
                        {
                            preFeasibilityDataModel.Variant = 7;
                            refDATfileName = "No Dat Found";
                            // package.Save();
                            Logger("Condition 7 met: F7 <= 46 And F10 >= 0.7 Or F11 >= 7000");
                        }
                    }
                    break;
                default:
                    Logger("Invalid inputPath type!");
                    break;
                
            }
        }
        catch(Exception ex){
            logger.LogError("SelectStandard", ex.Message);
        }
            turbineDataModel.DatFilePath = Path.Combine(sourceFilePath, refDATfileName);
            return Path.Combine(sourceFilePath, refDATfileName);
    }
        public string findNextVariant(int currentNo,int maxlP=0)
        {

            string sourceFilePath = AppContext.BaseDirectory;
            string refDATfileName = "";
            if (currentNo == 1)
            {
                refDATfileName = "3_TURBAE1.DAT";
                PreFeasibilityDataModel.getInstance().Variant = 2;
                return Path.Combine(sourceFilePath, refDATfileName);
            }
            else if (currentNo == 2)
            {

                return SelectStandard("Opt2", maxlP);
            }
            else if (currentNo == 3)
            {
                refDATfileName = "66_5TURBAE1.DAT";
                PreFeasibilityDataModel.getInstance().Variant = 5;
                return Path.Combine(sourceFilePath, refDATfileName);
            }
            else if (currentNo == 4)
            {
                refDATfileName = "46_5TURBAE2.DAT";
                PreFeasibilityDataModel.getInstance().Variant = 6;
                return Path.Combine(sourceFilePath, refDATfileName);
            }
            PreFeasibilityDataModel.getInstance().Variant = -1;
            return "";
        }
        public void CopyRefDATFile(string path)
        {
        if (!IsFileReadyForOpen(path))
        {
            TerminateIgniteX("CopyRefDATFile");
            return;
        }

        // Define variables
        string destinationFolderPath = @"C:\testDir\";
        string sourceFilePath = path;
        string[] AB = path.Split('\\');
        string refDATfileName = AB[AB.Length - 1];

        // Check if the file exists
        if (File.Exists(sourceFilePath))
        {
            // Copy the file to the destination folder
            Logger($"{sourceFilePath}, {destinationFolderPath}");
            //Path.GetFileName(sourceFilePath) -> replaced this with refDATfileName
            File.Copy(sourceFilePath, Path.Combine(destinationFolderPath, refDATfileName), true);

            Logger($"Copying reference DAT file in {destinationFolderPath} Folder.");
            string currentFile = Path.Combine(destinationFolderPath, refDATfileName);
            string newFile = Path.Combine(destinationFolderPath, "TURBATURBAE1.DAT.DAT");

            if (File.Exists(newFile))
            {
                Logger("File already exists, replacing old file with new DAT..");

            RetryFile:
                if (IsFileReadyForOpen(newFile))
                {
                    File.Delete(newFile);
                }
                else
                {
                    goto RetryFile;
                }
            }
            File.Move(currentFile, newFile);
                UpdateHeaderOrderUserDate(newFile, turbineDataModel.TSPID,turbineDataModel.UserName, DateTime.Now.ToString("dd.MM.yy"));

            }
            else
        {
            Logger("Source DAT file does not exist.");
        }
    }
        public void UpdateHeaderOrderUserDate(string filePath, string newOrderNr, string newUser, string newDate)
        {
            if (!File.Exists(filePath))
            {
                Logger("Target DAT file does not exist.");
                return;
            }

            var lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count == 0)
            {
                Logger("File is empty.");
                return;
            }

            // Defensive: pad header if too short
            string[] header = lines[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); ;  // ensure it's at least long enough for all header fields

            // Parse current fields
            string oldTyp = header[1];
            string oldInformation = header[header.Length - 1];

            // Trim or pad new data
            string orderNr = newOrderNr.Length > 12 ? newOrderNr.Substring(0, 12) : newOrderNr.PadRight(12);
            string user = newUser.Length > 12 ? newUser.Substring(0, 12) : newUser.PadRight(12);
            string date = newDate.Length > 8 ? newDate.Substring(0, 8) : newDate.PadRight(8);

            // Reconstruct header
            string newHeaderLine = orderNr + " " + oldTyp +"    " + user + date + oldInformation;

            // Update header
            lines[1] = newHeaderLine;

            File.WriteAllLines(filePath, lines);

            Logger("Header updated: ORDER-NR, USER, DATE.");
        }





        private bool IsFileReadyForOpen(string path)
    {
        // Check if the file exists and is not read-only
        if (File.Exists(path))
        {
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReadOnly) == 0;
        }
        return false;
    }

    private void TerminateIgniteX(string functionName)
    {
            if (TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                return;
            }
            // Implement your logic to terminate IgniteX
            TurbineDesignPage.cts.Cancel();
        logger.LogInformation($"Terminating IgniteX from {functionName}");
            //logger.LogInformation("Does not fit into any template");
            //Environment.Exit(0);
        }

    private void Logger(string message)
    {
        logger.LogInformation(message);
    }
}
}