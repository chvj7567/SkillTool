using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using static DefEnum;

public interface IUnitInfo
{
    public DefEnum.EUnit UnitType { get; }
    public DefEnum.EStandardAxis StandardAxis { get; }
    public DefEnum.ESkill Skill1Type { get; }

    public bool IsNormal { get; }
    public bool IsDie { get; }
    public bool IsAirborne { get; }

    public Transform UnitTransform { get; }
    public DefClass.TargetInfo Target { get; }
}

public interface IUnitGauge
{
    public void SetHPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
    public void SetMPGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
    public void SetCTGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime, float gaugeTime, bool viewDamage = true);
}

public interface IUnitAnim
{
    public void SetAttackAnim();
    public void SetSightAnim(bool sight);
    public float GetSkillAnimTime(DefEnum.EAnim eAnim);
}

public class CHUnit : MonoBehaviour, IUnitInfo, IUnitGauge, IUnitAnim
{
    #region Parameter
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Animator _animator;
    [SerializeField] Collider _unitCollider;
    [SerializeField] CHTargetTracker _targetTracker;
    [SerializeField] List<string> _liAnimName = new List<string>();
    [SerializeField, ReadOnly] CHSkill _skill;
    [SerializeField, ReadOnly] Dictionary<string, float> _dicAnimTime = new Dictionary<string, float>();
    [SerializeField, ReadOnly] float _maxHp;
    [SerializeField, ReadOnly] float _maxMp;
    [SerializeField, ReadOnly] float _curHp;
    [SerializeField, ReadOnly] float _curMp;
    [SerializeField, ReadOnly] float _bonusHp;
    [SerializeField, ReadOnly] float _bonusMp;

    CHGaugeBar _gaugeBarHP;
    CHGaugeBar _gaugeBarMP;
    CHGaugeBar _gaugeBarCT;

    UnitData _unitData;
    LevelData _levelData;

    CancellationTokenSource _cancleTokenSource;
    Sequence _seqAirborn;
    IDisposable _disposePerSecond;
    #endregion

    #region Property
    public DefEnum.EStandardAxis StandardAxis { get; set; }
    public DefEnum.EUnit UnitType { get; set; }
    public bool ShowHp { get; set; }
    public bool ShowMp { get; set; }
    public bool ShowCoolTime { get; set; }
    public DefEnum.EUnitState MyUnitState { get; private set; }
    public Transform UnitTransform => transform;
    public DefClass.TargetInfo Target => _targetTracker.GetClosestTargetInfo();
    public bool IsNormal => MyUnitState == 0;
    public bool IsDie => (MyUnitState & DefEnum.EUnitState.IsDie) != 0;
    public bool IsAirborne => (MyUnitState & DefEnum.EUnitState.IsAirborne) != 0;
    #endregion

    void OnEnable()
    {
        Init();
    }

    void OnDisable()
    {
        if (_seqAirborn != null && _seqAirborn.IsComplete() == false)
        {
            _seqAirborn.Complete();
        }

        _seqAirborn = null;
        _disposePerSecond.Dispose();
    }

    void Awake()
    {
        _cancleTokenSource = new CancellationTokenSource();
    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        _skill.OnUpdate();
    }

    private void OnDestroy()
    {
        if (_cancleTokenSource != null && _cancleTokenSource.IsCancellationRequested == false)
        {
            _cancleTokenSource.Cancel();
        }
    }

