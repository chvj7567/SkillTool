using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;

public partial class DefEnum
{
    public enum EAnim
    {
        Idle,
        Attack,
        Run,
        Death
    }
}

public class CHContBase : MonoBehaviour
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

    [SerializeField, ReadOnly] Animator _animator;
    [SerializeField, ReadOnly] CHUnitBase _unitBase;
    [SerializeField, ReadOnly] CHTargetTracker _targetTracker;
    [SerializeField, ReadOnly] NavMeshAgent _agent;

    [SerializeField] List<string> _liAnimName = new List<string>();

    [SerializeField, ReadOnly] Dictionary<string, float> _dicAnimTime = new Dictionary<string, float>();

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

    public int Death
    {
        get
        {
            return Animator.StringToHash("IsDeath");
        }
    }

    CancellationTokenSource _cancleTokenSource;

    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        if (_cancleTokenSource != null && !_cancleTokenSource.IsCancellationRequested)
        {
            _cancleTokenSource.Cancel();
        }
    }

    public Animator GetAnimator()
    {
        return _animator;
    }

    public float GetTimeSinceLastSkill1() { return _timeSinceLastSkill1; }
    public float GetTimeSinceLastSkill2() { return _timeSinceLastSkill2; }
    public float GetTimeSinceLastSkill3() { return _timeSinceLastSkill3; }
    public float GetTimeSinceLastSkill4() { return _timeSinceLastSkill4; }

    public void UseSkill1(bool use)
    {
        _useSkill1 = use;
    }

    public void OpenSkill2()
    {
        _useSkill2 = true;
        _skill2Lock = false;
    }

    public void OpenSkill3()
    {
        _useSkill3 = true;
        _skill3Lock = false;
    }

    public void OpenSkill4()
    {
        _useSkill4 = true;
        _skill4Lock = false;
    }

    public virtual void Init()
    {
        _cancleTokenSource = new CancellationTokenSource();

        _animator = GetComponent<Animator>();
        if (_animator != null)
        {
            RuntimeAnimatorController ac = _animator.runtimeAnimatorController;

            foreach (AnimationClip clip in ac.animationClips)
            {
                //Debug.Log($"{clip.name}, {clip.length}");
                _dicAnimTime.Add(clip.name, clip.length);
            }
        }

        _agent = gameObject.GetOrAddComponent<NavMeshAgent>();
        _unitBase = gameObject.GetOrAddComponent<CHUnitBase>();
        _targetTracker = gameObject.GetOrAddComponent<CHTargetTracker>();
        if (_unitBase != null && _targetTracker != null)
        {
            if (_unitBase.GetOriginSkill1Data() == null) _skill1Lock = true;
            if (_unitBase.GetOriginSkill2Data() == null) _skill2Lock = true;
            if (_unitBase.GetOriginSkill3Data() == null) _skill3Lock = true;
            if (_unitBase.GetOriginSkill4Data() == null) _skill4Lock = true;

            _targetTracker.SetValue(_unitBase);

            _timeSinceLastSkill1 = -1f;
            _timeSinceLastSkill2 = -1f;
            _timeSinceLastSkill3 = -1f;
            _timeSinceLastSkill4 = -1f;

            gameObject.UpdateAsObservable().Subscribe(async _ =>
            {
                //# 죽거나 땅에 있는 상태가 아닐 때 (CC 상태인 경우)
                if (_unitBase.IsDeath() || _unitBase.IsAirborne())
                    return;

                DefClass.TargetInfo mainTarget = _targetTracker.GetClosestTargetInfo();

                if (_timeSinceLastSkill1 >= 0f && _timeSinceLastSkill1 < _unitBase.GetCurrentSkill1CoolTime())
                {
                    _timeSinceLastSkill1 += Time.deltaTime;
                    _unitBase.SetCTGaugeBar(_unitBase.GetCurrentSkill1CoolTime(), _timeSinceLastSkill1, 0, 0, 0);
                }
                else
                {
                    _timeSinceLastSkill1 = -1f;
                    _unitBase.SetCTGaugeBar(_unitBase.GetCurrentSkill1CoolTime(), _unitBase.GetCurrentSkill1CoolTime(), 0, 0, 0);
                }

                if (_timeSinceLastSkill2 >= 0f && _timeSinceLastSkill2 < _unitBase.GetCurrentSkill2CoolTime())
                {
                    _timeSinceLastSkill2 += Time.deltaTime;
                }
                else
                {
                    _timeSinceLastSkill2 = -1f;
                }

                if (_timeSinceLastSkill3 >= 0f && _timeSinceLastSkill3 < _unitBase.GetCurrentSkill3CoolTime())
                {
                    _timeSinceLastSkill3 += Time.deltaTime;
                }
                else
                {
                    _timeSinceLastSkill3 = -1f;
                }

                if (_timeSinceLastSkill4 >= 0f && _timeSinceLastSkill4 < _unitBase.GetCurrentSkill4CoolTime())
                {
                    _timeSinceLastSkill4 += Time.deltaTime;
                }
                else
                {
                    _timeSinceLastSkill4 = -1f;
                }

                if (mainTarget == null || mainTarget.objTarget == null)
                {
                    if (_animator)
                    {
                        _animator.SetBool(SightRange, false);
                    }
                }
                //# 타겟이 범위 안에 있으면 즉시 공격 후 공격 딜레이 설정
                else
                {
                    Vector3 posMainTarget = mainTarget.objTarget.transform.position;
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
                    if ((_skill1Lock == false) && _useSkill1 && _unitBase.IsNormalState())
                    {
                        if ((_skill1Channeling == false) && (_timeSinceLastSkill1 < 0f) && (mainTarget.distance <= _unitBase.GetCurrentSkill1Distance()))
                        {
                            if (_animator)
                            {
                                _animator.SetTrigger(AttackRange);
                                
                                if (_unitBase.GetOriginSkill1Data().isChanneling)
                                {
                                    //# 애니메이션 시전 시간동안 채널링
                                    _skill1Channeling = true;

                                    //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                                    float skillAnimTime = _dicAnimTime[_liAnimName[(int)DefEnum.EAnim.Attack]];
                                    if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                                        return;

                                    _skill1Channeling = false;
                                }
                            }

                            CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                            {
                                trCaster = transform,
                                posCaster = posMy,
                                dirCaster = dirMy,
                                trTarget = mainTarget.objTarget.transform,
                                posTarget = posMainTarget,
                                dirTarget = posMainTarget - posMy,
                                posSkill = posMainTarget,
                                dirSkill = posMainTarget - posMy,
                            }, _unitBase.GetOriginSkill1Data().eSkill);

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
                    if ((_skill2Lock == false) && _useSkill2 && _unitBase.IsNormalState())
                    {
                        if ((_skill2Channeling == false) && _timeSinceLastSkill2 < 0f && mainTarget.distance <= _unitBase.GetCurrentSkill2Distance())
                        {
                            if (_animator)
                            {
                                _animator.SetTrigger(AttackRange);

                                if (_unitBase.GetOriginSkill2Data().isChanneling)
                                {
                                    //# 애니메이션 시전 시간동안 채널링
                                    _skill2Channeling = true;
                                    //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                                    if (await Util.Delay(_dicAnimTime[_liAnimName[(int)DefEnum.EAnim.Attack]], _cancleTokenSource) == false)
                                        return;

                                    _skill2Channeling = false;
                                }
                            }

                            CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                            {
                                trCaster = transform,
                                posCaster = posMy,
                                dirCaster = dirMy,
                                trTarget = mainTarget.objTarget.transform,
                                posTarget = posMainTarget,
                                dirTarget = posMainTarget - posMy,
                                posSkill = posMainTarget,
                                dirSkill = posMainTarget - posMy,
                            }, _unitBase.GetOriginSkill2Data().eSkill);

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
                    if ((_skill3Lock == false) && _useSkill3 && _unitBase.IsNormalState())
                    {
                        if ((_skill3Channeling == false) && _timeSinceLastSkill3 < 0f && mainTarget.distance <= _unitBase.GetCurrentSkill3Distance())
                        {
                            if (_animator)
                            {
                                _animator.SetTrigger(AttackRange);

                                if (_unitBase.GetOriginSkill3Data().isChanneling)
                                {
                                    //# 애니메이션 시전 시간동안 채널링
                                    _skill3Channeling = true;
                                    //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                                    if (await Util.Delay(_dicAnimTime[_liAnimName[(int)DefEnum.EAnim.Attack]], _cancleTokenSource) == false)
                                        return;

                                    _skill3Channeling = false;
                                }
                            }

                            CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                            {
                                trCaster = transform,
                                posCaster = posMy,
                                dirCaster = dirMy,
                                trTarget = mainTarget.objTarget.transform,
                                posTarget = posMainTarget,
                                dirTarget = posMainTarget - posMy,
                                posSkill = posMainTarget,
                                dirSkill = posMainTarget - posMy,
                            }, _unitBase.GetOriginSkill3Data().eSkill);

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
                    if ((_skill4Lock == false) && _useSkill4 && _unitBase.IsNormalState())
                    {
                        if ((_skill4Channeling == false) && _timeSinceLastSkill4 < 0f && mainTarget.distance <= _unitBase.GetCurrentSkill4Distance())
                        {
                            if (_animator)
                            {
                                _animator.SetTrigger(AttackRange);

                                if (_unitBase.GetOriginSkill4Data().isChanneling)
                                {
                                    //# 애니메이션 시전 시간동안 채널링
                                    _skill4Channeling = true;
                                    //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                                    if (await Util.Delay(_dicAnimTime[_liAnimName[(int)DefEnum.EAnim.Attack]], _cancleTokenSource) == false)
                                        return;

                                    _skill4Channeling = false;
                                }
                            }

                            CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                            {
                                trCaster = transform,
                                posCaster = posMy,
                                dirCaster = dirMy,
                                trTarget = mainTarget.objTarget.transform,
                                posTarget = posMainTarget,
                                dirTarget = posMainTarget - posMy,
                                posSkill = posMainTarget,
                                dirSkill = posMainTarget - posMy,
                            }, _unitBase.GetOriginSkill4Data().eSkill);

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
            }).AddTo(this);
        }
    }
}
