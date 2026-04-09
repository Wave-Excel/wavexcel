using System;
using System.Linq;
using DocumentFormat.OpenXml.Math;
using ERG_PowerMatch;
using ERG_ValvePointOptimizer;
using ERG_Verification;
using Handlers.DAT_Handler;
using HMBD.HMBDInformation;
using HMBD.LoadPointGenerator;
using HMBD.Ref_DAT_selector;
using Microsoft.Extensions.Configuration;
using Models.AdditionalLoadPointModel;
using Models.LoadPointDataModel;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
using OfficeOpenXml;
using Turba.TurbaConfiguration;
//using Budoom;
using StartExecutionMain;
using Interfaces.ILogger;
using System.Runtime.InteropServices;
using Interfaces.IThermodynamicLibrary;
using System.ComponentModel.DataAnnotations;
using Services.ThermodynamicService;
using System.Security.Cryptography;
using HMBD.Power_KNN;
using Optimizers.ERG_NozzleOptimizer;
using Microsoft.Extensions.Hosting;
using Utilities.Logger;
using Models.PowerEfficiencyData;
using Models.PreFeasibility;
using Models.NozzleTurbaData;
using Ignite_x_wavexcel;
using Ignite_X.Models;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using System.Diagnostics;
using StartKreislExecution;
using Ignite_X.src.core.Handlers;
using Kreisl.KreislConfig;
using Ignite_X.src.core.Services;
using Interfaces.IERGHandlerService;
using Ignite_X.Converters;
using Microsoft.Extensions.Options;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using PdfSharp.Pdf.Annotations;
using DocumentFormat.OpenXml.Presentation;


namespace ExtraLoadPoints;
public class CustomLoadPointHandler
{
    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double hVon_p_t(double P, double T, double unknown);

    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double sVon_p_h(double P, double H, double unknown);

    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double pVon_h_s(double H, double S, double unknown);


    IThermodynamicLibrary thermodynamicService;

    private AdditionalLoadPoint additionalLoadPoint;
    private PowerNearest powerNearest = new PowerNearest();
    public static string excelPath;
    public static List<LoadPoint> extraLP = new List<LoadPoint>();
    LoadPointDataModel loadPointDataModel;
    TurbaOutputModel turbaOutputModel;
    TurbineDataModel turbineDataModel;
    public static Dictionary<int, int> lpNumberToIndexMap;
    public static List<CustomerLoadPoint> initList;

    ILogger logger;
 
