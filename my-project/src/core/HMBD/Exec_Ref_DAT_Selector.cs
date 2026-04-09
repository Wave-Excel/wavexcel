using System;
using System.IO;
namespace HMBD.Exec_Ref_DAT_Selector;
using System.IO.Packaging;
using Microsoft.Extensions.Configuration;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Interfaces.IThermodynamicLibrary;
using Interfaces.ILogger;
using Models.PreFeasibility;
using Models.ExecutedProjectDB;
using Models.TurbineData;
using Exec_HMBD_Configuration;
using StartExecutionMain;
using System.Runtime.InteropServices.Marshalling;
using Ignite_x_wavexcel;

public class FlowPathSelector
{
    private IConfiguration configuration;
    private IThermodynamicLibrary thermodynamicService;
    private ILogger logger;
    private PreFeasibilityDataModel preFeasibilityDataModel;
    // private ExecutedProjectDB executedProjectDB;

    // private ExecutedProjectDB executedProjectDB;
    private ExecutedDB executedDB;
    private TurbineDataModel turbineDataModel;
    public FlowPathSelector()
    {
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        thermodynamicService =  MainExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        // executedProjectDB = ExecutedProjectDB.getInstance();
        turbineDataModel = TurbineDataModel.getInstance();
        executedDB = ExecutedDB.getInstance();
    }

    public void ReferenceDATSelectorExecuted(string criteria)
    {
        GetFlowPathExecuted(criteria);
    }

    public string ReferenceDATSelector()
    {
        return GetFlowPath();
    }

    public string GetFlowPath()
    {
        // Worksheet sheetFeasibility = workbook.Worksheets["Pre-Feasibility checks"];
        string GetFlowPath = "";
        bool preFeasibility1 = (preFeasibilityDataModel.Decision == "TRUE") ? true : false;//sheetFeasibility.Range["G14"].Value;
        bool preFeasibility2 = (preFeasibilityDataModel.Decision_2=="TRUE")?true:false;//(bool)sheetFeasibility.Range["G26"].Value;

        Logger("Pre feasibility criteria: " + preFeasibility1);
        Logger("HBD VolFlow: " + preFeasibilityDataModel.InletVolumetricFlowActualValue + ", HBD Power: " +  preFeasibilityDataModel.PowerActualValue);

        if (preFeasibility1)
        {
            Logger("Selecting from existing flow paths..");
            string path = SelectStandard("Straight");
            Console.WriteLine(path);
            if (string.IsNullOrEmpty(path))
            {
                Logger("No Standard Variant Found, Selecting Executed Project...");
                GetFlowPath = "Exe";
                MainExecuted("BCD1120");
            }
            else
            {
                Logger("Variant selected: " + preFeasibilityDataModel.Variant + ", path: " + path);
                CopyRefDATFile(path);
                GetFlowPath = "Std";
            }
        }
        else
        {
            if (preFeasibility2)
            {
                if ((double) preFeasibilityDataModel.InletVolumetricFlowActualValue_2 < 0.9)
                {
                    Logger("Initiate Executed Flow Path search");
                    GetFlowPath = "Exe";
                    MainExecuted("BCD1190");
                }
                else
                {
                    Logger("Going to Throttle...");
                    MainExecuted("Throttle");
                }
            }
            else
            {

                Logger("Initiate 2 GBC Design..");
                TurbineDesignPage.cts.Cancel();
            }
        }

        return GetFlowPath;
    }

    public virtual void GetFlowPathExecuted(string criteria)
    {
        string path = string.Empty;
        turbineDataModel.TurbineStatus = criteria;

        switch (criteria)
        {
            case "BCD1120":
                // Console.WriteLine("Executing flow path for 1120 ...");
                Logger("Executing flow path for BCD1120 ...");
                path = SelectExecutedFlowPath("BCD1120");
                CopyRefDATFile(path);
                break;

            case "BCD1190":
                // Console.WriteLine("Executing flow path for BCD1190 ...");
                Logger("Executing flow path for BCD1190 ...");
                path = SelectExecutedFlowPath("BCD1190");
                //Console.WriteLine("Pathhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"+path);
                CopyRefDATFile(path);
                break;

            case "Throttle":
                Console.WriteLine("Executing flow path for Throttle ...");
                Logger("Executing flow path for Throttle ...");
                path = SelectExecutedFlowPath("Throttle");
                CopyRefDATFile(path);
                break;

            default:
                Logger("No valid criteria met.");
                Console.WriteLine("No valid criteria met.");
                break;
        }
    }

