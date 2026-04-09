using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Models.TurbineData;
using OfficeOpenXml;

namespace Models.PreFeasibility
{
    class PreFeasibilityDataModel
    {
        private IConfiguration configuration;
        private static PreFeasibilityDataModel preFeasibilityDataModel;
        double inlet_pressure_upper_limit = 75, inlet_pressure_lower_limit = 24, inlet_pressure_actual_value = 66.00;
        string inlet_pressure_pass_fail = "TRUE", inlet_pressure_remarks = "Direct Input from Customer";
        double temperature_lower_limit = 362.1673169,  temperature_upper_limit = 495, temperature_actual_value = 490.00;
        string temperature_remarks = "Direct Input from Customer", temperature_pass_fail = "TRUE";
        double backpressure_upper_limit = 6.668505, backpressure_lower_limit = 1.2, backpressure_actual_value = 5.00;
        string backpressure_remarks = "Direct Input from Customer", backpressure_pass_fail = "TRUE";
        double inlet_volumetric_flow_upper_limit = 0.9, inlet_volumetric_flow_lower_limit = 0.18, inlet_volumetric_flow_actual_value = 0.642217623;
        string inlet_volumetric_flow_pass_fail = "TRUE", inlet_volumetric_flow_remarks = "Check after Cycle Heat and Mass Balance Calulation";
        double power_upper_limit = 7100, power_lower_limit = 600, power_actual_value = 5423;
        string power_pass_fail = "TRUE",power_remarks = "Check after Cycle Heat and Mass Balance Calulation";
        string decision = "TRUE";
        int variant = -1;
        List<FlowPathData> flowPathDataList = new List<FlowPathData>();



        private PreFeasibilityDataModel()
        {
            //temperature_lower_limit = Math.Sqrt(Math.Sqrt(TurbineDataModel.getInstance().InletTemperature)) * 100 + 80;
            //temperature_lower_limit_2 = Math.Sqrt(Math.Sqrt(TurbineDataModel.getInstance().InletTemperature)) * 100 + 80;
        }

        private double inlet_pressure_upper_limit_2 = 95;
        private double inlet_pressure_lower_limit_2 = 24;
        private double inlet_pressure_actual_value_2 = 66.00;
        private string inlet_pressure_pass_fail_2 = "TRUE";
        private string inlet_pressure_remarks_2 = "Direct Input from Customer";

        public List<FlowPathData> FlowPathDataList
        {
            get { return flowPathDataList; }
            set { flowPathDataList = value; }
        }
        public double InletPressureUpperLimit
        {
            get { return inlet_pressure_upper_limit; }
            set { inlet_pressure_upper_limit = value; }
        }

        public double InletPressureLowerLimit
        {
            get { return inlet_pressure_lower_limit; }
            set { inlet_pressure_lower_limit = value; }
        }

        public double InletPressureActualValue
        {
            get { return inlet_pressure_actual_value; }
            set { inlet_pressure_actual_value = value; }
        }

        public string InletPressurePassFail
        {
            get { return inlet_pressure_pass_fail; }
            set { inlet_pressure_pass_fail = value; }
        }

        public string InletPressureRemarks
        {
            get { return inlet_pressure_remarks; }
            set { inlet_pressure_remarks = value; }
        }

        public double TemperatureLowerLimit
        {
            get { return temperature_lower_limit; }
            set { temperature_lower_limit = value; }
        }

        public double TemperatureUpperLimit
        {
            get { return temperature_upper_limit; }
            set { temperature_upper_limit = value; }
        }

        public double TemperatureActualValue
        {
            get { return temperature_actual_value; }
            set { temperature_actual_value = value; }
        }

        public string TemperatureRemarks
        {
            get { return temperature_remarks; }
            set { temperature_remarks = value; }
        }

        public string TemperaturePassFail
        {
            get { return temperature_pass_fail; }
            set { temperature_pass_fail = value; }
        }

        public double BackpressureUpperLimit
        {
            get { return backpressure_upper_limit; }
            set { backpressure_upper_limit = value; }
        }

