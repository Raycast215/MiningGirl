using UnityEngine;

namespace System
{
    public enum EScreenMode
    {
        Portrait,
        Landscape,
    }
    
    public class CameraController : GameMonoInitializer
    {
        [SerializeField] 
        private Camera cam;

        [SerializeField] 
        private int projectionSizePortrait = 27;
        [SerializeField]
        private int projectionSizeLandScape = 14;

        private bool _isChanged;
        private EScreenMode _screenMode;

        private void Start()
        {
            UpdateScreenMode();
        }

        private void LateUpdate()
        {
            var isLandscape = Screen.width > Screen.height;

            switch (isLandscape)
            {
                case true when _screenMode == EScreenMode.Portrait:
                case false when _screenMode == EScreenMode.Landscape:
                    _isChanged = true;
                    break;
            }

            if (_isChanged)
                UpdateScreenMode();
        }

        private void UpdateScreenMode()
        {
            var width = Screen.width;
            var height = Screen.height;

            Debug.Log("Update!");
            
            if (width > height)
            {
                _screenMode = EScreenMode.Landscape;
                cam.orthographicSize = projectionSizeLandScape;
            }
            else
            {
                _screenMode = EScreenMode.Portrait;
                cam.orthographicSize = projectionSizePortrait;
            }

            _isChanged = false;
        }
    }
}