    public virtual string SelectExecutedFlowPath(string criteria)
    {
        // Worksheet projectMaster = workbook.Worksheets["ExecutedProjects"];
        List<PowerNearest> powerNearest = turbineDataModel.ListPower;
        // Worksheet selectedProjects = workbook.Worksheets["PowerNearest"];
        // Console.WriteLine("SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS:"+powerNearest.Count+","+powerNearest[0].SteamMass);
        int filteredProjectsCount = Convert.ToInt32(powerNearest[0].KNearest);//selectedProjects.Range["H2"].Value;
        Console.WriteLine("filtersssssss Projectttttttttttttttttttttt"+filteredProjectsCount);
        List<ExecutedProject> db = executedDB.ExecutedProjectDB;

        for (int project = 1; project <= filteredProjectsCount; project++)
        {
            if (powerNearest[project].KNearest == "Y")
            {
                string projectName = powerNearest[project].ProjectName;//   selectedProjects.Cells[project, "B"].Value.ToString();
                Console.WriteLine("PROJECTTTTTTTTTTTTTTTTTTT:"+ projectName);
                for (int row = 0; row < db.Count(); row++)
                {
                    
                    if (db[row].Project.Contains(projectName))
                    {
                        string projectID = db[row].ProjectID;//projectMaster.Cells[row, "B"].Value.ToString();
                        turbineDataModel.ClosestProjectID = db[row].ProjectID;
                        turbineDataModel.ClosestProjectName = projectName;
                        turbineDataModel.DatFilePath = db[row].DatFilePath;
                        string projectDatFile = db[row].DatFilePath;//projectMaster.Cells[row, "BJ"].Value.ToString();
                        Logger("Selected Project id: " + projectID + " Dat Path: " + projectDatFile);
                        return projectDatFile;
                    }
                }
            }
        }

        return string.Empty;
    }