    public CustomLoadPointHandler(){
        additionalLoadPoint = AdditionalLoadPoint.GetInstance();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        loadPointDataModel = LoadPointDataModel.getInstance();
        turbaOutputModel = TurbaOutputModel.getInstance();
        turbineDataModel = TurbineDataModel.getInstance();
        StartExec.GlobalHost = CreateHostBuilder(null).Build();
        additionalLoadPoint = AdditionalLoadPoint.GetInstance();
        MainExecutedClass.GlobalHost = StartExec.GlobalHost;
        //StartKreisl.GlobalHost = StartExec.FillGlobalHost();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        logger.clear();
    }
    public long cxLP_RngStart { get; set; }
    public long cxLP_RngStop { get; set; }
    public long cxLP_LPcount { get; set; }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services with the DI container
                    services.AddSingleton<IThermodynamicLibrary, ThermodynamicService>();
                    services.AddSingleton<ILogger, Logger>();
                });


    public List<Enquiry> getLoadPointsForEnquiry(string EnquiryId, string RevisionNo)
    {
        string sourceDirectory = @"C:\testDir";
        string csvFilePath = Path.Combine(sourceDirectory, "SampleData.csv");
        var filteredRows = new List<Enquiry>();

        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        }))
        {
            csv.Context.RegisterClassMap<EnquiryMap>();
            var records = csv.GetRecords<Enquiry>();
            foreach (var record in records)
            {
                if(record.EnquiryID.Equals(EnquiryId, StringComparison.OrdinalIgnoreCase) && record.RevisionNo.Equals(RevisionNo, StringComparison.OrdinalIgnoreCase))
                {
                    filteredRows.Add(record);
                }
            }
        }
        return filteredRows;
    }
    public void fillLoadPointList(){
        extraLP.Add(new LoadPoint());
        for(int i = 1; i<= cxLP_RngStop;i++)
        {
            extraLP.Add(new LoadPoint());
            extraLP[i].MassFlow = additionalLoadPoint.CustomerLoadPoints[i].SteamMass;
            extraLP[i].Rpm = 12000;
            extraLP[i].Pressure = additionalLoadPoint.CustomerLoadPoints[i].SteamPressure;
            extraLP[i].Temp = additionalLoadPoint.CustomerLoadPoints[i].SteamTemp;
            extraLP[i].EIN = 0;
            extraLP[i].BackPress = additionalLoadPoint.CustomerLoadPoints[i].ExhaustPressure;
            extraLP[i].BYP = -1;
            extraLP[i].EIN = 0;
            extraLP[i].WANZ = 0;
            extraLP[i].RSMIN = 0;
        }
        extraLP[1].BYP = 0;
        extraLP[1].WANZ = 1;
    }

    public void FillExtraLoadPoints(int maxLoadPoints)
    {
        
    }
    public void fillCustomerLoadPointList(List<CustomerLoadPoint> customerLPList)
    {
        NozzleTurbaDataModel.getInstance().fillNozzleTurbaDataModel();
        PowerEfficiencyModel.getInstance().fillPowerEfficiencyDataModel();
        PreFeasibilityDataModel.getInstance().fillPreFeasibilityData();
        LoadPointDataModel.getInstance().fillLoadPoints();
        TurbaOutputModel.getInstance().fillTurbaOutputDataList();

        //logger = StartExec.GlobalHost.Services.GetRequiredService<ILogger>();
        AdditionalLoadPoint.GetInstance().K = customerLPList.Count;
        AdditionalLoadPoint.GetInstance().FillCustomerLoadPoint();
        List<CustomerLoadPoint> lpList = AdditionalLoadPoint.GetInstance().CustomerLoadPoints;
        for(int i = 0; i < customerLPList.Count; ++i)
        {
            lpList[i+1] = (customerLPList[i]);
        }
    }
    public string MainTemp = "";
    public void fillLP(int i, string unk, int count)
    {
            
            string filePath = StartKreisl.filePath;
            KreislDATHandler kreislDATHandler = new KreislDATHandler();

            string dat = Path.Combine(AppContext.BaseDirectory, "loadPoint.dat");
            if(turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    dat = Path.Combine(AppContext.BaseDirectory, "LoadPointDumpCondenPRV.DAT");
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    dat = Path.Combine(AppContext.BaseDirectory, "loadpointclosecyclePRV.DAT");
                }
                turbineDataModel.PST  = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure) + 5) : turbineDataModel.PST;
            }
            else if (turbineDataModel.PST > 0)
            {
                 dat = Path.Combine(AppContext.BaseDirectory, "loadPointD.dat");
            }

            if(unk == "Pr")
            {
                File.Copy(dat, "C:\\testDir\\KREISL.DAT",true);
                File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
                string content = File.ReadAllText(dat);
                content = content.Replace("lp", count.ToString());
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
                kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamMass.ToString());
                kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
                kreislDATHandler.FillInletPressure(filePath, 5, "42.981",-1);
                kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure.ToString());
                kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamTemp.ToString());
                kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration+25).ToString());
            
                if (turbineDataModel.PST > 0)
                {
                    kreislDATHandler.FillPressureDesh(filePath, 8, "80");
                }
                if (turbineDataModel.DumpCondensor)
                {

                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity > 0)
                    {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i , AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                    {
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                    {
                        kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                        kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                        kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                        kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                        kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                    }
                }
            }
            else if(unk == "M")
            {
                File.Copy(dat, "C:\\testDir\\KREISL.DAT",true);
                File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
                string content = File.ReadAllText(dat);
                content = content.Replace("lp", count.ToString());
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
                kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
                kreislDATHandler.FillMassFlow(filePath, 5, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow+10).ToString(), -1);
                kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure.ToString());
                kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure.ToString());
                kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamTemp.ToString());
                
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration != 0)
                {
                    kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration + 25).ToString());
                }
                else if ((turbineDataModel.DeaeratorOutletTemp > 0 ||  turbineDataModel.PST  > 0) && AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow != 0)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                }
                if (turbineDataModel.DumpCondensor)
                {
                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity > 0)
                    {
                        turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity;
                        kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity.ToString());
                        kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                    {
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                        kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration + 25).ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                    {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                }
            }
            else if(unk == "T")
                {
                    File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
                    File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
                    string content = File.ReadAllText(dat);
                    content = content.Replace("lp", count.ToString());
                    File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
                    kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamMass.ToString());
                    kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure.ToString());
                    kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure.ToString());
                    kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
                    kreislDATHandler.FillInletTemperature(filePath, 5, "440",-1);
                    kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration + 25).ToString());
                if (turbineDataModel.DumpCondensor)
                {

                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity > 0)
                    {
                        turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity;
                        kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity.ToString());
                        kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                    {
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                    {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                }
            }
            else if(unk == "P")
                {
                File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
                File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
                string content = File.ReadAllText(dat);
                content = content.Replace("lp", count.ToString());
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
                kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamMass.ToString());
                kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure.ToString());
                kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure.ToString());
                kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamTemp.ToString());
                if (turbineDataModel.DumpCondensor)
                {
                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity > 0)
                    {
                        turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity;
                        kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity.ToString());
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow).ToString(), -1);
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                    {
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                    {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                }

            }
            else if (unk == "E")
            {
                File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
                File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
                string content = File.ReadAllText(dat);
                content = content.Replace("lp", count.ToString());
                File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
                kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamMass.ToString());
                kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure.ToString());
                kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
                kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59",-1);
                kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamTemp.ToString());
                kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PowerGeneration + 25).ToString());
                if (turbineDataModel.DumpCondensor)
                {
                    if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity > 0)
                    {
                        turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity;
                        kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity.ToString());
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                        kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow).ToString(), -1);
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                    {
                        kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustMassFlow.ToString());
                    }
                    else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                    {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                }
            }
            if (turbineDataModel.DeaeratorOutletTemp > 0 && !turbineDataModel.IsPRVTemplate)
            {
                    if (turbineDataModel.DumpCondensor) {
                    
                    kreislDATHandler.UpdateTemplatePRVToWPRVInDumpCondensor(filePath);  
                    
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {

                    kreislDATHandler.UpdateTemplatePRVToWPRV(filePath);

                    }
            }
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                kreislDATHandler.MakeUpTemperature(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].MakeUpTempe.ToString());
                kreislDATHandler.Processcondensatetemperature(filePath, 12, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].CondRetTemp.ToString());
                kreislDATHandler.FillCondensateReturn(filePath, "14", AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ProcessCondReturn.ToString());
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure) + 5) : turbineDataModel.PST;
                kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    if (kreislDATHandler.InPrvMultipleBackPressure(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].ExhaustPressure))
                    {
                        kreislDATHandler.fillPsatvont_t(filePath, 13, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].DeaeratorOutletTemp.ToString());
                    }
                    else
                    {
                        if (turbineDataModel.DumpCondensor)
                        {
                        kreislDATHandler.MakePRVToWPRVMultipleinDumpCondensor(filePath);
                        }
                        else if (!turbineDataModel.DumpCondensor)
                        {
                        kreislDATHandler.MakePRVToWPRVMultiple(filePath);

                        }
                    }
                }
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure != 0)
                {
                    if (unk != "Pr")
                    {
                        kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure).ToString());
                    }
                }
            }
            else if (turbineDataModel.PST > 0)
            {
                kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
                if(unk != "Pr")
                {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].SteamPressure).ToString());

                }
            }
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[i].PST == 0)
                {
                   turbineDataModel.PST = 0;
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
    }
    public void fillLPAgain(int i, string unk, int count,List<CustomerLoadPoint> initList)
    {

        string filePath = StartKreisl.filePath;

        string dat = Path.Combine(AppContext.BaseDirectory, "loadPoint.dat");
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor)
            {
                dat = Path.Combine(AppContext.BaseDirectory, "LoadPointDumpCondenPRV.DAT");
            }
            else if (!turbineDataModel.DumpCondensor)
            {
                dat = Path.Combine(AppContext.BaseDirectory, "loadpointclosecyclePRV.DAT");
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            dat = Path.Combine(AppContext.BaseDirectory, "loadPointD.dat");
        }
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        //turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(initList[i].ExhaustPressure) + 5) : turbineDataModel.PST;

        if (unk == "Pr")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
            kreislDATHandler.FillInletPressure(filePath, 5, "42.981", -1);
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            
            if (turbineDataModel.PST > 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, "80".ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        else if (unk == "M")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
            kreislDATHandler.FillMassFlow(filePath, 5, (initList[i].ExhaustMassFlow+10).ToString(), -1);
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            //kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            
            if (initList[i].PowerGeneration != 0)
            {
                kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            }
            else if ((turbineDataModel.DeaeratorOutletTemp > 0  || turbineDataModel.PST > 0) && initList[i].ExhaustMassFlow != 0)
            {
                kreislDATHandler.ProcessMassFlow(filePath, 9, initList[i].ExhaustMassFlow.ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    //kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());

                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, initList[i].PowerGeneration + 25.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        else if (unk == "T")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
            kreislDATHandler.FillInletTemperature(filePath, 5, "440", -1);
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        else if (unk == "P")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        else if (unk == "E")
        {
            File.Copy(dat, "C:\\testDir\\KREISL.DAT", true);
            File.WriteAllText("C:\\testDir\\KREISL.DAT", "");
            string content = File.ReadAllText(dat);
            content = content.Replace("lp", count.ToString());
            File.AppendAllText("C:\\testDir\\KREISL.DAT", content);
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
            kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59", -1);
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0 && !turbineDataModel.IsPRVTemplate)
        {
            if (turbineDataModel.DumpCondensor)
            {

                kreislDATHandler.UpdateTemplatePRVToWPRVInDumpCondensor(filePath);

            }
            else if (!turbineDataModel.DumpCondensor)
            {

                kreislDATHandler.UpdateTemplatePRVToWPRV(filePath);

            }
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            kreislDATHandler.MakeUpTemperature(filePath, 9, initList[i].MakeUpTempe.ToString());
            kreislDATHandler.Processcondensatetemperature(filePath, 12, initList[i].CondRetTemp.ToString());
            kreislDATHandler.FillCondensateReturn(filePath, "14", initList[i].ProcessCondReturn.ToString());
            turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                if (kreislDATHandler.InPrvMultipleBackPressure(initList[i].ExhaustPressure))
                {
                    kreislDATHandler.fillPsatvont_t(filePath, 13, initList[i].DeaeratorOutletTemp.ToString());
                }
                else
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultipleinDumpCondensor(filePath);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultiple(filePath);

                    }
                }
            }
            if (initList[i].SteamPressure != 0)
            {
                if (unk != "Pr")
                {
                    kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            if(unk != "Pr")
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (initList[i].PST == 0)
            {
                turbineDataModel.PST = 0;
            }
        }
        MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");

    }
    
    public void fillAGainDat(int i , List<CustomerLoadPoint> initList)
    {
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        //List<CustomerLoadPoint> input = AdditionalLoadPoint.GetInstance().CustomerLoadPoints;
        string filePath = StartKreisl.filePath;
        double press = initList[i].SteamPressure;
        double Temp = initList[i].SteamTemp;
        double mass = initList[i].SteamMass;
        double exPres = initList[i].ExhaustPressure;
        double Power = initList[i].PowerGeneration;
        double exMass = initList[i].ExhaustMassFlow;


        if (initList[i].DeaeratorOutletTemp > 0)
        {
            kreislDATHandler.MakeUpTemperature(filePath, 9, initList[i].MakeUpTempe.ToString());
            kreislDATHandler.Processcondensatetemperature(filePath, 12, initList[i].CondRetTemp.ToString());
            kreislDATHandler.FillCondensateReturn(filePath, "14", initList[i].ProcessCondReturn.ToString());
            turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(initList[i].ExhaustPressure) + 5) : turbineDataModel.PST;
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(initList[i].ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                if (kreislDATHandler.InPrvMultipleBackPressure(initList[i].ExhaustPressure))
                {
                    
                    kreislDATHandler.fillPsatvont_t(filePath, 13, initList[i].DeaeratorOutletTemp.ToString());
                }
                else
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultipleinDumpCondensor(filePath);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.MakePRVToWPRVMultiple(filePath);
                    }
                }
            }
            if (initList[i].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            if (initList[i].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * initList[i].SteamPressure).ToString());
            }
        }
        if(turbineDataModel.DeaeratorOutletTemp == 0 && turbineDataModel.PST  == 0)
        {
            if (mass == 0 && exMass > 0)
            {
                initList[i].SteamMass = 0.055 + initList[i].ExhaustMassFlow;
            }
            else if (mass > 0 && exMass == 0)
            {
                initList[i].ExhaustMassFlow = initList[i].SteamMass - 0.055;
            }
        }
        

        if (initList[i].SteamPressure == 0)
        {

            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
            kreislDATHandler.FillInletPressure(filePath, 5, "42.981", -1);
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if(turbineDataModel.PST > 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, "80") ;
            }
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10+ initList[i].ExhaustMassFlow).ToString(),-1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].SteamTemp == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
            kreislDATHandler.FillInletTemperature(filePath, 5, "440", -1);
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if (turbineDataModel.DumpCondensor)
            {

                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].SteamMass == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
            kreislDATHandler.FillMassFlow(filePath, 5, (initList[i].ExhaustMassFlow+10).ToString(), -1);
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            //kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if (initList[i].PowerGeneration != 0)
            {
                kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            }
            else if ((turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0) && initList[i].ExhaustMassFlow != 0)
            {
                kreislDATHandler.ProcessMassFlow(filePath, 9, initList[i].ExhaustMassFlow.ToString());
            }
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].PowerGeneration == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, initList[i].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    //kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
                    //kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (initList[i].ExhaustPressure == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, initList[i].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, initList[i].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
            kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59", -1);
            kreislDATHandler.FillInletTemperature(filePath, 5, initList[i].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (initList[i].PowerGeneration + 25).ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (initList[i].Capacity > 0)
                {
                    kreislDATHandler.fillCapacity(filePath, 9, initList[i].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + initList[i].ExhaustMassFlow).ToString(), -1);
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i, initList) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, initList[i].ExhaustMassFlow.ToString());
                }
                else if (initList[i].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(i,initList) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }

        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (initList[i].PST == 0)
            {
                turbineDataModel.PST = 0;
            }
        }
    }
    public void fillLPINDat()
    {

        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        List<CustomerLoadPoint> input = AdditionalLoadPoint.GetInstance().CustomerLoadPoints;
        string filePath = StartKreisl.filePath;
        double press = input[1].SteamPressure;
        double Temp = input[1].SteamTemp;
        double mass = input[1].SteamMass;
        double exPres = input[1].ExhaustPressure;
        double Power = input[1].PowerGeneration;
        double exMass = input[1].ExhaustMassFlow;
        double pst = input[1].PST;
        
        if (input[1].DeaeratorOutletTemp > 0)
        {
            kreislDATHandler.MakeUpTemperature(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].MakeUpTempe.ToString());
            kreislDATHandler.Processcondensatetemperature(filePath, 12, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].CondRetTemp.ToString());
            kreislDATHandler.FillCondensateReturn(filePath, "14", AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ProcessCondReturn.ToString());
            turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
            if (turbineDataModel.IsPRVTemplate)
            {
                kreislDATHandler.fillPsatvont_t(filePath, 13, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].DeaeratorOutletTemp.ToString());
            }
            if (input[1].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure).ToString());
            }
        }
        else if(pst > 0)
        {
            kreislDATHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            if (input[1].SteamPressure != 0)
            {
                kreislDATHandler.FillPressureDesh(filePath, 8, (1.2 * AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure).ToString());
            }
        }
        if(input[1].DeaeratorOutletTemp ==  0 && pst == 0)
        {
            if (mass == 0 && exMass > 0)
            {
                input[1].SteamMass = 0.055 + input[1].ExhaustMassFlow;
            }
            else if (mass > 0 && exMass == 0)
            {
                input[1].ExhaustMassFlow = input[1].SteamMass - 0.055;
            }
        }
        
        

        if (input[1].SteamPressure == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, "0.000");
            kreislDATHandler.FillInletPressure(filePath, 5, "42.981", -1);
            kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration + 25).ToString());
            if(turbineDataModel.PST > 0)
            {

                kreislDATHandler.FillPressureDesh(filePath,8, "80");
            }
            if (turbineDataModel.DumpCondensor)
            {
                
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity > 0)
                {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }
                else if(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }else if(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath,3,9,15,1,-21);
                    kreislDATHandler.TurnOffCondensor(filePath,5,14,13,1,-21);
                    kreislDATHandler.TurnOffCondensor(filePath,2,18,19,1,-21);
                    kreislDATHandler.TurnOffCondensor(filePath,3,18,19,1,-20);
                    kreislDATHandler.TurnOffCondensor(filePath,2,19,4,1,-20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (input[1].SteamTemp == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, "0.000");
            kreislDATHandler.FillInletTemperature(filePath, 5, "440", -1);
            kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration + 25).ToString());

            if (turbineDataModel.DumpCondensor)
            {

                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity > 0)
                {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow).ToString(), -1);
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (input[1].SteamMass == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, "0.000");
            kreislDATHandler.FillMassFlow(filePath, 5, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow+10).ToString(), -1);
            kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamTemp.ToString());
            if(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
            {
                kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration + 25).ToString());
            }else if((turbineDataModel.DeaeratorOutletTemp> 0 || turbineDataModel.PST > 0) && AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow != 0)
            {
                kreislDATHandler.ProcessMassFlow(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
            }

            if (turbineDataModel.DumpCondensor)
            {
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity > 0)
                {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration + 25).ToString());
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        else if (input[1].PowerGeneration == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure.ToString());
            kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamTemp.ToString());

            if (turbineDataModel.DumpCondensor)
            {
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity > 0)
                {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow).ToString(),-1);
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");

        }
        else if (input[1].ExhaustPressure == 0)
        {
            kreislDATHandler.FillMassFlow(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass.ToString());
            kreislDATHandler.FillInletPressure(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure.ToString());
            kreislDATHandler.FillExhaustPressure(filePath, 4, "0.000");
            kreislDATHandler.FillExhaustPressure(filePath, 4, "4.59", -1);
            kreislDATHandler.FillInletTemperature(filePath, 5, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamTemp.ToString());
            kreislDATHandler.FillVariablePower(filePath, 6, (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration + 25).ToString());
            if (turbineDataModel.DumpCondensor)
            {
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity > 0)
                {
                    turbineDataModel.CheckForCapacity = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity;
                    kreislDATHandler.fillCapacity(filePath, 9, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity.ToString());
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, 0.ToString());
                    kreislDATHandler.FillMassFlow(filePath, 5, (10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow).ToString(), -1);
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == true)
                {
                    kreislDATHandler.ProcessMassFlow(filePath, 16, AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow.ToString());
                }
                else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].Capacity == 0 && kreislDATHandler.checkIfDumpcondensorON(1, AdditionalLoadPoint.GetInstance().CustomerLoadPoints) == false)
                {
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    kreislDATHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    kreislDATHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
            }
            MainTemp += File.ReadAllText("C:\\testDir\\KREISL.DAT");
        }
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (pst == 0)
            {
                turbineDataModel.PST = 0;
            }
        }
        int count = 11;
        
        for(int i = 2; i< input.Count; i++)
        {
             press = input[i].SteamPressure;
             Temp = input[i].SteamTemp;
             mass = input[i].SteamMass;
             exPres = input[i].ExhaustPressure;
             Power = input[i].PowerGeneration;
             exMass = input[i].ExhaustMassFlow;
            if(turbineDataModel.DeaeratorOutletTemp ==  0 && turbineDataModel.PST == 0)
            {
                if (mass == 0 && exMass > 0)
                {
                    input[i].SteamMass = 0.055 + input[i].ExhaustMassFlow;
                }
                else if (mass > 0 && exMass == 0)
                {
                    input[i].ExhaustMassFlow = input[i].SteamMass - 0.055;
                }
            }
            

            if (input[i].SteamPressure == 0)
            {
                fillLP(i,"Pr",count);
            }
            else if (input[i].SteamTemp == 0)
            {
                fillLP(i,"T",count);
            }
            else if (input[i].SteamMass == 0)
            {
                fillLP(i,"M",count);
            }
            else if (input[i].PowerGeneration == 0)
            {
                fillLP(i,"P",count);
            }else if (input[i].ExhaustPressure == 0)
            {
                fillLP(i, "E", count);
            }
            count++;
        }
        File.WriteAllText("C:\\testDir\\KREISL.DAT", MainTemp);

        KreislIntegration kreislIntegration = new KreislIntegration();

        kreislIntegration.LaunchKreisL();

        if(turbineDataModel.DeaeratorOutletTemp>0 ||  turbineDataModel.PST > 0)
        {
            // add dearator check here or pst  so below cal pst
            checkNowTemplate();
            kreislIntegration.LaunchKreisL();
        }


    }
    public void checkNowTemplate()
    {
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        double exhaustTemp = 0;
        for (int i = 1; i < AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count; i++)
        {
            if(turbineDataModel.DeaeratorOutletTemp> 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    exhaustTemp = kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, 1, i);
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    exhaustTemp = kreislERGHandlerService.ExtractTempForClosedCycle(StartKreisl.ergFilePath, 1, i);
                }
            }
            else
            {
                 exhaustTemp = kreislERGHandlerService.ExtractTempForDesuparator(StartKreisl.ergFilePath, 5, i);

            }
            if (exhaustTemp > turbineDataModel.PST)
            {
                if(turbineDataModel.DeaeratorOutletTemp > 0)
                {
                    if (turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, i);
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, i);
                        kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, i);
                    }
                    else if (!turbineDataModel.DumpCondensor)
                    {
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, i);
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, i);
                        kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, i);
                    }
                }
                else
                {
                    kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, i);
                    kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, i);
                }
               
            }
        }
    }
    
    public void RunKriesl5()
    {
        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
        TurbaConfig turbaConfig = new TurbaConfig();
        turbaConfig.LaunchTurba();
        kreislIntegration.RenameTurbaCON("C:\\testDir\\Turman250\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
    }
    public void FillInputDat()
    {
        string filePath = "C:\\testDir\\kreisl.dat";
        
        KreislDATHandler datHandler = new KreislDATHandler();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor == true)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, turbineDataModel.ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                datHandler.FillInletPressure(filePath, 18, turbineDataModel.InletPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, turbineDataModel.InletTemperature.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
                datHandler.ProcessMassFlow(filePath, 9, turbineDataModel.OutletMassFlow.ToString());
                if (turbineDataModel.Capacity == 0 && StartKreisl.checkIfDumpcondensorON() == false)
                {
                    datHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    datHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                //datHandler.fillCapacity(filePath, 9, turbineDataModel.Capacity.ToString());
                if (turbineDataModel.MassFlowRate > 0)
                {
                    datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                }
                else if (turbineDataModel.AK25 > 0)
                {
                    datHandler.FillVariablePower(filePath, 19, turbineDataModel.AK25.ToString());
                }
            }
            else if (turbineDataModel.DumpCondensor == false)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, turbineDataModel.ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, turbineDataModel.MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, turbineDataModel.CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", turbineDataModel.ProcessCondReturn.ToString());
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                datHandler.FillInletPressure(filePath, 18, turbineDataModel.InletPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, turbineDataModel.InletTemperature.ToString());
                datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, turbineDataModel.DeaeratorOutletTemp.ToString());
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            if (turbineDataModel.PST < 120)
            {
                turbineDataModel.PST -= 10;
            }
            datHandler.fillProcessSteamTemperatur(filePath, 3, turbineDataModel.PST.ToString());
            datHandler.FillPressureDesh(filePath, 8, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.FillMassFlow(filePath, 9, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 2, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 6, turbineDataModel.InletTemperature.ToString());
        }
        else
        {
            datHandler.FillMassFlow(filePath, 5, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 5, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 4, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
        }
    }

    public void fillInputDatFileForParLoad(int i , List<CustomerLoadPoint> customerLoadPoints)
    {
        string filePath = "C:\\testDir\\kreisl.dat";
        turbineDataModel.InletPressure = customerLoadPoints[i].SteamPressure;
        turbineDataModel.InletTemperature = customerLoadPoints[i].SteamTemp;
        turbineDataModel.ExhaustPressure = customerLoadPoints[i].ExhaustPressure;
        turbineDataModel.MassFlowRate = customerLoadPoints[i].SteamMass;
        turbineDataModel.OutletMassFlow = customerLoadPoints[i].ExhaustMassFlow;
        
        KreislDATHandler datHandler = new KreislDATHandler();
        if (turbineDataModel.DeaeratorOutletTemp > 0)
        {
            if (turbineDataModel.DumpCondensor == true)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * customerLoadPoints[i].SteamPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, customerLoadPoints[i].ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, customerLoadPoints[i].MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, customerLoadPoints[i].CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", customerLoadPoints[i].ProcessCondReturn.ToString());
                turbineDataModel.PST = customerLoadPoints[i].PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, customerLoadPoints[i].PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : turbineDataModel.PST.ToString());
                datHandler.FillInletPressure(filePath, 18, customerLoadPoints[i].SteamPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, customerLoadPoints[i].SteamTemp.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, customerLoadPoints[i].DeaeratorOutletTemp.ToString());
                }
                datHandler.ProcessMassFlow(filePath, 9, customerLoadPoints[i].ExhaustMassFlow.ToString());
                if (turbineDataModel.Capacity == 0 && StartKreisl.checkIfDumpcondensorON() == false)
                {
                    datHandler.TurnOffCondensor(filePath, 3, 9, 15, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 5, 14, 13, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 2, 18, 19, 1, -21);
                    datHandler.TurnOffCondensor(filePath, 3, 18, 19, 1, -20);
                    datHandler.TurnOffCondensor(filePath, 2, 19, 4, 1, -20);
                }
                //datHandler.fillCapacity(filePath, 9, customerLoadPoints[i].Capacity.ToString());
                if (turbineDataModel.MassFlowRate > 0)
                {
                    datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                }
                else if (turbineDataModel.AK25 > 0)
                {
                    datHandler.FillVariablePower(filePath, 19, turbineDataModel.AK25.ToString());
                }
            }
            else if (turbineDataModel.DumpCondensor == false)
            {
                datHandler.FillPressureDesh(filePath, 4, (1.2 * turbineDataModel.InletPressure).ToString());
                datHandler.FillExhaustPressure(filePath, 7, turbineDataModel.ExhaustPressure.ToString());
                datHandler.MakeUpTemperature(filePath, 9, customerLoadPoints[i].MakeUpTempe.ToString());
                datHandler.Processcondensatetemperature(filePath, 12, customerLoadPoints[i].CondRetTemp.ToString());
                datHandler.FillCondensateReturn(filePath, "14", customerLoadPoints[i].ProcessCondReturn.ToString());
                turbineDataModel.PST = customerLoadPoints[i].PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                datHandler.fillProcessSteamTemperatur(filePath, 16, customerLoadPoints[i].PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5).ToString() : customerLoadPoints[i].PST.ToString());
                datHandler.FillInletPressure(filePath, 18, turbineDataModel.InletPressure.ToString());
                datHandler.FillInletTemperature(filePath, 18, turbineDataModel.InletTemperature.ToString());
                datHandler.FillMassFlow(filePath, 19, turbineDataModel.MassFlowRate.ToString());
                if (turbineDataModel.IsPRVTemplate)
                {
                    datHandler.fillPsatvont_t(filePath, 13, customerLoadPoints[i].DeaeratorOutletTemp.ToString());
                }
            }
        }
        else if (turbineDataModel.PST > 0)
        {
            if (turbineDataModel.PST < 120)
            {
                turbineDataModel.PST -= 10;
            }
            datHandler.fillProcessSteamTemperatur(filePath, 3, customerLoadPoints[i].PST.ToString());
            datHandler.FillPressureDesh(filePath, 8, (1.2 * turbineDataModel.InletPressure).ToString());
            datHandler.FillMassFlow(filePath, 9, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 6, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 2, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 6, turbineDataModel.InletTemperature.ToString());
        }
        else
        {
            datHandler.FillMassFlow(filePath, 5, turbineDataModel.MassFlowRate.ToString());
            datHandler.FillInletPressure(filePath, 5, turbineDataModel.InletPressure.ToString());
            datHandler.FillExhaustPressure(filePath, 4, turbineDataModel.ExhaustPressure.ToString());
            datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
        }
    }
    public void checkingPartLoadExist(List<CustomerLoadPoint> customerLoadPoints)
    {
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        KreislIntegration kreislIntegration = new KreislIntegration();
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
        bool containsPartLoad = false;
        for(int i = 0; i < customerLoadPoints.Count; i++)
        {
            double partLoad = customerLoadPoints[i].PartLoad;
            if(partLoad > 0)
            {
                containsPartLoad = true;
                break;
            }
        }
        if (containsPartLoad)
        {
            double power = 0;
            for (int i = 0; i < customerLoadPoints.Count; i++)
            {
                if (customerLoadPoints[i].SteamPressure > 0 && customerLoadPoints[i].SteamTemp > 0 && customerLoadPoints[i].SteamMass > 0 && customerLoadPoints[i].ExhaustPressure > 0 && customerLoadPoints[i].PartLoad == 0)
                {
                    turbineDataModel.ExhaustPressure = customerLoadPoints[i].ExhaustPressure;
                    kreislDATHandler.RefreshKreislDAT();
                    fillInputDatFileForParLoad(i, customerLoadPoints);
                    kreislIntegration.RenameTurbaCON();
                    kreislIntegration.LaunchKreisL();
                    power = Math.Max(power,kreislERGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath));
                }else
                {
                    power = Math.Max(power, customerLoadPoints[i].PowerGeneration);
                }
            }
            for (int i = 0; i < customerLoadPoints.Count; i++)
            {
                double partLoad = customerLoadPoints[i].PartLoad;
                if (partLoad > 0)
                {
                    customerLoadPoints[i].PowerGeneration = (power * partLoad) / 100;
                }
            }
        }
    }
    public void CorrectLP1unknowParams()
    {
        KreislDATHandler datHandler = new KreislDATHandler();
        HBDsetDefaultCustomerParamas();
        cxLP_GenerateLoadPoints();
        ReferenceDATSelector(1);
        cxLP_prepareDATFile(1);
        LaunchTurba(2);
        //ergResultsCheck(1);
        double wheelP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
        if (wheelP > 0)
            datHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", wheelP.ToString());
        int lpNo = additionalLoadPoint.CustomerLoadPoints[1].LPNumber;
        int index = lpNumberToIndexMap[lpNo];
        MainTemp = "";
        fillAGainDat(index, initList);
        File.WriteAllText("C:\\testDir\\KREISL.DAT", MainTemp);

        //KreislIntegration kreislIntegration = new KreislIntegration();

        if (turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0)
        {
            UpdateDesupratorWithTurba(1);
        }
        datHandler.FillVari40();
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
        datHandler.FillTurbineEff(StartKreisl.filePath, "4", Convert.ToString(turbaOutputModel.OutputDataList[1].Efficiency));
        datHandler.FillTurbineEff(StartKreisl.filePath, "1", Convert.ToString(turbaOutputModel.OutputDataList[1].Efficiency));
        KreislIntegration kreislIntegration = new KreislIntegration();
        TurbaConfig turbaConfig = new TurbaConfig();
        turbaConfig.LaunchTurba(2);
        File.Move("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON", true);
        kreislIntegration.LaunchKreisL();
        turbaConfig.LaunchTurba(2);
        File.Move("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON", true);
        kreislIntegration.LaunchKreisL();
        turbaConfig.LaunchTurba(2);
        File.Move("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON", true);
        kreislIntegration.LaunchKreisL();
        turbaConfig.LaunchTurba(2);
        double press = initList[index].SteamPressure;
        double Temp = initList[index].SteamTemp;
        double mass = initList[index].SteamMass;
        double exPres = initList[index].ExhaustPressure;
        double Power = initList[index].PowerGeneration;
        double exMass = initList[index].ExhaustMassFlow;
            if (initList[index].SteamPressure == 0)
            {
                additionalLoadPoint.CustomerLoadPoints[1].SteamPressure = kreislERGHandlerService.ExtractPressure(StartKreisl.ergFilePath, 1, 4);
                turbineDataModel.InletPressure = additionalLoadPoint.CustomerLoadPoints[1].SteamPressure;
            }
            else if (initList[index].SteamTemp == 0)
            {
               additionalLoadPoint.CustomerLoadPoints[1].SteamTemp = kreislERGHandlerService.ExtractTemperature(StartKreisl.ergFilePath, 1, 4);
               turbineDataModel.InletTemperature =  additionalLoadPoint.CustomerLoadPoints[1].SteamTemp;
            }
            else if (initList[index].SteamMass == 0)
            {
                additionalLoadPoint.CustomerLoadPoints[1].SteamMass = kreislERGHandlerService.ExtractMassFlow(StartKreisl.ergFilePath, 1, 4);
                turbineDataModel.MassFlowRate =  additionalLoadPoint.CustomerLoadPoints[1].SteamMass;
            }
            else if (initList[index].PowerGeneration == 0)
            {
                //input[i].PowerGeneration = kreislERGHandlerService.ExtractGeneratorPower(ergPath, i);
            }
            else if (initList[index].ExhaustPressure == 0)
            {
               additionalLoadPoint.CustomerLoadPoints[1].ExhaustPressure = kreislERGHandlerService.ExtractBackPressure(StartKreisl.ergFilePath, 1, 5);
               turbineDataModel.ExhaustPressure = additionalLoadPoint.CustomerLoadPoints[1].ExhaustPressure;
            }
            ClearFile();
            turbineDataModel.AK25 = 0;
    }

    public void ClearFile()
    {
        StartKreisl.DeleteCONFiles();
    }
    //public void CorrectingLP1()
    //{

    //}
    public void cxLP_mainKreisl(List<CustomerLoadPoint> customerLPList)
    {
        try
        {

            if (StartKreisl.GlobalHost == null)
            {

                StartKreisl.FillGlobalHost();
            }
            checkingPartLoadExist(customerLPList);

            initList = new List<CustomerLoadPoint>();
            lpNumberToIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < customerLPList.Count; i++)
            {
                var original = customerLPList[i];
                initList.Add(new CustomerLoadPoint
                {
                    LPNumber = original.LPNumber,

                    SteamPressure = original.SteamPressure,
                    SteamTemp = original.SteamTemp,
                    SteamMass = original.SteamMass,
                    ExhaustPressure = original.ExhaustPressure,
                    PowerGeneration = original.PowerGeneration,
                    ExhaustMassFlow = original.ExhaustMassFlow,
                    VolFlow = original.VolFlow,
                    PST = original.PST,
                    MakeUpTempe = original.MakeUpTempe,
                    CondRetTemp = original.CondRetTemp,
                    ProcessCondReturn = original.ProcessCondReturn,
                    DeaeratorOutletTemp = original.DeaeratorOutletTemp,
                    Capacity = original.Capacity

                });
                lpNumberToIndexMap[original.LPNumber] = i;
            }


            string ergPath = @"C:\testDir\KREISL.ERG";
            Logger("Starting Ignite-X project");
            if (StartKreisl.GlobalHost == null)
            {

                StartKreisl.FillGlobalHost();
            }
            StartKreisl.DeleteCONFiles();

            fillCustomerLoadPointList(customerLPList);

            KreislDATHandler kreislDATHandler = new KreislDATHandler();
            KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
            turbineDataModel.ExhaustPressure = customerLPList[0].ExhaustPressure;
            kreislDATHandler.RefreshKreislDAT();

            cxLP_GetLPcount();

            fillLPINDat();

            List<CustomerLoadPoint> input = AdditionalLoadPoint.GetInstance().CustomerLoadPoints;
            for (int i = 1; i < input.Count; i++)
            {
                double press = input[i].SteamPressure;
                double Temp = input[i].SteamTemp;
                double mass = input[i].SteamMass;
                double exPres = input[i].ExhaustPressure;
                double Power = input[i].PowerGeneration;
                double exMass = input[i].ExhaustMassFlow;
                if (input[i].SteamPressure == 0)
                {
                    input[i].SteamPressure = kreislERGHandlerService.ExtractPressure(ergPath, i, 4);
                }
                else if (input[i].SteamTemp == 0)
                {
                    input[i].SteamTemp = kreislERGHandlerService.ExtractTemperature(ergPath, i, 4);
                }
                else if (input[i].SteamMass == 0)
                {
                    input[i].SteamMass = kreislERGHandlerService.ExtractMassFlow(ergPath, i, 4);
                }
                else if (input[i].PowerGeneration == 0)
                {
                    //input[i].PowerGeneration = kreislERGHandlerService.ExtractGeneratorPower(ergPath, i);
                }
                else if (input[i].ExhaustPressure == 0)
                {
                    input[i].ExhaustPressure = kreislERGHandlerService.ExtractBackPressure(ergPath, i, 5);
                }
                input[i].VolFlow = kreislERGHandlerService.ExtractVolFlowForLoadPoint(ergPath, i, 4);
            }
            AdditionalLoadPoint.GetInstance().SortCustomerLoadPointsByVol();



            Console.WriteLine("Checking");
            Console.WriteLine(AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count());
            turbineDataModel.InletPressure = Math.Round(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamPressure, 3);
            turbineDataModel.InletTemperature = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamTemp;
            turbineDataModel.ExhaustPressure = Math.Round(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure, 3);
            turbineDataModel.MassFlowRate = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass;
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(turbineDataModel.ExhaustPressure) + 5) : turbineDataModel.PST;
                if (turbineDataModel.DumpCondensor)
                {
                    turbineDataModel.OutletMassFlow = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustMassFlow;
                    if (turbineDataModel.Capacity > 0)
                    {
                        turbineDataModel.CheckForCapacity = turbineDataModel.Capacity;
                        input.Add(
                        new CustomerLoadPoint
                        {
                            LPNumber = customerLPList.Count + 1,
                            SteamPressure = turbineDataModel.InletPressure,
                            SteamTemp = turbineDataModel.InletTemperature,
                            ExhaustPressure = turbineDataModel.ExhaustPressure,
                        }
                        );
                        input[customerLPList.Count + 1].Capacity = turbineDataModel.Capacity;
                        input[customerLPList.Count + 1].PST = turbineDataModel.PST;
                        input[customerLPList.Count + 1].DeaeratorOutletTemp = turbineDataModel.DeaeratorOutletTemp;
                        input[customerLPList.Count + 1].MakeUpTempe = turbineDataModel.MakeUpTempe;
                        input[customerLPList.Count + 1].ProcessCondReturn = turbineDataModel.ProcessCondReturn;
                        input[customerLPList.Count + 1].CondRetTemp = turbineDataModel.CondRetTemp;
                        input[customerLPList.Count + 1].LoadPoint = "Dump Condenser with Capacity";
                        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
                        {
                            input[customerLPList.Count + 1].PowerGeneration = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration;
                        }
                        else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass != 0)
                        {
                            input[customerLPList.Count + 1].SteamMass = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass;
                        }
                        initList.Add(
                        new CustomerLoadPoint
                        {
                            LPNumber = customerLPList.Count + 1,
                            SteamPressure = turbineDataModel.InletPressure,
                            SteamTemp = turbineDataModel.InletTemperature,
                            ExhaustPressure = turbineDataModel.ExhaustPressure,
                        }
                        );
                        initList[customerLPList.Count].Capacity = turbineDataModel.Capacity;
                        initList[customerLPList.Count].PST = turbineDataModel.PST;
                        initList[customerLPList.Count].DeaeratorOutletTemp = turbineDataModel.DeaeratorOutletTemp;
                        initList[customerLPList.Count].MakeUpTempe = turbineDataModel.MakeUpTempe;
                        initList[customerLPList.Count].ProcessCondReturn = turbineDataModel.ProcessCondReturn;
                        initList[customerLPList.Count].CondRetTemp = turbineDataModel.CondRetTemp;
                        initList[customerLPList.Count].LoadPoint = "Dump Condenser with Capacity";
                        if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration != 0)
                        {
                            initList[customerLPList.Count].PowerGeneration = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PowerGeneration;
                        }
                        else if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass != 0)
                        {
                            initList[customerLPList.Count].SteamMass = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].SteamMass;
                        }
                        lpNumberToIndexMap[customerLPList.Count + 1] = customerLPList.Count;
                        turbineDataModel.Capacity = 0;
                        cxLP_RngStop += 1;
                    }
                }
            }
            fillLoadPointList();

            kreislDATHandler.RefreshKreislDAT();
            FillInputDat();
            if (customerLPList.Count == 1)
            {
                CorrectLP1unknowParams();
            }
            kreislDATHandler.RefreshKreislDAT();
            FillInputDat();
            //CorrectingLP1();
            //---> Starting Standard Flow Path
            HBDsetDefaultCustomerParamas();
            HBDupdateEff_Generator_Init();
            HBDPersistInitialPower();
            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            Logger("Selecting DAT file..");
            ReferenceDATSelector((int)cxLP_RngStop + 10);
            if (TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                return;
            }
            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            cxLP_GenerateLoadPoints("Recal");
            Logger("Generating standard internal load points..");
            GenerateLoadPoints();
            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            Logger("Start preparing DAT file...");

            //FillExtraLoadPoints((int)cxLP_RngStop + 10);
            KreislIntegration kreislIntegration = new KreislIntegration();
            //kreislIntegration.LaunchKreisL();
            prepareDATFile((int)cxLP_RngStop + 10);


            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            //datFileInitParamsExceptLP(cxLP_GetMaxPower());
            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            LaunchTurba((int)cxLP_RngStop + 10);

            if (TurbineDesignPage.cts.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            NozzleOptimizer.nozzleOptimizeCount = 0;
            ERGVerification.isCheckingLP5 = false;
            ergResultsCheck((int)cxLP_RngStop + 10);
            ResetNozzleCounter();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            UpdateLP5();
            ERGVerification.isCheckingLP5 = true;
            ergResultsCheck((int)cxLP_RngStop + 10);
            ResetNozzleCounter();
            Logger("---------------------------");
            Logger("Checking valve point.....");
            ValvePointOptimize((int)cxLP_RngStop + 10);
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            if (!File.Exists("C:\\testDir\\TURBA.CON"))
                kreislIntegration.RenameTurbaCON("C:\\testDir\\TURBATURBAE1.DAT.CON", "C:\\testDir\\TURBA.CON");
            kreislDATHandler.FillVari40();
            MainTemp = "";
            RemoveErg();
            kreislDATHandler.RefreshKreislDAT();
            double wheelChamberPressure = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;
            kreislDATHandler.FillWheelChamberPressure(StartKreisl.filePath, "1 0", Convert.ToString(wheelChamberPressure));
            //KreislDATHandler kreislDATHandler = new KreislDATHandler();
            int count = 11;
            for (int i = 1; i < additionalLoadPoint.CustomerLoadPoints.Count; i++)
            {
                if (i == 1)
                {
                    int lpNo = additionalLoadPoint.CustomerLoadPoints[i].LPNumber;
                    int index = lpNumberToIndexMap[lpNo];
                    fillAGainDat(index, initList);
                }
                else
                {

                    int lpNo = additionalLoadPoint.CustomerLoadPoints[i].LPNumber;
                    int index = lpNumberToIndexMap[lpNo];
                    double press = initList[index].SteamPressure;
                    double Temp = initList[index].SteamTemp;
                    double mass = initList[index].SteamMass;
                    double exPres = initList[index].ExhaustPressure;
                    double Power = initList[index].PowerGeneration;
                    double exMass = initList[index].ExhaustMassFlow;
                    if (turbineDataModel.DeaeratorOutletTemp == 0 && turbineDataModel.PST == 0)
                    {
                        if (mass == 0 && exMass > 0)
                        {
                            initList[index].SteamMass = 0.055 + initList[index].ExhaustMassFlow;
                        }
                        else if (mass > 0 && exMass == 0)
                        {
                            initList[index].ExhaustMassFlow = initList[index].SteamMass - 0.055;
                        }
                    }


                    if (initList[index].SteamPressure == 0)
                    {
                        fillLPAgain(index, "Pr", count, initList);
                    }
                    else if (initList[index].SteamTemp == 0)
                    {
                        fillLPAgain(index, "T", count, initList);
                    }
                    else if (initList[index].SteamMass == 0)
                    {
                        fillLPAgain(index, "M", count, initList);
                    }
                    else if (initList[index].PowerGeneration == 0)
                    {
                        fillLPAgain(index, "P", count, initList);
                    }
                    else if (initList[index].ExhaustPressure == 0)
                    {
                        fillLPAgain(index, "E", count, initList);
                    }
                    count++;
                }
            }
            File.WriteAllText("C:\\testDir\\KREISL.DAT", MainTemp);

            //KreislIntegration kreislIntegration = new KreislIntegration();
            if (TurbineDesignPage.cts.IsCancellationRequested || TurbineDesignPage.finalToken.IsCancellationRequested)
            {
                logger.moveLogs();
                return;
            }
            if (turbineDataModel.DeaeratorOutletTemp > 0 || turbineDataModel.PST > 0)
            {
                UpdateDesupratorWithTurba(10 + AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count - 2);
            }
            kreislIntegration.LaunchKreisL();
            CheckPower(10 + (int)cxLP_RngStop);
        }
        catch(Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    public void RemoveErg()
    {
        string file = @"C:\testDir\KREISL.ERG";
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    public void UpdateDesupratorWithTurba(int loadPoint)
    {
        string[] lines = File.ReadAllLines(@"C:\testDir\TURBATURBAE1.DAT.ERG");
        KreislDATHandler kreislDATHandler = new KreislDATHandler();
        //string[] lines = File.ReadAllLines("yourfile.erg");
        List<double[]> pressuresList = new List<double[]>();
        List<double[]> tempsList = new List<double[]>();
        List<double[]> enthList = new List<double[]>();
        List<double[]> massList = new List<double[]>();
        int count = 1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("DRUECKE - bar - pressures"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    pressuresList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("TEMPERATUREN - grd C - temperatures"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    tempsList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("ENTHALPIEN - kJ/kg - enthalpies"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    enthList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
            if (lines[i].Contains("DAMPFMENGEN - kg/s - mass flow"))
            {
                i += 2;
                count = 1;
                for (int j = i; count <= loadPoint; j++)
                {
                    massList.Add(Array.ConvertAll(lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), Double.Parse));
                    count++;
                }
            }
        }

        double[,] pressures = new double[3, loadPoint];
        double[,] temps = new double[3, loadPoint];
        double[,] enthalpies = new double[3, loadPoint];
        double[,] massFlows = new double[3, loadPoint];

        for (int col = 0; col < loadPoint; col++)
        {
            pressures[0, col] = pressuresList[col][0]; // Inlet, Point col+1
            pressures[1, col] = pressuresList[col][1]; // Wheel
            pressures[2, col] = pressuresList[col][2]; // Exhaust

            temps[0, col] = tempsList[col][0];
            temps[1, col] = tempsList[col][1];
            temps[2, col] = tempsList[col][2];

            enthalpies[0, col] = enthList[col][0];
            enthalpies[1, col] = enthList[col][1];
            enthalpies[2, col] = enthList[col][2];

            massFlows[0, col] = massList[col][0];
            massFlows[1, col] = massList[col][1];
            massFlows[2, col] = massList[col][2];
        }
        double exhaustTemp = temps[2, 0];
        //double pst = AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PST;
        turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].ExhaustPressure) + 5) : turbineDataModel.PST;
        if (exhaustTemp > turbineDataModel.PST)
        {
            if (turbineDataModel.DeaeratorOutletTemp > 0)
            {
                if (turbineDataModel.DumpCondensor)
                {
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, 1);
                }
                else if (!turbineDataModel.DumpCondensor)
                {
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, 1);
                    kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, 1);

                }
            }
            else
            {
                kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, 1);
                kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, 1);
            }
            //kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath,1);
            //kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath,1);
        }
        if(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[1].PST == 0)
        {
            turbineDataModel.PST = 0;
        }
        count = 2;
        if(loadPoint - 10 >= 1)
        {
            int tt = loadPoint;
            for (int i = 10; i < tt; i++)
            {
                exhaustTemp = temps[2, i];
                turbineDataModel.PST = turbineDataModel.PST == 0 ? (thermodynamicService.tsatvonp(AdditionalLoadPoint.GetInstance().CustomerLoadPoints[count].ExhaustPressure) + 5) : turbineDataModel.PST;
                if (exhaustTemp > turbineDataModel.PST)
                {
                    if (turbineDataModel.DeaeratorOutletTemp > 0)
                    {
                        if (turbineDataModel.DumpCondensor)
                        {
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 3, 8, 15, count);
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 5, 15, 13, count);
                            kreislDATHandler.UpdateDesupratorClosedPRVDumpCondensor(StartKreisl.filePath, 4, 17, 4, count);
                        }
                        else if (!turbineDataModel.DumpCondensor)
                        {
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 5, 15, 13, count);
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 4, 16, 4, count);
                            kreislDATHandler.UpdateDesupratorClosedPRV(StartKreisl.filePath, 3, 17, 15, count);

                        }
                    }
                    else
                    {
                        kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, count);
                        kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, count);
                    }
                    //kreislDATHandler.UpdateDesupratorFirst(StartKreisl.filePath, count);
                    //kreislDATHandler.UpdateDesupratorSecond(StartKreisl.filePath, count);
                    
                }
                if (AdditionalLoadPoint.GetInstance().CustomerLoadPoints[count].PST == 0)
                {
                    turbineDataModel.PST = 0;
                }
                count++;
            }
        }
    }
    public static void CommonFunction()
    {

    }
    public void ResetNozzleCounter()
    {
        NozzleOptimizer.Na = 0;
        NozzleOptimizer.nozzleOptimizeCount = 0;
        NozzleOptimizer.Nb = 0;
        NozzleOptimizer.A1 = 0;
        NozzleOptimizer.A2 = 0;
    }
    public void UpdateLP5()
    {
        double temp = TurbineDataModel.getInstance().InletTemperature - thermodynamicService.tsatvonp(TurbineDataModel.getInstance().InletPressure);
        if (temp >= 110)
        {
            temp = 60;
        }
        else
        {
            temp += 50;
        }
        LoadPointDataModel lpDataModel = LoadPointDataModel.getInstance();
        List<LoadPoint> lpList = lpDataModel.LoadPoints;
        lpList[5].Pressure = TurbineDataModel.getInstance().InletPressure;
        lpList[5].Temp = TurbineDataModel.getInstance().InletTemperature - temp;
        lpList[5].MassFlow = TurbineDataModel.getInstance().MassFlowRate;
        lpList[5].BackPress = 0.5 * TurbineDataModel.getInstance().ExhaustPressure;
        lpList[5].Rpm = 12000;
        lpList[5].InFlow = 0;
        lpList[5].BYP = -1;
        lpList[5].EIN = 0;
        lpList[5].WANZ = 0;
        lpList[5].RSMIN = 0;
    }
    public void cxLP_Init(ExcelPackage package)
    {
        initConfig();
    }
 
    public string cxLP_validateLPs()
    {
        int knownValuesCount;
        bool isLPValid=false;
        bool isAll_LPhasPower = true;
        bool isAll_LPhasFlow = true;
 
        long lastRow = cxLP_RngStop;
        for (int i = 1; i <= lastRow; i++)
        {
            knownValuesCount = 0;
            isLPValid = true;
 
            if (additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration <= 0)
            {
                isAll_LPhasPower = false;
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].SteamMass <= 0)
            {
                isAll_LPhasFlow = false;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].SteamPressure>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].SteamTemp>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].SteamMass>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].ExhaustPressure>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].ExhaustMassFlow > 0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].PartLoad>0){
                knownValuesCount++;
            }
            if(additionalLoadPoint.CustomerLoadPoints[i].VolFlow>0){
                knownValuesCount++;
            }
            if (knownValuesCount <= 3)
            {
                isLPValid = false;
                Logger("Custom Load point invalid @LP:" + (i));
                TerminateIgniteX("cxLP_validateLPs");
            }
        }
 
        if (isAll_LPhasPower)
        {
            Logger("All LPs has output power value..");
            return "Power";
        }
        else if (isAll_LPhasFlow)
        {
            Logger("All LPs has output Flow value..");
            return "Flow";
        }
        else if (isLPValid)
        {
            Logger("All LPs are valid and has mix of power and flow");
            return "Hybrid";
        }
        else
        {
            Logger("Invalid Load points data, can't proceed further");
            return "Error";
        }
    }
 
    public void cxLP_recalcUnknownParams()
    {
        Logger("Copying Turba erg efficiency For all LPs..");
        cxLP_getEffERG();
    }
 
    public void cxLP_prepareDATFile(int maxLP = 0)
    {
        loadLP1fromDAT();
        DeleteRowAfterFirstLoadPoint();
        InsertDataLineUnderFirstLPFixed();
        DeleteLoadPoints();
        InsertLoadPointsWithExactFormattingUsingMid(maxLP);
        InsertDataLineUnderND(maxLP);
        Logger("Load points written into DAT file...");
        datFileInitParamsExceptLP(cxLP_GetMaxPower());
        Logger("Updated the DAT file...");
    }
 
    public void FillKreislDAT()
    {
        KreislDATHandler datHandler = new KreislDATHandler();
        string filePath = StartKreisl.filePath;
        double P1 = turbineDataModel.InletPressure;
        double P2 = turbineDataModel.ExhaustPressure;
        double T1 = turbineDataModel.InletTemperature;
        double massFlow = turbineDataModel.MassFlowRate;
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());

    }
    public double cxLP_GetMaxPower()
    {
        double power = -1;
        for(int i = 0; i < additionalLoadPoint.CustomerLoadPoints.Count; ++i){
            power = double.Max(power, additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration);
        }
        return power;
    }
 
    //public double cxLP_getRefProjectEff(string Power_or_Mass)
    //{
    //    List<PowerNearest> powerNearestList = turbineDataModel.ListPower;
    //    for(int i = 0; i <= Math.Max(3, cxLP_LPcount); ++i)
    //    {
    //        powerNearestList.Add(new PowerNearest());
    //    }
    //    powerNearestList[0].KNearest = "2";
    //    powerNearestList[0].Power = additionalLoadPoint.CustomerLoadPoints[1].PowerGeneration;
    //    powerNearestList[0].ExhaustPressure = additionalLoadPoint.CustomerLoadPoints[1].ExhaustPressure;
    //    powerNearestList[0].SteamPressure = additionalLoadPoint.CustomerLoadPoints[1].SteamPressure;
    //    powerNearestList[0].SteamTemperature = additionalLoadPoint.CustomerLoadPoints[1].SteamTemp;
    //    powerNearestList[0].SteamMass = additionalLoadPoint.CustomerLoadPoints[1].SteamMass;
        
    //    cxLP_UpdateCustomParamsForkNN();
    //    HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
    //    if (StartKreisl.kreislKey)
    //    {
    //        hBDPowerCalculator.GetTurbaCON(powerNearestList[1].ProjectID);
    //    }
    //    return Convert.ToDouble(powerNearestList[1].Efficiency);
    //}
 
    public void cxLP_getEffERG()
    {
        for(int i = 1;i <= cxLP_RngStop; i++){
            additionalLoadPoint.CustomerLoadPoints[i].EffFromTurba = turbaOutputModel.OutputDataList[i].Efficiency;
        }
       
        string efficiencyList = "";
        for(int i = 1; i <= cxLP_RngStop; ++i){
            efficiencyList += additionalLoadPoint.CustomerLoadPoints[i].EffFromTurba+"-";
        }
        Logger("Efficiency from Turba for customer LPs -");
        Logger(efficiencyList);
    }
 
    public void cxLP_GenerateLoadPoints(string runMode = "")
    {
        loadPointDataModel.fillLoadPoints();
 
        loadPointDataModel.LoadPoints[0].MassFlow = extraLP[0].MassFlow;
        loadPointDataModel.LoadPoints[0].Rpm = extraLP[0].Rpm;
        loadPointDataModel.LoadPoints[0].Pressure = extraLP[0].Pressure;
        loadPointDataModel.LoadPoints[0].Temp = extraLP[0].Temp;
        loadPointDataModel.LoadPoints[0].EIN = extraLP[0].EIN;
        loadPointDataModel.LoadPoints[0].BackPress = extraLP[0].BackPress;
        loadPointDataModel.LoadPoints[0].BYP=extraLP[0].BYP;
        loadPointDataModel.LoadPoints[0].EIN=extraLP[0].EIN;
        loadPointDataModel.LoadPoints[0].WANZ=extraLP[0].WANZ;
        loadPointDataModel.LoadPoints[0].RSMIN=extraLP[0].RSMIN;
 
        if (string.IsNullOrEmpty(runMode))
        {
            for(int i = 1; i <= cxLP_RngStop; i++)
            {
                loadPointDataModel.LoadPoints[i].MassFlow = extraLP[i].MassFlow;
                loadPointDataModel.LoadPoints[i].Rpm = extraLP[i].Rpm;
                loadPointDataModel.LoadPoints[i].Pressure = extraLP[i].Pressure;
                loadPointDataModel.LoadPoints[i].Temp = extraLP[i].Temp;
                loadPointDataModel.LoadPoints[i].EIN = extraLP[i].EIN;
                loadPointDataModel.LoadPoints[i].BackPress = extraLP[i].BackPress;
                loadPointDataModel.LoadPoints[i].BYP=extraLP[i].BYP;
                loadPointDataModel.LoadPoints[i].EIN=extraLP[i].EIN;
                loadPointDataModel.LoadPoints[i].WANZ=extraLP[i].WANZ;
                loadPointDataModel.LoadPoints[i].RSMIN=extraLP[i].RSMIN;
            }  
        }
        else
        {
            for(int i = 11; i < 10 + cxLP_RngStop; ++i)
            {
                int j = i - 9;
                loadPointDataModel.LoadPoints[i].MassFlow = extraLP[j].MassFlow;
                loadPointDataModel.LoadPoints[i].Rpm = extraLP[j].Rpm;
                loadPointDataModel.LoadPoints[i].Pressure = extraLP[j].Pressure;
                loadPointDataModel.LoadPoints[i].Temp = extraLP[j].Temp;
                loadPointDataModel.LoadPoints[i].EIN = extraLP[j].EIN;
                loadPointDataModel.LoadPoints[i].BackPress = extraLP[j].BackPress;
                loadPointDataModel.LoadPoints[i].BYP = extraLP[j].BYP;
                loadPointDataModel.LoadPoints[i].EIN = extraLP[j].EIN;
                loadPointDataModel.LoadPoints[i].WANZ = extraLP[j].WANZ;
                loadPointDataModel.LoadPoints[i].RSMIN = extraLP[j].RSMIN;
            }
        }
    }
 
    //public void updateBaseLPinSheet1(double eff = 0)
    //{
    //    turbineDataModel.InletPressure = additionalLoadPoint.CustomerLoadPoints[1].SteamPressure;
    //    turbineDataModel.InletTemperature = additionalLoadPoint.CustomerLoadPoints[1].SteamTemp;
    //    turbineDataModel.MassFlowRate = additionalLoadPoint.CustomerLoadPoints[1].SteamMass;
    //    turbineDataModel.ExhaustPressure = additionalLoadPoint.CustomerLoadPoints[1].ExhaustPressure;
    //    turbineDataModel.TurbineEfficiency = eff;
    //    double power = additionalLoadPoint.CustomerLoadPoints[1].PowerGeneration;
    //    turbineDataModel.GeneratorEfficiency = thermodynamicService.calculateGeneratorEfficiency(power);
    //}
 
    public void cxLP_GetLPcount()
    {
        cxLP_LPcount = additionalLoadPoint.CustomerLoadPoints.Count-1;
        cxLP_RngStart = 1;
        //Debug.
        cxLP_RngStop = additionalLoadPoint.CustomerLoadPoints.Count-1;
        
    }
 
    //public void cxLP_CalculateUnknowParams(double InitEff, string RecalcMode = "")
    //{
    //    KreislDATHandler kreislDatHandler = new KreislDATHandler();
    //    double SteamPressure, SteamTemp, SteamMass, ExhaustPressure, PowerGeneration, ExhaustMassFlow, eff;
    //    double leakageSteamMassFlow = 1.03;
    //    Logger("Calculating unknown parameters....");
    //    List<CustomerLoadPoint> lpList = AdditionalLoadPoint.GetInstance().CustomerLoadPoints; 
    //    for (int i = 1; i <= cxLP_RngStop; i++)
    //    {
    //        Logger("Inspecting Missing Params----LP:" + (i));
    //        SteamPressure = lpList[i].SteamPressure;
    //        SteamTemp = lpList[i].SteamTemp;
    //        SteamMass = lpList[i].SteamMass;
    //        ExhaustPressure = lpList[i].ExhaustPressure;
    //        PowerGeneration = lpList[i].PowerGeneration;
    //        ExhaustMassFlow = lpList[i].ExhaustMassFlow;
    //        var IsPartialLoad = lpList[i].PartLoad;
 
    //        if (string.IsNullOrEmpty(RecalcMode))
    //        {
    //            eff = InitEff;
    //            Logger("Running in Initial mode with Hypothetical Efficiency:" + eff);
    //        }
    //        else
    //        {
    //            eff = lpList[i].EffFromTurba;
    //            Logger("Running in recalculation mode with Efficiency:" + eff);
    //            kreislDatHandler.FillTurbineEff(StartKreisl.filePath, "4", Convert.ToString(eff));
    //        }
 
    //        if (!IsEmpty(IsPartialLoad))
    //        {
    //            Logger("Part load detected skipping");
    //            continue;
    //        }
 
    //        if (lpList[i].SteamPressure == 0)
    //        {
    //            Logger("Steam Pressure");
    //            lpList[i].SteamPressure = cxLP_CalculateParam("GetSteamPressure", eff, 0, SteamTemp, SteamMass, ExhaustPressure, PowerGeneration);
    //            extraLP[i].Pressure = lpList[i].SteamPressure;
               
    //            kreislDatHandler.FillInletPressure(StartKreisl.filePath, 5, lpList[i].SteamPressure.ToString());
    //        }
    //        if (lpList[i].SteamTemp == 0)
    //        {
    //            Logger("Steam Temp");

    //            lpList[i].SteamTemp = cxLP_CalculateParam("GetSteamTemp", eff, SteamPressure, 0, SteamMass, ExhaustPressure, PowerGeneration);
    //            extraLP[i].Temp = lpList[i].SteamTemp;

    //            kreislDatHandler.FillInletTemperature(StartKreisl.filePath, 5, extraLP[i].Temp.ToString());
    //        }
    //        if (lpList[i].SteamMass == 0)
    //        {
    //            Logger("Steam Mass Flow");
    //            if (ExhaustMassFlow > 0)
    //            {
    //                lpList[i].SteamMass = ExhaustMassFlow + leakageSteamMassFlow;
    //            }
    //            else
    //            {
    //                lpList[i].SteamMass = cxLP_CalculateParam( "GetSteamMass", eff, SteamPressure, SteamTemp, 0, ExhaustPressure, PowerGeneration);
    //            }
    //            extraLP[i].MassFlow = lpList[i].SteamMass;//Convert.ToDouble(ws.Cells[i, 4].Value);
    //        }
    //        if (lpList[i].ExhaustPressure == 0)
    //        {
    //            Logger("Exhaust Pressure");
    //            lpList[i].ExhaustPressure = cxLP_CalculateParam("GetExPressure", eff, SteamPressure, SteamTemp, SteamMass, 0, PowerGeneration);
    //            extraLP[i].BackPress = lpList[i].ExhaustPressure;
    //        }
    //        if (lpList[i].PowerGeneration == 0)
    //        {
    //            Logger("Power Generation");
    //            lpList[i].PowerGeneration = cxLP_CalculateParam("GetPower", eff, SteamPressure, SteamTemp, SteamMass, ExhaustPressure);
    //        }
    //        if (lpList[i].ExhaustMassFlow == 0)
    //        {
    //            Logger("Exhaust Mass Flow");
    //        }
    //        fillInitialValues(lpList[i].SteamPressure, lpList[i].SteamTemp, lpList[i].SteamMass, lpList[i].ExhaustPressure, lpList[i].PowerGeneration);
    //        lpList[i].VolFlow = cxLP_GetVolFlow();
    //        Logger("``````````````````````````````````");
    //    }
 
    //    double basePower = 0;//Convert.ToDouble(ws.Cells[BaseCaseRow, 6].Value);
    //    double maxVolFlow = -1;
    //    for(int i = 1; i <= cxLP_RngStop; i++){
    //        if(maxVolFlow <= lpList[i].VolFlow){
    //            maxVolFlow = lpList[i].VolFlow;
    //            basePower = lpList[i].PowerGeneration;
    //        }
    //    }
    //    for (int i = 1; i <= cxLP_RngStop; i++)
    //    {
    //        Logger("Handling part load Case Params----LP:" + (i ));
    //        double IsPartialLoad = lpList[i].PartLoad;//ws.Cells[i, 8].Value;
    //        if (IsPartialLoad != 0)
    //        {
    //            lpList[i].PowerGeneration = basePower * (Convert.ToDouble(IsPartialLoad) / 100);
    //        }
    //    }
    //}
 
    public void cxLP_UpdatePartLoad(double InitEff, string RecalcMode = "")
    {
        double SteamPressure, SteamTemp, SteamMass, ExhaustPressure, PowerGeneration, ExhaustMassFlow;
        double leakageSteamMassFlow = 1.03;
 
        Logger("Calculating power using partial loading% ....");
 
        for (int i = 1; i <= cxLP_RngStop; i++)
        {
            Logger("Inspecting Missing Params----LP:" + (i));
            SteamPressure = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].SteamPressure);
            SteamTemp = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].SteamTemp);
            SteamMass = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].SteamMass);
            ExhaustPressure = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].ExhaustPressure);
           
            PowerGeneration = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration);
            ExhaustMassFlow = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].ExhaustMassFlow);
            var IsPartialLoad = additionalLoadPoint.CustomerLoadPoints[i].PartLoad;
 
            if (IsPartialLoad < 0)
            {
                Logger("Not a Part load skipping");
                continue;
            }
            double eff;
            if (string.IsNullOrEmpty(RecalcMode))
            {
                eff = InitEff;
                Logger("Running in Initial mode with Hypothetical Efficiency:" + eff);
            }
            else
            {
                eff = (additionalLoadPoint.CustomerLoadPoints[i].EffFromTurba);
                Logger("Running in recalculation mode with Efficiency:" + eff);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].SteamPressure == 0)
            {
                Logger("Steam Pressure");
                additionalLoadPoint.CustomerLoadPoints[i].SteamPressure = cxLP_CalculateParam("GetSteamPressure", eff, 0, SteamTemp, SteamMass, ExhaustPressure, PowerGeneration);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].SteamTemp == 0)
            {
                Logger("Steam Temp");
                additionalLoadPoint.CustomerLoadPoints[i].SteamTemp = cxLP_CalculateParam("GetSteamTemp", eff, SteamPressure, 0, SteamMass, ExhaustPressure, PowerGeneration);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].SteamMass == 0)
            {
                Logger("Steam Mass Flow");
                if (ExhaustMassFlow > 0)
                {
                    additionalLoadPoint.CustomerLoadPoints[i].SteamMass = ExhaustMassFlow + leakageSteamMassFlow;
                }
                else
                {
                    additionalLoadPoint.CustomerLoadPoints[i].SteamMass = cxLP_CalculateParam("GetSteamMass", eff, SteamPressure, SteamTemp, 0, ExhaustPressure, PowerGeneration);
                }
                SteamMass = Convert.ToDouble(additionalLoadPoint.CustomerLoadPoints[i].SteamMass);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].ExhaustPressure == 0)
            {
                Logger("Exhaust Pressure");
                additionalLoadPoint.CustomerLoadPoints[i].ExhaustPressure = cxLP_CalculateParam("GetExPressure", eff, SteamPressure, SteamTemp, SteamMass, 0, PowerGeneration);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration == 0)
            {
                Logger("Power Generation");
                additionalLoadPoint.CustomerLoadPoints[i].PowerGeneration = cxLP_CalculateParam( "GetPower", eff, SteamPressure, SteamTemp, SteamMass, ExhaustPressure);
            }
            if (additionalLoadPoint.CustomerLoadPoints[i].ExhaustMassFlow == 0)
            {
                Logger("Exhaust Mass Flow");
            }
            if(turbineDataModel.MassFlowRate == 0){
                turbineDataModel.MassFlowRate = additionalLoadPoint.CustomerLoadPoints[i].SteamMass;
            }
            if(turbineDataModel.InletPressure == 0){
                turbineDataModel.InletPressure = additionalLoadPoint.CustomerLoadPoints[i].SteamPressure;
            }
            if(turbineDataModel.InletTemperature == 0){
                turbineDataModel.InletTemperature = additionalLoadPoint.CustomerLoadPoints[i].SteamTemp;
            }
            additionalLoadPoint.CustomerLoadPoints[i].VolFlow = cxLP_GetVolFlow();
            Logger("``````````````````````````````````");
        }
    }
 
    public void cxLP_ReorderbyPower_Mass(string Power_or_Mass)
    {
        additionalLoadPoint.SortCustomerLoadPointsByPower();
    }
 
    public void cxLP_ReorderbyVolFlow()
    {
        additionalLoadPoint.SortCustomerLoadPointsByVol();
    }

    public double cxLP_GetVolFlow()
    {
        return thermodynamicService.getVolumetricFlow();
    }
    public void fillInitialValues(double P1, double T1, double massFlow, double P2, double pow)
    {
        turbineDataModel.InletPressure = P1;
        turbineDataModel.InletTemperature = T1;
        turbineDataModel.MassFlowRate = massFlow;
        turbineDataModel.ExhaustPressure = P2;
        turbineDataModel.AK25 = pow;
        turbineDataModel.FinalPower = pow;
    }
    public double cxLP_CalculateParam(string OperationType, double eff, double PressureValue = 0, double TempValue = 0, double InMassFlow = 0, double BackpressureValue = 0, double PowerReq = 0)
    {
        fillInitialValues(PressureValue, TempValue, InMassFlow, BackpressureValue, PowerReq);
        if (PowerReq == 0 && eff != 0)
            HBDupdateEff(eff);
       
 
        double result = 0;
        decimal pow = Convert.ToDecimal(turbineDataModel.AK25);
        KreislDATHandler datHandler = new KreislDATHandler();
        if(PowerReq != 0)
            pow = Convert.ToDecimal(PowerReq);
        turbineDataModel.TurbineEfficiency = eff;
        turbineDataModel.GeneratorEfficiency = thermodynamicService.calculateGeneratorEfficiency(Convert.ToDouble(pow));
        switch (OperationType)
        {
            case "GetPower":
                result = CalculateKreislPower();
                //thermodynamicService.PerformCalculations();
                //result = turbineDataModel.AK25;//Convert.ToDouble(HMBD.Cells["AK25"].Value);
                break;

            case "GetSteamMass":
                GoalMassFlow goalMassFlow = new GoalMassFlow(this);
                if(StartKreisl.kreislKey)
                {
                    result = goalMassFlow.FindMassFlowKreisl(PressureValue, BackpressureValue, TempValue, PowerReq);
                    datHandler.FillMassFlow(StartKreisl.filePath, 5, "0.000", -1);
                    datHandler.FillMassFlow(StartKreisl.filePath, 5, result.ToString());
                }
                else
                {
                    result = goalMassFlow.GetMassFlow();
                }
                break;

            case "GetSteamPressure":
                GoalInletPressure goalInletPressure = new GoalInletPressure(this);
                if(StartKreisl.kreislKey)
                {
                    result = goalInletPressure.FindInletPressure(BackpressureValue, TempValue, InMassFlow, PowerReq);
                    datHandler.FillInletPressure(StartKreisl.filePath, 5, "0.000", -1);
                    datHandler.FillInletPressure(StartKreisl.filePath, 5, result.ToString());
                }
                else
                {
                    //var goalSeekP1 = GoalSeek.TrySeek(goalInletPressure.Calculate, new List<decimal> { 0.15m }, pow, 100m, 1000, false);
                    //result = Convert.ToDouble(goalSeekP1.ClosestValue);
                }
                break;

            case "GetSteamTemp":
                GoalInletTemperature goalInletTemperature = new GoalInletTemperature(this);
                if (StartKreisl.kreislKey)
                {
                    double inletTemp = goalInletTemperature.FindInletTemperature(PressureValue, BackpressureValue, InMassFlow, PowerReq);
                    datHandler.FillInletTemperature(StartKreisl.filePath, 5, "0.000", -1);
                    datHandler.FillInletTemperature(StartKreisl.filePath, 5, inletTemp.ToString());
                    result = inletTemp;
                }
                else
                {
                    //var goalSeekT1 = GoalSeek.TrySeek(goalInletTemperature.Calculate, new List<decimal> { 0.15m }, pow, 1000m, 1000, false);
                    //result = Convert.ToDouble(goalSeekT1.ClosestValue);
                }
                break;

            case "GetExPressure":
                GoalExhaustPressure goalP2 =new GoalExhaustPressure(this);
                if(StartKreisl.kreislKey)
                {
                    result = goalP2.FindExhaustPressure(PressureValue, TempValue, InMassFlow, PowerReq);
                    datHandler.FillExhaustPressure(StartKreisl.filePath, 4, "0.000", -1);
                    datHandler.FillExhaustPressure(StartKreisl.filePath, 4, result.ToString());
                }
                else
                {
                    result = goalP2.GetExhaustPressure();
                }
                break;

            default:
                Logger("Invalid Operation Type");
                result = -1;
                break;
        }
        return Math.Round(result, 3);
    }
    
    //public void FillLoadPointTemperature(int lp, double temp)
    //{
    //    extraLP[lp].Temp = temp;
    //}
    public long FindMaxValueRow(ExcelRange rng)
    {
        double maxValue = double.MinValue;
        long maxRow = rng.Start.Row;
 
        foreach (var cell in rng)
        {
            if (Convert.ToDouble(cell.Value) > maxValue)
            {
                maxValue = Convert.ToDouble(cell.Value);

                maxRow = cell.Start.Row;
            }
        }
 
        return maxRow;
    }
 
    // Placeholder methods for the external calls in the original VBA code
    private void Logger(string message) {
        logger.LogInformation(message);
    }

    private void TerminateIgniteX(string message) {
 
    }
    private void initConfig() {
 
    }
    private void HBDsetDefaultCustomerParamas()
    {
        HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
        if(StartKreisl.kreislKey)
        {
            hBDPowerCalculator.HBDSetDefaultCustomerParamsKreisL();
        }
        else
            hBDPowerCalculator.HBDSetDefaultCustomerParams();
    }
    private void HBDupdateEff_Generator_Init()
    {
         HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
         hBDPowerCalculator.HBDUpdateEffGeneratorInit();
    }
    private void HBDPersistInitialPower() {
        HBDPowerCalculator hBDPowerCalculator = new HBDPowerCalculator();
        hBDPowerCalculator.HBDPersistInitialPower();
    }
    private void ReferenceDATSelector(int maxLp = 0) {
        DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        datFileSelector.ReferenceDATSelector(maxLp);  
    }
    private void GenerateLoadPoints() {
        LoadPointGen loadPointGen= new LoadPointGen();
        loadPointGen.GenerateLoadPoints((int)cxLP_RngStop);
     }
    private void prepareDATFile(int maxLps = 0) {
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.PrepareDATFile(maxLps);
    }
    private void datFileInitParamsExceptLP(double maxPower) {
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.DatFileInitParamsExceptLP1(maxPower);
        // dATFileProcessor
     }
    public void LaunchTurba(int maxLPs = 0) {
        TurbaConfig turbaConfig= new TurbaConfig();
        turbaConfig.LaunchTurba(maxLPs);
       
    }
    private void ergResultsCheck(int maxLps = 0) {
        ERGVerification eRGVerification= new ERGVerification();
        eRGVerification.ErgResultsCheck(maxLps);
     }
    private void ValvePointOptimize(int maxLP) {
        ValvePointOptimizer valvePointOptimizer= new ValvePointOptimizer();
        valvePointOptimizer.ValvePointOptimize(maxLP);
     }
    private void CheckPower(int maxLP = 0) {
        PowerMatch powerMatch = new PowerMatch();
        powerMatch.CheckPower(maxLP);
    }
    private void prepareTurbineFiles() {
        TurbaConfig turbaConfig= new TurbaConfig();
        turbaConfig.PrepareTurbineFiles();
    }
    private void loadLP1fromDAT() {
        DATFileProcessor dATFileProcessor= new DATFileProcessor();
        dATFileProcessor.LoadLP1FromDAT();
     }
    private void DeleteRowAfterFirstLoadPoint() {
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.DeleteRowAfterFirstLoadPoint();
     }
    private void InsertDataLineUnderFirstLPFixed() {
        DATFileProcessor dATFileProcessor = new DATFileProcessor();
        dATFileProcessor.InsertDataLineUnderFirstLPFixed();
    }
    private void DeleteLoadPoints() {
        DATFileProcessor dATFileProcessor= new DATFileProcessor();
        dATFileProcessor.DeleteLoadPoints();
    }
    private void InsertLoadPointsWithExactFormattingUsingMid(int maxLP = 0) {
        DATFileProcessor dATFileProcessor= new DATFileProcessor();
        if (maxLP > 0)
        {
            dATFileProcessor.InsertLoadPointsWithExactFormattingUsingMid(maxLP);
        }
        else
            dATFileProcessor.InsertLoadPointsWithExactFormattingUsingMid((int)cxLP_RngStop);
     }
    private void InsertDataLineUnderND(int maxLP = 0) {
        DATFileProcessor dATFileProcessor =  new DATFileProcessor();
        if (maxLP > 0)
        {
            dATFileProcessor.InsertDataLineUnderND(maxLP);
        }
        else
            dATFileProcessor.InsertDataLineUnderND((int)cxLP_RngStop);
     }
    private void cxLP_UpdateCustomParamsForkNN() {
        PowerKNN powerKNN = new PowerKNN();
        powerKNN.ExecutePowerKNN("CustomLoadCases");
    }
    private void HBDupdateEff(double eff) {
        HBDPowerCalculator hBDPowerCalculator= new HBDPowerCalculator();
        hBDPowerCalculator.HBDUpdateEff(eff);
    }

    private double CalculateKreislPower()
    {
        KreislDATHandler datHandler = new KreislDATHandler();
        string filePath = StartKreisl.filePath;
        datHandler.FillMassFlow(filePath, 5, turbineDataModel.MassFlowRate.ToString());
        datHandler.FillInletPressure(filePath, 5, turbineDataModel.InletPressure.ToString());
        datHandler.FillExhaustPressure(filePath, 4, turbineDataModel.ExhaustPressure.ToString());
        datHandler.FillInletTemperature(filePath, 5, turbineDataModel.InletTemperature.ToString());
         KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
        KreislERGHandlerService kreislERGHandlerService = new KreislERGHandlerService();
        double pow = 0;
        while (pow == 0)
        {
            pow = kreislERGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath);
        }
        return pow;
    }
    private bool IsEmpty(object value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is int intValue && intValue == 0)
        {
            return true;
        }
        if (value is double doubleValue && doubleValue == 0.0)
        {
            return true;
        }

        return string.IsNullOrEmpty(value.ToString());
    }
}

