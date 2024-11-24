using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Item Data", order = 3)]
public class ItemData : ScriptableObject
{
    [Header("������")]
    [Tooltip("������ �̸�"), HideInInspector] public DefEnum.EItem eItem = DefEnum.EItem.None;
    [Tooltip("������ ����"), Multiline(5), HideInInspector] public string itemDesc = null;

    [Header("������ ���� ȿ��")]
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

    [Header("������ ��ų ȿ��")]
    [Tooltip("�߰� ��ų �����Ÿ�"), HideInInspector] public float distance = 0f;
    [Tooltip("�߰� ��ų ��Ÿ��"), HideInInspector] public float coolTime = 0f;
    [Tooltip("�߰� ��ų ������"), HideInInspector] public float damage = 0f;

    public ItemData Clone()
    {
        ItemData clone = ScriptableObject.CreateInstance<ItemData>();
        clone.eItem = this.eItem;
        clone.itemDesc = this.itemDesc;
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
