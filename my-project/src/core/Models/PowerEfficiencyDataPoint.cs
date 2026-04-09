namespace Models.PowerEfficiencyData;
class PowerEfficiencyDataPoint{
        private double power; //in MW
        private double eff_100, eff_75, eff_50, eff_25; //make it divide by 100 before passing

        public PowerEfficiencyDataPoint(double power, double eff_100, double eff_75, double eff_50, double eff_25)
        {
            this.power = power;
            this.eff_100 = eff_100;
            this.eff_75 = eff_75;
            this.eff_50 = eff_50;
            this.eff_25 = eff_25;
        }

        public PowerEfficiencyDataPoint(){
            
        }
        public double Power
        {
            get { return power; }
            set { power = value; }
        }

        public double Eff100
        {
            get { return eff_100; }
            set { eff_100 = value; }
        }

        public double Eff75
        {
            get { return eff_75; }
            set { eff_75 = value; }
        }

        public double Eff50
        {
            get { return eff_50; }
            set { eff_50 = value; }
        }

        public double Eff25
        {
            get { return eff_25; }
            set { eff_25 = value; }
        }
    }

    