public class GoalMassFlow
{
    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double hVon_p_t(double P, double T, double unknown);

    private string excelPath;
    TurbineDataModel turbineDataModel;
    PreFeasibilityDataModel preFeasibilityDataModel;
    IThermodynamicLibrary thermodynamicService;
    CustomLoadPointHandler customLoadPointHandler;

    public GoalMassFlow(){
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
    }

    public GoalMassFlow(CustomLoadPointHandler lpHandler)
    {
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        customLoadPointHandler = lpHandler;
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
    }

    public double FindMassFlowKreisl(double P1, double P2, double T1, double power)
    {
        double result = 0;
        string filePath = StartKreisl.filePath;
        string ergFilePath = StartKreisl.ergFilePath;
        KreislDATHandler datHandler = new KreislDATHandler();

        datHandler.FillWheelChamberPressure(filePath, "1 0", (P1 / 2.00).ToString());
        datHandler.FillMassFlow(filePath, 5, "0.000");
        datHandler.FillMassFlow(filePath, 5, "8.000", -1);
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
       
        KreislERGHandlerService eRGHandlerService = new KreislERGHandlerService();
        while (result == 0)
        {
            result = eRGHandlerService.ExtractMassFlowFromERG(ergFilePath);
            Thread.Sleep(500);
            Debug.WriteLine("waiting for kriesl result");
        }

        double volFlow = eRGHandlerService.ExtractVolFlowFromERG(ergFilePath);
        preFeasibilityDataModel.InletPressureActualValue = P1;
        preFeasibilityDataModel.BackpressureActualValue = P2;
        preFeasibilityDataModel.TemperatureActualValue = T1;
        CustomLoadPointHandler.extraLP[1].MassFlow = result; //Fill Back Pressure in loadpoint-1
        preFeasibilityDataModel.PowerActualValue = power;
        preFeasibilityDataModel.InletVolumetricFlowActualValue = volFlow;
        customLoadPointHandler.fillInitialValues(P1, T1, result, P2, power);
        // preFeasibilityDataModel
        RefDATSelector();
        //customLoadPointHandler.FillLoadPointTemperature();
        customLoadPointHandler.cxLP_GenerateLoadPoints();
        customLoadPointHandler.cxLP_prepareDATFile(1);
        customLoadPointHandler.LaunchTurba(1);
        double wheelP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;

        //Fill the Wheel Chamber Pressure back
        if (wheelP > 0)
            datHandler.FillWheelChamberPressure(filePath, "1 0", wheelP.ToString());
        datHandler.FillMassFlow(filePath, 5, "0.000");
        datHandler.FillMassFlow(filePath, 5, "8.000", -1);
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        kreislIntegration.LaunchKreisL();
        result = eRGHandlerService.ExtractMassFlowFromERG(ergFilePath);
        datHandler.FillVariablePower(filePath, 6, "0.000");
        return result;
    }
    public void RefDATSelector()
    {
        DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        datFileSelector.ReferenceDATSelector();
    }
    public double GetMassFlow()
    {
        double pow = turbineDataModel.AK25;
        pow *= ((100.000)/turbineDataModel.GeneratorEfficiency);
        pow *= (100.0000)/(100.0000 - turbineDataModel.Margin);
        double gearLoss = thermodynamicService.calculateGearLosses(pow - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses);
        pow += (turbineDataModel.OilLosses + turbineDataModel.InventoryLosses + gearLoss);
        double H1 = getInletEnthalpy();
        double H2 = getOutletEnthalpy(H1);
        pow/=(H1-H2);
        return pow;
    }

