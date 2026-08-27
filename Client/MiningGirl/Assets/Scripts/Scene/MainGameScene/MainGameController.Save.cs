using System.Collections.Generic;
using Data;
using Manager;
using Manager.Save;
using Scene.MainGameScene.Progress;
using UnityEngine;

namespace Scene.MainGameScene
{
    // 진행 상태 저장. 컨트롤러의 나머지와 파일만 나눕니다.
    //
    // 담고 되돌리는 일이 거의 모든 시스템을 건드려서, 한 파일에 두면 판을 굴리는
    // 코드와 섞입니다. 클래스를 나누면 참조를 열 개 넘게 넘겨야 해서 partial로 둡니다.
    public partial class MainGameController
    {
        // 복원할 저장. 스테이지를 정할 때 꺼내 두고, 판을 세운 뒤에 씁니다.
        private RunSaveData _pendingRestore;

        // 지정 진입(에디터 디버그)으로 들어왔는가.
        //
        // 이 경우 복원하지 않고 저장도 지우지 않습니다. 개발자가 스테이지를
        // 강제했으면 그 스테이지를 새로 돌고 싶은 것이지 이어하기를 원하는 게
        // 아니고, 디버그로 한 판 봤다고 유저 진행이 날아가면 안 됩니다.
        private bool _isDebugStageEntry;

        // 지금 3택에 떠 있는 카드들. 표시 순서 그대로입니다.
        //
        // _shownChoiceKeys는 비교용이라 정렬돼 있어 순서가 없습니다.
        private readonly List<string> _openChoiceKeys = new List<string>();

#region 저장

        // 백그라운드로 갈 때 씁니다. OnApplicationQuit은 모바일에서 안 불릴 수 있습니다 -
        // 안드로이드가 프로세스를 그냥 죽입니다.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow("일시정지");
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                SaveNow("포커스 잃음");
        }

        // 웨이브가 바뀔 때의 보조 저장.
        //
        // "그 순간 그대로"의 대체가 아니라 바닥입니다. 프로세스가 예고 없이 죽었을 때
        // 최악이 "웨이브 하나 되감기"가 되도록 하는 것입니다.
        private void HandleWaveStartedForSave(int waveNo)
        {
            SaveNow($"웨이브 {waveNo}");
        }

        private void SaveNow(string reason)
        {
            if (!IsInitialized || _isFinished || _stage == null)
                return;

            // 지정 진입으로 돌던 판은 저장하지 않습니다. 유저 진행을 덮어씁니다.
            if (_isDebugStageEntry)
                return;

            var data = CaptureSave();

            if (RunSaveStore.Write(data))
                Debug.Log($"[Save] 저장했습니다 ({reason}) - {data.StageId} 웨이브 {_waveRunner.CurrentWaveNo}");
        }

        private RunSaveData CaptureSave()
        {
            var data = new RunSaveData
            {
                StageId = _stage.Id,
                CharacterId = _character.Id,
                Elapsed = _elapsed,
                FirstLevelUpTime = FirstLevelUpElapsed,
                TowerHealth = tower != null ? tower.CurrentHealth : 0f,
            };

            data.Wave.Phase = _waveRunner.CapturePhase();
            data.Wave.WaveIndex = _waveRunner.CaptureWaveIndex();
            data.Wave.Timer = _waveRunner.CaptureTimer();
            data.Wave.ScheduleIndex = _waveRunner.CaptureScheduleIndex();
            data.Wave.ScheduleCount = _waveRunner.CaptureScheduleCount();

            data.Level.Level = _levelSystem.Level;
            data.Level.ExpInLevel = _levelSystem.ExpInLevel;
            data.Level.TotalExp = _levelSystem.TotalExp;
            data.Level.TotalKills = _levelSystem.TotalKills;
            data.Level.PendingLevelUps = _pendingLevelUps;

            data.Choice.RerollsLeft = _rerollsLeft;
            data.Choice.ShownKeys.AddRange(_shownChoiceKeys);
            data.Choice.PanelOpen = _choiceViewModel != null && _choiceViewModel.IsVisible.Value;

            if (data.Choice.PanelOpen)
                data.Choice.OpenKeys.AddRange(_openChoiceKeys);

            var masteryTable = DataTableManager.Instance.SkillMasteryDataTable;
            var skills = _inventory.Skills;

            for (var i = 0; i < skills.Count; i++)
            {
                var state = skills[i];

                var save = new SkillSave
                {
                    SkillId = state.Row.Id,
                    CooldownRemaining = _skillRunner.GetCooldownRemaining(state),
                };

                // 강화스킬은 스킬 하나에 하나뿐이라 스킬 Id로 되찾을 수 있습니다.
                // MasterySpec은 행 Id를 들고 있지 않아 시트에서 다시 꺼냅니다.
                if (state.Mastery.HasValue)
                {
                    var row = masteryTable?.FindBySkillId(state.Row.Id);

                    if (row != null)
                        save.MasteryId = row.Id;
                }

                foreach (var pair in state.UpgradeCounts)
                {
                    if (pair.Value > 0)
                        save.UpgradeCounts.Add(new UpgradeCountSave(pair.Key.ToString(), pair.Value));
                }

                data.Skills.Add(save);
            }

            var alive = _field.Alive;

            for (var i = 0; i < alive.Count; i++)
            {
                var unit = alive[i];

                if (unit == null || !unit.IsAlive)
                    continue;

                var position = unit.Position;

                data.Monsters.Add(new MonsterSave
                {
                    MonsterId = unit.Row.Id,
                    X = position.x,
                    Y = position.y,
                    Health = unit.CurrentHealth,
                    AttackTimer = unit.AttackTimer,
                    FreezeRemaining = unit.FreezeRemaining,
                    BurnRemaining = unit.BurnRemaining,
                    BurnPerSecond = unit.BurnPerSecond,
                    HasReachedTower = unit.HasReachedTower,
                });
            }

            return data;
        }

#endregion

#region 복원

