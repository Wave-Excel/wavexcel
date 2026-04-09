using System;
using System.IO;
using System.Text.RegularExpressions;
namespace HMBD.Exec_SteamPath_Delete;
public class FileProcessor
{
    public void DeleteSteamPath()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent = ReadFile(filePath);
        string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string newContent = "";
        bool deleteMode = false;

        // Patterns to match
        string[] patterns = { @"^\s*!\s*ANZAHL.*", @"^\s*!\s*LP2.*" };

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            bool matchFound = false;

            foreach (string pattern in patterns)
            {
                if (Regex.IsMatch(line, pattern))
                {
                    deleteMode = !deleteMode;
                    matchFound = true;
                    break;
                }
            }

            if (!deleteMode)
            {
                newContent += line + Environment.NewLine;
            }
        }

        WriteFile(filePath, newContent);
    }

    private string ReadFile(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    private void WriteFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
    }
}

