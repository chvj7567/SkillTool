using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Level Data", order = 2)]
public class LevelData : ScriptableObject
{
    [Header("레벨")]
    [Tooltip("레벨 수"), HideInInspector] public DefEnum.ELevel eLevel = DefEnum.ELevel.Level1;
    [Tooltip("레벨 설명"), Multiline(5), HideInInspector] public string levelDesc = null;

    [Header("레벨 유닛 효과")]
    [Tooltip("추가 최대 HP"), HideInInspector] public float maxHp = 0f;
    [Tooltip("추가 HP 초당 회복량"), HideInInspector] public float hpRegenPerSecond = 0f;
    [Tooltip("추가 최대 MP"), HideInInspector] public float maxMp = 0f;
    [Tooltip("추가 MP 초당 회복량"), HideInInspector] public float mpRegenPerSecond = 0f;
    [Tooltip("추가 공격력"), HideInInspector] public float attackPower = 0f;
    [Tooltip("추가 방어력"), HideInInspector] public float defensePower = 0f;
    [Tooltip("추가 움직일 때 스피드"), HideInInspector] public float moveSpeed = 0f;
    [Tooltip("추가 회전할 때 스피드"), HideInInspector] public float rotateSpeed = 0f;
    [Tooltip("추가 타겟이 없을 경우 타겟 감지 범위"), HideInInspector] public float range = 0f;
    [Tooltip("추가 타겟이 있을 경우 확장되는 감지 배수 범위"), HideInInspector] public float rangeMulti = 0f;
    [Tooltip("추가 타겟 감지 각도"), HideInInspector] public float viewAngle = 0f;

    [Header("레벨 스킬 효과")]
    [Tooltip("추가 스킬 사정거리"), HideInInspector] public float distance = 0f;
    [Tooltip("추가 스킬 쿨타임"), HideInInspector] public float coolTime = 0f;
    [Tooltip("추가 스킬 데미지"), HideInInspector] public float damage = 0f;

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
