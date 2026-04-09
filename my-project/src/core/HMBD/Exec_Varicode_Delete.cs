using System;
using System.IO;
using System.Text.RegularExpressions;
namespace HMBD.Exec_Varicode_Delete;
public class VaricodeDeleter
{
    public void DeleteVaricode()
    {
        string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
        string fileContent;
        string[] lines;
        string newContent = "";
        Regex regex;
        string[] patterns = { @"^\s*5\s*30.*", @"^\s*5\s*54.*", @"^\s*5\s*24.*" };

        // Read the file content
        fileContent = ReadFile(filePath);

        // Split the file content into lines
        lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Loop through the lines and build new content
        foreach (string line in lines)
        {
            bool matchFound = false;

            foreach (string pattern in patterns)
            {
                regex = new Regex(pattern, RegexOptions.None);
                if (regex.IsMatch(line))
                {
                    matchFound = true;
                    break;
                }
            }

            // If no match is found, keep the line
            if (!matchFound)
            {
                newContent += line + Environment.NewLine;
            }
        }

        // Write the new content to the file
        WriteFile(filePath, newContent);
    }

    private string ReadFile(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading the file: " + ex.Message);
            return string.Empty;
        }
    }

    private void WriteFile(string filePath, string content)
    {
        try
        {
            File.WriteAllText(filePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing to the file: " + ex.Message);
        }
    }
}
