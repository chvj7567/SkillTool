using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Level Data", order = 2)]
public class LevelData : ScriptableObject
{
    [Header("����")]
    [Tooltip("���� ��"), HideInInspector] public DefEnum.ELevel eLevel = DefEnum.ELevel.Level1;
    [Tooltip("���� ����"), Multiline(5), HideInInspector] public string levelDesc = null;

    [Header("���� ���� ȿ��")]
    [Tooltip("�߰� �ִ� HP"), HideInInspector] public float maxHp = 0f;
    [Tooltip("�߰� HP �ʴ� ȸ����"), HideInInspector] public float hpRegenPerSecond = 0f;
    [Tooltip("�߰� �ִ� MP"), HideInInspector] public float maxMp = 0f;
    [Tooltip("�߰� MP �ʴ� ȸ����"), HideInInspector] public float mpRegenPerSecond = 0f;
    [Tooltip("�߰� ���ݷ�"), HideInInspector] public float attackPower = 0f;
    [Tooltip("�߰� ����"), HideInInspector] public float defensePower = 0f;
    [Tooltip("�߰� ������ �� ���ǵ�"), HideInInspector] public float moveSpeed = 0f;
    [Tooltip("�߰� ȸ���� �� ���ǵ�"), HideInInspector] public float rotateSpeed = 0f;
    [Tooltip("�߰� Ÿ���� ���� ��� Ÿ�� ���� ����"), HideInInspector] public float range = 0f;
    [Tooltip("�߰� Ÿ���� ���� ��� Ȯ��Ǵ� ���� ��� ����"), HideInInspector] public float rangeMulti = 0f;
    [Tooltip("�߰� Ÿ�� ���� ����"), HideInInspector] public float viewAngle = 0f;

    [Header("���� ��ų ȿ��")]
    [Tooltip("�߰� ��ų �����Ÿ�"), HideInInspector] public float distance = 0f;
    [Tooltip("�߰� ��ų ��Ÿ��"), HideInInspector] public float coolTime = 0f;
    [Tooltip("�߰� ��ų ������"), HideInInspector] public float damage = 0f;

    public LevelData Clone()
    {
        LevelData clone = ScriptableObject.CreateInstance<LevelData>();
        clone.eLevel = this.eLevel;
        clone.levelDesc = this.levelDesc;
        clone.attackPower = this.attackPower;
        clone.defensePower = this.defensePower;
        clone.maxHp = this.maxHp;
        clone.maxMp = this.maxMp;
        clone.hpRegenPerSecond = this.hpRegenPerSecond;
        clone.mpRegenPerSecond = this.mpRegenPerSecond;
        clone.moveSpeed = this.moveSpeed;
        clone.rotateSpeed = this.rotateSpeed;
        clone.coolTime = this.coolTime;
        clone.damage = this.damage;

        return clone;
    }
}
