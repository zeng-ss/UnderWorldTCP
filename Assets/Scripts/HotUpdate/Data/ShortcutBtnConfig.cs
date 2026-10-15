using System;
using System.Collections.Generic;
using HotUpdate.UI;
using UnityEngine;

namespace HotUpdate.Data
{
    [CreateAssetMenu(menuName = "Config/ShortcutBtnConfig")]
    public class ShortcutBtnConfig : ScriptableObject
    {
        public List<ShortcutBtnData> shortcutButtonsConfig = new();
    }

    [Serializable]
    public struct ShortcutBtnData
    {
        public string name;
        public KeyCode key;
        public string spritePath;
        public PanelId panel;
    }
}