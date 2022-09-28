using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Firebase.Editor
{
    [InitializeOnLoad]
    public class FirebasePluginsFilter
    {
        private static bool FilterPlugin(string s, BuildProcessorContext context)
        {
            var param = context.BuildChannels.Get<BuildFirebaseParams>("Firebase");
            if (!param.Enable)
            {
                Debug.Log("Filter lib file :::::::  " + s);
            }

            return !param.Enable;
        }
        
        static FirebasePluginsFilter()
        {
            PluginFilter.RegisterFilter("com.pluginset.firebase/Plugins", FilterPlugin);
            PluginFilter.RegisterFilter("com.pluginset.firebase/Plugins/iOS", FilterPlugin);
            PluginFilter.RegisterFilter("com.pluginset.firebase/Plugins/iOS/Plugin", FilterPlugin);
            PluginFilter.RegisterFilter("com.pluginset.firebase/Plugins/iOS/Firebase", FilterPlugin);
            PluginFilter.RegisterFilter("com.pluginset.firebase/Plugins/x84_64", FilterPlugin);
        }
    }
}