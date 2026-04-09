using System.Runtime.InteropServices;
using System;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
namespace Models.TurbineData
{
    public class TurbineDataModel
    {
        private List<PowerNearest> listPower = new List<PowerNearest>();

        private List<string> dat_data;

        private double _inletPressure;
        private int _executedCountNumber;
        private double _exhaustPressure;
        private double _steamMass;
        private string _TurbineStatus = "OPT1";
        // private double _steamTemperature;
        private string _projectName;
        private double _massFlowRate;
        private double _inletTemperature;
        private double _leakagePressure = 1.015;
        private double _leakageTemperature;
        private double _oilLosses = 11.00;
        private double _inventoryLosses = 38.00;
        private double _margin = 0.5; // Margine = 0.5%
        private double _outletTemperature;
        private double _gearLosses;
        private double _AK25 = 0, _finalPower = 0.00;
        private double _inletVelocity;

        private double _volumetricFlow;

        private double _specificVolume;
        private double _generatorEfficiency, _turbineEfficiency;

        private double _ouletMassFlow;// = 4.93;

        private double _leakageMassFlow = 0.2;
        private double _inletEnthalphy;

        private double _outletEnthalphy;

        private string _tspId;
        private static TurbineDataModel turbineDataModel;
        private double _leakageEnthalphy;

        private int _noOfExecuted;
        private double _BEAUFSCHL;
        private double _RADKAMMER;
        private double _DRUCK;
        private double _INNNEN;
        private double _AUSGL;
        private string closestProjectName = "NA";
        private string closestProjectID = "0";
        private string _lpName = "0";

        private double _pst;

        private double _PDesh;

        private string _userName;

        private double _makeupTemp;// = 30;// = 30;// 30;

        private double _condretTemp;//= 50;// = 50;// 50;

        private double _processcondreturn;// = 80;// = 80;//80;

        private double _deaeratoroutlettemp;// = 125;// = 155;//144;

        private bool _dumpCondensor;// = true;

        private double _capacity;

        private bool _isPRVTemplate = true;

        private bool _oneLPMissing = false;

        private double _checkForCapacity;

        private double _oldNa = 0;

        private double _oldNb = 0;

        private bool _isScheneChange = false;

        private bool _isAK1Change = false;

        private double _ak1OldValue = 0;

        private double _ak1NewValue = 0;

        private string _datFilePath;



        public string DatFilePath
        {
            get
            {
                return _datFilePath;
            }
            set
            {
                _datFilePath = value;
            }
        }


        public double AK1OldValue
        {
            get
            {
                return _ak1OldValue;
            }
            set
            {
                _ak1OldValue = value;
            }
        }


        public double AK1NewValue
        {
            get
            {
                return _ak1NewValue;
            }
            set
            {
                _ak1NewValue = value;
            }
        }



        public bool IsAK1Change
        {
            get
            {
                return _isAK1Change;
            }
            set
            {
                _isAK1Change = value;
            }
        }

        public bool IsScheneChange
        {
            get
            {
                return _isScheneChange;
            }
            set
            {
                _isScheneChange = value;
            }
        }

        public double OldNa
        {
            get
            {
                return _oldNa;
            }
            set
            {
                _oldNa = value;
            }
        }
        public double OldNb
        {
            get
            {
                return _oldNb;
            }
            set
            {
                _oldNb = value;
            }
        }
        public double CheckForCapacity
        {
            get
            {
                return _checkForCapacity;
            }
            set
            {
                _checkForCapacity = value;
            }
        }
        public bool OneLPMissing
        {
            get
            {
                return _oneLPMissing;
            }
            set
            {
                _oneLPMissing = value;
            }
        }

        public bool DumpCondensor
        {
            get
            {
                return _dumpCondensor;
            }
            set
            {
                _dumpCondensor = value;
            }
        }


        public double Capacity
        {
            get
            {
                return _capacity;
            }
            set
            {
                _capacity = value;
            }
        }


