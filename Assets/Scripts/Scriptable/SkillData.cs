using System;
using System.Collections.Generic;
using UnityEngine;
using static DefEnum;

[CreateAssetMenu(menuName = "Scriptable Object/Skill Data", order = 1)]
public class SkillData : ScriptableObject
{
    [Header("��ų")]
    [Tooltip("��ų �̸�"), HideInInspector] public ESkill eSkill = ESkill.None;
    [Tooltip("��ų ����"), Multiline(5), HideInInspector] public string skillDesc = null;
    [Tooltip("��ų Ÿ���� ����"), HideInInspector] public bool isTargeting = false;
    [Tooltip("��ų ä�θ� ����"), HideInInspector] public bool isChanneling = false;
    [Tooltip("��ų �����Ÿ�"), HideInInspector] public float distance = -1f;
    [Tooltip("��ų ��Ÿ��"), HideInInspector] public float coolTime = -1f;
    [Tooltip("��ų ��� Ÿ��"), HideInInspector] public ESkillCost eSkillCost = ESkillCost.None;
    [Tooltip("��ų ���"), HideInInspector] public float cost = -1f;
    [Tooltip("����Ʈ ���� ����Ʈ"), HideInInspector] public List<EffectData> liEffectData = new List<EffectData>();

    public SkillData Clone()
    {
        SkillData clone = ScriptableObject.CreateInstance<SkillData>();
        clone.eSkill = this.eSkill;
        clone.skillDesc = this.skillDesc;
        clone.isTargeting = this.isTargeting;
        clone.distance = this.distance;
        clone.coolTime = this.coolTime;
        clone.eSkillCost = this.eSkillCost;
        clone.cost = this.cost;
        clone.liEffectData = new List<EffectData>();

        foreach (EffectData effectData in this.liEffectData)
        {
            clone.liEffectData.Add(effectData.Clone());
        }

        return clone;
    }

    [Serializable]
    public class EffectData
    {
        [Header("����Ʈ")]
        [Tooltip("����Ʈ �̸�"), HideInInspector] public EParticle EParticle = EParticle.None;
        [Tooltip("����Ʈ ����"), HideInInspector] public float effectAngle = -1f;
        [Tooltip("����Ʈ ���� ��ġ�� ������ ��ġ���� ����(�ڱ� �ڽ��� ��ų ȿ�� ���� ���)"), HideInInspector] public bool createCasterPosition = false;
        [Tooltip("����Ʈ ���� ������"), HideInInspector] public float startDelay = -1f;
        [Tooltip("Ÿ���� ���� ��� ����Ʈ ���� ����"), HideInInspector] public bool createOnEmpty = false;
        [Tooltip("����Ʈ ������Ʈ�� Ÿ�� �ڽ� ������Ʈ�� ���� ����"), HideInInspector] public bool attach = false;
        [Tooltip("����Ʈ �ߺ� ����"), HideInInspector] public bool duplication = false;
        [Tooltip("��Į ǥ�� ����"), HideInInspector] public bool onDecal = false;

        [Header("��ų ȿ��")]
        [Tooltip("������ ����"), HideInInspector] public EStatModifyType eStatModifyType = EStatModifyType.None;
        [Tooltip("������ ����"), HideInInspector] public EDamageType1 eDamageType1 = EDamageType1.None;
        [Tooltip("������ Ÿ��"), HideInInspector] public EDamageType2 eDamageType2 = EDamageType2.None;
        [Tooltip("������"), HideInInspector] public float damage = -1f;

        [Header("��ų ���")]
        [Tooltip("����Ʈ Ÿ��"), HideInInspector] public ESkillTarget eTarget = ESkillTarget.None;
        [Tooltip("����Ʈ Ÿ�� ���̾�"), HideInInspector] public ETargetMask eTargetMask = ETargetMask.None;

        [Header("��ų �����ڰ� ��ǥ �������� ������ ���")]
        [Tooltip("��ǥ �������� ������ ����"), HideInInspector] public bool moveToPos = false;
        [Tooltip("������ �� ���ǵ�"), HideInInspector] public float moveSpeed = -1f;
        [Tooltip("��ǥ �������� offset"), HideInInspector] public float offsetToTarget = -1f;

        [Header("�浹ü")]
        [Tooltip("�浹ü ���"), HideInInspector] public ECollision eCollision = ECollision.None;
        [Tooltip("�浹ü ũ��"), HideInInspector] public float sphereRadius = -1f;
        [Tooltip("�浹ü ũ��"), HideInInspector] public Vector3 boxHalf = Vector3.zero;
        [Tooltip("�浹ü ����"), HideInInspector] public float collisionAngle = -1f;

        [Header("�浹ü ����")]
        [Tooltip("����Ʈ ���� �� �� �� �ڿ� ��������"), HideInInspector] public float triggerStartDelay = -1f;
        [Tooltip("�浹ü ���� �� �� �ʰ� ��������"), HideInInspector] public float triggerStayTime = -1f;
        [Tooltip("�浹ü�� ����� ��� ��ų ���� ����"), HideInInspector] public bool triggerEnter = false;
        [Tooltip("�浹ü�� ��Ҵٰ� �������� ��� ��ų ���� ����"), HideInInspector] public bool triggerExit = false;
        [Tooltip("�浹ü�� ����ִ� ��� ��ų ƽ ���� Ÿ��"), HideInInspector] public float stayTickTime = -1f;

        public EffectData Clone()
        {
            EffectData clone = new EffectData();
            clone.EParticle = this.EParticle;
            clone.effectAngle = this.effectAngle;
            clone.createCasterPosition = this.createCasterPosition;
            clone.startDelay = this.startDelay;
            clone.createOnEmpty = this.createOnEmpty;
            clone.attach = this.attach;
            clone.duplication = this.duplication;
            clone.onDecal = this.onDecal;
            clone.eStatModifyType = this.eStatModifyType;
            clone.eDamageType1 = this.eDamageType1;
            clone.eDamageType2 = this.eDamageType2;
            clone.damage = this.damage;
            clone.eTarget = this.eTarget;
            clone.eTargetMask = this.eTargetMask;
            clone.moveToPos = this.moveToPos;
            clone.moveSpeed = this.moveSpeed;
            clone.offsetToTarget = this.offsetToTarget;
            clone.eCollision = this.eCollision;
            clone.collisionAngle = this.collisionAngle;
            clone.sphereRadius = this.sphereRadius;
            clone.boxHalf = this.boxHalf;
            clone.triggerStartDelay = this.triggerStartDelay;
            clone.triggerStayTime = this.triggerStayTime;
            clone.triggerEnter = this.triggerEnter;
            clone.triggerExit = this.triggerExit;
            clone.stayTickTime = this.stayTickTime;
            return clone;
        }
    }

}
