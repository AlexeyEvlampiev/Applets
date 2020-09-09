﻿using System;
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

        private Dictionary<Guid, DtoInfo> _dtoTypeInfoByGuid = new Dictionary<Guid, DtoInfo>();
        private HashSet<Assembly> _dtoAssemblies = new HashSet<Assembly>();
        private Dictionary<Type, DtoSerializer> _dtoSerializerByType = new Dictionary<Type, DtoSerializer>();
        private Dictionary<Guid, AppletInfo> _appletInfosById = new Dictionary<Guid, AppletInfo>();
        private Dictionary<Guid, IntentInfo> _intentInfosById = new Dictionary<Guid, IntentInfo>();
        private HashSet<Binding> _incomingMessageBindings = new HashSet<Binding>();
        private HashSet<Binding> _outgoingMessageBindings = new HashSet<Binding>();
        private HashSet<Binding> _privateResponseBindings = new HashSet<Binding>();
        private HashSet<ReplyBinding> _fanInIntentBindings = new HashSet<ReplyBinding>();
        private HashSet<Guid> _catchAllIncomingIntentAppletIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _standardIntents = new HashSet<Guid>()
        {
            InfoIntent, WarningIntent, ErrorIntent, HeartbeatIntent
        };
        private static readonly  ConcurrentDictionary<Type, AppInfo> _appInfosByType = new ConcurrentDictionary<Type, AppInfo>();

        readonly struct Binding
        {
            public Guid Applet { get; }
            public Guid Intent { get; }

            public Binding(Guid applet, Guid intent)
            {
                Applet = applet;
                Intent = intent;
            }
        }

        readonly struct ReplyBinding
        {
            public Guid Applet { get; }
            public Guid RequestIntent { get; }
            public Guid ReplyIntent { get; }

            public ReplyBinding(Guid applet, Guid requestIntent, Guid replyIntent) : this()
            {
                Applet = applet;
                RequestIntent = requestIntent;
                ReplyIntent = replyIntent;
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


        protected AppInfo(Guid id, string applicationName)
        {
            _applicationId = id;
            ApplicationName = applicationName;

            RegisterDtoAssembly(GetType().Assembly);
        }

        public string ApplicationName { get; }


        protected void RegisterIncomingIntent(Guid applet, Guid intent)
        {
            if (!_appletInfosById.ContainsKey(applet))
                throw new ArgumentException($"Invalid applet ID. ID: {applet}");
            if (!_intentInfosById.ContainsKey(intent))
                throw new ArgumentException($"Invalid intent ID. ID: {intent}");
            _incomingMessageBindings.Add(new Binding(applet, intent));
        }

        protected void RegisterFanInIntent(Guid applet, Guid requestIntent, Guid responseIntent)
        {
            if (!_appletInfosById.ContainsKey(applet))
                throw new ArgumentException($"Invalid applet ID. ID: {applet}");
            if (!_intentInfosById.ContainsKey(requestIntent))
                throw new ArgumentException($"Invalid intent ID. ID: {requestIntent}");
            if (!_intentInfosById.ContainsKey(responseIntent))
                throw new ArgumentException($"Invalid intent ID. ID: {responseIntent}");
            _outgoingMessageBindings.Add(new Binding(applet, requestIntent));
            _privateResponseBindings.Add(new Binding(applet, responseIntent));
            var binding = new ReplyBinding(applet, requestIntent, responseIntent);
            _fanInIntentBindings.Add(binding);
            Debug.Assert(_fanInIntentBindings.Contains(binding));
        }

        protected void RegisterOutgoingIntent(Guid applet, Guid intent)
        {
            if(!_appletInfosById.ContainsKey(applet))
                throw new ArgumentException($"Invalid applet ID. ID: {applet}");
            if (!_intentInfosById.ContainsKey(intent))
                throw new ArgumentException($"Invalid intent ID. ID: {intent}");
            _outgoingMessageBindings.Add(new Binding(applet, intent));
        }

        

        protected void RegisterApplet(Guid applet, string name)
        {
            _appletInfosById.Add(applet, new AppletInfo(applet, name));
        }

        protected void RegisterIntent(Guid intent, string name)
        {
            _intentInfosById.Add(intent, new IntentInfo(intent, name));
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
                this.Assert();
            }
            else
            {
                Debug.Write($"Redundant call to {GetType()}.{nameof(Assert)}");
            }
        }
        public IEnumerable<IntentInfo> GetAppletIncomingIntents(Guid appletId)
        {
            return
                from b in _incomingMessageBindings
                where b.Applet == appletId
                select _intentInfosById[b.Intent];
        }

        public bool CanReceiveEventNotification(Guid appletId, Guid publicEventIntentId)
        {
            return _incomingMessageBindings.Contains(new Binding(appletId, publicEventIntentId)) ||
                _catchAllIncomingIntentAppletIds.Contains(appletId);
        }

        public bool CanSend(Guid appletId, Guid intent)
        {
            bool result = _standardIntents.Contains(intent) || 
                   _outgoingMessageBindings.Contains(new Binding(appletId, intent));
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
                bool isSent = _outgoingMessageBindings.Any(b => b.Intent == intentId);
                bool isReceived = _incomingMessageBindings.Any(b => b.Intent == intentId) ||
                                  _privateResponseBindings.Any(b=> b.Intent == intentId);
                if (false == isSent)
                {
                    throw new ValidationException(
                        new StringBuilder("The specified message intent is not registered to be sent by any of the application applets.")
                            .Append($" Intent: {GetIntentName(intentId)} ({intentId})")
                            .Append($" Application: {ApplicationName}")
                            .Append($" See {GetType()} constructor.")
                            .ToString());
                }

                if (false == isReceived)
                {
                    throw new ValidationException(
                        new StringBuilder("The specified message intent is not registered to be received by any of the application applets.")
                            .Append($" Intent: {GetIntentName(intentId)} ({intentId})")
                            .Append($" Application: {ApplicationName}")
                            .Append($" See {GetType()} constructor.")
                            .ToString());
                }
            }
        }

        public bool RequiresPublicInboxQueue(Guid appletId)
        {
            return _incomingMessageBindings.Any(b => b.Applet == appletId) ||
                   _catchAllIncomingIntentAppletIds.Contains(appletId);
        }

        public bool IsExpectedReply(Guid appletId, Guid requestIntent, Guid replyIntent)
        {
            var binding = new ReplyBinding(appletId, requestIntent, replyIntent);
            var found = _fanInIntentBindings.Contains(binding);
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
            if(_incomingMessageBindings.Any(b=> b.Applet == appletId))
                throw new InvalidOperationException(
                    new StringBuilder("Conflicting incoming intent bindings.")
                        //TODO: add more details
                        .ToString()
                    );
            _catchAllIncomingIntentAppletIds.Add(appletId);
        }
    }
}
