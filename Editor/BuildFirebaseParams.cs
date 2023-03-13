﻿using System.Collections.Generic;
using System.IO;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEngine;

namespace PluginSet.Firebase.Editor
{
    [BuildChannelsParams("Firebase", "Firebase配置")]
    [VisibleCaseBoolValue("SupportAndroid", true)]
    [VisibleCaseBoolValue("SupportIOS", true)]
    public class BuildFirebaseParams: ScriptableObject
    {
        [Tooltip("是否启用Firebase插件")]
        public bool Enable;

        [Tooltip("是否启用Firebase登录")]
        public bool EnableFirebaseLogin;
        
        [Tooltip("统计平台超时设置（秒）")]
        public float SessionTimeoutSeconds = 1800;

        [Tooltip("等待初始化最大时长（毫秒）")]
        public int MaxWaitInitDuration = 0;

        [Tooltip("Google-service.json文件路径")]
        [BrowserFile("Google-service.json文件路径", "json")]
        public string GoogleServiceJson;

        [Tooltip("GoogleService-Info.plist文件路径")]
        [BrowserFile("GoogleService-Info.plist文件路径", "plist")]
        public string GoogleServicePlist;

        [Tooltip("内部事件名称映射表")] [SerializableDict("stringValue", "")]
        public SerializableDict<string, string> InternalEventMapping;

        [Tooltip("内部参数名称映射表")] [SerializableDict("stringValue", "")]
        public SerializableDict<string, string> InternalParamaterMapping;

        private void OnValidate()
        {
            if (InternalEventMapping?.Pairs == null || InternalEventMapping.Pairs.Length <= 0)
            {
                InternalEventMapping = new SerializableDict<string, string>(new Dictionary<string, string>()
                {
                    {"login", ""},
                    {"purchase", ""},
                    {"refund", ""},
                    {"search", ""},
                    {"share", ""},
                    {"ad_impression", ""},
                    {"app_open", ""},
                    {"begin_checkout", ""},
                    {"campaign_details", ""},
                    {"view_cart", ""},
                    {"generate_lead", ""},
                    {"join_group", ""},
                    {"level_start", ""},
                    {"level_end", ""},
                    {"level_up", ""},
                    {"post_score", ""},
                    {"screen_view", ""},
                    {"select_content", ""},
                    {"select_item", ""},
                    {"select_promotion", ""},
                    {"sign_up", ""},
                    {"tutorial_begin", ""},
                    {"tutorial_complete", ""},
                    {"unlock_achievement", ""},
                    {"view_item", ""},
                    {"view_promotion", ""},
                    {"add_payment_info", ""},
                    {"add_shipping_info", ""},
                    {"add_to_cart", ""},
                    {"add_to_wishlist", ""},
                    {"remove_from_cart", ""},
                    {"earn_virtual_currency", ""},
                    {"spend_virtual_currency", ""},
                    {"view_item_list", ""},
                    {"view_search_results", ""},
                });
            }

            if (InternalParamaterMapping?.Pairs == null || InternalParamaterMapping.Pairs.Length <= 0)
            {
                InternalParamaterMapping = new SerializableDict<string, string>(new Dictionary<string, string>()
                {
                    { "affiliation", "" },
                    { "campaign", "" },
                    { "character", "" },
                    { "content", "" },
                    { "coupon", "" },
                    { "currency", "" },
                    { "destination", "" },
                    { "discount", "" },
                    { "index", "" },
                    { "items", "" },
                    { "level", "" },
                    { "location", "" },
                    { "medium", "" },
                    { "method", "" },
                    { "origin", "" },
                    { "price", "" },
                    { "quantity", "" },
                    { "score", "" },
                    { "shipping", "" },
                    { "source", "" },
                    { "success", "" },
                    { "tax", "" },
                    { "term", "" },
                    { "value", "" },
                    { "achievement_id", "" },
                    { "ad_format", "" },
                    { "ad_platform", "" },
                    { "ad_source", "" },
                    { "ad_unit_name", "" },
                    { "aclid", "" },
                    { "content_type", "" },
                    { "cp1", "" },
                    { "creative_format", "" },
                    { "creative_name", "" },
                    { "end_date", "" },
                    { "extend_session", "" },
                    { "flight_number", "" },
                    { "group_id", "" },
                    { "creative_slot", "" },
                    { "item_brand", "" },
                    { "item_category", "" },
                    { "item_category2", "" },
                    { "item_category3", "" },
                    { "item_category4", "" },
                    { "item_category5", "" },
                    { "item_id", "" },
                    { "item_name", "" },
                    { "item_variant", "" },
                    { "item_list_name", "" },
                    { "item_list_id", "" },
                    { "level_name", "" },
                    { "marketing_tactic", "" },
                    { "payment_type", "" },
                    { "promotion_name", "" },
                    { "promotion_id", "" },
                    { "screen_name", "" },
                    { "screen_class", "" },
                    { "search_term", "" },
                    { "shipping_tier", "" },
                    { "source_platform", "" },
                    { "start_date", "" },
                    { "transaction_id", "" },
                    { "travel_class", "" },
                    { "campaign_id", "" },
                    { "location_id", "" },
                    { "number_of_nights", "" },
                    { "number_of_passengers", "" },
                    { "number_of_rooms", "" },
                    { "virtual_currency_name", "" },
                });
            }
        }
    }
}
