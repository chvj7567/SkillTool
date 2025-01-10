using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;

public interface IUnitInfo
{
    public int UnitID { get; }
    public DefEnum.EUnit UnitType { get; }
    public DefEnum.EStandardAxis StandardAxis { get; }
    public DefEnum.ESkill Skill1Type { get; }

    public bool IsIdle { get; }
    public bool IsDie { get; }
    public bool IsAirborne { get; }
    public bool IsOnNavMesh { get; }
    public int Layer { get; }

    public Transform transform { get; }
    public DefClass.TargetInfo Target { get; }

    public void SetAgentSpeed(float speed);
    public void SetAgentStoppingDistance(float distance);
    public void SetAgentAngularSpeed(float angularSpeed);

    public float GetCurrentMaxHp();
    public float GetCurrentHpRegenPerSecond();
    public float GetCurrentMaxMp();
    public float GetCurrentMpRegenPerSecond();
    public float GetCurrentAttackPower();
    public float GetCurrentDefensePower();
    public float GetCurrentMoveSpeed();
    public float GetCurrentRotateSpeed();
    public float GetCurrentRange();
    public float GetCurrentRangeMulti();
    public float GetCurrentViewAngle();
    public float GetCurrentHp();
    public float GetCurrentMp();
    public float GetPlusDamage();
    public float GetCurrentSkill1Distance();
}

public interface IUnitGauge
{
    public void SetHPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
    public void SetMPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
    public void SetCTGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
}

public interface IUnitAnim
{
    public void LookAtPosition(Vector3 destPos);
    public void SetAttackAnim();
    public void SetSightAnim(bool sight);
    public float GetSkillAnimTime(DefEnum.EAnim eAnim);
    public void Move(Vector3 destPos);
    public void Stop();
}

public class CHUnit : MonoBehaviour, IUnitInfo, IUnitGauge, IUnitAnim
{
    [Serializable]
    private class CharacterInfo
    {
        public int unitID;
        public DefEnum.EUnitState unitState;
        public float maxHp;
        public float maxMp;
        public float curHp;
        public float curMp;
        public float bonusHp;
        public float bonusMp;

        public UnitData unitData;
        public LevelData levelData;
    }

    [Serializable]
    private class CharacterFunction
    {
        public CHTargetTracker targetTracker;
        public CHSkill skill;
    }

    [Serializable]
    private class CharacterController
    {
        public NavMeshAgent agent;
        public Animator animator;
        public Collider collider;
        public List<string> liAnimName = new List<string>
        {
            "Idle_Guard_AR",
            "Shoot_SingleShot_AR",
            "Run_guard_AR",
            "Die"
        };

        public Dictionary<string, float> dicAnimTime = new Dictionary<string, float>();
    }

    #region Parameter
    [Header("캐릭터 정보"), SerializeField, ReadOnly]
    private CharacterInfo _characterInfo;

    [Header("캐릭터 컨트롤러"), SerializeField]
    private CharacterController _characterController;

    [Header("캐릭터 기능"), SerializeField]
    private CharacterFunction _characterFunction;

    private CHGaugeBar _gaugeBarHP;
    private CHGaugeBar _gaugeBarMP;
    private CHGaugeBar _gaugeBarCT;
    
    CancellationTokenSource _cancleTokenSource;
    Sequence _seqAirborn;
    IDisposable _disposePerSecond;

    bool _initialize = false;
    #endregion

    #region Property
    public DefEnum.EStandardAxis StandardAxis
    {
        get
        {
            return _characterFunction.targetTracker.StandardAxis;
        }
    }

    public LayerMask TargetMask
    {
        get
        {
            return _characterFunction.targetTracker.TargetMask;
        }
        set
        {
            _characterFunction.targetTracker.SetTargetMask(value);
        }
    }

    public LayerMask IgnoreMask
    {
        get
        {
            return _characterFunction.targetTracker.IgnoreMask;
        }
    }

    public DefEnum.EUnit UnitType { get; set; }
    public bool ShowHp { get; set; }
    public bool ShowMp { get; set; }
    public bool ShowCoolTime { get; set; }
    public int Layer
    {
        get
        {
            return gameObject.layer;
        }
        set
        {
            gameObject.layer = value;
        }
    }
    #endregion

