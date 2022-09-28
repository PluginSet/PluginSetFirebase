using System.IO;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Firebase.Editor
{
    [InitializeOnLoad]
    public static class EditorServiceFileChecker
    {
        static EditorServiceFileChecker()
        {
            if (Application.isBatchMode)
                return;

            var buildChannel = EditorSetting.CurrentBuildChannel;
            var buildParams = buildChannel.Get<BuildFirebaseParams>("Firebase");

            if (buildParams == null || !buildParams.Enable)
                return;
            
            var desktopFile = Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json");
            if (File.Exists(desktopFile))
                return;

            var jsonFile = buildParams.GoogleServiceJson;
            if (string.IsNullOrEmpty(jsonFile))
                return;

            jsonFile = Path.Combine(Application.dataPath, "..", jsonFile);
            if (!File.Exists(jsonFile))
                return;
            
            File.Copy(jsonFile, desktopFile);
        }
    }
}