using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Applets.ComponentModel;

namespace Applets.Common
{
    public abstract class AppInfo : IAppInfo
    {
        public const string InfoIntentGuid = "10000000-0000-0000-0000-000000000000";
        public const string HeartbeatIntentGuid = "11000000-0000-0000-0000-000000000000";
        public const string WarningIntentGuid = "20000000-0000-0000-0000-000000000000";
        public const string ErrorIntentGuid = "30000000-0000-0000-0000-000000000000";

        public static readonly Guid InfoIntent = Guid.Parse(InfoIntentGuid);
        public static readonly Guid HeartbeatIntent = Guid.Parse(HeartbeatIntentGuid);
        public static readonly Guid WarningIntent = Guid.Parse(WarningIntentGuid);
        public static readonly Guid ErrorIntent = Guid.Parse(ErrorIntentGuid);

        private int _asserted;
        
        private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(10);

        private readonly Guid _applicationId;

        private readonly Dictionary<Guid, DtoInfo> _dtoTypeInfoByGuid = new Dictionary<Guid, DtoInfo>();
        private readonly HashSet<Assembly> _dtoAssemblies = new HashSet<Assembly>();
        private readonly Dictionary<Type, DtoSerializer> _dtoSerializerByType = new Dictionary<Type, DtoSerializer>();
        private readonly Dictionary<Guid, AppletInfo> _appletInfosById = new Dictionary<Guid, AppletInfo>();
        private readonly Dictionary<Guid, IntentInfo> _intentInfosById = new Dictionary<Guid, IntentInfo>();
        private readonly HashSet<AppletNotificationBinding> _appletIncomingNotificationIntentBindings = new HashSet<AppletNotificationBinding>();
        private readonly HashSet<AppletNotificationBinding> _appletOutgoingNotificationIntentBindings = new HashSet<AppletNotificationBinding>();
        private readonly HashSet<FanOutFanInIntentBinding> _fanOutFanInIntentBindings = new HashSet<FanOutFanInIntentBinding>();
        private readonly HashSet<Guid> _catchAllIncomingIntentAppletIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _fanOutIntentIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _standardIntentIds = new HashSet<Guid>()
        {
            InfoIntent, WarningIntent, ErrorIntent, HeartbeatIntent
        };
        private static readonly  ConcurrentDictionary<Type, AppInfo> _appInfosByType = new ConcurrentDictionary<Type, AppInfo>();

        readonly struct AppletNotificationBinding
        {
            public Guid AppletId { get; }
            public Guid IntentId { get; }

            public AppletNotificationBinding(Guid appletId, Guid intentId)
            {
                AppletId = appletId;
                IntentId = intentId;
            }
        }

        readonly struct FanOutFanInIntentBinding
        {
            public Guid FanOutIntentId { get; }
            public Guid FanInIntentId { get; }

            public FanOutFanInIntentBinding(Guid requestIntent, Guid fanInIntentId) : this()
            {
                FanOutIntentId = requestIntent;
                FanInIntentId = fanInIntentId;
            }
        }

        public static T GetOrCreate<T>() where T : AppInfo, new()
        {
            return (T)_appInfosByType.GetOrAdd(typeof(T), type =>
            {
                var appInfo = new T();
                appInfo.Assert();
                return appInfo;
            });
        }


        protected AppInfo(Guid id, string name)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Application ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Application name is required.", nameof(name));
            _applicationId = id;
            ApplicationName = name;

            RegisterDtoAssembly(GetType().Assembly);
        }

        public string ApplicationName { get; }


        protected void RegisterIncomingNotificationIntent(Guid applet, Guid intent)
        {
            if (!_appletInfosById.ContainsKey(applet))
                throw new ArgumentException($"Invalid applet ID. ID: {applet}");
            if (!_intentInfosById.ContainsKey(intent))
                throw new ArgumentException($"Invalid intentId ID. ID: {intent}");
            _appletIncomingNotificationIntentBindings.Add(new AppletNotificationBinding(applet, intent));
        }


