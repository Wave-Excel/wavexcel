using System.Diagnostics;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
//using static Android.Icu.Text.CaseMap;

namespace Models.TurbaOutputDataModel{
  public class TurbaOutputModel{
    private List<OutputLoadPoint> outputDataList;
    private static TurbaOutputModel turbaOutputModel;

    private IConfiguration configuration;
    private string _Check_DELTA_T;
    private string _Check_Wheel_Chamber_Pressure;
    private string _Check_Wheel_Chamber_Temperature;

    // private double _Thrust;


    private string _checkVolFlow;
    private string _BendingCheck;
    private string _Stage_Pressure_Check;

    private string _Check_Thrust;
    private string _Check_Power_KW;

    private double deltaT_UpperLimit, deltaT_LowerLimit;
    private double abweichung_LowerLimit, abweichung_UpperLimit;

    private double wheelchamberP_LowerLimit, wheelchamberP_UpperLimit, wheelchamberP_UpperLimit2;
    private double wheelchamberT_LowerLimit, wheelchamberT_UpperLimit, wheelchamberT_UpperLimit2;
    private double fmin1_LowerLimit, fmin1_UpperLimit;
    private double fmin2_LowerLimit, fmin2_UpperLimit;
        private double _thurstlimit;
    private double volFlow_LowerLimit, volFlow_UpperLimit;

    private string _check_BIEGESPANNUNG_TRUE_FALSE;
    private string _Check_HOEHE;
    private string _check_ABWEICHUNG;
    private string _check_BIEGESPANNUNG;
    

    private string _Check_FMIN1;
    private string _check_FMIN2;
    private string _check_DUESEN;
    private double _H40;
    private double _H41;
    private double _AA41;
    private double _H30;
    private double _H31;

    private double _G40;
    private double _E41=1;
    private double _G41=1;
    private double _P41=1;

    private double _AC41=0.1;
    private double _AE41 =1;
    private double _AF41=1;

    

    



    private string _check_AENTW;
    private string _check_AK_LECKDVERDICT;
    private string _check_HSTAT_MINUS_HGES;
    private string _check_DT;
    private string _check_SAXA_MINUS_SAXI;
    private string _check_Efficiency;
    private string _check_LAUFZAHL;
    private string _check_HSTAT;
    private string _check_HGES;
    private string _check_AK_LECKD1;
    private string _check_AK_LECKD2;
    private string _check_FMIN1_DUESEN;
    private string _check_FMIN2_DUESEN;
    private string _check_Admission_Factor_Group1;
    private string _check_AdmissionNFactor;
    private string _check_Admission_Factor_Group2;
    private string _check_DUESEN_GRUPPE_AUSGEST;
    private string _check_FMIN_IST;
    private string _check_FMIN_SOLL;
    private string _check_WINKELALFAA;
    private string _check_TEILUNG;
    private string _check_Max_Exhaust_Temperature;
    private string _check_ExhaustPressureBackPress;
    public string Check_BIEGESPANNUNG{
        get{return _check_BIEGESPANNUNG;}
        set{_check_BIEGESPANNUNG=value;}
    }
    public double E41
    {
        get { return _E41; }
    }

    // Getter for _G41
    public double G41
    {
        get { return _G41; }
    }

    // Getter for _P41
    public double P41
    {
        get { return _P41; }
    }

    // Getter for _AC41
    public double AC41
    {
        get { return _AC41; }
    }

    // Getter for _AE41
    public double AE41
    {
        get { return _AE41; }
    }

    // Getter for _AF41
    public double AF41
    {
        get { return _AF41; }
    }

    public double H41{
        get{ return _H41; }
        set{ _H41 = value;}
    }
    public double AA41{
        get{ return _AA41; }
        set{ _AA41 = value;}
    }
    public string Check_TEILUNG
        {
            get { return _check_TEILUNG; }
            set { _check_TEILUNG = value; }
        }
       
        public string Check_Max_Exhaust_Temperature
        {
            get { return _check_Max_Exhaust_Temperature; }
            set { _check_Max_Exhaust_Temperature = value; }
        }
 
        public string Check_ExhaustPressureBackPress
        {
            get { return _check_ExhaustPressureBackPress; }
            set { _check_ExhaustPressureBackPress = value; }
        }
    public string Check_DUESEN_GRUPPE_AUSGEST
        {
            get { return _check_DUESEN_GRUPPE_AUSGEST; }
            set { _check_DUESEN_GRUPPE_AUSGEST = value; }
        }
 
        public string Check_FMIN_IST
        {
            get { return _check_FMIN_IST; }
            set { _check_FMIN_IST = value; }
        }
 
