using Interfaces.IThermodynamicLibrary;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.Runtime.InteropServices;
using StartExecutionMain;
using Microsoft.Extensions.DependencyInjection;
using Models.TurbineData;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Ignite_X.src.core.Handlers;
using StartKreislExecution;
using Kreisl.KreislConfig;

using Ignite_X.src.core.Services;
using Interfaces.IERGHandlerService;


namespace Services.ThermodynamicService{
 public class ThermodynamicService : IThermodynamicLibrary{        
        private string filePath;
        public string FilePath
        {
            get => filePath;
            set => filePath = value;
        }

        private static string dllPath;

        TurbineDataModel turbineDataModel;
        IConfiguration configuration;
        IERGHandlerService eRGHandlerService;

        
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double hVon_p_t(double P, double T, double unknown);
        
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double hVon_p_s(double P, double T);
        
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double sVon_p_h(double p, double H1, double unknown);
        
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double tVon_p_h(double P2, double H2);
        
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double vVon_p_t(double P1, double T1);
        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double tSattVon_p(double P1);

        [DllImport("H2O64Bit.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double pSattVon_t(double T1);


        public double psatvont(double T1)
        {
            try
            {
                return pSattVon_t((double)T1);
            }
            catch (Exception e)
            {
                throw new Exception($"DLL threw an error while calculating saturation pressure for Temperature = {T1}");
            }
        }
        public double tsatvonp(double P)
        {
            try
            {
                return tSattVon_p((double)P);
            }
            catch (Exception e)
            {
                throw new Exception($"DLL threw an error while calculating saturation temperature for Pressure = {P}.");
            }
        }

        public ThermodynamicService(double steamPressure, double steamTemperature, double steamMass, double exhaustPressure, IConfiguration configuration)
        {
            // Load the DLL path
          
        }
        
        public ThermodynamicService(){
            turbineDataModel = TurbineDataModel.getInstance();
            configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
            eRGHandlerService = new KreislERGHandlerService();// StartKreisl.GlobalHost.Services.GetRequiredService<IERGHandlerService>();
            filePath = configuration.GetValue<string>("AppSettings:ExcelFilePath");
            LoadDll();
        }
        
        public double getInletPressure(){
            return turbineDataModel.InletPressure;
        }

        public void LoadDll()
        {
            // Get the path of the DLL based on the current directory
            string targetDirectory = AppContext.BaseDirectory;
            // Console.WriteLine(targetDirectory);
            // co
            string sourceDllPath = Path.Combine("src", "core", "Dll", "H2O64Bit.dll"); // Adjust this path based on where the DLL is located in your project
            // Console.WriteLine(sourceDllPath);
            string targetDllPath = Path.Combine(targetDirectory, "H2O64Bit.dll");
            //Debug.WriteLine(targetDllPath + "-----------------------------");

            // Create the target directory if it doesn't exist
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Check if the DLL exists in the target directory
            if (!File.Exists(targetDllPath))
            {
                // Copy the DLL from the source to the target directory
                File.Copy(sourceDllPath, targetDllPath, true);
            }

            // Check if the copied DLL exists
            if (!File.Exists(targetDllPath))
            {
                throw new DllNotFoundException($"DLL not found at path: {targetDllPath}");
            }
        }

        public string FillClosestTurbineEfficiency()
        {
            string conPath = "";
            Debug.WriteLine("----------------------------------------------------------" + filePath);
            double inletPressure = getInletPressure(), inletTemp = turbineDataModel.InletTemperature;
            double exhaustPressure = getExhaustPressure();
            double massFlowRate = turbineDataModel.MassFlowRate;
            double normPressure1 = getNormalisedValue("Steam Pressure", inletPressure, filePath);
            double normPressure2 = getNormalisedValue("Exhaust Pressure", exhaustPressure, filePath);
            double normMassFlowRate = getNormalisedValue("Steam Mass", turbineDataModel.MassFlowRate, filePath);
            double normTemperature = getNormalisedValue("Steam Temp", turbineDataModel.InletTemperature, filePath);
            

            // Load the data points from Excel
            List<DataPoint> dataPoints = LoadDataFromExcel(filePath, "PowerNormDB");

            double efficiency = getClosestEfficiency(dataPoints, normPressure1, normPressure2, normMassFlowRate, normTemperature);

            Console.WriteLine("Efficiency Of Turbine : " + efficiency);
            turbineDataModel.TurbineEfficiency = efficiency;

            return conPath;
        }

        public void PerformCalculations()
        {
            Debug.WriteLine("----------------------------------------------------------" + filePath);
            double inletPressure = getInletPressure(), inletTemp = turbineDataModel.InletTemperature;
            double exhaustPressure = getExhaustPressure();
            double massFlowRate = turbineDataModel.MassFlowRate;
            double normPressure1 = getNormalisedValue("Steam Pressure", inletPressure, filePath);
            double normPressure2 = getNormalisedValue("Exhaust Pressure", exhaustPressure, filePath);
            double normMassFlowRate = getNormalisedValue("Steam Mass", turbineDataModel.MassFlowRate, filePath);
            double normTemperature = getNormalisedValue("Steam Temp", turbineDataModel.InletTemperature, filePath);
            

            // Load the data points from Excel
            List<DataPoint> dataPoints = LoadDataFromExcel(filePath, "PowerNormDB");

            double efficiency = getClosestEfficiency(dataPoints, normPressure1, normPressure2, normMassFlowRate, normTemperature);

            Console.WriteLine("Efficiency Of Turbine : " + efficiency);
            turbineDataModel.TurbineEfficiency = efficiency;
            double inletEnthalpy = getInletEnthalpy(inletPressure, inletTemp);
            turbineDataModel.InletEnthalphy = inletEnthalpy;
            double outletEnthalpy = getOutletEnthalpy(exhaustPressure, inletEnthalpy, efficiency, inletPressure);
            turbineDataModel.OutletEnthalphy = outletEnthalpy;

            double outletTemp = getOutletTemperature(exhaustPressure, outletEnthalpy);
            // temperature2 = getTemperature2(exhaustPressure, outletEnthalpy);
            turbineDataModel.OutletTemperature = outletTemp;
            double iPower = massFlowRate * (inletEnthalpy - outletEnthalpy);
            // Console.WriteLine("IIIIPOWER:"+ iPower);
            // Console.WriteLine("GLOSS:"+ calculateGearLosses(iPower));
            // Console.WriteLine("TURBINE EFF:" + efficiency);

            double power = findPowerOfTurbine(inletEnthalpy, outletEnthalpy, massFlowRate, turbineDataModel.OilLosses, turbineDataModel.GearLosses);
            
            // Console.WriteLine("Power Of Turbine : " + power);
            turbineDataModel.LeakageTemperature = getLeakageTemperature();
            // turbineDataModel.LeakageTemperature = leakageTemperature;
            // Console.WriteLine("leakage Temperature of Turbine : " + leakageTemperature);
            turbineDataModel.LeakageEnthalphy = getLeakageEnthalpy();
            // turbineDataModel.LeakageEnthalphy = leakageEnthalphy;
            // Console.WriteLine("Leakage Enthalphy of Turbine : " + leakageEnthalphy);

            double specificVolume = getSpecificVolume();
            double volumetricFlow = getVolumetricFlow(massFlowRate, specificVolume);
            if(turbineDataModel.DeaeratorOutletTemp == 0)
            {
                turbineDataModel.OutletMassFlow = getOutletMassFlow();
            }
            Console.WriteLine("getMassFlowFromPower" + getMassFlowFromPower(power, inletPressure, exhaustPressure, inletTemp, massFlowRate, efficiency, turbineDataModel.GeneratorEfficiency));

        }

        public double getOutletMassFlow(){

            return turbineDataModel.MassFlowRate - (turbineDataModel.LeakageMassFlow / 3.600);
        }
        public  List<DataPoint> LoadDataFromExcel(string filePath, string sheetName)
        {
            List<DataPoint> dataPoints = new List<DataPoint>();

            // Ensure the file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found: " + filePath);
                return dataPoints;
            }

            // Load the Excel file
            FileInfo fileInfo = new FileInfo(filePath);
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set the license context
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;


            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                // Get the specified worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];

                if (worksheet != null)
                {
                    int rowCount = worksheet.Dimension.Rows;

                    // Read each row and create DataPoint objects
                    for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip header
                    {
                        double efficiency = double.Parse(worksheet.Cells[row, 1].Text);
                        // Console.WriteLine("eff : " + efficiency + "\n");
                        string project = worksheet.Cells[row, 2].Text; // Read project name
                        double pressure1 = double.Parse(worksheet.Cells[row, 4].Text);
                        double temperature = double.Parse(worksheet.Cells[row, 5].Text);
                        double massFlowRate = double.Parse(worksheet.Cells[row, 6].Text);
                        double pressure2 = double.Parse(worksheet.Cells[row, 7].Text);
                        string refProjectId = worksheet.Cells[row, 10].Text;

                        dataPoints.Add(new DataPoint(efficiency, project, pressure1, temperature, massFlowRate, pressure2, refProjectId));
                    }
                }
                else
                {
                    Console.WriteLine($"Worksheet '{sheetName}' not found.");
                }
            }

            return dataPoints;
        }

