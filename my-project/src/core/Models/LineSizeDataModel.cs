using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite_X.src.core.Models
{
    public class LineSizeDataModel
    {
        public Dictionary<string, List<Dictionary<string, object>>> LineSize { get; set; }

        public LineSizeDataModel()
        {
            LineSize = new Dictionary<string, List<Dictionary<string, object>>>
        {
            { "Pipes", new List<Dictionary<string, object>>() }
        };
        }
    }
}
