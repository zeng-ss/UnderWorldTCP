using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;

/// <summary>
/// 一局战斗的编排
///   进入 GameScene → 按关卡波次表刷怪 → 记录击杀与本局掉落 →
///   清完最后一波判胜 / 全员阵亡判负 → 下发 BattleResultNtf 给这局的所有玩家。
/// </summary>
public class BattleMgr : Singleton<BattleMgr>
{
    // 目前只有一张关卡表，房间的 MapId 先统一用 1
    private const int DefaultLevelId = 1;

    private class Battle
    {
        public int Key; // 房间 id；单人时是 -roleId
        public int LevelId;
        public readonly List<int> Roles = new List<int>();
        public readonly List<int> SpawnedIds = new List<int>(); // 本局刷过的所有实例，结算时清场
        public int WaveIndex; // 已经刷出的波数
        public int AliveInWave; // 当前波还活着几只
        public int Kills;
        public float Timer; // 开波 / 波间倒计时
        public bool Finished;
        public DateTime StartTime;
        public readonly Dictionary<int, int> Drops = new Dictionary<int, int>();
    }

    private readonly object _lock = new object();
    private readonly Dictionary<int, Battle> _battles = new Dictionary<int, Battle>();
    private readonly Dictionary<int, int> _roleBattle = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _enemyBattle = new Dictionary<int, int>();

    #region 进出场

    /// <summary>玩家进入战斗场景：加入（或新建）本房间的一局</summary>
    public void OnPlayerEnterGame(int roleId)
    {
        Room room = RoomMgr.Instance.GetRoomByRoleId(roleId);
        int key = room != null ? room.RoomId : -roleId;
        int waveCount;
        int memberCount;

        lock (_lock)
        {
            if (!_battles.TryGetValue(key, out Battle battle))
            {
                waveData cfg = LubanMgr.Instance.GetWaveDataById(DefaultLevelId);
                battle = new Battle
                {
                    Key = key,
                    LevelId = cfg != null ? cfg.LevelId : DefaultLevelId,
                    Timer = cfg != null ? cfg.StartDelay : 1.5f,
                    StartTime = DateTime.Now,
                };

                // 房间里的所有人都算这一局的人；单人（不在房间）就把自己当一个队
                if (room != null)
                {
                    foreach (RoomPlayerData p in room.Players) battle.Roles.Add(p.RoleId);
                }
                else
                {
                    battle.Roles.Add(roleId);
                }

                _battles[key] = battle;
            }

            if (!battle.Roles.Contains(roleId)) battle.Roles.Add(roleId);
            _roleBattle[roleId] = key;

            waveCount = LubanMgr.Instance.GetWaveDataById(battle.LevelId)?.EnemyIds.Count ?? 0;
            memberCount = battle.Roles.Count;
        }

        LogMsg.Info($"[Battle] 开局: roleId={roleId} 房间={key} 人数={memberCount} 波数={waveCount}", ConsoleColor.Yellow);
    }

    /// <summary>玩家离开战斗（退出房间 / 掉线）：从这一局里摘掉；一个人都不剩就把怪清掉</summary>
    public void OnPlayerLeave(int roleId)
    {
        int key;
        List<int> enemies = new List<int>();

        lock (_lock)
        {
            if (!_roleBattle.TryGetValue(roleId, out key)) return;
            _roleBattle.Remove(roleId);

            if (!_battles.TryGetValue(key, out Battle battle)) return;
            battle.Roles.Remove(roleId);
            if (battle.Roles.Count > 0) return;

            _battles.Remove(key);
            enemies.AddRange(battle.SpawnedIds);
            foreach (int id in battle.SpawnedIds) _enemyBattle.Remove(id);
        }

        foreach (int id in enemies) EnemyMgr.Instance.RemoveEnemy(id);
        LogMsg.Info($"[Battle] 无人剩余，结束本局: 房间={key}");
    }

    #endregion

    #region 击杀 / 掉落

    /// <summary>敌人死亡（服务端结算，和客户端上报谁打死无关）</summary>
    public void OnEnemyDead(int instanceId)
    {
        lock (_lock)
        {
            if (!_enemyBattle.TryGetValue(instanceId, out int key)) return;
            _enemyBattle.Remove(instanceId);

            if (!_battles.TryGetValue(key, out Battle battle) || battle.Finished) return;

            battle.Kills++;
            if (battle.AliveInWave > 0) battle.AliveInWave--;

            // 本波清空：给一个波间间隔，Tick 到点后刷下一波（或判胜）
            if (battle.AliveInWave <= 0)
            {
                waveData cfg = LubanMgr.Instance.GetWaveDataById(battle.LevelId);
                battle.Timer = cfg != null ? cfg.WaveInterval : 2f;
            }
        }
    }

    /// <summary>本局累计掉落（结算界面「获得」用，实际发奖在击杀那一刻已经做完）</summary>
    public void AddDrops(int roleId, List<DropItem> drops)
    {
        if (drops == null || drops.Count == 0) return;

        lock (_lock)
        {
            if (!_roleBattle.TryGetValue(roleId, out int key)) return;
            if (!_battles.TryGetValue(key, out Battle battle) || battle.Finished) return;

            foreach (DropItem drop in drops)
            {
                battle.Drops[drop.ItemId] = battle.Drops.TryGetValue(drop.ItemId, out int old)
                    ? old + drop.Count
                    : drop.Count;
            }
        }
    }

    #endregion

    #region 逻辑帧