    public double getInletEnthalpy()
    {
        return hVon_p_t(turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 0);
    }
    public double getOutletEnthalpy(double H1)
    {
        double outletEnthalpy = thermodynamicService.getOutletEnthalpy(turbineDataModel.ExhaustPressure, H1, (turbineDataModel.TurbineEfficiency==0)?85: turbineDataModel.TurbineEfficiency, turbineDataModel.InletPressure);
        return outletEnthalpy;
    }
}
public class GoalExhaustPressure
{
    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double hVon_p_t(double P, double T, double unknown);

    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double sVon_p_h(double P, double H, double unknown);

    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double pVon_h_s(double H, double S, double unknown);

    private string excelPath;
    TurbineDataModel turbineDataModel;
    PreFeasibilityDataModel preFeasibilityDataModel;
    IThermodynamicLibrary thermodynamicService;
    IERGHandlerService kreislERGHandlerService;
    CustomLoadPointHandler customLoadPointHandler;

    public GoalExhaustPressure(){
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        kreislERGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        customLoadPointHandler = new CustomLoadPointHandler();
    }

    public GoalExhaustPressure(CustomLoadPointHandler lpHandler)
    {
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        kreislERGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        customLoadPointHandler = lpHandler;
    }

    public double FindExhaustPressure(double P1, double T1, double massFlow, double power)
    {
        double result = 0;
        string filePath = StartKreisl.filePath;
        string ergFilePath = StartKreisl.ergFilePath;
        //Fill the Power, Vol Flow
        KreislDATHandler datHandler = new KreislDATHandler();
        
        
        datHandler.FillWheelChamberPressure(filePath, "1 0", (P1 / 2.00).ToString());
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, "0.000");
        datHandler.FillExhaustPressure(filePath, 4, "5.000", -1);
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
       