        public double BackpressureLowerLimit
        {
            get { return backpressure_lower_limit; }
            set { backpressure_lower_limit = value; }
        }

        public double BackpressureActualValue
        {
            get { return backpressure_actual_value; }
            set { backpressure_actual_value = value; }
        }

        public string BackpressureRemarks
        {
            get { return backpressure_remarks; }
            set { backpressure_remarks = value; }
        }

        public string BackpressurePassFail
        {
            get { return backpressure_pass_fail; }
            set { backpressure_pass_fail = value; }
        }

        public double InletVolumetricFlowUpperLimit
        {
            get { return inlet_volumetric_flow_upper_limit; }
            set { inlet_volumetric_flow_upper_limit = value; }
        }

        public double InletVolumetricFlowLowerLimit
        {
            get { return inlet_volumetric_flow_lower_limit; }
            set { inlet_volumetric_flow_lower_limit = value; }
        }

        public double InletVolumetricFlowActualValue
        {
            get { return inlet_volumetric_flow_actual_value; }
            set { inlet_volumetric_flow_actual_value = value; }
        }

        public string InletVolumetricFlowPassFail
        {
            get { return inlet_volumetric_flow_pass_fail; }
            set { inlet_volumetric_flow_pass_fail = value; }
        }

        public string InletVolumetricFlowRemarks
        {
            get { return inlet_volumetric_flow_remarks; }
            set { inlet_volumetric_flow_remarks = value; }
        }

        public double PowerUpperLimit
        {
            get { return power_upper_limit; }
            set { power_upper_limit = value; }
        }

        public double PowerLowerLimit
        {
            get { return power_lower_limit; }
            set { power_lower_limit = value; }
        }

        public double PowerActualValue
        {
            get { return power_actual_value; }
            set { power_actual_value = value; }
        }

        public string PowerPassFail
        {
            get { return power_pass_fail; }
            set { power_pass_fail = value; }
        }

        public string PowerRemarks
        {
            get { return power_remarks; }
            set { power_remarks = value; }
        }

        public double InletPressureUpperLimit_2
        {
            get { return inlet_pressure_upper_limit_2; }
            set { inlet_pressure_upper_limit_2 = value; }
        }

        public double InletPressureLowerLimit_2
        {
            get { return inlet_pressure_lower_limit_2; }
            set { inlet_pressure_lower_limit_2 = value; }
        }

        public double InletPressureActualValue_2
        {
            get { return inlet_pressure_actual_value_2; }
            set { inlet_pressure_actual_value_2 = value; }
        }

        public string InletPressurePassFail_2
        {
            get { return inlet_pressure_pass_fail_2; }
            set { inlet_pressure_pass_fail_2 = value; }
        }

        public string InletPressureRemarks_2
        {
            get { return inlet_pressure_remarks_2; }
            set { inlet_pressure_remarks_2 = value; }
        }

        private double temperature_lower_limit_2 = 362.84;
        private double temperature_upper_limit_2 = 525;
        private double temperature_actual_value_2 = 490.00;
        private string temperature_remarks_2 = "Direct Input from Customer";
        private string temperature_pass_fail_2 = "TRUE";

        public double TemperatureLowerLimit_2
        {
            get { return temperature_lower_limit_2; }
            set { temperature_lower_limit_2 = value; }
        }

        public double TemperatureUpperLimit_2
        {
            get { return temperature_upper_limit_2; }
            set { temperature_upper_limit_2 = value; }
        }

        public double TemperatureActualValue_2
        {
            get { return temperature_actual_value_2; }
            set { temperature_actual_value_2 = value; }
        }

        public string TemperatureRemarks_2
        {
            get { return temperature_remarks_2; }
            set { temperature_remarks_2 = value; }
        }

        public string TemperaturePassFail_2
        {
            get { return temperature_pass_fail_2; }
            set { temperature_pass_fail_2 = value; }
        }

