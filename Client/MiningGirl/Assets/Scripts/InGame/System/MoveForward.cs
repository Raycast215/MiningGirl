using System;
using UnityEngine;

namespace InGame.System
{
    public class MoveForward
    {
        public event Action<Vector2> OnMoveCompleted;
        private readonly Rigidbody2D _rigidbody2D;
        private Vector2 _moveVec;

        public MoveForward(Rigidbody2D rigidbody2D)
        {
            _rigidbody2D = rigidbody2D;
        }

        public void Move(float moveSpeed)
        {
            // 움직일 거리 계산.
            var moveDir = (Vector3)_moveVec * (moveSpeed * Time.deltaTime);
            // 실제 이동할 위치값.
            var movePos = _rigidbody2D.transform.position + moveDir;
            
            // 이동 실행.
            _rigidbody2D.MovePosition(movePos);
            // 이동 후 실행할 이벤트 실행.
            OnMoveCompleted?.Invoke(_moveVec);
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