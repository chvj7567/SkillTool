using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CHTargetTracker
{
    [Header("타겟 감지 설정")]
    [SerializeField] DefEnum.EStandardAxis _standardAxis; // 정면 기준이 될 축
    [SerializeField] LayerMask _targetMask; // 타겟이 될 레이어
    [SerializeField] LayerMask _ignoreMask; // 무시할 레이어
    [SerializeField, ReadOnly] float _range; // 타겟을 감지할 범위
    [SerializeField, ReadOnly] float _rangeMulti = 2; // 타겟을 감지 후 늘어나는 시야 배수
    [SerializeField, ReadOnly] float _rangeMultiTime = 3; // 타겟을 감지 후 시야가 늘어나는 시간(초)
    [SerializeField, ReadOnly, Range(0, 360)] float _viewAngle; // 타겟을 감지할 시야각

    [Header("스킬 사정거리")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("근접 타겟")]
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
        //# 죽었으면 타겟 감지 X
        if (_unitInfo.IsDie)
        {
            _unitAnim.Stop();
            return;
        }

        //# 감지된 타겟, 추적 중인 타겟이 모두 없는 경우
        if (_trackerTarget.target == null)
        {
            //# 시야 범위 안에 들어온 타겟 중 제일 가까운 타겟 감지
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
        //# 감지된 타겟이 있거나 추적 중인 타겟이 있는 경우
        else
        {
            if (_trackerTarget.target.IsDie)
            {
                _trackerTarget.target = null;
            }
            else
            {
                //# 현재 타겟과의 거리 갱신
                _trackerTarget.distance = Vector3.Distance(
                    new Vector3(_unitInfo.transform.position.x , 0, _unitInfo.transform.position.z),
                    new Vector3(_trackerTarget.target.transform.position.x ,0, _trackerTarget.target.transform.position.z));

                //# 스킬 사정거리 내에 있으면 멈추도록 설정
                _unitInfo.SetAgentStoppingDistance(_skill1Distance);

                _unitAnim.LookAtPosition(_trackerTarget.target.transform.position);

                //# 공격 가능한 상태이면(CC 등 안 걸려있는 상태인지)
                if (_unitInfo.IsIdle)
                {
                    //# 스킬 사정거리 밖에 있는 경우
                    if (_trackerTarget.distance > _skill1Distance)
                    {
                        _unitAnim.Move(_trackerTarget.target.transform.position);
                    }
                    //# 스킬 사정거리 안에 있는 경우
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

        //# 시야각의 경계선4
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

        // 범위내에 있는 타겟들 확인
        Collider[] targets = Physics.OverlapSphere(originPos, range, lmTarget);

        foreach (Collider target in targets)
        {
            Vector3 posTarget = target.transform.position;
            posTarget.y = 0f;
            originPos.y = 0f;
            Vector3 dirTarget = (posTarget - originPos).normalized;

            // 시야각에 걸리는지 확인
            if (Vector3.Angle(direction, dirTarget) <= viewAngle / 2)
            {
                float targetDis = Vector3.Distance(originPos, posTarget);

                // 장애물이 있는지 확인
                if (Physics.Raycast(originPos, dirTarget, targetDis, ~(lmTarget | _ignoreMask)) == false)
                {
                    var unit = target.GetComponent<CHUnit>();
                    // 타겟이 살아있으면 타겟으로 지정
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
