using System;
using UnityEngine;
using UnityEngine.UI;

public partial class DefClass
{
    #region Json
    [Serializable]
    public class StringData
    {
        public int stringID;
        public string value;
    }
    #endregion

    [Serializable]
    public class UnitInfo
    {
        public DefEnum.EUnit eUnit = DefEnum.EUnit.None;
        public int nameStringID = -1;
        public float maxHp = -1f;
        public float hp = -1f;
        public float hpRegenPerSecond = -1f;
        public float maxMp = -1f;
        public float mp = -1f;
        public float mpRegenPerSecond = -1f;
        public float attackPower = -1f;
        public float defensePower = -1f;
        public float attackDelay = -1f;
        public float attackDistance = -1f;
        public float moveSpeed = -1f;
        public float rotateSpeed = -1f;
        public float range = -1f;
        public float rangeMulti = -1f;
        public float viewAngle = -1f;
        public DefEnum.ESkill eSkill1ID = DefEnum.ESkill.None;
        public DefEnum.ESkill eSkill2ID = DefEnum.ESkill.None;
        public DefEnum.ESkill eSkill3ID = DefEnum.ESkill.None;
        public DefEnum.ESkill eSkill4ID = DefEnum.ESkill.None;

        public UnitInfo Clone()
        {
            UnitInfo newUnitInfo = new UnitInfo();

            newUnitInfo.eUnit = this.eUnit;
            newUnitInfo.nameStringID = this.nameStringID;
            newUnitInfo.maxHp = this.maxHp;
            newUnitInfo.hp = this.hp;
            newUnitInfo.hpRegenPerSecond = this.hpRegenPerSecond;
            newUnitInfo.maxMp = this.maxMp;
            newUnitInfo.mp = this.mp;
            newUnitInfo.mpRegenPerSecond = this.mpRegenPerSecond;
            newUnitInfo.attackPower = this.attackPower;
            newUnitInfo.defensePower = this.defensePower;
            newUnitInfo.attackDelay = this.attackDelay;
            newUnitInfo.attackDistance = this.attackDistance;
            newUnitInfo.moveSpeed = this.moveSpeed;
            newUnitInfo.rotateSpeed = this.rotateSpeed;
            newUnitInfo.range = this.range;
            newUnitInfo.rangeMulti = this.rangeMulti;
            newUnitInfo.viewAngle = this.viewAngle;
            newUnitInfo.eSkill1ID = this.eSkill1ID;
            newUnitInfo.eSkill2ID = this.eSkill2ID;
            newUnitInfo.eSkill3ID = this.eSkill3ID;
            newUnitInfo.eSkill4ID = this.eSkill4ID;

            return newUnitInfo;
        }
    }

    [Serializable]
    public class TargetInfo
    {
        public GameObject target = null;
        public float distance = -1f;
    }

    public class SkillLocationInfo
    {
        public Transform trCaster;
        public Vector3 posCaster;
        public Vector3 dirCaster;
        public Transform trTarget;
        public Vector3 posTarget;
        public Vector3 dirTarget;
        public Vector3 posSkill;
        public Vector3 dirSkill;

        public SkillLocationInfo Copy()
        {
            SkillLocationInfo copy = new SkillLocationInfo();
            copy.trCaster = this.trCaster;
            copy.posCaster = this.posCaster;
            copy.dirCaster = this.dirCaster;
            copy.trTarget = this.trTarget;
            copy.posTarget = this.posTarget;
            copy.dirTarget = this.dirTarget;
            copy.posSkill = this.posSkill;
            copy.dirSkill = this.dirSkill;
            return copy;
        }
    }
}
