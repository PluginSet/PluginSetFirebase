#if ENABLE_FIREBASE
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
    public partial class PluginFirebase : PluginBase, IStartPlugin
    {
        [AttributeUsage(AttributeTargets.Method)]
        private class FirebaseInitedExecutableAttribute: ExecutableAttribute
        {
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        private class FirebaseDisposeExecutableAttribute: ExecutableAttribute
        {
        }
        
        private static readonly Logger Logger = LoggerManager.GetLogger("Firebase");
        
        public override string Name => "Firebase";

        public int StartOrder => PluginsStartOrder.SdkDefault;
        public bool IsRunning { get; private set; }

        private FirebaseApp _appInstance;

        private PluginFirebaseConfig _config;

        private bool _isInited = false;
        
        private WaitForSecondsRealtime _waitForSeconds;

        private Dictionary<string, string> eventNameMapping;
        private Dictionary<string, string> parameterNameMapping;

        protected override void Init(PluginSetConfig config)
        {
            _config = config.Get<PluginFirebaseConfig>();
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

            try
            {
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
            }
            catch (Exception e)
            {
                Logger.Error("Firebase init failed: " + e);
                yield break;
            }

            if (_isInited || _config.MaxWaitInitDuration <= 0)
                yield break;
            
            _waitForSeconds = new WaitForSecondsRealtime(_config.MaxWaitInitDuration / 1000f);
            yield return _waitForSeconds;
            _waitForSeconds = null;
        }

        public void DisposePlugin(bool isAppQuit = false)
        {
            ExecuteAll<FirebaseDisposeExecutableAttribute>();
        }
        
        private void InitFirebase()
        {
            Logger.Debug("Firebase init start:::");
            if (_waitForSeconds != null)
                _waitForSeconds.waitTime = 0;
            
            _isInited = true;
            
            ExecuteAll<FirebaseInitedExecutableAttribute>();
            Logger.Debug("Firebase init completed!");
        }
    }
}
#endif
