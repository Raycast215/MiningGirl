using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 캐릭터 선택 항목 하나를 표시하는 뷰.
    // 팝업이 이 프리팹을 데이터 수만큼 생성해서 사용합니다.
    public class CharacterSelectSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI statText;

        [Header("Colors")]
        [SerializeField]
        private Color valueColor = new Color(0.30f, 0.85f, 0.39f, 1f);

        private Action _onClick;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public void SetData(CharacterStatDataRow row, string displayName, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = displayName;

            if (statText != null)
                statText.text = BuildStatText(row);
        }

        // 주요 스탯을 한눈에 비교할 수 있게 정리합니다. 수치는 색으로 강조합니다.
        private string BuildStatText(CharacterStatDataRow row)
        {
            var hex = ColorUtility.ToHtmlStringRGB(valueColor);

            string V(float value, string suffix = "")
            {
                var text = Mathf.Approximately(value, Mathf.Round(value))
                    ? Mathf.RoundToInt(value).ToString()
                    : value.ToString("0.##");

                return $"<color=#{hex}>{text}{suffix}</color>";
            }

            return $"공격력 {V(row.Damage)}   주기 {V(row.AttackDelay, "초")}   이동 {V(row.MoveSpeed)}\n" +
                   $"치명타 {V(row.CriRate, "%")} (+{V(Mathf.RoundToInt(row.CriDamage * 100f), "%")})   추가타 {V(row.ExtraHitRate, "%")}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
