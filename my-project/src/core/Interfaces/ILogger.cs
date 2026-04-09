namespace Interfaces.ILogger;

public interface ILogger{
  void LogInformation(string message);

  void LogInformation(string functionName,string message);

  void LogError(string functionName,string message);

   void clear();

   void moveLogs();
}

