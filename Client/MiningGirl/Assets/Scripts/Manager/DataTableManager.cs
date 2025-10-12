using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 특정 JSON 파일(baseName.json)과 클래스를 매핑하기 위한 Attribute.
/// 예) [DataFile("Stage")] → "Stage.json"
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DataFileAttribute : Attribute
{
    public string BaseFileName { get; }
    public DataFileAttribute(string baseFileName) => BaseFileName = baseFileName;
}

/// <summary>
/// Addressables 라벨로 묶인 JSON(TextAsset)들을 로드해서
/// [DataFile("BaseName")] Attribute가 붙은 타입으로 자동 파싱/캐싱하는 매니저.
/// - JSON 루트는 배열여야 함: [{...}, {...}]
/// - 파일명에서 확장자를 뺀 "BaseName"으로 타입을 찾음 (대소문자 무시)
/// - GetAll<T>()로 파싱된 List<T>를 조회
/// </summary>
public static class DataTableManager
{
    // 타입 인덱스: baseName(lower) → 타입
    private static readonly Dictionary<string, Type> _typeByBase = new();
    // 로드된 데이터 캐시: 타입 → List<T> (object로 박싱)
    private static readonly Dictionary<Type, object> _cache = new();
    private static bool _typesIndexed;

    /// <summary> Addressables 라벨로 로드 (모든 JSON 파싱, 캐시 갱신). </summary>
    public static async Task LoadLabelAsync(string label, CancellationToken ct = default)
    {
        EnsureTypeIndex();

        // 라벨에 해당하는 모든 로케이션을 조회
        AsyncOperationHandle<IList<IResourceLocation>> locHandle =
            Addressables.LoadResourceLocationsAsync(label, typeof(TextAsset));

        IList<IResourceLocation> locations;
        try
        {
            locations = await locHandle.Task;
        }
        finally
        {
            Addressables.Release(locHandle);
        }

        int loaded = 0, skipped = 0;

        foreach (var loc in locations)
        {
            ct.ThrowIfCancellationRequested();

            // TextAsset 로드
            AsyncOperationHandle<TextAsset> loadHandle = Addressables.LoadAssetAsync<TextAsset>(loc);
            TextAsset ta = null;
            try
            {
                ta = await loadHandle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesSheetsDataManager] 에셋 로드 실패: {loc.PrimaryKey}\n{e}");
            }
            finally
            {
                // TextAsset 인스턴스를 즉시 해제하지 않고, 필요시 유지하고 싶다면 Release 생략 가능.
                // 여기서는 텍스트만 복사 후 바로 해제.
                if (ta != null)
                {
                    // 파싱 후 아래에서 Release
                }
            }

            if (ta == null)
            {
                skipped++;
                continue;
            }

            try
            {
                // baseName 계산: Addressables는 보통 PrimaryKey가 에셋 경로/이름.
                // 확장자 제거한 이름을 사용.
                string baseName = GetBaseName(ta.name);
                string key = baseName.ToLowerInvariant();

                if (!_typeByBase.TryGetValue(key, out var targetType))
                {
                    // 매핑된 타입 없음 → 스킵
                    skipped++;
                }
                else
                {
                    LoadTextForType(targetType, ta.text);
                    loaded++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressablesSheetsDataManager] 파싱 실패: {loc.PrimaryKey}\n{ex}");
            }
            finally
            {
                Addressables.Release(loadHandle); // TextAsset 해제
            }
        }

        Debug.Log($"[AddressablesSheetsDataManager] 라벨 로드 완료: '{label}'  로드={loaded}, 스킵={skipped}");
    }

    /// <summary> 특정 타입의 모든 데이터를 반환 (없으면 빈 배열). </summary>
    public static IReadOnlyList<T> GetAll<T>()
    {
        if (_cache.TryGetValue(typeof(T), out var listObj) && listObj is List<T> list)
            return list;
        return Array.Empty<T>();
    }

    /// <summary> 모든 캐시 삭제 (타입 인덱스는 유지). </summary>
    public static void ClearCache() => _cache.Clear();

    /// <summary> [DataFile] 타입 인덱스 구축 (한 번만). </summary>
    private static void EnsureTypeIndex()
    {
        if (_typesIndexed) return;
        _typeByBase.Clear();

        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes);

        foreach (var t in allTypes)
        {
            if (!t.IsClass || t.IsAbstract) continue;
            var attr = t.GetCustomAttribute<DataFileAttribute>();
            if (attr == null) continue;

            var key = (attr.BaseFileName ?? "").Trim();
            if (string.IsNullOrEmpty(key)) continue;

            key = key.ToLowerInvariant();
            if (!_typeByBase.ContainsKey(key))
                _typeByBase.Add(key, t);
        }

        _typesIndexed = true;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        catch { return Array.Empty<Type>(); }
    }

    private static string GetBaseName(string nameOrPath)
    {
        // Addressables의 TextAsset.name 은 보통 파일명(확장자 없음).
        // 혹시 경로 형태면 마지막 path segment만 사용.
        if (string.IsNullOrEmpty(nameOrPath)) return "";
        var lastSlash = Mathf.Max(nameOrPath.LastIndexOf('/'), nameOrPath.LastIndexOf('\\'));
        var justName = lastSlash >= 0 ? nameOrPath[(lastSlash + 1)..] : nameOrPath;

        // 이미 확장자 없는 name일 확률이 높지만, 혹시 모를 점에 대비해 한 번 더 제거.
        var dot = justName.LastIndexOf('.');
        return dot >= 0 ? justName.Substring(0, dot) : justName;
    }

    // ───────── JSON 파싱 래퍼 (JsonUtility는 루트 배열 불가) ─────────

    private static void LoadTextForType(Type targetType, string jsonArrayText)
    {
        if (string.IsNullOrWhiteSpace(jsonArrayText))
        {
            _cache[targetType] = CreateEmptyList(targetType);
            return;
        }

        string wrapped = WrapArray(jsonArrayText);

        var wrapperType = typeof(ListWrapper<>).MakeGenericType(targetType);
        var parsed = JsonUtility_FromJson(wrapped, wrapperType);

        var itemsField = wrapperType.GetField("items");
        var listObj = itemsField?.GetValue(parsed) ?? CreateEmptyList(targetType);

        _cache[targetType] = listObj;
    }

    private static string WrapArray(string raw)
    {
        var s = raw.TrimStart();
        if (s.Length > 0 && s[0] == '[') // 루트 배열이면 감싼다
            return "{\"items\":" + raw + "}";
        return raw; // 이미 감싸져 있거나 객체 형태면 그대로(필요시 정책 조정)
    }

    private static object CreateEmptyList(Type t)
    {
        var listType = typeof(List<>).MakeGenericType(t);
        return Activator.CreateInstance(listType);
    }

    [Serializable]
    private class ListWrapper<T> { public List<T> items = new(); }

    private static object JsonUtility_FromJson(string json, Type t)
    {
        // JsonUtility.FromJson<T> 제네릭만 제공 → 리플렉션으로 호출
        var m = typeof(JsonUtility).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(x => x.Name == "FromJson" &&
                                 x.IsGenericMethodDefinition &&
                                 x.GetGenericArguments().Length == 1 &&
                                 x.GetParameters().Length == 1 &&
                                 x.GetParameters()[0].ParameterType == typeof(string));
        if (m == null)
            throw new MissingMethodException("JsonUtility.FromJson<T>(string) not found.");

        var gm = m.MakeGenericMethod(t);
        return gm.Invoke(null, new object[] { json });
    }
}
