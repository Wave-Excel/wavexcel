using System.Diagnostics;
// using Excel = Microsoft.Office.Interop.Excel;
// using DAT_Handler;
// using ERG_Handler;
using ERG_Handler;
using ERG_RsminHandler;
using Handlers.DAT_Handler;
using Ignite_x_wavexcel;
using Interfaces.ILogger;
using StartExecutionMain;
using StartKreislExecution;

namespace Turba.TurbaConfiguration
{
    class TurbaConfig
    {
        public static int TurbaIterationCount = 0;
        public static bool TurbaRunningFlag = false;
        ILogger logger;
        public TurbaConfig()
        {
            logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();


        }
        public void StartTurbaIteration()
        {
            PrepareDATFile();
            Logger("Preparing DAT File..");
            LaunchTurba();
            Logger("Executing TURBA..");
        }

     public void LaunchTurba(int mxLPs = 0)
        {
        Logger("Attempt to start TURBA execution..");
        string conFilePath = "C:\\testDir\\KREISL.CON";
        if(File.Exists(conFilePath) && StartKreisl.kreislKey)
        {
            File.Move(conFilePath, "C:\\testDir\\KREISLTURBAE1.DAT.CON", true);
        }
        RetryTurbaRun:
            if (!TurbaRunningFlag)
            {
                string targetDirectory = AppContext.BaseDirectory;
                // Console.WriteLine(targetDirectory);
                // co
                //string sourceDllPath = Path.Combine("src", "core", "Dll", "H2O64Bit.dll"); // Adjust this path based on where the DLL is located in your project
                // Console.WriteLine(sourceDllPath);
                string targetDllPath = Path.Combine(targetDirectory, "auto.bat");
                RunBatchFile(targetDllPath);
                TurbaRunningFlag = true;
                Logger("Waiting for the TURBA results...");
                Thread.Sleep(3000);
                CheckTurbaFinishedFlag(mxLPs);
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
            const int timeoutSeconds = 750;
            var startTime = DateTime.Now;
            var theFileName = @"C:\testDir\TURBA_FLAG.bin";

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                if (File.Exists(theFileName))
                {
                    TurbaIterationCount++;
                    Logger($"Turba[{TurbaIterationCount}] results: ERG File Found..");
                    TurbaRunningFlag = false;
                    LoadERGFile(mxLPs);
                    return true;
                }
                Thread.Sleep(1000);
            }

            Logger("Wait time expired...");
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
            RunBatchFile(targetDllPath);
            Logger("Turbine Files are available in folder - C:/testDir/turbine_files_YYYYMMDD-HHMMSS");
        }

        public void LaunchRsmin()
        {
            Logger("Attempt to start RSMIN 2.5.0  ....");
        RetryRsminRun:
            if (!TurbaRunningFlag)
            {
                string targetDirectory = AppContext.BaseDirectory;
                // Console.WriteLine(targetDirectory);
                // co
                //string sourceDllPath = Path.Combine("src", "core", "Dll", "H2O64Bit.dll"); // Adjust this path based on where the DLL is located in your project
                // Console.WriteLine(sourceDllPath);
                string targetDllPath = Path.Combine(targetDirectory, "autoTurba250.bat");
                RunBatchFile(targetDllPath);
                Logger("Waiting For the RSMIN results...");
                Thread.Sleep(1000);
                CheckRsminFinishedFlag();
            }
            else
            {
                Logger("Concurrent call!! Wait Turba is already running.....");
                Thread.Sleep(3000);
                Logger("Attempt to retry RSMIN..");
                goto RetryRsminRun;
            }
        }

        public bool CheckRsminFinishedFlag()
        {
            const int timeoutSeconds = 30;
            var startTime = DateTime.Now;
            var theFileName = @"C:\testDir\RSMIN_FLAG.bin";

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                if (File.Exists(theFileName))
                {
                    Logger("RSMIN 2.5.0 results: RSMIN.ERG File Found..");
                    LoadRsminERGFile();
                    return true;
                }
                Thread.Sleep(1000);
            }

            Logger("RSMIN results Wait time expired...");
            return false;
        }

        public void RunBatchFile(string filePath)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c \"{filePath}\"")
            {
                CreateNoWindow = true,//false,
                UseShellExecute = false
            };
            Process process = Process.Start(processInfo);
            //Logger("Process Idddddddddddddddddddddddddddddddd" + process.Id);

            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                return;
            }
            process.WaitForExit();

            string[] fileLine = File.ReadAllLines("C:\\testDir\\TURBATURBAE1.DAT.LOG");
            for (int i = 0; i < fileLine.Length; i++)
            {
                if (fileLine[i].Contains("Error in input file"))
                {
                    Logger("TURBA ERROR : Please Check TURBATURBAE1.DAT.LOG File in TestDir");
                    TurbineDesignPage.cts.Cancel();
                    if (TurbineDesignPage.cts.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

        }

        public void Logger(string message)
        {
            logger.LogInformation(message);
            // Console.WriteLine(message);
        }

        public void PrepareDATFile()
        {
            DATFileProcessor dATFileProcessor = new DATFileProcessor();
            dATFileProcessor.PrepareDATFile();
            //From DATFileProcessor
            // Implement the logic to prepare the DAT file
        }

        public void LoadERGFile(int mxLps = 0)
        {
            ERGFileReader eRGFileReader = new ERGFileReader();
            eRGFileReader.LoadERGFile(mxLps);
            //From ERG_Handler.cs
            // Implement the logic to load the ERG file
        }

        public void LoadRsminERGFile()
        {
            ThrustCalculator thrustCalculator = new ThrustCalculator();
            thrustCalculator.LoadRsminERGFile();
            //From ERG_RsminHandler.cs
            // Implement the logic to load the RSMIN ERG file
        }
    }
}