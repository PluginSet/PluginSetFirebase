﻿﻿#if ENABLE_FIREBASE
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using PluginSet.Core;
using UnityEngine;
using Logger = PluginSet.Core.Logger;
using System.IO;
using System.Text;

namespace PluginSet.Firebase
{   
    [PluginRegister]
    public partial class PluginFirebase : PluginBase, IStartPlugin, IAnalytics, IUserSet
    {
        private struct EventCacheItem
        {
            public string Name;
            public Parameter[] Parameters;
        }
        
        private static readonly Logger Logger = LoggerManager.GetLogger("Firebase");
        
        public override string Name => "Firebase";

        public int StartOrder => PluginsStartOrder.SdkDefault;
        public bool IsRunning { get; private set; }

        private FirebaseApp _appInstance;

        private PluginFirebaseConfig _config;

        private bool _isInited = false;
        
        private string _userId = string.Empty;
        private Dictionary<string, object> _userInfo;

        private readonly List<EventCacheItem> _eventCacheItems = new List<EventCacheItem>();
        
        private WaitForSecondsRealtime _waitForSeconds;

        private Dictionary<string, string> eventNameMapping;
        private Dictionary<string, string> parameterNameMapping;

        protected override void Init(PluginSetConfig config)
        {
            _config = config.Get<PluginFirebaseConfig>("Firebase");
            eventNameMapping = new Dictionary<string, string>();
            parameterNameMapping = new Dictionary<string, string>();
            foreach (var kv in _config.InternalEventMapping.Pairs)
            {
                eventNameMapping.Add(kv.Key, kv.Value);
            }

            foreach (var kv in _config.InternalParamaterMapping.Pairs)
            {
                parameterNameMapping.Add(kv.Key, kv.Value);
            }
        }

        public IEnumerator StartPlugin()
        {
            if (IsRunning)
                yield break;

            IsRunning = true;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Logger.Debug("CheckAndFixDependenciesAsync complete");
#if UNITY_EDITOR
                    var json = "{}";
                    var desktopFile = Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json");
                    if (File.Exists(desktopFile))
                        json = File.ReadAllText(desktopFile);

                    _appInstance = FirebaseApp.Create(AppOptions.LoadFromJsonConfig(json), "Editor");
#else
                    _appInstance = FirebaseApp.DefaultInstance;
#endif
                    InitFirebase();

                    Logger.Debug("Firebase create app and init crashlytics completed!");
                }
            });

            if (_isInited || _config.MaxWaitInitDuration <= 0)
                yield break;
            
            _waitForSeconds = new WaitForSecondsRealtime(_config.MaxWaitInitDuration / 1000f);
            yield return _waitForSeconds;
            _waitForSeconds = null;
        }

        public void DisposePlugin(bool isAppQuit = false)
        {
#if ENABLE_FIREBASE_LOGIN
            ClearLoginState();
#endif
            SetUserInfo(false, null);
        }

        public void SetUserInfo(bool isNewUser, string userId, Dictionary<string, object> pairs = null)
        {
            if (string.Equals(_userId, userId))
                return;
            
            _userId = userId;
            _userInfo = pairs;
            if (IsRunning && _isInited && !string.IsNullOrEmpty(_userId))
            {
                FirebaseAnalytics.SetUserId(_userId);
                FlushUserInfo();
            }
        }

        public void ClearUserInfo()
        {
            _userInfo = null;
        }

        public void FlushUserInfo()
        {
            if (!IsRunning || !_isInited || _userInfo == null) return;
            foreach (var kv in _userInfo)
            {
                FirebaseAnalytics.SetUserProperty(kv.Key, kv.Value.ToString());
            }
        }

        public void CustomEvent(string customEventName, Dictionary<string, object> eventData = null)
        {
            var eventName = customEventName;
            if (eventNameMapping.TryGetValue(eventName, out var e))
                eventName = e;
            
            if (eventData == null || eventData.Count <= 0)
            {
                LogEvent(eventName, null);
                return;
            }

            var list = new List<Parameter>();
            foreach (var kv in eventData)
            {
                var parameterName = kv.Key;
                if (parameterNameMapping.TryGetValue(parameterName, out var p))
                    parameterName = p;
                list.Add(new Parameter(parameterName, kv.Value.ToString()));
            }
            LogEvent(eventName, list.ToArray());
        }
        
        private void InitFirebase()
        {
            Logger.Debug("Firebase init start:::");
            if (_waitForSeconds != null)
                _waitForSeconds.waitTime = 0;
            
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            var timeOutSeconds = _config.SessionTimeoutSeconds;
            var hour = (int) timeOutSeconds / 3600;
            var min = (int) (timeOutSeconds - hour * 60) / 60;
            var seconds = (int)timeOutSeconds % 60;
            FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(hour, min, seconds));
            
#if ENABLE_FIREBASE_LOGIN
            InitFirebaseAuth();
#endif
            _isInited = true;
            Logger.Debug("Firebase init completed!");

            if (!string.IsNullOrEmpty(_userId))
            {
                FirebaseAnalytics.SetUserId(_userId);
                FlushUserInfo();
            }
            
            if (_eventCacheItems.Count > 0)
            {
                foreach (var item in _eventCacheItems)
                {
                    LogEvent(item.Name, item.Parameters);
                }
                _eventCacheItems.Clear();
            }
        }
        
        private void LogEvent(string eventName, Parameter[] parameters)
        {
            if (IsRunning && _isInited)
            {
                if (parameters == null || parameters.Length <= 0)
                    FirebaseAnalytics.LogEvent(eventName);
                else
                    FirebaseAnalytics.LogEvent(eventName, parameters);
                
                Logger.Debug("Firebase LogEvent {0}", eventName);
            }
            else
            {
                _eventCacheItems.Add(new EventCacheItem
                {
                    Name = eventName,
                    Parameters = parameters
                });
            }
        }
    }
}
#endif
