#if ENABLE_FIREBASE
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using System.Threading.Tasks;
using Firebase.Messaging;
using PluginSet.Core;
using UnityEngine;
using Logger = PluginSet.Core.Logger;
using System.IO;

namespace PluginSet.Firebase
{   
    [PluginRegister]
    public partial class PluginFirebase : PluginBase, IStartPlugin, IAnalytics
    {
        private struct EventCacheItem
        {
            public string Name;
            public Parameter[] Parameters;
        }
        
        private static readonly Logger Logger = LoggerManager.GetLogger("Firebase");
        private const char ScreenSpliter = '#';
        
        public override string Name => "Firebase";
        public int StartOrder => -100000;
        public bool IsRunning { get; private set; }

        private FirebaseApp _appInstance;

        private PluginFirebaseConfig _config;

        private bool _isInited = false;
        
        private string _userId = string.Empty;
        private Dictionary<string, object> _userInfo;

        private string currentScreenName = string.Empty;
        private string currentScreenClass = string.Empty;
        private List<EventCacheItem> _eventCacheItems = new List<EventCacheItem>();
        private WaitForSecondsRealtime _waitForSeconds;

        protected override void Init(PluginSetConfig config)
        {
            _config = config.Get<PluginFirebaseConfig>("Firebase");
            
            AddEventListener(PluginConstants.NOTIFY_USER_ID, OnUserIdChanged);
            AddEventListener(PluginConstants.NOTIFY_USER_INFO, OnUserInfoChanged);
            AddEventListener(PluginConstants.FIREBASE_SET_CURRENT_SCREEN, SetCurrentScreen);
        }

        public IEnumerator StartPlugin()
        {
            if (IsRunning)
                yield break;

            IsRunning = true;

            // 加上 ContinueWithOnMainThread的初始化逻辑测试发现有一定几率会卡死 测试机器 模拟器 32位 by:ccj
            //          FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            //            {
            //                var dependencyStatus = task.Result;
            //                Logger.Debug("Firebase CheckAndFixDependenciesAsync result:{0}", dependencyStatus);
            //                if (dependencyStatus == DependencyStatus.Available)
            //                {
            //                    Logger.Debug("Firebase CheckAndFixDependenciesAsync completed!");
            //                    // Create and hold a reference to your FirebaseApp,
            //                    // where app is a Firebase.FirebaseApp property of your application class.
            //                    // Crashlytics will use the DefaultInstance, as well;
            //                    // this ensures that Crashlytics is initialized.
            //#if UNITY_EDITOR
            //                    var json = "{}";
            //                    var desktopFile = System.IO.Path.Combine(Application.dataPath, "google-services-desktop.json");
            //                    if (System.IO.File.Exists(desktopFile))
            //                        json = System.IO.File.ReadAllText(desktopFile);

            //                    _appInstance = FirebaseApp.Create(AppOptions.LoadFromJsonConfig(json), "Editor");
            //#else
            //                    _appInstance = FirebaseApp.DefaultInstance;
            //#endif
            //                    Logger.Debug("Firebase create app and init crashlytics completed!");
            //                    // Set a flag here for indicating that your project is ready to use Firebase.
            //                }
            //                else
            //                {
            //                    Logger.Error("Could not resolve all Firebase dependencies: {0}", dependencyStatus);
            //                    // Firebase Unity SDK is not safe to use here.
            //                }
            //                return task;
            //            }).Unwrap().ContinueWithOnMainThread(task =>
            //            {
            //                var dependencyStatus = task.Result;
            //                Logger.Debug("Firebase ContinueWithOnMainThread result:{0}", dependencyStatus);
            //                if (dependencyStatus == DependencyStatus.Available)
            //                {
            //                    InitFirebase();
            //                }
            //            });

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Logger.Debug("CheckAndFixDependenciesAsync complete");
#if UNITY_EDITOR
                    var json = "{}";
                    var desktopFile = Path.Combine(Application.dataPath, "../../PluginSet/Assets/PluginSetFirebase/Editor/google-services-desktop.json");
                    if (File.Exists(desktopFile))
                        json = File.ReadAllText(desktopFile);

                    var appEditroJsonPath = Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json");
                    if (!File.Exists(appEditroJsonPath))
                        File.Copy(desktopFile,appEditroJsonPath);
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
            
            _waitForSeconds = new WaitForSecondsRealtime(_config.MaxWaitInitDuration);
            yield return _waitForSeconds;
            _waitForSeconds = null;
        }

        public void DisposePlugin(bool isAppQuit = false)
        {
#if ENABLE_FIREBASE_LOGIN
            ClearLoginState();
#endif
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
            if (eventData == null || eventData.Count <= 0)
            {
                LogEvent(customEventName, null);
                return;
            }

            var list = new List<Parameter>();
            foreach (var kv in eventData)
            {
                list.Add(new Parameter(kv.Key, kv.Value.ToString()));
            }
            LogEvent(customEventName, list.ToArray());
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

            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;
            
#if ENABLE_FIREBASE_LOGIN
            InitFirebaseAuth();
#endif
            _isInited = true;
            Logger.Debug("Firebase init completed!");
            
            if (!string.IsNullOrEmpty(_userId))
                FirebaseAnalytics.SetUserId(_userId);

            FlushUserInfo();
            
            if (!string.IsNullOrEmpty(currentScreenName))
                SetCurrentScreen(currentScreenName, currentScreenClass);

            if (_eventCacheItems.Count > 0)
            {
                foreach (var item in _eventCacheItems)
                {
                    LogEvent(item.Name, item.Parameters);
                }
                _eventCacheItems.Clear();
            }
            
            AddEventListener(PluginConstants.NOTIFY_CLEAR_USER_INFO, OnUserInfoClear);
        }
            
        /// <summary>
        /// 收到新令牌
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="token"></param>
        private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
        {
            Logger.Debug("FirebaseMessage: Received Registration Token: {0}", token.Token);
        }

        /// <summary>
        /// 收到Message消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Logger.Debug("FirebaseMessage: Received a new message:  {0}", e.Message.From);
            SendNotification(PluginConstants.ON_MESSAGE_RECEIVED, e.Message.Link?.ToString());
        }
        
        private void OnUserInfoClear()
        {
            _userInfo = null;
        }
        
        private void OnUserIdChanged(PluginsEventContext context)
        {
            OnUserIdChanged((string) context.Data);
        }

        private void OnUserIdChanged(string userId)
        {
            if (_userId.Equals(userId))
                return;

            _userId = userId;
            if (IsRunning && _isInited && !string.IsNullOrEmpty(_userId))
                FirebaseAnalytics.SetUserId(_userId);
        }
        
        private void OnUserInfoChanged(PluginsEventContext context)
        {
            _userInfo = (Dictionary<string, object>) context.Data;
            FlushUserInfo();
        }

        private void SetCurrentScreen(PluginsEventContext context)
        {
            var str = (string) context.Data;
            if (string.IsNullOrEmpty(str))
                return;

            var list = str.Split(ScreenSpliter);
            SetCurrentScreen(list[0], list.Length > 1 ? list[1] : string.Empty);
        }

        private void SetCurrentScreen(string screen, string screenClass)
        {
            if (IsRunning && _isInited)
            {
                FirebaseAnalytics.SetCurrentScreen(screen, screenClass);
            }
            else
            {
                currentScreenName = screen;
                currentScreenClass = screenClass;
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
