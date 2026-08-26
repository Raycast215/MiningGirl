using System;
using UnityEngine;

namespace Scene.MainGameScene.ViewModel
{
    // 인게임 메뉴의 표시용 상태와 커맨드.
    //
    // 실제로 시간을 멈추거나 판을 끝내는 건 컨트롤러가 합니다. 여기서는 무엇을
    // 눌렀는지만 알립니다. ViewModel이 Time.timeScale을 건드리기 시작하면
    // 3택 정지와 누가 주인인지 알 수 없게 됩니다.
    public class PauseMenuViewModel
    {
        // 메뉴를 열고 닫을 때. 컨트롤러가 시간을 멈춥니다.
        public event Action<bool> PauseRequested;

        // 포기를 누르고 확인까지 마쳤을 때.
        public event Action SurrenderRequested;

        // 배속이 바뀌었을 때. 값은 배수입니다.
        public event Action<float> SpeedChanged;

        public ObservableProperty<bool> IsOpen { get; } = new ObservableProperty<bool>();

        // 포기를 한 번 눌렀을 때 뜨는 되묻기. 한 번에 판이 날아가면 안 됩니다.
        public ObservableProperty<bool> IsConfirmingSurrender { get; } = new ObservableProperty<bool>();

        public ObservableProperty<float> Speed { get; } = new ObservableProperty<float>(1f);

        // 배속 버튼에 그대로 넣을 문자열.
        public ObservableProperty<string> SpeedText { get; } = new ObservableProperty<string>("x1");

        // 결과 화면이 뜬 뒤에는 메뉴가 열리면 안 됩니다.
        public ObservableProperty<bool> IsAvailable { get; } = new ObservableProperty<bool>(true);

        private readonly float[] _speedSteps;

        public PauseMenuViewModel(float[] speedSteps)
        {
            _speedSteps = speedSteps != null && speedSteps.Length > 0
                ? speedSteps
                : new[] { 1f, 2f };

            ApplySpeed(_speedSteps[0]);
        }

        public void Open()
        {
            if (!IsAvailable.Value || IsOpen.Value)
                return;

            IsConfirmingSurrender.Value = false;
            IsOpen.Value = true;

            PauseRequested?.Invoke(true);
        }

        public void Close()
        {
            if (!IsOpen.Value)
                return;

            IsConfirmingSurrender.Value = false;
            IsOpen.Value = false;

            PauseRequested?.Invoke(false);
        }

        // 포기를 처음 누르면 되묻고, 되묻는 중에 다시 누르면 실행합니다.
        public void RequestSurrender()
        {
            if (!IsOpen.Value)
                return;

            if (!IsConfirmingSurrender.Value)
            {
                IsConfirmingSurrender.Value = true;

                return;
            }

            IsConfirmingSurrender.Value = false;
            IsOpen.Value = false;
            IsAvailable.Value = false;

            SurrenderRequested?.Invoke();
        }

        public void CancelSurrender()
        {
            IsConfirmingSurrender.Value = false;
        }

        // 눌릴 때마다 다음 단계로 넘어가고 끝에서 처음으로 돌아옵니다.
        public void ToggleSpeed()
        {
            var index = 0;

            for (var i = 0; i < _speedSteps.Length; i++)
            {
                if (!Mathf.Approximately(_speedSteps[i], Speed.Value))
                    continue;

                index = i;

                break;
            }

            ApplySpeed(_speedSteps[(index + 1) % _speedSteps.Length]);
        }

        // 결과 화면이 뜨면 메뉴를 닫고 잠급니다.
        public void Lock()
        {
            IsConfirmingSurrender.Value = false;
            IsOpen.Value = false;
            IsAvailable.Value = false;
        }

        private void ApplySpeed(float speed)
        {
            Speed.Value = speed;

            // 정수 배속은 소수점을 붙이지 않습니다. x1.5 같은 값이 생기면 그때만 붙습니다.
            SpeedText.Value = Mathf.Approximately(speed, Mathf.Round(speed))
                ? $"x{Mathf.RoundToInt(speed)}"
                : $"x{speed:0.#}";

            SpeedChanged?.Invoke(speed);
        }
    }
}