        protected void RegisterOutgoingNotificationIntent(Guid applet, Guid intent)
        {
            if(!_appletInfosById.ContainsKey(applet))
                throw new ArgumentException($"Invalid applet ID. ID: {applet}");
            if (!_intentInfosById.ContainsKey(intent))
                throw new ArgumentException($"Invalid intentId ID. ID: {intent}");
            _appletOutgoingNotificationIntentBindings.Add(new AppletNotificationBinding(applet, intent));
        }

        

        protected void RegisterApplet(Guid applet, string name)
        {
            if (applet == Guid.Empty)
                throw new ArgumentException("Applet ID is required.", nameof(applet));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Applet name is required.", nameof(name));
            _appletInfosById.Add(applet, new AppletInfo(applet, name.Trim()));
        }

        protected void RegisterIntent(Guid intent, string name)
        {
            if(intent == Guid.Empty)
                throw new ArgumentException("Intent ID is required.", nameof(intent));
            if(string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Intent name is required.", nameof(name));
            if (_standardIntentIds.Contains(intent))
            {
                throw new InvalidOperationException(
                    new StringBuilder("Invalid intent id.")
                        .Append($" {intent} is a reserved intent id.")
                        .ToString());
            }

            _intentInfosById.Add(intent, new IntentInfo(intent, name.Trim()));
        }

        protected void RegisterDtoAssembly(Assembly assembly)
        {
            if (false == _dtoAssemblies.Add(assembly)) return;
            var items =
                from t in assembly.GetTypes()
                where t.IsClass && t.IsAbstract == false
                let guidAttribute = t.GetCustomAttribute<GuidAttribute>()
                where guidAttribute != null
                let ctor = t.GetConstructor(Array.Empty<Type>())
                let serializerAtt = t.GetCustomAttribute<DtoSerializerAttribute>()
                where ctor != null
                select new
                {
                    DtoType = t,
                    serializerAtt?.SerializerType 
                };

            foreach (var item in items)
            {
                var serializer = DtoSerializer.Default;
                if (item.SerializerType != null)
                {
                    if (!_dtoSerializerByType.ContainsKey(item.SerializerType))
                    {
                        var instance = Activator.CreateInstance(item.SerializerType);
                        serializer = (DtoSerializer) instance;
                        _dtoSerializerByType.Add(item.SerializerType, serializer);
                    }
                }
                if (_dtoTypeInfoByGuid.TryGetValue(item.DtoType.GUID, out var otherType))
                {
                    throw new InvalidOperationException($"Dto type Guid attributes must not be identical. See types {item.DtoType} and {otherType.DtoType}");
                }

                var info = new DtoInfo(item.DtoType, serializer);
                _dtoTypeInfoByGuid.Add(info.Contract, info);
            }
        }

        Guid IAppInfo.ApplicationId => _applicationId;

        string IAppInfo.ApplicationName => ApplicationName;

        public IEnumerable<AppletInfo> Applets => this._appletInfosById.Values;

        public TimeSpan HeartbeatInterval => DefaultHeartbeatInterval;

        

        public string GetIntentName(Guid intentCode)
        {
            if (_intentInfosById.TryGetValue(intentCode, out var info))
            {
                return info.Name;
            }

            return "Unknown";
        }

        public string GetAppletName(Guid applet)
        {
            if (_appletInfosById.TryGetValue(applet, out var info))
            {
                return info.Name;
            }

            return "Unknown";
        }

        public bool IsAppletId(Guid id) => _appletInfosById.ContainsKey(id);

        void IAppInfo.Assert()
        {
            if (Interlocked.CompareExchange(ref _asserted, 1, 0) == 0)
            {
                try
                {
                    this.Assert();
                }
                catch
                {
                    Interlocked.Exchange(ref _asserted, 0);
                    throw;
                }
                
            }
            else
            {
                Debug.Write($"Redundant call to {GetType()}.{nameof(Assert)}");
            }
        }
        public IEnumerable<IntentInfo> GetAppletIncomingIntents(Guid appletId)
        {
            return
                from b in _appletIncomingNotificationIntentBindings
                where b.AppletId == appletId
                select _intentInfosById[b.IntentId];
        }

        public bool CanReceiveEventNotification(Guid appletId, Guid publicEventIntentId)
        {
            return _appletIncomingNotificationIntentBindings.Contains(new AppletNotificationBinding(appletId, publicEventIntentId)) ||
                _catchAllIncomingIntentAppletIds.Contains(appletId);
        }

        public bool CanSend(Guid appletId, Guid intent)
        {
            bool result = _standardIntentIds.Contains(intent) ||
                          _fanOutIntentIds.Contains(intent) ||
                          _appletOutgoingNotificationIntentBindings.Contains(new AppletNotificationBinding(appletId, intent));
            if (!result)
            {
                Debug.WriteLine($"Cannot send {GetIntentName(intent)} from {GetAppletName(appletId)}");
            }
            return result;
        }

        protected virtual void Assert()
        {
            foreach (var intentId in _intentInfosById.Keys)
            {
                AssertIntentBalance(intentId);
            }

            AssertIntentIds(_fanOutIntentIds);
            AssertIntentIds(_appletIncomingNotificationIntentBindings.Select(b=> b.IntentId));
            AssertIntentIds(_appletOutgoingNotificationIntentBindings.Select(b => b.IntentId));

            AssertAppletIds(_catchAllIncomingIntentAppletIds);
            AssertAppletIds(_appletIncomingNotificationIntentBindings.Select(b=> b.AppletId));
            AssertAppletIds(_appletOutgoingNotificationIntentBindings.Select(b => b.AppletId));
        }

        private void AssertAppletIds(IEnumerable<Guid> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var invalidAppletIds = ids
                .Where(id => !_appletInfosById.ContainsKey(id))
                .ToList();
            if (invalidAppletIds.Any())
            {
                var csv = string.Join(", ", invalidAppletIds);
                throw new ValidationException(
                    new StringBuilder("Invalid applet ID.")
                        .Append($" Invalid ids: {csv}")
                        .Append($" Application: {ApplicationName}")
                        .Append($" See {GetType()} constructor.")
                        .ToString());
            }
        }

        private void AssertIntentIds(IEnumerable<Guid> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var invalidIntentIds = ids
                .Where(id => !_intentInfosById.ContainsKey(id))
                .ToList();
            if (invalidIntentIds.Any())
            {
                var csv = string.Join(", ", invalidIntentIds);
                throw new ValidationException(
                    new StringBuilder("Invalid intent ID.")
                        .Append($" Invalid ids: {csv}")
                        .Append($" Application: {ApplicationName}")
                        .Append($" See {GetType()} constructor.")
                        .ToString());
            }
        }

        private void AssertIntentBalance(Guid intentId)
        {
            bool isSent = _appletOutgoingNotificationIntentBindings.Any(b => b.IntentId == intentId) ||
                          _fanOutIntentIds.Contains(intentId);
            bool isReceived = _appletIncomingNotificationIntentBindings.Any(b => b.IntentId == intentId) ||
                              _fanOutFanInIntentBindings.Any(b => b.FanInIntentId == intentId);
            if (false == isSent)
            {
                throw new ValidationException(
                    new StringBuilder(
                            "The specified message intentId is not registered to be sent by any of the application applets.")
                        .Append($" IntentId: {GetIntentName(intentId)} ({intentId})")
                        .Append($" Application: {ApplicationName}")
                        .Append($" See {GetType()} constructor.")
                        .ToString());
            }

            if (false == isReceived)
            {
                throw new ValidationException(
                    new StringBuilder(
                            "The specified message intentId is not registered to be received by any of the application applets.")
                        .Append($" IntentId: {GetIntentName(intentId)} ({intentId})")
                        .Append($" Application: {ApplicationName}")
                        .Append($" See {GetType()} constructor.")
                        .ToString());
            }
        }

        public bool RequiresPublicInboxQueue(Guid appletId)
        {
            return _appletIncomingNotificationIntentBindings.Any(b => b.AppletId == appletId) ||
                   _catchAllIncomingIntentAppletIds.Contains(appletId);
        }

        [Obsolete]
        public bool IsExpectedReply(Guid appletId, Guid requestIntent, Guid replyIntent)
        {
            if (replyIntent == AppInfo.ErrorIntent)
                return true;
            var binding = new FanOutFanInIntentBinding(requestIntent, replyIntent);
            var found = _fanOutFanInIntentBindings.Contains(binding);
            return found;
        }

        public bool IsExpectedReply(Guid requestIntent, Guid replyIntent)
        {
            var binding = new FanOutFanInIntentBinding(requestIntent, replyIntent);
            var found = _fanOutFanInIntentBindings.Contains(binding);
            return found;
        }

        public object Deserialize(DeliveryArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (_dtoTypeInfoByGuid.TryGetValue(args.DataContractId, out var info))
            {
                return info.Serializer.Deserialize(args.Body, info.DtoType);
            }

            return args.Body;
        }

        public DispatchArgs ToDispatchArgs(object dto)
        {
            if (_dtoTypeInfoByGuid.TryGetValue(dto.GetType().GUID, out var info))
            {
                var body = info.Serializer.Serialize(dto, out var contentType);
                return new DispatchArgs(body)
                {
                    DataContractId = dto.GetType().GUID,
                    ContentType = contentType
                };
            }
            else if(dto is byte[] bytes)
            {
                return new DispatchArgs(bytes)
                {
                    ContentType = "application/octet-stream"
                };
            }
            else if(dto is string text)
            {
                var body = Encoding.UTF8.GetBytes(text);
                return new DispatchArgs(body)
                {
                    DataContractId = typeof(string).GUID,
                    ContentType = "text/plain"
                };
            }
            else
            {
                var body = DtoSerializer.Default.Serialize(dto, out var contentType);
                return new DispatchArgs(body)
                {
                    DataContractId = typeof(object).GUID,
                    ContentType = contentType
                };
            }
        }


        protected void RegisterCatchAllIntentsAppletPolicy(Guid appletId)
        {
            if(_appletIncomingNotificationIntentBindings.Any(b=> b.AppletId == appletId))
                throw new InvalidOperationException(
                    new StringBuilder("Conflicting incoming intentId bindings.")
                        //TODO: add more details
                        .ToString()
                    );
            _catchAllIncomingIntentAppletIds.Add(appletId);
        }

        protected void RegisterAppletNotifications(Guid appletId, Guid[] incoming, Guid[] outgoing)
        {
            foreach (var intentId in incoming ?? Array.Empty<Guid>())
            {
                RegisterIncomingNotificationIntent(appletId, intentId);
            }

            foreach (var intentId in outgoing ?? Array.Empty<Guid>())
            {
                RegisterOutgoingNotificationIntent(appletId, intentId);
            }
        }

        protected void RegisterFanInFanOutIntentBinding(Guid fanOutIntentId, Guid fanInIntentId)
        {
            _fanOutIntentIds.Add(fanOutIntentId);
            _fanOutFanInIntentBindings.Add(new FanOutFanInIntentBinding(fanOutIntentId, fanInIntentId));
        }

        protected void RegisterResponses(Guid fanOutIntentId, params Guid[] responseIntentIds)
        {
            foreach (var intentId in responseIntentIds)
            {
                RegisterFanInFanOutIntentBinding(fanOutIntentId, intentId);
            }
        }
    }
}
