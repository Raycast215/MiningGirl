using System;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 화면 하단의 타워. 몬스터의 유일한 공격 대상이고, 체력이 0이 되면 스테이지 실패입니다.
    // 캐릭터는 피격 대상이 아니라 여기에만 체력이 있습니다.
    public class Tower : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged;
        public event Action OnDestroyed;

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        // 몬스터가 이 높이를 기준으로 사거리를 잽니다.
        // 각목 끝이 아니라 판자 몸통 윗선입니다. 각목은 높이가 들쭉날쭉해서
        // 기준으로 삼으면 몬스터가 서는 높이가 아트에 따라 흔들립니다.
        public float TopY => transform.position.y + topOffset;

        // 배치를 UI 위치에서 역산할 때 씁니다. 아트가 바뀌어도 따라갑니다.
        public float HalfHeight => bodyRenderer != null ? bodyRenderer.bounds.extents.y : 1f;

        [SerializeField]
        [Tooltip("타워 원점에서 윗면까지의 높이. 몬스터가 멈추는 기준선입니다.")]
        private float topOffset = 0.7f;

        [Header("손상 단계")]
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private Sprite intactSprite;

        [SerializeField]
        private Sprite damagedSprite;

        [SerializeField]
        private Sprite brokenSprite;

        [SerializeField]
        [Tooltip("체력이 이 비율 아래로 내려가면 금이 간 그림으로 바뀝니다.")]
        [Range(0f, 1f)]
        private float damagedRatio = 0.66f;

        [SerializeField]
        [Tooltip("체력이 이 비율 아래로 내려가면 부서진 그림으로 바뀝니다.")]
        [Range(0f, 1f)]
        private float brokenRatio = 0.33f;

        // 같은 그림을 다시 넣는 낭비를 막습니다.
        private Sprite _appliedSprite;

        public void Setup(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            CurrentHealth = MaxHealth;

            RefreshVisual();

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            RefreshVisual();

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
                OnDestroyed?.Invoke();
        }

        // 체력 바만으로는 얼마나 밀렸는지 잘 안 읽힙니다. 그림도 같이 무너집니다.
        private void RefreshVisual()
        {
            if (bodyRenderer == null)
                return;

            var ratio = MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

            var sprite = ratio > damagedRatio
                ? intactSprite
                : ratio > brokenRatio
                    ? damagedSprite
                    : brokenSprite;

            if (sprite == null || sprite == _appliedSprite)
                return;

            _appliedSprite = sprite;
            bodyRenderer.sprite = sprite;
        }
    }
}
