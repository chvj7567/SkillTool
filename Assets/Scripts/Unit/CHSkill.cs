using System.Threading;
using System;
using UnityEngine;

[Serializable]
public class CHSkill
{
    //# 스킬 정보
    [SerializeField] SkillData _skill1Data;
    //# 스킬 사용 여부
    [SerializeField] bool _useSkill1 = true;

    //# 클릭으로 스킬 활성화할지 여부(활성화 시 스킬 쿨타입 0인 대신 클릭하여 수동 스킬 사용(useSkill 사용))
    [SerializeField] bool _skill1NoCoolClick = false;

    //# 스킬 잠금 여부(잠금되어있으면 해당 스킬은 NULL)
    [SerializeField, ReadOnly] bool _skill1Lock = false;

    //# 스킬 채널링 여부(애니메이션 있을 경우)
    [SerializeField, ReadOnly] bool _skill1Channeling = false;

    //# 스킬 쓴 후 지난 시간
    [SerializeField, ReadOnly] float _timeSinceLastSkill1 = -1f;

    IUnitInfo _unitInfo;
    IUnitGauge _unitGauge;
    IUnitAnim _unitAnim;

    CancellationTokenSource _cancleTokenSource;

    public float Skill1Distance => _skill1Data.distance;

    public void Init(CancellationTokenSource cancleTokenSource, IUnitInfo unitInfo, IUnitGauge unitGauge, IUnitAnim unitAnim)
    {
        _cancleTokenSource = cancleTokenSource;
        _unitInfo = unitInfo;
        _unitGauge = unitGauge;
        _unitAnim = unitAnim;

        _timeSinceLastSkill1 = -1f;
        _skill1Data = CHMSkill.Instance.GetSkillData(_unitInfo.Skill1Type);

        if (_skill1Data == null)
            _skill1Lock = true;
    }

    public async void OnUpdate()
    {
        //# 죽거나 땅에 있는 상태가 아닐 때 (CC 상태인 경우)
        if (_unitInfo.IsDie || _unitInfo.IsAirborne)
            return;

        DefClass.TargetInfo mainTarget = _unitInfo.Target;

        float skil1CoolTime = _skill1Data.coolTime;
        if (_timeSinceLastSkill1 >= 0f && _timeSinceLastSkill1 < skil1CoolTime)
        {
            _timeSinceLastSkill1 += Time.deltaTime;
            _unitGauge.SetCTGaugeBar(skil1CoolTime, _timeSinceLastSkill1, 0, 0, 0);
        }
        else
        {
            _timeSinceLastSkill1 = -1f;
            _unitGauge.SetCTGaugeBar(skil1CoolTime, skil1CoolTime, 0, 0, 0);
        }

        if (mainTarget == null || mainTarget.target == null)
        {
            _unitAnim.SetSightAnim(false);
        }
        //# 타겟이 범위 안에 있으면 즉시 공격 후 공격 딜레이 설정
        else
        {
            Vector3 posMainTarget = mainTarget.target.transform.position;
            Vector3 posMy = _unitInfo.UnitTransform.position;
            Vector3 dirMy = Vector3.zero;

            switch (_unitInfo.StandardAxis)
            {
                case DefEnum.EStandardAxis.X:
                    {
                        dirMy = _unitInfo.UnitTransform.right;
                    }
                    break;
                case DefEnum.EStandardAxis.Z:
                    {
                        dirMy = _unitInfo.UnitTransform.forward;
                    }
                    break;
            }
            posMainTarget.y = 0f;
            posMy.y = 0f;
            var dirMainTarget = posMainTarget - posMy;

            //# 1번 스킬
            if ((_skill1Lock == false) && _useSkill1 && _unitInfo.IsNormal)
            {
                if ((_skill1Channeling == false) && (_timeSinceLastSkill1 < 0f) && (mainTarget.distance <= _skill1Data.distance))
                {
                    _unitAnim.SetAttackAnim();

                    if (_skill1Data.isChanneling)
                    {
                        //# 애니메이션 시전 시간동안 채널링
                        _skill1Channeling = true;

                        //# 일단 모든 스킬은 공격 애니메이션으로 통일하지만 추후 활용시 유닛정보에 애니메이션 정보 담아서 활용 가능
                        float skillAnimTime = _unitAnim.GetSkillAnimTime(DefEnum.EAnim.Attack);
                        if (await Util.Delay(skillAnimTime, _cancleTokenSource) == false)
                            return;

                        _skill1Channeling = false;
                    }

                    CHMSkill.Instance.CreateSkill(new DefClass.SkillLocationInfo
                    {
                        trCaster = _unitInfo.UnitTransform,
                        posCaster = posMy,
                        dirCaster = dirMy,
                        trTarget = mainTarget.target.transform,
                        posTarget = posMainTarget,
                        dirTarget = posMainTarget - posMy,
                        posSkill = posMainTarget,
                        dirSkill = posMainTarget - posMy,
                    }, _unitInfo.Skill1Type);

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
        }
    }
}