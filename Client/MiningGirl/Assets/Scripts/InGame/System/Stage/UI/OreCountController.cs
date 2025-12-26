using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Stage.UI
{
    public class OreCountController : GameInitializer
    {
        [SerializeField]
        private OreCountUI rockCountUI;
        
        private VerticalLayoutGroup _verticalLayoutGroup;
        private Queue<Action> _queue;
        private bool _isPlaying;
        
        public void Initialize()
        {
            _verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            _verticalLayoutGroup.enabled = false;
            _queue = new Queue<Action>();
            _isPlaying = false;
            
            rockCountUI.Init();
            
            IsInitialized = true;
        }
        
        public void Appear()
        {
            rockCountUI.Appear();
        }
        
        public void IncreaseOreCount(int addCount)
        {
            Play().Forget();
            _queue.Enqueue(() => rockCountUI.IncreaseCount(addCount));
        }

        private async UniTaskVoid Play()
        {
            if (_isPlaying)
                return;

            _isPlaying = true;
            
            while (true)
            {
                if (_queue == null || _queue.Count == 0)
                {
                    await UniTask.Yield();
                    continue;
                }
                    
                _queue.Dequeue().Invoke();
                await UniTask.DelayFrame(8);
            }
        }
    }
}