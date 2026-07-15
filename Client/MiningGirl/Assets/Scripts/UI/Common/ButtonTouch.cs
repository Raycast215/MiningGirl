using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Common
{
    public class ButtonTouch : GameMonoInitializer, IPointerEnterHandler, IPointerExitHandler
    {
        public Button GetButton { get; private set; }

        [Header("UI")]
        [SerializeField] 
        private Transform iconTransform;
        
        [Header("Option")]
        [SerializeField] private List<Color> colorList;
        [SerializeField]
        private float animationDuration = 0.1f;
        [SerializeField] 
        private float toScale = 0.9f;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
            GetButton = GetComponent<Button>();
        }

        public void SetColor(int index)
        {
            _image.color = colorList[index];
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            iconTransform.DOScale(toScale, animationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            iconTransform.DOScale(1.0f, animationDuration);
        }
    }
}