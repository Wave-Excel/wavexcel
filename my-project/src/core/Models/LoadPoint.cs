using Models.LoadPointDataModel;
namespace Models.LoadPointDataModel;
public class LoadPoint{
        private double _massFlow;
        private double _rpm;
        private double _pressure;
        private double _temp;
        private double _inFlow; //EINSTROEM
        private double _backPress;
        private double _byp;
        private double _ein;
        private double _wanz;
        private double _rsmin;
        private string _lpName;

        public LoadPoint(){
        }
        
        // Properties
        public double MassFlow 
        { 
            get => _massFlow; 
            set => _massFlow = value; 
        }

        public double Rpm 
        { 
            get => _rpm; 
            set => _rpm = value; 
        }

        public double Pressure 
        { 
            get => _pressure; 
            set => _pressure = value; 
        }

        public double Temp 
        { 
            get => _temp; 
            set => _temp = value; 
        }

        public double InFlow 
        { 
            get => _inFlow; 
            set => _inFlow = value; 
        }

        public double BackPress 
        { 
            get => _backPress; 
            set => _backPress = value; 
        }

        public double BYP 
        { 
            get => _byp; 
            set => _byp = value; 
        }

        public double EIN 
        { 
            get => _ein; 
            set => _ein = value; 
        }

        public double WANZ 
        { 
            get => _wanz; 
            set => _wanz = value; 
        }

        public double RSMIN 
        { 
            get => _rsmin; 
            set => _rsmin = value; 
        }

        public string LPName 
        { 
            get => _lpName; 
            set => _lpName = value; 
        }
    }