        KreislERGHandlerService eRGHandlerService = new KreislERGHandlerService();
        while (result == 0)
        {
            result = kreislERGHandlerService.ExtractExhaustPressureFromERG(ergFilePath);
            Thread.Sleep(500);
        }
        double volFlow = kreislERGHandlerService.ExtractVolFlowFromERG(ergFilePath);
        preFeasibilityDataModel.InletPressureActualValue = P1;
        preFeasibilityDataModel.BackpressureActualValue = result;
        preFeasibilityDataModel.TemperatureActualValue = T1;
        CustomLoadPointHandler.extraLP[1].BackPress = result; //Fill Back Pressure in loadpoint-1
        preFeasibilityDataModel.PowerActualValue = power;
        preFeasibilityDataModel.InletVolumetricFlowActualValue = volFlow;
        customLoadPointHandler.fillInitialValues(P1, T1, massFlow, result, power);
        // preFeasibilityDataModel
        RefDATSelector();
        //customLoadPointHandler.FillLoadPointTemperature();
        customLoadPointHandler.cxLP_GenerateLoadPoints();
        customLoadPointHandler.cxLP_prepareDATFile(1);
        customLoadPointHandler.LaunchTurba(1);
        double wheelP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;

        //Fill the Wheel Chamber Pressure back
        if (wheelP > 0)
            datHandler.FillWheelChamberPressure(filePath, "1 0", wheelP.ToString());

        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, "0.000");
        datHandler.FillExhaustPressure(filePath, 4, "5.000", -1);
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        kreislIntegration.LaunchKreisL();

        result = kreislERGHandlerService.ExtractExhaustPressureFromERG(ergFilePath);
        datHandler.FillVariablePower(filePath, 6, "0.000");
        return result;
    }
    public void RefDATSelector()
    {
        DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        datFileSelector.ReferenceDATSelector();
    }
    public double GetExhaustPressure()
    {
        double pow = turbineDataModel.AK25;
        pow *= (100.0000/turbineDataModel.GeneratorEfficiency) * (100.0000/(100.0000 - turbineDataModel.Margin));
        double gearLoss = thermodynamicService.calculateGearLosses(pow - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses);
        pow += (turbineDataModel.OilLosses + turbineDataModel.InventoryLosses + gearLoss);
        pow /= turbineDataModel.MassFlowRate; //delta-H
        double inletEnthalpy = hVon_p_t(turbineDataModel.InletPressure, turbineDataModel.InletTemperature, 0);
        // Console.WriteLine("InletEnthalpy;;;"+ inletEnthalpy);
        double hEntropy = inletEnthalpy - (pow * (100.00 / turbineDataModel.TurbineEfficiency));
        //i have s, h, i want p2
        double entropy = sVon_p_h(turbineDataModel.InletPressure, inletEnthalpy, 0);
        double result = pVon_h_s(hEntropy, entropy, 0);
        return result;
    }
}
public class GoalInletTemperature //: IGoalSeek
{
    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double hVon_p_t(double P, double T, double unknown);

