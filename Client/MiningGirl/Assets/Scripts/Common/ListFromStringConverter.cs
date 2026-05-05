using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ListFromStringConverter<T> : JsonConverter<List<T>>
{
    public override List<T>? ReadJson(
        JsonReader reader,
        Type objectType,
        List<T>? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var result = new List<T>();

        if (reader.TokenType == JsonToken.StartArray)
            return serializer.Deserialize<List<T>>(reader);

        if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
        {
            result.Add(ConvertValue(reader.Value!.ToString()!));
            return result;
        }

        if (reader.TokenType == JsonToken.String)
        {
            var str = reader.Value?.ToString();

            if (string.IsNullOrWhiteSpace(str))
                return result;

            foreach (var item in str.Split(',', StringSplitOptions.RemoveEmptyEntries))
                result.Add(ConvertValue(item.Trim()));

            return result;
        }

        throw new JsonSerializationException($"Invalid token: {reader.TokenType}");
    }

    public override void WriteJson(JsonWriter writer, List<T>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    private static T ConvertValue(string value)
    {
        if (typeof(T).IsEnum)
            return (T)Enum.Parse(typeof(T), value, true);

        return (T)Convert.ChangeType(value, typeof(T));
    }
}