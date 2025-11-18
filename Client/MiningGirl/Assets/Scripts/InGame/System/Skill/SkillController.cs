using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.Skill.UI;
using UnityEngine;

namespace InGame.System.Skill
{
    public class SkillController : GameInitializer, IDisposable
    {
        [SerializeField]
        private SkillPointGauge skillPointGauge;
        [SerializeField]
        private SkillInfoViewer skillInfoViewer;
        [SerializeField] 
        private List<SkillCardElementUI> testUiList;
        
        private bool _isGameStated;
        private CancellationTokenSource _cts;
        
        public void Init()
        {
            IsInitialized = false;
            _isGameStated = false;
            
            skillInfoViewer.Hide();
            skillPointGauge.Init(10);

            for (var i = 0; i < testUiList.Count; i++)
            {
                testUiList[i].Init(i, SkillCardTouch, ExecuteSkillEffect);
            }
            
            IsInitialized = true;
        }

        public void Appear()
        {
            skillPointGauge.Appear();
        }

        public async UniTaskVoid ExecuteSkillPointGauge()
        {
            _isGameStated = true;
            Dispose();
            _cts ??= new CancellationTokenSource();
            
            try
            {
                while (_isGameStated)
                {
                    await UniTask.WaitForSeconds(1.0f, cancellationToken: _cts.Token);
                
                    skillPointGauge.UpdateGaugeUI(1);
                }
            }
            catch (OperationCanceledException)
            {
                _isGameStated = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isGameStated = false;
            }
        }

        private void SkillCardTouch(int index)
        {
            var isShow = false;
            
            for (var i = 0; i < testUiList.Count; i++)
            {
                if (i != index)
                    testUiList[i].UnTouch();
            }
            
            skillInfoViewer.Set(index);
            skillInfoViewer.Show(); 
        }

        private void ExecuteSkillEffect(SkillCardElementUI selectSkill)
        {
            skillPointGauge.UpdateGaugeUI(-selectSkill.SkillCost);
            skillInfoViewer.Hide();
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion
    }
}