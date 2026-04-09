namespace Handlers.Exec_ERG_Handler;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Interfaces.ILogger;
using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Configuration;
using Models.TurbaOutputDataModel;
using Microsoft.Extensions.DependencyInjection;
using StartExecutionMain;
using Utilities.Logger;


public class EXECERGFileReader
{
    TurbaOutputModel turbaOutputModel;
    int lpCount;
    ILogger logger;



    public EXECERGFileReader(){
       
    turbaOutputModel = TurbaOutputModel.getInstance();
     IConfiguration configuration = new ConfigurationBuilder()
     .SetBasePath(Directory.GetCurrentDirectory())
     .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
     .Build();
    //  configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
     lpCount = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
     logger = MainExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();
    }
    string filePath = "C:\\testDir\\TURBATURBAE1.DAT.ERG";
    public void LoadERGFile(int mxLPs = 0)
    {
        if (mxLPs != 0)
        {
            lpCount = mxLPs - 1;
            //Console.WriteLine("LLLLLLLLPPPPPPPPPPPPPSSSSSSSSSSSSSSSSS:"+ lpCount);
        }
        logger.LogInformation("Reading ERG File..");
        if (mxLPs == 0)
        {
            turbaOutputModel.OutputDataList.Clear();
            turbaOutputModel.fillTurbaOutputDataList();
        }
        else
        {
            turbaOutputModel.OutputDataList.Clear();
            turbaOutputModel.fillTurbaOutputDataList();
            for (int i = 1; i <= mxLPs; ++i)
            {
                turbaOutputModel.OutputDataList[i] = new OutputLoadPoint();
            }
        }
        logger.LogInformation("Reading ERG File..");
        //turbaOutputModel.OutputDataList.Clear();
        //turbaOutputModel.fillTurbaOutputDataList();
        
        ScanTurbaERG();
        
    }


    public virtual void ScanTurbaERG()
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

        turbaOutputModel.OutputDataList[3].FMIN1 = 0;
        turbaOutputModel.OutputDataList[3].DUESEN  =0;
    }
public void GetLPfromERG()
{
    int lpMax=50;

    for (int loadPoint = 1; loadPoint <= lpMax; loadPoint++)
    {
        string flag = loadPoint < 10 ? $"#NLP {loadPoint}" : $"#NLP{loadPoint}";
        int searchState=0;
        int lineNumber = 0;  
        using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    // Process each line (or display it)
                    if(line==flag){
                      searchState=1;
                        break;
                    }
                }
            }
            if(searchState > 0){
              turbaOutputModel.OutputDataList[loadPoint].LoadPoint = flag;
            //   Console.WriteLine(turbaOutputModel.OutputDataList[loadPoint].LoadPoint);
              turbaOutputModel.OutputDataList[loadPoint].ERG_Line = lineNumber;
            //   Console.WriteLine(turbaOutputModel.OutputDataList[loadPoint].ERG_Line);
            }
     }
   }

public void GetParam_ABWEICHUNG()
{  
   string parameter = "ABWEICHUNG";
   int parameterPos = 43;
   int parameterLen = parameter.Length;
   string[] fileLines = File.ReadAllLines(filePath);
   for(int i = 1;i <= lpCount; i++){
      double start = turbaOutputModel.OutputDataList[i].ERG_Line;
      double end;
      if(i == lpCount){
        end = fileLines.Length-1;
      }else{
        end = turbaOutputModel.OutputDataList[i+1].ERG_Line;
      }
      for(double lineNumber = start; lineNumber <= end; lineNumber++){
        string line = fileLines[(int)lineNumber];
        if(line!=null && line.Contains(parameter)){
          string subLine = line.Substring(parameterPos - 1);
          string paramVal = subLine.Substring(parameterLen).Trim();
          turbaOutputModel.OutputDataList[i].ABWEICHUNG = Convert.ToDouble(paramVal.Replace("%","").Trim());
        }
      }
   }
}


  public void GetParam_DELTA_T()
   {  
   string parameter = "DELTA-T LEITSCHAUFELTRAEGER  1:";
   int parameterPos = 43;
   int parameterLen = parameter.Length;
   string[] fileLines = File.ReadAllLines(filePath);
   int len = fileLines.Length;
   for(int lineNumber=0;lineNumber<len;lineNumber++){
    string line = fileLines[lineNumber];
    if(line!=null && line.Contains(parameter)){
      string subLine = line.Substring(parameterPos - 1);
      string paramVal = subLine.Substring(0, 4).Trim();
      turbaOutputModel.OutputDataList[0].DELTA_T = Convert.ToDouble(paramVal);
      break;
    }
   }
}


