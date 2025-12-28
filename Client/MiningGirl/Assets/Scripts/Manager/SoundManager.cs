using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pool;
using UnityEngine;
using Utility;

namespace Manager
{
    // Scripted by Raycast
    // 2025.01.14
    // Bgm과 Sfx를 관리하는 클래스입니다.
    
    public class SoundManager : SingletonBase<SoundManager>
    {
        private event Action<AudioClip> OnBgmPlayed = delegate {  };
        
        private readonly Dictionary<string, AudioClip> _audioClipDic = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, int> _sfxCountDic = new Dictionary<string, int>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        private AudioSource _bgmSource;
        private Pooling<SoundPool> _soundPool;

        public void PreLoadData()
        {
            InitializedSfx();
            InitializedBgm();
            Subscribed();

            IsInitialized = true;
        }
        
        /// BGM을 실행합니다.
        public void PlayBgm(string clipName, float volume = 1.0f, bool isLoop = true, bool isFade = false)
        {
            // 받아온 Clip이 비어있는 경우 예외.
            if (string.IsNullOrEmpty(clipName))
            {
                Debug.Log($"[BGM] The '{clipName}' is null or empty...");
                return;
            }
            
            // Bgm이 재생하면 실행할 이벤트 선언.
            Action<AudioClip> onBgmPlayed = ac => PlayBgmSound(ac, volume, isFade);
            
            // Bgm의 Loop 설정.
            _bgmSource.loop = isLoop;

            if (_audioClipDic.TryGetValue(clipName, out var clip))
            {
                // 동일한 Clip이 재생중인 경우 예외.
                if (_bgmSource.clip == clip)
                {
                    Debug.Log($"[BGM] The '{clipName}' is already playing...");
                    return;
                }
                    
                // Bgm 재생 이벤트 실행.
                onBgmPlayed.Invoke(clip);
            }
            else
            {
                // Bgm의 Clip이 없는 경우 Bgm 재생하면 Clip을 추가하는 이벤트 구독.
                onBgmPlayed += ac => AddAudioClip(clipName, ac);
                // Bgm의 Clip을 로드하고, 로드에 성공하면 Bgm 재생 이벤트를 실행함.
                AddressableManager.Instance.LoadAsset<AudioClip>(clipName, onBgmPlayed.Invoke).Forget();
            }
        }
        
        /// SFX를 실행합니다. 
        public void PlaySfx(string clipName, float volume = 1.0f)
        {
            if (string.IsNullOrEmpty(clipName))
                return;
            
            if (_sfxCountDic.ContainsKey(clipName) is false)
                _sfxCountDic.Add(clipName, 1);

            // 동일한 Clip이 일정 수 이상 재생중인 경우 리턴.
            if (_sfxCountDic[clipName] > 10)
                return;
            
            _sfxCountDic[clipName]++;
            
            Action<AudioClip> onSfxPlayed = null;
            var soundObject = _soundPool.Get();
            
            onSfxPlayed += _ => soundObject.OnReleased += () => _sfxCountDic[clipName]--;
            onSfxPlayed += ac => soundObject.PlaySound(ac, volume).Forget();

            if (_audioClipDic.ContainsKey(clipName))
            {
                onSfxPlayed.Invoke(_audioClipDic[clipName]);
            }
            else
            {
                onSfxPlayed += ac => AddAudioClip(clipName, ac);
                AddressableManager.Instance.LoadAsset<AudioClip>(clipName, onSfxPlayed.Invoke).Forget();
            }
        }

        /// Sfx를 Pool에 반환합니다.
        public void ReleaseSfx(SoundPool sound)
        {
            _soundPool.Return(sound);
        }

        /// AudioClip을 추가합니다.
        private void AddAudioClip(string clipName, AudioClip clip)
        {
            if (clip is null)
                return;

            if (_audioClipDic.ContainsKey(clipName))
                return;
            
            _audioClipDic.Add(clipName, clip);
        }
        
        private void PlayBgmSound(AudioClip clipAsset, float volume, bool isFade)
        {
            var toVolume = Mathf.Clamp(volume, 0.0f, 1.0f);

            if (_bgmSource.isPlaying)
            {
                // 이전 사운드 Fade로 멈추고, 다음 사운드 Fade로 재생.
                if (isFade)
                {
                    Fade(_bgmSource, 0.0f, onFinished: Finished).Forget();
                    return;

                    void Finished()
                    {
                        _bgmSource.Stop();
                        OnBgmPlayed?.Invoke(clipAsset);
                        Fade(_bgmSource, volume).Forget();
                    }
                }

                // 이전 사운드 멈추고, 다음 사운드 재생.
                _bgmSource.Stop();
                OnBgmPlayed?.Invoke(clipAsset);
                return;
            }

            // Fade로 재생.
            if (isFade)
            {
                OnBgmPlayed?.Invoke(clipAsset);
                Fade(_bgmSource, toVolume).Forget();
                return;
            }

            // 즉시 재생.
            OnBgmPlayed?.Invoke(clipAsset);
        }
        
        private async UniTaskVoid Fade(AudioSource source, float toVolume, Action onFinished = null)
        {
            while (true)
            {
                await UniTask.Yield(cancellationToken: _cts.Token);

                if (_cts is null || _cts.IsCancellationRequested)
                    return;
                    
                // 볼륨을 목표 볼륨에 따라 조절.
                source.volume += Util.GetToggleOne(toVolume > 0.0f) * Time.deltaTime;

                // 목표 볼륨에 근사하지 않은 경우 건너뜀.
                if (Mathf.Approximately(source.volume, toVolume) is false) 
                    continue;
                    
                source.volume = toVolume;
                break;
            }
                
            onFinished?.Invoke();
        }
        
        private void InitializedBgm()
        {
            _bgmSource ??= Util.CreateObject<AudioSource>($"Bgm", transform, Vector3.zero);
            _bgmSource.volume = 1.0f;
        }
        
        private void Subscribed()
        {
            OnBgmPlayed += clip => _bgmSource.clip = clip;
            OnBgmPlayed += _ => _bgmSource.Play();
        }

        private void InitializedSfx()
        {
            var sfxObject = Resources.Load<SoundPool>("Sound/SoundObject");
            _soundPool = new Pooling<SoundPool>(sfxObject, 10, transform);
            _soundPool.Pool();
        }

#region Override Methods Implementation
        
        protected override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
         
            base.OnDestroy();
        }
        
#endregion
    }
}