using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Legacy.MainGame.UI
{
    // 버프 하나를 표시하는 항목. 아이콘 + 남은 시간(예: 9s)
    public class BuffIconView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TMP_Text timeText;
        [SerializeField]
        private Image background;

        // icon이 null이면 색만으로 구분합니다(아이콘 에셋이 아직 없을 때).
        public void SetData(Sprite icon, Color color, float remainTime)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = color;
            }

            if (background != null)
                background.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.9f);

            if (timeText == null)
                return;

            // 1초 미만은 0s로 보이지 않도록 올림 처리합니다.
            var seconds = Mathf.Max(1, Mathf.CeilToInt(remainTime));

            timeText.text = $"{seconds}s";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
