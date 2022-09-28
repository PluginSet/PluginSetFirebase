﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;

namespace PluginSet.Firebase.Editor
{
    [BuildTools]
    public static class BuildFirebaseTools
    {
        private static void CopyDependencyPath(string fileName)
        {
            Global.CopyDependenciesFileInLib("com.pluginset.firebase", fileName, "Dependencies",
                delegate(string src, string dst)
                {
                    var text = File.ReadAllText(src);
                    text = text.Replace("$LIB_PATH", Global.GetPackageFullPath("com.pluginset.firebase"));
                    File.WriteAllText(dst, text);
                });
        }

        private static SerializableDict<string, string> TransferMapping(SerializableDict<string, string> mapping)
        {
            var dict = new Dictionary<string, string>();
            foreach (var kv in mapping.Pairs)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    continue;
                
                if (dict.ContainsKey(kv.Value))
                    throw new Exception("Internal mapping has save value with " + kv.Value + " Key: " + kv.Key);
                
                dict.Add(kv.Value, kv.Key);
            }
            
            return new SerializableDict<string, string>(dict);
        }

        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
                return;

            context.Symbols.Add("ENABLE_FIREBASE");

            CopyDependencyPath("AnalyticsDependencies.xml");
            CopyDependencyPath("AppDependencies.xml");
            CopyDependencyPath("CrashlyticsDependencies.xml");
            
            if (buildParams.EnableFirebaseLogin)
            {
                context.Symbols.Add("ENABLE_FIREBASE_LOGIN");
                CopyDependencyPath("AuthDependencies.xml");
            } else if (context.BuildTarget == BuildTarget.iOS)
            {
                CopyDependencyPath("AuthDependencies.xml");
            }

            context.AddLinkAssembly("PluginSet.Firebase");
            context.AddLinkAssembly("Firebase.Crashlytics");
            
            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.Get<PluginFirebaseConfig>("Firebase");
            config.SessionTimeoutSeconds = buildParams.SessionTimeoutSeconds;
            config.MaxWaitInitDuration = buildParams.MaxWaitInitDuration;
            config.InternalEventMapping = TransferMapping(buildParams.InternalEventMapping);
            config.InternalParamaterMapping = TransferMapping(buildParams.InternalParamaterMapping);
        }

        [AndroidProjectModify]
        public static void OnAndroidProjectModify(BuildProcessorContext context, AndroidProjectManager projectManager)
        {
            var buildParams = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!buildParams.Enable)
                return;

            var serverToPath = Path.Combine(projectManager.LibraryPath, "src", "main", "assets");
            File.Copy(buildParams.GoogleServiceJson, Path.Combine(serverToPath, "google-services.json"), true);

            var node = projectManager.ProjectGradle.ROOT.GetOrCreateNode("allprojects/buildscript/dependencies");
            node.AppendContentNode("classpath 'com.google.firebase:firebase-crashlytics-gradle:2.1.1'");
            
            var root = projectManager.LauncherGradle.ROOT;
            const string applyPlugin = "apply plugin: 'com.google.firebase.crashlytics'";
            root.RemoveContentNode(applyPlugin);
            root.InsertChildNode(new GradleContentNode(applyPlugin, root), 2);
            
//            //加入firebase 性能工具的依赖
//            var unityGradle = Path.Combine(projRootDir, "launcher", "build.gradle");
//            if (!File.Exists(unityGradle))
//                throw new BuildException($"Project {unityGradle} does not have build.gradle");
//            var gradle = new GradleConfig(unityGradle);
//            gradle.ROOT.AppendContentNode("apply plugin: 'com.google.firebase.firebase-perf'");
//            File.WriteAllText(unityGradle, gradle.Print());
//
//            var mainGradle = Path.Combine(projRootDir, "build.gradle");
//            if (!File.Exists(mainGradle))
//                throw new BuildException($"{mainGradle} does not exist");
//            gradle = new GradleConfig(mainGradle);
//            var node = gradle.ROOT.TryGetNode("allprojects/buildscript/dependencies");
//            node.AppendContentNode("classpath 'com.google.firebase:perf-plugin:1.4.0'", "classpath 'com.google.firebase:perf-plugin:{0}'");
//            node.AppendContentNode("classpath 'com.google.gms:google-services:4.3.10'", "classpath 'com.google.gms:google-services:{0}'");
//
//            File.WriteAllText(mainGradle, gradle.Print());
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
            
            File.Copy(buildParams.GoogleServicePlist, Path.Combine(project.ProjectPath, "GoogleService-Info.plist"), true);

//            //云推送相关
//            UnityEngine.Debug.Log("[Firebase]: Adding UserNotifications.framework to Xcode project.");
//            xcodeProject.AddFrameworkToProject(xcodeTarget, "UserNotifications.framework", true);
//            UnityEngine.Debug.Log("[Firebase]: UserNotifications.framework added successfully.");

//            project.AddPushNotifications(true);
//            project.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
        }
    }
}
