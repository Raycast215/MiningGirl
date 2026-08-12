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

            // 치명타 데미지는 '추가 배율'이라 +표기로 따로 보여줍니다.
            // (확률 옆에 괄호로 붙이면 확률이 150%인 것처럼 읽혀서 분리했습니다.)
            return $"채굴 데미지 {V(row.Damage)}   채굴 주기 {V(row.AttackDelay, "초")}   이동속도 {V(row.MoveSpeed)}\n" +
                   $"치명타 확률 {V(row.CriRate, "%")}   치명타 데미지 +{V(Mathf.RoundToInt(row.CriDamage * 100f), "%")}   추가타 확률 {V(row.ExtraHitRate, "%")}\n" +
                   $"체력 {V(row.MaxHealth)}   피격 무적 시간 {V(row.InvincibleDuration, "초")}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
