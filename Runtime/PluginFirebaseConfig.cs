using System.Collections;
using System.Collections.Generic;
using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Firebase
{
    [PluginSetConfig("Firebase")]
    public class PluginFirebaseConfig : ScriptableObject
    {
        public float SessionTimeoutSeconds;
        
        public float MaxWaitInitDuration = 0f;
    }
}
