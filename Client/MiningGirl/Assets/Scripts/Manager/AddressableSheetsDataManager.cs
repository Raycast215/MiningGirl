using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

[AttributeUsage(AttributeTargets.Class)]
public class DataFileAttribute : Attribute
{
    public string BaseFileName;

    public DataFileAttribute(string baseFileName)
    {
        BaseFileName = baseFileName;
    }
}

public static class AddressableSheetsDataManager
{
    private static readonly Dictionary<string, Type> _typeByBase = new();
    private static readonly Dictionary<Type, object> _cache = new();

    private static bool _initialized;

    public static async Task<bool> LoadLabelAsync(string label, CancellationToken ct = default)
    {
        EnsureTypeIndex();

        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(TextAsset));
        var locations = await locHandle.Task;

        if (locations == null || locations.Count == 0)
        {
            Debug.LogWarning($"[DataManager] No locations for label: {label}");
            Addressables.Release(locHandle);
            return false;
        }

        int success = 0;
        int fail = 0;

        foreach (var loc in locations)
        {
            ct.ThrowIfCancellationRequested();

            var handle = Addressables.LoadAssetAsync<TextAsset>(loc);
            var ta = await handle.Task;

            if (ta == null)
            {
                Debug.LogError($"[DataManager] TextAsset null: {loc.PrimaryKey}");
                fail++;
                continue;
            }

            string baseName = GetBaseName(ta.name).ToLowerInvariant();

            if (!_typeByBase.TryGetValue(baseName, out var targetType))
            {
                Debug.LogWarning($"[DataManager] No mapping for: {baseName}");
                continue;
            }

            try
            {
                Type listType = typeof(List<>).MakeGenericType(targetType);
                var list = JsonConvert.DeserializeObject(ta.text, listType);

                _cache[targetType] = list ?? Activator.CreateInstance(listType);

                int count = GetCount(_cache[targetType]);

                Debug.Log($"[DataManager] Loaded: {targetType.Name} count={count}");

                success++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Parse failed: {targetType.Name}\n{e}");
                fail++;
            }

            Addressables.Release(handle);
        }

        Addressables.Release(locHandle);

        Debug.Log($"[DataManager] Load complete success={success}, fail={fail}");

        _initialized = true;
        return fail == 0;
    }

    public static IReadOnlyList<T> GetAll<T>()
    {
        if (_cache.TryGetValue(typeof(T), out var obj) && obj is List<T> list)
            return list;

        Debug.LogError($"[DataManager] Cache miss: {typeof(T).Name}");
        return Array.Empty<T>();
    }

    private static void EnsureTypeIndex()
    {
        if (_initialized)
            return;

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<DataFileAttribute>();
            if (attr == null)
                continue;

            string key = attr.BaseFileName.ToLowerInvariant();

            if (_typeByBase.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate key: {key}");
                continue;
            }

            _typeByBase.Add(key, type);
        }

        Debug.Log($"[DataManager] TypeIndex Count={_typeByBase.Count}");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch { return Array.Empty<Type>(); }
    }

    private static string GetBaseName(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot >= 0 ? name.Substring(0, dot) : name;
    }

    private static int GetCount(object list)
    {
        if (list == null) return 0;
        var prop = list.GetType().GetProperty("Count");
        return prop != null ? (int)prop.GetValue(list) : 0;
    }
}