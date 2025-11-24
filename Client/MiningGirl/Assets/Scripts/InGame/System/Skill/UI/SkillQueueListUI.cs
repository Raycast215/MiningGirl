using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public class SkillQueueListUI : GameInitializer
    {
        [SerializeField] 
        private List<Image> queueListUI;

        private Dictionary<string, Sprite> _spriteDic;
        private Queue<string> _skillIdQueue;

        public void Init()
        {
            _spriteDic = new Dictionary<string, Sprite>();
            _skillIdQueue = new Queue<string>();
        }

        public void Enqueue(string skillId, Sprite sprite)
        {
            _skillIdQueue.Enqueue(skillId);

            // UI 수 보다 크면 큐에서 제거.
            if (_skillIdQueue.Count > queueListUI.Count)
                _skillIdQueue.Dequeue();
            
            // 키가 없으면 추가.
            if (!_spriteDic.ContainsKey(skillId))
            {
                _spriteDic.Add(skillId, sprite);
                Debug.Log($"skillId: {skillId} / sprite: {sprite.name}");
            }
                
            // _spriteDic.TryAdd(skillId, sprite);

            for (var i = 0; i < queueListUI.Count; i++)
            {
                queueListUI[i].sprite = _spriteDic[_skillIdQueue.ElementAt(i)];
            }
        }
    }
}