        public double GetMassFlowUsingKreislPower(double targetPower, double lower_limit, double higher_limit)
        {
            double midMassFlow = 0;
            double ergPower = 0;
            int iterationCount = 0;
            while (Math.Abs(targetPower - ergPower) >= 1)
            {
                ++iterationCount;
                midMassFlow = lower_limit + (higher_limit - lower_limit) / 2.000;
                KreislDATHandler kreislDATHandler = new KreislDATHandler();
                kreislDATHandler.FillMassFlow(StartKreisl.filePath, 5, midMassFlow.ToString());
                KreislIntegration krisl = new KreislIntegration();
                krisl.LaunchKreisL();
                Thread.Sleep(700);
                ergPower = eRGHandlerService.ExtractPowerFromERG(StartKreisl.ergFilePath);
                if (targetPower > ergPower)
                {
                    lower_limit = midMassFlow + 0.001;
                }
                else
                {
                    higher_limit = midMassFlow;
                }
            }
            return midMassFlow;
        }
        public double getClosestEfficiency(List<DataPoint> dataPoints, double pressure1, double pressure2, double massFlowRate, double temperature)
        {
            DataPoint closestPoint = null;
            double minDistance = double.MaxValue;

            DataPoint closestPoint1 = null;
            double minDistance1 = double.MaxValue;

            DataPoint closestPoint2 = null;
            double minDistane2 = double.MaxValue;

            foreach (var point in dataPoints)
            {
                // Calculate the Euclidean distance^2
                double distance =
                    Math.Pow(point.Pressure1 - pressure1, 2) +
                    Math.Pow(point.Temperature - temperature, 2) +
                    Math.Pow(point.MassFlowRate - massFlowRate, 2) +
                    Math.Pow(point.Pressure2 - pressure2, 2);

                // Update the closest point if this distance is smaller
                if (distance < minDistance)
                {
                    closestPoint2 = closestPoint1;
                    minDistane2 = minDistance1;
                    closestPoint1 = closestPoint;
                    minDistance1 = minDistance;
                    minDistance = distance;
                    closestPoint = point;
                }else if (distance > minDistance && distance < minDistance1)
                {
                    closestPoint2 = closestPoint1;
                    minDistane2 = minDistance1;
                    closestPoint1 = point;
                    minDistance1 = distance;
                }
                else if (distance > minDistance && distance > minDistance1 && distance < minDistane2)
                {
                    closestPoint2 = point;
                    minDistane2 = distance;
                }
               
            }
            //Debug.WriteLine(closestPoint.Efficiency +" "+ closestPoint.Project);
            //Debug.WriteLine(closestPoint1.Efficiency + " "+ closestPoint1.Project);
            //Debug.WriteLine(closestPoint2.Efficiency + " " + closestPoint2.Project);
            //if (true) // will check that user click on solve for higher eff or not
            //{



            //    double eff = Math.Max(closestPoint.Efficiency, Math.Max(closestPoint1.Efficiency, closestPoint2.Efficiency));

            //    turbineDataModel.ClosestProjectName = closestPoint.Project;


            //    turbineDataModel.ClosestProjectID = closestPoint.projID;

            //    // Return the efficiency of the closest point
            //    return eff; // Return NaN if no points were found

            //}

            turbineDataModel.ClosestProjectName = closestPoint.Project;
            

            turbineDataModel.ClosestProjectID = closestPoint.projID;

            // Return the efficiency of the closest point
            return closestPoint?.Efficiency ?? double.NaN; // Return NaN if no points were found
        }
        public  double getNormalisedValue(string columnName, double inputValue, string filePath)
        {
            double minColumn = double.MaxValue;
            double maxColumn = double.MinValue;


            // Ensure the file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found: " + filePath);
                return -1;
            }

