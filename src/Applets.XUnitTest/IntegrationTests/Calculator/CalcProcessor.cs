using System;
using Applets.Common;
using Applets.ComponentModel;

namespace Applets.IntegrationTests.Calculator
{
    public class CalcProcessor : AppletDeliveryProcessor
    {
        public CalcProcessor(IAppletChannelFactory factory) 
            : base(factory.Create(CalculatorAppInfo.ServerAppletId))
        {
        }

        public CalcProcessor(IAppletChannel channel) : base(channel)
        {
        }

        [Intent(CalculatorAppInfo.RequestIntentGuid)]
        protected void Calc(IDeliveryArgs args, CalcRequest request)
        {
            try
            {
                double result = default;
                switch (request.Operation)
                {
                    case (CalcOperation.Add):
                        result = request.Lhs + request.Rhs;
                        break;
                    case (CalcOperation.Subtract):
                        result = request.Lhs - request.Rhs;
                        break;
                    case (CalcOperation.Multiply):
                        result = request.Lhs * request.Rhs;
                        break;
                    case (CalcOperation.Divide):
                        result = request.Lhs / request.Rhs;
                        if(double.IsInfinity(result))
                            throw new DivideByZeroException();
                        break;
                    default:
                        throw new InvalidOperationException($"{request.Operation} is invalid");
                }

                
                args.ReplyWithAsync(new CalcResponse()
                {
                    Result = result
                });
            }
            catch (Exception e)
            {
                args.ReplyWithErrorAsync(e.Message);
            }
            

            
        }
    }
}
