using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.Extensions.Configuration;

namespace Models.PowerEfficiencyData{
    class PowerEfficiencyModel{
        private List<PowerEfficiencyDataPoint> powerEfficiencyPoints = new List<PowerEfficiencyDataPoint>();
        private IConfiguration configuration;
        private static PowerEfficiencyModel powerEfficiencyModel;

        public static PowerEfficiencyModel getInstance(){
            if(powerEfficiencyModel == null){
                powerEfficiencyModel = new PowerEfficiencyModel();
            }
            return powerEfficiencyModel;
        }
        public List<PowerEfficiencyDataPoint> PowerEfficiencyPoints
        {
            get { return powerEfficiencyPoints; }
            set { powerEfficiencyPoints = value; }
        }

        private PowerEfficiencyModel(){
        }

        public void fillPowerEfficiencyDataModel(){
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        
            string csvFilePath = configuration["AppSettings:DAT_FILE_PARAMS_CSV"];
            if (File.Exists(csvFilePath))
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // Skip the first 24 rows
                    for (int i = 1; i <= 24; i++)
                    {
                        csv.Read();
                    }
                    
                    // Manually read the records from the 25th row
                    while (csv.Read())
                    {
                        if(string.IsNullOrEmpty(csv.GetField<string>(2))){
                            break;
                        }
                        var power = Double.Parse(csv.GetField<string>(2));
                        
                        var eff_100 = Double.Parse(csv.GetField<string>(3));
                        var eff_75 = Double.Parse(csv.GetField<string>(4));
                        var eff_50 = Double.Parse(csv.GetField<string>(5));
                        var eff_25 = Double.Parse(csv.GetField<string>(6));
                        var dataPoint = new PowerEfficiencyDataPoint(
                            power, 
                            eff_100, 
                            eff_75, eff_50, eff_25
                        );
                        powerEfficiencyPoints.Add(dataPoint);
                    }
                }
            }
            else
            {
                Console.WriteLine($"[PowerEfficiencyModel]:CSV file not found at path: {csvFilePath}");
            }

        }
        
    }


    
}