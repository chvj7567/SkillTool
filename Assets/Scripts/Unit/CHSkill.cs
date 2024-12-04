using System.Threading;
using System;
using UnityEngine;

[Serializable]
public class CHSkill
{
    //# ��ų ����
    [SerializeField] SkillData _skill1Data;
    //# ��ų ��� ����
    [SerializeField] bool _useSkill1 = true;

    //# Ŭ������ ��ų Ȱ��ȭ���� ����(Ȱ��ȭ �� ��ų ��Ÿ�� 0�� ��� Ŭ���Ͽ� ���� ��ų ���(useSkill ���))
    [SerializeField] bool _skill1NoCoolClick = false;

    //# ��ų ��� ����(��ݵǾ������� �ش� ��ų�� NULL)
    [SerializeField, ReadOnly] bool _skill1Lock = false;

    //# ��ų ä�θ� ����(�ִϸ��̼� ���� ���)
    [SerializeField, ReadOnly] bool _skill1Channeling = false;

    //# ��ų �� �� ���� �ð�
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
        //# �װų� ���� �ִ� ���°� �ƴ� �� (CC ������ ���)
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
        //# Ÿ���� ���� �ȿ� ������ ��� ���� �� ���� ������ ����
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

            //# 1�� ��ų
            if ((_skill1Lock == false) && _useSkill1 && _unitInfo.IsNormal)
            {
                if ((_skill1Channeling == false) && (_timeSinceLastSkill1 < 0f) && (mainTarget.distance <= _skill1Data.distance))
                {
                    _unitAnim.SetAttackAnim();

                    if (_skill1Data.isChanneling)
                    {
                        //# �ִϸ��̼� ���� �ð����� ä�θ�
                        _skill1Channeling = true;

                        //# �ϴ� ��� ��ų�� ���� �ִϸ��̼����� ���������� ���� Ȱ��� ���������� �ִϸ��̼� ���� ��Ƽ� Ȱ�� ����
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
                        //# ��ų ��Ÿ�� �ʱ�ȭ
                        _timeSinceLastSkill1 = 0.0001f;
                    }
                }
            }
        }
    }
}