        public string Check_FMIN_SOLL
        {
            get { return _check_FMIN_SOLL; }
            set { _check_FMIN_SOLL = value; }
        }
 
        public string Check_WINKELALFAA
        {
            get { return _check_WINKELALFAA; }
            set { _check_WINKELALFAA = value; }
        }

    public string Check_Admission_Factor_Group1
        {
            get { return _check_Admission_Factor_Group1; }
            set { _check_Admission_Factor_Group1 = value; }
        }
 
        public string Check_Admission_Factor_Group2
        {
            get { return _check_Admission_Factor_Group2; }
            set { _check_Admission_Factor_Group2 = value; }
        }
 
        public string Check_AdmissionNFactor
        {
            get { return _check_AdmissionNFactor; }
            set { _check_AdmissionNFactor = value; }
        }
    public string Check_FMIN1_DUESEN
        {
            get { return _check_FMIN1_DUESEN; }
            set { _check_FMIN1_DUESEN = value; }
        }
 
        public string Check_FMIN2_DUESEN
        {
            get { return _check_FMIN2_DUESEN; }
            set { _check_FMIN2_DUESEN = value; }
        }



    public string Check_Efficiency
        {
            get { return _check_Efficiency; }
            set { _check_Efficiency = value; }
        }
    public string Check_AK_LECKD1
        {
            get { return _check_AK_LECKD1; }
            set { _check_AK_LECKD1 = value; }
        }
 
        public string Check_AK_LECKD2
        {
            get { return _check_AK_LECKD2; }
            set { _check_AK_LECKD2 = value; }
        }
    public string Check_LAUFZAHL
        {
            get { return _check_LAUFZAHL; }
            set { _check_LAUFZAHL = value; }
        }
    public string Check_HSTAT
        {
            get { return _check_HSTAT; }
            set { _check_HSTAT = value; }
        }
 
        public string Check_HGES
        {
            get { return _check_HGES; }
            set { _check_HGES = value; }
        }
    public string Check_FMIN2
        {
            get { return _check_FMIN2; }
            set { _check_FMIN2 = value; }
        }
 
    public string Check_ABWEICHUNG
        {
            get { return _check_ABWEICHUNG; }
            set { _check_ABWEICHUNG = value; }
        }
    public string Check_SAXA_MINUS_SAXI
        {
            get { return _check_SAXA_MINUS_SAXI; }
            set { _check_SAXA_MINUS_SAXI = value; }
        }
        public string Check_DUESEN
        {
            get { return _check_DUESEN; }
            set { _check_DUESEN = value; }
        }
    public string Check_DT
        {
            get { return _check_DT; }
            set { _check_DT = value; }
        }
     public string Check_HSTAT_MINUS_HGES
        {
            get { return _check_HSTAT_MINUS_HGES; }
            set { _check_HSTAT_MINUS_HGES = value; }
        }
    public string Check_AK_LECKDVERDICT
        {
            get { return _check_AK_LECKDVERDICT; }
            set { _check_AK_LECKDVERDICT = value; }
        }

    public string Check_AENTW
        {
            get { return _check_AENTW; }
            set { _check_AENTW = value; }
        }

    public double G40{
        get{ return _G40; }
        set{ _G40 = value; }
    }

    public double H30{
        get{ return _H30; }
        set{ _H30 = value; }
    }
    public double H31{
        get{ return _H31; }
        set{ _H31 = value; }
    }
    public string Check_BIEGESPANNUNG_TRUE_FALSE
        {
            get { return _check_BIEGESPANNUNG_TRUE_FALSE; }
            set { _check_BIEGESPANNUNG_TRUE_FALSE = value; }
        }
    
    private double _AF40;

    private double _P40;
    public double P40{
        get{ return _P40; }
        set{ _P40 = value; }
    }
    private double _E40;
    
    public double E40{
        get{ return _E40; }
        set{ _E40 = value; }
    }

    private string _Check_PSI;

    private string _Check_GBC_Length;
    
    public string Check_GBC_Length{
        get { return _Check_GBC_Length;}
        set { _Check_GBC_Length =value;}
    }
    private double _AC40;

    public double AC40{
        get { return _AC40; }
        set { _AC40 = value;}
    }

    public string Check_PSI{
        get{ return _Check_PSI; }
        set{ _Check_PSI = value;}
    }
    private double _AE40;

    public double AE40{
        get{ return _AE40; }
        set{ _AE40 = value;}
    }
    public double AF40{
        get{ return _AF40; }
        set{ _AF40 = value; }
    }

    private string _Check_Lang;

    public string Check_Lang{
        get{ return _Check_Lang; }
        set{ _Check_Lang = value; }
    }
    public string Check_FMIN1{
        get{ return _Check_FMIN1; } 
        set{ _Check_FMIN1 = value;}
    }

