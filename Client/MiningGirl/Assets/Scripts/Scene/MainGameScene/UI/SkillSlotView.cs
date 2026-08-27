using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 하단 스킬 슬롯 한 칸.
    //
    // 부모가 값을 밀어넣는 순수 표시 컴포넌트라 ViewModel을 따로 두지 않았습니다.
    public class SkillSlotView : MonoBehaviour
    {
        [SerializeField]
        private Image icon;

        [SerializeField]
        [Tooltip("Fill Method를 Radial 360으로 둔 이미지. 남은 쿨다운만큼 덮습니다.")]
        private Image cooldownCover;

        [SerializeField]
        [Tooltip("레벨 숫자와 그 뒤 판. 빈 칸에서는 통째로 꺼집니다.")]
        private GameObject levelRoot;

        [SerializeField]
        private TMP_Text levelText;

        [SerializeField]
        [Tooltip("아직 얻지 않은 칸에 보여 줄 빈 표시")]
        private GameObject emptyMark;

        // 같은 아이콘을 매번 다시 넣지 않도록 들고 있습니다.
        private string _appliedIconId;

        private bool _hasSkill;

        public void SetEmpty()
        {
            _hasSkill = false;
            _appliedIconId = null;

            if (icon != null)
                icon.enabled = false;

            if (cooldownCover != null)
                cooldownCover.enabled = false;

            if (levelRoot != null)
                levelRoot.SetActive(false);

            if (emptyMark != null)
                emptyMark.SetActive(true);
        }

        public void SetSkill(string iconAssetId, int level)
        {
            _hasSkill = true;

            if (emptyMark != null)
                emptyMark.SetActive(false);

            if (icon != null && _appliedIconId != iconAssetId)
            {
                _appliedIconId = iconAssetId;
                AddressableManager.Instance.ApplySprite(iconAssetId, icon);
            }

            if (levelRoot != null)
                levelRoot.SetActive(true);

            // 접두어 없이 숫자만 적습니다.
            //
            // 표시 레벨이 "획득 1 + 강화 합"이 되면서 상한이 14라 두 자리가 나옵니다.
            // "Lv."까지 넣으면 판이 슬롯 폭의 62%를 먹어 아이콘 하단을 덮습니다.
            // 숫자만이면 39%라 모서리에만 앉고, 같은 자리에서 글자가 커집니다.
            if (levelText != null)
                levelText.text = level.ToString();
        }

        // 매 프레임 들어옵니다. 스킬이 없는 칸에는 아무것도 하지 않습니다.
        public void SetCooldown(float ratio)
        {
            if (!_hasSkill || cooldownCover == null)
                return;

            cooldownCover.enabled = ratio > 0f;
            cooldownCover.fillAmount = ratio;
        }
    }
}
