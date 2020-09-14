using System.Runtime.InteropServices;
using Applets.ComponentModel;

namespace Applets.IntegrationTests.Calculator
{
    [Guid(CalculatorAppInfo.RequestIntentGuid)]
    public sealed class CalcRequest : FanOutRequest<CalcResponse>
    {
        public CalcRequest()
        {
            
        }

        public CalcRequest(double lhs, double rhs, CalcOperation operation)
        {
            Lhs = lhs;
            Rhs = rhs;
            Operation = operation;
        }
        public double Lhs { get; set; }
        public double Rhs { get; set; }
        public CalcOperation Operation { get; set; }
    }
}