public void GetParam_RADRAUM()
{  
   string parameter = "RADRAUM";
   int parameterPos = 61;
   int parameterLen = parameter.Length;
   string[] fileLines = File.ReadAllLines(filePath);
   int len = fileLines.Length;
   for(int lineNumber=1;lineNumber<len;lineNumber++){
    string line = fileLines[lineNumber];
    if(line!=null && line.Contains(parameter)){
      string paramVal = line.Substring(0, parameterPos).Substring(parameterPos - 7, 7).Trim();
      turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure = Convert.ToDouble(paramVal);
      string subLine = line.Substring(73).Trim();
      turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature = Convert.ToDouble(subLine);
      break;
    }
   }
}
public void GetParam_FMIN_1()
{
   string parameter = "FMIN1";
   int parameterLen = parameter.Length;
   string[] fileLines = File.ReadAllLines(filePath);
   int len = fileLines.Length;
   for(int lineNumber=1;lineNumber<len;lineNumber++){
    string line = fileLines[lineNumber];
    if(line!=null && line.Contains(parameter)){
     string subLine = line.Substring(0, 33).Trim(); // first 33 chrs
     string paramVal = subLine.Substring(5).Trim(); // string[6-32]=27chars
      turbaOutputModel.OutputDataList[0].FMIN1 = Convert.ToDouble(paramVal);
      //logger.LogInformation($"{len}, {lineNumber}, {paramVal}");
      subLine = line.Substring(59).Trim();
      paramVal = subLine.Substring(0, subLine.Length - 7).Trim();
      turbaOutputModel.OutputDataList[0].DUESEN = Convert.ToDouble(paramVal);
      turbaOutputModel.OutputDataList[0].FMIN1_DUESEN = Convert.ToDouble(paramVal);
      break;
    }
   }
}


