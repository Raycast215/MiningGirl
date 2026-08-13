using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace InGame.temp.System.FloatingDamage
{
    public class Damage : GameMonoInitializer
    {
        private event Action<Damage> OnReleased;
        
        [SerializeField]
        private TMP_Text damageText;

        private Tween _damageTween;
        private Tween _scaleTween;
        private bool _isPaused;
        
        public void Init(int damage, Vector3 position, Action<Damage> onReleased, bool isCritical = false)
        {
            OnReleased = null;
            OnReleased += onReleased;
            
            gameObject.SetActive(true);
            
            var startPos = position + new Vector3(0f, 0.2f, 10.0f);
            var endPos = position + new Vector3(0f, 1.0f, 10.0f);

            damageText.text = $"{damage}";
            transform.position = startPos;

            _damageTween?.Kill();
            _damageTween = transform.DOMove(endPos, 1.0f).OnComplete(Clear);
            
            if (isCritical)
            {
                transform.localScale = Vector3.one * 3.0f;
                _scaleTween?.Kill();
                _scaleTween = transform.DOScale(Vector3.one, 0.5f).SetDelay(0.1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            // 정지 중에 생성되면 곧바로 멈춘 상태로 시작합니다.
            ApplyPause();
        }
        
        // 게임이 멈춘 동안 떠오르는 연출도 함께 멈춥니다.
        // (DOTween은 프로젝트의 정지 플래그와 무관하게 돌기 때문에 직접 제어해야 합니다.)
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            ApplyPause();
        }

        private void ApplyPause()
        {
            if (_isPaused)
            {
                _damageTween?.Pause();
                _scaleTween?.Pause();
            }
            else
            {
                _damageTween?.Play();
                _scaleTween?.Play();
            }
        }

        private void Clear()
        {
            gameObject.SetActive(false);
            OnReleased?.Invoke(this);
        }

        // 외부(DamageController.Clear 등)에서 아직 떠 있는 데미지를 즉시 정리할 때 호출합니다.
        // 진행 중인 이동 트윈을 죽이고 곧바로 풀로 반환합니다.
        public void ForceRelease()
        {
            if (!gameObject.activeSelf)
                return;

            _damageTween?.Kill();
            _damageTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
            Clear();
        }
    }
}
