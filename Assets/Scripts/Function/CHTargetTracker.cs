using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class CHTargetTracker : MonoBehaviour
{
    [Header("Ÿ�� ���� ����")]
    [SerializeField] DefEnum.EStandardAxis _standardAxis; // ���� ������ �� ��
    [SerializeField] LayerMask _targetMask; // Ÿ���� �� ���̾�
    [SerializeField] LayerMask _ignoreMask; // ������ ���̾�
    [SerializeField] float _range; // Ÿ���� ������ ����
    [SerializeField] float _rangeMulti = 2; // Ÿ���� ���� �� �þ�� �þ� ���
    [SerializeField] float _rangeMultiTime = 3; // Ÿ���� ���� �� �þ߰� �þ�� �ð�(��)
    [SerializeField, Range(0, 360)] float _viewAngle; // Ÿ���� ������ �þ߰�
    [SerializeField] bool _viewEditor; // ������ �󿡼� �þ߰� Ȯ�� ����

    [Header("���� ��")]
    [SerializeField, ReadOnly] float _orgRangeMulti = -1f;
    [SerializeField, ReadOnly] float _orgViewAngle = -1f;

    [Header("��ų �����Ÿ�")]
    [SerializeField, ReadOnly] float _skill1Distance = -1f;

    [Header("�þ� Ȯ�� ����")]
    [SerializeField, ReadOnly] bool _isExpensionRange = false;

    [Header("���� Ÿ��")]
    [SerializeField, ReadOnly] DefClass.TargetInfo _trackerTarget = new DefClass.TargetInfo();

    [Header("����")]
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

    private void Start()
    {
        _unit.StandardAxis = _standardAxis;

        //# ������ ������ Ÿ�� ����
        gameObject.UpdateAsObservable().Subscribe(_ =>
        {
            //# ��Ȱ��ȭ �Ǿ������� Ÿ�� ���� X
            if (gameObject.activeSelf == false)
                return;

            //# �׾����� Ÿ�� ���� X
            if (_unit.IsDie)
                return;

            //# ������ Ÿ��, ���� ���� Ÿ���� ��� ���� ���
            if (_trackerTarget.target == null)
            {
                //# �þ� ���� �ȿ� ���� Ÿ�� �� ���� ����� Ÿ�� ����
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
                    _trackerTarget.distance = Vector3.Distance(transform.position, _trackerTarget.target.transform.position);

                    SetExpensionRange(true);

                    //# ��ų �����Ÿ� ���� ������ ���ߵ��� ����
                    _unit.SetAgentStoppingDistance(_skill1Distance);

                    //# ���� ������ �����̸�(CC �� �� �ɷ��ִ� ��������)
                    if (_unit.IsNormal)
                    {
                        //# ��ų �����Ÿ� �ۿ� �ִ� ���
                        if (_trackerTarget.distance > _skill1Distance)
                        {
                            //# �׺�޽� �����̶��
                            if (_unit.IsOnNavMesh)
                            {
                                //# Ÿ�� ��ġ�� �����Ͽ� �i�ư�
                                _unit.SetDestination(_trackerTarget.target.transform.position);
                            }

                            _unit.LookAtPosition(_trackerTarget.target.transform.position);
                            _unit.PlayRunAnim();
                        }
                        //# ��ų �����Ÿ� �ȿ� �ִ� ���
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

            //# �þ߰��� ��輱4
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
