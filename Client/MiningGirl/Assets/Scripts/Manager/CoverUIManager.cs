using UI.CoverUI;
using UnityEngine;

namespace Manager
{
    public class CoverUIManager : SingletonBase<CoverUIManager>
    {
        public CoverUI CoverUI { get; private set; }
        
        public void PreLoadData()
        {
            var coverUIPrefab = Resources.Load<CoverUI>("CoverUI");
            
            CoverUI = Instantiate(coverUIPrefab, transform);
            CoverUI.Initialize();
            
            IsInitialized = true;
        }
    }
}