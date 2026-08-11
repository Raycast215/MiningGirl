using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Floating : GameMonoInitializer
{
    private event Action<Floating> OnReleased;
    
    [SerializeField]
    private TMP_Text damageText;
    
    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    
    public async void Init(int damage, Vector2 position, Action<Floating> onReleased, bool isCritical = false)
    {
        OnReleased = null;
        OnReleased += onReleased;
        
        gameObject.SetActive(true);
        
        _rect ??= GetComponent<RectTransform>();
        _canvasGroup ??= GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0;
        
        var startPos = position + new Vector2(0f, 64.0f);
        
        damageText.text = $"{damage}";
        _rect.localPosition = startPos;
        _rect.DOScale(isCritical ? 3.0f : 1.5f, 0.0f);
        
        await UniTask.Yield();
        
        _canvasGroup.alpha = 1;
        
        if (isCritical)
        {
            // Camera.main.orthographicSize = 20;
            // await UniTask.Yield();
            
            // _rect.DOShakeAnchorPos(
            //     duration: 0.1f,
            //     strength: new Vector3(64f, 0f, 0f),  // X만
            //     vibrato: 10,
            //     randomness: 90,
            //     snapping: false,
            //     fadeOut: true
            // ).SetRelative(true).OnComplete(() => Camera.main.orthographicSize = 25);
        }
        
        _rect.DOScale(1.0f, 0.3f).SetDelay(0.2f);
        _rect.DOAnchorPosY(_rect.anchoredPosition.y + 200.0f, 1.2f);
        _canvasGroup.DOFade(0.0f, 1.0f).SetDelay(0.5f).OnComplete(Clear);
    }

    private void Clear()
    {
        OnReleased?.Invoke(this);
    }
}