    public string SelectStandard(string inputPath)
    {
        // Worksheet ws = workbook.Worksheets["Pre-Feasibility checks"];
        string cellValue = preFeasibilityDataModel.Variant.ToString(); //ws.Range["G15"].Value.ToString();
        string sourceFilePath = @"C:\testDir\projects_repository\standard_flowPaths\";
        string destinationFolderPath = @"C:\testDir\";
        string refDATfileName = string.Empty;
        double valF10 = (double) preFeasibilityDataModel.InletVolumetricFlowActualValue; // ws.Range["F10"].Value;
                double valF11 = (double) preFeasibilityDataModel.PowerActualValue; //ws.Range["F11"].Value;
                double valF7 = (double) preFeasibilityDataModel.InletPressureActualValue;// ws.Range["F7"].Value;
                List<FlowPathData> flowPaths = preFeasibilityDataModel.FlowPathDataList;
        switch (inputPath)
        {
            case "NextLarger":
                switch (cellValue)
                {
                    case "3":
                        preFeasibilityDataModel.Variant = 5;
                        refDATfileName = "66_5TURBAE1.DAT";
                        break;
                    case "4":
                        preFeasibilityDataModel.Variant = 6;
                        refDATfileName = "46_5TURBAE2.DAT";
                        break;
                    case "5":
                    case "6":
                        preFeasibilityDataModel.Variant = 7;
                        refDATfileName = string.Empty;
                        break;
                }
                break;

            case "Straight":
                
                // flowPaths[1].H;
                if (valF10 < (double)flowPaths[1].H && valF11 < (double) flowPaths[1].G)
                {
                    
                    preFeasibilityDataModel.Variant = 1;
                    refDATfileName = "2_TURBAE1.DAT";
                }
                else if (valF10 < (double) flowPaths[2].H && valF11 < (double) flowPaths[2].G)
                {
                    preFeasibilityDataModel.Variant = 2;
                    refDATfileName = "3_TURBAE1.DAT";
                }
                else if (valF7 > (double) flowPaths[3].F)
                {
                    if (valF10 < (double)flowPaths[3].H && valF11 < (double) flowPaths[3].G)
                    {
                        preFeasibilityDataModel.Variant = 3;
                        refDATfileName = "66_2_TURBAE1.DAT";
                    }
                    else if (valF10 < (double) flowPaths[5].H && valF11 < (double) flowPaths[5].G)
                    {
                        preFeasibilityDataModel.Variant = 5;
                        refDATfileName = "66_5TURBAE1.DAT";
                    }
                    else
                    {
                        refDATfileName = string.Empty;
                    }
                }
                else if (valF7 <= (double) flowPaths[4].F)
                {
                    if (valF10 < (double) flowPaths[4].H  && valF11 < (double) flowPaths[4].G)
                    {
                        preFeasibilityDataModel.Variant = 4;
                        refDATfileName = "46_2_TURBAE2.DAT";
                    }
                    else if (valF10 < (double) flowPaths[6].H && valF11 < (double) flowPaths[6].G)
                    {
                        preFeasibilityDataModel.Variant = 6;
                        refDATfileName = "46_5TURBAE2.DAT";
                    }
                    else
                    {
                        preFeasibilityDataModel.Variant = 7;
                        refDATfileName = "No Dat Found";
                    }
                }
                break;

            case "Opt2":
                if (valF7 > (double) flowPaths[4].F)
                {
                    if (valF10 < (double) flowPaths[3].H && valF11 < (double) flowPaths[3].G)
                    {
                        preFeasibilityDataModel.Variant = 3;
                        refDATfileName = "66_2_TURBAE1.DAT";
                    }
                    else if (valF10 < (double) flowPaths[5].H && valF11 < (double) flowPaths[5].G)
                    {
                        preFeasibilityDataModel.Variant = 5;
                        refDATfileName = "66_5TURBAE1.DAT";
                    }
                    else
                    {
                        refDATfileName = string.Empty;
                    }
                }
                else if (valF7 <= (double) flowPaths[4].F)
                {
                    if (valF10 < (double) flowPaths[4].H && valF11 < (double) flowPaths[4].G)
                    {
                        preFeasibilityDataModel.Variant = 4;
                        refDATfileName = "46_2_TURBAE2.DAT";
                    }
                    else if (valF10 < (double) flowPaths[6].H && valF11 < (double) flowPaths[6].G)
                    {
                        preFeasibilityDataModel.Variant = 6;
                        refDATfileName = "46_5TURBAE2.DAT";
                    }
                    else
                    {
                        preFeasibilityDataModel.Variant = 7;
                        refDATfileName = string.Empty;
                    }
                }
                break;

            default:
                Console.WriteLine("Invalid inputPath type!");
                break;
        }

        if (string.IsNullOrEmpty(refDATfileName))
        {
            return string.Empty;
        }
        else
        {
            return Path.Combine(sourceFilePath, refDATfileName);
        }
    }

    public int AddOrMoveY()
    {

        List<PowerNearest> powerNearest = turbineDataModel.ListPower;
        // for(int i=1;i<=)
        int k = Convert.ToInt32(powerNearest[0].KNearest);
        for(int i=1;i<=k;i++){
            Console.WriteLine("Project nameeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"+powerNearest[i].ProjectName);
        }
        bool foundY = false;
        // long AddOrMoveY = -1;
        int lastRow = 1;
        for(int i=1;i<=k;i++){
          if(powerNearest[i].KNearest=="Y"){
              foundY  = true;
              lastRow = i;
          }
        }
        lastRow  = foundY == false ? 1 : lastRow+1;
        

        bool isLeftColsEmpty = true;
        if(lastRow <=k){
          if(powerNearest[lastRow].SteamPressure>0){
            isLeftColsEmpty = false;
          }
        }
        

        if (isLeftColsEmpty)
        {
            Console.WriteLine("All nearest neighbours tried, proceeding to the next workflow");
            // MainExecuted("BCD1190");
            // changes i =2 to 1
            for(int i=1;i<=k;i++){
              if(powerNearest[i].KNearest=="Y"){
                powerNearest[i].KNearest="";
              }
            }

            return -1;
        }
        else
        {
            powerNearest[(int)lastRow].KNearest="Y";
            //powerNearest[(int)lastRow - 1].KNearest = "";
            if (TurbineDesignPage.solveForHigherEfficiencyFlag)
            {
                MainExecutedClass.row++;
            }

            // selectedProjects.Cells[lastRow, "H"].Value = "Y";
            powerNearest[0].Efficiency = powerNearest[(int)lastRow].Efficiency;
            // selectedProjects.Cells[2, "A"].Value = selectedProjects.Cells[lastRow, "A"].Value;

            if (foundY == true)
            {
                powerNearest[(int)lastRow-1].KNearest="";
            }

            // AddOrMoveY = lastRow;
            return lastRow;
        }
    }

