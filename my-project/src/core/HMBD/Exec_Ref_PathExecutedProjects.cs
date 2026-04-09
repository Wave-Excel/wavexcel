namespace HMBD.Exec_Ref_PathExecutedProjects;
using System;
using System.IO;
// using Microsoft.Office.Interop.Excel;
using Models.ExecutedProjectDB;
public class FilePathUpdater
{
    // ExecutedProjectDB executedProjectDB;
    ExecutedDB executedDB;
    public FilePathUpdater(){
        executedDB = ExecutedDB.getInstance();
    //   executedProjectDB = ExecutedProjectDB.getInstance();
    }
    public void ExecutedUpdateFilePaths()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open("path_to_your_workbook.xlsx");
        // Worksheet ws = workbook.Sheets["ExecutedProjects"];
        // FileSystemObject fso = new FileSystemObject();
        List<ExecutedProject> db = executedDB.ExecutedProjectDB;
        try
        {
            // Find the last row with data in column A (project_id)
            // int lastRow = ws.Cells[ws.Rows.Count, 1].End(XlDirection.xlUp).Row;
            int lastRow = 50;
            // Defining Folder paths
            string folderPath1 = @"\\invadi7fla.ad101.siemens-energy.net\ai@stg\Database for ex_SwitchingIteratingExecuted.Executed Turbine\prodis\";

            // Loop through each project ID
            for (int i = 1; i <= db.Count(); i++) // Assign the starting row
            {
                string projectID = db[i].ProjectID;//executedProjectDB.ProjectID; //ws.Cells[i, 2].Value.ToString(); // Picks projectID from the 2nd Column

                // Check if folderPath1 exists
                if (Directory.Exists(folderPath1))
                {
                    DirectoryInfo folder = new DirectoryInfo(folderPath1);

                    foreach (DirectoryInfo subFolderProdis in folder.GetDirectories())
                    {
                        string prodisProjectName = subFolderProdis.Name.Substring(0, 6);

                        if (projectID.Substring(0, 5) == prodisProjectName.Substring(0, 5))
                        {
                            foreach (DirectoryInfo esubFolder in subFolderProdis.GetDirectories())
                            {
                                foreach (FileInfo file in esubFolder.GetFiles())
                                {
                                    if (file.Extension.ToLower() == ".dat")
                                    {
                                        string filePath = file.FullName;
                                        db[i].DatFilePath = filePath;
                                        // ws.Cells[i, 62].Value = filePath;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    db[i].DatFilePath = ""; // If the folder does not exist, leave the cell blank
                }
            }

            Console.WriteLine("File paths updated successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            // workbook.Close(false);
            // excelApp.Quit();
        }
    }
}


