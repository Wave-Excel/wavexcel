namespace Models.ExecutedProjectDB;
using System;
using System.IO.Pipes;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

public class ExecutedDB{
    private List<ExecutedProject> executedProjectDB = new List<ExecutedProject>();
    private static ExecutedDB executedDB;
    private IConfiguration configuration;
    private string excelPath = @"C:\testDir\RunTurbaCycle_V1.5.7.xlsm";
    
    private ExecutedDB(){
        configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        excelPath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
    } 
    public static ExecutedDB getInstance(){
        if (executedDB == null) {
            executedDB = new ExecutedDB();
        }
        return executedDB;
    }
    public List<ExecutedProject> ExecutedProjectDB{
        get { return executedProjectDB; }
        
    }
    
    public void fillExecutedDB(){
        string pp = Path.Combine(AppContext.BaseDirectory, excelPath);
      FileInfo excelFile = new FileInfo(pp);
      ExcelPackage  package = new ExcelPackage(excelFile);
      var workbook = package.Workbook.Worksheets["ExecutedProjects"];
      int row =0;
      for(int i=5;i<=127;i++){
        executedProjectDB.Add(new ExecutedProject());
        int col = 1;
        executedProjectDB[row].PNoRev = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].ProjectID = workbook.Cells[i, col++].Value.ToString();
        //Console.WriteLine("PROHEJCCCCCCCCCCCCJJJJJJJJJJJJJJJJJ:"+ executedProjectDB[row].ProjectID);
        executedProjectDB[row].Project = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].PNo = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].GBC = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Type = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Application = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].InletControl = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].InletType = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Size = workbook.Cells[i, col++].Value.ToString();

        executedProjectDB[row].RpmMin = int.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].RpmMax = int.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].EtaIt = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].DHS = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].CouplePower = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LiveSteamPressure = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LiveSteamTemperature = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LiveSteamMassFlow = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LiveSteamVolumetricFlow = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].WheelChamberPressure = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].ExtraDeviceCss = workbook.Cells[i, col++].Value.ToString();

        string extr1P = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].Extr1Pressure = double.Parse(extr1P);
        col++;

        string extr2P = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].Extr2Pressure = double.Parse(extr2P);
        col++;

        string exhaustMassFlow = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].ExhaustMassFlow = double.Parse(exhaustMassFlow);
        col++;

        executedProjectDB[row].ExhaustTemp = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].ExhaustX = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].ExhaustPressure = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].ExhaustVolumetricFlow = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LpPart = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Exhaust = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].CalcProgram = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].AbnDesign = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].AgeOfDesign = int.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].BearDist = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].CvSize = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].ESV = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].ESVSize = workbook.Cells[i, col++].Value.ToString();

        string numAdm = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumAdm = int.Parse(numAdm);
        col++;

        string numBleeds = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumBleeds = int.Parse(numBleeds);
        col++;

        string numCv = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumCv = int.Parse(numCv);
        col++;

        string numDrums = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumDrums = int.Parse(numDrums);
        col++;

        string numExtr = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumExtr = int.Parse(numExtr);
        col++;

        string numStages = (workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].NumStages = int.Parse(numStages);
        col++;

        executedProjectDB[row].SteamEntry = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].SteamExhaust = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Order = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].MechDrive = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Tandem = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].Scrh = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].OutletToInletFlowRatio = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].Drwg = ""; col++; //workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].CouplDrwg = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].LastModified = DateTime.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? DateTime.MinValue.ToString() : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LD = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].LDBenchm = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].Owner = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].BP1 = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].BP2 = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].BP3 = workbook.Cells[i, col++].Value.ToString();
        executedProjectDB[row].MaxInletMassFlow = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        executedProjectDB[row].MaxExhaustMassFlow = double.Parse((workbook.Cells[i, col].Value?.ToString() == "-" || workbook.Cells[i, col].Value == null) ? "0" : workbook.Cells[i, col].Value.ToString());
        col++;
        string dat = (workbook.Cells[i, col].Value == null) ? "" : workbook.Cells[i, col].Value.ToString();
        executedProjectDB[row].DatFilePath = dat;
        ++row;
      }
    }
}
public class ExecutedProject
{
    // Private fields with underscores

    public ExecutedProject(){

    }
    private static ExecutedProject executedProjectDB;

