using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite_X.src.core.Models
{
    public class KGraphDataModel
    {
        public static KGraphDataModel kgraphDataModel;
        public static KGraphDataModel getInstance()
        {
            if (kgraphDataModel == null)
            {
                kgraphDataModel = new KGraphDataModel();
            }
            return kgraphDataModel;
        }



        public double _generatorNumber = 9;
        public double GeneratorNumber
        {
            get => _generatorNumber;
            set => _generatorNumber = value;
        }

    }
}
