using System;
using Xunit;
// ReSharper disable All

namespace Applets.Common
{
    public class AppletInfo_Assert_Should
    {
        [Fact]
        public void SucceedForBalancedApp()
        {
            IAppInfo target = new CalculatorAppInfo();
            target.Assert();
        }
        
    }
}
