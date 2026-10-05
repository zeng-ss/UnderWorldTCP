using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能连招子系统：连招配置切换、攻击段 / 特效段位管理、技能起手与远端特效生成
/// （原 PlayerCtrl 的技能相关 region）。
/// 依赖：共享状态（PlayerCore）、表现层与联网同步（均为单向依赖）。
/// </summary>
public class PlayerSkillCombo
{
    private readonly PlayerCore _core;
    private readonly PlayerPresentation _presentation;
    private readonly PlayerNetworkSync _network;

    private int _curAttackIndex;
    private int _curVFXIndex;
    private int _curSkillIndex;

    public int CurVFXIndex
    {
        get => _curVFXIndex;
        private set
        {
            if (CurAttackIndex == -1 || CurSkillConfig == null || CurSkillConfig.skillConfigs.Count <= CurAttackIndex)
            {
                _curVFXIndex = 0;
                return;
            }

            _curVFXIndex = value >= CurSkillConfig.skillConfigs[CurAttackIndex].vfxDataList.Count ? 0 : value;
        }
    }

    public int CurAttackIndex
    {
        get => _curAttackIndex;
        set => _curAttackIndex = value >= CurSkillConfig.skillConfigs.Count ? 0 : value;
    }

    public SkillConfig CurSkillConfig { get; private set; }
    public List<SkillConfig> SkillConfigList { get; } = new();
    public bool CanSwitchSkill { get; private set; }

    /// <summary>动画同步要带上当前连招索引</summary>
    public int CurSkillIndex => _curSkillIndex;

    public PlayerSkillCombo(PlayerCore core, PlayerPresentation presentation, PlayerNetworkSync network)
    {
        _core = core;
        _presentation = presentation;
        _network = network;
    }

    /// <summary>Awake 阶段调用：默认第一套连招</summary>
    public void InitDefault()
    {
        if (SkillConfigList is { Count: > 0 })
        {
            CurSkillConfig = SkillConfigList[0];
        }
    }

    // 技能连招配置切换
    public void UpdateSkillConfig(int skillIndex, bool isPin = false)
    {
        if (skillIndex < 0 || skillIndex >= SkillConfigList.Count)
        {
            Debug.LogWarning($"切换连招索引越界：{skillIndex}，默认切回第一套");
            skillIndex = 0;
        }

        _curSkillIndex = skillIndex;
        CurAttackIndex = isPin ? 1 : 0;
        CurVFXIndex = isPin ? 1 : 0;
        CurSkillConfig = SkillConfigList[skillIndex];
    }

    public void StartSkill(AttackData attackData)
    {
        CurVFXIndex = 0;
        CanSwitchSkill = false;
        // 原 PlayerCtrl.PlayAnimation：表现 + 联网两段
        _presentation.PlayAnimation(attackData.attackAnimationName, 0.1f);
        _network.SyncAnimation(attackData.attackAnimationName, _curSkillIndex);
        _presentation.PlaySound(attackData.vfxDataList[CurVFXIndex].vfxClip);
    }

    public void StartSkillHit(int weaponIndex)
    {
        if (CurAttackIndex == -1) CurAttackIndex = 0;
        if (CurVFXIndex < 0 || CurVFXIndex >= CurSkillConfig.skillConfigs[CurAttackIndex].vfxDataList.Count)
            CurVFXIndex = 0;
        _presentation.SpawnSkillVfx(CurSkillConfig.skillConfigs[CurAttackIndex].vfxDataList[CurVFXIndex]);

        if (_core.IsLocalPlayer)
        {
            _network.SyncVfx(_core.PlayerDataId, _curSkillIndex, CurAttackIndex, _curVFXIndex);
        }
    }

    public void SpawnRemoteVfx(int skillConfigIndex, int attackIndex, int vfxIndex)
    {
        if (skillConfigIndex < 0 || skillConfigIndex >= SkillConfigList.Count) return;
        var config = SkillConfigList[skillConfigIndex];
        if (attackIndex < 0 || attackIndex >= config.skillConfigs.Count) return;
        var vfxList = config.skillConfigs[attackIndex].vfxDataList;
        if (vfxIndex < 0 || vfxIndex >= vfxList.Count) return;
        _presentation.SpawnSkillVfx(vfxList[vfxIndex]);
    }

    public void StopSkillHit(int weaponIndex)
    {
        CurVFXIndex++;
    }

    public void SkillCanSwitch()
    {
        CanSwitchSkill = true;
    }
}