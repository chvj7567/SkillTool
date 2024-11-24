using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;

public class CHTargetTracker : MonoBehaviour
{
    [Header("타겟 감지 설정")]
    public DefEnum.EStandardAxis standardAxis; // 정면 기준이 될 축
    public LayerMask targetMask; // 타겟이 될 레이어
    public LayerMask ignoreMask; // 무시할 레이어
    public float range; // 타겟을 감지할 범위
    public float rangeMulti = 2; // 타겟을 감지 후 늘어나는 시야 배수
    public float rangeMultiTime = 3; // 타겟을 감지 후 시야가 늘어나는 시간(초)
    [Range(0, 360)] public float viewAngle; // 타겟을 감지할 시야각
    public bool viewEditor; // 에디터 상에서 시야각 확인 여부

    [Header("원본 값")]
    [SerializeField, ReadOnly] float _orgRangeMulti = -1f;
    [SerializeField, ReadOnly] float _orgViewAngle = -1f;

    [Header("스킬 사정거리")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("시야 확장 여부")]
    [SerializeField, ReadOnly] bool _expensionRange = false;

    [Header("움직임 관련")]
    [SerializeField] CHMover _mover;

    [Header("근접 타겟")]
    [SerializeField, ReadOnly] DefClass.TargetInfo _closestTarget = new DefClass.TargetInfo();

    #region Get
    public DefClass.TargetInfo GetClosestTargetInfo()
    {
        return _closestTarget;
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
                if (Physics.Raycast(originPos, dirTarget, targetDis, ~(lmTarget | ignoreMask)) == false)
                {
                    var unitBase = target.GetComponent<CHUnitBase>();
                    // 타겟이 살아있으면 타겟으로 지정
                    if (unitBase != null && unitBase.IsDeath() == false)
                    {
                        targetInfoList.Add(new DefClass.TargetInfo
                        {
                            objTarget = target.gameObject,
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
        _mover.Init(standardAxis);

        NavMeshAgent agent = _mover.Agent;
        CHUnitBase unitData = _mover.UnitData;

        SetValue(unitData);

        //# 프레임 단위로 타겟 감지
        gameObject.UpdateAsObservable().Subscribe(_ =>
        {
            //# 비활성화 되어있으면 타겟 감지 X
            if (gameObject.activeSelf == false)
                return;

            //# 죽었으면 타겟 감지 X
            bool isDead = unitData == null ? false : unitData.IsDeath();
            if (isDead)
                return;

            //# 시야 범위 안에 들어온 타겟 중 제일 가까운 타겟 감지
            switch (standardAxis)
            {
                case DefEnum.EStandardAxis.X:
                    {
                        _closestTarget = GetClosestTargetInfo(transform.position, transform.right, targetMask, range * rangeMulti, viewAngle);
                    }
                    break;
                case DefEnum.EStandardAxis.Z:
                    {
                        _closestTarget = GetClosestTargetInfo(transform.position, transform.forward, targetMask, range * rangeMulti, viewAngle);
                    }
                    break;
            }

            //# 감지된 타겟이 없는 경우
            if (_closestTarget.objTarget == null)
            {
                SetExpensionRange(false);
                _mover.StopRunAnim();
            }
            //# 감지된 타겟이 있는 경우
            else
            {
                SetExpensionRange(true);

                //# 스킬 사정거리 내에 있으면 멈추도록 설정
                agent.stoppingDistance = _skill1Distance;

                //# 공격 가능한 상태이면(CC 등 안 걸려있는 상태인지)
                if (unitData.IsNormalState())
                {
                    //# 스킬 사정거리 밖에 있는 경우
                    if (_closestTarget.distance > _skill1Distance)
                    {
                        //# 네비메쉬 지형이라면
                        if (agent.isOnNavMesh)
                        {
                            //# 타겟 위치를 갱신하여 쫒아감
                            agent.SetDestination(_closestTarget.objTarget.transform.position);
                        }
                        else
                        {
                            //# 타겟을 향해 바라보는 것만 갱신
                            _mover.LookAtPosition(_closestTarget.objTarget.transform.position);
                        }

                        _mover.PlayRunAnim();
                    }
                    //# 스킬 사정거리 안에 있는 경우
                    else
                    {
                        _mover.LookAtPosition(_closestTarget.objTarget.transform.position);
                        _mover.StopRunAnim();
                    }
                }
            }
        }).AddTo(this);
    }

    void OnDrawGizmos()
    {
        bool isDead = false;
        if (_mover.UnitData)
            isDead = _mover.UnitData.IsDeath();

        if (viewEditor && isDead == false)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, range * rangeMulti);

            // 시야각의 경계선
            Vector3 left = transform.Angle(-viewAngle * 0.5f, standardAxis);
            Vector3 right = transform.Angle(viewAngle * 0.5f, standardAxis);

            Debug.DrawRay(transform.position, left * range, Color.green);
            Debug.DrawRay(transform.position, right * range, Color.green);
        }
    }

    public void SetValue(CHUnitBase unitBase)
    {
        if (unitBase == null)
            return;

        range = unitBase.GetCurrentRange();
        rangeMulti = unitBase.GetCurrentRangeMulti();
        _orgRangeMulti = rangeMulti;
        rangeMulti = 1f;
        viewAngle = unitBase.GetCurrentViewAngle();
        _orgViewAngle = viewAngle;
        _skill1Distance = unitBase.GetCurrentSkill1Distance();

        _mover.Agent.speed = unitBase.GetCurrentMoveSpeed();
        _mover.Agent.angularSpeed = unitBase.GetCurrentRotateSpeed();
    }

    public async void SetExpensionRange(bool active)
    {
        if (active)
        {
            _expensionRange = true;
            viewAngle = 360f;
            rangeMulti = _orgRangeMulti;
        }
        else
        {
            await Task.Delay((int)(rangeMultiTime * 1000));

            _expensionRange = false;
            viewAngle = _orgViewAngle;
            rangeMulti = 1f;
        }
    }
}
