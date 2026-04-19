using BehaviourTree;
using InGame.System;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Node
{
    public class MoveNode
    {
        private readonly MoveForward _moveComponent;
        private readonly IEntity _entity;

        private float _moveSpeed;
        private float _minDistance;

        // 겹침 보정용
        private float _separationDistance = 0.5f;
        private float _separationStrength = 0.35f;
        private float _maxSeparationOffset = 0.4f;

        public MoveNode(Rigidbody rigidbody, IEntity iEntity)
        {
            _moveComponent = new MoveForward(rigidbody);
            _entity = iEntity;

            rigidbody.freezeRotation = true;
        }

        public MoveNode SetMoveSpeed(float speed)
        {
            _moveSpeed = speed;
            return this;
        }

        public MoveNode SetMinDistance(float minDistance)
        {
            _minDistance = minDistance;
            return this;
        }

        public MoveNode SetSeparationDistance(float distance)
        {
            _separationDistance = distance;
            return this;
        }

        public MoveNode SetSeparationStrength(float strength)
        {
            _separationStrength = strength;
            return this;
        }

        public MoveNode SetMaxSeparationOffset(float maxOffset)
        {
            _maxSeparationOffset = maxOffset;
            return this;
        }

        public NodeState ProcessNode()
        {
            if (_entity == null || !_entity.GetActiveState())
                return NodeState.Failure;

            var target = _entity.GetTarget();
            if (target == null || !target.GetActiveState())
            {
                _moveComponent.Move(0f);
                return NodeState.Failure;
            }

            var myPos = _entity.GetPosition();
            var targetPos = target.GetPosition();

            myPos.z = 0f;
            targetPos.z = 0f;

            var toTarget = targetPos - myPos;
            var dist = toTarget.magnitude;

            if (dist <= _minDistance)
            {
                _moveComponent.Move(0f);
                return NodeState.Success;
            }

            // 기본 이동은 무조건 타겟 방향
            var moveDir = toTarget.normalized;

            // 겹침 보정은 "아주 가까운 경우만" 적용
            Vector3 separationOffset = Vector3.zero;
            var checkEntities = _entity.GetNearCheckEntities();

            if (checkEntities != null)
            {
                foreach (var other in checkEntities)
                {
                    if (other == null || other == _entity || !other.GetActiveState())
                        continue;

                    var otherPos = other.GetPosition();
                    otherPos.z = 0f;

                    var diff = myPos - otherPos;
                    var d = diff.magnitude;

                    // 너무 가깝지도 멀지도 않은 것만 처리
                    if (d < 0.001f || d >= _separationDistance)
                        continue;

                    // 가까울수록 조금 더 밀기
                    float ratio = 1f - (d / _separationDistance);
                    separationOffset += diff.normalized * (ratio * _separationStrength);
                }
            }

            // 보정량 제한
            if (separationOffset.magnitude > _maxSeparationOffset)
                separationOffset = separationOffset.normalized * _maxSeparationOffset;

            // 이동 방향을 크게 틀지 않도록 아주 소량만 더함
            var finalDir = moveDir + separationOffset;

            if (finalDir.sqrMagnitude < 0.0001f)
                finalDir = moveDir;
            else
                finalDir.Normalize();

            _moveComponent.SetMoveVec(finalDir);
            _moveComponent.Move(_moveSpeed);
            _entity.SetDirection(moveDir); // 바라보는 방향은 타겟 기준 유지

            return NodeState.Running;
        }
    }
}

// using System.Linq;
// using BehaviourTree;
// using InGame.System;
// using Scene.InGame.Entity.Interface;
// using UnityEngine;
//
// namespace Scene.InGame.Entity.Node
// {
//     public class MoveNode
//     {
//         private readonly MoveForward _moveComponent;
//         private readonly IEntity _entity;
//         private float _moveSpeed;
//         private float _minDistance;
//         
//         public MoveNode(Rigidbody2D rigidbody2D, IEntity iEntity)
//         {
//             _moveComponent = new MoveForward(rigidbody2D);
//             _entity = iEntity;
//         }
//
//         public MoveNode SetMoveSpeed(float speed)
//         {
//             _moveSpeed = speed;
//             return this;
//         }
//         
//         public MoveNode SetMinDistance(float minDistance)
//         {
//             _minDistance = minDistance;
//             return this;
//         }
//         
//         public NodeState ProcessNode()
//         {
//             if (_entity == null || !_entity.GetActiveState())
//                 return NodeState.Failure;
//         
//             var myPos = _entity.GetPosition();
//             var targetPos = _entity.GetTarget().GetPosition();
//             var dist = Vector3.Distance(myPos, targetPos);
//
//             var checkEntities = _entity.GetNearCheckEntities();
//             
//             if (checkEntities != null && checkEntities.Any())
//             {
//                 // 다른 대상이랑 위치 겹치지 않도록 하기 위함
//                 foreach (var entity in checkEntities)
//                 {
//                     var pos = entity.GetPosition();
//                     
//                     // 로직
//                 }
//             }
//             
//             if (dist <= _minDistance)
//             {
//                 _moveComponent.Move(0.0f);
//                 return NodeState.Success;
//             }
//             
//             var dirVec = (targetPos - myPos).normalized;
//
//             _moveComponent.Move(_moveSpeed);
//             _moveComponent.SetMoveVec(dirVec);
//             _entity.SetDirection(dirVec);
//
//             return NodeState.Running;
//         }
//     }
// }