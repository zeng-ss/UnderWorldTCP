using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 序列化和反序列化Json时  使用的是哪种方案    有两种  JsonUtility 不能直接序列化字典  ligJson可以序列化字典 
/// </summary>
public enum JsonType    
{
    JsonUtility,
    LitJson,
    Newtonsoft,
}

/// <summary>
/// Json数据管理类 主要用于进行 Json的序列化存储到硬盘 和 反序列化从硬盘中读取到内存中
/// </summary>
public class JsonMgr : SingleTonMono<JsonMgr>
{
    public JsonMgr() { }

    //存储Json数据 序列化
    public void SaveData(object data, string fileName, string directPath = "", JsonType type = JsonType.Newtonsoft)
    {
        //确定存储路径
        string directoryPath = Application.persistentDataPath+"/"+directPath;
        string filepath = directoryPath + fileName + ".json";
        //序列化 得到Json字符串
        string jsonStr = "";
        switch (type)
        {
            case JsonType.JsonUtility:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
            case JsonType.Newtonsoft:
                jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                break;
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        //把序列化的Json字符串 存储到指定路径的文件中
        File.WriteAllText(filepath, jsonStr);
    }
    
    

    //读取指定文件中的 Json数据 反序列化
    public T LoadData<T>(string fileName,string loadPath="", JsonType type = JsonType.Newtonsoft) where T : new()
    {
        //数据对象
        T data = new T();
        //确定从哪个路径读取
        //首先先判断 默认数据文件夹中是否有我们想要的数据 如果有 就从中获取
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        //先判断 是否存在这个文件
        //如果不存在默认文件 就从 读写文件夹中去寻找
        if(!File.Exists(path))  //如果是读取持久化路径下面的文件  那么就把 loadpath传进来 
            path = loadPath + "/" + fileName + ".json";
        //如果读写文件夹中都还没有 那就返回一个默认对象
        if (!File.Exists(path))
            return data;
        //进行反序列化
        string jsonStr = File.ReadAllText(path);
        switch (type)
        {
            case JsonType.JsonUtility:
                data = JsonUtility.FromJson<T>(jsonStr);
                break;
            case JsonType.LitJson:
                data = JsonMapper.ToObject<T>(jsonStr);
                break;
            case JsonType.Newtonsoft:
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonStr);
                break;
        }
        //把对象返回出去
        return data;
    }
}
