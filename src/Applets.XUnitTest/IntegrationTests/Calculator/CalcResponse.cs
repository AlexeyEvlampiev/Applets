using System.Runtime.InteropServices;

namespace Applets.IntegrationTests.Calculator
{
    [Guid(CalculatorAppInfo.ResponseIntentGuid)]
    public sealed class CalcResponse
    {
        public double Result { get; set; }
    }
}
