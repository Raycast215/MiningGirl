using System.Collections.Generic;
using BehaviourTree;
using InGame.System;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Spatial;
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
        private System.Func<IReadOnlyList<IEntity>> _obstacleProvider;
        private float _obstacleDistance = 2.5f;
        private float _obstacleStrength = 1.5f;
        private float _maxObstacleOffset = 2.0f;

        // 다른 개체와 이만큼 안쪽으로는 이동하지 않습니다(겹침 방지).
        private float _blockRadius = 1.3f;

        // 겹침을 풀려고 옆으로 비켜설 때의 속도(이동 속도 대비). 0이면 비켜서지 않습니다.
        private float _stepAsideSpeedScale = 0.5f;

        // 옆으로 비켜설 때 개체마다 좌/우가 갈리도록 하는 고정 부호.
        private readonly float _spacingSign;

        // 프레임 간 이동 방향이 급변해서 떨리는 것을 막기 위한 스무딩용 이전 방향.
        private Vector3 _lastMoveDir = Vector3.zero;
        // 값이 클수록 방향 전환이 빠릅니다(작을수록 부드럽지만 반응이 늦음).
        private float _dirSmoothSpeed = 6f;

        // ── 근접 조회 ──────────────────────────────────────────────
        //
        // 전수 비교는 계산량보다 IEntity.GetPosition() 같은 인터페이스/네이티브 호출이 비쌉니다.
        // (156마리 기준 인터페이스 경유 29ms vs 위치 배열 캐시 1.4ms)
        // 그래서 컨트롤러가 프레임당 한 번 만들어 둔 격자를 조회하고,
        // ProcessNode 한 번에 조회도 한 번만 합니다.
        private SpatialHashGrid _neighborGrid;
        private SpatialHashGrid _obstacleGrid;

        private readonly List<int> _neighborHits = new List<int>();
        private readonly List<int> _obstacleHits = new List<int>();

        // 격자가 없으면(플레이어 등) 예전처럼 목록을 그대로 훑습니다.
        private IReadOnlyList<IEntity> _neighborList;

        public MoveNode(Rigidbody rigidbody, IEntity iEntity)
        {
            _moveComponent = new MoveForward(rigidbody);
            _entity = iEntity;
            _spacingSign = iEntity != null && (iEntity.GetHashCode() & 1) == 0 ? 1f : -1f;

            rigidbody.freezeRotation = true;
        }

        public MoveNode SetDirectionSmoothSpeed(float speed)
        {
            _dirSmoothSpeed = speed;
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

        public MoveNode SetBlockRadius(float radius)
        {
            _blockRadius = radius;
            return this;
        }

        public MoveNode SetStepAsideSpeedScale(float scale)
        {
            _stepAsideSpeedScale = scale;
            return this;
        }

        public MoveNode SetMaxSeparationOffset(float maxOffset)
        {
            _maxSeparationOffset = maxOffset;
            return this;
        }

        // 같은 종류끼리의 근접 조회에 쓸 격자. null이면 목록 전수 비교로 넘어갑니다.
        public MoveNode SetNeighborGrid(SpatialHashGrid grid)
        {
            _neighborGrid = grid;
            return this;
        }

        // 장애물(광물) 조회에 쓸 격자. null이면 SetObstacleProvider 목록을 씁니다.
        public MoveNode SetObstacleGrid(SpatialHashGrid grid)
        {
            _obstacleGrid = grid;
            return this;
        }

        // 장애물 목록 공급자(격자가 없을 때의 대체 경로).
        public MoveNode SetObstacleProvider(System.Func<IReadOnlyList<IEntity>> provider)
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

            // 이번 프레임에 쓸 이웃을 한 번만 뽑아 둡니다(아래 세 판정이 같이 씁니다).
            CollectNeighbors(myPos);

            var toTarget = targetPos - myPos;
            var dist = toTarget.magnitude;

            // 기본 이동은 무조건 타겟 방향
            var moveDir = toTarget.normalized;

            // 겹침 보정은 이동 중에만 하면 사거리에 들어와 멈춘 순간부터 서로 포개집니다.
            // 그래서 멈춘 뒤에도 쓸 수 있도록 먼저 계산해 둡니다.
            var separationOffset = ComputeSeparation(myPos);

            if (dist <= _entity.GetAttackDistance())
            {
                _entity.SetDirection(moveDir);
                StepAsideWhileStopped(myPos, moveDir);

                return NodeState.Success;
            }

            var obstacleOffset = ComputeObstacleAvoidance(myPos, moveDir);

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

            // 다른 개체와 겹칠 자리로는 아예 들어가지 않습니다.
            // (밀어내는 방식은 서로를 계속 튕겨 떨림이 생기고, 캐릭터까지 밀고 들어갑니다.)
            StepOrSlide(myPos, finalDir);
            _entity.SetDirection(moveDir); // 바라보는 방향은 타겟 기준 유지

            return NodeState.Running;
        }

        // ── 근접 조회 ──────────────────────────────────────────────

        // 이번 프레임에 볼 이웃을 확정합니다. 격자가 있으면 3x3 셀만, 없으면 목록 전체입니다.
        private void CollectNeighbors(Vector3 myPos)
        {
            _neighborList = null;
            _neighborHits.Clear();

            if (_neighborGrid != null)
            {
                _neighborGrid.Query(myPos, _neighborHits);

                return;
            }

            _neighborList = _entity.GetNearCheckEntities();
        }

        private int NeighborCount
        {
            get
            {
                if (_neighborGrid != null)
                    return _neighborHits.Count;

                return _neighborList != null ? _neighborList.Count : 0;
            }
        }

        // 이웃 i의 위치. 격자 경로에서는 인터페이스 호출 없이 배열에서 바로 읽습니다.
        private bool TryGetNeighbor(int i, out Vector3 position)
        {
            if (_neighborGrid != null)
            {
                var index = _neighborHits[i];

                if (_neighborGrid.GetEntity(index) == _entity)
                {
                    position = Vector3.zero;

                    return false;
                }

                position = _neighborGrid.GetPosition(index);

                return true;
            }

            var other = _neighborList[i];

            if (other == null || other == _entity || !other.GetActiveState())
            {
                position = Vector3.zero;

                return false;
            }

            position = other.GetPosition();
            position.z = 0f;

            return true;
        }

        // 같은 종류끼리(예: 몬스터끼리) 겹치지 않도록 밀어내는 보정량을 구합니다.
        private Vector3 ComputeSeparation(Vector3 myPos)
        {
            var offset = Vector3.zero;
            var count = NeighborCount;

            for (var i = 0; i < count; i++)
            {
                Vector3 otherPos;

                if (!TryGetNeighbor(i, out otherPos))
                    continue;

                var diff = myPos - otherPos;
                var d = diff.magnitude;

                if (d >= _separationDistance)
                    continue;

                // 정확히 포개져 방향이 없으면 개체별 고정 방향으로 갈라섭니다.
                // (여기서 그냥 건너뛰면 완전히 겹친 둘은 영원히 서로를 못 벗어납니다.)
                if (d < 0.001f)
                {
                    offset += GetFallbackDir() * _separationStrength;

                    continue;
                }

                // 가까울수록 조금 더 밀기
                var ratio = 1f - (d / _separationDistance);
                offset += diff / d * (ratio * _separationStrength);
            }

            // 보정량 제한
            if (offset.magnitude > _maxSeparationOffset)
                offset = offset.normalized * _maxSeparationOffset;

            return offset;
        }

        // 장애물(광물 등) 회피 — 같은 종류끼리의 겹침 보정보다 넓은 반경/강한 힘으로 밀어냅니다.
        private Vector3 ComputeObstacleAvoidance(Vector3 myPos, Vector3 moveDir)
        {
            var offset = Vector3.zero;

            IReadOnlyList<IEntity> list = null;
            var count = 0;

            if (_obstacleGrid != null)
            {
                _obstacleGrid.Query(myPos, _obstacleHits);
                count = _obstacleHits.Count;
            }
            else
            {
                list = _obstacleProvider == null ? null : _obstacleProvider.Invoke();
                count = list != null ? list.Count : 0;
            }

            for (var i = 0; i < count; i++)
            {
                Vector3 obstaclePos;

                if (_obstacleGrid != null)
                {
                    var index = _obstacleHits[i];

                    if (_obstacleGrid.GetEntity(index) == _entity)
                        continue;

                    obstaclePos = _obstacleGrid.GetPosition(index);
                }
                else
                {
                    var obstacle = list[i];

                    if (obstacle == null || obstacle == _entity || !obstacle.GetActiveState())
                        continue;

                    obstaclePos = obstacle.GetPosition();
                    obstaclePos.z = 0f;
                }

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

                offset += avoidDir * (ratio * _obstacleStrength);
            }

            if (offset.magnitude > _maxObstacleOffset)
                offset = offset.normalized * _maxObstacleOffset;

            return offset;
        }

        // ── 이동 ──────────────────────────────────────────────────

        // 한 걸음 나아가되, 그 자리가 다른 개체와 겹치면 들어가지 않습니다.
        //
        // 위치를 밀어내 겹침을 푸는 방식은 세 가지 부작용이 있었습니다.
        //   1) 서로를 계속 밀어 원하지 않는 이동이 생김
        //   2) 밀어내기와 이동이 매 프레임 맞부딪혀 떨림
        //   3) 밀린 몬스터가 캐릭터 안으로 들어감
        // 그래서 '밀어내기'가 아니라 '들어가지 않기'로 바꿨습니다.
        // 힘이 아니라 필터라 진동이 원리적으로 생기지 않고, 남을 밀 수도 없습니다.
        private void StepOrSlide(Vector3 myPos, Vector3 moveDir)
        {
            var speed = _entity.GetMoveSpeed();
            var step = speed * Time.deltaTime;

            if (step <= 0f)
            {
                _moveComponent.Move(0f);

                return;
            }

            if (TryStep(myPos, moveDir, step, speed))
                return;

            // 정면이 막혔으면 옆으로 미끄러져 빈자리를 찾습니다.
            // 좌우 중 개체마다 정해진 쪽을 먼저 보고, 둘 다 막히면 그 프레임은 멈춥니다.
            var perp = new Vector3(-moveDir.y, moveDir.x, 0f) * _spacingSign;

            if (TryStep(myPos, perp, step, speed))
                return;

            if (TryStep(myPos, -perp, step, speed))
                return;

            _moveComponent.Move(0f);
        }

        private bool TryStep(Vector3 myPos, Vector3 dir, float step, float speed)
        {
            if (IsBlocked(myPos, myPos + dir * step))
                return false;

            _moveComponent.SetMoveVec(dir);
            _moveComponent.Move(speed);

            return true;
        }

        // to로 옮겼을 때 다른 개체와 겹치는지.
        //
        // '겹치면 무조건 금지'가 아니라 '지금보다 더 가까워지는 이동만 금지'입니다.
        // 이미 겹친 채로 스폰되거나 캐릭터가 무리를 통과한 경우에도
        // 멀어지는 방향으로는 언제든 움직일 수 있어 스스로 풀립니다.
        private bool IsBlocked(Vector3 from, Vector3 to)
        {
            if (_blockRadius <= 0f)
                return false;

            var radiusSqr = _blockRadius * _blockRadius;
            var count = NeighborCount;

            for (var i = 0; i < count; i++)
            {
                Vector3 otherPos;

                if (!TryGetNeighbor(i, out otherPos))
                    continue;

                var toSqr = (to - otherPos).sqrMagnitude;

                if (toSqr >= radiusSqr)
                    continue;

                // 이미 겹쳐 있어도 '멀어지는 이동'은 막지 않습니다.
                if (toSqr < (from - otherPos).sqrMagnitude)
                    return true;
            }

            return false;
        }

        // 사거리에 들어와 멈춰 있는 동안의 처리.
        //
        // 평소에는 가만히 있습니다. 다른 몬스터와 이미 겹쳐 있을 때만(넉백에 밀렸거나,
        // 캐릭터가 무리를 통과했거나) 스스로 옆으로 비켜 빈자리를 찾습니다.
        // 남을 밀지 않고 자기만 움직이며, 겹침이 풀리는 즉시 멈춥니다.
        //
        // 타겟 방향 성분을 빼고 접선 성분만 쓰기 때문에 타겟까지의 거리는 그대로 유지됩니다.
        // (거리가 변하면 다가갔다 멀어지는 진동이 생깁니다.)
        private void StepAsideWhileStopped(Vector3 myPos, Vector3 toTargetDir)
        {
            var away = GetOverlapEscapeDir(myPos);

            if (away.sqrMagnitude < 0.0001f)
            {
                _moveComponent.Move(0f);

                return;
            }

            var tangential = away - toTargetDir * Vector3.Dot(away, toTargetDir);

            // 정확히 포개져 접선이 사라지면 개체마다 다른 각도로 흩어집니다.
            if (tangential.sqrMagnitude < 0.0001f)
                tangential = GetFallbackDir();

            var speed = _entity.GetMoveSpeed() * _stepAsideSpeedScale;
            var step = speed * Time.deltaTime;
            var dir = tangential.normalized;

            // 더 가까워지는 이동은 어느 쪽으로도 하지 않습니다.
            if (TryStep(myPos, dir, step, speed))
                return;

            if (TryStep(myPos, -dir, step, speed))
                return;

            _moveComponent.Move(0f);
        }

        // 겹쳐 있는 상대들에게서 멀어지는 방향. 겹친 상대가 없으면 zero.
        private Vector3 GetOverlapEscapeDir(Vector3 myPos)
        {
            var away = Vector3.zero;

            if (_blockRadius <= 0f)
                return away;

            var count = NeighborCount;

            for (var i = 0; i < count; i++)
            {
                Vector3 otherPos;

                if (!TryGetNeighbor(i, out otherPos))
                    continue;

                var diff = myPos - otherPos;
                var d = diff.magnitude;

                if (d >= _blockRadius)
                    continue;

                // 완전히 포개진 상대는 방향이 없으므로 개체별 고정 방향으로 갈라섭니다.
                if (d < 0.001f)
                {
                    away += GetFallbackDir();

                    continue;
                }

                away += diff / d * (1f - d / _blockRadius);
            }

            return away;
        }

        // 완전히 포개졌을 때 쓰는 개체별 고정 방향. 해시로 각도를 갈라 서로 다른 쪽으로 흩어집니다.
        private Vector3 GetFallbackDir()
        {
            var angle = (_entity.GetHashCode() & 0xFFFF) * (Mathf.PI * 2f / 65536f);

            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
    }
}
