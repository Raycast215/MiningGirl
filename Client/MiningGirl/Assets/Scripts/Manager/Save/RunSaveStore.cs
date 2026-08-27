using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Manager.Save
{
    // 진행 저장을 파일에 넣고 빼는 자리.
    //
    // 계정 저장(골드, 메타 강화)과 파일을 나눕니다. 진행 저장은 판이 끝날 때마다
    // 통째로 지우는 것이라, 한 파일에 섞어 두면 지우는 쪽이 계정 데이터를 건드릴
    // 위험을 계속 안고 갑니다.
    public static class RunSaveStore
    {
        // 저장 구조가 바뀔 때만 올립니다. 시트 값이 바뀌었다고 올리지 마십시오.
        public const int SchemaVersion = 2;

        private const string FileName = "run_save.json";

        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists()
        {
            try
            {
                return File.Exists(Path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 저장 파일을 확인하지 못했습니다: {e.Message}");

                return false;
            }
        }

        // 실패해도 게임을 멈추지 않습니다. 저장이 안 되는 것보다
        // 판이 끊기는 쪽이 나쁩니다.
        public static bool Write(RunSaveData data)
        {
            if (data == null)
                return false;

            try
            {
                data.SchemaVersion = SchemaVersion;
                data.SavedAt = DateTime.UtcNow.ToString("o");

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // 쓰다 앱이 죽으면 반쪽짜리 파일이 남습니다. 임시로 쓴 뒤 갈아 끼웁니다.
                var temp = Path + ".tmp";

                File.WriteAllText(temp, json);

                if (File.Exists(Path))
                    File.Delete(Path);

                File.Move(temp, Path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 진행 저장 실패: {e.Message}");

                return false;
            }
        }

        // 파일이 없거나 읽지 못하면 null입니다. 내용이 쓸 만한지는 여기서 안 봅니다.
        public static RunSaveData Read()
        {
            try
            {
                if (!File.Exists(Path))
                    return null;

                var json = File.ReadAllText(Path);

                return JsonConvert.DeserializeObject<RunSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 진행 저장을 읽지 못했습니다: {e.Message}");

                return null;
            }
        }

        // 판이 끝나면 지웁니다. 클리어든 타워 파괴든 포기든 같습니다.
        //
        // 포기에만 붙이면 클리어한 판의 저장이 남아, 다음 실행에서 이미 끝난 판이
        // 되살아납니다. 플레이어가 클리어한 스테이지에 다시 갇힙니다.
        public static void Clear()
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 진행 저장을 지우지 못했습니다: {e.Message}");
            }
        }
    }
}