public void GetParam_FMIN_2()
{  
   string parameter = "FMIN2";
   int parameterLen = parameter.Length;
   string[] fileLines = File.ReadAllLines(filePath);
   int len = fileLines.Length;
   for(int lineNumber=1;lineNumber<len;lineNumber++){
    string line = fileLines[lineNumber];
    if(line!=null && line.Contains(parameter)){
     string subLine = line.Substring(0, 33).Trim();
     string paramVal = subLine.Substring(5).Trim();
      turbaOutputModel.OutputDataList[0].FMIN2=Convert.ToDouble(paramVal);
      //logger.LogInformation($"{len}, {lineNumber}, {paramVal}");
       subLine = line.Substring(59).Trim();
            paramVal = subLine.Substring(0, subLine.Length - 6).Trim();
      turbaOutputModel.OutputDataList[0].FMIN2_DUESEN=Convert.ToDouble(paramVal);
      break;
    }
   }
}
public void GetParam_AUSTRITT()
{          
            
   string nextSearchText = "AUSTRITT";
   bool found = false;
   string[] fileLines = File.ReadAllLines(filePath);
   int lastRow = fileLines.Length;
   for(int lineNumber=1;lineNumber<lastRow;lineNumber++){
     if (!found)
        {
            if (fileLines[lineNumber] != null && fileLines[lineNumber].Contains("VOLSTR", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
            }
        }
        else
        {
            string line = fileLines[lineNumber];
            if (line != null)
            {
                int startPos = line.IndexOf(nextSearchText, StringComparison.OrdinalIgnoreCase);
                if (startPos >= 0)
                {
                    startPos += nextSearchText.Length;
                    string[] parts = line.Substring(startPos).Split(' ');
                    int nonEmptyCount = 0;
                    bool afterAUSTRITT = false;


                    foreach (string part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                        {
                            nonEmptyCount++;
                            if (afterAUSTRITT && nonEmptyCount == 2)
                            {
                           
                            turbaOutputModel.OutputDataList[0].Vol_Flow = Convert.ToDouble(part);//.Trim();
                            //logger.LogInformation(Convert.ToString(turbaOutputModel.OutputDataList[0].Vol_Flow));
                            // Console.WriteLine(turbaOutputModel.OutputDataList[0].Vol_Flow);
                            return;
                            }
                            afterAUSTRITT = true;
                        }
                    }
                }
            }
        }


   }
    if (!found)
    {
        Console.WriteLine("Initial line not found");
    }
    else
    {
        Console.WriteLine("Next line starting with AUSTRITT not found");
    }
}


public void GetCheck_GEF()
{  
   
   string parameter = "STUFE SIGZV";
        turbaOutputModel.OutputDataList[0].Bending = "";
   string[] fileLines = File.ReadAllLines(filePath);
   int lastRow = fileLines.Length;
   int lp = 0;
    for (int lineNumber = 1; lineNumber < lastRow; lineNumber++)
    {
        string line = fileLines[lineNumber];
        if (line != null && line.Contains(parameter))
        {
            ++lp;
            for (int lineNumberNested = lineNumber; lineNumberNested < lastRow; lineNumberNested++)
            {
                lineNumber = lineNumberNested;
                line = fileLines[lineNumberNested];
                if (line != null)
                {
                    if (line.Contains("Schaufel:"))
                    {
                        break;
                    }
                    try 
                    {
                        if (line.Length < 5)
                        {
                            continue;
                        }
                        string last5_line = line.Substring(line.Length - 5);
                        string trimmedLine = last5_line.Trim();
                        if (trimmedLine == "LR" || trimmedLine == "NIB")
                        {
                            if (line.Length < 14)
                            {
                                continue;
                            }
                            string gefValue = line.Substring(line.Length - 14, 9).Trim();
                            if (!string.IsNullOrEmpty(gefValue))
                            {
                                turbaOutputModel.OutputDataList[0].Bending = gefValue;
                                turbaOutputModel.OutputDataList[lp].Bending = gefValue;
                                Console.WriteLine($"Bending failed at line: {lineNumberNested}");
                                //return;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("error:"+e.Message);
                    }
                }
            }
        }
    }
}

public void GetCheck_PressureBar()
{          
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "STUFE  CAX";
    int ergEoF = fileLines.Length;
    turbaOutputModel.Stage_Pressure_Check = "TRUE";
    // int lpCount = turbaOutputModel.OutputDataList.Count()-1;
    int loadPointCounter = 1;
    for(int lineNumber = 0;lineNumber<ergEoF;lineNumber++){
      string line = fileLines[lineNumber];
      if(line!=null && line.Contains(parameter) && loadPointCounter<=lpCount-2){
        
        double initPressure = 0;
        for(int lineNumberNested = lineNumber;lineNumberNested<ergEoF;lineNumberNested++){
           lineNumber = lineNumberNested;
           line = fileLines[lineNumberNested];
           if(line!=null && line.Length>10 && line.Substring(line.Length-5).Trim().Length>4){
            string pressureLaterStr = line.Substring(0,50).Substring(42,8).Trim();
            if(double.TryParse(pressureLaterStr, out double pressureLater)){
              if((pressureLater > initPressure && initPressure > 0) || (turbaOutputModel.OutputDataList[7].Power_KW < 0.1 * turbaOutputModel.OutputDataList[1].Power_KW))
              {
                // Console.WriteLine("Pr Later:"+ pressureLater+", init Pre:"+ initPressure);
                turbaOutputModel.OutputDataList[0].Stage_Pressure = "LP" + (loadPointCounter - 1);
                turbaOutputModel.Stage_Pressure_Check="FALSE";
                // Console.w
                turbaOutputModel.OutputDataList[loadPointCounter].Stage_Pressure="FALSE";
                // loadPointCounter++; 
              }
              initPressure = pressureLater;
            }
           } 
           if(line !=null && line.Contains("#END")){
            break;
           }

        }
        loadPointCounter++;
        if(loadPointCounter > lpCount-2)
            break;
      }
    }
}


public void GetCheck_KLEMMENLEISTUNG()
{ 
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "KLEMMENLEISTUNG";
    int ergEoF = fileLines.Length;
    // int lpCount = turbaOutputModel.OutputDataList.Count()-1;
    int loadPointCounter = 1;

    for(int lineNumber = 1;lineNumber<ergEoF;lineNumber++){
      string line = fileLines[lineNumber];
      if(line!=null && line.Contains(parameter) && loadPointCounter<=lpCount){
        string power = line.Substring(line.Length-16);
        power = power.Substring(0,power.Length-5).Trim();
        // Console.WriteLine(loadPointCounter+" Powerrrrrrr"+ power);
        turbaOutputModel.OutputDataList[loadPointCounter].Power_KW=Convert.ToDouble(power);
        loadPointCounter++;
        if(loadPointCounter > lpCount)break;
        }
      }
      turbaOutputModel.OutputDataList[0].Power_KW= turbaOutputModel.OutputDataList[1].Power_KW;  
}
public void GetCheck_EffINNERER_WIRKG_GES()
{ 
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "INNERER WIRKG GES";
    int ergEoF = fileLines.Length;   
    // int lpCount = turbaOutputModel.OutputDataList.Count()-1;
    int loadPointCounter = 1;
    for(int lineNumber = 1;lineNumber<ergEoF;lineNumber++){
      string line = fileLines[lineNumber];
      if(line!=null && line.Contains(parameter) && loadPointCounter<=lpCount){
       
       string eff = line.Substring(line.Length - 12);//.Trim();
            eff = eff.Substring(0, eff.Length - 1).Trim();
        turbaOutputModel.OutputDataList[loadPointCounter].Efficiency=Convert.ToDouble(eff);
        loadPointCounter++;
        }
      }
      turbaOutputModel.OutputDataList[0].Efficiency= turbaOutputModel.OutputDataList[1].Efficiency;
      logger.LogInformation($"Base load Eff %:{turbaOutputModel.OutputDataList[1].Efficiency}");
    //   Console.WriteLine("Base load Eff %: " +  turbaOutputModel.OutputDataList[1].Efficiency);
}


public void GetParam_BEAUFSCHL()
{  
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "SPEZ. VOLUMEN";
    int ergEoF = fileLines.Length;
    for (int lineNumber = 1; lineNumber < ergEoF; lineNumber++)
    {
        string line = fileLines[lineNumber];
        if (line != null && line.Contains(parameter))
        {
            lineNumber++;
            line = fileLines[lineNumber];
            if (line != null)
            {
                string subLine = line.Substring(0, 59).Trim();
                string paramVal = subLine.Substring(subLine.Length - 5).Trim();

                turbaOutputModel.OutputDataList[0].Admission_Factor_Group1=Convert.ToDouble(paramVal);
            }


            lineNumber++;
            line = fileLines[lineNumber];
            if (line != null)
            {
                string subLine = line.Substring(0, 59).Trim();
                string paramVal = subLine.Substring(subLine.Length - 5).Trim();
                turbaOutputModel.OutputDataList[0].Admission_Factor_Group2=Convert.ToDouble(paramVal);
            }
            lineNumber++;
            line = fileLines[lineNumber];
            if (line != null)
            {
                string subLine = line.Substring(0, 59).Trim();
                string paramVal = subLine.Substring(subLine.Length - 5).Trim();
                turbaOutputModel.OutputDataList[0].Admission_Factor=Convert.ToDouble(paramVal);
            }


            break; // Only considering first occurrence. Skip to next LP
        }
    }

}

       
public void GetParam_DUESENGRUPPE_AUSGEST()
{ 
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "DUESENGRUPPE";
    int parameterPos = 12;
    int parameterLen = parameter.Length;
     for (int i = 1; i <= lpCount; i++)
    {
        int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);
        int rangeStop ;
        if (i == lpCount)
        {
            rangeStop = fileLines.Length-1;
        }else{
            rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);
        }


        for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line != null && line.Contains(parameter))
            {
                string subLine = line.Substring(0, 39);
                string paramVal = subLine.Substring(subLine.Length - 7).Trim();

                turbaOutputModel.OutputDataList[i].DUESEN_GRUPPE_AUSGEST=paramVal;


                break; // Only considering first occurrence. Skip to next LP
            }
        }
    }
}


