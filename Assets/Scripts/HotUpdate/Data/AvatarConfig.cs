using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
    /// <summary>
    /// 预设头像表：库里只存编号，贴图从 Resources/Icon 读。
    /// 下标即头像编号，0 为默认头像；服务端 CenterServer/DB/AvatarCatalog.cs 的 Count 必须与本表一致。
    /// </summary>
    public static class AvatarConfig
    {
        private const string DefaultIcon = "Icon/hero_avatar";

        private static readonly string[] Icons =
        {
            DefaultIcon,
            "Icon/icon_male",
            "Icon/icon_female",
            "Icon/icon_warrior",
            "Icon/icon_hunter",
            "Icon/icon_caster",
            "Icon/faction_a",
            "Icon/faction_b",
            "Icon/faction_c",
        };

        public static int Count => Icons.Length;

        private static readonly Dictionary<int, Sprite> Cache = new();

        /// <summary>编号 → 头像贴图（越界 / 资源缺失一律回落到默认头像）</summary>
        public static Sprite Get(int avatar)
        {
            if (avatar < 0 || avatar >= Icons.Length) avatar = 0;
            if (Cache.TryGetValue(avatar, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>(Icons[avatar]);
            if (sprite == null) sprite = Resources.Load<Sprite>(DefaultIcon);
            Cache[avatar] = sprite;
            return sprite;
        }
    }
}
