using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;

public class CHTargetTracker : MonoBehaviour
{
    [Header("Ÿ�� ���� ����")]
    public DefEnum.EStandardAxis standardAxis; // ���� ������ �� ��
    public LayerMask targetMask; // Ÿ���� �� ���̾�
    public LayerMask ignoreMask; // ������ ���̾�
    public float range; // Ÿ���� ������ ����
    public float rangeMulti = 2; // Ÿ���� ���� �� �þ�� �þ� ���
    public float rangeMultiTime = 3; // Ÿ���� ���� �� �þ߰� �þ�� �ð�(��)
    [Range(0, 360)] public float viewAngle; // Ÿ���� ������ �þ߰�
    public bool viewEditor; // ������ �󿡼� �þ߰� Ȯ�� ����

    [Header("���� ��")]
    [SerializeField, ReadOnly] float _orgRangeMulti = -1f;
    [SerializeField, ReadOnly] float _orgViewAngle = -1f;

    [Header("��ų �����Ÿ�")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("�þ� Ȯ�� ����")]
    [SerializeField, ReadOnly] bool _expensionRange = false;

    [Header("������ ����")]
    [SerializeField] CHMover _mover;

    [Header("���� Ÿ��")]
    [SerializeField, ReadOnly] DefClass.TargetInfo _closestTarget = new DefClass.TargetInfo();

    #region Get
    public DefClass.TargetInfo GetClosestTargetInfo()
    {
        return _closestTarget;
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
                if (Physics.Raycast(originPos, dirTarget, targetDis, ~(lmTarget | ignoreMask)) == false)
                {
                    var unitBase = target.GetComponent<CHUnitBase>();
                    // Ÿ���� ��������� Ÿ������ ����
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

        //# ������ ������ Ÿ�� ����
        gameObject.UpdateAsObservable().Subscribe(_ =>
        {
            //# ��Ȱ��ȭ �Ǿ������� Ÿ�� ���� X
            if (gameObject.activeSelf == false)
                return;

            //# �׾����� Ÿ�� ���� X
            bool isDead = unitData == null ? false : unitData.IsDeath();
            if (isDead)
                return;

            //# �þ� ���� �ȿ� ���� Ÿ�� �� ���� ����� Ÿ�� ����
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

            //# ������ Ÿ���� ���� ���
            if (_closestTarget.objTarget == null)
            {
                SetExpensionRange(false);
                _mover.StopRunAnim();
            }
            //# ������ Ÿ���� �ִ� ���
            else
            {
                SetExpensionRange(true);

                //# ��ų �����Ÿ� ���� ������ ���ߵ��� ����
                agent.stoppingDistance = _skill1Distance;

                //# ���� ������ �����̸�(CC �� �� �ɷ��ִ� ��������)
                if (unitData.IsNormalState())
                {
                    //# ��ų �����Ÿ� �ۿ� �ִ� ���
                    if (_closestTarget.distance > _skill1Distance)
                    {
                        //# �׺�޽� �����̶��
                        if (agent.isOnNavMesh)
                        {
                            //# Ÿ�� ��ġ�� �����Ͽ� �i�ư�
                            agent.SetDestination(_closestTarget.objTarget.transform.position);
                        }
                        else
                        {
                            //# Ÿ���� ���� �ٶ󺸴� �͸� ����
                            _mover.LookAtPosition(_closestTarget.objTarget.transform.position);
                        }

                        _mover.PlayRunAnim();
                    }
                    //# ��ų �����Ÿ� �ȿ� �ִ� ���
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

            // �þ߰��� ��輱
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
