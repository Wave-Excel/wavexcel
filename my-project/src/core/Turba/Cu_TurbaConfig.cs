using System;
using System.Diagnostics;
using System.IO;
using Interfaces.ILogger;
using StartExecutionMain;
using System.Threading;
using Handlers.Exec_ERG_RsminHandler;
using Handlers.Exec_ERG_Handler;
using Handlers.Exec_DAT_Handler;
using Handlers.Custom_DAT_Handler;
using Handlers.CustomERGHandler;
using Handlers.CU_ERG_RsminHandler;
namespace Turba.Cu_TurbaConfig;
public class CuTurbaAutomation
{
    public static int TurbaIterationCount = 0;
    public static bool TurbaRunningFlag = false;

    private ILogger logger;

    public CuTurbaAutomation(){
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    public void AwaitSyncInvokeApp(string command)
    {
        Process process = new Process();
        //process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = command;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        process.Start();
        process.WaitForExit();
        //Thread.Sleep(1000);
    }

    public void StartTurbaIteration()
    {
        PrepareDATFile();
        Logger("Preparing DAT File..");
        LaunchTurba();
        Logger("Executing TURBA..");
    }

    public void LaunchTurba(int mxLp = 0)
    {
        Logger("Attempt to start TURBA execution..");
        string conFilePath = "C:\\testDir\\KREISL.CON";
        if (File.Exists(conFilePath) && CustomExecutedClass.kreislKey)
        {
            File.Move(conFilePath, "C:\\testDir\\KREISLTURBAE1.DAT.CON", true);
        }
    RetryTurbaRun:
        if (!TurbaRunningFlag)
        {
            TurbaRunningFlag = true;
            Logger("Waiting for the TURBA results...");
            //if()
            string targetDirectory = AppContext.BaseDirectory;
            
            string targetDllPath = Path.Combine(targetDirectory, "autoTurba250.bat");
            AwaitSyncInvokeApp(targetDllPath);
            CheckTurbaFinishedFlag(mxLp);
        }
        else
        {
            Logger("Concurrent call!! Wait Turba is already running.....");
            Thread.Sleep(3000);
            Logger("Attempt to retry Turba ..");
            goto RetryTurbaRun;
        }
    }

    public bool CheckTurbaFinishedFlag(int maxLp = 0)
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
                LoadERGFile(maxLp);
                return true;
            }
            Thread.Sleep(1000);
        }

        if (!checkTurbaFinishedFlag)
        {
            Debug.WriteLine("Wait time expired...");
            LaunchTurba();
        }

        return false;
    }

    public void PrepareTurbineFiles()
    {
        Logger("Preparing Turbine Files..");
        Process.Start(@"C:\testDir\auto_prepfiles.bat");
        Logger("Turbine Files are available in folder - C:/testDir/turbine_files_YYYYMMDD-HHMMSS");
    }

    public void LaunchRsmin(int maxlp = 0)
    {
        CheckRsminFinishedFlag(maxlp);
    }

    public bool CheckRsminFinishedFlag(int maxlp = 0)
    {
        const int timeoutSeconds = 0;
        bool checkRsminFinishedFlag = false;
        string theFileName = @"C:\testDir\RSMIN_FLAG.bin";
        DateTime startTime = DateTime.Now;

        //while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
        //{
            if (File.Exists(theFileName))
            {
                Logger("RSMIN 2.5.0 results: RSMIN.ERG File Found..");
                checkRsminFinishedFlag = true;
                LoadRsminERGFile(maxlp);
                return true;
            }
            Thread.Sleep(1000);
        //}

        if (!checkRsminFinishedFlag)
        {
            Debug.WriteLine("RSMIN results Wait time expired...");
        }

        return false;
    }

    private void PrepareDATFile()
    {
        CustomDATFileProcessor customDATFileProcessor = new CustomDATFileProcessor();
        customDATFileProcessor.PrepareDatFile();
        //Executed DAT Handler
        // ExecutedDATFileProcessor datFileHandler = new ExecutedDATFileProcessor();
        // datFileHandler.PrepareDatFile();
    }

    private void LoadERGFile(int maxlp =0)
    {
        CustomERGFileReader customERGFileReader     = new CustomERGFileReader();
        customERGFileReader.LoadERGFile(maxlp);
        // EXECERGFileReader eXECERGFileReader = new EXECERGFileReader();
        // eXECERGFileReader.LoadERGFile();
        // Implement the logic to load the ERG file
    }
    
    public void Z_obsolete_LaunchRsmin(){
        Logger("Attempt to start RSMIN 2.5.0 ....");
    RetryRsminRun:
        if (!TurbaRunningFlag)
        {
            AwaitSyncInvokeApp(@"C:\testDir\autoTurba250.bat");
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

    private void LoadRsminERGFile(int maxlp = 0)
    {
        CustomThrustCalculator customThrustCalculator= new CustomThrustCalculator();
        customThrustCalculator.LoadRsminERGFile(maxlp);
        // ExecERGFileHandler execERGFileHandler = new ExecERGFileHandler();
        // execERGFileHandler.GetThrust();
    }

    private void Logger(string message)
    {
        logger.LogInformation(message);
    }
}

