using HotUpdate.Network;
using HotUpdate.Player;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 敌人与服务端之间的同步，两个方向都在这：
    ///   · 非宿主：吃服务端下发的位置 / 朝向，做插值跟随；
    ///   · 宿主：索敌（本地玩家 + 远程玩家里最近的活人），并按 10Hz 上报位置 / 朝向 / 动作。
    /// 命中判定不在这里 —— 那在武器碰撞盒（见 WeaponController）。
    /// </summary>
    public class EnemySync
    {
        private const float PositionLerpSpeed = 12f;
        private const float RotationLerpSpeed = 12f;
        private const float ReportInterval = 0.1f;
        private const float TargetRefreshInterval = 1f;

        private readonly EnemyCtrl _ctrl;
        private readonly EnemyMotor _motor;

        private Vector3 _targetPos;
        private float _targetYaw;
        private bool _hasPos;
        private string _lastAnim = "Idle";

        private float _reportTimer;
        private PlayerCtrl _localPlayer;
        private float _findTimer;

        public EnemySync(EnemyCtrl ctrl, EnemyMotor motor)
        {
            _ctrl = ctrl;
            _motor = motor;
        }

        /// <summary>宿主目标的查询结果（状态机读这几个值）</summary>
        public bool HasTarget { get; private set; }

        public Vector3 TargetPosition { get; private set; }

        public int TargetRoleId { get; private set; }

        public float DistanceToTarget { get; private set; }

        public void SetLastAnim(string animName) => _lastAnim = animName;

        /// <summary>生成 / 瞬移后把基线对齐，避免第一帧插值把人拽回去</summary>
        public void ResetBaseline(Vector3 pos)
        {
            _targetPos = pos;
            _hasPos = true;
        }

        /// <summary>服务端权威位置（非宿主才吃）</summary>
        public void ApplyPosition(float x, float y, float z, float rotationY)
        {
            if (_ctrl.IsHost) return;

            _targetPos = new Vector3(x, y, z);
            _targetYaw = rotationY;
            _hasPos = true;
        }

        /// <summary>非宿主：朝服务端坐标插值，朝向跟随服务端 Yaw</summary>
        public void Follow(float dt)
        {
            if (_ctrl.IsHost || !_hasPos || _ctrl.IsDead) return;

            Transform model = _ctrl.enemyModel.transform;
            float rk = 1f - Mathf.Exp(-RotationLerpSpeed * dt);
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.Euler(0f, _targetYaw, 0f), rk);

            Vector3 cur = _ctrl.transform.position;

            // 位移动画在这端也吃根运动，偏差小于 2 米就交给动画
            if (_ctrl.Anim(_lastAnim).useRootMotion)
            {
                float dx = _targetPos.x - cur.x;
                float dz = _targetPos.z - cur.z;
                if (dx * dx + dz * dz < 4f) return;
            }

            float k = 1f - Mathf.Exp(-PositionLerpSpeed * dt);
            Vector3 next = new Vector3(Mathf.Lerp(cur.x, _targetPos.x, k), cur.y, Mathf.Lerp(cur.z, _targetPos.z, k));

            Vector3 delta = next - cur;
            delta.y = 0f;
            _motor.Move(delta);
        }

        /// <summary>宿主：索敌，结果写进 HasTarget / TargetPosition / TargetRoleId / DistanceToTarget</summary>
        public void UpdateHostTarget(float dt)
        {
            _findTimer -= dt;
            if (_localPlayer == null || _findTimer <= 0f)
            {
                _findTimer = TargetRefreshInterval;
                _localPlayer = FindLocalPlayer();
            }

            bool found = false;
            Vector3 best = Vector3.zero;
            int bestRole = 0;
            float bestSq = float.MaxValue;
            Vector3 self = _ctrl.transform.position;

            if (_localPlayer.gameObject.activeInHierarchy && _localPlayer.Core.Health > 0f)
            {
                best = _localPlayer.transform.position;
                bestSq = (best - self).sqrMagnitude;
                bestRole = AppContext.Session.RoleId;
                found = true;
            }

            if (AppContext.RemotePlayer.TryGetNearest(self, out int roleId, out Vector3 remotePos))
            {
                float sq = (remotePos - self).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = remotePos;
                    bestRole = roleId;
                    found = true;
                }
            }

            HasTarget = found;
            TargetPosition = best;
            TargetRoleId = bestRole;
            DistanceToTarget = found ? Mathf.Sqrt(bestSq) : 0f;
        }

        /// <summary>宿主：10Hz 上报状态，服务端校验后转发给其他人</summary>
        public void Report(float dt)
        {
            _reportTimer += dt;
            if (_reportTimer < ReportInterval) return;
            _reportTimer = 0f;

            if (AppContext.IsOffline) return;

            Transform t = _ctrl.transform;
            AppContext.Proto.RequestEnemyAiSync(AppContext.Session.RoleId, _ctrl.serverInstanceId,
                t.position, t.eulerAngles.y, _ctrl.CurrentAnimName, (int)_ctrl.CurrentState);
        }

        private static PlayerCtrl FindLocalPlayer()
        {
            PlayerCtrl[] all = Object.FindObjectsByType<PlayerCtrl>();
            foreach (var pc in all)
            {
                if (!pc.Core.IsLocalPlayer) continue;
                if (pc.GetComponent<RemotePlayer>() != null) continue; // 远程玩家身上也挂着 PlayerCtrl
                return pc;
            }

            return null;
        }
    }
}