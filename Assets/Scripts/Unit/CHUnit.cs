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

public partial class CHUnit : MonoBehaviour
{
    #region Parameter
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Animator _animator;
    [SerializeField] Collider _unitCollider;
    [SerializeField] CHTargetTracker _targetTracker;
    [SerializeField] List<string> _liAnimName = new List<string>();

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
    SkillData _skill1Data;
    SkillData _skill2Data;
    SkillData _skill3Data;
    SkillData _skill4Data;
    ItemData _item1Data;
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
        SkillUpdate();
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
        InitSkill();
        InitGaugeBar(ShowHp, ShowMp, ShowCoolTime);

        _disposePerSecond = gameObject.UpdateAsObservable()
            .ThrottleFirst(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                if (IsDie)
                    return;

                //# 초당 Hp/Mp 회복 적용
                if (_item1Data == null)
                {
                    ChangeHp(DefEnum.ESkill.None, this, _unitData.hpRegenPerSecond, DefEnum.EDamageType1.None);
                    ChangeMp(DefEnum.ESkill.None, this, _unitData.mpRegenPerSecond, DefEnum.EDamageType1.None);
                }
                else
                {
                    ChangeHp(DefEnum.ESkill.None, this, _unitData.hpRegenPerSecond + _item1Data.hpRegenPerSecond, DefEnum.EDamageType1.None);
                    ChangeMp(DefEnum.ESkill.None, this, _unitData.mpRegenPerSecond + _item1Data.mpRegenPerSecond, DefEnum.EDamageType1.None);
                }
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

            _levelData = CHMLevel.Instance.GetLevelData(UnitType, _unitData.eLevel);

            if (_levelData != null)
            {
                _maxHp += _levelData.maxHp;
                _maxMp += _levelData.maxMp;
                _curHp += _levelData.maxHp;
                _curMp += _levelData.maxMp;
            }

            _skill1Data = CHMSkill.Instance.GetSkillData(_unitData.eSkill1);
            _skill2Data = CHMSkill.Instance.GetSkillData(_unitData.eSkill2);
            _skill3Data = CHMSkill.Instance.GetSkillData(_unitData.eSkill3);
            _skill4Data = CHMSkill.Instance.GetSkillData(_unitData.eSkill4);

            _item1Data = CHMItem.Instance.GetItemData(_unitData.eItem1);

            if (_item1Data != null)
            {
                _maxHp += _item1Data.maxHp;
                _maxMp += _item1Data.maxMp;
                _curHp += _item1Data.maxHp;
                _curMp += _item1Data.maxMp;
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

    public bool CheckSkill1 => _skill1Data != null;
    public bool CheckSkill2 => _skill2Data != null;
    public bool CheckSkill3 => _skill3Data != null;
    public bool CheckSkill4 => _skill4Data != null;

    public bool IsSkill1Channeling => _skill1Data == null ? false : _skill1Data.isChanneling;
    public bool IsSkill2Channeling => _skill2Data == null ? false : _skill2Data.isChanneling;
    public bool IsSkill3Channeling => _skill3Data == null ? false : _skill3Data.isChanneling;
    public bool IsSkill4Channeling => _skill4Data == null ? false : _skill4Data.isChanneling;

    public DefEnum.ESkill Skill1Type => _skill1Data == null ? DefEnum.ESkill.None : _skill1Data.eSkill;
    public DefEnum.ESkill Skill2Type => _skill2Data == null ? DefEnum.ESkill.None : _skill2Data.eSkill;
    public DefEnum.ESkill Skill3Type => _skill3Data == null ? DefEnum.ESkill.None : _skill3Data.eSkill;
    public DefEnum.ESkill Skill4Type => _skill4Data == null ? DefEnum.ESkill.None : _skill4Data.eSkill;

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

        if (_item1Data != null)
        {
            maxHp += _item1Data.maxHp;
        }

        return maxHp;
    }
    public float GetCurrentHpRegenPerSecond()
    {
        float hpRegenPerSecond = 0f;

        hpRegenPerSecond += _unitData.hpRegenPerSecond;

        if (_item1Data != null)
        {
            hpRegenPerSecond += _item1Data.hpRegenPerSecond;
        }

        return hpRegenPerSecond;
    }
    public float GetCurrentMaxMp()
    {
        float maxMp = 0f;

        maxMp += _unitData.maxMp;

        if (_item1Data != null)
        {
            maxMp += _item1Data.maxMp;
        }

        return maxMp;
    }
    public float GetCurrentMpRegenPerSecond()
    {
        float mpRegenPerSecond = 0f;

        mpRegenPerSecond += _unitData.mpRegenPerSecond;

        if (_item1Data != null)
        {
            mpRegenPerSecond += _item1Data.mpRegenPerSecond;
        }

        return mpRegenPerSecond;
    }
    public float GetCurrentAttackPower()
    {
        float attackPower = 0f;

        attackPower += _unitData.attackPower;

        if (_item1Data != null)
        {
            attackPower += _item1Data.attackPower;
        }

        return attackPower;
    }
    public float GetCurrentDefensePower()
    {
        float defensePower = 0f;

        defensePower += _unitData.defensePower;

        if (_item1Data != null)
        {
            defensePower += _item1Data.defensePower;
        }

        return defensePower;
    }
    public float GetCurrentMoveSpeed()
    {
        float moveSpeed = 0f;

        moveSpeed += _unitData.moveSpeed;

        if (_item1Data != null)
        {
            moveSpeed += _item1Data.moveSpeed;
        }

        return moveSpeed;
    }
    public float GetCurrentRotateSpeed()
    {
        float rotateSpeed = 0f;

        rotateSpeed += _unitData.rotateSpeed;

        if (_item1Data != null)
        {
            rotateSpeed += _item1Data.rotateSpeed;
        }

        return rotateSpeed;
    }
    public float GetCurrentRange()
    {
        float range = 0f;

        range += _unitData.range;

        if (_item1Data != null)
        {
            range += _item1Data.range;
        }

        return range;
    }
    public float GetCurrentRangeMulti()
    {
        float rangeMulti = 0f;

        rangeMulti += _unitData.rangeMulti;

        if (_item1Data != null)
        {
            rangeMulti += _item1Data.rangeMulti;
        }

        return rangeMulti;
    }
    public float GetCurrentViewAngle()
    {
        float viewAngle = 0f;

        viewAngle += _unitData.viewAngle;

        if (_item1Data != null)
        {
            viewAngle += _item1Data.viewAngle;
        }

        return viewAngle;
    }
    public float GetCurrentSkill1Distance()
    {
        float distance = 0f;

        distance += _skill1Data.distance;

        if (_item1Data != null)
        {
            distance += _item1Data.distance;
        }

        return distance;
    }
    public float GetCurrentSkill2Distance()
    {
        float distance = 0f;

        distance += _skill2Data.distance;

        if (_item1Data != null)
        {
            distance += _item1Data.distance;
        }

        return distance;
    }
    public float GetCurrentSkill3Distance()
    {
        float distance = 0f;

        distance += _skill3Data.distance;

        if (_item1Data != null)
        {
            distance += _item1Data.distance;
        }

        return distance;
    }
    public float GetCurrentSkill4Distance()
    {
        float distance = 0f;

        distance += _skill4Data.distance;

        if (_item1Data != null)
        {
            distance += _item1Data.distance;
        }

        return distance;
    }
    public float GetCurrentSkill1CoolTime()
    {
        float coolTime = 0f;

        coolTime += _skill1Data.coolTime;

        if (_item1Data != null)
        {
            coolTime += _item1Data.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill2CoolTime()
    {
        float coolTime = 0f;

        coolTime += _skill2Data.coolTime;

        if (_item1Data != null)
        {
            coolTime += _item1Data.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill3CoolTime()
    {
        float coolTime = 0f;

        coolTime += _skill3Data.coolTime;

        if (_item1Data != null)
        {
            coolTime += _item1Data.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill4CoolTime()
    {
        float coolTime = 0f;

        coolTime += _skill4Data.coolTime;

        if (_item1Data != null)
        {
            coolTime += _item1Data.coolTime;
        }

        return coolTime;
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
        if (_item1Data == null)
            return _levelData.damage;

        return _levelData.damage + _item1Data.damage;
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

    public float GetSkillAnimTime(EAnim eAnim)
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

    public bool IsNormalState => MyUnitState == 0;

    public bool IsDie => (MyUnitState & DefEnum.EUnitState.IsDie) != 0;

    public bool IsAirborne => (MyUnitState & DefEnum.EUnitState.IsAirborne) != 0;

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

    public void ChangeSkill1(DefEnum.ESkill eSkill)
    {
        _skill1Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill2(DefEnum.ESkill eSkill)
    {
        _skill2Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill3(DefEnum.ESkill eSkill)
    {
        _skill3Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill4(DefEnum.ESkill eSkill)
    {
        _skill4Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeItem1(DefEnum.EItem eItem)
    {
        _item1Data = CHMItem.Instance.GetItemData(eItem);
        if (_item1Data != null)
        {
            _maxHp = _unitData.maxHp + _item1Data.maxHp;
            _maxMp = _unitData.maxMp + _item1Data.maxMp;

            _curHp += _item1Data.maxHp;
            _curMp += _item1Data.maxMp;
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

public partial class CHUnit
{
    //# 스킬 사용 여부
    [SerializeField] bool _useSkill1 = true;
    [SerializeField] bool _useSkill2 = true;
    [SerializeField] bool _useSkill3 = true;
    [SerializeField] bool _useSkill4 = true;

    //# 클릭으로 스킬 활성화할지 여부(활성화 시 스킬 쿨타입 0인 대신 클릭하여 수동 스킬 사용(useSkill 사용))
    [SerializeField] bool _skill1NoCoolClick = false;
    [SerializeField] bool _skill2NoCoolClick = false;
    [SerializeField] bool _skill3NoCoolClick = false;
    [SerializeField] bool _skill4NoCoolClick = false;

    //# 스킬 잠금 여부(잠금되어있으면 해당 스킬은 NULL)
    [SerializeField, ReadOnly] bool _skill1Lock = false;
    [SerializeField, ReadOnly] bool _skill2Lock = false;
    [SerializeField, ReadOnly] bool _skill3Lock = false;
    [SerializeField, ReadOnly] bool _skill4Lock = false;

    //# 스킬 채널링 여부(애니메이션 있을 경우)
    [SerializeField, ReadOnly] bool _skill1Channeling = false;
    [SerializeField, ReadOnly] bool _skill2Channeling = false;
    [SerializeField, ReadOnly] bool _skill3Channeling = false;
    [SerializeField, ReadOnly] bool _skill4Channeling = false;

    //# 스킬 쓴 후 지난 시간
    [SerializeField, ReadOnly] float _timeSinceLastSkill1 = -1f;
    [SerializeField, ReadOnly] float _timeSinceLastSkill2 = -1f;
    [SerializeField, ReadOnly] float _timeSinceLastSkill3 = -1f;
    [SerializeField, ReadOnly] float _timeSinceLastSkill4 = -1f;

    public void InitSkill()
    {
        if (_targetTracker != null)
        {
            if (CheckSkill1 == false)
                _skill1Lock = true;
            if (CheckSkill2 == false)
                _skill2Lock = true;
            if (CheckSkill3 == false)
                _skill3Lock = true;
            if (CheckSkill4 == false)
                _skill4Lock = true;

            _targetTracker.SetValue(this);

            _timeSinceLastSkill1 = -1f;
            _timeSinceLastSkill2 = -1f;
            _timeSinceLastSkill3 = -1f;
            _timeSinceLastSkill4 = -1f;
        }
    }

    public async void SkillUpdate()
    {
        //# 죽거나 땅에 있는 상태가 아닐 때 (CC 상태인 경우)
        if (IsDie || IsAirborne)
            return;

        DefClass.TargetInfo mainTarget = _targetTracker.GetClosestTargetInfo();

        if (_timeSinceLastSkill1 >= 0f && _timeSinceLastSkill1 < GetCurrentSkill1CoolTime())
        {
            _timeSinceLastSkill1 += Time.deltaTime;
            SetCTGaugeBar(GetCurrentSkill1CoolTime(), _timeSinceLastSkill1, 0, 0, 0);
        }
        else
        {
            _timeSinceLastSkill1 = -1f;
            SetCTGaugeBar(GetCurrentSkill1CoolTime(), GetCurrentSkill1CoolTime(), 0, 0, 0);
        }

        if (_timeSinceLastSkill2 >= 0f && _timeSinceLastSkill2 < GetCurrentSkill2CoolTime())
        {
            _timeSinceLastSkill2 += Time.deltaTime;
        }
        else
        {
            _timeSinceLastSkill2 = -1f;
        }

        if (_timeSinceLastSkill3 >= 0f && _timeSinceLastSkill3 < GetCurrentSkill3CoolTime())
        {
            _timeSinceLastSkill3 += Time.deltaTime;
        }
        else
        {
            _timeSinceLastSkill3 = -1f;
        }

        if (_timeSinceLastSkill4 >= 0f && _timeSinceLastSkill4 < GetCurrentSkill4CoolTime())
        {
            _timeSinceLastSkill4 += Time.deltaTime;
        }
        else
        {
            _timeSinceLastSkill4 = -1f;
        }

        if (mainTarget == null || mainTarget.target == null)
        {
            SetSightAnim(false);
        }
        //# 타겟이 범위 안에 있으면 즉시 공격 후 공격 딜레이 설정
        else
        {
            Vector3 posMainTarget = mainTarget.target.transform.position;
            Vector3 posMy = transform.position;
            Vector3 dirMy = Vector3.zero;

            switch (_targetTracker.StandardAxis)
            {
                case DefEnum.EStandardAxis.X:
                    {
                        dirMy = transform.right;
                    }
                    break;
                case DefEnum.EStandardAxis.Z:
                    {
                        dirMy = transform.forward;
                    }
                    break;
            }
            posMainTarget.y = 0f;
            posMy.y = 0f;
            var dirMainTarget = posMainTarget - posMy;

            //# 1번 스킬
            if ((_skill1Lock == false) && _useSkill1 && IsNormalState)
            {
                if ((_skill1Channeling == false) && (_timeSinceLastSkill1 < 0f) && (mainTarget.distance <= GetCurrentSkill1Distance()))
                {
                    SetAttackAnim();

                    if (IsSkill1Channeling)
                    {
                        //# 애니메이션 시전 시간동안 채널링
                        _skill1Channeling = true;

                        //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                        float skillAnimTime = GetSkillAnimTime(DefEnum.EAnim.Attack);
                        if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                            return;

                        _skill1Channeling = false;
                    }

                    CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                    {
                        trCaster = transform,
                        posCaster = posMy,
                        dirCaster = dirMy,
                        trTarget = mainTarget.target.transform,
                        posTarget = posMainTarget,
                        dirTarget = posMainTarget - posMy,
                        posSkill = posMainTarget,
                        dirSkill = posMainTarget - posMy,
                    }, Skill1Type);

                    if (_skill1NoCoolClick == true)
                    {
                        _useSkill1 = false;
                    }
                    else
                    {
                        //# 스킬 쿨타임 초기화
                        _timeSinceLastSkill1 = 0.0001f;
                    }
                }
            }

            //# 2번 스킬
            if ((_skill2Lock == false) && _useSkill2 && IsNormalState)
            {
                if ((_skill2Channeling == false) && _timeSinceLastSkill2 < 0f && mainTarget.distance <= GetCurrentSkill2Distance())
                {
                    SetAttackAnim();

                    if (IsSkill2Channeling)
                    {
                        //# 애니메이션 시전 시간동안 채널링
                        _skill2Channeling = true;
                        //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                        float skillAnimTime = GetSkillAnimTime(DefEnum.EAnim.Attack);
                        if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                            return;

                        _skill2Channeling = false;
                    }

                    CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                    {
                        trCaster = transform,
                        posCaster = posMy,
                        dirCaster = dirMy,
                        trTarget = mainTarget.target.transform,
                        posTarget = posMainTarget,
                        dirTarget = posMainTarget - posMy,
                        posSkill = posMainTarget,
                        dirSkill = posMainTarget - posMy,
                    }, Skill2Type);

                    if (_skill2NoCoolClick == true)
                    {
                        _useSkill2 = false;
                    }
                    else
                    {
                        //# 스킬 쿨타임 초기화
                        _timeSinceLastSkill2 = 0.0001f;
                    }
                }
            }

            //# 3번 스킬
            if ((_skill3Lock == false) && _useSkill3 && IsNormalState)
            {
                if ((_skill3Channeling == false) && _timeSinceLastSkill3 < 0f && mainTarget.distance <= GetCurrentSkill3Distance())
                {
                    SetAttackAnim();

                    if (IsSkill3Channeling)
                    {
                        //# 애니메이션 시전 시간동안 채널링
                        _skill3Channeling = true;
                        //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                        float skillAnimTime = GetSkillAnimTime(DefEnum.EAnim.Attack);
                        if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                            return;

                        _skill3Channeling = false;
                    }

                    CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                    {
                        trCaster = transform,
                        posCaster = posMy,
                        dirCaster = dirMy,
                        trTarget = mainTarget.target.transform,
                        posTarget = posMainTarget,
                        dirTarget = posMainTarget - posMy,
                        posSkill = posMainTarget,
                        dirSkill = posMainTarget - posMy,
                    }, Skill3Type);

                    if (_skill3NoCoolClick == true)
                    {
                        _useSkill3 = false;
                    }
                    else
                    {
                        //# 스킬 쿨타임 초기화
                        _timeSinceLastSkill3 = 0.0001f;
                    }
                }
            }

            //# 4번 스킬
            if ((_skill4Lock == false) && _useSkill4 && IsNormalState)
            {
                if ((_skill4Channeling == false) && _timeSinceLastSkill4 < 0f && mainTarget.distance <= GetCurrentSkill4Distance())
                {
                    SetAttackAnim();

                    if (IsSkill4Channeling)
                    {
                        //# 애니메이션 시전 시간동안 채널링
                        _skill4Channeling = true;
                        //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                        float skillAnimTime = GetSkillAnimTime(DefEnum.EAnim.Attack);
                        if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                            return;

                        _skill4Channeling = false;
                    }

                    CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                    {
                        trCaster = transform,
                        posCaster = posMy,
                        dirCaster = dirMy,
                        trTarget = mainTarget.target.transform,
                        posTarget = posMainTarget,
                        dirTarget = posMainTarget - posMy,
                        posSkill = posMainTarget,
                        dirSkill = posMainTarget - posMy,
                    }, Skill4Type);

                    if (_skill4NoCoolClick == true)
                    {
                        _useSkill4 = false;
                    }
                    else
                    {
                        //# 스킬 쿨타임 초기화
                        _timeSinceLastSkill4 = 0.0001f;
                    }
                }
            }
        }
    }
}