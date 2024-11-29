
using System;

public partial class DefEnum
{
    public enum EResource
    {
        None = 0,
        Json,
        Scriptable,
        Skill,
        Unit,
        Level,
        UI,
        Item,
        Particle,
        Major,
        Decal,
    }

    public enum EAssetPiece
    {
        None = 0,
        Material
    }

    public enum EMajor
    {
        None = 0,
        GlobalVolume,
        Player,
        GaugeBar,
    }

    public enum EJson
    {
        None = 0,
        String
    }

    public enum ELayer
    {
        None = 0,
        Red = 6,
        Blue = 7,
    }

    public enum ETargetMask
    {
        None = 0,
        Me,
        MyTeam,
        Enemy,
        MyTeam_Enemy,
    }

    public enum EStandardAxis
    {
        None = 0,
        X,
        Y,
        Z
    }

    public enum ESkill
    {
        None = 0,
        Arrow1,
        Ax,
        Devil,
        Explosion,
        Heal,
        IceArrow,
        IceArrow2,
        Meteor,
        Slash,
        Tornado,
    }

    public enum EUnit
    {
        None = 0,
        White,
        Brown,
        Orange,
        Yellow,
        Blue,
        Red,
        Pink,
        Green,
    }

    public enum ELevel
    {
        Level1 = 1,
    }

    public enum EItem
    {
        None = 0,
        White
    }

    public enum EUI
    {
        None = 0,
        UICamera,
        UICanvas,
        EventSystem,
    }

    public enum ESkillTarget
    {
        None = 0,
        Position,
        Target_One,
        Target_All
    }

    // 스킬대상자 스킬형태
    public enum EStatModifyType
    {
        None = 0,

        Hp_Up,
        Hp_Down,
        Mp_Up,
        Mp_Down,
        AttackPower_Up,
        AttackPower_Down,
        DefensePower_Up,
        DefensePower_Down,
    }

    public enum EDamageType1
    {
        None = 0,
        AtOnce, // 한번에
        Continuous_1Sec_3Count, // 1초동안 3번 지속 데미지
        Continuous_Dot1Sec_10Count, // 0.1초동안 10번 지속 데미지
    }

    public enum EDamageType2
    {
        None = 0,
        Fixed, // 고정 데미지
        Percent_Me_MaxHp, // 나의 전체 Hp 퍼센트 데미지
        Percent_Me_RemainHp, // 나의 남은 Hp 퍼센트 데미지
        Percent_Target_MaxHp, // 타겟의 전체 Hp 퍼센트 데미지
        Percent_Target_RemainHp, // 타겟의 남은 Hp 퍼센트 데미지
    }

    public enum ESkillCost
    {
        None = 0,
        Fixed_HP,
        Percent_MaxHP,
        Percent_RemainHP,
        Fixed_MP,
        Percent_MaxMP,
        Percent_RemainMP,
    }

    public enum ECollision
    {
        None = 0,
        Sphere,
        Box,
    }

    public enum EParticle
    {
        None = 0,
        Slash,
        SlashHit,
        FX_Circle_meteor,
        FX_Arrow_impact,
        FX_Arrow_impact2,
        FX_Ax,
        FX_Tornado,
        FX_Explosion,
        FX_Explosion_Hit,
        FX_Arrow_impact_sub,
        FX_Circle_hit,
        FX_Circle_ring,
        FX_Curse,
        FX_Defense,
        FX_Devil,
        FX_Electricity,
        FX_Electricity_Hit,
        FX_Explosion_Magic,
        FX_Explosion_Magic2,
        FX_Fire,
        FX_Gun_Impact,
        Fx_Healing,
        FX_IceArrow_Hit,
        FX_Iceflake,
        FX_Poison,
    }

    public enum EStat
    {
        None = 0,
        Hp,
        Mp,
        AttackPower,
        DefensePower,
    }

    [Flags]
    public enum EUnitState
    {
        Normal = 0,
        IsDie = 1 << 0,
        IsAirborne = 1 << 1
    }

    public enum EDecal
    {
        None = 0,
        Round,
        Box,
    }
}
