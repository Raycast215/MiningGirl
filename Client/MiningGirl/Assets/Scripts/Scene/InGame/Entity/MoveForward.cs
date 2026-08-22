using System;
using UnityEngine;

namespace InGame.System
{
    public class MoveForward
    {
        public event Action<Vector2> OnMoveCompleted;
        private readonly Rigidbody _rigidbody;
        private Vector2 _moveVec;

        public MoveForward(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        // 이 메서드는 Update에서 호출됩니다.
        // 예전에는 Time.fixedDeltaTime(프레임레이트와 무관한 상수 0.02)을 곱하고 있어서
        // 초당 이동거리가 '속도 x 0.02 x fps'가 됐습니다. 실측으로 29fps에서 0.467,
        // 116fps에서 1.072 units/s — 같은 설정값인데 기기에 따라 2.3배 차이가 났습니다.
        // Time.deltaTime을 쓰면 프레임레이트와 무관하게 설정값 그대로 움직입니다.
        public void Move(float moveSpeed)
        {
            Vector3 moveDir = _moveVec.normalized * (moveSpeed * Time.deltaTime);
            var movePos = _rigidbody.position + moveDir;
            _rigidbody.MovePosition(movePos);
            OnMoveCompleted?.Invoke(_moveVec);
            
            // // 움직일 거리 계산.
            // var moveDir = (Vector3)_moveVec * (moveSpeed * Time.deltaTime);
            // // 실제 이동할 위치값.
            // var movePos = _rigidbody2D.transform.position + moveDir;
            //
            // // 이동 실행.
            // _rigidbody2D.MovePosition(movePos);
            // // 이동 후 실행할 이벤트 실행.
            // OnMoveCompleted?.Invoke(_moveVec);
        }

        public void SetMoveVec(Vector2 moveVec)
        {
            _moveVec = moveVec;
        }
        
        public void CompleteMove(Action<Vector2> callback)
        {
            OnMoveCompleted += callback;
        }
    }
}
