namespace HMBD.Cu_Ref_DAT_Selector;
using System;
using System.IO;
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
using HMBD.Exec_Ref_DAT_Selector;
using HMBD.CustomPower_KNN;
using Cu_HMBD_Configuration;

// using StartExecutionMain;

public class CuFlowPathSelector : FlowPathSelector
{
    private IConfiguration configuration;
    private IThermodynamicLibrary thermodynamicService;
    private ILogger logger;
    private PreFeasibilityDataModel preFeasibilityDataModel;
    private ExecutedDB executedDB;
    private TurbineDataModel turbineDataModel;
    public CuFlowPathSelector()
    {
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        thermodynamicService =  CustomExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        // executedProjectDB = ExecutedProjectDB.getInstance();
        turbineDataModel = TurbineDataModel.getInstance();
        executedDB = ExecutedDB.getInstance();
    }
    public override void GetFlowPathExecuted(string criteria){
        string path;

        // Use the criteria to determine the flow
        switch (criteria)
        {
            case "BCD1120":
                Console.WriteLine("Executing flow path for 1120 ...");
                Logger("Executing flow path for BCD1120 ...");
                path = SelectExecutedFlowPath("BCD1120");
                CopyRefDATFile(path);
                break;

            case "BCD1190":
                Console.WriteLine("Executing flow path for BCD1190 ...");
                Logger("Executing flow path for BCD1190 ...");
                path = SelectExecutedFlowPath("BCD1190");
                CopyRefDATFile(path);
                break;

            case "Throttle":
                Console.WriteLine("Executing flow path for Throttle ...");
                Logger("Executing flow path for Throttle ...");
                path = SelectExecutedFlowPath("Throttle");
                CopyRefDATFile(path);
                break;

            case "All":
                Console.WriteLine("Executing flow path for All ...");
                Logger("Executing flow path for All ...");
                path = SelectExecutedFlowPath("All");
                CopyRefDATFile(path);
                break;

            case "Nozzle":
                Console.WriteLine("Executing flow path for cu_DAT_CustomPath_DeletePrepare.Nozzle ...");
                Logger("Executing flow path for cu_DAT_CustomPath_DeletePrepare.Nozzle ...");
                path = SelectExecutedFlowPath("Nozzle");
                CopyRefDATFile(path);
                break;

            default:
                Logger("No valid criteria met.");
                Console.WriteLine("No valid criteria met.");
                break;
        }

    }
    public override string SelectExecutedFlowPath(string criteria)
    {
        PowerKNN(criteria);
        List<PowerNearest> powerNearest = turbineDataModel.ListPower;
        // Worksheet selectedProjects = workbook.Worksheets["PowerNearest"];
        // Console.WriteLine("SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS:"+powerNearest.Count+","+powerNearest[0].SteamMass);
        int filteredProjectsCount = Convert.ToInt32(powerNearest[0].KNearest);//selectedProjects.Range["H2"].Value;
        //Console.WriteLine("filtersssssss Projectttttttttttttttttttttt"+filteredProjectsCount);
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
                        string projectDatFile = db[row].DatFilePath;//projectMaster.Cells[row, "BJ"].Value.ToString();
                        Logger("Selected Project id: " + projectID + " Dat Path: " + projectDatFile);
                        return projectDatFile;
                    }
                }
            }
        }

        return string.Empty;
    }
    public void PowerKNN(string criteria){
     CustomPowerKNN customPowerKNN = new CustomPowerKNN();
     customPowerKNN.ExecutePowerKNN(criteria);

    }   
    public void MoveYAndSetParamsCustom(){
        int updatedRow;
        updatedRow = AddOrMoveY();
        if(updatedRow!=0){
          UpdateHBDParamsCustom(updatedRow);
        }else{
            Console.WriteLine("Moving to Next");
        }
    }
    public override void MainExecuted(string criteria)
    {
        // base.MainExecuted(criteria);
    }
    public override void TerminateIgniteX(string functionName)
    {
        Console.WriteLine(functionName);
        // base.TerminateIgniteX(functionName);
    }
    public override void UpdateHBDParamsExecuted(int updatedRow)
    {
         CustomHMBDConfiguration customHMBDConfiguration = new CustomHMBDConfiguration();
        customHMBDConfiguration.UpdateHBDParamsCustom(updatedRow);
        // base.UpdateHBDParamsExecuted(updatedRow);
    }
    public void UpdateHBDParamsCustom(int updatedRow){
        CustomHMBDConfiguration customHMBDConfiguration = new CustomHMBDConfiguration();
        customHMBDConfiguration.UpdateHBDParamsCustom(updatedRow);
    }
}