    #region Initialize
    void Init()
    {
        InitUnitData();
        _targetTracker.SetValue(this);
        InitGaugeBar(ShowHp, ShowMp, ShowCoolTime);

        _disposePerSecond = gameObject.UpdateAsObservable()
            .ThrottleFirst(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                if (IsDie)
                    return;

                //# 초당 Hp/Mp 회복 적용
                ChangeHp(DefEnum.ESkill.None, this, _unitData.hpRegenPerSecond, DefEnum.EDamageType1.None);
                ChangeMp(DefEnum.ESkill.None, this, _unitData.mpRegenPerSecond, DefEnum.EDamageType1.None);
            });
    }

    void InitUnitData()
    {
        RuntimeAnimatorController ac = _animator.runtimeAnimatorController;

        foreach (AnimationClip clip in ac.animationClips)
        {
            if (_dicAnimTime.TryGetValue(clip.name, out var time) == false)
                _dicAnimTime.Add(clip.name, clip.length);
        }

        MyUnitState = 0;
        _maxHp = _bonusHp;
        _maxMp = _bonusMp;
        _curHp = _bonusHp;
        _curMp = _bonusMp;
        _unitCollider.enabled = true;

        if (UnitType == DefEnum.EUnit.None)
        {
            UnitType = (DefEnum.EUnit)UnityEngine.Random.Range(1, (int)DefEnum.EUnit.White);
        }

        _unitData = CHMUnit.Instance.GetUnitData(UnitType);

        if (_unitData == null)
        {
            Debug.Log($"{UnitType} UnitData is null");
        }
        else
        {
            _maxHp += _unitData.maxHp;
            _maxMp += _unitData.maxMp;
            _curHp += _unitData.maxHp;
            _curMp += _unitData.maxMp;

            CHMUnit.Instance.SetUnit(gameObject, UnitType);

            _skill.Init(_cancleTokenSource, this, this, this);

            _levelData = CHMLevel.Instance.GetLevelData(UnitType, _unitData.eLevel);

            if (_levelData != null)
            {
                _maxHp += _levelData.maxHp;
                _maxMp += _levelData.maxMp;
                _curHp += _levelData.maxHp;
                _curMp += _levelData.maxMp;
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
                    gaugeBar.transform.SetParent(transform);

                    _gaugeBarHP = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarHP)
                    {
                        if (_unitCollider == null)
                        {
                            _unitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarHP.Init(this, _unitCollider.bounds.size.y / 2f / transform.localScale.x, 2.3f);
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
                    gaugeBar.transform.SetParent(transform);

                    _gaugeBarMP = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarMP)
                    {
                        if (_unitCollider == null)
                        {
                            _unitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarMP.Init(this, _unitCollider.bounds.size.y / 2f / transform.localScale.x, 1.7f);
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
                    gaugeBar.transform.SetParent(transform);

                    _gaugeBarCT = gaugeBar.GetComponent<CHGaugeBar>();
                    if (_gaugeBarCT)
                    {
                        if (_unitCollider == null)
                        {
                            _unitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarCT.Init(this, _unitCollider.bounds.size.y / 2f / transform.localScale.x, -2f);
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
    public bool IsOnNavMesh => _agent.isOnNavMesh;

    public DefEnum.ESkill Skill1Type => _unitData.eSkill1;

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

        maxHp += _unitData.maxHp;

        return maxHp;
    }
    public float GetCurrentHpRegenPerSecond()
    {
        float hpRegenPerSecond = 0f;

        hpRegenPerSecond += _unitData.hpRegenPerSecond;

        return hpRegenPerSecond;
    }
    public float GetCurrentMaxMp()
    {
        float maxMp = 0f;

        maxMp += _unitData.maxMp;

        return maxMp;
    }
    public float GetCurrentMpRegenPerSecond()
    {
        float mpRegenPerSecond = 0f;

        mpRegenPerSecond += _unitData.mpRegenPerSecond;

        return mpRegenPerSecond;
    }
    public float GetCurrentAttackPower()
    {
        float attackPower = 0f;

        attackPower += _unitData.attackPower;

        return attackPower;
    }
    public float GetCurrentDefensePower()
    {
        float defensePower = 0f;

        defensePower += _unitData.defensePower;

        return defensePower;
    }
    public float GetCurrentMoveSpeed()
    {
        float moveSpeed = 0f;

        moveSpeed += _unitData.moveSpeed;

        return moveSpeed;
    }
    public float GetCurrentRotateSpeed()
    {
        float rotateSpeed = 0f;

        rotateSpeed += _unitData.rotateSpeed;

        return rotateSpeed;
    }
    public float GetCurrentRange()
    {
        float range = 0f;

        range += _unitData.range;

        return range;
    }
    public float GetCurrentRangeMulti()
    {
        float rangeMulti = 0f;

        rangeMulti += _unitData.rangeMulti;

        return rangeMulti;
    }
    public float GetCurrentViewAngle()
    {
        float viewAngle = 0f;

        viewAngle += _unitData.viewAngle;

        return viewAngle;
    }
    public float GetCurrentHp()
    {
        return _curHp;
    }
    public float GetCurrentMp()
    {
        return _curMp;
    }
    public float GetPlusDamage()
    {
        return _levelData.damage;
    }
    public float GetCurrentSkill1Distance()
    {
        return _skill.Skill1Distance;
    }
    #endregion

    public void SetSightAnim(bool sight)
    {
        _animator.SetBool(SightRange, sight);
    }

    public void SetAttackAnim()
    {
        _animator.SetTrigger(AttackRange);
    }

    public float GetSkillAnimTime(DefEnum.EAnim eAnim)
    {
        return _dicAnimTime[_liAnimName[(int)eAnim]];
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

    public void SetDestination(Vector3 destPos)
    {
        _agent.SetDestination(destPos);
    }

    public void SetAgentSpeed(float speed)
    {
        _agent.speed = speed;
    }

    public void SetAgentStoppingDistance(float distance)
    {
        _agent.stoppingDistance = distance;
    }

    public void SetAgentAngularSpeed(float angularSpeed)
    {
        _agent.angularSpeed = angularSpeed;
    }

    public bool IsRunAnimPlaying()
    {
        if (_animator == null)
            return true;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션의 해시 값 비교
        if (stateInfo.IsName("Run"))
        {
            // 애니메이션의 재생 시간 비교
            if (stateInfo.normalizedTime < 1f)
            {
                return true;
            }
        }

        return false;
    }

    public void PlayRunAnim()
    {
        _animator.SetBool(SightRange, true);
    }

    public void StopRunAnim()
    {
        _agent.velocity = Vector3.zero;

        _animator.SetBool(SightRange, false);

        if (IsOnNavMesh)
        {
            _agent.ResetPath();
        }
    }

    public void SetAirborne(bool isAirborne, DG.Tweening.Sequence sequence = null)
    {
        _seqAirborn = sequence;

        if (isAirborne)
        {
            MyUnitState |= DefEnum.EUnitState.IsAirborne;
        }
        else
        {
            MyUnitState &= ~DefEnum.EUnitState.IsAirborne;
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
            var targetTracker = GetComponent<CHTargetTracker>();
            if (targetTracker != null && targetTracker.GetClosestTargetInfo() != null && targetTracker.GetClosestTargetInfo().target != null)
            {
                targetTracker.SetExpensionRange(true);
            }

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

        if (_levelData != null)
        {
            _maxHp += changeLevelData.maxHp - _levelData.maxHp;
            _maxMp += changeLevelData.maxMp - _levelData.maxMp;
            _curHp += changeLevelData.maxHp - _levelData.maxHp;
            _curMp += changeLevelData.maxMp - _levelData.maxMp;
        }
        else
        {
            _maxHp += changeLevelData.maxHp;
            _maxMp += changeLevelData.maxMp;
            _curHp += changeLevelData.maxHp;
            _curMp += changeLevelData.maxMp;
        }

        _levelData = changeLevelData;
    }

    void AtOnceChangeHp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        if (IsDie)
            return;

        float hpOrigin = _curHp;
        float hpResult = _curHp + value;
        if (hpResult >= _maxHp)
        {
            hpResult = _maxHp;
        }

        _curHp = hpResult;

        if (_gaugeBarHP)
            _gaugeBarHP.SetGaugeBar(_maxHp, this.GetCurrentHp(), value, 1.5f, 1f);

        if (eSkill != DefEnum.ESkill.None)
        {
            Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, Damage : {value}" +
            $"{_unitData.unitName}<{gameObject.name}> => Hp : {hpOrigin} -> {hpResult}");
        }

        //# 죽음 Die
        if (hpResult <= 0.00001f)
        {
            Debug.Log($"{name} is Died");

            hpResult = 0f;

            _animator.SetTrigger(AttackRange);
            _animator.SetBool(SightRange, false);
            _animator.SetTrigger(Die);

            MyUnitState |= DefEnum.EUnitState.IsDie;

            _unitCollider.enabled = false;

            if (_gaugeBarHP)
                _gaugeBarHP.gameObject.SetActive(false);

            //CHMResource.Instance.Destroy(gameObject, 1f);
        }
    }

    void AtOnceChangeMp(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float mpOrigin = _curMp;
        float mpResult = _curMp + value;
        if (mpResult >= _maxMp)
        {
            mpResult = _maxMp;
        }
        else if (mpResult < 0)
        {
            mpResult = 0f;
        }

        _curMp = mpResult;

        if (_gaugeBarMP)
            _gaugeBarMP.SetGaugeBar(_maxMp, this.GetCurrentMp(), value, 1.5f, 1f, false);

        if (eSkill != DefEnum.ESkill.None)
        {
            Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_unitData.unitName}<{gameObject.name}> => Mp : {mpOrigin} -> {mpResult}");
        }
    }

    void AtOnceChangeAttackPower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float attackPowerOrigin = _unitData.attackPower;
        float attackPowerResult = _unitData.attackPower + value;
        CheckMaxStatValue(DefEnum.EStat.AttackPower, ref attackPowerResult);

        _unitData.attackPower = attackPowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_unitData.unitName}<{gameObject.name}> => AttackPower : {attackPowerOrigin} -> {attackPowerResult}");
    }

    void AtOnceChangeDefensePower(DefEnum.ESkill eSkill, CHUnit attackUnit, float value)
    {
        float defensePowerOrigin = _unitData.defensePower;
        float defensePowerResult = _unitData.defensePower + value;
        CheckMaxStatValue(DefEnum.EStat.DefensePower, ref defensePowerResult);

        _unitData.attackPower = defensePowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{_unitData.unitName}<{gameObject.name}> => DefensePower : {defensePowerOrigin} -> {defensePowerResult}");
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