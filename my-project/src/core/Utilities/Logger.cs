using Ignite_x_wavexcel;
using Interfaces.ILogger;

namespace Utilities.Logger{
   
    public class Logger : ILogger{
        string logFilePath = "igniteX.log";
        public void clear()
        {
            File.WriteAllText(logFilePath, string.Empty);  
        }
        public void LogInformation(string message){
            string currDate = DateTime.Now.ToString("yyyy-MM-dd"); // Format date as yyyy-MM-dd
            string currTime = DateTime.Now.ToString("HH:mm:ss");
            File.AppendAllText(logFilePath, currDate+" "+ currTime + ": "+ message + Environment.NewLine);
            TurbineDesignPage.page.AddLogMessage(message);
            Console.WriteLine($"[INFO] {message}");
        }
    public void LogInformation(string functionName,string message){
            string currDate = DateTime.Now.ToString("yyyy-MM-dd"); // Format date as yyyy-MM-dd
            string currTime = DateTime.Now.ToString("HH:mm:ss");
            File.AppendAllText(logFilePath, currDate + " " + currTime + ": " + message + Environment.NewLine);
            TurbineDesignPage.page.AddLogMessage(message);
            Console.WriteLine($"[FunctionName] {functionName} - [INFO] {message}");
    }
    public void LogError(string functionName,string message){
      Console.WriteLine($"[FunctionName] {functionName} - [ERROR] {message}");
    }

    public void moveLogs()
    {
        string sourceFilePath = Path.Combine(AppContext.BaseDirectory, "igniteX.log");
        string destinationFilePath = Path.Combine("C:\\testDir", "igniteX.log");
        if (File.Exists(destinationFilePath))
        {
            File.Delete(destinationFilePath);
        }
        File.Copy(sourceFilePath, destinationFilePath,true);
    }
  }

}

