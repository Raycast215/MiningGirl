using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.Skill.UI;
using UnityEngine;

namespace InGame.System.Skill
{
    public interface ISkillControllerHandler : ISkillHandlerParentHandler
    {
        public void ExecuteSkillEffect(int cost);
        public void OnSkillCardTouch(int index);
        public void HideInfoUI();
        public int GetSkillPoint();
        public SkillCardDelete GetSkillCardDelete();
    }
    
    public class SkillController : GameInitializer, IDisposable, ISkillControllerHandler
    {
        [SerializeField]
        private SkillPointGauge skillPointGauge;
        [SerializeField]
        private SkillInfoViewer skillInfoViewer;
        [SerializeField]
        private SkillCardDelete skillCardDelete;
        [SerializeField] 
        private List<SkillCardElementUI> testUiList;
        
        private bool _isGameStated;
        private CancellationTokenSource _cts;
        private ISkillHandlerParentHandler _handler;
        
        public void Init(ISkillHandlerParentHandler handler)
        {
            IsInitialized = false;
            _isGameStated = false;
            
            _handler = handler;
            
            skillInfoViewer.Hide();
            skillPointGauge.Init(10);

            for (var i = 0; i < testUiList.Count; i++)
            {
                testUiList[i].Init(i, this);
            }
            
            IsInitialized = true;
        }

        public async void Appear()
        {
            skillPointGauge.Appear();

            foreach (var skillUI in testUiList)
            {
                await UniTask.WaitForSeconds(0.1f);
                skillUI.Appear();
            }
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
                    await UniTask.WaitForSeconds(2.0f, cancellationToken: _cts.Token);
                
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
        
        private void OnDestroy()
        {
            Dispose();
        }

#region ISkillControllerHandler

        public Canvas GetUICanvas()
        {
            return _handler.GetUICanvas();
        }
        
        public void ExecuteSkillEffect(int cost)
        {
            skillPointGauge.UpdateGaugeUI(cost);
            skillInfoViewer.Hide();
        }

        public void OnSkillCardTouch(int index)
        {
            for (var i = 0; i < testUiList.Count; i++)
            {
                if (i != index)
                    testUiList[i].OnDeselectCard();
            }
            
            skillInfoViewer.Set(index);
            skillInfoViewer.Show(); 
        }

        public void HideInfoUI()
        {
            skillInfoViewer.Hide();
        }

        public int GetSkillPoint()
        {
            return skillPointGauge.SkillPoint;
        }

        public SkillCardDelete GetSkillCardDelete()
        {
            return skillCardDelete;
        }

#endregion
        
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