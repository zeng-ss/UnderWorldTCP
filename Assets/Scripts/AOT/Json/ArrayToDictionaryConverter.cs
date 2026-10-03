using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ArrayToDictionaryConverter<TKey, TValue> : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Dictionary<TKey, TValue>).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jArray = JArray.Load(reader);
        var dict = new Dictionary<TKey, TValue>();

        foreach (var item in jArray)
        {
            if (item is JArray kvPair && kvPair.Count == 2)
            {
                var key = kvPair[0].ToObject<TKey>(serializer);
                var value = kvPair[1].ToObject<TValue>(serializer);
                dict[key] = value;
            }
            else
            {
                throw new JsonSerializationException("Invalid key-value pair format");
            }
        }

        return dict;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var dict = (Dictionary<TKey, TValue>)value;
        var array = new JArray();

        foreach (var kv in dict)
        {
            var kvPair = new JArray
            {
                JToken.FromObject(kv.Key, serializer),
                JToken.FromObject(kv.Value, serializer)
            };
            array.Add(kvPair);
        }

        array.WriteTo(writer);
    }
}