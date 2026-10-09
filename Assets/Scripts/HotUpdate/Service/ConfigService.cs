using System.Collections.Generic;
using HotUpdate.Data.Config;
using UnityEngine;

namespace HotUpdate.Service
{
    /// <summary>
    /// 配置表服务：读取 Luban 导出的 Json 配置表，是客户端所有静态配置的唯一来源。
    /// </summary>
    public class ConfigService
    {
        private const string TableMaterialData = "tbmaterialdata";
        private const string TableDriverDisk = "tbdriverdisk";
        private const string TableDialogue = "tbdialogue";
        private const string TableDialogueLine = "tbdialogueline";
        private const string TableDialogueOption = "tbdialogueoption";

        public List<MaterialDataRow> Materials { get; private set; }
        public List<DriverDiskRow> DriverDisks { get; private set; }
        public List<DialogueRow> Dialogues { get; private set; }
        public List<DialogueLineRow> DialogueLines { get; private set; }
        public List<DialogueOptionRow> DialogueOptions { get; private set; }

        public void Load()
        {
            Materials = LoadTable<MaterialDataRow>(TableMaterialData);
            DriverDisks = LoadTable<DriverDiskRow>(TableDriverDisk);
            Dialogues = LoadTable<DialogueRow>(TableDialogue);
            DialogueLines = LoadTable<DialogueLineRow>(TableDialogueLine);
            DialogueOptions = LoadTable<DialogueOptionRow>(TableDialogueOption);
        }

        public void Clear()
        {
            Materials = null;
            DriverDisks = null;
            Dialogues = null;
            DialogueLines = null;
            DialogueOptions = null;
        }

        // 读一张表；文件不存在或为空时返回空列表并报错，避免调用方拿到 null。
        private static List<T> LoadTable<T>(string tableName)
        {
            List<T> rows = JsonMgr.Instance.LoadData<List<T>>(tableName);
            if (rows is { Count: > 0 }) return rows;
            Debug.LogError($"ConfigService: 配置表 {tableName}.json 不存在或为空。" +
                           "请先执行 LubanConfig/MiniTemplate/gen.bat 生成到 Assets/StreamingAssets。");
            return new List<T>();
        }
    }
}