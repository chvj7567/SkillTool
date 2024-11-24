using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Unit Data", order = 0)]
public class UnitData : ScriptableObject
{
    [Header("유닛 정보")]
    [Tooltip("유닛 이름"), HideInInspector] public DefEnum.EUnit eUnit = DefEnum.EUnit.None;
    [Tooltip("유닛 이름 String"), HideInInspector] public string unitName = null;
    [Tooltip("유닛 레벨"), HideInInspector] public DefEnum.ELevel eLevel = DefEnum.ELevel.Level1;
    [Tooltip("최대 HP"), HideInInspector] public float maxHp = -1f;
    [Tooltip("HP 초당 회복량"), HideInInspector] public float hpRegenPerSecond = -1f;
    [Tooltip("최대 MP"), HideInInspector] public float maxMp = -1f;
    [Tooltip("MP 초당 회복량"), HideInInspector] public float mpRegenPerSecond = -1f;
    [Tooltip("공격력"), HideInInspector] public float attackPower = -1f;
    [Tooltip("방어력"), HideInInspector] public float defensePower = -1f;
    [Tooltip("움직일 때 스피드"), HideInInspector] public float moveSpeed = -1f;
    [Tooltip("회전할 때 스피드"), HideInInspector] public float rotateSpeed = -1f;
    [Tooltip("타겟이 없을 경우 타겟 감지 범위"), HideInInspector] public float range = -1f;
    [Tooltip("타겟이 있을 경우 확장되는 감지 배수 범위"), HideInInspector] public float rangeMulti = -1f;
    [Tooltip("타겟 감지 각도"), HideInInspector] public float viewAngle = -1f;

    [Header("스킬 정보")]
    [Tooltip("스킬1 이름"), HideInInspector] public DefEnum.ESkill eSkill1 = DefEnum.ESkill.None;
    [Tooltip("스킬2 이름"), HideInInspector] public DefEnum.ESkill eSkill2 = DefEnum.ESkill.None;
    [Tooltip("스킬3 이름"), HideInInspector] public DefEnum.ESkill eSkill3 = DefEnum.ESkill.None;
    [Tooltip("스킬4 이름"), HideInInspector] public DefEnum.ESkill eSkill4 = DefEnum.ESkill.None;

    [Header("아이템 정보")]
    [Tooltip("아이템1 이름"), HideInInspector] public DefEnum.EItem eItem1 = DefEnum.EItem.None;

    public UnitData Clone()
    {
        UnitData newUnitInfo = new UnitData();

        newUnitInfo.eUnit = this.eUnit;
        newUnitInfo.unitName = this.unitName;
        newUnitInfo.maxHp = this.maxHp;
        newUnitInfo.hpRegenPerSecond = this.hpRegenPerSecond;
        newUnitInfo.maxMp = this.maxMp;
        newUnitInfo.mpRegenPerSecond = this.mpRegenPerSecond;
        newUnitInfo.attackPower = this.attackPower;
        newUnitInfo.defensePower = this.defensePower;
        newUnitInfo.moveSpeed = this.moveSpeed;
        newUnitInfo.rotateSpeed = this.rotateSpeed;
        newUnitInfo.range = this.range;
        newUnitInfo.rangeMulti = this.rangeMulti;
        newUnitInfo.viewAngle = this.viewAngle;
        newUnitInfo.eSkill1 = this.eSkill1;
        newUnitInfo.eSkill2 = this.eSkill2;
        newUnitInfo.eSkill3 = this.eSkill3;
        newUnitInfo.eSkill4 = this.eSkill4;
        newUnitInfo.eItem1 = this.eItem1;

        return newUnitInfo;
    }
}
