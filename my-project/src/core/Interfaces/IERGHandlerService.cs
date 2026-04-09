using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces.IERGHandlerService;

public interface IERGHandlerService
{
    double ExtractPowerFromERG(string filePath);
    double ExtractVolFlowFromERG(string filePath);
    double ExtractInletTemperatureFromERG(string filePath);
    double ExtractExhaustPressureFromERG(string filePath);
    double ExtractInletPressureFromERG(string filePath);

    double ExtractVolFlowFromERGForDesuparator(string filePath);
    double ExtractVolFlowFromERGForPRV(string filePath);
}