public void GetParam_HOEHE()
{  
    string [] fileLines = File.ReadAllLines(filePath);
      string parameter = "HOEHE";
    int parameterPos = 17;
    int ergEoF = fileLines.Length;
    int parameterLen = parameter.Length;
   
    for (int lineNumber = 1; lineNumber < ergEoF; lineNumber++)
    {
        string line = fileLines[lineNumber];
        if (line != null && line.Contains(parameter))
        {
            string subLine = line.Substring(parameterPos - 1);
            string paramVal = subLine.Substring(0, 16).Trim();


            turbaOutputModel.OutputDataList[0].HOEHE=Convert.ToDouble(paramVal);


            break; // Only considering first occurrence. Skip to next LP
        }
    }
    
}
public void GetParam_TEILUNG()
{  
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "TEILUNG";
    int parameterPos = 19;
    int parameterLen = parameter.Length;


    int ergEoF = fileLines.Length;


    for (int lineNumber = 1; lineNumber < ergEoF; lineNumber++)
    {
        string line = fileLines[lineNumber];
        if (line != null && line.Contains(parameter))
        {
            string subLine = line.Substring(parameterPos - 1);
            string paramVal = subLine.Substring(0, 15).Trim();

            turbaOutputModel.OutputDataList[0].TEILUNG=Convert.ToDouble(paramVal);
            // ergResult.Cells["Z2"].Value = paramVal;


            break; // Only considering first occurrence. Skip to next LP
        }
    }
}


