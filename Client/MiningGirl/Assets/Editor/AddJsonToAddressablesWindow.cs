#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Addressables
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public class AddJsonToAddressablesWindow : EditorWindow
{
    // 기본값은 네가 JSON을 뱉는 폴더로 맞춰 둠
    private string jsonFolder = "Assets/Data/SheetsJson";
    private string groupName  = "DataTable";     // 없으면 생성
    private string labelName  = "DataTable";     // 없으면 생성
    private bool   useFileNameAsAddress = true; // true면 파일명(확장자 제외)로 address 설정

    [MenuItem("Tools/DataTable/Add JSONs to Addressables")]
    private static void Open()
    {
        var w = GetWindow<AddJsonToAddressablesWindow>("JSON → Addressables");
        w.minSize = new Vector2(520, 220);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(6);
        EditorGUILayout.HelpBox("지정한 폴더의 .json 파일들을 Addressables에 자동 추가하고 라벨을 붙입니다.", MessageType.Info);

        EditorGUILayout.LabelField("소스 폴더", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            jsonFolder = EditorGUILayout.TextField(new GUIContent("JSON Folder", "예: Assets/Data/SheetsJson"), jsonFolder);
            if (GUILayout.Button("찾기", GUILayout.Width(70)))
            {
                var abs = EditorUtility.OpenFolderPanel("JSON 폴더 선택", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    abs = Path.GetFullPath(abs);

                    if (!abs.StartsWith(projectRoot))
                    {
                        EditorUtility.DisplayDialog("경고", "프로젝트 폴더 내부를 선택하세요.", "OK");
                    }
                    else
                    {
                        // 절대경로 → 프로젝트 상대경로(Assets/…)
                        jsonFolder = abs.Substring(projectRoot.Length + 1).Replace("\\", "/");
                    }
                }
            }
        }

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Addressables 설정", EditorStyles.boldLabel);
        groupName = EditorGUILayout.TextField(new GUIContent("Group"), groupName);
        labelName = EditorGUILayout.TextField(new GUIContent("Label"), labelName);
        useFileNameAsAddress = EditorGUILayout.Toggle(new GUIContent("Use FileName as Address", "켜면 address를 '파일명(확장자 제외)'로 설정, 끄면 Asset 경로를 그대로 Address로 사용"), useFileNameAsAddress);

        GUILayout.Space(12);
        if (GUILayout.Button("추가 / 갱신 실행", GUILayout.Height(34)))
        {
            try
            {
                Run(jsonFolder, groupName, labelName, useFileNameAsAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorUtility.DisplayDialog("실패", ex.Message, "OK");
            }
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "• 그룹/라벨이 없으면 자동으로 생성\n" +
            "• 이미 Addressables에 있는 항목은 라벨만 보강/주소 갱신\n" +
            "• 실행 후 Addressables Groups 창에서 결과 확인 가능",
            MessageType.None);
    }

    private static void Run(string folder, string groupName, string labelName, bool useFileNameAsAddress)
    {
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            throw new Exception($"폴더가 존재하지 않습니다: {folder}");

        // 프로젝트 내 JSON 파일 수집
        var jsonGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder })
            .Where(guid => AssetDatabase.GUIDToAssetPath(guid).EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (jsonGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("결과", "해당 폴더에서 .json(TextAsset)을 찾지 못했습니다.", "OK");
            return;
        }

        // AddressableAssetSettings 가져오기
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
            throw new Exception("AddressableAssetSettings를 찾을 수 없습니다. Window > Asset Management > Addressables > Groups에서 초기화하세요.");

        // 그룹 확보 (없으면 생성)
        var group = settings.groups.FirstOrDefault(g => g != null && g.Name == groupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                groupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: true,
                schemasToCopy: new List<AddressableAssetGroupSchema>(),
                types: new[] { typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema) }
            );

            // 기본 스키마 설정 (필요시 옵션 조정)
            var bundle = group.GetSchema<BundledAssetGroupSchema>();
            if (bundle != null)
            {
                bundle.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundle.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            }
        }

        // 라벨 확보 (없으면 생성)
        if (!string.IsNullOrEmpty(labelName) && !settings.GetLabels().Contains(labelName))
        {
            settings.AddLabel(labelName);
        }

        int added = 0, updated = 0, labeled = 0;

        foreach (var guid in jsonGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // 이미 Addressable 엔트리가 있는지 확인
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                added++;
            }
            else
            {
                // 기존에 다른 그룹에 있으면 지정 그룹으로 이동
                if (entry.parentGroup != group)
                {
                    settings.MoveEntry(entry, group, readOnly: false, postEvent: false);
                    updated++;
                }
            }

            // Address (표시용 키) 설정
            if (useFileNameAsAddress)
            {
                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (entry.address != fileName)
                {
                    entry.SetAddress(fileName, postEvent: false);
                    updated++;
                }
            }
            else
            {
                // 경로 전체를 address로
                if (entry.address != assetPath)
                {
                    entry.SetAddress(assetPath, postEvent: false);
                    updated++;
                }
            }

            // 라벨 부여
            if (!string.IsNullOrEmpty(labelName) && !entry.labels.Contains(labelName))
            {
                entry.SetLabel(labelName, true, postEvent: false);
                labeled++;
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "완료",
            $"그룹: {groupName}\n라벨: {labelName}\n\n추가: {added}개\n업데이트: {updated}개\n라벨 부여: {labeled}개",
            "OK"
        );
    }
}
#endif
