#if ENABLE_FIREBASE
using System;
using System.Collections.Generic;
using Firebase.Analytics;
using PluginSet.Core;

namespace PluginSet.Firebase
{
    public partial class PluginFirebase: IAnalytics, IUserSet
    {
        private struct EventCacheItem
        {
            public string Name;
            public Parameter[] Parameters;
        }

        private string _userId = string.Empty;
        private Dictionary<string, object> _userInfo;

        private readonly List<EventCacheItem> _eventCacheItems = new List<EventCacheItem>();

        [FirebaseInitedExecutable]
        private void OnAnalyticsInited()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            var timeOutSeconds = _config.SessionTimeoutSeconds;
            var hour = (int) timeOutSeconds / 3600;
            var min = (int) (timeOutSeconds - hour * 60) / 60;
            var seconds = (int)timeOutSeconds % 60;
            FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(hour, min, seconds));

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
        
        [FirebaseDisposeExecutable]
        private void OnAnalyticsDispose()
        {
            SetUserInfo(false, null);
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
#if !UNITY_EDITOR
            foreach (var kv in eventData)
            {
                var parameterName = kv.Key;
                if (parameterNameMapping.TryGetValue(parameterName, out var p))
                    parameterName = p;
                list.Add(new Parameter(parameterName, kv.Value.ToString()));
            }
#endif
            LogEvent(eventName, list.ToArray());
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