using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using NotImplementedException = System.NotImplementedException;

namespace InGame.System.Result
{
    public interface IResultHandler
    {
        void OnSuccess();
        void OnFail();
        void OnRetry();
        void OnHome();
    }
    
    public class ResultController : GameInitializer, IResultHandler
    {
        [SerializeField] 
        private ResultFailed resultFailed;

        public void Initialize()
        {
            resultFailed.Initialize(this);
            IsInitialized = true;
        }
        
  #region IResultHandler

        public void OnSuccess()
        {
           
        }

        public void OnFail() 
        { 
            resultFailed.Set(30, 99);
        }

        public void OnRetry()
        {
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("InGameScene")).Forget();
        }

        public void OnHome()
        {
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("StartScene")).Forget();
        }

#endregion
    }
}