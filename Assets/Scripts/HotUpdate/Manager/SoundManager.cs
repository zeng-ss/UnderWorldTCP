using System.Collections.Generic;
using HotUpdate.Core;
using UnityEngine;

namespace HotUpdate.Manager
{
    public class SoundManager
    {
        #region 音频数据

        public AudioClip FootSound;
        public AudioClip FootBackSound;
        public AudioClip WeaponBackSound;
        public AudioClip WeaponEndSound;
        public readonly List<AudioClip> PlayerAttackSpeaks = new();
        public AudioClip StartPinSound;
        public readonly List<AudioClip> ExSounds = new();

        #endregion

        private const string PoolName = "AudioPrefab";
        private const string SoundPath = "Audio/AnBi/";
        private const string FootPath = "Audio/FOOT/";
        private GameObject _pool;

        public void Init()
        {
            _pool = new GameObject("ActiveAudio");
            AppContext.Pool.Preload("AudioPrefab", PoolName, 30);
            StartPinSound = Resources.Load<AudioClip>("Audio/拼刀开始");
            FootSound = Resources.Load<AudioClip>(FootPath + "脚步声");
            WeaponEndSound = Resources.Load<AudioClip>(SoundPath + "安比入鞘");
            WeaponBackSound = Resources.Load<AudioClip>(SoundPath + "安比收刀");
            FootBackSound = Resources.Load<AudioClip>(FootPath + "收脚音1");
            ExSounds.Add(Resources.Load<AudioClip>(SoundPath + "安比：速清"));
            ExSounds.Add(Resources.Load<AudioClip>(SoundPath + "安比：处决"));
            ExSounds.Add(Resources.Load<AudioClip>(SoundPath + "安比：碍事"));
            ExSounds.Add(Resources.Load<AudioClip>(SoundPath + "安比：消失吧"));
            ExSounds.Add(Resources.Load<AudioClip>(SoundPath + "安比：锁定目标"));
            PlayerAttackSpeaks.Add(Resources.Load<AudioClip>(SoundPath + "安比：嘿（攻击）"));
            PlayerAttackSpeaks.Add(Resources.Load<AudioClip>(SoundPath + "安比：Ye（攻击）"));
            PlayerAttackSpeaks.Add(Resources.Load<AudioClip>(SoundPath + "安比：Ye轻（攻击）"));
        }


        /// <summary>
        /// 从池获取音效播放器并播放指定音效
        /// </summary>
        /// <param name="clip">音效片段</param>
        /// <param name="position">播放位置</param>
        /// <param name="volume">基础音量（0~1）</param>
        /// <param name="pitch">音调（默认1）</param>
        public void PlaySound(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (!clip) return;
            // 从对象池获取播放器
            AppContext.Pool.GetObj(PoolName, audioPlayer =>
            {
                audioPlayer.transform.position = position;
                audioPlayer.transform.SetParent(_pool.transform);
                var audioSource = audioPlayer.GetComponent<AudioSource>();

                // 设置音效属性
                audioSource.clip = clip;
                audioSource.volume = volume;
                audioSource.pitch = pitch;
                audioSource.Play();

                // 播放完毕后自动回收
                float clipLength = clip.length;
                AppContext.Pool.ReturnAfter(audioPlayer, clipLength);
            }, "AudioPrefab");
        }
    }
}