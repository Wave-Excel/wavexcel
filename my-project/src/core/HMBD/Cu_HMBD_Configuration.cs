namespace Cu_HMBD_Configuration;
using System;
using System.Collections.Generic;
using HMBD.HMBDInformation;
using Models.TurbineData;
using Interfaces.IThermodynamicLibrary;
using StartExecutionMain;
public class CustomHMBDConfiguration : HBDPowerCalculator{
  IThermodynamicLibrary thermodynamicService;
  TurbineDataModel turbineDataModel;
  public CustomHMBDConfiguration()
  {
    turbineDataModel = TurbineDataModel.getInstance();
    thermodynamicService =  CustomExecutedClass.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();
  }
  public void UpdateHBDParamsCustom(int row){
    List<PowerNearest> powerNearests = turbineDataModel.ListPower;
    turbineDataModel.TurbineEfficiency=  powerNearests[row].Efficiency;
    // Console.WriteLine("PPPPPPPPPPPPPHEHEHEBSKJFBJSBFKSJBFKS:"+thermodynamicService.getPowerFromTurbineEfficiency(turbineDataModel.TurbineEfficiency));
    base.HBDFUpdatePreFeasibility();
  }
   public void HBDsetDefaultCustomerParamas_Custom()
    {
      
        thermodynamicService.PerformCalculations();
        // Turbine.Main1();//write it in ThermoService
        turbineDataModel.AK25 = thermodynamicService.getPowerFromClosestEfficiency(turbineDataModel.AK25, turbineDataModel.GeneratorEfficiency);
        // HBD.Cells["AK25"].Value = Turbine.getPowerFromClosestEfficiency(Turbine.AK25, Convert.ToDouble(HBD.Cells["O10"].Value));//Turbine.AK25;
        // Turbine.finalPower = Convert.ToDouble(HBD.Cells["AK25"].Value);
        // HBDFUpdatePreFeasibility();
    }

}