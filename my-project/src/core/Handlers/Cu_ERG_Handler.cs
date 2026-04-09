using System.Diagnostics;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Handlers.Exec_ERG_Handler;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Models.TurbaOutputDataModel;
using StartExecutionMain;
using Turba.Exec_TurbaConfig;
namespace Handlers.CustomERGHandler;

public class CustomERGFileReader:EXECERGFileReader{
    TurbaOutputModel turbaOutputModel;
    int lpCount;
    IConfiguration configuration;
    string filePath = "C:\\testDir\\TURBATURBAE1.DAT.ERG";
    ILogger logger;
    public CustomERGFileReader(){
        turbaOutputModel = TurbaOutputModel.getInstance();
        configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
        lpCount = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
    }
    public void LoadERGFile(int mxLps = 0)
    {
        if (mxLps != 0)
        {
            lpCount = mxLps - 1;
        }
        logger.LogInformation("Reading ERG File..");
        if (mxLps == 0)
        {
            turbaOutputModel.OutputDataList.Clear();
            turbaOutputModel.fillTurbaOutputDataList();
        }
        else
        {
            turbaOutputModel.OutputDataList.Clear();
            turbaOutputModel.fillTurbaOutputDataList();
            for (int i = 1; i <= mxLps; ++i)
            {
                turbaOutputModel.OutputDataList[i] = new OutputLoadPoint();
            }
        }
        ScanTurbaERG();
        
    }
    public override void ScanTurbaERG()
    {
        logger.LogInformation("Scanning TURBA results..");

        GetLPfromERG();

        GetParam_ABWEICHUNG();
        
        GetParam_DELTA_T();
        
        GetParam_RADRAUM();
        
        GetParam_FMIN_1();
        
        GetParam_FMIN_2();
        
        GetParam_AUSTRITT();
        
        
        GetCheck_GEF();
        
        GetCheck_KLEMMENLEISTUNG();

        GetCheck_PressureBar();


        GetCheck_EffINNERER_WIRKG_GES();
        
        GetParam_BEAUFSCHL();
        
        GetParam_DUESENGRUPPE_AUSGEST();
        
        GetParam_HOEHE();
        
        GetParam_TEILUNG();
        
        GetParam_WINKEL_ALFAA();
        
        GetParam_FMIN_IST();
        
        GetParam_FMIN_SOLL();

        GetCheck_Exhaust_Temperature();
        GetCheck_ABSTAND();
        GetCheck_PSI();
        GetCheckLang();
        GetParamLaufzahl();
        GetParam_BIEGESPANNUNG();
        FillCheckBIEGESPANNUNG();
        GetParam_HSTAT();
        GetParam_HGES();
        FillCheckHSTAT_HGES();
        GetParam_AK_LECKD_1();
        GetParam_AK_LECKD_2();
        FillCheckAkLECKDVERDICT();
        GetParam_DT();
        GetParam_SAXA();
        GetGEF_RDEHN();

        turbaOutputModel.OutputDataList[3].FMIN1 = 0;
        turbaOutputModel.OutputDataList[3].DUESEN  =0;

    }
    public void FillCheckAkLECKDVERDICT(){
        int count = 0;
        // D3 Should be checked at last
        for(int i = 2 ; i<lpCount;i++){
            double AM5 = turbaOutputModel.OutputDataList[i].AKLECKD1;
            double AN5 = turbaOutputModel.OutputDataList[i].AKLECKD2;
            if(AM5==AN5*-1){
                turbaOutputModel.OutputDataList[i].AK_LECKDVERDICT="TRUE";
            }else{
                count = 1;
                turbaOutputModel.OutputDataList[i].AK_LECKDVERDICT = "FALSE";
            }
        } 
        if(count==1){
            turbaOutputModel.OutputDataList[1].AK_LECKDVERDICT = "FALSE";
            turbaOutputModel.Check_AK_LECKDVERDICT = "FALSE";
        }else{
            turbaOutputModel.OutputDataList[1].AK_LECKDVERDICT = "TRUE";
            turbaOutputModel.Check_AK_LECKDVERDICT = "TRUE";
        }
    }
    public void FillCheckHSTAT_HGES(){
        int count = 0;
        for(int i=1;i<=lpCount;i++){
            double val1 = turbaOutputModel.OutputDataList[i].HSTAT;
            double val2 = turbaOutputModel.OutputDataList[i].HGES;
            if(Math.Abs(val1-val2)<50){
                turbaOutputModel.OutputDataList[i].HSTAT_MINUS_HGES = "TRUE";
            }else{
                count = 1;
                turbaOutputModel.OutputDataList[i].HSTAT_MINUS_HGES = "FALSE";
            }
        }
        if(count ==1){
            turbaOutputModel.Check_HSTAT_MINUS_HGES = "FALSE";
        }else{
            turbaOutputModel.Check_HSTAT_MINUS_HGES = "TRUE";
        }

    }
    public void FillCheckBIEGESPANNUNG(){
        int count =0;
        for(int i=1;i<=lpCount;i++){
            double val = turbaOutputModel.OutputDataList[i].BIEGESPANNUNG;
            if(val<33){
                turbaOutputModel.OutputDataList[i].BIEGESPANNUNG_TRUE_FALSE = "TRUE";
            }else{
                count =1;
                turbaOutputModel.OutputDataList[i].BIEGESPANNUNG_TRUE_FALSE = "FALSE";
            }
        }
        if(count==1){
            turbaOutputModel.Check_BIEGESPANNUNG_TRUE_FALSE = "FALSE";
        }else{
            turbaOutputModel.Check_BIEGESPANNUNG_TRUE_FALSE = "TRUE";
        }
        
    }
    public void GetCheck_ABSTAND()
        {
            // Worksheet ErgData = workbook.Sheets["ERG_DATA"];
            // Worksheet ErgResult = workbook.Sheets["Output"];
            string Parameter = "ABSTAND DEP";
            int ParameterPos = 80;
            int ParameterLen = Parameter.Length;
            string [] fileLines = File.ReadAllLines(filePath);

            // int ERG_EoF = ErgData.Cells[ErgData.Rows.Count, "B"].End(XlDirection.xlUp).Row;
            // int LpCount = ErgResult.Cells[ErgResult.Rows.Count, "B"].End(XlDirection.xlUp).Row - 3;

            for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
            {
                if (fileLines[lineNumber].Contains(Parameter))
                {
                    lineNumber += 3;
                    string line = fileLines[lineNumber];//ErgData.Cells[lineNumber, "B"].Value.ToString();

                    for (int lineNumberNested = lineNumber; lineNumberNested < fileLines.Length; lineNumberNested++)
                    {
                        lineNumber = lineNumberNested;
                        line = fileLines[lineNumber]; //ErgData.Cells[lineNumber, "B"].Value.ToString();

                        if (line.Length > 10)
                        {
                            string LangLater = line.Substring(68, 22);

                            if (double.Parse(LangLater) < 320)
                            {
                                turbaOutputModel.OutputDataList[0].GBC_Length = Convert.ToDouble(LangLater);
                                // ErgResult.Cells[2, "AC"].Value = LangLater;
                            }
                        }

                        if (line.Contains("!"))
                        {
                            return;
                        }
                    }
                }
            }
        }

