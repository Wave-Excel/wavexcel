using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.Extensions.Configuration;


//DAT_FILE_PARAMS file
namespace Models.NozzleTurbaData{
    public class NozzleTurbaDataModel{
        private List<NozzleTurbaData> nozzleTurbaDataList = new List<NozzleTurbaData>();

        private IConfiguration configuration;
        private static NozzleTurbaDataModel nozzleTurbaDataModel;
        public static NozzleTurbaDataModel getInstance(){
            if(nozzleTurbaDataModel == null){
                nozzleTurbaDataModel = new NozzleTurbaDataModel();
            }
            return nozzleTurbaDataModel;
        }
        private NozzleTurbaDataModel(){

        }

        public List<NozzleTurbaData> NozzleTurbaDataList
        {
            get { return nozzleTurbaDataList; }
            set { nozzleTurbaDataList = value; }
        }

        public void fillNozzleTurbaDataModel(){
            
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
        
            string csvFilePath = configuration["AppSettings:DAT_FILE_PARAMS_CSV"];
            if (File.Exists(csvFilePath))
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // Skip the first three rows
                    for (int i = 0; i <= 3; i++)
                    {
                        csv.Read();
                        Console.WriteLine("HEllo"+csv.GetField<string>(1));
                    }
                    
                    // Manually read the records from the 4th row
                    while (csv.Read())
                    {
                        var inletVolumetricFlowRange = csv.GetField<string>(2);
                        if(string.IsNullOrEmpty(inletVolumetricFlowRange)){
                            break;
                        }
                        var fmin1 = Double.Parse(csv.GetField<string>(3));
                        var fminGes = Double.Parse(csv.GetField<string>(4));
                        var initLimit = Double.Parse(csv.GetField<string>(5));
                        var remark = csv.GetField<string>(5);
                        
                        var dataPoint = new NozzleTurbaData(
                            inletVolumetricFlowRange, 
                            fmin1, 
                            fminGes, 
                            initLimit, 
                            remark
                        );
                        nozzleTurbaDataList.Add(dataPoint);
                    }
                }
            }
            else
            {
                Console.WriteLine($"[NozzleTurbaDataModel]:CSV file not found at path: {csvFilePath}");
            }

        }

    }

    public class NozzleTurbaData{
        private string inlet_volumetric_flow_range;
        private double fmin1, fmin_ges, init_limit;
        private string remark;
        public NozzleTurbaData(string inletVolumetricFlowRange, double fmin1, double fminGes, double initLimit, string remark)
        {
            this.inlet_volumetric_flow_range = inletVolumetricFlowRange;
            this.fmin1 = fmin1;
            this.fmin_ges = fminGes;
            this.init_limit = initLimit;
            this.remark = remark;
        }

        public NozzleTurbaData(){
            
        }
        public string InletVolumetricFlowRange
        {
            get { return inlet_volumetric_flow_range; }
            set { inlet_volumetric_flow_range = value; }
        }

        public double Fmin1
        {
            get { return fmin1; }
            set { fmin1 = value; }
        }

        public double FminGes
        {
            get { return fmin_ges; }
            set { fmin_ges = value; }
        }

        public double InitLimit
        {
            get { return init_limit; }
            set { init_limit = value; }
        }

        public string Remark
        {
            get { return remark; }
            set { remark = value; }
        }
    }
}