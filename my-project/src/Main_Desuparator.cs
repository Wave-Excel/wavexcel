using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces.IThermodynamicLibrary;
using Models.TurbineData;
using StartExecutionMain;

namespace mainDesuparator;
    

    public class Main_Desuparator
    {
        public TurbineDataModel turbineDataModel;
        private static IThermodynamicLibrary thermodynamicService;
        public Main_Desuparator() { 
            turbineDataModel = TurbineDataModel.getInstance();
            thermodynamicService = StartExec.GlobalHost.Services.GetRequiredService<IThermodynamicLibrary>();

        }
        public static void MainDesuparator()
        {
            
        }

    }
