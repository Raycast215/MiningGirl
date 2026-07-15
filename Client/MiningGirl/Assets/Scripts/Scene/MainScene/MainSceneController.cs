using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Scene.MainScene.SubContents;
using Scene.StartScene.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Scene.MainScene
{
    public enum ESubContentsType
    {
        Shop,
        Collection,
        InGame,
        Mission,
        Setting,
        Home
    }
    
    public interface IMainBottomMenuHandler
    {
        void ShowHomeMenu();
        void ShowInGameMenu();
        void ShowMissionsMenu();
        void ShowSettingsMenu();
        void ShowCollectionMenu();
        void ShowShopMenu();
    }
    
    public class MainSceneController : GameMonoInitializer, IMainBottomMenuHandler
    {
        [FormerlySerializedAs("tabGame")] [SerializeField]
        private TabGameMono tabGameMono;

        [SerializeField] 
        private List<GameObject> contentsGroupList;
        
        [Header("ButtonMenu")]
        [SerializeField]
        private List<MainMenuButton> bottomMenuButtonList;
        
        private void Start()
        {
            Initialize().Forget();
        }

        public void EnterMiningCave()
        {
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("InGameScene")).Forget();
        }

        private async UniTaskVoid Initialize()
        {
            tabGameMono.Initialize();
            
            bottomMenuButtonList[(int)ESubContentsType.Shop].Initialize(this, ShowShopMenu);
            bottomMenuButtonList[(int)ESubContentsType.Collection].Initialize(this, ShowCollectionMenu);
            bottomMenuButtonList[(int)ESubContentsType.InGame].Initialize(this, ShowInGameMenu);
            bottomMenuButtonList[(int)ESubContentsType.Mission].Initialize(this, ShowMissionsMenu);
            bottomMenuButtonList[(int)ESubContentsType.Setting].Initialize(this, ShowSettingsMenu);
            
            CoverUIManager.Instance.CoverUI.Hide().Forget();
            IsInitialized = true;
        }

        private void ChangeContents(ESubContentsType type)
        {
            for (var i = 0; i < contentsGroupList.Count; i++)
            {
                contentsGroupList[i].gameObject.SetActive(i == (int)type);

                if (i != (int)type)
                    bottomMenuButtonList[i].Unselect();
            }
        }

#region IMainBottomMenuHandler

        public void ShowHomeMenu()
        {
            ChangeContents(ESubContentsType.Home);
        }

        public void ShowInGameMenu()
        {
            ChangeContents(ESubContentsType.InGame);
        }

        public void ShowMissionsMenu()
        {
            ChangeContents(ESubContentsType.Mission);
        }

        public void ShowSettingsMenu()
        {
            ChangeContents(ESubContentsType.Setting);
        }

        public void ShowCollectionMenu()
        {
            ChangeContents(ESubContentsType.Collection);
        }

        public void ShowShopMenu()
        {
            ChangeContents(ESubContentsType.Shop);
        }

#endregion
    }
}
