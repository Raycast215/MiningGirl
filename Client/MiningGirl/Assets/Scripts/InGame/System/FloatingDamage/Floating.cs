using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Floating : GameInitializer
{
    private event Action<Floating> OnReleased;
    
    [SerializeField]
    private TMP_Text damageText;
    [SerializeField] 
    private GameObject addObject;
    
    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    
    public async void Init(int damage, Vector2 position, Action<Floating> onReleased, bool isAdd = false)
    {
        OnReleased = null;
        OnReleased += onReleased;
        
        gameObject.SetActive(true);
        
        _rect ??= GetComponent<RectTransform>();
        _canvasGroup ??= GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0;
        
        var startPos = position + new Vector2(0f, 100.0f) + (isAdd ? new Vector2(0f, 100.0f) : Vector2.zero);
        
        addObject.SetActive(isAdd);
        
        damageText.text = $"{damage}";
        _rect.anchoredPosition = startPos;
        _rect.DOScale(1.5f, 0.0f);

        await UniTask.Yield();
        
        _canvasGroup.alpha = 1;
        _rect.DOScale(1.0f, 0.3f).SetDelay(0.2f);
        _rect.DOAnchorPosY(_rect.anchoredPosition.y + 500.0f, 1.2f);
        _canvasGroup.DOFade(0.0f, 1.0f).SetDelay(0.3f).OnComplete(Clear);
    }

    private void Clear()
    {
        OnReleased?.Invoke(this);
    }
}