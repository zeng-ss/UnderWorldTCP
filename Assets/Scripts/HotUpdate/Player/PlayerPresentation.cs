using System;
using System.Collections;
using DamageNumbersPro;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 玩家表现层：技能特效、命中特效、音效、屏幕反馈（震屏 / 色差 / 暗角）、伤害飘字。
// 由控制器在 Awake 里运行时 AddComponent 挂载（不需要改预制体），
// Init 注入数据后，控制器直调这里的公开方法 ——
// 发布者明确知道唯一接收者，直调即可，不走事件总线广播。
// 本类不持有控制器：需要的数据（动画模型、飘字组件、跟随 Transform）全部由 Init 注入。
// 唯一的接缝是技能特效上可能挂的 ParticleCtrl 需要回调所有者发起命中判定，
// 通过 BindVfxOwner 注入委托完成，本类不认识具体的所有者类型。
// 内部实现（协程、DOTween、Cinemachine、后处理）与表现行为重构前完全一致。
public class PlayerPresentation : MonoBehaviour
{
    private PlayerModel _model;
    private DamageNumber _damageNumber;
    private Transform _followTarget;
    private AudioSource _audioSource;
    private CinemachineImpulseSource _impulseSource;

    private ChromaticAberration _chromaticAberration;
    private Vignette _vignette;
    private Tweener _vignetteTweener;

    // 特效生成后把所有者接进 ParticleCtrl 的委托（控制器装配时绑定）
    private Action<ParticleCtrl> _initVfxOwner;

    public void Init(PlayerModel model, DamageNumber damageNumber, Transform followTarget)
    {
        _model = model;
        _damageNumber = damageNumber;
        _followTarget = followTarget;
        _audioSource = GetComponent<AudioSource>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        ResolveVolumeEffects();
    }

    /// <summary>注入特效 → 所有者的绑定委托</summary>
    public void BindVfxOwner(Action<ParticleCtrl> initVfxOwner)
    {
        _initVfxOwner = initVfxOwner;
    }

    // 从场景里的 Volume 取出后处理组件
    private void ResolveVolumeEffects()
    {
        var volume = GameObject.Find("Volume")?.GetComponent<Volume>();
        if (volume == null) return;

        volume.profile.TryGet(out _chromaticAberration);
        volume.profile.TryGet(out _vignette);
    }

    #region 表现指令（控制器直调）

    public void PlayAnimation(string animationName, float transitionTime)
    {
        if (_model == null) return;

        _model.Animator.CrossFadeInFixedTime(animationName, transitionTime, 0, 0f);
    }

    public void PlaySound(AudioClip clip)
    {
        if (_audioSource == null || clip == null) return;

        _audioSource.PlayOneShot(clip);
    }

    public void SpawnSkillVfx(VFXData vfxData)
    {
        if (vfxData == null || vfxData.prefab == null) return;

        StartCoroutine(DoSpawnVfx(vfxData));
    }

    public void PlayHitEffects(HitData hitData, int vfxIndex, Vector3 worldPos)
    {
        if (hitData == null) return;

        SpawnHitPrefab(hitData, vfxIndex, worldPos);

        _impulseSource?.GenerateImpulse(hitData.screenImpulseValue);

        if (_chromaticAberration != null && _vignette != null)
        {
            DOTween.To(() => _chromaticAberration.intensity.value,
                    x => _chromaticAberration.intensity.value = x,
                    hitData.chromaticAberrationValue, 0.2f)
                .OnComplete(() => { _chromaticAberration.intensity.value = 0; });
        }

        AppContext.Sound.PlaySound(hitData.hitClip, worldPos);
    }

    public void PlayHurtFeedback(float damageValue, float vignetteValue, float screenImpulseValue)
    {
        Vector3 worldPos = _followTarget != null ? _followTarget.position : transform.position;
        if (_damageNumber != null) _damageNumber.Spawn(worldPos, damageValue);

        _impulseSource?.GenerateImpulse(screenImpulseValue);
        PlayVignette(vignetteValue);
    }

    #endregion

    #region 表现实现

    private IEnumerator DoSpawnVfx(VFXData vfxData)
    {
        yield return new WaitForSeconds(vfxData.spawnTime);

        Vector3 worldPos = _model.transform.TransformPoint(vfxData.spawnPos);
        Quaternion worldRot = _model.transform.rotation * Quaternion.Euler(vfxData.spawnRot);

        var vfxObj = Instantiate(vfxData.prefab);
        var particle = vfxObj.GetComponent<ParticleCtrl>();
        if (particle != null && _initVfxOwner != null) _initVfxOwner(particle);
        vfxObj.transform.position = worldPos;
        vfxObj.transform.rotation = worldRot;
        vfxObj.transform.localScale += vfxData.spawnScale;

        Destroy(vfxObj, vfxObj.TryGetComponent<ParticleSystem>(out var ps) ? ps.main.duration : 2f);
    }

    private void SpawnHitPrefab(HitData hitData, int vfxIndex, Vector3 worldPos)
    {
        if (hitData.hitPrefabs == null || vfxIndex < 0 || vfxIndex >= hitData.hitPrefabs.Count) return;

        var prefab = hitData.hitPrefabs[vfxIndex];
        if (prefab == null) return;

        var obj = Instantiate(prefab);
        obj.transform.position = worldPos;
        Destroy(obj, obj.TryGetComponent<ParticleSystem>(out var ps) ? ps.main.duration : 2f);
    }

    private void PlayVignette(float vignetteValue)
    {
        if (_vignette == null) return;

        if (_vignetteTweener != null && _vignetteTweener.IsActive()) _vignetteTweener.Kill();

        _vignetteTweener = DOTween.To(() => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                vignetteValue, 0.2f)
            .OnComplete(() =>
            {
                DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, 0, 0.3f);
            });
    }

    #endregion
}