    public double H40{
        get{ return _H40; }
        set{ _H40 = value; }
    }
    private double _AA40;

    public double AA40{
        get{ return _AA40; }
        set{ _AA40 = value; }
    }

    public string Check_HOEHE { 
        get{ return _Check_HOEHE; }
        set{ _Check_HOEHE = value;}
    }

    private TurbaOutputModel(){
      
      configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            //using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
            //{
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        // Split the line by comma
            //        var values = line.Split(',');

            //        // Process the values (for example, print them)
            //        //foreach (var value in values)
            //        //{
            //        //    Console.Write(value + " ");
            //        //}

            //        if (values[0] == "ABWEICHUNG LOWER")
            //        {
            //            Debug.WriteLine("ABWEICHUNG LOWER" + Convert.ToDouble(values[1]));
            //            abweichung_LowerLimit = Convert.ToDouble(values[1]);
            //        }
            //        else if (values[0] == "ABWEICHUNG HIGHER")
            //        {
            //            Debug.WriteLine("ABWEICHUNG HIGHER" + Convert.ToDouble(values[1]));
            //            abweichung_UpperLimit = Convert.ToDouble(values[1]);
            //        }
            //        else if (values[0] == "FMIN2 MAX")
            //        {
            //            fmin1_UpperLimit = Convert.ToDouble(values[1]);
            //        }
            //        else if (values[0] == "FMIN1 MAX")
            //        {
            //            fmin2_UpperLimit = Convert.ToDouble(values[1]);
            //        }
            //        else if (values[0] == "Exhaust Volumetric Flow Upper Limit")
            //        {
            //            volFlow_UpperLimit = Convert.ToDouble(values[1]);
            //        }
            //        else if (values[0] == "Axial Thrust")
            //        {
            //            Debug.WriteLine("tHRUST" + values[1]);
            //            _thurstlimit = Convert.ToDouble(values[1]);
            //        }


            //    }
            //}
      deltaT_UpperLimit = 240.00;
      deltaT_LowerLimit = 240.00;
      //abweichung_LowerLimit = 2;
      //abweichung_UpperLimit = 7;
      wheelchamberP_LowerLimit = 30.00;

      wheelchamberP_UpperLimit = 32.00;
      wheelchamberP_UpperLimit2 = 33.00;
      wheelchamberT_LowerLimit = 300;
            wheelchamberT_UpperLimit = 410;
            wheelchamberT_UpperLimit2 = 420;
            fmin1_LowerLimit = 1400;
      //fmin1_UpperLimit = 1400;
      fmin2_LowerLimit = 1300;
      //fmin2_UpperLimit = 1300;
      volFlow_LowerLimit = 3.9;
      //volFlow_UpperLimit = 7.7;
    }


    // Getter and Setter for deltaT_UpperLimit
    public double DeltaT_UpperLimit
    {
        get { return deltaT_UpperLimit; }
        set { deltaT_UpperLimit = value; }
    }

    // Getter and Setter for deltaT_LowerLimit
    public double DeltaT_LowerLimit
    {
        get { return deltaT_LowerLimit; }
        set { deltaT_LowerLimit = value; }
    }
    public double ThrustLimit
        {
            get { return _thurstlimit; }
            set { _thurstlimit = value; }
        }

    // Getter and Setter for abweichung_UpperLimit
    public double Abweichung_UpperLimit
    {
        get { return abweichung_UpperLimit; }
        set { abweichung_UpperLimit = value; }
    }

    // Getter and Setter for abweichung_LowerLimit
    public double Abweichung_LowerLimit
    {
        get { return abweichung_LowerLimit; }
        set { abweichung_LowerLimit = value; }
    }

    // Getter and Setter for wheelchamberP_UpperLimit
    public double WheelchamberP_UpperLimit
    {
        get { return wheelchamberP_UpperLimit; }
        set { wheelchamberP_UpperLimit = value; }
    }

    // Getter and Setter for wheelchamberP_UpperLimit
    public double WheelchamberP_UpperLimit2
    {
        get { return wheelchamberP_UpperLimit2; }
        set { wheelchamberP_UpperLimit2 = value; }
    }

    // Getter and Setter for wheelchamberP_LowerLimit
    public double WheelchamberP_LowerLimit
    {
        get { return wheelchamberP_LowerLimit; }
        set { wheelchamberP_LowerLimit = value; }
    }

    // Getter and Setter for wheelchamberT_UpperLimit
    public double WheelchamberT_UpperLimit
    {
        get { return wheelchamberT_UpperLimit; }
        set { wheelchamberT_UpperLimit = value; }
    }