    private void Awake()
    {
        _characterController.agent = GetComponent<NavMeshAgent>();
        _characterController.animator = GetComponent<Animator>();
        _characterController.collider = GetComponent<Collider>();
    }

    private void OnDisable()
    {
        if (_seqAirborn != null && _seqAirborn.IsComplete() == false)
        {
            _seqAirborn.Complete();
        }

        _seqAirborn = null;
        _disposePerSecond.Dispose();
        _initialize = false;
    }

    private void Update()
    {
        _characterFunction.skill.OnUpdate();
        _characterFunction.targetTracker.OnUpdate();
    }

    private void OnDestroy()
    {
        if (_cancleTokenSource != null && _cancleTokenSource.IsCancellationRequested == false)
        {
            _cancleTokenSource.Cancel();
        }
    }

    private void OnDrawGizmos()
    {
        _characterFunction.targetTracker.OnDrawGizmos();
    }

    #region Initialize
    public void Init(int unitID)
    {
        if (_initialize)
            return;

        _initialize = true;
        _cancleTokenSource = new CancellationTokenSource();

        _characterInfo.unitID = unitID;
        InitUnitData();
        InitGaugeBar(ShowHp, ShowMp, ShowCoolTime);

        _disposePerSecond = gameObject.UpdateAsObservable()
            .ThrottleFirst(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                if (IsDie)
                    return;

                //# 초당 Hp/Mp 회복 적용
                ChangeHp(DefEnum.ESkill.None, this, _characterInfo.unitData.hpRegenPerSecond, DefEnum.EDamageType1.None);
                ChangeMp(DefEnum.ESkill.None, this, _characterInfo.unitData.mpRegenPerSecond, DefEnum.EDamageType1.None);
            });
    }

    void InitUnitData()
    {
        RuntimeAnimatorController ac = _characterController.animator.runtimeAnimatorController;

        foreach (AnimationClip clip in ac.animationClips)
        {
            if (_characterController.dicAnimTime.TryGetValue(clip.name, out var time) == false)
                _characterController.dicAnimTime.Add(clip.name, clip.length);
        }

        _characterInfo.unitState = DefEnum.EUnitState.Idle;
        _characterInfo.maxHp = _characterInfo.bonusHp;
        _characterInfo.maxMp = _characterInfo.bonusMp;
        _characterInfo.curHp = _characterInfo.bonusHp;
        _characterInfo.curMp = _characterInfo.bonusMp;
        _characterController.collider.enabled = true;

        if (UnitType == DefEnum.EUnit.None)
        {
            UnitType = (DefEnum.EUnit)UnityEngine.Random.Range(1, (int)DefEnum.EUnit.White);
        }

        _characterInfo.unitData = CHMUnit.Instance.GetUnitData(UnitType);

        if (_characterInfo.unitData == null)
        {
            Debug.Log($"{UnitType} UnitData is null");
        }
        else
        {
            SetAgentSpeed(GetCurrentMoveSpeed());
            SetAgentAngularSpeed(GetCurrentRotateSpeed());

            _characterInfo.maxHp += _characterInfo.unitData.maxHp;
            _characterInfo.maxMp += _characterInfo.unitData.maxMp;
            _characterInfo.curHp += _characterInfo.unitData.maxHp;
            _characterInfo.curMp += _characterInfo.unitData.maxMp;

            CHMUnit.Instance.SetUnit(this, UnitType);

            _characterFunction.skill.Init(_cancleTokenSource, this, this, this);
            _characterFunction.targetTracker.Init(this, this);

            _characterInfo.levelData = CHMLevel.Instance.GetLevelData(UnitType, _characterInfo.unitData.eLevel);

            if (_characterInfo.levelData != null)
            {
                _characterInfo.maxHp += _characterInfo.levelData.maxHp;
                _characterInfo.maxMp += _characterInfo.levelData.maxMp;
                _characterInfo.curHp += _characterInfo.levelData.maxHp;
                _characterInfo.curMp += _characterInfo.levelData.maxMp;
            }
        }
    }

    void InitGaugeBar(bool onHpBar, bool onMpBar, bool onCoolTimeBar)
    {
        if (onHpBar && _gaugeBarHP == null)
        {
            CHMResource.Instance.InstantiateMajor(DefEnum.EMajor.GaugeBar, ((gaugeBar) =>
            {
                if (gaugeBar)
                {
                    gaugeBar.name = "HpBar";
                    gaugeBar.transform.SetParent(base.transform);

                    _gaugeBarHP = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarHP)
                    {
                        _gaugeBarHP.Init(_characterController.collider.bounds.size.y / 2f / base.transform.localScale.x, 2.3f);
                        _gaugeBarHP.SetGaugeBar(1, 1, 0);
                    }
                }
            }));
        }
        else
        {
            if (_gaugeBarHP != null)
                _gaugeBarHP.ResetGaugeBar();
        }

        if (onMpBar && _gaugeBarMP == null)
        {
            CHMResource.Instance.InstantiateMajor(DefEnum.EMajor.GaugeBar, ((gaugeBar) =>
            {
                if (gaugeBar)
                {
                    gaugeBar.name = "MpBar";
                    gaugeBar.transform.SetParent(base.transform);

                    _gaugeBarMP = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarMP)
                    {
                        _gaugeBarMP.Init(_characterController.collider.bounds.size.y / 2f / base.transform.localScale.x, 1.7f);
                        _gaugeBarMP.SetGaugeBar(1, 1, 0f);
                    }
                }
            }));
        }
        else
        {
            if (_gaugeBarMP != null)
                _gaugeBarMP.ResetGaugeBar();
        }

        if (onCoolTimeBar && _gaugeBarCT == null)
        {
            CHMResource.Instance.InstantiateMajor(DefEnum.EMajor.GaugeBar, ((gaugeBar) =>
            {
                if (gaugeBar)
                {
                    gaugeBar.name = "CoolTimeBar";
                    gaugeBar.transform.SetParent(base.transform);

                    _gaugeBarCT = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarCT)
                    {
                        _gaugeBarCT.Init(_characterController.collider.bounds.size.y / 2f / base.transform.localScale.x, -2f);
                        _gaugeBarCT.SetGaugeBar(1, 1, 0f);
                    }
                }
            }));
        }
        else
        {
            if (_gaugeBarCT != null)
                _gaugeBarCT.ResetGaugeBar();
        }
    }
    #endregion

    #region Getter
    public int UnitID => _characterInfo.unitID;
    public DefClass.TargetInfo Target => _characterFunction.targetTracker.GetClosestTargetInfo();
    public bool IsIdle => _characterInfo.unitState == DefEnum.EUnitState.Idle;
    public bool IsRun => _characterInfo.unitState == DefEnum.EUnitState.Run;
    public bool IsDie => _characterInfo.unitState == DefEnum.EUnitState.Die;
    public bool IsAirborne => _characterInfo.unitState == DefEnum.EUnitState.Airborne;
    public bool IsOnNavMesh => _characterController.agent.isOnNavMesh;

    public DefEnum.ESkill Skill1Type => _characterInfo.unitData.eSkill1;

    public int AttackRange
    {
        get
        {
            return Animator.StringToHash("AttackRange");
        }
    }

    public int SightRange
    {
        get
        {
            return Animator.StringToHash("SightRange");
        }
    }

    public int Die
    {
        get
        {
            return Animator.StringToHash("Die");
        }
    }

    public float GetCurrentMaxHp()
    {
        float maxHp = 0f;

        maxHp += _characterInfo.unitData.maxHp;

        return maxHp;
    }
    public float GetCurrentHpRegenPerSecond()
    {
        float hpRegenPerSecond = 0f;

        hpRegenPerSecond += _characterInfo.unitData.hpRegenPerSecond;

        return hpRegenPerSecond;
    }
    public float GetCurrentMaxMp()
    {
        float maxMp = 0f;

        maxMp += _characterInfo.unitData.maxMp;

        return maxMp;
    }
    public float GetCurrentMpRegenPerSecond()
    {
        float mpRegenPerSecond = 0f;

        mpRegenPerSecond += _characterInfo.unitData.mpRegenPerSecond;

        return mpRegenPerSecond;
    }
    public float GetCurrentAttackPower()
    {
        float attackPower = 0f;

        attackPower += _characterInfo.unitData.attackPower;

        return attackPower;
    }
    public float GetCurrentDefensePower()
    {
        float defensePower = 0f;

        defensePower += _characterInfo.unitData.defensePower;

        return defensePower;
    }
    public float GetCurrentMoveSpeed()
    {
        float moveSpeed = 0f;

        moveSpeed += _characterInfo.unitData.moveSpeed;

        return moveSpeed;
    }
    public float GetCurrentRotateSpeed()
    {
        float rotateSpeed = 0f;

        rotateSpeed += _characterInfo.unitData.rotateSpeed;

        return rotateSpeed;
    }
    public float GetCurrentRange()
    {
        float range = 0f;

        range += _characterInfo.unitData.range;

        return range;
    }
    public float GetCurrentRangeMulti()
    {
        float rangeMulti = 0f;

        rangeMulti += _characterInfo.unitData.rangeMulti;

        return rangeMulti;
    }
    public float GetCurrentViewAngle()
    {
        float viewAngle = 0f;

        viewAngle += _characterInfo.unitData.viewAngle;

        return viewAngle;
    }
    public float GetCurrentHp()
    {
        return _characterInfo.curHp;
    }
    public float GetCurrentMp()
    {
        return _characterInfo.curMp;
    }
    public float GetPlusDamage()
    {
        return _characterInfo.levelData.damage;
    }
    public float GetCurrentSkill1Distance()
    {
        return _characterFunction.skill.Skill1Distance;
    }
    #endregion

    public void SetSightAnim(bool sight)
    {
        _characterController.animator.SetBool(SightRange, sight);
    }

    public void SetAttackAnim()
    {
        _characterController.animator.SetTrigger(AttackRange);
    }

    public float GetSkillAnimTime(DefEnum.EAnim eAnim)
    {
        return _characterController.dicAnimTime[_characterController.liAnimName[(int)eAnim]];
    }

    public void LookAtPosition(Vector3 destPos)
    {
        var posTarget = destPos;
        var posMy = transform.position;

        posTarget.y = 0f;
        posMy.y = 0f;

        switch (StandardAxis)
        {
            case DefEnum.EStandardAxis.X:
                {
                    transform.right = posTarget - posMy;
                }
                break;
            case DefEnum.EStandardAxis.Z:
                {
                    transform.forward = posTarget - posMy;
                }
                break;
        }
    }

    public void SetAgentSpeed(float speed)
    {
        _characterController.agent.speed = speed;
    }

    public void SetAgentStoppingDistance(float distance)
    {
        _characterController.agent.stoppingDistance = distance;
    }

    public void SetAgentAngularSpeed(float angularSpeed)
    {
        _characterController.agent.angularSpeed = angularSpeed;
    }

    public void Move(Vector3 destPos)
    {
        if (IsIdle == false || IsDie)
            return;

        _characterController.animator.SetBool(SightRange, true);

        if (IsOnNavMesh)
        {
            _characterController.agent.SetDestination(destPos);
        }
    }

    public void Stop()
    {
        _characterController.agent.velocity = Vector3.zero;

        _characterController.animator.SetBool(SightRange, false);

        if (IsOnNavMesh)
        {
            _characterController.agent.ResetPath();
        }
    }

    public void SetAirborne(bool isAirborne, DG.Tweening.Sequence sequence = null)
    {
        _seqAirborn = sequence;

        if (isAirborne)
        {
            _characterInfo.unitState = DefEnum.EUnitState.Airborne;
        }
        else
        {
            _characterInfo.unitState = DefEnum.EUnitState.Idle;
        }
    }

    public void SetHPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true)
    {
        _gaugeBarHP?.SetGaugeBar(maxValue, curValue, damage, backGaugeTime, gaugeTime, viewDamage);
    }

    public void SetMPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true)
    {
        _gaugeBarMP?.SetGaugeBar(maxValue, curValue, damage, backGaugeTime, gaugeTime, viewDamage);
    }

    public void SetCTGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true)
    {
        _gaugeBarCT?.SetGaugeBar(maxValue, curValue, damage, backGaugeTime, gaugeTime, viewDamage);
    }

    public void ChangeHp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDie == false)
        {
            switch (eDamageType1)
            {
                case DefEnum.EDamageType1.AtOnce:
                    AtOnceChangeHp(eSkill, attackUnit, value);
                    break;
                case DefEnum.EDamageType1.Continuous_1Sec_3Count:
                    ContinuousChangeHp(eSkill, attackUnit, 1f, 3, value);
                    break;
                case DefEnum.EDamageType1.Continuous_Dot1Sec_10Count:
                    ContinuousChangeHp(eSkill, attackUnit, .1f, 10, value);
                    break;
                default:
                    AtOnceChangeHp(eSkill, attackUnit, value);
                    break;
            }
        }
    }

    public void ChangeMp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDie == false)
        {
            switch (eDamageType1)
            {
                case DefEnum.EDamageType1.AtOnce:
                    AtOnceChangeMp(eSkill, attackUnit, value);
                    break;
                case DefEnum.EDamageType1.Continuous_1Sec_3Count:
                    ContinuousChangeMp(eSkill, attackUnit, 1f, 3, value);
                    break;
                default:
                    AtOnceChangeMp(eSkill, attackUnit, value);
                    break;
            }
        }
    }

    public void ChangeAttackPower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDie == false)
        {
            switch (eDamageType1)
            {
                case DefEnum.EDamageType1.AtOnce:
                    AtOnceChangeAttackPower(eSkill, attackUnit, value);
                    break;
                case DefEnum.EDamageType1.Continuous_1Sec_3Count:
                    ContinuousChangeAttackPower(eSkill, attackUnit, 1f, 3, value);
                    break;
                default:
                    AtOnceChangeAttackPower(eSkill, attackUnit, value);
                    break;
            }
        }
    }

    public void ChangeDefensePower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDie == false)
        {
            switch (eDamageType1)
            {
                case DefEnum.EDamageType1.AtOnce:
                    AtOnceChangeDefensePower(eSkill, attackUnit, value);
                    break;
                case DefEnum.EDamageType1.Continuous_1Sec_3Count:
                    ContinuousChangeDefensePower(eSkill, attackUnit, 1f, 3, value);
                    break;
                default:
                    AtOnceChangeDefensePower(eSkill, attackUnit, value);
                    break;
            }
        }
    }

    public void ChangeLevel(DefEnum.ELevel eLevel)
    {
        var changeLevelData = CHMLevel.Instance.GetLevelData(UnitType, eLevel);
        if (changeLevelData == null)
            return;

        if (_characterInfo.levelData != null)
        {
            _characterInfo.maxHp += changeLevelData.maxHp - _characterInfo.levelData.maxHp;
            _characterInfo.maxMp += changeLevelData.maxMp - _characterInfo.levelData.maxMp;
            _characterInfo.curHp += changeLevelData.maxHp - _characterInfo.levelData.maxHp;
            _characterInfo.curMp += changeLevelData.maxMp - _characterInfo.levelData.maxMp;
        }
        else
        {
            _characterInfo.maxHp += changeLevelData.maxHp;
            _characterInfo.maxMp += changeLevelData.maxMp;
            _characterInfo.curHp += changeLevelData.maxHp;
            _characterInfo.curMp += changeLevelData.maxMp;
        }

        _characterInfo.levelData = changeLevelData;
    }

    void AtOnceChangeHp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        if (IsDie)
            return;

        float hpOrigin = _characterInfo.curHp;
        float hpResult = _characterInfo.curHp + value;
        if (hpResult >= _characterInfo.maxHp)
        {
            hpResult = _characterInfo.maxHp;
        }

        _characterInfo.curHp = hpResult;

        if (_gaugeBarHP)
            _gaugeBarHP.SetGaugeBar(_characterInfo.maxHp, this.GetCurrentHp(), value, 1.5f, 1f);

        if (eSkill != DefEnum.ESkill.None)
        {
            Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, Damage : {value}" +
            $"{_characterInfo.unitData.unitName}<{gameObject.name}> => Hp : {hpOrigin} -> {hpResult}");
        }

        //# 죽음 Die
        if (hpResult <= 0.00001f)
        {
            Debug.Log($"{name} is Died");

            hpResult = 0f;

            _characterController.animator.SetTrigger(AttackRange);
            _characterController.animator.SetBool(SightRange, false);
            _characterController.animator.SetTrigger(Die);

            _characterInfo.unitState = DefEnum.EUnitState.Die;

            _characterController.collider.enabled = false;

            if (_gaugeBarHP)
                _gaugeBarHP.gameObject.SetActive(false);

            //CHMResource.Instance.Destroy(gameObject, 1f);
        }
    }

    void AtOnceChangeMp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float mpOrigin = _characterInfo.curMp;
        float mpResult = _characterInfo.curMp + value;
        if (mpResult >= _characterInfo.maxMp)
        {
            mpResult = _characterInfo.maxMp;
        }
        else if (mpResult < 0)
        {
            mpResult = 0f;
        }

        _characterInfo.curMp = mpResult;

        if (_gaugeBarMP)
            _gaugeBarMP.SetGaugeBar(_characterInfo.maxMp, this.GetCurrentMp(), value, 1.5f, 1f, false);

        if (eSkill != DefEnum.ESkill.None)
        {
            Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_characterInfo.unitData.unitName}<{gameObject.name}> => Mp : {mpOrigin} -> {mpResult}");
        }
    }

    void AtOnceChangeAttackPower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float attackPowerOrigin = _characterInfo.unitData.attackPower;
        float attackPowerResult = _characterInfo.unitData.attackPower + value;
        CheckMaxStatValue(DefEnum.EStat.AttackPower, ref attackPowerResult);

        _characterInfo.unitData.attackPower = attackPowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_characterInfo.unitData.unitName}<{gameObject.name}> => AttackPower : {attackPowerOrigin} -> {attackPowerResult}");
    }

    void AtOnceChangeDefensePower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float defensePowerOrigin = _characterInfo.unitData.defensePower;
        float defensePowerResult = _characterInfo.unitData.defensePower + value;
        CheckMaxStatValue(DefEnum.EStat.DefensePower, ref defensePowerResult);

        _characterInfo.unitData.attackPower = defensePowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_characterInfo.unitData.unitName}<{gameObject.name}> => DefensePower : {defensePowerOrigin} -> {defensePowerResult}");
    }

    async void ContinuousChangeHp(DefEnum.ESkill eSkill, CHUnit attackUnit, float time, int count, float value)
    {
        float tickTime = time / (count - 1);

        for (int i = 0; i < count; ++i)
        {
            AtOnceChangeHp(eSkill, attackUnit, value);

            if (i == count - 1)
            {
                break;
            }

            await Task.Delay((int)(tickTime * 1000f));

            if (_cancleTokenSource.IsCancellationRequested) return;
        }
    }

    async void ContinuousChangeMp(DefEnum.ESkill eSkill, CHUnit attackUnit, float time, int count, float value)
    {
        float tickTime = time / (count - 1);

        for (int i = 0; i < count; ++i)
        {
            AtOnceChangeMp(eSkill, attackUnit, value);

            if (i == count - 1)
            {
                break;
            }

            await Task.Delay((int)(tickTime * 1000f));

            if (_cancleTokenSource.IsCancellationRequested) return;
        }
    }

    async void ContinuousChangeAttackPower(DefEnum.ESkill eSkill, CHUnit attackUnit, float time, int count, float value)
    {
        float tickTime = time / (count - 1);

        for (int i = 0; i < count; ++i)
        {
            AtOnceChangeAttackPower(eSkill, attackUnit, value);

            if (i == count - 1)
            {
                break;
            }

            await Task.Delay((int)(tickTime * 1000f));

            if (_cancleTokenSource.IsCancellationRequested) return;
        }
    }

    async void ContinuousChangeDefensePower(DefEnum.ESkill eSkill, CHUnit attackUnit, float time, int count, float value)
    {
        float tickTime = time / (count - 1);

        for (int i = 0; i < count; ++i)
        {
            AtOnceChangeDefensePower(eSkill, attackUnit, value);

            if (i == count - 1)
            {
                break;
            }

            await Task.Delay((int)(tickTime * 1000f));

            if (_cancleTokenSource.IsCancellationRequested) return;
        }
    }

    public void CheckMaxStatValue(DefEnum.EStat eStat, ref float value)
    {
        switch (eStat)
        {
            case DefEnum.EStat.Hp:
                if (value < 0f)
                {
                    value = 0f;
                }
                else if (value > 10000f)
                {
                    value = 10000f;
                }
                break;
            case DefEnum.EStat.Mp:
                if (value < 0f)
                {
                    value = 0f;
                }
                else if (value > 10000f)
                {
                    value = 10000f;
                }
                break;
            case DefEnum.EStat.AttackPower:
                if (value < 0f)
                {
                    value = 0f;
                }
                else if (value > 10000f)
                {
                    value = 10000f;
                }
                break;
            case DefEnum.EStat.DefensePower:
                if (value < 0f)
                {
                    value = 0f;
                }
                else if (value > 10000f)
                {
                    value = 10000f;
                }
                break;
            default:
                break;
        }
    }
}