        private double backpressure_upper_limit_2 = 17;
        private double backpressure_lower_limit_2 = 1.2;
        private double backpressure_actual_value_2 = 5.00;
        private string backpressure_remarks_2 = "Direct Input from Customer";
        private string backpressure_pass_fail_2 = "TRUE";

        public double BackpressureUpperLimit_2
        {
            get { return backpressure_upper_limit_2; }
            set { backpressure_upper_limit_2 = value; }
        }

        public double BackpressureLowerLimit_2
        {
            get { return backpressure_lower_limit_2; }
            set { backpressure_lower_limit_2 = value; }
        }

        public double BackpressureActualValue_2
        {
            get { return backpressure_actual_value_2; }
            set { backpressure_actual_value_2 = value; }
        }

        public string BackpressureRemarks_2
        {
            get { return backpressure_remarks_2; }
            set { backpressure_remarks_2 = value; }
        }

        public string BackpressurePassFail_2
        {
            get { return backpressure_pass_fail_2; }
            set { backpressure_pass_fail_2 = value; }
        }

        private double inlet_volumetric_flow_upper_limit_2 = 1.09;
        private double inlet_volumetric_flow_lower_limit_2 = 0.18;
        private double inlet_volumetric_flow_actual_value_2 = 0.642217623;
        private string inlet_volumetric_flow_pass_fail_2 = "TRUE";
        private string inlet_volumetric_flow_remarks_2 = "Check after Cycle Heat and Mass Balance Calulation";

        public double InletVolumetricFlowUpperLimit_2
        {
            get { return inlet_volumetric_flow_upper_limit_2; }
            set { inlet_volumetric_flow_upper_limit_2 = value; }
        }

        public double InletVolumetricFlowLowerLimit_2
        {
            get { return inlet_volumetric_flow_lower_limit_2; }
            set { inlet_volumetric_flow_lower_limit_2 = value; }
        }

        public double InletVolumetricFlowActualValue_2
        {
            get { return inlet_volumetric_flow_actual_value_2; }
            set { inlet_volumetric_flow_actual_value_2 = value; }
        }

        public string InletVolumetricFlowPassFail_2
        {
            get { return inlet_volumetric_flow_pass_fail_2; }
            set { inlet_volumetric_flow_pass_fail_2 = value; }
        }

        public string InletVolumetricFlowRemarks_2
        {
            get { return inlet_volumetric_flow_remarks_2; }
            set { inlet_volumetric_flow_remarks_2 = value; }
        }

        private double power_upper_limit_2 = 7100;
        private double power_lower_limit_2 = 600;
        private double power_actual_value_2 = 5422.65;
        private string power_pass_fail_2 = "TRUE";
        private string power_remarks_2 = "Check after Cycle Heat and Mass Balance Calulation";

        public double PowerUpperLimit_2
        {
            get { return power_upper_limit_2; }
            set { power_upper_limit_2 = value; }
        }

        public double PowerLowerLimit_2
        {
            get { return power_lower_limit_2; }
            set { power_lower_limit_2 = value; }
        }

        public double PowerActualValue_2
        {
            get { return power_actual_value_2; }
            set { power_actual_value_2 = value; }
        }

        public string PowerPassFail_2
        {
            get { return power_pass_fail_2; }
            set { power_pass_fail_2 = value; }
        }

        public string PowerRemarks_2
        {
            get { return power_remarks_2; }
            set { power_remarks_2 = value; }
        }

        private string decision_2 = "TRUE";

        public string Decision_2
        {
            get { return decision_2; }
            set { decision_2 = value; }
        }
        public string Decision
        {
            get { return decision; }
            set { decision = value; }
        }

        // Public property for 'variant'
        public int Variant
        {
            get { return variant; }
            set { variant = value; }
        }
        public static PreFeasibilityDataModel getInstance()
        {
            if (preFeasibilityDataModel == null)
            {
                preFeasibilityDataModel = new PreFeasibilityDataModel();
            }
            return preFeasibilityDataModel;
        }

