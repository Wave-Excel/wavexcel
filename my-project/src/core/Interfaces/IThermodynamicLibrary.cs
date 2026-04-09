namespace Interfaces.IThermodynamicLibrary;

  public interface IThermodynamicLibrary{

    double tsatvonp(double p1);

    double psatvont(double t1);
    // double getClosestEfficiency(List<DataPoint> dataPoints, double pressure1, double pressure2, double massFlowRate, double temperature);
    double getNormalisedValue(string columnName, double inputValue, string filePath);

    double getMassFlowFromPower(double power, double pressure1, double pressure2, double temperature1, double massFlow, double closestEfficiency, double generatorEff);
    double GetMassFlowUsingKreislPower(double neededPower, double lower_limit, double higher_limit);


    double getInletEnthalpy(double pressure, double temperature);

    double getOutletTemperature(double pressure, double enthalpy);

    double getSpecificVolume();

    double GetInletVelocity(double massinKg);


    double getOutletEnthalpy(double pressure, double enthalpy1, double efficiency, double pressure1);

     double getVolumetricFlow();

     double getVolumetricFlow(double mass_Flow_Rate, double specificVolume);

     double findPowerOfTurbine(double enthalpy1, double enthalpy2, double massFlowRate, double oilLosses, double gearLosses);

      double getPowerFromClosestEfficiency(double power, double eff);

      string FillClosestTurbineEfficiency();

      double getPowerFromTurbineEfficiency(double turbineEff);


      double calculateGeneratorEfficiency(double power);

      List<double> getGeneratorEfficiencies(double power, string variant = "");

      double calculateGearLosses(double initialPower);

      double getLeakageTemperature();

      double getLeakageEnthalpy();

      void PerformCalculations();

      double CalculateGearLosses();
  }