        public bool IsPRVTemplate
        {
            get
            {
                return _isPRVTemplate;
            }
            set
            {
                _isPRVTemplate = value;
            }
        }


        public double DeaeratorOutletTemp
        {
            get
            {
                return _deaeratoroutlettemp;
            }
            set
            {
                _deaeratoroutlettemp = value;
            }
        }
        public double ProcessCondReturn
        {
            get
            {
                return _processcondreturn;
            }
            set
            {
                _processcondreturn = value;
            }
        }
        public double CondRetTemp
        {
            get
            {
                return _condretTemp;
            }
            set
            {
                _condretTemp = value;
            }
        }


        public double MakeUpTempe
        {
            get
            {
                return _makeupTemp;
            }
            set
            {
                _makeupTemp = value;
            }
        }

        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }

        public string TSPID
        {
            get { return _tspId; }
            set
            {
                _tspId = value;
            }
        }
        
        public string LpName
        {
            get { return _lpName; }
            set
            {
                _lpName = value;
            }
        }
        public double PST
        {
            get { return _pst; }
            set
            {
                _pst = value;
            }
        }
        public double PDESH
        {
            get { return _PDesh; }
            set
            {
                _PDesh = value;
            }
        }
        public string ClosestProjectName
        {
            get { return closestProjectName; }
            set { closestProjectName = value; }
        }

        // Public property for closestProjectID
        public string ClosestProjectID
        {
            get { return closestProjectID; }
            set { closestProjectID = value; }
        }
        public string TurbineStatus
        {
            get { return _TurbineStatus; }
            set { _TurbineStatus = value; }
        }

        public double BEAUFSCHL
        {
            get { return _BEAUFSCHL; }
            set { _BEAUFSCHL = value; }
        }
        public double RADKAMMER
        {
            get { return _RADKAMMER; }
            set { _RADKAMMER = value; }
        }
        public double DRUCK
        {
            get { return _DRUCK; }
            set { _DRUCK = value; }
        }
        public double INNNEN
        {
            get { return _INNNEN; }
            set { _INNNEN = value; }
        }
        public double AUSGL
        {
            get { return _AUSGL; }
            set { _AUSGL = value; }
        }
        public int NoOfExecuted
        {
            get { return _noOfExecuted; }
            set { _noOfExecuted = value; }
        }
        public void fillPowerNearestData()
        {
            for (int i = 1; i <= _noOfExecuted + 5; ++i)
            {
                listPower.Add(new PowerNearest());
            }
        }
        public List<string> DAT_DATA
        {
            get { return dat_data; }
            set { dat_data = value; }
        }
        public List<PowerNearest> ListPower
        {
            get { return listPower; }
            set { listPower = value; }
        }
        public int ExecutedCountNumber
        {
            get { return _executedCountNumber; }
            set { _executedCountNumber = value; }
        }
        public double LeakagePressure
        {
            get { return _leakagePressure; }
            set { _leakagePressure = value; }
        }
        public double LeakageEnthalphy
        {
            get { return _leakageEnthalphy; }
            set { _leakageEnthalphy = value; }
        }
        public double LeakageTemperature
        {
            get { return _leakageTemperature; }
            set { _leakageTemperature = value; }
        }

        public double InletEnthalphy
        {
            get { return _inletEnthalphy; }
            set { _inletEnthalphy = value; }
        }
        public double OutletEnthalphy
        {
            get { return _outletEnthalphy; }
            set { _outletEnthalphy = value; }
        }
        public double LeakageMassFlow
        {
            get { return _leakageMassFlow; }
            set { _leakageMassFlow = value; }
        }
        public double OutletMassFlow
        {
            get { return _ouletMassFlow; }
            set { _ouletMassFlow = value; }
        }
        public double SpecificVolume
        {
            get { return _specificVolume; }
            set { _specificVolume = value; }
        }
        public double InletPressure
        {
            get { return _inletPressure; }
            set { _inletPressure = value; }
        }

