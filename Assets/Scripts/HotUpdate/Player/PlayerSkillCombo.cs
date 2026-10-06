using System.Collections.Generic;
using UnityEngine;

// 技能连招子系统：连招配置切换、攻击段 / 特效段位管理、技能起手与远端特效生成。
// 配置来源：由 PlayerCtrl 在 Inspector 的 skillConfigList 上赋值后注入，
// 本类不负责加载、也不持有加载逻辑。
// 依赖：共享状态（PlayerCore）、表现层（单向依赖）；联网同步直接调 AppContext.Proto。
public class PlayerSkillCombo
{
    private readonly PlayerCore _core;
    private readonly PlayerPresentation _presentation;
    private readonly List<SkillConfig> _skillConfigList;

    private int _curAttackIndex;
    private int _curVFXIndex;
    private int _curSkillIndex;

    public int CurVFXIndex
    {
        get => _curVFXIndex;
        private set
        {
            AttackData cur = CurrentAttackData;
            if (CurAttackIndex == -1 || cur == null || cur.vfxDataList == null)
            {
                _curVFXIndex = 0;
                return;
            }

            _curVFXIndex = value >= cur.vfxDataList.Count ? 0 : value;
        }
    }

    public int CurAttackIndex
    {
        get => _curAttackIndex;
        set
        {
            SkillConfig config = CurSkillConfig;
            // 配置缺失时不做越界判断（避免 NRE），原样写入
            _curAttackIndex = config != null && value >= config.Count ? 0 : value;
        }
    }

    public SkillConfig CurSkillConfig { get; private set; }
    public bool CanSwitchSkill { get; private set; }

    // 当前段（表 + 段位）对应的攻击数据；配置缺失或越界时为 null
    public AttackData CurrentAttackData
    {
        get
        {
            SkillConfig config = CurSkillConfig;
            if (config == null) return null;
            if (_curAttackIndex < 0) return null;
            return config.GetAttackData(_curAttackIndex);
        }
    }

    // 动画同步要带上当前连招索引
    public int CurSkillIndex => _curSkillIndex;

    public PlayerSkillCombo(PlayerCore core, PlayerPresentation presentation, List<SkillConfig> skillConfigList)
    {
        _core = core;
        _presentation = presentation;
        _skillConfigList = skillConfigList ?? new List<SkillConfig>();
    }

    /// <summary>装配阶段调用：默认切到第一套连招（普攻表）</summary>
    public void InitDefault()
    {
        SkillConfig normal = GetSkillConfig(ComboSet.Normal);
        if (normal == null)
        {
            Debug.LogError("[PlayerSkillCombo] 连招配置表为空：请在 Character 预制体的 PlayerCtrl 上给 skillConfigList 赋值");
            return;
        }

        CurSkillConfig = normal;
        _curSkillIndex = (int)ComboSet.Normal;
    }

    #region 配置访问

    public SkillConfig GetSkillConfig(ComboSet set) => GetSkillConfig((int)set);

    private SkillConfig GetSkillConfig(int index)
    {
        if (index < 0 || index >= _skillConfigList.Count) return null;
        return _skillConfigList[index];
    }

    /// <summary>当前连招表是不是指定的那一套</summary>
    public bool IsCurrent(ComboSet set)
    {
        SkillConfig config = GetSkillConfig(set);
        return config != null && config == CurSkillConfig;
    }

    #endregion

    // 技能连招配置切换
    public void UpdateSkillConfig(ComboSet set, bool isPin = false)
    {
        if (_skillConfigList.Count == 0) return;

        int skillIndex = (int)set;
        if (skillIndex < 0 || skillIndex >= _skillConfigList.Count)
        {
            Debug.LogWarning($"切换连招索引越界：{skillIndex}，默认切回第一套");
            skillIndex = 0;
        }

        _curSkillIndex = skillIndex;
        CurAttackIndex = isPin ? 1 : 0;
        CurVFXIndex = isPin ? 1 : 0;
        CurSkillConfig = _skillConfigList[skillIndex];
    }

    public void StartSkill()
    {
        if (CurrentAttackData == null) return;

        CurVFXIndex = 0;
        CanSwitchSkill = false;
        _presentation.PlayAnimation(CurrentAttackData.attackAnimationName, 0.1f);
        AppContext.Proto.RequestSyncAni(AppContext.Session.RoleId, CurrentAttackData.attackAnimationName,
            _curSkillIndex, ret => AppContext.RemotePlayer.OnSyncAni(ret));

        if (CurrentAttackData.vfxDataList is { Count: > 0 })
        {
            _presentation.PlaySound(CurrentAttackData.vfxDataList[CurVFXIndex].vfxClip);
        }
    }

    /// <summary>起手当前配置表的指定段</summary>
    public void StartSkillByIndex(int index)
    {
        if (CurSkillConfig?.GetAttackData(index) == null) return;
        CurAttackIndex = index;
        StartSkill();
    }

    public void StartSkillHit()
    {
        if (CurAttackIndex == -1) CurAttackIndex = 0;

        AttackData data = CurrentAttackData;
        if (data?.vfxDataList == null || data.vfxDataList.Count == 0) return;

        if (CurVFXIndex < 0 || CurVFXIndex >= data.vfxDataList.Count) CurVFXIndex = 0;
        _presentation.SpawnSkillVfx(data.vfxDataList[CurVFXIndex]);

        if (_core.IsLocalPlayer)
        {
            AppContext.Proto.RequestSyncVfx(new PlayerVfxNtf
            {
                RoleId = _core.PlayerDataId,
                SkillConfigIndex = _curSkillIndex,
                AttackIndex = CurAttackIndex,
                VfxIndex = _curVFXIndex
            });
        }
    }

    public void SpawnRemoteVfx(int skillConfigIndex, int attackIndex, int vfxIndex)
    {
        var config = GetSkillConfig(skillConfigIndex);
        var vfxList = config?.GetAttackData(attackIndex)?.vfxDataList;
        if (vfxList == null || vfxIndex < 0 || vfxIndex >= vfxList.Count) return;
        _presentation.SpawnSkillVfx(vfxList[vfxIndex]);
    }

    public void StopSkillHit() => CurVFXIndex++;

    public void SkillCanSwitch() => CanSwitchSkill = true;
}