        public string[] fillPreFeasibilityCreatriea(string inletPressure, string power, string volFlow)
        {
            string[] Filledvalues = new string[3];

            using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Split the line by comma
                    var values = line.Split(',');
                    if (values[0] == inletPressure)
                    {
                        Filledvalues[0] = values[1];
                    }
                    else if (values[0] == power)
                    {
                        Filledvalues[1] = values[1];
                    }
                    else if (values[0] == volFlow)
                    {
                        Filledvalues[2] = values[1];
                    }
                }
            }
            return Filledvalues;
        }
        public void fillPreFeasibilityData()
        {
            configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json").Build();
            // configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            //string excelPath = configuration["AppSettings:ExcelFilePath"];
            //FileInfo existingFile = new FileInfo(excelPath);
            //ExcelPackage package = new ExcelPackage(existingFile);
            //var preFeasibility = package.Workbook.Worksheets["Pre-Feasibility"];
            flowPathDataList.Add(new FlowPathData());
            string[] case1 = fillPreFeasibilityCreatriea("Opt 2 _ 2 MW_InletPressure", "Opt 2 _ 2 MW_Power", "Opt 2 _ 2 MW_Vol_Flow");
            string[] case2 = fillPreFeasibilityCreatriea("Opt 2 _ 3 MW_InletPressure", "Opt 2 _ 3 MW_Power", "Opt 2 _ 3 MW_Vol_Flow");
            string[] case3 = fillPreFeasibilityCreatriea("Opt 1 _ 66_480_4 MW_InletPressure", "Opt 1 _ 66_480_4 MW_Power", "Opt 1 _ 66_480_4 MW_Vol_Flow");
            string[] case4 = fillPreFeasibilityCreatriea("Opt 1 _ 46_460_4 MW_InletPressure", "Opt 1 _ 46_460_4 MW_Power", "Opt 1 _ 46_460_4 MW_Vol_Flow");
            string[] case5 = fillPreFeasibilityCreatriea("Opt 1 _ 64_480_6 MW_InletPressure", "Opt 1 _ 64_480_6 MW_Power", "Opt 1 _ 64_480_6 MW_Vol_Flow");
            string[] case6 = fillPreFeasibilityCreatriea("Opt 1 _ 46_460_6 MW_InletPressure", "Opt 1 _ 46_460_6 MW_Power", "Opt 1 _ 46_460_6 MW_Vol_Flow");
            flowPathDataList.Add(new FlowPathData("Opt 2 _ 2 MW", "", 2, 0.25, Convert.ToDouble(case1[0]), Convert.ToDouble(case1[1]), Convert.ToDouble(case1[2])));
            flowPathDataList.Add(new FlowPathData("Opt 2 _ 3 MW", "", 3.5, 0.51, Convert.ToDouble(case2[0]), Convert.ToDouble(case2[1]), Convert.ToDouble(case2[2])));
            flowPathDataList.Add(new FlowPathData("Opt 1 _ 66_480_4 MW", ">46", 4.5, 0.45, Convert.ToDouble(case3[0]), Convert.ToDouble(case3[1]), Convert.ToDouble(case3[2])));
            flowPathDataList.Add(new FlowPathData("Opt 1 _ 46_460_4 MW", "<46", 4.5, 0.90, Convert.ToDouble(case4[0]), Convert.ToDouble(case4[1]), Convert.ToDouble(case4[2])));
            flowPathDataList.Add(new FlowPathData("Opt 1 _ 64_480_6 MW", ">46", 7, 0.52, Convert.ToDouble(case5[0]), Convert.ToDouble(case5[1]), Convert.ToDouble(case5[2])));
            flowPathDataList.Add(new FlowPathData("Opt 1 _ 46_460_6 MW", "<46", 7, 0.90, Convert.ToDouble(case6[0]), Convert.ToDouble(case6[1]), Convert.ToDouble(case6[2])));


            //using (var reader = new StreamReader("C:\\testDir\\AdminControl.csv"))
            //{
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        // Split the line by comma
            //        var values = line.Split(',');
            //        if (values[0] == "Opt 2 _ 2 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 2 _ 2 MW", "", 2, 0.25, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        } else if (values[0] == "Opt 2 _ 3 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 2 _ 3 MW", "", 3.5, 0.51, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        } else if (values[0] == "Opt 1 _ 66_480_4 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 1 _ 66_480_4 MW", ">46", 4.5, 0.45, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        } else if (values[0] == "Opt 1 _ 46_460_4 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 1 _ 46_460_4 MW", "<46", 4.5, 0.90, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        } else if (values[0] == "Opt 1 _ 64_480_6 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 1 _ 64_480_6 MW", ">46", 7, 0.52, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        }
            //        else if (values[0] == "Opt 1 _ 46_460_6 MW")
            //        {
            //            flowPathDataList.Add(new FlowPathData("Opt 1 _ 46_460_6 MW", "<46", 7, 0.90, Convert.ToDouble(values[1]), Convert.ToDouble(values[2]), Convert.ToDouble(values[3])));
            //        }

            //    }
            //}



        }

        public void fillPrefeasibilityDecisionChecks()
        {
            temperature_lower_limit = Math.Sqrt(Math.Sqrt(TurbineDataModel.getInstance().InletPressure)) * 100 + 80;
            temperature_lower_limit_2 = Math.Sqrt(Math.Sqrt(TurbineDataModel.getInstance().InletPressure)) * 100 + 80;
            if (preFeasibilityDataModel.InletPressureActualValue < preFeasibilityDataModel.inlet_pressure_upper_limit && preFeasibilityDataModel.InletPressureActualValue > preFeasibilityDataModel.inlet_pressure_lower_limit)
            {
                preFeasibilityDataModel.InletPressurePassFail = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.InletPressurePassFail = "FALSE";
            }
            if (preFeasibilityDataModel.temperature_actual_value < preFeasibilityDataModel.temperature_upper_limit && preFeasibilityDataModel.temperature_actual_value > preFeasibilityDataModel.TemperatureLowerLimit)
            {
                preFeasibilityDataModel.TemperaturePassFail = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.TemperaturePassFail = "FALSE";
            }
            if (preFeasibilityDataModel.backpressure_actual_value < preFeasibilityDataModel.backpressure_upper_limit && preFeasibilityDataModel.backpressure_actual_value > preFeasibilityDataModel.BackpressureLowerLimit)
            {
                preFeasibilityDataModel.backpressure_pass_fail = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.backpressure_pass_fail = "FALSE";
            }
            if (preFeasibilityDataModel.InletVolumetricFlowActualValue < preFeasibilityDataModel.InletVolumetricFlowUpperLimit && preFeasibilityDataModel.InletVolumetricFlowActualValue > preFeasibilityDataModel.inlet_volumetric_flow_lower_limit)
            {
                preFeasibilityDataModel.InletVolumetricFlowPassFail = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.InletVolumetricFlowPassFail = "FALSE";
            }
            if (preFeasibilityDataModel.PowerActualValue < preFeasibilityDataModel.power_upper_limit && preFeasibilityDataModel.PowerActualValue > preFeasibilityDataModel.PowerLowerLimit)
            {
                preFeasibilityDataModel.PowerPassFail = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.PowerPassFail = "FALSE";
            }
            if (preFeasibilityDataModel.InletPressurePassFail == "TRUE" && preFeasibilityDataModel.TemperaturePassFail == "TRUE" && preFeasibilityDataModel.backpressure_pass_fail == "TRUE" && preFeasibilityDataModel.InletVolumetricFlowPassFail == "TRUE" && preFeasibilityDataModel.PowerPassFail == "TRUE")
            {
                preFeasibilityDataModel.Decision = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.Decision = "FALSE";
            }
            fillPrefeasibilityDecision_2_Checks();
        }

        public void fillPrefeasibilityDecision_2_Checks()
        {
            if (preFeasibilityDataModel.InletPressureActualValue_2 < preFeasibilityDataModel.inlet_pressure_upper_limit_2 && preFeasibilityDataModel.InletPressureActualValue_2 > preFeasibilityDataModel.inlet_pressure_lower_limit_2)
            {
                preFeasibilityDataModel.InletPressurePassFail_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.InletPressurePassFail_2 = "FALSE";
            }
            if (preFeasibilityDataModel.temperature_actual_value_2 < preFeasibilityDataModel.temperature_upper_limit_2 && preFeasibilityDataModel.temperature_actual_value_2 > preFeasibilityDataModel.TemperatureLowerLimit_2)
            {
                preFeasibilityDataModel.TemperaturePassFail_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.TemperaturePassFail_2 = "FALSE";
            }
            if (preFeasibilityDataModel.backpressure_actual_value_2 < preFeasibilityDataModel.backpressure_upper_limit_2 && preFeasibilityDataModel.backpressure_actual_value_2 > preFeasibilityDataModel.BackpressureLowerLimit_2)
            {
                preFeasibilityDataModel.backpressure_pass_fail_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.backpressure_pass_fail_2 = "FALSE";
            }
            if (preFeasibilityDataModel.InletVolumetricFlowActualValue_2 < preFeasibilityDataModel.InletVolumetricFlowUpperLimit_2 && preFeasibilityDataModel.InletVolumetricFlowActualValue_2 > preFeasibilityDataModel.inlet_volumetric_flow_lower_limit_2)
            {
                preFeasibilityDataModel.InletVolumetricFlowPassFail_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.InletVolumetricFlowPassFail_2 = "FALSE";
            }
            if (preFeasibilityDataModel.PowerActualValue_2 < preFeasibilityDataModel.power_upper_limit_2 && preFeasibilityDataModel.PowerActualValue_2 > preFeasibilityDataModel.PowerLowerLimit_2)
            {
                preFeasibilityDataModel.PowerPassFail_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.PowerPassFail_2 = "FALSE";
            }
            if (preFeasibilityDataModel.InletPressurePassFail_2 == "TRUE" && preFeasibilityDataModel.TemperaturePassFail_2 == "TRUE" && preFeasibilityDataModel.backpressure_pass_fail_2 == "TRUE" && preFeasibilityDataModel.InletVolumetricFlowPassFail_2 == "TRUE" && preFeasibilityDataModel.PowerPassFail_2 == "TRUE")
            {
                preFeasibilityDataModel.Decision_2 = "TRUE";
            }
            else
            {
                preFeasibilityDataModel.Decision_2 = "FALSE";
            }
        }


    }

    class FlowPathData
    {
        // Private fields with underscore prefix
        private string _flowPathName;
        private string _inletPressure; // Storing pressure as string because it includes symbols like '<' and '>'
        private double _power;
        private double _inletVolumetricFlow;
        private double _f;
        private double _g;
        private double _h;

        // Public properties with PascalCase
        public string FlowPathName
        {
            get => _flowPathName;
            set => _flowPathName = value;
        }

        public string InletPressure
        {
            get => _inletPressure;
            set => _inletPressure = value;
        }

        public double Power
        {
            get => _power;
            set => _power = value;
        }

        public double InletVolumetricFlow
        {
            get => _inletVolumetricFlow;
            set => _inletVolumetricFlow = value;
        }

        public double F
        {
            get => _f;
            set => _f = value;
        }

        public double G
        {
            get => _g;
            set => _g = value;
        }

        public double H
        {
            get => _h;
            set => _h = value;
        }

        // Constructor to initialize fields (optional)
        public FlowPathData(string flowPathName, string inletPressure, double power, double inletVolumetricFlow, double f, double g, double h)
        {
            _flowPathName = flowPathName;
            _inletPressure = inletPressure;
            _power = power;
            _inletVolumetricFlow = inletVolumetricFlow;
            _f = f;
            _g = g;
            _h = h;
        }

        public FlowPathData()
        {
        }
    }
}