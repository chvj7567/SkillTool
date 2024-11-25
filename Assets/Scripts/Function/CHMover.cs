using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CHMover : MonoBehaviour
{
    [Header("관련 컴포넌트")]
    [SerializeField, ReadOnly] NavMeshAgent _agent;
    [SerializeField, ReadOnly] Animator _animator;
    [SerializeField, ReadOnly] CHUnitBase _unitBase;
    [SerializeField, ReadOnly] CHContBase _contBase;

    DefEnum.EStandardAxis _standardAxis;

    public bool IsOnNavMesh => _agent.isOnNavMesh;

    private void Awake()
    {
        _agent = gameObject.GetComponent<NavMeshAgent>();
        _animator = gameObject.GetComponent<Animator>();
        _unitBase = gameObject.GetComponent<CHUnitBase>();
        _contBase = gameObject.GetComponent<CHContBase>();
    }

    public void Init(DefEnum.EStandardAxis standardAxis)
    {
        _standardAxis = standardAxis;
    }

    public void LookAtPosition(Vector3 destPos)
    {
        var posTarget = destPos;
        var posMy = transform.position;

        posTarget.y = 0f;
        posMy.y = 0f;

        switch (_standardAxis)
        {
            case DefEnum.EStandardAxis.X:
                {
                    transform.right = posTarget - posMy;
                }
                break;
            case DefEnum.EStandardAxis.Z:
                {
                    transform.forward = posTarget - posMy;
                }
                break;
        }
    }

    public void SetDestination(Vector3 destPos)
    {
        _agent.SetDestination(destPos);
    }

    public void SetAgentSpeed(float speed)
    {
        _agent.speed = speed;
    }

    public void SetAgentStoppingDistance(float distance)
    {
        _agent.stoppingDistance = distance;
    }

    public void SetAgentAngularSpeed(float angularSpeed)
    {
        _agent.angularSpeed = angularSpeed;
    }

    public bool IsRunAnimPlaying()
    {
        if (_animator == null)
            return true;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션의 해시 값 비교
        if (stateInfo.IsName("Run"))
        {
            // 애니메이션의 재생 시간 비교
            if (stateInfo.normalizedTime < 1f)
            {
                return true;
            }
        }

        return false;
    }

    public void PlayRunAnim()
    {
        if (_contBase && _animator)
        {
            _animator.SetBool(_contBase.SightRange, true);
        }
    }

    public void StopRunAnim()
    {
        _agent.velocity = Vector3.zero;

        if (_contBase && _animator)
        {
            _animator.SetBool(_contBase.SightRange, false);
        }

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
    }
}
