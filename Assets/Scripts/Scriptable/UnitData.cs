using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Unit Data", order = 0)]
public class UnitData : ScriptableObject
{
    [Header("���� ����")]
    [Tooltip("���� �̸�"), HideInInspector] public DefEnum.EUnit eUnit = DefEnum.EUnit.None;
    [Tooltip("���� �̸� String"), HideInInspector] public string unitName = null;
    [Tooltip("���� ����"), HideInInspector] public DefEnum.ELevel eLevel = DefEnum.ELevel.Level1;
    [Tooltip("�ִ� HP"), HideInInspector] public float maxHp = -1f;
    [Tooltip("HP �ʴ� ȸ����"), HideInInspector] public float hpRegenPerSecond = -1f;
    [Tooltip("�ִ� MP"), HideInInspector] public float maxMp = -1f;
    [Tooltip("MP �ʴ� ȸ����"), HideInInspector] public float mpRegenPerSecond = -1f;
    [Tooltip("���ݷ�"), HideInInspector] public float attackPower = -1f;
    [Tooltip("����"), HideInInspector] public float defensePower = -1f;
    [Tooltip("������ �� ���ǵ�"), HideInInspector] public float moveSpeed = -1f;
    [Tooltip("ȸ���� �� ���ǵ�"), HideInInspector] public float rotateSpeed = -1f;
    [Tooltip("Ÿ���� ���� ��� Ÿ�� ���� ����"), HideInInspector] public float range = -1f;
    [Tooltip("Ÿ���� ���� ��� Ȯ��Ǵ� ���� ��� ����"), HideInInspector] public float rangeMulti = -1f;
    [Tooltip("Ÿ�� ���� ����"), HideInInspector] public float viewAngle = -1f;

    [Header("��ų ����")]
    [Tooltip("��ų1 �̸�"), HideInInspector] public DefEnum.ESkill eSkill1 = DefEnum.ESkill.None;
    [Tooltip("��ų2 �̸�"), HideInInspector] public DefEnum.ESkill eSkill2 = DefEnum.ESkill.None;
    [Tooltip("��ų3 �̸�"), HideInInspector] public DefEnum.ESkill eSkill3 = DefEnum.ESkill.None;
    [Tooltip("��ų4 �̸�"), HideInInspector] public DefEnum.ESkill eSkill4 = DefEnum.ESkill.None;

    [Header("������ ����")]
    [Tooltip("������1 �̸�"), HideInInspector] public DefEnum.EItem eItem1 = DefEnum.EItem.None;

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
