using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CHTargetTracker
{
    [Header("Ÿ�� ���� ����")]
    [SerializeField] DefEnum.EStandardAxis _standardAxis; // ���� ������ �� ��
    [SerializeField] LayerMask _targetMask; // Ÿ���� �� ���̾�
    [SerializeField] LayerMask _ignoreMask; // ������ ���̾�
    [SerializeField, ReadOnly] float _range; // Ÿ���� ������ ����
    [SerializeField, ReadOnly] float _rangeMulti = 2; // Ÿ���� ���� �� �þ�� �þ� ���
    [SerializeField, ReadOnly] float _rangeMultiTime = 3; // Ÿ���� ���� �� �þ߰� �þ�� �ð�(��)
    [SerializeField, ReadOnly, Range(0, 360)] float _viewAngle; // Ÿ���� ������ �þ߰�

    [Header("��ų �����Ÿ�")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("���� Ÿ��")]
    [SerializeField, ReadOnly] DefClass.TargetInfo _trackerTarget = new DefClass.TargetInfo();

    IUnitInfo _unitInfo;
    IUnitAnim _unitAnim;

    public void Init(IUnitInfo unitInfo, IUnitAnim unitAnim)
    {
        _unitInfo = unitInfo;
        _unitAnim = unitAnim;

        _range = _unitInfo.GetCurrentRange();
        _rangeMulti = _unitInfo.GetCurrentRangeMulti();
        _rangeMulti = 1f;
        _viewAngle = _unitInfo.GetCurrentViewAngle();
        _skill1Distance = _unitInfo.GetCurrentSkill1Distance();
    }

    public void OnUpdate()
    {
        //# �׾����� Ÿ�� ���� X
        if (_unitInfo.IsDie)
        {
            _unitAnim.Stop();
            return;
        }

        //# ������ Ÿ��, ���� ���� Ÿ���� ��� ���� ���
        if (_trackerTarget.target == null)
        {
            //# �þ� ���� �ȿ� ���� Ÿ�� �� ���� ����� Ÿ�� ����
            switch (_standardAxis)
            {
                case DefEnum.EStandardAxis.X:
                    {
                        _trackerTarget = GetClosestTargetInfo(_unitInfo.transform.position, _unitInfo.transform.right, _targetMask, _range * _rangeMulti, _viewAngle);
                    }
                    break;
                case DefEnum.EStandardAxis.Z:
                    {
                        _trackerTarget = GetClosestTargetInfo(_unitInfo.transform.position, _unitInfo.transform.forward, _targetMask, _range * _rangeMulti, _viewAngle);
                    }
                    break;
            }

            _unitAnim.Stop();
        }
        //# ������ Ÿ���� �ְų� ���� ���� Ÿ���� �ִ� ���
        else
        {
            if (_trackerTarget.target.IsDie)
            {
                _trackerTarget.target = null;
            }
            else
            {
                //# ���� Ÿ�ٰ��� �Ÿ� ����
                _trackerTarget.distance = Vector3.Distance(
                    new Vector3(_unitInfo.transform.position.x , 0, _unitInfo.transform.position.z),
                    new Vector3(_trackerTarget.target.transform.position.x ,0, _trackerTarget.target.transform.position.z));

                //# ��ų �����Ÿ� ���� ������ ���ߵ��� ����
                _unitInfo.SetAgentStoppingDistance(_skill1Distance);

                _unitAnim.LookAtPosition(_trackerTarget.target.transform.position);

                //# ���� ������ �����̸�(CC �� �� �ɷ��ִ� ��������)
                if (_unitInfo.IsIdle)
                {
                    //# ��ų �����Ÿ� �ۿ� �ִ� ���
                    if (_trackerTarget.distance > _skill1Distance)
                    {
                        _unitAnim.Move(_trackerTarget.target.transform.position);
                    }
                    //# ��ų �����Ÿ� �ȿ� �ִ� ���
                    else
                    {
                        _unitAnim.Stop();
                    }
                }
            }

        }
    }

    public void OnDrawGizmos()
    {
        if (_unitInfo == null || _unitInfo.IsDie)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(_unitInfo.transform.position, _range * _rangeMulti);

        //# �þ߰��� ��輱4
        Vector3 left = _unitInfo.transform.Angle(-_viewAngle * 0.5f, _standardAxis);
        Vector3 right = _unitInfo.transform.Angle(_viewAngle * 0.5f, _standardAxis);

        Debug.DrawRay(_unitInfo.transform.position, left * _range, Color.green);
        Debug.DrawRay(_unitInfo.transform.position, right * _range, Color.green);
    }

    #region Getter
    public DefEnum.EStandardAxis StandardAxis => _standardAxis;
    public LayerMask TargetMask => _targetMask;
    public LayerMask IgnoreMask => _ignoreMask;

    public void SetTargetMask(LayerMask layer)
    {
        _targetMask = 1 << layer;
    }

    public DefClass.TargetInfo GetClosestTargetInfo()
    {
        return _trackerTarget;
    }

    public List<DefClass.TargetInfo> GetTargetInfoListInRange(Vector3 originPos, Vector3 direction, LayerMask lmTarget, float range, float viewAngle = 360f)
    {
        List<DefClass.TargetInfo> targetInfoList = new List<DefClass.TargetInfo>();

        // �������� �ִ� Ÿ�ٵ� Ȯ��
        Collider[] targets = Physics.OverlapSphere(originPos, range, lmTarget);

        foreach (Collider target in targets)
        {
            Vector3 posTarget = target.transform.position;
            posTarget.y = 0f;
            originPos.y = 0f;
            Vector3 dirTarget = (posTarget - originPos).normalized;

            // �þ߰��� �ɸ����� Ȯ��
            if (Vector3.Angle(direction, dirTarget) <= viewAngle / 2)
            {
                float targetDis = Vector3.Distance(originPos, posTarget);

                // ��ֹ��� �ִ��� Ȯ��
                if (Physics.Raycast(originPos, dirTarget, targetDis, ~(lmTarget | _ignoreMask)) == false)
                {
                    var unit = target.GetComponent<CHUnit>();
                    // Ÿ���� ��������� Ÿ������ ����
                    if (unit != null && unit.IsDie == false)
                    {
                        targetInfoList.Add(new DefClass.TargetInfo
                        {
                            target = unit,
                            distance = targetDis,
                        });
                    }
                }
            }
        }

        return targetInfoList;
    }

    public DefClass.TargetInfo GetClosestTargetInfo(Vector3 originPos, Vector3 direction, LayerMask lmTarget, float range, float viewAngle = 360f)
    {
        DefClass.TargetInfo closestTargetInfo = new DefClass.TargetInfo();
        List<DefClass.TargetInfo> targetInfoList = GetTargetInfoListInRange(originPos, direction, lmTarget, range, viewAngle);

        if (targetInfoList.Count > 0)
        {
            float minDis = Mathf.Infinity;

            foreach (DefClass.TargetInfo targetInfo in targetInfoList)
            {
                if (targetInfo.distance < minDis)
                {
                    minDis = targetInfo.distance;
                    closestTargetInfo = targetInfo;
                }
            }
        }

        return closestTargetInfo;
    }
    #endregion
}
