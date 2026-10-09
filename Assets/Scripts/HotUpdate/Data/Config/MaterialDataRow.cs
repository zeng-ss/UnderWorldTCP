namespace HotUpdate.Data.Config
{
    // materialData 配置表的一行，供 Json 反序列化使用。
    // 字段名必须与 Luban 导出的 tbmaterialdata.json 的键名完全一致（区分大小写）。
    public class MaterialDataRow
    {
        public int materialId; // 材料编号（5=金币，6-9=四种驱动材料）
        public string name; // 材料名
        public string iconName; // 图标资源名
        public int expValue; // 作为升级材料时提供的经验值
    }
}
