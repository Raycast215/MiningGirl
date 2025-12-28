using System;
using Cysharp.Threading.Tasks;
using Manager;
using Sound;
using UnityEngine;

namespace Pool
{
    // Scripted by Raycast
    // 2025.01.17
    // Sound Pool의 오브젝트를 포함하는 클래스.
    public class SoundPool : PoolBase<SoundController>
    {
        public event Action OnReleased = delegate {  }; 
        
        [SerializeField] private AudioSource source;
        
        /// 받아온 clip으로 Sound를 재생하는 함수입니다. 
        public async UniTaskVoid PlaySound(AudioClip clip, float volume)
        {
            gameObject.SetActive(true);
            
            // clip 설정.
            source.clip = clip;
            // Volume 설정.
            source.volume = volume;
            // Sound 재생.
            source.Play();

            await UniTask.WaitForSeconds(source.clip.length, cancellationToken: Cts.Token);
            
            if (Cts is null || Cts.IsCancellationRequested)
                return;
            
            Release();
        }

#region Override Methods

        protected override void Release()
        {
            // Clip 초기화.
            source.clip = null;
            
            OnReleased?.Invoke();
            OnReleased = null;
            
            // Pool 해제.
            SoundManager.Instance.ReleaseSfx(this);
        }

#endregion
    }
}