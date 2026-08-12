// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Data;
// using DG.Tweening;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace InGame.System.Skill.UI
// {
//     public interface ISkillCardUIHandler
//     {
//         void Select();
//         void Deselect();
//         void ShowEffect();
//         void HideEffect();
//         void ChangeSkillCard(bool isSkillExecuted);
//         void ExecuteSkillEffect();
//         void MoveContents(Vector2 pos, bool isUseDuration, Action callback = null);
//         RectTransform GetContentsRectTransform();
//     }
//
//     public class SkillCardElementUI : GameMonoInitializer
//     {
//         public Sprite GetSpriteIcon { get; private set; }
//         // public SkillDataRowTable Data { get; private set; }
//         public int Index { get; private set; }
//         
//         private RectTransform RectTransform { get; set; }
//         private float Duration => 0.2f;
//         private float SelectScale => 1.2f;
//         private float StartPosY => -800.0f;
//
//         [SerializeField] 
//         private Image skillIconImage;
//         [SerializeField] 
//         private TMP_Text skillCostText;
//         [SerializeField] 
//         private TMP_Text skillNameText;
//         [SerializeField] 
//         private TMP_Text skillDescText;
//         [SerializeField] 
//         private TMP_Text skillTypeText;
//         [SerializeField] 
//         private RectTransform contents;
//         [SerializeField] 
//         private SkillCardDrag dragHandler;
//         [SerializeField]
//         private GameObject effectObject;
//
//         [SerializeField] 
//         private List<GameObject> symbolObjectList;
//         [SerializeField]
//         private GameObject costEffectObject;
//
//         private void Awake()
//         {
//             RectTransform = GetComponent<RectTransform>();
//         }
//
//         // public void Init(SkillDataRowTable data, ISkillUIControllerHandler uiHandler)
//         // {
//         //     Data = data;
//         //     SkillUIControllerHandler = uiHandler;
//         //     costEffectObject.SetActive(true);
//         //     
//         //     // To Do: 임시
//         //     skillIconImage.sprite = Resources.Load<Sprite>($"Icon/{Data.IconAssetKey}");
//         //     GetSpriteIcon = skillIconImage.sprite;
//         //     skillCostText.text = $"{Data.Cost}";
//         //     skillNameText.text = $"{Data.NameKey}";
//         //     skillTypeText.text = $"{Data.SkillType}";
//         //     
//         //     var args = (Data.EffectValueList ?? new List<float>())
//         //         .Select(x => (object)$"<color=#FFFF00>{x}</color>")
//         //         .ToArray();
//         //     
//         //     skillDescText.text = string.Format(Data.DescKey, args);
//         //     
//         //     for (var i = 0; i < symbolObjectList.Count; i++)
//         //     {
//         //         symbolObjectList[i].SetActive(i == (int)Data.SkillType);
//         //     }
//         //     
//         //     contents.localScale = Vector3.one;
//         //     contents.anchoredPosition = new Vector2(0, StartPosY);
//         //     
//         //     effectObject.SetActive(false);
//         //     dragHandler.Init(Data, this);
//         // }
//
//         public void SetIndex(int index)
//         {
//             Index = index;
//         }
//         
//         public void Appear()
//         {
//             contents.DOAnchorPos(Vector2.zero, Duration);
//         }
//
//         public void Move(Vector2 pos)
//         {
//             switch (pos.x)
//             {
//                 case > 0:
//                     RectTransform.DORotate(new Vector3(0, 0, -3), 0.2f);
//                     break;
//                 case < 0:
//                     RectTransform.DORotate(new Vector3(0, 0, 3), 0.2f);
//                     break;
//                 default:
//                     RectTransform.DORotate(Vector3.zero, 0.2f);
//                     pos += new Vector2(0, 12);
//                     break;
//             }
//             
//             RectTransform
//                 .DOAnchorPos(pos, Duration)
//                 .OnComplete(Appear);
//         }
//         
// #region ISkillCardUIHandler
//
//         public void Select()
//         {
//             contents.DOScale(SelectScale, Duration);
//             transform.SetAsLastSibling();
//         }
//         
//         public void Deselect()
//         {
//             HideEffect();
//             contents.DOScale(1.0f, Duration);
//         }
//
//         public void ShowEffect()
//         {
//             effectObject.SetActive(true);
//         }
//
//         public void HideEffect()
//         {
//             effectObject.SetActive(false);
//         }
//
//         public void ExecuteSkillEffect()
//         {
//             // SkillUIControllerHandler.ExecuteSkillEffect(Data);
//         }
//
//         public void MoveContents(Vector2 pos, bool isUseDuration, Action callback = null)
//         {
//             contents
//                 .DOAnchorPos(pos, isUseDuration ? Duration : 0.0f)
//                 .OnComplete(() => callback?.Invoke());
//         }
//
//         public RectTransform GetContentsRectTransform()
//         {
//             return contents;
//         }
//
// #endregion
//     }
// }