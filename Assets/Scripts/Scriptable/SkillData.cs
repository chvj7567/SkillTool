using System;
using System.Collections.Generic;
using UnityEngine;
using static DefEnum;

[CreateAssetMenu(menuName = "Scriptable Object/Skill Data", order = 1)]
public class SkillData : ScriptableObject
{
    [Header("스킬")]
    [Tooltip("스킬 이름"), HideInInspector] public ESkill eSkill = ESkill.None;
    [Tooltip("스킬 설명"), Multiline(5), HideInInspector] public string skillDesc = null;
    [Tooltip("스킬 타겟팅 여부"), HideInInspector] public bool isTargeting = false;
    [Tooltip("스킬 채널링 여부"), HideInInspector] public bool isChanneling = false;
    [Tooltip("스킬 사정거리"), HideInInspector] public float distance = -1f;
    [Tooltip("스킬 쿨타임"), HideInInspector] public float coolTime = -1f;
    [Tooltip("스킬 비용 타입"), HideInInspector] public ESkillCost eSkillCost = ESkillCost.None;
    [Tooltip("스킬 비용"), HideInInspector] public float cost = -1f;
    [Tooltip("이펙트 정보 리스트"), HideInInspector] public List<EffectData> liEffectData = new List<EffectData>();

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
        [Header("이펙트")]
        [Tooltip("이펙트 이름"), HideInInspector] public EParticle EParticle = EParticle.None;
        [Tooltip("이펙트 각도"), HideInInspector] public float effectAngle = -1f;
        [Tooltip("이펙트 생성 위치가 시전자 위치인지 여부(자기 자신이 스킬 효과 받을 경우)"), HideInInspector] public bool createCasterPosition = false;
        [Tooltip("이펙트 시작 딜레이"), HideInInspector] public float startDelay = -1f;
        [Tooltip("타겟이 없을 경우 이펙트 생성 여부"), HideInInspector] public bool createOnEmpty = false;
        [Tooltip("이펙트 오브젝트를 타겟 자식 오브젝트로 들어갈지 여부"), HideInInspector] public bool attach = false;
        [Tooltip("이펙트 중복 여부"), HideInInspector] public bool duplication = false;
        [Tooltip("데칼 표시 여부"), HideInInspector] public bool onDecal = false;

        [Header("스킬 효과")]
        [Tooltip("적용할 스탯"), HideInInspector] public EStatModifyType eStatModifyType = EStatModifyType.None;
        [Tooltip("데미지 상태"), HideInInspector] public EDamageType1 eDamageType1 = EDamageType1.None;
        [Tooltip("데미지 타입"), HideInInspector] public EDamageType2 eDamageType2 = EDamageType2.None;
        [Tooltip("데미지"), HideInInspector] public float damage = -1f;

        [Header("스킬 대상")]
        [Tooltip("이펙트 타겟"), HideInInspector] public ESkillTarget eTarget = ESkillTarget.None;
        [Tooltip("이펙트 타겟 레이어"), HideInInspector] public ETargetMask eTargetMask = ETargetMask.None;

        [Header("스킬 시전자가 목표 지점으로 움직일 경우")]
        [Tooltip("목표 지점으로 움직임 여부"), HideInInspector] public bool moveToPos = false;
        [Tooltip("움직일 때 스피드"), HideInInspector] public float moveSpeed = -1f;
        [Tooltip("목표 지점과의 offset"), HideInInspector] public float offsetToTarget = -1f;

        [Header("충돌체")]
        [Tooltip("충돌체 모양"), HideInInspector] public ECollision eCollision = ECollision.None;
        [Tooltip("충돌체 크기"), HideInInspector] public float sphereRadius = -1f;
        [Tooltip("충돌체 크기"), HideInInspector] public Vector3 boxHalf = Vector3.zero;
        [Tooltip("충돌체 각도"), HideInInspector] public float collisionAngle = -1f;

        [Header("충돌체 설정")]
        [Tooltip("이펙트 실행 후 몇 초 뒤에 생성할지"), HideInInspector] public float triggerStartDelay = -1f;
        [Tooltip("충돌체 생성 후 몇 초간 유지할지"), HideInInspector] public float triggerStayTime = -1f;
        [Tooltip("충돌체에 닿았을 경우 스킬 적용 여부"), HideInInspector] public bool triggerEnter = false;
        [Tooltip("충돌체에 닿았다가 빠져나간 경우 스킬 적용 여부"), HideInInspector] public bool triggerExit = false;
        [Tooltip("충돌체에 닿아있는 경우 스킬 틱 적용 타임"), HideInInspector] public float stayTickTime = -1f;

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
