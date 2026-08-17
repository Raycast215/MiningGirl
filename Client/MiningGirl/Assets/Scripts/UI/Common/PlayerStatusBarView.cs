using Scene.InGame.Entity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 캐릭터 머리 위에 표시되는 체력바.
    // 피격 무적 동안에는 위쪽에 무적 시간 게이지가 함께 표시됩니다.
    // (부활 대기 게이지는 사망=스테이지 실패로 바뀌면서 없앴습니다.)
    public class PlayerStatusBarView : MonoBehaviour, IPlayerStatusPresenter
    {
        [Header("References")]
        [SerializeField]
        private Image healthTrack;
        [SerializeField]
        private Image healthFill;
        [SerializeField]
        private Image invincibleTrack;
        [SerializeField]
        private Image invincibleFill;

        [Header("Colors")]
        [SerializeField]
        private Color trackColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        [SerializeField]
        private Color healthColor = new Color(0.35f, 0.82f, 0.36f, 1f);
        [SerializeField]
        [Tooltip("체력이 이 비율 아래면 체력바가 붉게 바뀝니다.")]
        private Color dangerColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        [SerializeField]
        [Range(0f, 1f)]
        private float dangerRatio = 0.3f;
        [SerializeField]
        private Color invincibleColor = new Color(0.45f, 0.78f, 1f, 1f);

        [Header("Options")]
        [SerializeField]
        [Tooltip("무적이 아닐 때 무적 게이지를 숨깁니다.")]
        private bool hideInvincibleWhenIdle = true;

        // IPlayerStatusPresenter 구현 — 플레이어 상태가 바뀔 때마다 호출합니다.
        public void SetStatus(float healthRatio, float gaugeRatio, bool isInvincible)
        {
            if (healthTrack != null)
                healthTrack.color = trackColor;

            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01(healthRatio);

                // 체력이 위험 구간이면 붉게 — 재시작이 임박했다는 신호입니다.
                healthFill.color = healthRatio <= dangerRatio ? dangerColor : healthColor;
            }

            var showGauge = isInvincible || !hideInvincibleWhenIdle;

            if (invincibleTrack != null)
            {
                invincibleTrack.color = trackColor;
                invincibleTrack.gameObject.SetActive(showGauge);
            }

            if (invincibleFill != null)
            {
                invincibleFill.gameObject.SetActive(showGauge);
                invincibleFill.fillAmount = Mathf.Clamp01(gaugeRatio);
                invincibleFill.color = invincibleColor;
            }
        }
    }
}
