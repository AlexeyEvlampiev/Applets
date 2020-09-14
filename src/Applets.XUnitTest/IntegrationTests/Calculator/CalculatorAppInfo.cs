using System;
using Applets.Common;

namespace Applets.IntegrationTests.Calculator
{
    sealed class CalculatorAppInfo : AppInfo
    {
        public static readonly Guid ApplicationId = Guid.Parse("bc707d73-fcbf-43cb-903f-d1922cf64bbd");

        public const string RequestIntentGuid = "0e14b28b-f43f-4f24-9095-f0bb1f6ff0e2";
        public const string ResponseIntentGuid = "6a9b7777-81c9-481d-9e53-780c3f40796f";

        public static readonly Guid RequestIntentId = Guid.Parse(RequestIntentGuid);
        public static readonly Guid ResponseIntentId = Guid.Parse(ResponseIntentGuid);

        public static readonly Guid ClientAppletId = Guid.Parse("e8f5fd61-1dfd-4679-8e40-79f9899d4c8e");
        public static readonly Guid ServerAppletId = Guid.Parse("6a3fb61d-c06d-4324-82c6-19507670ce19");


        public CalculatorAppInfo() 
            : base(ApplicationId, "Balanced applications")
        {
            RegisterApplet(ClientAppletId, "Client applet");
            RegisterApplet(ServerAppletId, "Service applet");

            RegisterIntent(RequestIntentId, "Request");
            RegisterIntent(ResponseIntentId, "Response");

            RegisterFanInFanOutIntentBinding(RequestIntentId, ResponseIntentId);

            RegisterAppletNotifications(ServerAppletId,
                incoming: new[] { RequestIntentId },
                outgoing: new[] { ResponseIntentId });
        }
    }
}
