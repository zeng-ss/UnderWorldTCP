using System;
using System.Collections;
using DamageNumbersPro;
using DG.Tweening;
using HotUpdate.Controller;
using HotUpdate.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Player
{
    // 玩家表现层：技能特效、命中特效、音效、屏幕反馈（震屏 / 色差 / 暗角）、伤害飘字。
    public class PlayerPresentation : MonoBehaviour
    {
        private PlayerModel _model;
        private DamageNumber _damageNumber;
        private Transform _followTarget;
        private AudioSource _audioSource;
        private PlayerCameraCtrl _camera;

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
            ResolveVolumeEffects();
        }

        public void Bind(PlayerCameraCtrl camera)
        {
            _camera = camera;
        }

        /// <summary>注入特效 → 所有者的绑定委托</summary>
        public void BindVfxOwner(Action<ParticleCtrl> initVfxOwner) => _initVfxOwner = initVfxOwner;

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
            Shake(hitData.screenImpulseValue, hitData.shakeDuration);

            if (_chromaticAberration != null && _vignette != null)
            {
                DOTween.To(() => _chromaticAberration.intensity.value,
                        x => _chromaticAberration.intensity.value = x,
                        hitData.chromaticAberrationValue, 0.2f)
                    .OnComplete(() => { _chromaticAberration.intensity.value = 0; });
            }

            AppContext.Sound.PlaySound(hitData.hitClip, worldPos);
        }

        public void PlayHurtFeedback(float damageValue, float vignetteValue, float screenImpulseValue,
            float shakeDuration = 0f)
        {
            Vector3 worldPos = _followTarget != null ? _followTarget.position : transform.position;
            if (_damageNumber != null) _damageNumber.Spawn(worldPos, damageValue);

            Shake(screenImpulseValue, shakeDuration);
            PlayVignette(vignetteValue);
        }

        // 命中 / 受击的震屏。相机由自研的 PlayerCameraCtrl 驱动，没注入进来时静默跳过。
        private void Shake(float strength, float duration = 0f)
        {
            if (_camera == null) return;
            _camera.Shake(strength, duration);
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
}