        // 스테이지를 정하기 전에 부릅니다. 복원할 게 있으면 담아 둡니다.
        //
        // 우선순위는 넷입니다.
        //   1  지정 진입(에디터)   있으면 복원도 삭제도 안 합니다
        //   2  진행 저장
        //   3  스테이지 선택
        //   4  인스펙터 값
        private string ResolveStageIdWithSave()
        {
            _pendingRestore = null;

#if UNITY_EDITOR
            var debugStageId = UnityEditor.SessionState.GetString(DebugStageIdKey, string.Empty);

            if (!string.IsNullOrEmpty(debugStageId))
            {
                _isDebugStageEntry = true;

                Debug.Log($"[MainGame] 스테이지 지정 진입: {debugStageId}");

                if (RunSaveStore.Exists())
                    Debug.Log($"[Save] 지정 진입이 있어 복원을 건너뜁니다: {debugStageId}");

                return debugStageId;
            }
#endif

            var save = RunSaveStore.Read();

            if (save != null)
            {
                var verdict = RunSaveValidator.Validate(save, DataTableManager.Instance);

                if (verdict.IsOk)
                {
                    _pendingRestore = save;

                    Debug.Log($"[Save] 이어하기: {save.StageId} 웨이브 {save.Wave.WaveIndex + 1}");

                    // 복원 중에는 StageEntry를 쓰지 않습니다. 소비해 버리면
                    // 복원이 취소됐을 때 그 값이 이미 없습니다.
                    return save.StageId;
                }

                RunSaveValidator.LogFailure(verdict);
                RunSaveStore.Clear();

                // 스테이지를 모르면 갈 곳이 없습니다. 나머지는 그 스테이지 처음부터입니다.
                if (verdict.Result != RunSaveValidator.EResult.StageMissing)
                    _restoreFailedStageId = save.StageId;
            }

            var selected = StageEntry.Consume();

            if (!string.IsNullOrEmpty(selected))
            {
                Debug.Log($"[MainGame] 스테이지 선택 진입: {selected}");

                return selected;
            }

            return stageId;
        }

        // 복원에 실패했을 때 대신 시작할 스테이지. 비어 있으면 실패가 없었습니다.
        private string _restoreFailedStageId;

        // 판을 다 세운 뒤에 부릅니다. 되돌리지 못하면 false입니다.
        private bool TryRestore()
        {
            if (_pendingRestore == null)
                return false;

            var save = _pendingRestore;

            _pendingRestore = null;

            // 웨이브를 먼저 봅니다. 스케줄 길이가 달라졌으면 나머지를 되돌릴 이유가 없습니다.
            if (!_waveRunner.RestoreState(
                    save.Wave.Phase,
                    save.Wave.WaveIndex,
                    save.Wave.Timer,
                    save.Wave.ScheduleIndex,
                    save.Wave.ScheduleCount))
            {
                RunSaveValidator.LogFailure(new RunSaveValidator.Verdict(
                    RunSaveValidator.EResult.ScheduleChanged,
                    $"웨이브 {save.Wave.WaveIndex + 1}의 스케줄이 달라졌습니다"));

                return false;
            }

            RestoreSkills(save);

            _levelSystem.Restore(
                save.Level.Level,
                save.Level.ExpInLevel,
                save.Level.TotalExp,
                save.Level.TotalKills);
            _pendingLevelUps = Mathf.Max(0, save.Level.PendingLevelUps);

            _elapsed = save.Elapsed;
            FirstLevelUpElapsed = save.FirstLevelUpTime;

            _rerollsLeft = Mathf.Max(0, save.Choice.RerollsLeft);
            _shownChoiceKeys.Clear();
            _shownChoiceKeys.AddRange(save.Choice.ShownKeys);

            if (tower != null)
                tower.RestoreHealth(save.TowerHealth);

            RestoreMonsters(save);

            return true;
        }