    private string excelPath;
    TurbineDataModel turbineDataModel;
    IThermodynamicLibrary thermodynamicService;
    IERGHandlerService kreislERGHandlerService;
    PreFeasibilityDataModel preFeasibilityDataModel;
    CustomLoadPointHandler customLoadPointHandler;
    public GoalInletTemperature(){
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        kreislERGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        customLoadPointHandler = new CustomLoadPointHandler();
    }

    public GoalInletTemperature(CustomLoadPointHandler lpHandler)
    {
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        kreislERGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
        .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        customLoadPointHandler = lpHandler;
    }

    public double FindInletTemperature(double P1, double P2, double massFlow, double power)
    {
        double result = 0;
        string filePath = StartKreisl.filePath;
        string ergFilePath = StartKreisl.ergFilePath;
        KreislDATHandler datHandler = new KreislDATHandler();

        datHandler.FillWheelChamberPressure(filePath, "1 0", (P1/2.00).ToString());
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, "450.000", -1);
        datHandler.FillInletTemperature(filePath, 5, "0.000");
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
        KreislERGHandlerService eRGHandlerService = new KreislERGHandlerService();
        while (result == 0)
        {
            result = kreislERGHandlerService.ExtractInletTemperatureFromERG(ergFilePath);
            Thread.Sleep(500);
        }
        //result = kreislERGHandlerService.ExtractInletTemperatureFromERG(ergFilePath);
        //double checkVOlFlow = kreislERGHandlerService.ExtractVolFlowFromERG(StartKreisl.ergFilePath);
        //datHandler.FillInletTemperature(StartKreisl.filePath, 5, "0.000", -1);
        //datHandler.FillInletTemperature(filePath, 5, result.ToString());
        //datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        //datHandler.FillInletPressure(filePath, 5, P1.ToString());
        //datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        //datHandler.FillVariablePower(filePath, 6, "0.000");
        //kreislIntegration.LaunchKreisL();

