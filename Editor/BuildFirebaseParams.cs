using System.Collections;
using System.Collections.Generic;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Firebase.Editor
{
    [BuildChannelsParams("Firebase", "Firebase配置")]
    public class BuildFirebaseParams: ScriptableObject
    {
        [Tooltip("是否启用Firebase插件")]
        public bool Enable;

        [Tooltip("是否启用Firebase登录")]
        public bool EnableFirebaseLogin;
        
        [Tooltip("统计平台超时设置（秒）")]
        public float SessionTimeoutSeconds = 1800;

        [Tooltip("等待初始化最大时长（秒）")]
        public float MaxWaitInitDuration = 0f;

        [Tooltip("是否查看Firebase性能事件的日志信息")]
        public bool EnablePerformanceLog;
    }
}
