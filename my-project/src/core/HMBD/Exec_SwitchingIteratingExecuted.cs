using System;
using System.Collections.Generic;
// using Microsoft.Office.Interop.Excel;
using Models.TurbineData;
using StartExecutionMain;
public class WorkbookHandler
{
    private Dictionary<string, int> callCounters;
    private const int MAX_THROTTLE_CALLS = 2;  // Fixed limit for Throttle calls
    // private Worksheet customerInputsSheet;
    public WorkbookHandler()
    {
        // this.customerInputsSheet = customerInputsSheet;
        InitializeCallCounters();
    }

    private void InitializeCallCounters()
    {
        callCounters = new Dictionary<string, int>();
    }
    public void ExecutedCounter(string arg)
    {
        int maxCalls = TurbineDataModel.getInstance().ExecutedCountNumber;
        //(int)customerInputsSheet.Range["H2"].Value;
        // Check if the counter exists for the argument
        if (!callCounters.ContainsKey(arg))
        {
            callCounters[arg] = 0;
        }
        // Check the counter value
        if (arg == "Throttle")
        {
            // Handle Throttle separately with its fixed limit
            if (callCounters[arg] < MAX_THROTTLE_CALLS)
            {
                callCounters[arg]++;
                Console.WriteLine("Subroutine executed with argument: " + arg);
            }
            else
            {
                Console.WriteLine("going to custom path");
            }
        }
        else
        {
            // Handle BCD1120 and BCD1190 with maxCalls from the sheet
            if (callCounters[arg] < maxCalls)
            {
                callCounters[arg]++;
                Console.WriteLine("Subroutine executed with argument: " + arg);
            }
            else
            {
                if (arg == "BCD1120")
                {
                    MainExecuted("BCD1190");
                }
                else if (arg == "BCD1190")
                {
                    Console.WriteLine("going to custom path");
                }
            }
        }
    }
    private void MainExecuted(string arg)
    {
        MainExecutedClass mainExec = new MainExecutedClass();
        mainExec.MainExecuted(arg);
        // Implement the logic for MainExecuted here
        // Console.WriteLine("MainExecuted called with argument: " + arg);
    }
}