            // Load the Excel file
            FileInfo fileInfo = new FileInfo(filePath);
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set the license context
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                // Assuming the first worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets["TurbineDB"];

                //Console.WriteLine(worksheet);
                if (worksheet != null)
                {
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // Find the index of the specified column
                    int columnIndex = -1;
                    for (int col = 1; col <= colCount; col++)
                    {

                        if (worksheet.Cells[1, col].Text.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            columnIndex = col;
                            break;
                        }
                    }

                    if (columnIndex == -1)
                    {
                        Console.WriteLine($"Column '{columnName}' not found.");
                        return -1;
                    }

                    // Get the minimum value in the specified column (excluding the header)
                    bool foundValue = false;

                    for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip header

                    {
                        //Console.WriteLine(worksheet.Cells[row, columnIndex].Text);
                        if (double.TryParse(worksheet.Cells[row, columnIndex].Text, out double cellValue))
                        {
                            maxColumn = Math.Max(maxColumn, cellValue);
                            if (cellValue < minColumn)
                            {
                                minColumn = cellValue;
                                foundValue = true;
                            }
                        }
                    }

                    if (foundValue)
                    {

                        double normalised = (inputValue - minColumn) / (maxColumn - minColumn);
                        // Console.WriteLine(normalised);
                        return normalised;
                    }
                    else
                    {
                        Console.WriteLine($"No numeric values found in column '{columnName}'.");
                        return -1;
                    }
                }
                else
                {
                    Console.WriteLine("Worksheet not found.");
                    return -1;
                }
                return -1;
            }


        }
        public  double getMassFlowFromPower(double power, double pressure1, double pressure2, double temperature1, double massFlow, double closestEfficiency, double generatorEff)
        {
            double inletEnthalpy = getInletEnthalpy(pressure1, temperature1);
            double outletEnthalpy = getOutletEnthalpy(pressure2, inletEnthalpy, closestEfficiency, pressure1);

            double req_power = power / ((generatorEff / 100.00000));//im using generator eff of 
            req_power /= ((1.00000 - turbineDataModel.Margin / 100.00000));
            double intialPower = massFlow * (inletEnthalpy - outletEnthalpy);
            req_power += (calculateGearLosses(intialPower, turbineDataModel.FinalPower) + turbineDataModel.OilLosses + turbineDataModel.InventoryLosses);
            Debug.WriteLine("INITITLA POWERRR:" + turbineDataModel.FinalPower + ", AK25" + turbineDataModel.AK25);
            Debug.WriteLine("intlet E1" + inletEnthalpy + ", outlet Enthalpy" + outletEnthalpy);
            Debug.WriteLine("Calculate GEARRRRRRRRRRRRRRRR" + calculateGearLosses(turbineDataModel.FinalPower));
            return (req_power) / (inletEnthalpy - outletEnthalpy);
        }
        public  double getInletEnthalpy(double pressure, double temperature)
        {
            //return 0;
            double enthalpy = hVon_p_t(pressure, temperature, 0);
            Console.WriteLine("Inlet Enthalpy: " + enthalpy);
            return enthalpy;
        }
        public  double getOutletTemperature(double pressure, double enthalpy)
        {
            //return 0;
            double temperature2 = tVon_p_h(pressure, enthalpy);
            Console.WriteLine("Temperature 2: " + temperature2);
            return temperature2;
        }
        public  double getSpecificVolume()
        {
            double flow = vVon_p_t(getInletPressure(), getInletTemperature());
            turbineDataModel.SpecificVolume = flow;
            return flow;
        }

        public double getInletTemperature(){
            return TurbineDataModel.getInstance().InletTemperature;
        }

        public  double GetInletVelocity(double massinKg){
            double O2 = massinKg * getSpecificVolume();
            // powerCalc.Cells["O2"].Value = 0.6520599;//O2;
            // powerCalc.Cells["Y8"].Value = O2;
            
            double volFlow_Velocity = O2/(0.01767);
            Console.WriteLine("VELOCITY: "+volFlow_Velocity);
            return volFlow_Velocity;
        }
        public  double getOutletEnthalpy(double pressure, double enthalpy1, double efficiency, double pressure1)
        {
            //Debug.WriteLine("Turbbbbb: " + efficiency + ", press2: " + pressure + ", press1: " + pressure1);
            double outletEnthalpy = enthalpy1 - (efficiency / 100.00) * (enthalpy1 - hVon_p_s(pressure, sVon_p_h(pressure1, enthalpy1, 0)));
            //Debug.WriteLine("Outlet Enthalpy: " + outletEnthalpy + ", eff" + efficiency);
            return outletEnthalpy;
        }

        public double getMassFlowRate(){
            return turbineDataModel.MassFlowRate;
        }
        public  double getVolumetricFlow(){
            double volFlow = getMassFlowRate() * getSpecificVolume();
            turbineDataModel.VolumetricFlow = volFlow;
            return volFlow;
        }

        public double getInventoryLosses(){
            return turbineDataModel.InventoryLosses;
        }
        public  double getVolumetricFlow(double mass_Flow_Rate, double specificVolume)
        {
            return mass_Flow_Rate * specificVolume;
        }
        //Assuming mass flow rate kg/sec;;; if tonnes/hr divide by 3.6
        public  double findPowerOfTurbine(double enthalpy1, double enthalpy2, double massFlowRate, double oilLosses, double gearLosses)
        {
            double initialPower = massFlowRate * (enthalpy1 - enthalpy2);
            Console.WriteLine("FJSBFSJBFJSBJGBFGSJSJJJJJJJJJJJJJJJJJJJJ: "+ initialPower+", e1:"+enthalpy1+",e2: "+enthalpy2+", mass:"+ massFlowRate);
            //need to send diff power not initial power, change this after confusion clears
            gearLosses = calculateGearLosses(initialPower - oilLosses - getInventoryLosses());
            double powerAfterLosses = initialPower - oilLosses - getInventoryLosses() - gearLosses;
            double margin = turbineDataModel.Margin;

            double generatorEfficiency = calculateGeneratorEfficiency(powerAfterLosses);//AK23 as input
            Debug.WriteLine("PowerAfterLosses: " + powerAfterLosses + " , generatorEFF: "+ generatorEfficiency);
            Console.WriteLine("Efffffffffffffffffffffffffffff: "+generatorEfficiency+", power after loss:"+ powerAfterLosses+", m:"+margin);
            turbineDataModel.AK25  = (powerAfterLosses) * (generatorEfficiency / 100.00) * (1.0 - margin / 100.00);
            turbineDataModel.FinalPower = powerAfterLosses + gearLosses;
            turbineDataModel.GeneratorEfficiency = generatorEfficiency;
            //Console.WriteLine("sidbfgojadgnosdangoanbgjadbgjkdbgaodjbg       :"+  turbineDataModel.AK25);
            return turbineDataModel.AK25;
        }

        public  double getPowerFromClosestEfficiency(double power, double eff){
            double generatorEfficiency = turbineDataModel.GeneratorEfficiency;
            double p =  (power * eff)/(generatorEfficiency);
            turbineDataModel.GeneratorEfficiency = eff;// * 100;
            return p;
        }

        public double getExhaustPressure(){
            return turbineDataModel.ExhaustPressure;
        }
        public  double getPowerFromTurbineEfficiency(double turbineEff){
            double pressure1 = getInletPressure(), pressure2 = getExhaustPressure();
            double temperature1  = getInletTemperature();
            double enthalpy1 = getInletEnthalpy(pressure1, temperature1);
            double outletEnthalpy = getOutletEnthalpy(pressure2,enthalpy1, turbineEff, pressure1);
            double initialPower = getMassFlowRate() * (enthalpy1 - outletEnthalpy);
            //need to send diff power not initial power, change this after confusion clears
            double gearLosses = calculateGearLosses(initialPower);
            double powerAfterLosses = initialPower - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses - gearLosses;
            double generatorEfficiency = calculateGeneratorEfficiency(powerAfterLosses);//AK23 as input
            
            turbineDataModel.AK25  = (powerAfterLosses) * (generatorEfficiency / 100.00) * (1.0 - turbineDataModel.Margin / 100.00);
            // Console.WriteLine("PPPPP: "+ turbineDataModel.AK25 + ", gen eff:"+ generatorEfficiency);
            // Console.WriteLine("E1:"+enthalpy1+", E2:"+outletEnthalpy+", GLOSS:"+ gearLosses);
            turbineDataModel.GeneratorEfficiency = generatorEfficiency;
            return turbineDataModel.AK25;
        }
        public double calculateGeneratorEfficiency(double power)
        {
            power = power / 1000.00;//convert to MW
                                    // Set the EPPlus license context to non-commercial
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            // Load the Excel package (workbook)
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Access the worksheet named "DAT_FILE_PARAMS"
                var worksheet = package.Workbook.Worksheets["DAT_FILE_PARAMS"];
                // Find the row where column C contains "MW"
                int startRow = 1; // Default starting row
                int lastRow = worksheet.Dimension.End.Row;
                for (int i = 1; i <= lastRow; i++)
                {
                    var cellValue = worksheet.Cells[i, 3].Text;
                    if (cellValue.Equals("MW", StringComparison.OrdinalIgnoreCase))
                    {
                        startRow = i + 1; // Start from the next row after "MW"
                        break;
                    }
                }
                // Initialize variables
                int maxPowerRow = -1;
                double maxPower = Double.MinValue;
                double correspondingEfficiency = 0;
                // Iterate through rows in column C starting from the row after "MW"
                for (int i = startRow; i <= lastRow; i++)
                {
                    // Get the value in column C
                    var cellValue = worksheet.Cells[i, 3].GetValue<double>();
                    // Check if the value is less than the input power
                    if (cellValue <= power)
                    {
                        maxPower = cellValue;
                        maxPowerRow = i;
                        correspondingEfficiency = worksheet.Cells[i, 4].GetValue<double>(); // Get the corresponding value in column D
                    }
                    else
                    {
                        // Since column C is sorted, we can break early
                        break;
                    }
                }
                // Display the result
                return correspondingEfficiency;
            }
        }

        public List<double> getGeneratorEfficiencies(double power, string variant = "")
        {
            List<double> generatorEffList = new List<double>();
            power = power / 1000.00;//convert to MW
                                    // Set the EPPlus license context to non-commercial
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            // Load the Excel package (workbook)
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Access the worksheet named "DAT_FILE_PARAMS"
                var worksheet = package.Workbook.Worksheets["DAT_FILE_PARAMS"];
                // Find the row where column C contains "MW"
                int startRow = 1; // Default starting row
                int lastRow = worksheet.Dimension.End.Row;
                for (int i = 1; i <= lastRow; i++)
                {
                    var cellValue = worksheet.Cells[i, 3].Text;
                    if (cellValue.Equals("MW", StringComparison.OrdinalIgnoreCase))
                    {
                        startRow = i + 1; // Start from the next row after "MW"
                        break;
                    }
                }
                // Initialize variables
                int maxPowerRow = -1;
                double maxPower = Double.MinValue;
                double correspondingEfficiency = 0;
                // Iterate through rows in column C starting from the row after "MW"
                for (int i = startRow; i <= lastRow; i++)
                {
                    // Get the value in column C
                    var cellValue = worksheet.Cells[i, 3].GetValue<double>();
                    // Check if the value is less than the input power
                    if (cellValue <= power)
                    {
                        maxPower = cellValue;
                        maxPowerRow = i;
                        correspondingEfficiency = worksheet.Cells[i, 4].GetValue<double>(); // Get the corresponding value in column D
                        for(int colNumber = 4; colNumber <= 7; ++colNumber)
                        {
                            generatorEffList.Add(worksheet.Cells[i, colNumber].GetValue<double>());
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return generatorEffList;
            }
        }

        public  double calculateGearLosses(double initialPower, double finalPow = 0)
        {
            double ar32 = finalPow / (0.986);
            double ar34 = ar32 - finalPow;
            if (finalPow == 0)
            {
                ar32 = initialPower / 0.986;
                ar34 = ar32 - initialPower;
            }
            double ar37 = 70.00;
            double ar39 = (initialPower);
            double loss = ar37 + ((ar34 - ar37) / (ar32 - ar37)) * (ar39 - ar37);
            return loss;
        }

        public double calculateGearLosses(double initialPower)
        {
            
            double ar32 = initialPower / 0.986;
            double ar34 = ar32 - initialPower;
            double ar37 = 70.00;
            double ar39 = (initialPower);
            double loss = ar37 + ((ar34 - ar37) / (ar32 - ar37)) * (ar39 - ar37);
            return loss;
        }
        public  double getLeakageTemperature()
        {
            return (double)(turbineDataModel.InletTemperature + turbineDataModel.OutletTemperature) / 2.0;
        }
        public double getLeakageEnthalpy()
        {
            //temp2 needs to filled before
            double leakageTemperature = getLeakageTemperature();
            return hVon_p_t(turbineDataModel.LeakagePressure, leakageTemperature, 0);
        }

        public double CalculateGearLosses(){
            double pressure1 = getInletPressure(), pressure2 = getExhaustPressure();
            double temperature1  = getInletTemperature();
            double enthalpy1 = getInletEnthalpy(pressure1, temperature1);
            double outletEnthalpy = getOutletEnthalpy(pressure2, enthalpy1, turbineDataModel.TurbineEfficiency, pressure1);
            double initialPower = getMassFlowRate() * (enthalpy1 - outletEnthalpy);
            return calculateGearLosses(initialPower - turbineDataModel.OilLosses - turbineDataModel.InventoryLosses);
        }

 }
 public class DataPoint
    {
        public double Efficiency { get; set; }
        public string Project { get; set; }
        public double Pressure1 { get; set; }
        public double Temperature { get; set; }
        public double MassFlowRate { get; set; }
        public double Pressure2 { get; set; }

        public string projID { get; set; }
        // Constructor
        public DataPoint(double efficiency, string project, double pressure1, double temperature, double massFlowRate, double pressure2, string refProjID)
        {
            Efficiency = efficiency;
            Project = project;
            Pressure1 = pressure1;
            Temperature = temperature;
            MassFlowRate = massFlowRate;
            Pressure2 = pressure2;
            projID = refProjID;
        }
    }
}