    public static ExecutedProject getInstance(){
      if(executedProjectDB==null){
        executedProjectDB = new ExecutedProject();
      }
      return executedProjectDB;
    }
    private string _pNoRev;
    private string _projectID;
    private string _project;

    private string _pno;
    private string _gbc;
    private string _type;
    private string _application;
    private string _inletControl;
    private string _inletType;
    private string _size;
    private int _rpmMin;
    private int _rpmMax;
    private double _etaIt;
    private string _dhs;
    private double _couplePower;
    private double _liveSteamPressure;
    private double _liveSteamTemperature;
    private double _liveSteamMassFlow;
    private double _liveSteamVolumetricFlow;
    private double _wheelChamberPressure;
    private string _extraDeviceCss;
    private double _extr1Pressure;
    private double _extr2Pressure;
    private double _exhaustMassFlow;
    private double _exhaustTemp;
    private double _exhaustX;
    private double _exhaustPressure;
    private double _exhaustVolumetricFlow;
    private string _lpPart;
    private string _exhaust;
    private string _calcProgram;
    private string _abnDesign;
    private int _ageOfDesign;
    private double _bearDist;
    private string _cvSize;
    private string _esv;
    private string _esvSize;
    private int _numAdm;
    private int _numBleeds;
    private int _numCv;
    private int _numDrums;
    private int _numExtr;
    private int _numStages;
    private string _steamEntry;
    private string _steamExhaust;
    private string _order;
    private string _mechDrive;
    private string _tandem;
    private string _scrh;
    private double _outletToInletFlowRatio;
    private string _drwg;
    private string _couplDrwg;
    private DateTime _lastModified;
    private double _lD;
    private double _lDBenchm;
    private string _owner;
    private string _bp1;
    private string _bp2;
    private string _bp3;
    private double _maxInletMassFlow;
    private double _maxExhaustMassFlow;
    private string _datFilePath;
    // Properties
    public string PNo{
        get {return _pno;}
        set { _pno = value;}
    }
    public string PNoRev
    {
        get { return _pNoRev; }
        set { _pNoRev = value; }
    }
    public string ProjectID
    {
        get { return _projectID; }
        set { _projectID = value; }
    }
    public string Project
    {
        get { return _project; }
        set { _project = value; }
    }
    public string GBC
    {
        get { return _gbc; }
        set { _gbc = value; }
    }
    public string Type
    {
        get { return _type; }
        set { _type = value; }
    }
    public string Application
    {
        get { return _application; }
        set { _application = value; }
    }
    public string InletControl
    {
        get { return _inletControl; }
        set { _inletControl = value; }
    }
    public string InletType
    {
        get { return _inletType; }
        set { _inletType = value; }
    }
    public string Size
    {
        get { return _size; }
        set { _size = value; }
    }
    public int RpmMin
    {
        get { return _rpmMin; }
        set { _rpmMin = value; }
    }
    public int RpmMax
    {
        get { return _rpmMax; }
        set { _rpmMax = value; }
    }
    public double EtaIt
    {
        get { return _etaIt; }
        set { _etaIt = value; }
    }
    public string DHS
    {
        get { return _dhs; }
        set { _dhs = value; }
    }
    public double CouplePower
    {
        get { return _couplePower; }
        set { _couplePower = value; }
    }
    public double LiveSteamPressure
    {
        get { return _liveSteamPressure; }
        set { _liveSteamPressure = value; }
    }
    public double LiveSteamTemperature
    {
        get { return _liveSteamTemperature; }
        set { _liveSteamTemperature = value; }
    }
    public double LiveSteamMassFlow
    {
        get { return _liveSteamMassFlow; }
        set { _liveSteamMassFlow = value; }
    }
    public double LiveSteamVolumetricFlow
    {
        get { return _liveSteamVolumetricFlow; }
        set { _liveSteamVolumetricFlow = value; }
    }
    public double WheelChamberPressure
    {
        get { return _wheelChamberPressure; }
        set { _wheelChamberPressure = value; }
    }
    public string ExtraDeviceCss
    {
        get { return _extraDeviceCss; }
        set { _extraDeviceCss = value; }
    }
    public double Extr1Pressure
    {
        get { return _extr1Pressure; }
        set { _extr1Pressure = value; }
    }
    public double Extr2Pressure
    {
        get { return _extr2Pressure; }
        set { _extr2Pressure = value; }
    }
    public double ExhaustMassFlow
    {
        get { return _exhaustMassFlow; }
        set { _exhaustMassFlow = value; }
    }
    public double ExhaustTemp
    {
        get { return _exhaustTemp; }
        set { _exhaustTemp = value; }
    }
    public double ExhaustX
    {
        get { return _exhaustX; }
        set { _exhaustX = value; }
    }
    public double ExhaustPressure
    {
        get { return _exhaustPressure; }
        set { _exhaustPressure = value; }
    }
    public double ExhaustVolumetricFlow
    {
        get { return _exhaustVolumetricFlow; }
        set { _exhaustVolumetricFlow = value; }
    }
    public string LpPart
    {
        get { return _lpPart; }
        set { _lpPart = value; }
    }
    public string Exhaust
    {
        get { return _exhaust; }
        set { _exhaust = value; }
    }
    public string CalcProgram
    {
        get { return _calcProgram; }
        set { _calcProgram = value; }
    }
    public string AbnDesign
    {
        get { return _abnDesign; }
        set { _abnDesign = value; }
    }
    public int AgeOfDesign
    {
        get { return _ageOfDesign; }
        set { _ageOfDesign = value; }
    }
    public double BearDist
    {
        get { return _bearDist; }
        set { _bearDist = value; }
    }
    public string CvSize
    {
        get { return _cvSize; }
        set { _cvSize = value; }
    }
    public string ESV
    {
        get { return _esv; }
        set { _esv = value; }
    }
    public string ESVSize
    {
        get { return _esvSize; }
        set { _esvSize = value; }
    }
    public int NumAdm
    {
        get { return _numAdm; }
        set { _numAdm = value; }
    }
    public int NumBleeds
    {
        get { return _numBleeds; }
        set { _numBleeds = value; }
    }
    public int NumCv
    {
        get { return _numCv; }
        set { _numCv = value; }
    }
    public int NumDrums
    {
        get { return _numDrums; }
        set { _numDrums = value; }
    }
    public int NumExtr
    {
        get { return _numExtr; }
        set { _numExtr = value; }
    }
    public int NumStages
    {
        get { return _numStages; }
        set { _numStages = value; }
    }
    public string SteamEntry
    {
        get { return _steamEntry; }
        set { _steamEntry = value; }
    }
    public string SteamExhaust
    {
        get { return _steamExhaust; }
        set { _steamExhaust = value; }
    }
    public string Order
    {
        get { return _order; }
        set { _order = value; }
    }
    public string MechDrive
    {
        get { return _mechDrive; }
        set { _mechDrive = value; }
    }
    public string Tandem
    {
        get { return _tandem; }
        set { _tandem = value; }
    }
    public string Scrh
    {
        get { return _scrh; }
        set { _scrh = value; }
    }
    public double OutletToInletFlowRatio
    {
        get { return _outletToInletFlowRatio; }
        set { _outletToInletFlowRatio = value; }
    }
    public string Drwg
    {
        get { return _drwg; }
        set { _drwg = value; }
    }
    public string CouplDrwg
    {
        get { return _couplDrwg; }
        set { _couplDrwg = value; }
    }
    public DateTime LastModified
    {
        get { return _lastModified; }
        set { _lastModified = value; }
    }
    public double LD
    {
        get { return _lD; }
        set { _lD = value; }
    }
    public double LDBenchm
    {
        get { return _lDBenchm; }
        set { _lDBenchm = value; }
    }
    public string Owner
    {
        get { return _owner; }
        set { _owner = value; }
    }
    public string BP1
    {
        get { return _bp1; }
        set { _bp1 = value; }
    }
    public string BP2
    {
        get { return _bp2; }
        set { _bp2 = value; }
    }
    public string BP3
    {
        get { return _bp3; }
        set { _bp3 = value; }
    }
    public double MaxInletMassFlow
    {
        get { return _maxInletMassFlow; }
        set { _maxInletMassFlow = value; }
    }
    public double MaxExhaustMassFlow
    {
        get { return _maxExhaustMassFlow; }
        set { _maxExhaustMassFlow = value; }
    }
    public string DatFilePath
    {
        get { return _datFilePath; }
        set { _datFilePath = value; }
    }
}


