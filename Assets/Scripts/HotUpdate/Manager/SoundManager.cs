using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    #region 音频数据

    public AudioClip FootSound;
    public AudioClip FootBackSound;
    public AudioClip WeaponBackSound;
    public AudioClip WeaponEndSound;
    public List<AudioClip> PlayerAttackSpeaks;
    public AudioClip StartPinSound;
    public List<AudioClip> ExSounds;

    #endregion

    private string _poolName = "AudioPrefab";
    private GameObject _pool;

    public void Init()
    {
        _pool = new GameObject("ActiveAudio");
        AppContext.Pool.Preload("AudioPrefab", _poolName, 30);
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
        AppContext.Pool.GetObj(_poolName, audioPlayer =>
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