    public double WheelchamberT_UpperLimit2
    {
        get { return wheelchamberT_UpperLimit2; }
        set { wheelchamberT_UpperLimit2 = value; }
    }

    // Getter and Setter for wheelchamberT_LowerLimit
    public double WheelchamberT_LowerLimit
    {
        get { return wheelchamberT_LowerLimit; }
        set { wheelchamberT_LowerLimit = value; }
    }

    // Getter and Setter for fmin1_UpperLimit
    public double Fmin1_UpperLimit
    {
        get { return fmin1_UpperLimit; }
        set { fmin1_UpperLimit = value; }
    }

    // Getter and Setter for fmin1_LowerLimit
    public double Fmin1_LowerLimit
    {
        get { return fmin1_LowerLimit; }
        set { fmin1_LowerLimit = value; }
    }

    // Getter and Setter for fmin2_UpperLimit
    public double Fmin2_UpperLimit
    {
        get { return fmin2_UpperLimit; }
        set { fmin2_UpperLimit = value; }
    }

    // Getter and Setter for fmin2_LowerLimit
    public double Fmin2_LowerLimit
    {
        get { return fmin2_LowerLimit; }
        set { fmin2_LowerLimit = value; }
    }

    // Getter and Setter for volFlow_UpperLimit
    public double VolFlow_UpperLimit
    {
        get { return volFlow_UpperLimit; }
        set { volFlow_UpperLimit = value; }
    }

    // Getter and Setter for volFlow_LowerLimit
    public double VolFlow_LowerLimit
    {
        get { return volFlow_LowerLimit; }
        set { volFlow_LowerLimit = value; }
    }


    public string CheckVolFlow { get => _checkVolFlow; set => _checkVolFlow = value;}
    public string Check_DELTA_T { get => _Check_DELTA_T; set => _Check_DELTA_T = value; }

    public string Check_Wheel_Chamber_Pressure { get => _Check_Wheel_Chamber_Pressure; set => _Check_Wheel_Chamber_Pressure = value; }

    public string Check_Wheel_Chamber_Temperature { get => _Check_Wheel_Chamber_Temperature; set => _Check_Wheel_Chamber_Temperature = value; }
    public string BendingCheck { get => _BendingCheck; set => _BendingCheck = value; }
    public string Stage_Pressure_Check { get => _Stage_Pressure_Check; set => _Stage_Pressure_Check = value; }

    public string Check_Thrust { get => _Check_Thrust; set => _Check_Thrust = value; }

    public string Check_Power_KW { get => _Check_Power_KW; set => _Check_Power_KW = value; }



    public List<OutputLoadPoint> OutputDataList
    {
        get { return outputDataList; }
        set { outputDataList = value; }
    }
    public static TurbaOutputModel getInstance(){
      if(turbaOutputModel == null){
        turbaOutputModel = new TurbaOutputModel();
      }
      return turbaOutputModel;
    }

    public void fillTurbaOutputDataList(){
        int loadPointsCount = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
        outputDataList = new List<OutputLoadPoint>();//need to fill it
        for(int i = 1; i <= loadPointsCount + 20; ++i){
         outputDataList.Add(new OutputLoadPoint());
         }

            using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Split the line by comma
                    var values = line.Split(',');

                    // Process the values (for example, print them)
                    //foreach (var value in values)
                    //{
                    //    Console.Write(value + " ");
                    //}

                    if (values[0] == "ABWEICHUNG LOWER")
                    {
                        Debug.WriteLine("ABWEICHUNG LOWER" + Convert.ToDouble(values[1]));
                        abweichung_LowerLimit = Convert.ToDouble(values[1]);
                    }
                    else if (values[0] == "ABWEICHUNG HIGHER")
                    {
                        Debug.WriteLine("ABWEICHUNG HIGHER" + Convert.ToDouble(values[1]));
                        abweichung_UpperLimit = Convert.ToDouble(values[1]);
                    }
                    else if (values[0] == "FMIN2 MAX")
                    {
                        fmin2_UpperLimit = Convert.ToDouble(values[1]);
                    }
                    else if (values[0] == "FMIN1 MAX")
                    {
                        fmin1_UpperLimit = Convert.ToDouble(values[1]);
                    }
                    else if (values[0] == "Exhaust Volumetric Flow Upper Limit")
                    {
                        volFlow_UpperLimit = Convert.ToDouble(values[1]);
                    }
                    else if (values[0] == "Axial Thrust")
                    {
                        Debug.WriteLine("thrustttt" + values[1]);
                        _thurstlimit = Convert.ToDouble(values[1]);
                    }


                }
            }
        }
  }
}