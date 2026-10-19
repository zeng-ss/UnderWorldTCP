using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
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

        public static Sprite Get(int avatar)
        {
            if (avatar < 0 || avatar >= Icons.Length) avatar = 0;
            if (Cache.TryGetValue(avatar, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>(Icons[avatar]) ?? Resources.Load<Sprite>(DefaultIcon);
            Cache[avatar] = sprite;
            return sprite;
        }
    }
}