    public void MoveYAndSetParams()
    {
        int updatedRow = AddOrMoveY();
        Console.WriteLine("UpdateRowwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww"+updatedRow);

        if (updatedRow != -1)
        {
            UpdateHBDParamsExecuted(updatedRow);
        }
        else
        {
            Console.WriteLine("Moving to Next");
        }
    }

    public virtual void UpdateHBDParamsExecuted(int updatedRow){
            ExecHMBDConfiguration execHMBDConfiguration = new ExecHMBDConfiguration();
            execHMBDConfiguration.UpdateHBDParamsExecuted(updatedRow);
    }

    public void CopyRefDATFile(string path)
    {
        if (!IsFileReadyForOpen(path))
        {
            TerminateIgniteX("copyRefDATFile");
        }

        string destinationFolderPath = @"C:\testDir\";
        string sourceFilePath = path;
        string refDATfileName = Path.GetFileName(path);
        string newFile = Path.Combine(destinationFolderPath, "TURBATURBAE1.DAT.DAT");

        if (IsFileReadyForOpen(path))
        {
            Console.WriteLine("Server is up and files are available...");
        }
        else
        {
            Console.WriteLine("Server is down files are unavailable...");
        }

        if (File.Exists(sourceFilePath))
        {
            File.Copy(sourceFilePath, Path.Combine(destinationFolderPath, refDATfileName), true);
            Logger("Copying reference DAT file in " + destinationFolderPath + " Folder.");

            if (File.Exists(newFile))
            {
                Logger("File already exists, replacing old file with new DAT..");
                if (IsFileReadyForOpen(newFile))
                {
                    File.Delete(newFile);
                }
            }

            File.Move(Path.Combine(destinationFolderPath, refDATfileName), newFile);
            UpdateHeaderOrderUserDate(newFile, turbineDataModel.TSPID, turbineDataModel.UserName, DateTime.Now.ToString("dd.MM.yy"));
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
        string newHeaderLine = orderNr + " " + oldTyp + "    " + user + date + oldInformation;

        // Update header
        lines[1] = newHeaderLine;

        File.WriteAllLines(filePath, lines);

        Logger("Header updated: ORDER-NR, USER, DATE.");
    }

    public bool IsFileReadyForOpen(string path)
    {
        try
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            return false;
        }

        return true;
    }

    public virtual void TerminateIgniteX(string functionName)
    {
        Environment.Exit(0);
    }
    public virtual void MainExecuted(string criteria)
    {
       MainExecutedClass mainExec = new MainExecutedClass();
       mainExec.MainExecuted(criteria);
    }

    public void Logger(string message)
    {
        // Implement the logic to log messages
        logger.LogInformation(message);
    }

    public void Close()
    {
        // workbook.Close(false);
        // excelApp.Quit();
    }
}

// class Program
// {
//     static void Main(string[] args)
//     {
//         // FlowPathSelector selector = new FlowPathSelector("path_to_your_workbook.xlsx");

//         // Example usage
//         // selector.ReferenceDATSelectorExecuted("BCD1120");
//         // string flowPath = selector.ReferenceDATSelector();

//         // selector.Close();
//     }
// }
