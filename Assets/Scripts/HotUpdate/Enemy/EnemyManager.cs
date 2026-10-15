using System.Collections.Generic;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Enemy
{
    // 客户端敌人管理器：「服务端实例ID → EnemyCtrl」这张表的唯一持有者与访问入口。
    // 敌人由服务端驱动，客户端只做三件事：落地出生数据、把服务端下发的状态转给对应 EnemyCtrl、
    // 把服务端结算出的受击/命中结果转成表现。别处一律通过 AppContext.Enemy 按 serverInstanceId 取敌人。
    public class EnemyManager
    {
        private readonly Dictionary<int, EnemyCtrl> _enemies = new();

        // 被击中者的命中点高度（胸口）。服务端只下发「命中了 + 伤害」，不下发命中坐标。
        private const float HitPointHeight = 1f;

        public void Init()
        {
            AppContext.Proto.OnEnemyPositionSyncReceived += OnEnemyPositionSync;
            AppContext.Proto.OnEnemySyncAniReceived += OnSyncEnemyAni;
            AppContext.Proto.OnPlayerAttackBroadcast += OnPlayerAttackBroadcast;
        }

        public void Clear()
        {
            if (AppContext.IsAlive)
            {
                AppContext.Proto.OnEnemyPositionSyncReceived -= OnEnemyPositionSync;
                AppContext.Proto.OnEnemySyncAniReceived -= OnSyncEnemyAni;
                AppContext.Proto.OnPlayerAttackBroadcast -= OnPlayerAttackBroadcast;
            }

            _enemies.Clear();
        }

        #region 注册 / 查找

        // 敌人落地出生数据时注册（EnemyCtrl.InitFromServer）
        public void Register(EnemyCtrl enemy)
        {
            if (enemy == null || enemy.serverInstanceId <= 0) return;
            _enemies[enemy.serverInstanceId] = enemy;
        }

        // 注销。只有表里存的还是它本人时才移除，避免同ID的新敌人被旧对象的销毁回调误删。
        public void Unregister(EnemyCtrl enemy)
        {
            if (enemy == null) return;
            if (_enemies.TryGetValue(enemy.serverInstanceId, out EnemyCtrl cur) && cur == enemy)
            {
                _enemies.Remove(enemy.serverInstanceId);
            }
        }

        // 按服务端实例ID查找（位置同步 / 动作同步 / 受击 / 命中表现都走这里）
        public bool TryGet(int enemyInstanceId, out EnemyCtrl enemy) => _enemies.TryGetValue(enemyInstanceId, out enemy);

        #endregion

        #region 生成 / 同步

        // 服务端下发敌人生成（请求者走回调、其他人走广播，最终都落到这里）
        public void SpawnEnemy(SpawnEnemyRet ret)
        {
            if (ret.CmdCode != CmdCode.Succeed)
            {
                Debug.LogError("[EnemyManager]生成敌人失败：" + ret.CmdCode);
                return;
            }

            InstantiateEnemy(ret.EnemyInstanceId, ret.MaxHp, ret.CurrHp, new Vector3(ret.PosX, ret.PosY, ret.PosZ));
        }

        /// <summary>
        /// 离线调试用：本地直接落地一只敌人。没有服务端驱动，它不会移动也不会攻击，
        /// 但可以被打（伤害由 PlayerCombat 在离线时本地结算），用来试手感 / 试相机。
        /// </summary>
        public void SpawnLocal(int instanceId, float maxHp, Vector3 pos)
        {
            InstantiateEnemy(instanceId, maxHp, maxHp, pos);
        }

        private void InstantiateEnemy(int instanceId, float maxHp, float currHp, Vector3 spawnPos)
        {
            AppContext.Res.LoadAndInstantiateAsync("enemy", null, enemy =>
            {
                EnemyCtrl ctrl = enemy.GetComponent<EnemyCtrl>();
                if (ctrl == null)
                {
                    Debug.LogError("[EnemyManager] enemy 预制体上缺少 EnemyCtrl");
                    return;
                }

                // 敌人由服务端驱动，客户端只落地出生数据，之后位置/动作都等服务端下发
                ctrl.InitFromServer(instanceId, maxHp, currHp, spawnPos);
            });
        }

        // 位置同步（服务端权威）：更新插值目标
        private void OnEnemyPositionSync(EnemyPositionSyncRet ret)
        {
            if (_enemies.TryGetValue(ret.EnemyInstanceId, out EnemyCtrl enemy))
            {
                enemy.ApplyServerPosition(ret.PosX, ret.PosY, ret.PosZ, ret.RotationY);
            }
        }

        // 动作 / 状态同步（服务端权威）：客户端只负责播
        private void OnSyncEnemyAni(EnemySyncAniRet ret)
        {
            if (_enemies.TryGetValue(ret.EnemyInstanceId, out EnemyCtrl enemy))
            {
                enemy.ApplyServerAnim(ret.AnimationName, ret.State);
            }
        }

        // 别人的攻击广播 → 同步敌人血量 / 受击 / 死亡
        private void OnPlayerAttackBroadcast(PlayerAttackRet ret)
        {
            if (_enemies.TryGetValue(ret.EnemyInstanceId, out EnemyCtrl enemy))
            {
                enemy.OnServerAttackResult(ret);
            }
        }

        #endregion

        #region 表现

        // 服务端命中广播的统一落地：按实例ID找敌人，在被击中者位置上方（胸口高度）播命中特效 / 音效。
        // 敌人可能已被打死并注销，取不到就什么都不做。
        public void PlayHitFeedback(int enemyInstanceId, Vector3 targetPosition)
        {
            if (!_enemies.TryGetValue(enemyInstanceId, out EnemyCtrl enemy)) return;
            enemy.PlayHitFeedback(targetPosition + Vector3.up * HitPointHeight);
        }

        #endregion
    }
}