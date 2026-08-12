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
        
        // 겹침 보정용 (같은 종류끼리 — 예: 몬스터끼리)
        private float _separationDistance = 0.5f;
        private float _separationStrength = 0.35f;
        private float _maxSeparationOffset = 0.4f;

        // 장애물 회피용 (예: 광물) — 같은 종류끼리보다 더 넓고 강하게 피하도록 별도 값을 씁니다.
        private System.Func<System.Collections.Generic.IReadOnlyList<IEntity>> _obstacleProvider;
        private float _obstacleDistance = 2.5f;
        private float _obstacleStrength = 1.5f;
        private float _maxObstacleOffset = 2.0f;

        // 프레임 간 이동 방향이 급변해서 떨리는 것을 막기 위한 스무딩용 이전 방향.
        private Vector3 _lastMoveDir = Vector3.zero;
        // 값이 클수록 방향 전환이 빠릅니다(작을수록 부드럽지만 반응이 늦음).
        private float _dirSmoothSpeed = 6f;

        public MoveNode SetDirectionSmoothSpeed(float speed)
        {
            _dirSmoothSpeed = speed;
            return this;
        }

        public MoveNode(Rigidbody rigidbody, IEntity iEntity)
        {
            _moveComponent = new MoveForward(rigidbody);
            _entity = iEntity;

            rigidbody.freezeRotation = true;
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

        // 장애물(광물 등) 목록 공급자를 지정합니다. 지정하지 않으면 장애물 회피는 동작하지 않습니다.
        public MoveNode SetObstacleProvider(System.Func<System.Collections.Generic.IReadOnlyList<IEntity>> provider)
        {
            _obstacleProvider = provider;
            return this;
        }

        // 장애물 회피 파라미터 — 같은 종류끼리의 겹침 보정과 독립적으로 조절합니다.
        public MoveNode SetObstacleAvoidance(float distance, float strength, float maxOffset)
        {
            _obstacleDistance = distance;
            _obstacleStrength = strength;
            _maxObstacleOffset = maxOffset;
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

            if (dist <= _entity.GetAttackDistance())
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

            // 장애물(광물 등) 회피 — 같은 종류끼리의 겹침 보정보다 넓은 반경/강한 힘으로 밀어냅니다.
            Vector3 obstacleOffset = Vector3.zero;
            var obstacles = _obstacleProvider?.Invoke();

            if (obstacles != null)
            {
                for (var i = 0; i < obstacles.Count; i++)
                {
                    var obstacle = obstacles[i];

                    if (obstacle == null || obstacle == _entity || !obstacle.GetActiveState())
                        continue;

                    var obstaclePos = obstacle.GetPosition();
                    obstaclePos.z = 0f;

                    var diff = myPos - obstaclePos;
                    var d = diff.magnitude;

                    if (d < 0.001f || d >= _obstacleDistance)
                        continue;

                    var ratio = 1f - (d / _obstacleDistance);
                    var away = diff / d; // 장애물 -> 나 방향(정규화)

                    // 정면으로 밀어내기만 하면 '밀렸다가 다시 다가가는' 진동이 생깁니다.
                    // 대신 장애물을 옆으로 돌아가는 접선 방향을 섞어 미끄러지듯 우회하게 합니다.
                    var tangent = new Vector3(-away.y, away.x, 0f);

                    // 진행 방향과 각도가 맞는 쪽 접선을 고릅니다(왼쪽/오른쪽 중 덜 돌아가는 쪽).
                    if (Vector3.Dot(tangent, moveDir) < 0f)
                        tangent = -tangent;

                    // 정면 밀어내기는 약하게, 접선(우회)을 주로 사용해 떨림 없이 비껴가게 합니다.
                    var avoidDir = (away * 0.35f + tangent).normalized;

                    obstacleOffset += avoidDir * (ratio * _obstacleStrength);
                }
            }

            if (obstacleOffset.magnitude > _maxObstacleOffset)
                obstacleOffset = obstacleOffset.normalized * _maxObstacleOffset;

            // 이동 방향을 크게 틀지 않도록 아주 소량만 더함
            var finalDir = moveDir + separationOffset + obstacleOffset;

            if (finalDir.sqrMagnitude < 0.0001f)
                finalDir = moveDir;
            else
                finalDir.Normalize();

            // 프레임 간 방향이 급변해서 떨리는 것을 막기 위해 이전 방향에서 부드럽게 보간합니다.
            //
            // 단, 회전 속도가 이동 속도를 못 따라가면 타겟 주위를 원을 그리며 맴돌게 됩니다.
            // (최소 선회 반경 = 이동속도 / 회전속도 이므로, 이 값이 사거리보다 크면 영영 도달하지 못합니다.)
            // 그래서 이동 속도에 비례해 회전 속도를 끌어올려 반경이 항상 사거리 안쪽이 되게 합니다.
            var moveSpeed = Mathf.Max(0.01f, _entity.GetMoveSpeed());
            var stopDistance = Mathf.Max(0.01f, _entity.GetAttackDistance());
            var requiredTurnSpeed = moveSpeed / stopDistance * 2f;
            var turnSpeed = Mathf.Max(_dirSmoothSpeed, requiredTurnSpeed);

            if (dist <= stopDistance * 2f)
            {
                // 거의 다 왔으면 스무딩 없이 곧장 파고듭니다(맴돌기 방지).
                _lastMoveDir = finalDir;
            }
            else if (_lastMoveDir.sqrMagnitude < 0.0001f)
            {
                _lastMoveDir = finalDir;
            }
            else
            {
                _lastMoveDir = Vector3.Slerp(_lastMoveDir, finalDir, Time.deltaTime * turnSpeed).normalized;
            }

            finalDir = _lastMoveDir;

            _moveComponent.SetMoveVec(finalDir);
            _moveComponent.Move(_entity.GetMoveSpeed());
            _entity.SetDirection(moveDir); // 바라보는 방향은 타겟 기준 유지

            return NodeState.Running;
        }
    }
}