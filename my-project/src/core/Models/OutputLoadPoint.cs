namespace Models.TurbaOutputDataModel;
public class OutputLoadPoint{
    private string _loadPoint;
    private double _ERG_Line;
    private double _ABWEICHUNG; //remove % when inserting
    private double _DELTA_T;
    private double _max_exhaust_temperature;
    private double _Wheel_Chamber_Pressure;
    private double _Wheel_Chamber_Temperature;
    private double _FMIN1;
    private double _FMIN2;
    private double _DUESEN;
    private double _FMIN1_DUESEN;
    private double _FMIN2_DUESEN;
    private double _Vol_Flow;

    private string _Bending;

    private string _stagePressure;
    private double _Thrust;
    private double _Power_KW;
    private double _Efficiency;
    private double _Admission_Factor_Group1;
    private double _Admission_Factor_Group2;
    private double _Admission_Factor;
    private string _DUESEN_GRUPPE_AUSGEST;
    private double _FMIN_IST;
    private double _FMIN_SOLL;
    private double _WINKEL_ALFAA;
    private double _TEILUNG;
    private double _HOEHE;

    private string _LANG;

    private double _GBC_Length;

    private string _PSI;

    private double _LAUFZAHL;

    private double _BIEGESPANNUNG;

    private double _HSTAT;
    private double _HGES;
    private double _AK_LECKD_1;

    private double _AK_LECKD_2;

    private double _DT;

    private double _SAXA_SAXI;
    private string _BIEGESPANNUNG_TRUE_FALSE;

    private double _AZ;
    private double _AY;

    private string _AK_LECKDVERDICT;
    private string _HSTAT_MINUS_HGES;

    public string AK_LECKDVERDICT{
        get{ return _AK_LECKDVERDICT;}
        set{ _AK_LECKDVERDICT=value;}
    }
    public string HSTAT_MINUS_HGES{
        get{ return _HSTAT_MINUS_HGES;}
        set{ _HSTAT_MINUS_HGES =value;}
    }

    // private double _AENTW;
    // public 
    public string BIEGESPANNUNG_TRUE_FALSE{
        get{ return _BIEGESPANNUNG_TRUE_FALSE;}
        set{ _BIEGESPANNUNG_TRUE_FALSE=value;}
    }

    public double AZ{
        get{ return _AZ; } 
        set{ _AZ = value; }
    }
    public double AY{
        get{ return _AY; }
        set{ _AY = value; }
    }

    public double SAXA_SAXI{
        get{ return _SAXA_SAXI; }
        set { _SAXA_SAXI = value;}
    }

    public double DT{
        get{ return _DT; }
        set{ _DT = value; }
    }

    public double AKLECKD2{
        get{ return _AK_LECKD_2; }
        set{_AK_LECKD_2 = value; }
    }

    public double AKLECKD1{
        set { _AK_LECKD_1 = value;}
        get { return _AK_LECKD_1;}
    }


    public double HGES{
        get {  return _HGES;}
        set { _HGES = value; }
    }


    public double HSTAT{
        get { return _HSTAT; }  
        set { _HSTAT = value; } 
    }


    public double BIEGESPANNUNG{
        get { return _BIEGESPANNUNG;}
        set{  _BIEGESPANNUNG = value;}
    }



    public double LAUFZAHL{
        get { return _LAUFZAHL;}
        set {  _LAUFZAHL = value;}
    }
    public string PSI{
        get{ return _PSI;}
        set{ _PSI = value; }

    }

    public double GBC_Length{
        get{ return _GBC_Length;}
        set{ _GBC_Length = value;}
    }


    public string Lang {
         get { return _LANG;}
         set { _LANG = value; }
    }
   

    // Properties with get and set
    //ab COLUMN
    public double Max_Exhaust_Temperature{
        get { return _max_exhaust_temperature; }
        set { _max_exhaust_temperature = value; }
    }
    public string Bending{ get => _Bending; set => _Bending = value;}

    public string Stage_Pressure{ get => _stagePressure; set => _stagePressure = value;}
    public string LoadPoint { get => _loadPoint; set => _loadPoint = value; }
    public double ERG_Line { get => _ERG_Line; set => _ERG_Line = value; }
    public double ABWEICHUNG { get => _ABWEICHUNG; set => _ABWEICHUNG = value; }
    public double DELTA_T { get => _DELTA_T; set => _DELTA_T = value; }
    public double Wheel_Chamber_Pressure { get => _Wheel_Chamber_Pressure; set => _Wheel_Chamber_Pressure = value; }
    public double Wheel_Chamber_Temperature { get => _Wheel_Chamber_Temperature; set => _Wheel_Chamber_Temperature = value; }
    public double FMIN1 { get => _FMIN1; set => _FMIN1 = value; }
    public double FMIN2 { get => _FMIN2; set => _FMIN2 = value; }
    public double DUESEN { get => _DUESEN; set => _DUESEN = value; }
    public double FMIN1_DUESEN { get => _FMIN1_DUESEN; set => _FMIN1_DUESEN = value; }
    public double FMIN2_DUESEN { get => _FMIN2_DUESEN; set => _FMIN2_DUESEN = value; }
    public double Vol_Flow { get => _Vol_Flow; set => _Vol_Flow = value; }
    public double Thrust { get => _Thrust; set => _Thrust = value; }
    public double Power_KW { get => _Power_KW; set => _Power_KW = value; }
    public double Efficiency { get => _Efficiency; set => _Efficiency = value; }
    public double Admission_Factor_Group1 { get => _Admission_Factor_Group1; set => _Admission_Factor_Group1 = value; }
    public double Admission_Factor_Group2 { get => _Admission_Factor_Group2; set => _Admission_Factor_Group2 = value; }
    public double Admission_Factor { get => _Admission_Factor; set => _Admission_Factor = value; }
    public string DUESEN_GRUPPE_AUSGEST { get => _DUESEN_GRUPPE_AUSGEST; set => _DUESEN_GRUPPE_AUSGEST = value; }
    public double FMIN_IST { get => _FMIN_IST; set => _FMIN_IST = value; }
    public double FMIN_SOLL { get => _FMIN_SOLL; set => _FMIN_SOLL = value; }
    public double WINKEL_ALFAA { get => _WINKEL_ALFAA; set => _WINKEL_ALFAA = value; }
    public double TEILUNG { get => _TEILUNG; set => _TEILUNG = value; }
    public double HOEHE { get => _HOEHE; set => _HOEHE = value; }

}