    public void GetCheck_PSI()
    {
        // STUFE  CAX
        // Worksheet ErgData, ErgResult;
        string Parameter = "STUFE  CAX";
        int ParameterPos = 43;
        int ParameterLen = Parameter.Length;
        double[] PSIValues = new double[30];
        int loadPointCounter = 1;
        string [] fileLines = File.ReadAllLines(filePath);
        turbaOutputModel.Check_PSI = "TRUE";
        turbaOutputModel.OutputDataList[0].PSI = "TRUE";
        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line, subLine;
            if (fileLines[lineNumber].Contains(Parameter) && loadPointCounter <= lpCount - 2)
            {
                // loadPointCounter++;
                line = fileLines[lineNumber];//ErgData.Range["B" + lineNumber].Text;
                double initPressure = 0;
                int Stage = 0;
                Console.WriteLine("PSI Values..");

                for (int lineNumberNested = lineNumber; lineNumberNested < fileLines.Length; lineNumberNested++)
                {
                    // Increment the Line pointer
                    lineNumber = lineNumberNested;
                    line = fileLines[lineNumber]; //ErgData.Range["B" + lineNumber].Text;

                    // We have a non-Null and non-empty String value
                    if (line.Length > 10 && line.Substring(line.Length - 5).Trim().Length > 4)
                    {
                        string PSI_val = line.Substring(0, 35).Substring(29, 6).Trim();
                        Console.WriteLine(Convert.ToDouble(PSI_val));

                        if ((Convert.ToDouble(PSI_val) != 0 && Convert.ToDouble(PSI_val) < 1.8) || initPressure > 6)
                        {
                            turbaOutputModel.OutputDataList[0].PSI = "LP" + loadPointCounter; 
                            // ErgResult.Range["AE2"].Value = "LP" + (loadPointCounter - 1);
                            // ErgResult.Range["AE3"].Value = false;
                            turbaOutputModel.Check_PSI = "FALSE";

                            turbaOutputModel.OutputDataList[0].PSI = "FALSE";
                            // string loadPointCell = "AE" + (loadPointCounter + 2);
                            // ErgResult.Range[loadPointCell].Value = false;
                            turbaOutputModel.OutputDataList[loadPointCounter].PSI = "FALSE";
                            //Debug.WriteLine()
                            Debug.WriteLine("PSI failed @line: " + lineNumberNested + ", " + Convert.ToDouble(PSI_val));
                            goto LineNext;
                        }
                    }

                    // cu_ERG_NozzleOptimizer. In case end of stage is detected then exit
                    if (line.Contains("#END"))
                    {
                        goto ReturnSub;
                    }
                }
                loadPointCounter++;
            LineNext:
                continue;
            }
        }
    ReturnSub:
        return;
    }
    public void GetCheckLang()
    {
        // STUFE  CAX

        // Worksheet ErgData, ErgResult;

        string Parameter;
        int ParameterPos, ParameterLen;

        // Set the Parameter
        Parameter = "STUFE PR    DI  SEHNE";
        ParameterPos = 28;
        ParameterLen = Parameter.Length;

        // Configure the WorkSheets
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open("YourWorkbookPathHere");
        // ErgData = workbook.Worksheets["ERG_DATA"];
        // ErgResult = workbook.Worksheets["Output"];
        string [] fileLines = File.ReadAllLines(filePath);

        // int ERG_EoF = ErgData.Cells[ErgData.Rows.Count, "B"].End(XlDirection.xlUp).Row;
// 
        // int LpCount = ErgResult.Cells[ErgResult.Rows.Count, "B"].End(XlDirection.xlUp).Row - 3;

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line, subLine;

            if (fileLines[lineNumber].Contains(Parameter))
            {
                lineNumber += 4;
                line = fileLines[lineNumber];//ErgData.Cells[lineNumber, "B"].Value.ToString();

                double initLang = 0.0;
                turbaOutputModel.Check_Lang = "TRUE";

                turbaOutputModel.OutputDataList[0].Lang = "TRUE";
                // ErgResult.Cells[3, "AF"].Value = true;

                for (int lineNumberNested = lineNumber - 1; lineNumberNested < fileLines.Length; lineNumberNested++)
                {
                    lineNumber = lineNumberNested;
                    line =  fileLines[lineNumber];//ErgData.Cells[lineNumber, "B"].Value.ToString();

                    if (line.Length > 10)
                    {
                        string LangLaterStr = line.Substring(34, 5).Trim();
                        double LangLater = double.Parse(LangLaterStr);

                        if (LangLater < initLang)
                        {
                            turbaOutputModel.Check_Lang = "FALSE";
                            turbaOutputModel.OutputDataList[0].Lang = "FALSE";
                            // ErgResult.Cells[3, "AF"].Value = false;
                        }

                        initLang = LangLater;
                    }

                    if (line.Contains("!"))
                    {
                        goto ReturnSub;
                    }
                }
            }
        }

    ReturnSub:
        return;
    }
    public void GetParamLaufzahl()
    {
        // Define the parameter
        string Parameter = "LAUFZAHL";
        int ParameterPos = 20;
        int ParameterLen = Parameter.Length;
        string [] fileLines = File.ReadAllLines(filePath);

        // Configure the WorkSheets
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.Workbooks.Open("YourWorkbookPathHere");
        // Worksheet ErgData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ErgResult = workbook.Worksheets["Output"];

        // int ERG_EoF = ErgData.Cells[ErgData.Rows.Count, "B"].End(XlDirection.xlUp).Row;

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line, subLine, paramVal;

            if (fileLines[lineNumber].Contains(Parameter))
            {
                line =  fileLines[lineNumber];//ErgData.Cells[lineNumber, "B"].Value.ToString();

                // Get the parameter value with relative position
                subLine = line.Substring(line.Length - 48);
                paramVal = subLine.Substring(0, 8).Trim();

                // ErgResult.Cells[2, "AG"].Value = paramVal;
                turbaOutputModel.OutputDataList[0].LAUFZAHL = Convert.ToDouble(paramVal);

                // Only considering first occurrence. Skip to next LP
                goto ReturnSub;
            }
        }

    ReturnSub:
        return;
    }
    public void GetParam_BIEGESPANNUNG()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "BIEGESPANNUNG 1";
        int parameterPos = 12;
        int parameterLen = parameter.Length;
        string [] fileLines = File.ReadAllLines(filePath);
        double mx = double.MinValue;

        // int lpCount = ergResult.Cells[ergResult.Rows.Count, 2].End(XlDirection.xlUp).Row - 3;
        //13-3 = 10
        for (int i = 1; i <= lpCount; i++)
        {
            int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);//(int)(ergResult.Cells[i + 3, 3] as Range).Value;
            int rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);//(int)(ergResult.Cells[i + 4, 3] as Range).Value;

            if (i == lpCount)
            {
                rangeStop = fileLines.Length-1;
            }

            for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
            {
                string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
                if (line.Contains(parameter))
                {
                    string subLine = line.Substring(parameterPos - 1);
                    string paramValTemp = subLine.Substring(0, 21).Trim();
                    string paramVal = paramValTemp.Substring(paramValTemp.Length - 4).Trim();
                    // ergResult.Cells[i + 3, 34].Value = paramVal;
                    turbaOutputModel.OutputDataList[i].BIEGESPANNUNG = Convert.ToDouble(paramVal);
                    mx = Math.Max(mx,Convert.ToDouble(paramVal));

                    // Only considering first occurrence. Skip to next LP
                    break;
                }
            }
        }
        turbaOutputModel.OutputDataList[0].BIEGESPANNUNG = mx;
    }
    public void GetParam_HSTAT()
    {

        string [] fileLines = File.ReadAllLines(filePath);

        string parameter = "UE-TEIL1";
        int parameterPos = 12;
        int parameterLen = parameter.Length;
        int valueIndex = 0;
        double[] hstatValues = new double[0];


        for (int i = 1; i <= lpCount; i++)
        {
            int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);//(int)(ergResult.Cells[i + 3, 3] as Range).Value;
            int rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);//(int)(ergResult.Cells[i + 4, 3] as Range).Value;

            if (i == lpCount)
            {
                rangeStop = fileLines.Length-1;//ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;
            }

            for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
            {
                string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
                if (line.Contains(parameter))
                {
                    string subLine = line.Substring(parameterPos - 1);
                    string paramValTemp = subLine.Substring(0, 33).Trim();
                    string paramVal = paramValTemp.Substring(paramValTemp.Length - 6).Trim();

                    Array.Resize(ref hstatValues, valueIndex + 1);
                    hstatValues[valueIndex] = double.Parse(paramVal);
                    valueIndex++;
                    turbaOutputModel.OutputDataList[i].HSTAT = Convert.ToDouble(paramVal);
                    // ergResult.Cells[i + 3, 36].Value = paramVal;

                    // Only considering first occurrence. Skip to next LP
                    break;
                }
            }
        }
    }

    public void GetParam_HGES()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "UE-TEIL1";
        string [] fileLines = File.ReadAllLines(filePath); 
        int parameterPos = 12;
        int parameterLen = parameter.Length;
        int valueIndex = 0;
        double[] hstatValues = new double[0];

        // int lpCount = ergResult.Cells[ergResult.Rows.Count, 2].End(XlDirection.xlUp).Row - 3;

        for (int i = 1; i <= lpCount; i++)
        {
            int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);//(int)(ergResult.Cells[i + 3, 3] as Range).Value;
            int rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);//(int)(ergResult.Cells[i + 4, 3] as Range).Value;

            if (i == lpCount)
            {
                rangeStop = fileLines.Length-1;//ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;
            }

            for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
            {
                string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
                if (line.Contains(parameter))
                {
                    string subLine = line.Substring(parameterPos - 1);
                    string paramValTemp = subLine.Substring(0, 39).Trim();
                    string paramVal = paramValTemp.Substring(paramValTemp.Length - 6).Trim();

                    Array.Resize(ref hstatValues, valueIndex + 1);
                    hstatValues[valueIndex] = double.Parse(paramVal);
                    valueIndex++;
                    turbaOutputModel.OutputDataList[i].HGES = Convert.ToDouble(paramVal);
                    // ergResult.Cells[i + 3, 37].Value = paramVal;

                    // Only considering first occurrence. Skip to next LP
                    break;
                }
            }
        }
    }
    public void GetParam_AK_LECKD_1()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "AK-LECKD";
        int parameterPos = 12;
        int parameterLen = parameter.Length;
        int valueIndex = 0;
        double[] hstatValues = new double[0];
        string [] fileLines = File.ReadAllLines(filePath);

        // int lpCount = ergResult.Cells[ergResult.Rows.Count, 2].End(XlDirection.xlUp).Row - 3;

        for (int i = 1; i <= lpCount; i++)
        {
            int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);//(int)(ergResult.Cells[i + 3, 3] as Range).Value;
            int rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);//(int)(ergResult.Cells[i + 4, 3] as Range).Value;

            if (i == lpCount)
            {
                rangeStop = fileLines.Length-1;//ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;
            }

            for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
            {
                string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
                if (line.Contains(parameter))
                {
                    string subLine = line.Substring(parameterPos - 1);
                    string paramVal = subLine.Substring(subLine.Length - 6).Trim();

                    Array.Resize(ref hstatValues, valueIndex + 1);
                    hstatValues[valueIndex] = double.Parse(paramVal);
                    valueIndex++;
                    turbaOutputModel.OutputDataList[i].AKLECKD1 = Convert.ToDouble(paramVal);
                    // ergResult.Cells[i + 3, 39].Value = paramVal;

                    // Only considering first occurrence. Skip to next LP
                    break;
                }
            }
        }
    }

    public void GetParam_AK_LECKD_2()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "AK-LECKD";
        int parameterPos = 12;
        int parameterLen = parameter.Length;
        int valueIndex = 0;
        double[] hstatValues = new double[0];
        string [] fileLines = File.ReadAllLines(filePath);

        // int lpCount = ergResult.Cells[ergResult.Rows.Count, 2].End(XlDirection.xlUp).Row - 3;

        for (int i = 1; i <= lpCount; i++)
        {
            int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);//(int)(ergResult.Cells[i + 3, 3] as Range).Value;
            int rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);//(int)(ergResult.Cells[i + 4, 3] as Range).Value;

            if (i == lpCount)
            {
                rangeStop = fileLines.Length; //ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;
            }

            int occurrenceCounter = 0;

            for (int lineNumber = rangeStart; lineNumber < rangeStop; lineNumber++)
            {
                string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
                if (line.Contains(parameter))
                {
                    occurrenceCounter++;

                    if (occurrenceCounter == 2)
                    {
                        string subLine = line.Substring(parameterPos - 1);
                        string paramVal = subLine.Substring(subLine.Length - 6).Trim();

                        Array.Resize(ref hstatValues, valueIndex + 1);
                        hstatValues[valueIndex] = double.Parse(paramVal);
                        valueIndex++;
                        turbaOutputModel.OutputDataList[i].AKLECKD2 = Convert.ToDouble(paramVal);
                        // ergResult.Cells[i + 3, 40].Value = paramVal;

                        // Only considering first occurrence. Skip to next LP
                        break;
                    }
                }
            }
        }
    }
    public void GetParam_DT()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "DT ";
        int parameterPos = 15;
        int parameterLen = parameter.Length;
        string [] fileLines = File.ReadAllLines(filePath);
        // int ergEoF = ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
            if (line.Contains(parameter))
            {
                string subLine = line.Substring(line.Length - 50);
                string paramVal = subLine.Substring(0, 10).Trim();

                // ergResult.Range["AP2"].Value = paramVal;
                turbaOutputModel.OutputDataList[0].DT = Convert.ToDouble(paramVal);

                // Only considering first occurrence. Skip to next LP
                break;
            }
        }
        if(turbaOutputModel.OutputDataList[0].DT>=400){
            turbaOutputModel.Check_DT="TRUE";
        }else{
            turbaOutputModel.Check_DT="FALSE";
        }
        
    }

    public void GetParam_SAXA()
    {
        // Application excelApp = new Application();
        // Workbook workbook = excelApp.ActiveWorkbook;
        // Worksheet ergData = workbook.Worksheets["ERG_DATA"];
        // Worksheet ergResult = workbook.Worksheets["Output"];

        string parameter = "!     STUFE  RT  SP  HD/ANS SRAD   SAXA";
        int parameterPos = 19;
        int parameterLen = parameter.Length;
        string [] fileLines = File.ReadAllLines(filePath);

        // int ergEoF = ergData.Cells[ergData.Rows.Count, 2].End(XlDirection.xlUp).Row;

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];//(ergData.Cells[lineNumber, 2] as Range).Value.ToString();
            if (line.Contains(parameter))
            {
                line = fileLines[lineNumber+2];//(ergData.Cells[lineNumber + 2, 2] as Range).Value.ToString();
                string subLine = line.Substring(0, 40);
                string paramVal = subLine.Substring(subLine.Length - 5).Trim();
                turbaOutputModel.OutputDataList[0].SAXA_SAXI = Convert.ToDouble(paramVal);
                // ergResult.Range["AR2"].Value = paramVal;

                // Only considering first occurrence. Skip to next LP
                break;
            }
        }
        if(turbaOutputModel.OutputDataList[0].SAXA_SAXI>=2.2){
            turbaOutputModel.Check_SAXA_MINUS_SAXI = "TRUE";
        }else{
            turbaOutputModel.Check_SAXA_MINUS_SAXI = "FALSE";
        }
        
    }
    //public void GetGEF_RDEHN()
    //{
    //    // Excel.Application excelApp = new Excel.Application();
    //    // Excel.Workbook workbook = excelApp.Workbooks.Open("path_to_your_workbook.xlsx");
    //    // Excel.Worksheet ergData = workbook.Sheets["ERG_DATA"];
    //    // Excel.Worksheet ergResult = workbook.Sheets["Output"];

    //    string parameter = "           STUFE  RSPALT  RDZENT   RDEHN  GEF";
    //    int outputCellRow = 1; // Starting at AY4
    //    string[] fileLines = File.ReadAllLines(filePath);

    //    // int ergEoF = ergData.Cells[ergData.Rows.Count, 2].End(Excel.XlDirection.xlUp).Row;

    //    for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
    //    {
    //        if (fileLines[lineNumber] != null && fileLines[lineNumber].Contains(parameter))
    //        {
    //            lineNumber += 3;
    //            for (int lineNum = lineNumber; lineNum < fileLines.Length; lineNum++)
    //            {
    //                string line = fileLines[lineNum];//ergData.Cells[lineNum, 2].Value?.ToString();
    //                if (string.IsNullOrWhiteSpace(line) || line.Trim().Length < 10)
    //                {
    //                    goto ReturnSub;
    //                }

    //                string paramStar = line.Trim().Substring(line.Trim().Length - 3);
    //                if (paramStar == "*")
    //                {
    //                    string subLine = line.Substring(0, 42);
    //                    string paramVal = subLine.Substring(subLine.Length - 7).Trim();
    //                    turbaOutputModel.OutputDataList[outputCellRow].AZ = Convert.ToDouble(paramVal);
    //                    turbaOutputModel.OutputDataList[outputCellRow].AY = outputCellRow;//Convert.ToInt32(paramVal);
    //                    // ergResult.Cells[outputCellRow, 52].Value = paramVal; // AZ column
    //                    // ergResult.Cells[outputCellRow, 51].Value = outputCellRow - 3; // AY column
    //                    outputCellRow++;
    //                }
    //            }
    //            goto ReturnSub;
    //        }
    //    }

    //ReturnSub:
    //    return;
    //}
    public void GetGEF_RDEHN()
    {
        string parameter = "           STUFE  RSPALT  RDZENT   RDEHN  GEF";
        int outputCellRow = 0; // Start at 0 for zero-indexed collections
        string[] fileLines = File.ReadAllLines(filePath);

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            if (fileLines[lineNumber]?.Contains(parameter) == true)
            {
                lineNumber += 3; // Skip 3 lines after finding parameter

                for (int lineNum = lineNumber; lineNum < fileLines.Length; lineNum++)
                {
                    string line = fileLines[lineNum];

                    // Check if line is valid
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().Length < 10)
                    {
                        return; // Exit method instead of goto
                    }

                    // Check if line ends with asterisk
                    string trimmedLine = line.Trim();
                    if (trimmedLine.EndsWith("*"))
                    {
                        // Ensure line is long enough for substring operations
                        if (line.Length >= 42)
                        {
                            string subLine = line.Substring(0, 42);
                            if (subLine.Length >= 7)
                            {
                                string paramVal = subLine.Substring(subLine.Length - 7).Trim();

                                // Ensure OutputDataList has enough capacity
                                if (outputCellRow < turbaOutputModel.OutputDataList.Count)
                                {
                                    if (double.TryParse(paramVal, out double value))
                                    {
                                        turbaOutputModel.OutputDataList[outputCellRow].AZ = value;
                                        turbaOutputModel.OutputDataList[outputCellRow].AY = outputCellRow;
                                        outputCellRow++;
                                    }
                                }
                            }
                        }
                    }
                }
                return; // Exit after processing the found parameter section
            }
        }
    }

    public void GetCheck_AENTW()
    {
        // Excel.Application excelApp = new Excel.Application();
        // Excel.Workbook workbook = excelApp.Workbooks.Open("path_to_your_workbook.xlsx");
        // Excel.Worksheet ergData = workbook.Sheets["ERG_DATA"];
        // Excel.Worksheet ergResult = workbook.Sheets["Output"];

        string parameter = "!         STUFE  DLTLE    DLTLA";
        int parameterPos = 28;
        int parameterLen = parameter.Length;
        string [] fileLines = File.ReadAllLines(filePath);

        // int ergEoF = ergData.Cells[ergData.Rows.Count, 2].End(Excel.XlDirection.xlUp).Row;
        // int lpCount = ergResult.Cells[ergResult.Rows.Count, 2].End(Excel.XlDirection.xlUp).Row - 3;

        for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
        {
            string line = fileLines[lineNumber];//ergData.Cells[lineNumber, 2].Value?.ToString();
            if (line != null && line.Contains(parameter))
            {
                lineNumber += 3;
                line = fileLines[lineNumber];//ergData.Cells[lineNumber, 2].Value?.ToString();
                double initLang = 0;
                turbaOutputModel.Check_AENTW = "TRUE";

                // ergResult.Cells[3, 43].Value = true; // AQ3

                for (int lineNumberNested = lineNumber - 1; lineNumberNested < fileLines.Length; lineNumberNested++)
                {
                    lineNumber = lineNumberNested;
                    line = fileLines[lineNumber];//ergData.Cells[lineNumber, 2].Value?.ToString();

                    if (!string.IsNullOrEmpty(line) && line.Length > 10)
                    {
                        string langLaterStr = line.Substring(0, 60).Trim().Substring(line.Substring(0, 60).Trim().Length - 5);
                        double langLater = double.Parse(langLaterStr);

                        if (langLater != 0)
                        {
                            turbaOutputModel.Check_AENTW = "FALSE";//ergResult.Cells[3, 43].Value = false; // AQ3
                        }

                        initLang = langLater;
                    }

                    if (line.Contains("!"))
                    {
                        goto ReturnSub;
                    }
                }
            }
        }

    ReturnSub:
        return;
    }

}