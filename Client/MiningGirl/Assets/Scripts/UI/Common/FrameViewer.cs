using TMPro;
using UnityEngine;

namespace UI.Common
{
    public class FrameViewer : GameMonoInitializer
    {
        [SerializeField]
        private TextMeshProUGUI frameText;
        [SerializeField] 
        private float speed = 0.1f;
        
        private float _deltaTime;
        private float _frame;
        
        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * speed;
            _frame = 1.0f / _deltaTime;
            
            SetText(_frame);
        }

        private void SetText(float frame)
        {
            frameText.text = $"{frame:F0}";
            frameText.color = GetFrameColor(frame);
        }

        // 30 이하: 빨강 / 30 초과 60 미만: 노랑 / 60 이상: 초록
        private Color GetFrameColor(float frame)
        {
            if (frame <= 30f)
                return Color.red;

            if (frame < 60f)
                return Color.yellow;

            return Color.green;
        }
    }
}