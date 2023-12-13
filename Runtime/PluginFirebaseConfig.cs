using System.Collections;
using System.Collections.Generic;
using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Firebase
{
    public class PluginFirebaseConfig : ScriptableObject
    {
        public float SessionTimeoutSeconds;
        
        public float MaxWaitInitDuration = 0f;

        public SerializableDict<string, string> InternalEventMapping;
        public SerializableDict<string, string> InternalParamaterMapping;
    }
}