    /// <summary>由登录服固定帧驱动：开波 / 波间等待 / 胜负判定</summary>
    public void Tick(float dt)
    {
        List<Battle> toSpawn = new List<Battle>();
        List<Battle> toWin = new List<Battle>();
        List<Battle> toLose = new List<Battle>();

        lock (_lock)
        {
            foreach (Battle battle in _battles.Values)
            {
                if (battle.Finished) continue;

                if (AllRolesDead(battle))
                {
                    battle.Finished = true;
                    toLose.Add(battle);
                    continue;
                }

                if (battle.Timer > 0f)
                {
                    battle.Timer -= dt;
                    continue;
                }

                if (battle.AliveInWave > 0) continue;

                if (battle.WaveIndex >= WaveCount(battle))
                {
                    battle.Finished = true;
                    toWin.Add(battle);
                    continue;
                }

                toSpawn.Add(battle);
            }
        }

        foreach (Battle battle in toLose) Finish(battle, false);
        foreach (Battle battle in toWin) Finish(battle, true);
        foreach (Battle battle in toSpawn) SpawnNextWave(battle);
    }

    private static int WaveCount(Battle battle)
    {
        waveData cfg = LubanMgr.Instance.GetWaveDataById(battle.LevelId);
        if (cfg == null) return 0;
        return Math.Min(cfg.EnemyIds.Count, cfg.Counts.Count);
    }

    private static bool AllRolesDead(Battle battle)
    {
        Dictionary<int, PlayerSceneInfo> players = PlayerSceneMgr.Instance.GetAllPlayers();
        bool any = false;

        foreach (int roleId in battle.Roles)
        {
            if (!players.TryGetValue(roleId, out PlayerSceneInfo info)) continue; // 已经离开场景的不算
            any = true;
            if (info.CurrHp > 0f) return false;
        }

        return any;
    }

    private void SpawnNextWave(Battle battle)
    {
        waveData cfg = LubanMgr.Instance.GetWaveDataById(battle.LevelId);
        if (cfg == null) return;

        int index;
        lock (_lock) index = battle.WaveIndex;
        if (index >= cfg.EnemyIds.Count || index >= cfg.Counts.Count) return;

        int enemyId = cfg.EnemyIds[index];
        int count = Math.Max(1, cfg.Counts[index]);
        GetSpawnCenter(battle, out float cx, out float cy, out float cz);

        List<int> spawned = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            // 围着玩家摊成一圈，避免全叠在一个点上
            float angle = (float)(Math.PI * 2 * i / count);
            float x = cx + (float)Math.Cos(angle) * cfg.SpawnRadius;
            float z = cz + (float)Math.Sin(angle) * cfg.SpawnRadius;
            spawned.Add(EnemyMgr.Instance.SpawnEnemy(enemyId, 0f, x, cy, z));
        }

        lock (_lock)
        {
            foreach (int id in spawned)
            {
                _enemyBattle[id] = battle.Key;
                battle.SpawnedIds.Add(id);
            }

            battle.WaveIndex++;
            battle.AliveInWave = spawned.Count;
            battle.Timer = 0f;
        }

        LogMsg.Info($"[Battle] 第 {index + 1}/{WaveCount(battle)} 波: enemyId={enemyId} ×{count}", ConsoleColor.Yellow);
    }

    // 取这只队伍的刷怪中心：第一个还在场上的活人脚下；都倒了就退回世界原点
    private static void GetSpawnCenter(Battle battle, out float x, out float y, out float z)
    {
        foreach (int roleId in battle.Roles)
        {
            if (PlayerSceneMgr.Instance.TryGetPlayerPos(roleId, out x, out y, out z)) return;
        }

        x = 0f;
        y = 0f;
        z = 0f;
    }

    #endregion

    #region 结算

    private void Finish(Battle battle, bool win)
    {
        int duration = Math.Max(0, (int)(DateTime.Now - battle.StartTime).TotalSeconds);
        string desc = BuildDropDesc(battle.Drops);
        int kills = battle.Kills;

        List<int> roles = new List<int>(battle.Roles);
        List<int> enemies = new List<int>();

        lock (_lock)
        {
            enemies.AddRange(battle.SpawnedIds);
            foreach (int id in battle.SpawnedIds) _enemyBattle.Remove(id);

            _battles.Remove(battle.Key);
            foreach (int roleId in roles)
            {
                if (_roleBattle.TryGetValue(roleId, out int key) && key == battle.Key) _roleBattle.Remove(roleId);
            }
        }

        // 清场：胜负都已定，场上不该再留着怪
        foreach (int id in enemies) EnemyMgr.Instance.RemoveEnemy(id);

        BattleResultNtf ntf = new BattleResultNtf
        {
            Result = win ? 1 : 0,
            DurationSec = duration,
            Kills = kills,
            RewardDesc = desc,
        };

        BasePackage pkg = new BasePackage
        {
            ProtoCode = NetDefine.CMD_BattleResultCode,
            Data = ntf.ToByteString(),
        };

        Dictionary<int, PlayerSceneInfo> players = PlayerSceneMgr.Instance.GetAllPlayers();
        foreach (int roleId in roles)
        {
            if (players.TryGetValue(roleId, out PlayerSceneInfo info))
            {
                SessionMgr.Instance.GetSession(info.SessionId)?.SendData(pkg);
            }
        }

        LogMsg.Info($"[Battle] 结算: {(win ? "胜利" : "失败")} 用时={duration}s 击杀={kills} 获得={desc}", ConsoleColor.Yellow);
    }

    // 本局掉落拼成一行：金币×40  攻击驱动×2（材料名走 Luban 表，登录服也有这张表）
    private static string BuildDropDesc(Dictionary<int, int> drops)
    {
        string desc = string.Empty;
        foreach (var kv in drops)
        {
            materialData material = LubanMgr.Instance.GetMaterialById(kv.Key);
            string name = material != null ? material.Name : kv.Key.ToString();

            if (desc.Length > 0) desc += "  ";
            desc += name + "×" + kv.Value;
        }

        return desc;
    }

    #endregion
}