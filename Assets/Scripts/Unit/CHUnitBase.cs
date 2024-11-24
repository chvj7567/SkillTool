using DG.Tweening;
using System;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class CHUnitBase : MonoBehaviour
{
    #region Parameter
    [SerializeField, ReadOnly] float _maxHp;
    [SerializeField, ReadOnly] float _maxMp;
    [SerializeField, ReadOnly] float _curHp;
    [SerializeField, ReadOnly] float _curMp;
    [SerializeField, ReadOnly] float _bonusHp;
    [SerializeField, ReadOnly] float _bonusMp;

    CHGaugeBar _gaugeBarHP;
    CHGaugeBar _gaugeBarMP;
    CHGaugeBar _gaugeBarCT;

    CancellationTokenSource _cancleTokenSource;

    DG.Tweening.Sequence _seqAirborn;

    public Action actDie;

    IDisposable _disposePerSecond;
    #endregion

    #region Property
    [SerializeField] public DefEnum.EUnit UnitType { get; set; }
    [SerializeField] public Collider UnitCollider { get; set; }
    [SerializeField] public MeshRenderer UnitMesh { get; set; }
    [SerializeField] public bool ShowHp { get; set; }
    [SerializeField] public bool ShowMp { get; set; }
    [SerializeField] public bool ShowCoolTime { get; set; }
    [SerializeField] public UnitData MyUnitData { get; private set; }
    [SerializeField] public LevelData MyLevelData { get; private set; }
    [SerializeField] public SkillData MySkill1Data { get; private set; }
    [SerializeField] public SkillData MySkill2Data { get; private set; }
    [SerializeField] public SkillData MySkill3Data { get; private  set; }
    [SerializeField] public SkillData MySkill4Data { get; private set; }
    [SerializeField] public ItemData MyItem1Data { get; private set; }
    [SerializeField] public DefEnum.EUnitState MyUnitState { get; private set; }
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

        if (UnitCollider == null)
            UnitCollider = gameObject.GetOrAddComponent<Collider>();
        if (UnitMesh == null)
            UnitMesh = gameObject.GetOrAddComponent<MeshRenderer>();
    }

    private void Start()
    {
        Init();
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
        InitGaugeBar(ShowHp, ShowMp, ShowCoolTime);

        _disposePerSecond = gameObject.UpdateAsObservable()
            .ThrottleFirst(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                if (IsDeath())
                    return;

                //# 초당 Hp/Mp 회복 적용
                if (MyItem1Data == null)
                {
                    ChangeHp(DefEnum.ESkill.None, this, MyUnitData.hpRegenPerSecond, DefEnum.EDamageType1.None);
                    ChangeMp(DefEnum.ESkill.None, this, MyUnitData.mpRegenPerSecond, DefEnum.EDamageType1.None);
                }
                else
                {
                    ChangeHp(DefEnum.ESkill.None, this, MyUnitData.hpRegenPerSecond + MyItem1Data.hpRegenPerSecond, DefEnum.EDamageType1.None);
                    ChangeMp(DefEnum.ESkill.None, this, MyUnitData.mpRegenPerSecond + MyItem1Data.mpRegenPerSecond, DefEnum.EDamageType1.None);
                }
            });
    }

    void InitUnitData()
    {
        MyUnitState = 0;
        _maxHp = _bonusHp;
        _maxMp = _bonusMp;
        _curHp = _bonusHp;
        _curMp = _bonusMp;
        UnitCollider.enabled = true;

        if (UnitType == DefEnum.EUnit.None)
        {
            UnitType = (DefEnum.EUnit)UnityEngine.Random.Range(1, (int)DefEnum.EUnit.White);
        }

        MyUnitData = CHMUnit.Instance.GetUnitData(UnitType);

        if (MyUnitData == null)
        {
            Debug.Log($"{UnitType} UnitData is null");
        }
        else
        {
            _maxHp += MyUnitData.maxHp;
            _maxMp += MyUnitData.maxMp;
            _curHp += MyUnitData.maxHp;
            _curMp += MyUnitData.maxMp;

            CHMUnit.Instance.SetUnit(gameObject, UnitType);

            MyLevelData = CHMLevel.Instance.GetLevelData(UnitType, MyUnitData.eLevel);

            if (MyLevelData != null)
            {
                _maxHp += MyLevelData.maxHp;
                _maxMp += MyLevelData.maxMp;
                _curHp += MyLevelData.maxHp;
                _curMp += MyLevelData.maxMp;
            }

            MySkill1Data = CHMSkill.Instance.GetSkillData(MyUnitData.eSkill1);
            MySkill2Data = CHMSkill.Instance.GetSkillData(MyUnitData.eSkill2);
            MySkill3Data = CHMSkill.Instance.GetSkillData(MyUnitData.eSkill3);
            MySkill4Data = CHMSkill.Instance.GetSkillData(MyUnitData.eSkill4);

            MyItem1Data = CHMItem.Instance.GetItemData(MyUnitData.eItem1);

            if (MyItem1Data != null)
            {
                _maxHp += MyItem1Data.maxHp;
                _maxMp += MyItem1Data.maxMp;
                _curHp += MyItem1Data.maxHp;
                _curMp += MyItem1Data.maxMp;
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
                        if (UnitCollider == null)
                        {
                            UnitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarHP.Init(this, UnitCollider.bounds.size.y / 2f / transform.localScale.x, 2.3f);
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
                        if (UnitCollider == null)
                        {
                            UnitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarMP.Init(this, UnitCollider.bounds.size.y / 2f / transform.localScale.x, 1.7f);
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
                        if (UnitCollider == null)
                        {
                            UnitCollider = gameObject.GetOrAddComponent<Collider>();
                        }

                        _gaugeBarCT.Init(this, UnitCollider.bounds.size.y / 2f / transform.localScale.x, -2f);
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

    public UnitData GetOriginUnitData() { return MyUnitData; }
    public SkillData GetOriginSkill1Data() { return MySkill1Data; }
    public SkillData GetOriginSkill2Data() { return MySkill2Data; }
    public SkillData GetOriginSkill3Data() { return MySkill3Data; }
    public SkillData GetOriginSkill4Data() { return MySkill4Data; }
    public ItemData GetOriginItem1Data() { return MyItem1Data; }
    public float GetCurrentMaxHp()
    {
        float maxHp = 0f;

        maxHp += GetOriginUnitData().maxHp;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            maxHp += item1.maxHp;
        }

        return maxHp;
    }
    public float GetCurrentHpRegenPerSecond()
    {
        float hpRegenPerSecond = 0f;

        hpRegenPerSecond += GetOriginUnitData().hpRegenPerSecond;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            hpRegenPerSecond += item1.hpRegenPerSecond;
        }

        return hpRegenPerSecond;
    }
    public float GetCurrentMaxMp()
    {
        float maxMp = 0f;

        maxMp += GetOriginUnitData().maxMp;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            maxMp += item1.maxMp;
        }

        return maxMp;
    }
    public float GetCurrentMpRegenPerSecond()
    {
        float mpRegenPerSecond = 0f;

        mpRegenPerSecond += GetOriginUnitData().mpRegenPerSecond;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            mpRegenPerSecond += item1.mpRegenPerSecond;
        }

        return mpRegenPerSecond;
    }
    public float GetCurrentAttackPower()
    {
        float attackPower = 0f;

        attackPower += GetOriginUnitData().attackPower;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            attackPower += item1.attackPower;
        }

        return attackPower;
    }
    public float GetCurrentDefensePower()
    {
        float defensePower = 0f;

        defensePower += GetOriginUnitData().defensePower;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            defensePower += item1.defensePower;
        }

        return defensePower;
    }
    public float GetCurrentMoveSpeed()
    {
        float moveSpeed = 0f;

        moveSpeed += GetOriginUnitData().moveSpeed;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            moveSpeed += item1.moveSpeed;
        }

        return moveSpeed;
    }
    public float GetCurrentRotateSpeed()
    {
        float rotateSpeed = 0f;

        rotateSpeed += GetOriginUnitData().rotateSpeed;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            rotateSpeed += item1.rotateSpeed;
        }

        return rotateSpeed;
    }
    public float GetCurrentRange()
    {
        float range = 0f;

        range += GetOriginUnitData().range;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            range += item1.range;
        }

        return range;
    }
    public float GetCurrentRangeMulti()
    {
        float rangeMulti = 0f;

        rangeMulti += GetOriginUnitData().rangeMulti;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            rangeMulti += item1.rangeMulti;
        }

        return rangeMulti;
    }
    public float GetCurrentViewAngle()
    {
        float viewAngle = 0f;

        viewAngle += GetOriginUnitData().viewAngle;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            viewAngle += item1.viewAngle;
        }

        return viewAngle;
    }
    public float GetCurrentSkill1Distance()
    {
        float distance = 0f;

        distance += GetOriginSkill1Data().distance;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            distance += item1.distance;
        }

        return distance;
    }
    public float GetCurrentSkill2Distance()
    {
        float distance = 0f;

        distance += GetOriginSkill2Data().distance;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            distance += item1.distance;
        }

        return distance;
    }
    public float GetCurrentSkill3Distance()
    {
        float distance = 0f;

        distance += GetOriginSkill3Data().distance;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            distance += item1.distance;
        }

        return distance;
    }
    public float GetCurrentSkill4Distance()
    {
        float distance = 0f;

        distance += GetOriginSkill4Data().distance;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            distance += item1.distance;
        }

        return distance;
    }
    public float GetCurrentSkill1CoolTime()
    {
        float coolTime = 0f;

        coolTime += GetOriginSkill1Data().coolTime;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            coolTime += item1.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill2CoolTime()
    {
        float coolTime = 0f;

        coolTime += GetOriginSkill2Data().coolTime;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            coolTime += item1.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill3CoolTime()
    {
        float coolTime = 0f;

        coolTime += GetOriginSkill3Data().coolTime;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            coolTime += item1.coolTime;
        }

        return coolTime;
    }
    public float GetCurrentSkill4CoolTime()
    {
        float coolTime = 0f;

        coolTime += GetOriginSkill4Data().coolTime;

        ItemData item1 = GetOriginItem1Data();
        if (item1 != null)
        {
            coolTime += item1.coolTime;
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

    public bool IsNormalState()
    {
        return MyUnitState == 0;
    }

    public bool IsDeath()
    {
        return (MyUnitState & DefEnum.EUnitState.IsDead) != 0;
    }

    public bool IsAirborne()
    {
        return (MyUnitState & DefEnum.EUnitState.IsAirborne) != 0;
    }

    public void ChangeHp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDeath() == false)
        {
            var targetTracker = GetComponent<CHTargetTracker>();
            if (targetTracker != null && targetTracker.GetClosestTargetInfo() != null && targetTracker.GetClosestTargetInfo().objTarget != null)
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

    public void ChangeMp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDeath() == false)
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

    public void ChangeAttackPower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDeath() == false)
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

    public void ChangeDefensePower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value, DefEnum.EDamageType1 eDamageType1)
    {
        if (IsDeath() == false)
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
        MySkill1Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill2(DefEnum.ESkill eSkill)
    {
        MySkill2Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill3(DefEnum.ESkill eSkill)
    {
        MySkill3Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeSkill4(DefEnum.ESkill eSkill)
    {
        MySkill4Data = CHMSkill.Instance.GetSkillData(eSkill);
    }

    public void ChangeItem1(DefEnum.EItem eItem)
    {
        MyItem1Data = CHMItem.Instance.GetItemData(eItem);
        if (MyItem1Data != null)
        {
            _maxHp = MyUnitData.maxHp + MyItem1Data.maxHp;
            _maxMp = MyUnitData.maxMp + MyItem1Data.maxMp;

            _curHp += MyItem1Data.maxHp;
            _curMp += MyItem1Data.maxMp;
        }
    }

    public void ChangeLevel(DefEnum.ELevel eLevel)
    {
        var changeLevelData = CHMLevel.Instance.GetLevelData(UnitType, eLevel);
        if (changeLevelData == null)
            return;

        if (MyLevelData != null)
        {
            _maxHp += changeLevelData.maxHp - MyLevelData.maxHp;
            _maxMp += changeLevelData.maxMp - MyLevelData.maxMp;
            _curHp += changeLevelData.maxHp - MyLevelData.maxHp;
            _curMp += changeLevelData.maxMp - MyLevelData.maxMp;
        }
        else
        {
            _maxHp += changeLevelData.maxHp;
            _maxMp += changeLevelData.maxMp;
            _curHp += changeLevelData.maxHp;
            _curMp += changeLevelData.maxMp;
        }

        MyLevelData = changeLevelData;
    }

    void AtOnceChangeHp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value)
    {
        if (IsDeath())
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
            $"{MyUnitData.unitName}<{gameObject.name}> => Hp : {hpOrigin} -> {hpResult}");
        }

        // 죽음 Die
        if (hpResult <= 0.00001f)
        {
            Debug.Log($"{name} is Died");

            hpResult = 0f;

            var contBase = GetComponent<CHContBase>();
            if (contBase != null)
            {
                var animator = contBase.GetAnimator();
                if (animator != null)
                {
                    animator.SetBool(contBase.AttackRange, false);
                    animator.SetBool(contBase.SightRange, false);
                    animator.SetTrigger(contBase.Death);
                }
            }

            MyUnitState |= DefEnum.EUnitState.IsDead;

            UnitCollider.enabled = false;

            if (_gaugeBarHP)
                _gaugeBarHP.gameObject.SetActive(false);

            transform.DOMoveY(-10f, 1f);

            actDie?.Invoke();

            CHMResource.Instance.Destroy(gameObject, 1f);
        }
    }

    void AtOnceChangeMp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value)
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
            $"{MyUnitData.unitName}<{gameObject.name}> => Mp : {mpOrigin} -> {mpResult}");
        }
    }

    void AtOnceChangeAttackPower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value)
    {
        float attackPowerOrigin = MyUnitData.attackPower;
        float attackPowerResult = MyUnitData.attackPower + value;
        CheckMaxStatValue(DefEnum.EStat.AttackPower, ref attackPowerResult);

        MyUnitData.attackPower = attackPowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{MyUnitData.unitName}<{gameObject.name}> => AttackPower : {attackPowerOrigin} -> {attackPowerResult}");
    }

    void AtOnceChangeDefensePower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float value)
    {
        float defensePowerOrigin = MyUnitData.defensePower;
        float defensePowerResult = MyUnitData.defensePower + value;
        CheckMaxStatValue(DefEnum.EStat.DefensePower, ref defensePowerResult);

        MyUnitData.attackPower = defensePowerResult;
        Debug.Log($"attacker : {attackUnit.name}, skill : {eSkill.ToString()}, " +
            $"{MyUnitData.unitName}<{gameObject.name}> => DefensePower : {defensePowerOrigin} -> {defensePowerResult}");
    }

    async void ContinuousChangeHp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float time, int count, float value)
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

    async void ContinuousChangeMp(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float time, int count, float value)
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

    async void ContinuousChangeAttackPower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float time, int count, float value)
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

    async void ContinuousChangeDefensePower(DefEnum.ESkill eSkill, CHUnitBase attackUnit, float time, int count, float value)
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
