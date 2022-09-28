using System;
using System.Collections.Generic;
using System.IO;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace PluginSet.Firebase.Editor
{
    [BuildTools]
    public static class BuildFirebaseTools
    {
        [OnFrameworkInit]
        public static void SetGoogleServicesEnable(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
            {
                //避免firebase 云推送引起的崩溃
                var androidManifest = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
                if (File.Exists(androidManifest)) File.Delete(androidManifest);
                return;
            }

            context.Set("EnableGoogleServices", true);
        }
        
        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");

            // 目前逻辑这个文件必须的
            var mustPath = Path.Combine(Application.dataPath, "Firebase/m2repository");
            if (!Directory.Exists(mustPath))
                Directory.CreateDirectory(mustPath);

            if (!buildParams.Enable)
            {
                //屏蔽掉fireabse 云推送的插件
                var dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Messaging.Editor.dll");
                if (File.Exists(dllPath))
                {
                    if (File.Exists($"{dllPath}_")) File.Delete($"{dllPath}_");
                    File.Copy(dllPath, $"{dllPath}_");
                    File.Delete(dllPath);
                }
                //屏蔽掉firebase 崩溃插件
                dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Crashlytics.Editor.dll");
                if (File.Exists(dllPath))
                {
                    if (File.Exists($"{dllPath}_")) File.Delete($"{dllPath}_");
                    File.Copy(dllPath, $"{dllPath}_");
                    File.Delete(dllPath);
                }
                //屏蔽掉firebase插件
                dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Editor.dll");
                if (File.Exists(dllPath))
                {
                    if (File.Exists($"{dllPath}_")) File.Delete($"{dllPath}_");
                    File.Copy(dllPath, $"{dllPath}_");
                    File.Delete(dllPath);
                }
                return;
            }

            context.Symbols.Add("ENABLE_FIREBASE");

            //纠正云推送插件
            var _dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Messaging.Editor.dll_");
            var _newPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Messaging.Editor.dll");
            if (File.Exists(_dllPath))
            {
                if (File.Exists(_newPath)) File.Delete(_newPath);
                File.Copy(_dllPath, _newPath);
                File.Delete(_dllPath);
            }
            //纠正崩溃插件
            _dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Crashlytics.Editor.dll_");
            _newPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Crashlytics.Editor.dll");
            if (File.Exists(_dllPath))
            {
                if (File.Exists(_newPath)) File.Delete(_newPath);
                File.Copy(_dllPath, _newPath);
                File.Delete(_dllPath);
            }
            //纠正firebase插件
            _dllPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Editor.dll_");
            _newPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Editor/Firebase.Editor.dll");
            if (File.Exists(_dllPath))
            {
                if (File.Exists(_newPath)) File.Delete(_newPath);
                File.Copy(_dllPath, _newPath);
                File.Delete(_dllPath);
            }

            var dependenciesPath = Path.Combine(Global.GetPackageFullPath("com.pluginset.firebase"), "Dependencies");
            var targetDepPath = context.Get<string>("pluginDependenciesPath");
            
            if (!Directory.Exists(dependenciesPath) || !Directory.Exists(targetDepPath))
                throw new Exception($"Cannot find dependenciesPath from {dependenciesPath} to {targetDepPath}");
            
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "AnalyticsDependencies.xml");
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "AppDependencies.xml");
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "MessagingDependencies.xml");
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "CrashlyticsDependencies.xml");
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "PerformanceDependencies.xml");

            if (buildParams.EnableFirebaseLogin)
            {
                context.Symbols.Add("ENABLE_FIREBASE_LOGIN");
                Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "AuthDependencies.xml");
            }
            //ios 必须需要auth的依赖库
#if UNITY_IOS
            Global.CopyFileFromDirectory(dependenciesPath, targetDepPath, "AuthDependencies.xml");
