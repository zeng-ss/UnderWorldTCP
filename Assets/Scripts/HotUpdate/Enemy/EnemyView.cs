using DamageNumbersPro;
using HotUpdate.Data;
using HotUpdate.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 敌人的表现层：血条、受击动画选择、飘字、命中特效。
    /// 只做「看起来怎样」，不参与任何判定与数据修改。
    /// </summary>
    public class EnemyView
    {
        private readonly EnemyCtrl _ctrl;
        private readonly GameObject _healthBar;
        private readonly Image _fill;
        private readonly TMP_Text _healthText;
        private readonly DamageNumber _damageNumber;
        private readonly HitData _hitData;

        public EnemyView(EnemyCtrl ctrl, GameObject healthBar, Image fill, TMP_Text healthText,
            DamageNumber damageNumber, HitData hitData)
        {
            _ctrl = ctrl;
            _healthBar = healthBar;
            _fill = fill;
            _healthText = healthText;
            _damageNumber = damageNumber;
            _hitData = hitData;
        }

        public void RefreshHealth(float hp, float max)
        {
            _fill.fillAmount = Mathf.Clamp01(hp / Mathf.Max(1f, max));
            _healthText.text = $"{hp}/{max}";
        }

        // 血条始终面向相机
        public void FaceCamera()
        {
            if (_healthBar == null || Camera.main == null) return;
            _healthBar.transform.LookAt(Camera.main.transform.position);
            _healthBar.transform.Rotate(0f, 180f, 0f);
        }

        public void ShowDamage(float damage, bool isBaoji)
        {
            if (_damageNumber == null) return;

            _damageNumber.SetColor(isBaoji ? Color.red : Color.white);
            _damageNumber.Spawn(_ctrl.transform.position + Vector3.up * 1.5f, damage);
        }

        // 按攻击者方位选前/后受击动画
        public string PickHurtAnim()
        {
            PlayerCtrl player = Object.FindAnyObjectByType<PlayerCtrl>();
            if (player == null) return "HurtFront";

            Vector3 toAttacker = player.transform.position - _ctrl.transform.position;
            toAttacker.y = 0f;
            if (toAttacker.sqrMagnitude < 0.0001f) return "HurtFront";

            float dot = Vector3.Dot(_ctrl.transform.forward, toAttacker.normalized);
            return dot > 0f ? "HurtFront" : "HurtBack";
        }

        // 命中表现：只播特效与音效，不扣血
        public void PlayHitFeedback(Vector3 hurtPos)
        {
            if (_hitData == null) return;

            if (_hitData.hitPrefabs is { Count: > 0 })
            {
                GameObject obj = Object.Instantiate(_hitData.hitPrefabs[0]);
                obj.transform.position = hurtPos;
                Object.Destroy(obj, obj.GetComponent<ParticleSystem>().main.duration);
            }

            AppContext.Sound.PlaySound(_hitData.hitClip, hurtPos);
        }
    }
}