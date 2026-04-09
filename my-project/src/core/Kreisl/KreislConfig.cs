using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Kreisl.KreislConfig
{
    public class KreislIntegration
    {
        public static int KreisLIterationCount = 1;

        public KreislIntegration() { }
        public void KreisLTurbaIteration()
        {
            LaunchKreisL();
            Console.WriteLine("Executing KreisL..");
        }

        public void RenameTurbaCON(string oldFilePath = @"C:\testDir\TURBA.CON", string newFilePath = @"C:\testDir\TURBAz1.CON")
        {
            try
            {
                if (File.Exists(oldFilePath))
                {
                    File.Move(oldFilePath, newFilePath, true);
                    File.Delete(oldFilePath);
                    Console.WriteLine("File renamed successfully.");
                }
                else if (File.Exists(newFilePath))
                {
                    File.Move(newFilePath, oldFilePath, true);
                    File.Delete(newFilePath);
                    Console.WriteLine("File renamed successfully.");
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"An I/O error occurred: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public void LaunchKreisL()
        {
            //Console.WriteLine("Attempt to start KreisL execution ["+ KreisLIterationCount + "]");
            //string WorkingDirectoryPath = @"C:\testDir";
            //string filePath = @"start C:\Turman\Turman\2.5.0\KreisL.exe";
            //ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c \"{filePath}\"")
            //{
            //    CreateNoWindow = true,//false,
            //    UseShellExecute = false,
            //    WorkingDirectory = WorkingDirectoryPath,
            //    WindowStyle = ProcessWindowStyle.Minimized
            //};
            //Process process = Process.Start(processInfo);
            //process.WaitForExit();
            //Thread.Sleep(2500);
            //KreisLIterationCount++;
            string WorkingDirectoryPath = @"C:\testDir";
            string filePath = @"C:\Turman\Turman\2.5.0\KreisL.exe"; // Removed "start" from the path
            ProcessStartInfo processInfo = new ProcessStartInfo(filePath)
            {
                CreateNoWindow = true, // Ensures no window is created
                UseShellExecute = false, // Required for redirecting output
                RedirectStandardOutput = true, // Redirects standard output
                RedirectStandardError = true, // Redirects standard error
                WorkingDirectory = WorkingDirectoryPath,
                WindowStyle = ProcessWindowStyle.Hidden // Ensures the process runs silently
            };

            using (Process process = Process.Start(processInfo))
            {
                // Optionally read the output or error streams if needed
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();
            }

            // Optional delay
            Thread.Sleep(2500);

        }
    }
}