        private void RestoreSkills(RunSaveData save)
        {
            var skillTable = DataTableManager.Instance.SkillDataTable;
            var upgradeTable = DataTableManager.Instance.SkillUpgradeDataTable;
            var masteryTable = DataTableManager.Instance.SkillMasteryDataTable;

            for (var i = 0; i < save.Skills.Count; i++)
            {
                var entry = save.Skills[i];
                var row = skillTable?.GetRow(entry.SkillId);

                if (row == null)
                    continue;

                var state = _inventory.Add(row);

                if (state == null)
                    continue;

                // 누적값이 아니라 횟수를 다시 적용합니다. 그래야 시트의 EffectValue가
                // 바뀌었을 때 최신 값으로 다시 쌓입니다.
                for (var k = 0; k < entry.UpgradeCounts.Count; k++)
                {
                    var count = entry.UpgradeCounts[k];
                    var upgrade = RunSaveValidator.FindUpgrade(upgradeTable, entry.SkillId, count.Type);

                    if (upgrade == null)
                        continue;

                    for (var n = 0; n < count.Count; n++)
                        state.ApplyUpgrade(upgrade);
                }

                if (!string.IsNullOrEmpty(entry.MasteryId))
                {
                    var mastery = masteryTable?.FindBySkillId(entry.SkillId);

                    if (mastery != null && mastery.Id == entry.MasteryId)
                    {
                        state.SetMastery(mastery);
                        _inventory.MarkMasteryTaken();
                    }
                }

                _skillRunner.SetCooldownRemaining(state, entry.CooldownRemaining);
            }
        }

        private void RestoreMonsters(RunSaveData save)
        {
            var monsterTable = DataTableManager.Instance.MonsterDataTable;

            for (var i = 0; i < save.Monsters.Count; i++)
            {
                var entry = save.Monsters[i];
                var row = monsterTable?.GetRow(entry.MonsterId);

                if (row == null)
                    continue;

                var unit = _field.SpawnAt(row, new Vector3(entry.X, entry.Y, 0f));

                if (unit == null)
                    continue;

                unit.RestoreState(
                    entry.Health,
                    entry.AttackTimer,
                    entry.FreezeRemaining,
                    entry.BurnRemaining,
                    entry.BurnPerSecond,
                    entry.HasReachedTower);
            }
        }

        // 3택이 열린 채로 껐다면 그 세 장을 그대로 되살립니다.
        // ViewModel을 붙인 뒤에 불러야 해서 복원 본체와 나눠 둡니다.
        private void RestoreOpenChoice(RunSaveData save)
        {
            if (save == null || !save.Choice.PanelOpen)
                return;

            var choices = _choiceBuilder.RebuildFromKeys(save.Choice.OpenKeys);

            if (choices == null || choices.Count == 0)
            {
                // 못 되살리면 그냥 다시 뽑습니다. 여기까지 왔으면 판 자체는 멀쩡하고,
                // 3택 한 번을 새로 뽑는 것이 판을 통째로 버리는 것보다 낫습니다.
                Debug.LogWarning("[Save] 열려 있던 3택을 되살리지 못해 새로 뽑습니다.");

                ShowNextLevelUp();

                return;
            }

            RememberShown(choices);

            SetPaused(true);
            _choiceViewModel.Show(_levelSystem.Level, choices);
            ApplyRerollState();
        }

        // 판이 끝나면 지웁니다. 클리어든 타워 파괴든 포기든 같습니다.
        //
        // 포기에만 붙이면 클리어한 판의 저장이 남아, 다음 실행에서 이미 끝난 판이
        // 되살아납니다. 플레이어가 클리어한 스테이지에 다시 갇힙니다.
        private void ClearRunSave()
        {
            // 지정 진입으로 돌던 판은 유저 저장을 건드리지 않습니다.
            if (_isDebugStageEntry)
                return;

            RunSaveStore.Clear();
        }

#endregion
    }
}