        double volFlow = kreislERGHandlerService.ExtractVolFlowFromERG(ergFilePath);
        preFeasibilityDataModel.InletPressureActualValue = P1;
        preFeasibilityDataModel.BackpressureActualValue = P2;
        preFeasibilityDataModel.TemperatureActualValue = result;
        CustomLoadPointHandler.extraLP[1].Temp = result; //Fill Temp in loadpoint-1
        preFeasibilityDataModel.PowerActualValue = power;
        preFeasibilityDataModel.InletVolumetricFlowActualValue = volFlow;
        customLoadPointHandler.fillInitialValues(P1, result, massFlow, P2, power);
        // preFeasibilityDataModel
        RefDATSelector();
        //customLoadPointHandler.FillLoadPointTemperature();
        customLoadPointHandler.cxLP_GenerateLoadPoints();
        customLoadPointHandler.cxLP_prepareDATFile(1);
        customLoadPointHandler.LaunchTurba(1);
        double wheelP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;


        //Fill the Wheel Chamber Pressure back
        if(wheelP > 0)
            datHandler.FillWheelChamberPressure(filePath, "1 0", wheelP.ToString());
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, P1.ToString());
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, "450.000", -1);
        datHandler.FillInletTemperature(filePath, 5, "0.000");
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        kreislIntegration.LaunchKreisL();
        result = kreislERGHandlerService.ExtractInletTemperatureFromERG(StartKreisl.ergFilePath);

        datHandler.FillVariablePower(filePath, 6, "0.000");
        return result;
    }

    public void RefDATSelector()
    {
        DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        datFileSelector.ReferenceDATSelector();
    }
    public decimal Calculate(decimal x){
        if(x < 0m){
            return x * 10m;
        }
        double H1 = getInletEnthalpy(Convert.ToDouble(x));
        double H2 = getOutletEnthalpy(turbineDataModel.InletPressure, H1);
        // Console.WriteLine("H1:"+H1+", H2:"+H2);
        double pow = turbineDataModel.MassFlowRate * (H1 - H2);
        double gearLoss = thermodynamicService.calculateGearLosses(pow - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses);
        pow -= (turbineDataModel.OilLosses + turbineDataModel.InventoryLosses + gearLoss);
        pow *= (turbineDataModel.GeneratorEfficiency/100.00);
        pow *= (1 - (turbineDataModel.Margin/100.00));
        // Console.WriteLine("power:"+pow);
        return Convert.ToDecimal(pow);


    }
    public double getInletEnthalpy(double inletTemp)
    {
        return hVon_p_t(turbineDataModel.InletPressure, inletTemp, 0);
    }
    public double getOutletEnthalpy(double press, double H1)
    {
        double outletEnthalpy = thermodynamicService.getOutletEnthalpy(turbineDataModel.ExhaustPressure, H1, turbineDataModel.TurbineEfficiency, press);
        return outletEnthalpy;
    }
}
public class GoalInletPressure //: IGoalSeek
{
    [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern double hVon_p_t(double P, double T, double unknown);

    private string excelPath;
    TurbineDataModel turbineDataModel;
    PreFeasibilityDataModel preFeasibilityDataModel;
    CustomLoadPointHandler customLoadPointHandler;
    IThermodynamicLibrary thermodynamicService;

    public GoalInletPressure(){
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        customLoadPointHandler = new CustomLoadPointHandler();
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
    }

    public GoalInletPressure(CustomLoadPointHandler lpHandler)
    {
        turbineDataModel = TurbineDataModel.getInstance();
        thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
        IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
       .Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
        customLoadPointHandler = lpHandler;
        preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
    }

    public double FindInletPressure(double P2, double T1, double massFlow, double power)
    {
        double result = 0;
        string filePath = StartKreisl.filePath;
        string ergFilePath = StartKreisl.ergFilePath;
        KreislDATHandler datHandler = new KreislDATHandler();
        datHandler.FillWheelChamberPressure(filePath, "1 0", (P2 + 10.00).ToString());
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, "40.000", -1);
        datHandler.FillInletPressure(filePath, 5, "0.000");
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        KreislIntegration kreislIntegration = new KreislIntegration();
        kreislIntegration.LaunchKreisL();
        KreislERGHandlerService eRGHandlerService = new KreislERGHandlerService();
        while (result == 0)
        {
            result = eRGHandlerService.ExtractInletPressureFromERG(StartKreisl.ergFilePath);
            Thread.Sleep(500);
        }
        //
        double volFlow = eRGHandlerService.ExtractVolFlowFromERG(ergFilePath);
        preFeasibilityDataModel.InletPressureActualValue = result;
        preFeasibilityDataModel.BackpressureActualValue = P2;
        preFeasibilityDataModel.TemperatureActualValue = T1;
        CustomLoadPointHandler.extraLP[1].Pressure = result; //Fill Back Pressure in loadpoint-1
        preFeasibilityDataModel.PowerActualValue = power;
        preFeasibilityDataModel.InletVolumetricFlowActualValue = volFlow;
        customLoadPointHandler.fillInitialValues(result, T1, massFlow, P2, power);
        // preFeasibilityDataModel
        RefDATSelector();
        //customLoadPointHandler.FillLoadPointTemperature();
        customLoadPointHandler.cxLP_GenerateLoadPoints();
        customLoadPointHandler.cxLP_prepareDATFile(1);
        customLoadPointHandler.LaunchTurba(1);
        double wheelP = TurbaOutputModel.getInstance().OutputDataList[0].Wheel_Chamber_Pressure;

        //Fill the Wheel Chamber Pressure back
        if (wheelP > 0)
            datHandler.FillWheelChamberPressure(filePath, "1 0", wheelP.ToString());
        datHandler.FillMassFlow(filePath, 5, massFlow.ToString());
        datHandler.FillInletPressure(filePath, 5, "40.000", -1);
        datHandler.FillInletPressure(filePath, 5, "0.000");
        datHandler.FillExhaustPressure(filePath, 4, P2.ToString());
        datHandler.FillInletTemperature(filePath, 5, T1.ToString());
        datHandler.FillVariablePower(filePath, 6, power.ToString());

        kreislIntegration.LaunchKreisL();
        result = eRGHandlerService.ExtractInletPressureFromERG(StartKreisl.ergFilePath);

        datHandler.FillVariablePower(filePath, 6, "0.000");
        return result;
    }
    public void RefDATSelector()
    {
        DatFileSelector datFileSelector = new DatFileSelector(excelPath);
        datFileSelector.ReferenceDATSelector();
    }
    public decimal Calculate(decimal x)
    {
        if(x<0m){
            return x*100m;
        }
        double p1 = Convert.ToDouble(x);
        // Console.WriteLine("P1:"+ p1);
        double outletEnthalpy = thermodynamicService.getOutletEnthalpy(turbineDataModel.ExhaustPressure, getInletEnthalpy(p1), turbineDataModel.GeneratorEfficiency, p1);
        double pow = turbineDataModel.MassFlowRate * (getInletEnthalpy(p1) - getOutletEnthalpy(p1));
        double gearLoss = thermodynamicService.calculateGearLosses(pow - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses);
        pow -= (turbineDataModel.OilLosses + turbineDataModel.InventoryLosses + gearLoss);
        pow *= (turbineDataModel.GeneratorEfficiency/100.00);
        pow *= (1 - (turbineDataModel.Margin/100.00));

        // Console.WriteLine("pow:"+pow);
        return Convert.ToDecimal(pow);

    }
    public double getInletEnthalpy(double press)
    {
        return hVon_p_t(press, turbineDataModel.InletTemperature, 0);
    }
    public double getOutletEnthalpy(double press)
    {
        double outletEnthalpy = thermodynamicService.getOutletEnthalpy(turbineDataModel.ExhaustPressure, getInletEnthalpy(press), turbineDataModel.TurbineEfficiency, press);
        return outletEnthalpy;
    }
}
public class EnquiryMap : ClassMap<Enquiry>
{
    public EnquiryMap()
    {
        Map(m => m.Customer).Name("Customer");
        Map(m => m.EnquiryID).Name("EnquiryID");
        Map(m => m.Consultant).Name("Consultant");
        Map(m => m.DateCreated).Name("DateCreated").TypeConverter<CustomDateTimeConverter>();
        Map(m => m.Power).Name("Power");
        Map(m => m.ExhaustPressure).Name("Exhaust Pressure");
        Map(m => m.SteamMass).Name("Steam Mass");
        Map(m => m.LoadPoint).Name("LoadPoints");
        Map(m => m.ExhaustMassFlow).Name("Exhaust MassFlow");
        Map(m => m.SteamTemperature).Name("Steam Temp"); // Map to the correct header
        Map(m => m.SteamPressure).Name("Steam Pressure");
        Map(m => m.PoC).Name("Sales SPoC");
        Map(m => m.Status).Name("Status");
        Map(m => m.Remark).Name("Remark");
        Map(m => m.TurbineNo).Name("Turbine No");
        Map(m => m.RevisionNo).Name("Revision No");
        Map(m => m.RefProjectNo).Name("Ref Project No");
        Map(m => m.Path).Name("Path");
        Map(m => m.SelectionStatus).Name("SelectionStatus");
        Map(m => m.PartLoad).Name("PartLoad");
    }
}