        public double VolumetricFlow
        {
            get { return _volumetricFlow; }
            set { _volumetricFlow = value; }
        }
        public double GeneratorEfficiency
        {
            get { return _generatorEfficiency; }
            set { _generatorEfficiency = value; }
        }

        public double TurbineEfficiency
        {
            get { return _turbineEfficiency; }
            set { _turbineEfficiency = value; }
        }
        public double InletVelocity
        {
            get { return _inletVelocity; }
            set { _inletVelocity = value; }
        }

        public double ExhaustPressure
        {
            get { return _exhaustPressure; }
            set { _exhaustPressure = value; }
        }

        public double SteamMass
        {
            get { return _steamMass; }
            set { _steamMass = value; }
        }

        public double InletTemperature
        {
            get { return _inletTemperature; }
            set { _inletTemperature = value; }
        }

        public string ProjectName
        {
            get { return _projectName; }
            set { _projectName = value; }
        }

        public double MassFlowRate
        {
            get { return _massFlowRate; }
            set { _massFlowRate = value; }
        }

        // public double LeakagePressure
        // {
        //     get { return _leakagePressure; }
        //     set { _leakagePressure = value; }
        // }

        public double OilLosses
        {
            get { return _oilLosses; }
            set { _oilLosses = value; }
        }

        public double InventoryLosses
        {
            get { return _inventoryLosses; }
            set { _inventoryLosses = value; }
        }

        public double Margin
        {
            get { return _margin; }
            set { _margin = value; }
        }

        public double OutletTemperature
        {
            get { return _outletTemperature; }
            set { _outletTemperature = value; }
        }

        public double GearLosses
        {
            get { return _gearLosses; }
            set { _gearLosses = value; }
        }

        public double AK25
        {
            get { return _AK25; }
            set { _AK25 = value; }
        }

        public double FinalPower
        {
            get { return _finalPower; }
            set { _finalPower = value; }
        }


        private TurbineDataModel()
        {
        }

        public TurbineDataModel(double steamPressure, double exhaustPressure, double steamMass, string projectName)
        {
            this._inletPressure = steamPressure;
            this._exhaustPressure = exhaustPressure;
            this._steamMass = steamMass;
            // this._steamTemperature = steamTemperature;
            this._projectName = projectName;
        }
        public static void ResetInstance()
        {
            if(turbineDataModel != null)
            {
                turbineDataModel = null;
            }
        }
        public static TurbineDataModel getInstance()
        {
            if (turbineDataModel == null)
            {
                turbineDataModel = new TurbineDataModel();
            }
            return turbineDataModel;
        }
    }

    public class PowerNearest
    {
        private double _efficiency;
        private string _projectName;
        private string _projectId;
        private double _power;
        private double _steamMass;
        private double _steamPressure;
        private double _exhaustPressure;
        private double _steamTemperature;
        private string _kNearest;
        private int _bcd;

        public double Efficiency
        {
            get { return _efficiency; }
            set { _efficiency = value; }
        }
        public string ProjectName
        {
            get { return _projectName; }
            set { _projectName = value; }
        }
        public string ProjectID { 
            get { return _projectId; }
            set { _projectId = value; }
        }

        public double Power
        {
            get { return _power; }
            set { _power = value; }
        }

        public double SteamMass
        {
            get { return _steamMass; }
            set { _steamMass = value; }
        }

        public double SteamPressure
        {
            get { return _steamPressure; }
            set { _steamPressure = value; }
        }

        public double ExhaustPressure
        {
            get { return _exhaustPressure; }
            set { _exhaustPressure = value; }
        }

        public double SteamTemperature
        {
            get { return _steamTemperature; }
            set { _steamTemperature = value; }
        }

        public string KNearest
        {
            get { return _kNearest; }
            set { _kNearest = value; }
        }

        public int Bcd
        {
            get { return _bcd; }
            set { _bcd = value; }
        }


    }


}