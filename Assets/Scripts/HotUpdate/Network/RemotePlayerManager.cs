using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Player;
using UnityEngine;

namespace HotUpdate.Network
{
    // 远端玩家的落地与同步：位置插值、动画、VFX、受击打击表现。
    // 敌人的生成 / 位置同步 / 动作同步 / 命中表现在由 EnemyManager（AppContext.Enemy）统一负责。
    public class RemotePlayerManager
    {
        private readonly Dictionary<int, RemotePlayer> _remotePlayers = new();

        public void Init()
        {
            AppContext.Proto.OnPlayerEnterScene += OnPlayerEnterScene;
            AppContext.Proto.OnPositionSyncReceived += OnPositionSync;
            AppContext.Proto.OnPlayerLeaveScene += OnPlayerLeaveScene;
            AppContext.Proto.OnSyncAniReceived += OnSyncAni;
            AppContext.Proto.OnPlayerVfxReceived += OnPlayerVfxReceived;
            AppContext.Proto.OnPlayerHurtReceived += OnPlayerHurt;
        }

        // 别人被敌人打中：血量与死亡表现由「他本人」的客户端落地（那边他才算本地玩家），
        // 这里只补一件事 —— 在同一只敌人身上播武器命中表现，让所有人都看到这一下的打击特效。
        private void OnPlayerHurt(PlayerHurtNtf ntf)
        {
            if (!_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp) || rp == null) return;

            AppContext.Enemy.PlayHitFeedback(ntf.EnemyInstanceId, rp.transform.position);
        }

        private void OnPlayerVfxReceived(PlayerVfxNtf ntf)
        {
            if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
            {
                rp.Ctrl.SkillCombo.SpawnRemoteVfx(ntf.SkillConfigIndex, ntf.AttackIndex, ntf.VfxIndex);
            }
        }

        private void OnPlayerEnterScene(PlayerEnterSceneNtf ntf)
        {
            if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer existed))
            {
                if (existed != null) return;
                _remotePlayers.Remove(ntf.RoleId);
            }

            Vector3 pos = new Vector3(ntf.PosX, ntf.PosY, ntf.PosZ);
            AppContext.Res.LoadAndInstantiateAsync("Character", null, obj =>
            {
                // 标记为非本地玩家，禁用 CharacterController 防止与位置插值冲突
                var pc = obj.GetComponent<PlayerCtrl>();
                if (pc != null)
                {
                    pc.Core.IsLocalPlayer = false;
                    pc.CharacterController.enabled = false;
                }

                var rp = obj.AddComponent<RemotePlayer>();
                rp.Init(ntf.RoleId, ntf.Nickname, pos);
                _remotePlayers[ntf.RoleId] = rp;

                Debug.Log($"[RemotePlayerManager] 玩家进入: roleId={ntf.RoleId} nickname={ntf.Nickname}");
            });
        }

        private void OnPositionSync(PositionSyncNtf ntf)
        {
            if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
            {
                rp.targetPos = new Vector3(ntf.PosX, ntf.PosY, ntf.PosZ);
                rp.targetRotation = Quaternion.Euler(0, ntf.RotationY, 0);
            }
        }

        // 服务端广播「别人」的动画同步（请求者自己收不到回包，因此只由事件触发）
        private void OnSyncAni(SyncAniRet ret)
        {
            if (_remotePlayers.TryGetValue(ret.RoleId, out RemotePlayer rp))
            {
                // 连招序号在协议里是裸 int：先校验成合法枚举再进玩法层，非法值直接丢弃
                if (System.Enum.IsDefined(typeof(ComboSet), ret.SkillConfigIndex))
                    rp.Ctrl.SkillCombo.UpdateSkillConfig((ComboSet)ret.SkillConfigIndex);
                rp.Ctrl.playerModel.Animator.CrossFadeInFixedTime(ret.AnimationName, 0.1f, 0, 0f);
            }
        }

        private void OnPlayerLeaveScene(PlayerLeaveSceneNtf ntf)
        {
            if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
            {
                GameObject.Destroy(rp.gameObject);
                _remotePlayers.Remove(ntf.RoleId);
                Debug.Log($"[RemotePlayerManager] 玩家离开: roleId={ntf.RoleId}");
            }
        }

        /// <summary>找离 from 最近的远程玩家（宿主 AI 索敌用）</summary>
        public bool TryGetNearest(Vector3 from, out int roleId, out Vector3 pos)
        {
            roleId = 0;
            pos = Vector3.zero;
            bool found = false;
            float minSq = float.MaxValue;

            foreach (RemotePlayer rp in _remotePlayers.Values)
            {
                if (rp == null) continue;
                float sq = (rp.transform.position - from).sqrMagnitude;
                if (sq >= minSq) continue;
                minSq = sq;
                roleId = rp.RoleId;
                pos = rp.transform.position;
                found = true;
            }

            return found;
        }

        public void ResetForNewScene()
        {
            foreach (var kv in _remotePlayers)
            {
                if (kv.Value != null) GameObject.Destroy(kv.Value.gameObject);
            }

            _remotePlayers.Clear();
        }

        public void Clear()
        {
            if (!AppContext.IsAlive) return;

            AppContext.Proto.OnPlayerEnterScene -= OnPlayerEnterScene;
            AppContext.Proto.OnPositionSyncReceived -= OnPositionSync;
            AppContext.Proto.OnPlayerLeaveScene -= OnPlayerLeaveScene;
            AppContext.Proto.OnSyncAniReceived -= OnSyncAni;
            AppContext.Proto.OnPlayerVfxReceived -= OnPlayerVfxReceived;
            AppContext.Proto.OnPlayerHurtReceived -= OnPlayerHurt;
        }
    }
}
