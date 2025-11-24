using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NotImplementedException = System.NotImplementedException;

namespace InGame.System.Skill.UI
{
    public interface ISkillCardUIHandler
    {
        void Select();
        void Deselect();
        void ShowEffect();
        void HideEffect();
        void ChangeSkillCard(bool isSkillExecuted);
        void MoveContents(Vector2 pos, bool isUseDuration, Action callback = null);
        RectTransform GetContentsRectTransform();
        ISkillUIControllerHandler GetSkillUIControllerHandler();
    }

    public class SkillCardElementUI : GameInitializer, ISkillCardUIHandler
    {
        public Sprite GetSpriteIcon { get; private set; }
        public SkillData Data { get; private set; }
        public int Index { get; private set; }
        
        private ISkillUIControllerHandler SkillUIControllerHandler { get; set; }
        private RectTransform RectTransform { get; set; }
        private float Duration => 0.2f;
        private float SelectScale => 1.2f;
        private float StartPosY => -800.0f;

        [SerializeField] 
        private Image skillIconImage;
        [SerializeField] 
        private TMP_Text skillCostText;
        [SerializeField] 
        private TMP_Text skillNameText;
        [SerializeField] 
        private RectTransform contents;
        [SerializeField] 
        private SkillCardDrag dragHandler;
        [SerializeField]
        private GameObject effectObject;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public void Init(SkillData data, ISkillUIControllerHandler uiHandler)
        {
            Data = data;
            SkillUIControllerHandler = uiHandler;
            
            // To Do: 임시
            skillIconImage.sprite = Resources.Load<Sprite>($"Icon/{Data.IconAssetName}");
            GetSpriteIcon = skillIconImage.sprite;
            skillCostText.text = $"{Data.Cost}";
            skillNameText.text = $"{Data.Id}";

            contents.localScale = Vector3.one;
            contents.anchoredPosition = new Vector2(0, StartPosY);
            
            effectObject.SetActive(false);
            dragHandler.Init(Data, this);
        }

        public void SetIndex(int index)
        {
            Index = index;
            skillNameText.text = $"{Index}";
        }
        
        public void Appear()
        {
            contents.DOAnchorPos(Vector2.zero, Duration);
        }

        public void Move(Vector2 pos)
        {
            RectTransform
                .DOAnchorPos(pos, Duration)
                .OnComplete(Appear);
        }
        
#region ISkillCardUIHandler

        public void Select()
        {
            contents.DOScale(SelectScale, Duration);
        }
        
        public void Deselect()
        {
            HideEffect();
            contents.DOScale(1.0f, Duration);
        }

        public void ShowEffect()
        {
            effectObject.SetActive(true);
        }

        public void HideEffect()
        {
            effectObject.SetActive(false);
        }

        public void ChangeSkillCard(bool isSkillExecuted)
        {
            MoveContents(new Vector2(0, StartPosY), true, 
                () => SkillUIControllerHandler.NextCard(this, isSkillExecuted));
        }

        public void MoveContents(Vector2 pos, bool isUseDuration, Action callback = null)
        {
            contents
                .DOAnchorPos(pos, isUseDuration ? Duration : 0.0f)
                .OnComplete(() => callback?.Invoke());
        }

        public RectTransform GetContentsRectTransform()
        {
            return contents;
        }

        public ISkillUIControllerHandler GetSkillUIControllerHandler()
        {
            return SkillUIControllerHandler;
        }

#endregion
    }
}