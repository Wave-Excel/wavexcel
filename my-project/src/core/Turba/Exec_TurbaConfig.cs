using System;
using System.Diagnostics;
using System.IO;
using Interfaces.ILogger;
using StartExecutionMain;
using System.Threading;
using Handlers.Exec_ERG_RsminHandler;
using Handlers.Exec_ERG_Handler;
using Handlers.Exec_DAT_Handler;
using Ignite_x_wavexcel;
namespace Turba.Exec_TurbaConfig;
public class TurbaAutomation
{
    public static int TurbaIterationCount = 0;
    public static bool TurbaRunningFlag = false;

    private ILogger logger;

    public TurbaAutomation(){
        logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    public void AwaitSyncInvokeApp(string command)
    {
        Process process = new Process();

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = command;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        process.Start();
        if (TurbineDesignPage.cts.IsCancellationRequested)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            return;
        }
        process.WaitForExit();
        //string[] fileLine = File.ReadAllLines("C:\\testDir\\TURBATURBAE1.DAT.LOG");
        //for (int i = 0; i < fileLine.Length; i++)
        //{
        //    if (fileLine[i].Contains("Error in input file"))
        //    {
        //        Logger("Check TURBATURBAE1.DAT.LOG File in TestDir");
        //        TurbineDesignPage.cts.Cancel();
        //        if (TurbineDesignPage.cts.IsCancellationRequested)
        //        {
        //            return;
        //        }
        //    }
        //}
    }

    public void StartTurbaIteration()
    {
        PrepareDATFile();
        Logger("Preparing DAT File..");
        LaunchTurba();
        Logger("Executing TURBA..");
    }

    public void LaunchTurba(int maxLp=0)
    {
        Logger("Attempt to start TURBA execution..");
        string conFilePath = "C:\\testDir\\KREISL.CON";
        if (File.Exists(conFilePath))
        {
            File.Move(conFilePath, "C:\\testDir\\KREISLTURBAE1.DAT.CON", true);
        }
    RetryTurbaRun:
        if (!TurbaRunningFlag)
        {
            TurbaRunningFlag = true;
            string targetDirectory = AppContext.BaseDirectory;
            // Console.WriteLine(targetDirectory);
            // co
            //string sourceDllPath = Path.Combine("src", "core", "Dll", "H2O64Bit.dll"); // Adjust this path based on where the DLL is located in your project
            // Console.WriteLine(sourceDllPath);
            string targetDllPath = Path.Combine(targetDirectory, "autoTurba250.bat");
            Logger("Waiting for the TURBA results...");
            AwaitSyncInvokeApp(targetDllPath);
            CheckTurbaFinishedFlag(maxLp);
        }
        else
        {
            Logger("Concurrent call!! Wait Turba is already running.....");
            Thread.Sleep(3000);
            Logger("Attempt to retry Turba ..");
            goto RetryTurbaRun;
        }
    }

    public bool CheckTurbaFinishedFlag(int mxLPs = 0)
    {
        const int timeoutSeconds = 25;
        bool checkTurbaFinishedFlag = false;
        string theFileName = @"C:\testDir\TURBA_FLAG.bin";
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
        {
            if (File.Exists(theFileName))
            {
                TurbaIterationCount++;
                Logger($"Turba[{TurbaIterationCount}] results: ERG File Found..");
                checkTurbaFinishedFlag = true;
                TurbaRunningFlag = false;
                LoadERGFile(mxLPs);
                return true;
            }
            Thread.Sleep(1000);
        }

        if (!checkTurbaFinishedFlag)
        {
            Debug.WriteLine("Wait time expired...");
        }

        return false;
    }

    public void PrepareTurbineFiles()
    {
        Logger("Preparing Turbine Files..");
        string targetDirectory = AppContext.BaseDirectory;
        // Console.WriteLine(targetDirectory);
        // co
        //string sourceDllPath = Path.Combine("src", "core", "Dll", "H2O64Bit.dll"); // Adjust this path based on where the DLL is located in your project
        // Console.WriteLine(sourceDllPath);
        string targetDllPath = Path.Combine(targetDirectory, "praneeth.bat");
        //string batchFilePath = @"C:\\testDir\\praneeth.bat";// @"C:\testDir\auto_prepfiles.bat";
        //Process.Start(@"C:\testDir\praneeth.bat");
        // Process.WaitForExit();
        Process process = new Process();
            process.StartInfo.FileName = targetDllPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                return;
            }

        // Wait for the process to complete
        process.WaitForExit();
        Logger("Turbine Files are available in folder - C:/testDir/turbine_files_YYYYMMDD-HHMMSS");
    }

    public void LaunchRsmin()
    {
        CheckRsminFinishedFlag();
    }

    public bool CheckRsminFinishedFlag()
    {
        const int timeoutSeconds = 0;
        bool checkRsminFinishedFlag = false;
        string theFileName = @"C:\testDir\RSMIN_FLAG.bin";
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
        {
            if (File.Exists(theFileName))
            {
                Logger("RSMIN 2.5.0 results: RSMIN.ERG File Found..");
                checkRsminFinishedFlag = true;
                LoadRsminERGFile();
                return true;
            }
            Thread.Sleep(1000);
        }

        if (!checkRsminFinishedFlag)
        {
            Debug.WriteLine("RSMIN results Wait time expired...");
        }

        return false;
    }

    private void PrepareDATFile()
    {
        //Executed DAT Handler
        ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        datFileHandler.PrepareDatFile();
    }

    private void LoadERGFile(int mxLPs = 0)
    {
        EXECERGFileReader eXECERGFileReader = new EXECERGFileReader();
        eXECERGFileReader.LoadERGFile(mxLPs);
        // Implement the logic to load the ERG file
    }
    
    public void Z_obsolete_LaunchRsmin()
    {
        Logger("Attempt to start RSMIN 2.5.0 ....");
        RetryRsminRun:
        if (!TurbaRunningFlag)
        {
            string targetDirectory = AppContext.BaseDirectory;
            string targetDllPath = Path.Combine(targetDirectory, "autoTurba250.bat");
            AwaitSyncInvokeApp(targetDllPath);
            Logger("Waiting for the RSMIN results...");
            Thread.Sleep(1000); // Wait for 1 second
            CheckRsminFinishedFlag();
        }
        else
        {
            Logger("Concurrent call!! Wait Turba is already running.....");
            Thread.Sleep(3000); // Wait for 3 seconds
            Logger("Attempt to retry RSMIN..");
            goto RetryRsminRun;
        }

    }
    private void LoadRsminERGFile()
    {
        ExecERGFileHandler execERGFileHandler = new ExecERGFileHandler();
        execERGFileHandler.GetThrust();
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
    }
}