#endif
        }

        [OnSyncExportSetting]
        public static void OnSyncExportSetting(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
            {
                // 删除不需要的模块
                var _dicPath = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseApp.androidlib");
                if (Directory.Exists(_dicPath)) Directory.Delete(_dicPath, true);
                Debug.Log("delete FirebaseCrashlytics.androidlib");
                _dicPath = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseCrashlytics.androidlib");
                if (Directory.Exists(_dicPath)) Directory.Delete(_dicPath, true);
                return;
            }

            context.AddLinkAssembly("PluginSet.Firebase");
            context.AddLinkAssembly("Firebase.Crashlytics");
            
            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.Get<PluginFirebaseConfig>("Firebase");
            config.SessionTimeoutSeconds = buildParams.SessionTimeoutSeconds;
            config.MaxWaitInitDuration = buildParams.MaxWaitInitDuration;

            //拷贝必须要使用到的模块
            var dicPath = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseApp.androidlib");
            if (Directory.Exists(dicPath)) Directory.Delete(dicPath, true);
            Global.CopyFiles("../PluginSet/Assets/PluginSetFirebase/Plugins/Android/FirebaseApp.androidlib", dicPath);

            Debug.Log("copy FirebaseCrashlytics.androidlib");
            dicPath = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseCrashlytics.androidlib");
            if (Directory.Exists(dicPath)) Directory.Delete(dicPath, true);
            Global.CopyFiles("../PluginSet/Assets/PluginSetFirebase/Plugins/Android/FirebaseCrashlytics.androidlib", dicPath);

            dicPath = Path.Combine(Application.dataPath, "Firebase/m2repository");
            if (Directory.Exists(dicPath)) Directory.CreateDirectory(dicPath);
        }

        [AndroidProjectModify]
        public static void OnAndroidProjectModify(BuildProcessorContext context, string projectPath)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
                return;

            Debug.Log("OnAndroidProjectModify firebase");
            var projRootDir = Path.Combine(projectPath, "..");
            // copy google-services.json
            var serverToPath = Path.Combine(projRootDir, "unityLibrary/src/main/assets");
            File.Copy("../PluginSet/Assets/PluginSetFirebase/Editor/google-services.json", Path.Combine(serverToPath, "google-services.json"), true);

            //加入firebase 性能工具的依赖
            var unityGradle = Path.Combine(projRootDir, "launcher", "build.gradle");
            if (!File.Exists(unityGradle))
                throw new BuildException($"Project {unityGradle} does not have build.gradle");
            var gradle = new GradleConfig(unityGradle);
            gradle.ROOT.AppendContentNode("apply plugin: 'com.google.firebase.firebase-perf'");
            File.WriteAllText(unityGradle, gradle.Print());

            var mainGradle = Path.Combine(projRootDir, "build.gradle");
            if (!File.Exists(mainGradle))
                throw new BuildException($"{mainGradle} does not exist");
            gradle = new GradleConfig(mainGradle);
            var node = gradle.ROOT.TryGetNode("allprojects/buildscript/dependencies");
            node.AppendContentNode("classpath 'com.google.firebase:perf-plugin:1.4.0'", "classpath 'com.google.firebase:perf-plugin:{0}'");
            node.AppendContentNode("classpath 'com.google.gms:google-services:4.3.10'", "classpath 'com.google.gms:google-services:{0}'");

            File.WriteAllText(mainGradle, gradle.Print());
        }

        [AndroidManifestModify]
        public static void OnAndroidManifestModify(BuildProcessorContext context, System.Xml.XmlDocument doc)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable || !buildParams.EnablePerformanceLog)
                return;

            //添加firebase性能工具的日志信息
            const string path = "/manifest/application";
            var applicationElems = doc.findElements(path, AndroidConst.NS_PREFIX);
            var application = applicationElems[0];

            var metaData = application.createSubElement("meta-data");
            metaData.SetAttribute("name", AndroidConst.NS_URI, "firebase_performance_logcat_enabled");

            doc.AddUsePermission("android.permission.ACCESS_COARSE_LOCATION");
            doc.AddUsePermission("android.permission.ACCESS_FINE_LOCATION");
        }


        [iOSXCodeProjectModify]
        public static void ModifyXCodeProject(BuildProcessorContext context, PBXProjectManager project)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
                return;

            var xcodeProject = project.Project;
#if UNITY_2019_3_OR_NEWER
            string xcodeTarget = xcodeProject.GetUnityFrameworkTargetGuid();
#else
            string xcodeTarget = xcodeProject.TargetGuidByName("Unity-iPhone");
#endif

            //云推送相关
            UnityEngine.Debug.Log("[Firebase]: Adding UserNotifications.framework to Xcode project.");
            xcodeProject.AddFrameworkToProject(xcodeTarget, "UserNotifications.framework", true);
            UnityEngine.Debug.Log("[Firebase]: UserNotifications.framework added successfully.");

            project.AddPushNotifications(true);
            project.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
        }
    }
}
