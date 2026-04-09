namespace Exec_HMBD_Configuration;
using System;
using System.Collections.Generic;
using HMBD.HMBDInformation;
using Models.TurbineData;
using Interfaces.IThermodynamicLibrary;
using Interfaces.ILogger;
using StartExecutionMain;
using Models.PreFeasibility;
using Ignite_X.src.core.Handlers;
using Kreisl.KreislConfig;
using Interfaces.IERGHandlerService;
// using TurbineUtils;
using StartKreislExecution;

public class ExecHMBDConfiguration : HBDPowerCalculator{
  IThermodynamicLibrary thermodynamicService;
  TurbineDataModel turbineDataModel;
  IERGHandlerService eRGHandlerService;
  ILogger logger;
  public ExecHMBDConfiguration()
  {
    turbineDataModel = TurbineDataModel.getInstance();
    thermodynamicService =  MainExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
    eRGHandlerService = StartKreisl.GlobalHost.Services.GetService<IERGHandlerService>();
  }
  public void UpdateHBDParamsExecuted(int row)
  {
    List<PowerNearest> powerNearests = turbineDataModel.ListPower;
    turbineDataModel.TurbineEfficiency=  powerNearests[row].Efficiency;
    Console.WriteLine("PPPPPPPPPPPPPHEHEHEBSKJFBJSBFKSJBFKS:"+thermodynamicService.getPowerFromTurbineEfficiency(turbineDataModel.TurbineEfficiency));
    base.HBDFUpdatePreFeasibility();
  }
    public void HBDsetDefaultCustomerParamas_Executed()
    {
        thermodynamicService.PerformCalculations();
        // Turbine.Main1();//write it in ThermoService
        turbineDataModel.AK25 = thermodynamicService.getPowerFromClosestEfficiency(turbineDataModel.AK25, turbineDataModel.GeneratorEfficiency);
        // HBDFUpdatePreFeasibility();
    }

    public void HBDsetDefaultCustomerParamas_Executed_Kreisl()
    {
        thermodynamicService.FillClosestTurbineEfficiency();

        GetTurbaCON(turbineDataModel.ClosestProjectID);

        KreislDATHandler kreislDATHandler = new KreislDATHandler();

        kreislDATHandler.FillTurbineEff(StartKreisl.filePath, "4", turbineDataModel.TurbineEfficiency.ToString());
        //kreislDATHandler
        //call kreisl

        KreislIntegration kreislConfig = new KreislIntegration();
        kreislConfig.LaunchKreisL();

        PreFeasibilityDataModel preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
        //read erg fill power, volflow
        preFeasibilityDataModel.PowerActualValue = eRGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath);
        turbineDataModel.AK25 = preFeasibilityDataModel.PowerActualValue;
        //Fill Efficiencies based on AK25
        if(turbineDataModel.DeaeratorOutletTemp > 0)
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERGForPRV(StartKreisl.ergFilePath);
        }
        else if (turbineDataModel.PST > 0)
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERGForDesuparator(StartKreisl.ergFilePath);
        }
        else
        {
            preFeasibilityDataModel.InletVolumetricFlowActualValue = eRGHandlerService.ExtractVolFlowFromERG(StartKreisl.ergFilePath);

        }
        turbineDataModel.VolumetricFlow = preFeasibilityDataModel.InletVolumetricFlowActualValue;
        HBDFUpdatePreFeasibility("KreisL");
    }

}