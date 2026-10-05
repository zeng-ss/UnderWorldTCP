using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    #region 音频数据

    public AudioClip footSound;
    public AudioClip footBackSound;
    public AudioClip weaponBackSound;
    public AudioClip weaponEndSound;
    public List<AudioClip> playerAttackSpeaks;
    public AudioClip startPinSound;
    public List<AudioClip> exSounds;

    #endregion

    private string poolName = "AudioPrefab";
    private GameObject pool;

    public void Init()
    {
        pool = new GameObject("ActiveAudio");
        AppContext.Pool.Preload("Assets/Res/Prefab/AudioPrefab", poolName, 30);
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
        AppContext.Pool.GetObj(poolName, audioPlayer =>
        {
            audioPlayer.transform.position = position;
            audioPlayer.transform.SetParent(pool.transform);
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