using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class CHTargetTracker : MonoBehaviour
{
    [Header("타겟 감지 설정")]
    [SerializeField] DefEnum.EStandardAxis _standardAxis; // 정면 기준이 될 축
    [SerializeField] LayerMask _targetMask; // 타겟이 될 레이어
    [SerializeField] LayerMask _ignoreMask; // 무시할 레이어
    [SerializeField] float _range; // 타겟을 감지할 범위
    [SerializeField] float _rangeMulti = 2; // 타겟을 감지 후 늘어나는 시야 배수
    [SerializeField] float _rangeMultiTime = 3; // 타겟을 감지 후 시야가 늘어나는 시간(초)
    [SerializeField, Range(0, 360)] float _viewAngle; // 타겟을 감지할 시야각
    [SerializeField] bool _viewEditor; // 에디터 상에서 시야각 확인 여부

    [Header("원본 값")]
    [SerializeField, ReadOnly] float _orgRangeMulti = -1f;
    [SerializeField, ReadOnly] float _orgViewAngle = -1f;

    [Header("스킬 사정거리")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("시야 확장 여부")]
    [SerializeField, ReadOnly] bool _isExpensionRange = false;

    [Header("근접 타겟")]
    [SerializeField, ReadOnly] DefClass.TargetInfo _trackerTarget = new DefClass.TargetInfo();

    [Header("유닛")]
    [SerializeField] CHUnit _unit;

    #region Setter
    public void SetTargetMask(int layer)
    {
        _targetMask = layer;
    }
    #endregion

    #region Getter
    public DefEnum.EStandardAxis StandardAxis => _standardAxis;
    public LayerMask TargetMask => _targetMask;

    public bool IsExpensionRange => _isExpensionRange;

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

    private void Start()
    {
        _unit.StandardAxis = _standardAxis;

        //# 프레임 단위로 타겟 감지
        gameObject.UpdateAsObservable().Subscribe(_ =>
        {
            //# 비활성화 되어있으면 타겟 감지 X
            if (gameObject.activeSelf == false)
                return;

            //# 죽었으면 타겟 감지 X
            if (_unit.IsDie)
                return;

            //# 감지된 타겟, 추적 중인 타겟이 모두 없는 경우
            if (_trackerTarget.target == null)
            {
                //# 시야 범위 안에 들어온 타겟 중 제일 가까운 타겟 감지
                switch (_standardAxis)
                {
                    case DefEnum.EStandardAxis.X:
                        {
                            _trackerTarget = GetClosestTargetInfo(transform.position, transform.right, _targetMask, _range * _rangeMulti, _viewAngle);
                        }
                        break;
                    case DefEnum.EStandardAxis.Z:
                        {
                            _trackerTarget = GetClosestTargetInfo(transform.position, transform.forward, _targetMask, _range * _rangeMulti, _viewAngle);
                        }
                        break;
                }

                SetExpensionRange(false);
                _unit.StopRunAnim();
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
                    _trackerTarget.distance = Vector3.Distance(transform.position, _trackerTarget.target.transform.position);

                    SetExpensionRange(true);

                    //# 스킬 사정거리 내에 있으면 멈추도록 설정
                    _unit.SetAgentStoppingDistance(_skill1Distance);

                    //# 공격 가능한 상태이면(CC 등 안 걸려있는 상태인지)
                    if (_unit.IsNormal)
                    {
                        //# 스킬 사정거리 밖에 있는 경우
                        if (_trackerTarget.distance > _skill1Distance)
                        {
                            //# 네비메쉬 지형이라면
                            if (_unit.IsOnNavMesh)
                            {
                                //# 타겟 위치를 갱신하여 쫒아감
                                _unit.SetDestination(_trackerTarget.target.transform.position);
                            }

                            _unit.LookAtPosition(_trackerTarget.target.transform.position);
                            _unit.PlayRunAnim();
                        }
                        //# 스킬 사정거리 안에 있는 경우
                        else
                        {
                            _unit.LookAtPosition(_trackerTarget.target.transform.position);
                            _unit.StopRunAnim();
                        }
                    }
                }
                
            }
        }).AddTo(this);
    }

    void OnDrawGizmos()
    {
        if (_viewEditor && _unit.IsDie == false)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _range * _rangeMulti);

            //# 시야각의 경계선4
            Vector3 left = transform.Angle(-_viewAngle * 0.5f, _standardAxis);
            Vector3 right = transform.Angle(_viewAngle * 0.5f, _standardAxis);

            Debug.DrawRay(transform.position, left * _range, Color.green);
            Debug.DrawRay(transform.position, right * _range, Color.green);
        }
    }

    public void SetValue(CHUnit unit)
    {
        if (unit == null)
            return;

        _range = unit.GetCurrentRange();
        _rangeMulti = unit.GetCurrentRangeMulti();
        _orgRangeMulti = _rangeMulti;
        _rangeMulti = 1f;
        _viewAngle = unit.GetCurrentViewAngle();
        _orgViewAngle = _viewAngle;
        _skill1Distance = unit.GetCurrentSkill1Distance();

        _unit.SetAgentSpeed(unit.GetCurrentMoveSpeed());
        _unit.SetAgentAngularSpeed(unit.GetCurrentRotateSpeed());
    }

    public async void SetExpensionRange(bool active)
    {
        if (active)
        {
            _isExpensionRange = true;
            _viewAngle = 360f;
            _rangeMulti = _orgRangeMulti;
        }
        else
        {
            await Task.Delay((int)(_rangeMultiTime * 1000));

            _isExpensionRange = false;
            _viewAngle = _orgViewAngle;
            _rangeMulti = 1f;
        }
    }
}
