using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Stage.UI
{
    public class OreCountUI : GameInitializer
    {
        [Header("UI")]
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TMP_Text countText;

        [Header("Option")]
        [SerializeField] 
        private float startPosX = -400.0f;
        
        private RectTransform _rect;
        private int _count;
        
        public void Init()
        {
            _rect = GetComponent<RectTransform>();
            _rect.anchoredPosition = new Vector2(startPosX, _rect.anchoredPosition.y);

            _count = 0;
            countText.text = $"x {_count}";
        }

        public void Appear()
        {
            _rect.DOAnchorPosX(0, 0.2f);
        }

        public void IncreaseCount(int addCount)
        {
            _count += addCount;
            countText.text = $"x {_count}";
            countText.transform.DOScale(1.2f, 0.0f);
            countText.transform.DOScale(1.0f, 0.2f);
        }
    }
}