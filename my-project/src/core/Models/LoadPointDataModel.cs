  using System.Runtime.CompilerServices;
  using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;


namespace Models.LoadPointDataModel{

    public class LoadPointDataModel{
        private List<LoadPoint> loadPoints;
        private static LoadPointDataModel loadPointDataModel;

        
        private IConfiguration configuration;
        public LoadPointDataModel()
        {
            loadPoints = new List<LoadPoint>(); // Initialize the list
        }
        public static LoadPointDataModel getInstance(){
            if(loadPointDataModel == null){
                loadPointDataModel = new LoadPointDataModel();
            }
            return loadPointDataModel;
        }

        public List<LoadPoint> LoadPoints{
            get{return loadPoints;}
            set{loadPoints = value;}
        }

        public void fillLoadPoints(){
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();

            int LOAD_POINT_COUNT = configuration.GetValue<int>("AppSettings:LOAD_POINT_COUNT");
            for(int i = 1; i <= Math.Max(10, LOAD_POINT_COUNT + 5); ++i)
                loadPoints.Add(new LoadPoint());
            
            loadPoints[1].LPName = "Base Load Case";
            loadPoints[2].LPName = "10% Higher Backpressure";
            loadPoints[3].LPName = "20% Lower Backpressure";
            loadPoints[4].LPName = "Temp -30 Degrees";
            loadPoints[5].LPName = "Temp -30 Degrees and -20% Backpressure";
            loadPoints[6].LPName = "Valve Point";
            loadPoints[7].LPName = "MCR- Minimum Continuous Rating";
            loadPoints[8].LPName = "MCR with Temp -30 Degrees";
            loadPoints[9].LPName = "No Load (100 kW)";
            loadPoints[10].LPName = "No Load (100 kW) and 1.013 Backpressure";
        }

    }
    
  }