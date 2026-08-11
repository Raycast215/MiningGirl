using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DebugUtil
{
    public class LogCatcher : MonoBehaviour
    {
        private const string Email = "hyeok0215@naver.com";
        private const string Header = "[버그 리포트]";
        private const int MaxLines = 500;
        private const string LogFileName = "unity_log.txt";

        private string _logFilePath;
        private bool _isInitialized;

        private void Awake()
        {
            _logFilePath = Path.Combine(Application.persistentDataPath, LogFileName);

            try
            {
                if (!File.Exists(_logFilePath))
                    File.WriteAllText(_logFilePath, string.Empty);
            }
            catch
            {
                return;
            }

            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;

            _isInitialized = true;
        }

        public void SendMail(Action onUnavailable = null)
        {
            if (!_isInitialized || !File.Exists(_logFilePath))
            {
                onUnavailable?.Invoke();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(_logFilePath);
                var logContent = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - MaxLines)));

                if (string.IsNullOrWhiteSpace(logContent))
                {
                    onUnavailable?.Invoke();
                    return;
                }

                var subject = EscapeURL($"{Header} {Application.version}");
                var body = EscapeURL(logContent);

                Application.OpenURL($"mailto:{Email}?subject={subject}&body={body}");
            }
            catch
            {
                onUnavailable?.Invoke();
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
                return;

            var builder = new StringBuilder();

            builder.AppendLine("=========================");
            builder.AppendLine();
            builder.AppendLine($"[Date] {DateTime.Now}");
            builder.AppendLine($"[Version] {Application.version}");
            builder.AppendLine($"[Platform] {Application.platform}");
            builder.AppendLine($"[DeviceName] {SystemInfo.deviceName}");
            builder.AppendLine($"[DeviceModel] {SystemInfo.deviceModel}");
            builder.AppendLine($"[OperatingSystem] {SystemInfo.operatingSystem}");
            builder.AppendLine($"[Language] {Application.systemLanguage}");
            builder.AppendLine($"[Screen] Width: {Screen.width} / Height: {Screen.height}");
            builder.AppendLine($"[LogType] {type}");
            builder.AppendLine($"[Log] {logString}");
            builder.AppendLine($"[StackTrace] {stackTrace}");
            builder.AppendLine();
            
            try
            {
                File.AppendAllText(_logFilePath, builder.ToString());
            }
            catch
            {
                // 로그 저장 실패 시 Debug.LogError 사용 금지
            }
        }

        private string EscapeURL(string value)
        {
            return UnityWebRequest
                .EscapeURL(value, Encoding.UTF8)
                .Replace("+", "%20");
        }
        
        private void ClearLog()
        {
            if (!_isInitialized || string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                File.WriteAllText(_logFilePath, string.Empty);
            }
            catch
            {
                // 로그 초기화 실패 시 무시
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }
    }
}