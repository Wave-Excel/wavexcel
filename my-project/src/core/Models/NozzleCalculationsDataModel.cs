namespace Models.NozzleDataModel
{
    public class NozzleCalculationsData
    {
        private static NozzleCalculationsData nozzleCalculationsData;
        // Private fields with underscore prefix
        private double _NA;
        private double _nozzle_Total;
        private double _HOEHE;
        private double _TEILUNG;
        private double _WIWINKEL_ALFAA;
        private double _FMIN_IST_V1_V2;
        private double _FMIN_IST_V1;
        private double _FMIN_SOLL_LP1;
        private double _FMIN_SOLL_LP6;

        private double _E_VALUE_LP1;
        private double _Calculated_IST_LP1;
        private double _Calculates_IST_LP6;
        private double _Deviation_LP1;
        private double _Deviation_LP6;
        private double _Allowed_LP1;
        private double _Allowed_LP6;
        private double _Cal_Nozzle_Total;
        private double _Cal_NA;
        private double _Cal_NB;
        // Constructor
        public NozzleCalculationsData(
            double NA,
            double nozzle_Total,
            double HOEHE,
            double TEILUNG,
            double WIWINKEL_ALFAA,
            double FMIN_IST_V1_V2,
            double FMIN_IST_V1,
            double FMIN_SOLL_LP1,
            double FMIN_SOLL_LP6)
        {
            _NA = NA;
            _nozzle_Total = nozzle_Total;
            _HOEHE = HOEHE;
            _TEILUNG = TEILUNG;
            _WIWINKEL_ALFAA = WIWINKEL_ALFAA;
            _FMIN_IST_V1_V2 = FMIN_IST_V1_V2;
            _FMIN_IST_V1 = FMIN_IST_V1;
            _FMIN_SOLL_LP1 = FMIN_SOLL_LP1;
            _FMIN_SOLL_LP6 = FMIN_SOLL_LP6;
            _E_VALUE_LP1=Math.Sin((_WIWINKEL_ALFAA * Math.PI) / 180.00) * _TEILUNG;
            _Calculated_IST_LP1=_E_VALUE_LP1*_nozzle_Total*_HOEHE;
            _Calculates_IST_LP6=_E_VALUE_LP1*_NA*_HOEHE;
            _Deviation_LP1=_Calculated_IST_LP1/_FMIN_IST_V1_V2;
            _Deviation_LP6=_Calculates_IST_LP6/_FMIN_IST_V1;
            _Allowed_LP1 = _FMIN_IST_V1_V2*1.02;
            _Allowed_LP6 = _FMIN_IST_V1;
            // MAY be +1 in Future
            _Cal_Nozzle_Total=  Math.Round((1.0200*_FMIN_SOLL_LP1)/(_HOEHE*_E_VALUE_LP1),0);
            _Cal_NA=Math.Round(_FMIN_SOLL_LP6/(_HOEHE*_E_VALUE_LP1),0);
            _Cal_NB=_Cal_Nozzle_Total-_Cal_NA;
        }

        public NozzleCalculationsData(){
        }

        // Properties
        public double NA
        {
            get => _NA;
            set => _NA = value;
        }

        public double nozzle_Total
        {
            get => _nozzle_Total;
            set => _nozzle_Total = value;
        }

        public double HOEHE
        {
            get => _HOEHE;
            set => _HOEHE = value;
        }

        public double TEILUNG
        {
            get => _TEILUNG;
            set => _TEILUNG = value;
        }

        public double WIWINKEL_ALFAA
        {
            get => _WIWINKEL_ALFAA;
            set => _WIWINKEL_ALFAA = value;
        }

        public double FMIN_IST_V1_V2
        {
            get => _FMIN_IST_V1_V2;
            set => _FMIN_IST_V1_V2 = value;
        }

        public double FMIN_IST_V1
        {
            get => _FMIN_IST_V1;
            set => _FMIN_IST_V1 = value;
        }

        public double FMIN_SOLL_LP1
        {
            get => _FMIN_SOLL_LP1;
            set => _FMIN_SOLL_LP1 = value;
        }

        public double FMIN_SOLL_LP6
        {
            get => _FMIN_SOLL_LP6;
            set => _FMIN_SOLL_LP6 = value;
        }
         public double E_VALUE_LP1
        {
            get => _E_VALUE_LP1;
            set => _E_VALUE_LP1 = value;
        }

        public double Calculated_IST_LP1
        {
            get => _Calculated_IST_LP1;
            set => _Calculated_IST_LP1 = value;
        }

        public double Calculates_IST_LP6
        {
            get => _Calculates_IST_LP6;
            set => _Calculates_IST_LP6 = value;
        }

        public double Deviation_LP1
        {
            get => _Deviation_LP1;
            set => _Deviation_LP1 = value;
        }

        public double Deviation_LP6
        {
            get => _Deviation_LP6;
            set => _Deviation_LP6 = value;
        }

        public double Allowed_LP1
        {
            get => _Allowed_LP1;
            set => _Allowed_LP1 = value;
        }

        public double Allowed_LP6
        {
            get => _Allowed_LP6;
            set => _Allowed_LP6 = value;
        }

        public double Cal_Nozzle_Total
        {
            get => _Cal_Nozzle_Total;
            set => _Cal_Nozzle_Total = value;
        }

        public double Cal_NA
        {
            get => _Cal_NA;
            set => _Cal_NA = value;
        }

        public double Cal_NB
        {
            get => _Cal_NB;
            set => _Cal_NB = value;
        }

        public static NozzleCalculationsData getInstance(){
            if(nozzleCalculationsData == null){
                nozzleCalculationsData = new NozzleCalculationsData();
            }
            return nozzleCalculationsData;
        }

        public void fillNozzleData(){
        
            this._E_VALUE_LP1=Math.Sin((this._WIWINKEL_ALFAA * Math.PI) / 180.00) * this._TEILUNG;
            this._Calculated_IST_LP1=this._E_VALUE_LP1*this._nozzle_Total*this._HOEHE;
            this._Calculates_IST_LP6=this._E_VALUE_LP1*this._NA*this._HOEHE;
            this._Deviation_LP1=this._Calculated_IST_LP1/this._FMIN_IST_V1_V2;
            this._Deviation_LP6=this._Calculates_IST_LP6/this._FMIN_IST_V1;
            this._Allowed_LP1 = this._FMIN_IST_V1_V2*1.02;
            this._Allowed_LP6 = this._FMIN_IST_V1;
            this._Cal_Nozzle_Total=  Math.Ceiling((1.0200*this._FMIN_SOLL_LP1)/(this._HOEHE*this._E_VALUE_LP1));
            this._Cal_NA=Math.Ceiling(this._FMIN_SOLL_LP6/(this._HOEHE*_E_VALUE_LP1));
            this._Cal_NB = this._Cal_Nozzle_Total-this._Cal_NA;
        }
    }
}