public void GetParam_WINKEL_ALFAA()
{ 
    string [] fileLines = File.ReadAllLines(filePath);
    string parameter = "WINKEL ALFAA";
    int parameterPos = 55;
    int parameterLen = parameter.Length;
    int ergEoF = fileLines.Length;
   
   for (int lineNumber = 1; lineNumber < ergEoF; lineNumber++)
    {
        string line = fileLines[lineNumber];
        if (line != null && line.Contains(parameter))
        {
            string subLine = line.Substring(parameterPos - 1);
            string paramVal = subLine.Substring(0, 10).Trim();


            turbaOutputModel.OutputDataList[0].WINKEL_ALFAA=Convert.ToDouble(paramVal);

            break; // Only considering first occurrence. Skip to next LP
        }
    }    
}
public void GetParam_FMIN_SOLL()
{  
      string [] fileLines = File.ReadAllLines(filePath);
    //   for(int i=0;i<fileLines.Length;i++){
    //     Console.WriteLine(fileLines[i]);
    //   }

      string parameter = "FMIN SOLL";
      int parameterPos = 52;
      int parameterLen = parameter.Length;
      
    
        // int lpCount =  turbaOutputModel.OutputDataList.Count()-1;

    
    for (int i = 1; i <= lpCount; i++)
      {
          int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);
        //   Console.WriteLine(rangeStart+"rangeeeeeeestart");
          int rangeStop;//= (int)ergResult.Cells[i + 4, 3].Value;


          if (i == lpCount)
          {
              rangeStop = fileLines.Length-1;
          }else{
              rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);
          }


          for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
          {
              string line = fileLines[lineNumber];
              if (line != null && line.Contains(parameter))
              {
                  string subLine = line.Substring(parameterPos - 1);
                  string paramVal = subLine.Substring(0, 13).Trim();
                //   Console.WriteLine("fminsolllllllllllllllllllllllllllll"+paramVal);

                  turbaOutputModel.OutputDataList[i].FMIN_SOLL=Convert.ToDouble(paramVal);
                  // ergResult.Cells[i + 3, 24].Value = paramVal;


                  break; // Only considering first occurrence. Skip to next LP
              }
          }
      }
}


public void GetParam_FMIN_IST()
{  
           
      string [] fileLines = File.ReadAllLines(filePath);
      string parameter = "FMIN IST";
    int parameterPos = 20;
    int parameterLen = parameter.Length;
      
    
        // int lpCount =  turbaOutputModel.OutputDataList.Count()-1;

    
    for (int i = 1; i <=lpCount; i++)
    {
        int rangeStart = Convert.ToInt32(turbaOutputModel.OutputDataList[i].ERG_Line);
        int rangeStop ; //= (int)ergResult.Cells[i + 4, 3].Value;


        if (i == lpCount)
        {
            rangeStop = fileLines.Length-1;

        }else{
         rangeStop = Convert.ToInt32(turbaOutputModel.OutputDataList[i+1].ERG_Line);

        }


        for (int lineNumber = rangeStart; lineNumber <= rangeStop; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line != null && line.Contains(parameter))
            {
                string subLine = line.Substring(parameterPos - 1);
                string paramVal = subLine.Substring(0, 13).Trim();

                turbaOutputModel.OutputDataList[i].FMIN_IST=Convert.ToDouble(paramVal);
                // ergResult.Cells[i + 3, 23].Value = paramVal;


                break; // Only considering first occurrence. Skip to next LP
            }
        }
    }
}

public void GetCheck_Exhaust_Temperature(){
 double [] paramsArray = new double[16];
 string parameter = "UE-TEIL1";
 int parameterPos = 20;
 int parameterLen = parameter.Length;
 string [] fileLines = File.ReadAllLines(filePath);
 int ERG_Eof = fileLines.Length;
 int extractedCount = 0;
 for (int lineNumber = 1; lineNumber < ERG_Eof; lineNumber++)
        {
            string line = fileLines[lineNumber];
            if (line != null && line.Contains(parameter))
            {
                string subLine = line.Substring(parameterPos - 1);
                string subLineTemp = subLine.Substring(subLine.Length - 12).Trim();
                string paramValStr = subLineTemp.Substring(0, 5).Trim();
                if (double.TryParse(paramValStr, out double paramVal))
                {
                    paramsArray[extractedCount] = paramVal;
                    extractedCount++;

                    // Exit if 16 instances are found
                    if (extractedCount >= 16)
                    {
                        break;
                    }
                }
            }
        }

        double maxValue = paramsArray.Max();
        turbaOutputModel.OutputDataList[0].Max_Exhaust_Temperature = maxValue;
        // ergResult.Cells[2, 28].Value = maxValue; // AB2 corresponds to column 28


}
}


