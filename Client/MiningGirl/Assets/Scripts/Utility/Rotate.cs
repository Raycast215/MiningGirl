using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utility
{
    // Scripted by Raycast
    // 2024.09.03
    // Object의 회전을 Loop하는 클래스. 
    
    public class Rotate : MonoBehaviour
    {
        private const float MaxAngle = 360.0f;
        
        [SerializeField] private bool isLeft = true;
        [SerializeField] private float speed = 0.25f;
        [SerializeField] private bool isX;
        [SerializeField] private bool isY;
        [SerializeField] private bool isZ = true;

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private float _acceleration;
        
        private void Start()
        {
            OnRotate().Forget();
        }

        /// 가속도를 변경합니다.
        public void SetAcceleration(float acceleration)
        {
            _acceleration = acceleration;
        }
        
        private async UniTaskVoid OnRotate()
        {
            var angle = 0.0f;
            
            while (true)
            {
                await UniTask.Yield(cancellationToken: _cts.Token);

                if (_cts is null || _cts.IsCancellationRequested)
                    return;
                
                // 좌, 우 방향 설정.
                var isTurn = isLeft ? -1.0f : 1.0f;
                
                // 각도 계산 및 적용.
                angle += (speed + _acceleration) * isTurn;
                
                // 최대 각도에 도달했을 경우 초기화.
                if (Mathf.Abs(angle) >= MaxAngle)
                    angle = 0.0f;

                var angleX = isX ? angle : 0.0f;
                var angleY = isY ? angle : 0.0f;
                var angleZ = isZ ? angle : 0.0f;

                SetRotate(new Vector3(angleX, angleY, angleZ));
            }
        }

        private void SetRotate(Vector3 euler)
        {
            transform.rotation = Quaternion.Euler(euler);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}