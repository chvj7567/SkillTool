using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using UniRx;
using System.Threading;
using DG.Tweening;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CHMParticle : CHSingleton<CHMParticle>
{
    #region Private Argument
    //# 파티클 타입과 실행 시간 저장 변수
    Dictionary<DefEnum.EParticle, float> _dicParticleTime = new Dictionary<DefEnum.EParticle, float>();

    CancellationTokenSource _cancleTokenSource;
    #endregion

    #region Initialize
    public bool Initialize => _initialize;

    bool _initialize = false;

    public void Init()
    {
        if (_initialize)
            return;

        _initialize = true;

        _cancleTokenSource = new CancellationTokenSource();

#if UNITY_EDITOR
        EditorApplication.quitting -= Clear;
        EditorApplication.quitting += Clear;
#else
        Application.quitting -= Clear;
        Application.quitting += Clear;
#endif
    }

    public void Clear()
    {
        _dicParticleTime.Clear();

        if (_cancleTokenSource != null && _cancleTokenSource.IsCancellationRequested == false)
        {
            _cancleTokenSource.Cancel();
        }
    }
    #endregion

    #region Getter
    public GameObject GetParticleObject(DefEnum.EParticle eParticle)
    {
        GameObject objParticle = null;

        CHMResource.Instance.InstantiateParticle(eParticle, (_) =>
        {
            if (_ == null) return;

            objParticle = _;

            if (_dicParticleTime.ContainsKey(eParticle) == false)
            {
                _dicParticleTime.Add(eParticle, GetParticleTime(objParticle));
            }

            if (IsAutoDestroy(eParticle))
            {
                DestroyParticle(eParticle, objParticle);
            }
        });

        return objParticle;
    }

    public float GetParticleTime(GameObject particleObj)
    {
        float time = -1;

        var arrParticle = particleObj.GetComponentsInChildren<ParticleSystem>();

        foreach (var particle in arrParticle)
        {
            time = Mathf.Max(time, particle.main.duration);
        }

        return time;
    }
    #endregion

    //# 타겟 확인
    public bool IsTarget(int casterLayer, int targetLayer, DefEnum.ETargetMask eTargetMask)
    {
        var targetMask = CHMSkill.Instance.GetTargetMask(casterLayer, eTargetMask);

        return (1 << targetLayer & targetMask.value) != 0;
    }

    //# 자동 파괴되는 파티클인지 확인
    bool IsAutoDestroy(DefEnum.EParticle eParticle)
    {
        switch (eParticle)
        {
            /*case Defines.EParticle.FX_Ax:
                return false;*/
            default:
                return true;
        }
    }

    
    public void CreateParticle(DefEnum.ESkill eSkill, Transform caster, List<Transform> liTarget, List<Vector3> liParticlePos, List<Vector3> liParticleDir, SkillData.EffectData effectData)
    {
        if (caster == null)
        {
            Debug.Log("No Caster");
            return;
        }

        if (liTarget == null)
        {
            Debug.Log("No Target");
            return;
        }

        if (liParticlePos == null || liParticleDir == null)
        {
            Debug.Log("No Skill Info");
            return;
        }

        List<GameObject> liParticle = new List<GameObject>();

        //# 타겟의 수 만큼 파티클 생성 
        for (int i = 0; i < liTarget.Count; ++i)
        {
            var objParticle = GetParticleObject(effectData.EParticle);

            if (objParticle == null)
            {
                Debug.Log($"{effectData.EParticle} Particle is Null");
                return;
            }

            liParticle.Add(objParticle);
        }

        //# 각 파티클의 콜리젼과 위치값 설정
        for (int i = 0; i < liParticle.Count; ++i)
        {
            var obj = liParticle.ElementAtOrDefault(i);
            var target = liTarget.ElementAtOrDefault(i);
            var position = liParticlePos.ElementAtOrDefault(i);
            var direction = liParticleDir.ElementAtOrDefault(i);

            position.y = 0f;
            direction.y = 0f;

            obj.transform.position = position;

            //# 파티클의 경우 z축이 정면이 되도록
            obj.transform.forward = direction;
            obj.transform.forward = obj.transform.Angle(effectData.effectAngle, DefEnum.EStandardAxis.Z);

            SetParticlePositionValue(eSkill, caster, target, obj, effectData);
            SetParticleCollision(eSkill, caster, target, obj, effectData);
        }
    }

    //# 파티클 파괴
    async void DestroyParticle(DefEnum.EParticle eParticle, GameObject particleObj)
    {
        if (await Util.Delay(_dicParticleTime[eParticle], _cancleTokenSource) == false)
            return;

        if (particleObj)
            CHMResource.Instance.Destroy(particleObj);
    }

    //# 파티클 콜리젼 설정
    async void SetParticleCollision(DefEnum.ESkill eSkill, Transform caster, Transform target, GameObject particleObj, SkillData.EffectData effectData)
    {
        if (effectData.eCollision == DefEnum.ECollision.None)
            return;

        //# 스킬 시전 후 딜레이가 있다면 딜레이 이후 콜리젼 생성
        if (await Util.Delay(effectData.triggerStartDelay, _cancleTokenSource) == false)
            return;

        var sphereCollision = particleObj.GetOrAddComponent<CHSphereCollision>();
        sphereCollision.Init(caster, effectData);
        sphereCollision.sphereCollider.enabled = true;

        //# 스킬에 맞은 경우 설정
        if (effectData.triggerEnter)
        {
            sphereCollision.TriggerEnterCallback(sphereCollision.OnEnter.Subscribe(collider =>
            {
                if (caster == null || target == null || particleObj == null)
                    return;

                if (IsTarget(caster.gameObject.layer, collider.gameObject.layer, effectData.eTargetMask))
                {
                    var hit = collider.ClosestPointOnBounds(sphereCollision.transform.position);
                    SetParticleTriggerValue(eSkill, caster, target, collider.transform, hit, particleObj, effectData);
                }
            }));
        }

        //# 스킬에 다 맞은 이후 설정
        if (effectData.triggerExit)
        {
            sphereCollision.TriggerExitCallback(sphereCollision.OnExit.Subscribe(collider =>
            {
                if (caster == null || target == null || particleObj == null)
                    return;

                if (IsTarget(caster.gameObject.layer, collider.gameObject.layer, effectData.eTargetMask))
                {
                    var hit = collider.ClosestPointOnBounds(sphereCollision.transform.position);
                    SetParticleTriggerValue(eSkill, caster, target, collider.transform, hit, particleObj, effectData);
                }
            }));
        }

        //# 스킬에 맞고 있는 도중 설정
        sphereCollision.TriggerStayCallback(sphereCollision.OnStay.Subscribe(collider =>
        {
            if (caster == null || target == null || particleObj == null)
                return;

            if (IsTarget(caster.gameObject.layer, collider.gameObject.layer, effectData.eTargetMask))
            {
                var hit = collider.ClosestPointOnBounds(sphereCollision.transform.position);
                SetParticleTriggerValue(eSkill, caster, target, collider.transform, hit, particleObj, effectData);
            }
        }));

        //# 스킬 유지 시간 있으면 해당 시간만큼 콜리젼 유지
        if (effectData.triggerStayTime >= 0f)
        {
            if (await Util.Delay(effectData.triggerStayTime, _cancleTokenSource) == false)
                return;

            if (sphereCollision != null && sphereCollision.sphereCollider != null)
                sphereCollision.sphereCollider.enabled = false;
        }
    }

    //# 파티클 위치 설정
    async void SetParticlePositionValue(DefEnum.ESkill eSkill, Transform caster, Transform target, GameObject particleObj, SkillData.EffectData effectData)
    {
        //# 이펙트가 붙어있어야하는 경우 SetParent를 해버리면 해당 타겟은 충돌체에 감지가 안되므로 타겟을 따라다니도록 수정
        if (effectData.attach)
        {
            await MoveTrasnform(particleObj.transform, target, -1f, _dicParticleTime[effectData.EParticle], 0f, particleObj);
        }

        //# 각 이펙트별로 위치 설정이 필요한 경우
        switch (effectData.EParticle)
        {
            case DefEnum.EParticle.Slash:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y - 2.2f, posOrigin.z);
                }
                break;
            case DefEnum.EParticle.FX_Circle_meteor:
            case DefEnum.EParticle.FX_Arrow_impact2:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y + 23f, posOrigin.z);
                }
                break;
            case DefEnum.EParticle.FX_Arrow_impact:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y + .5f, posOrigin.z);

                    await MoveDirection(particleObj.transform.forward, 30f, _dicParticleTime[effectData.EParticle], particleObj);
                }
                break;
            case DefEnum.EParticle.FX_Ax:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y + 3f, posOrigin.z);

                    await MoveDirection(particleObj.transform.forward, 30f, 1f, particleObj);
                    await MoveTrasnform(particleObj.transform, caster, 30f, -1f, 3f, particleObj);

                    CHMResource.Instance.Destroy(particleObj);
                }
                break;
            case DefEnum.EParticle.FX_Tornado:
                {
                    await MoveDirection(particleObj.transform.forward, 10f, _dicParticleTime[effectData.EParticle], particleObj);
                }
                break;
            case DefEnum.EParticle.FX_Explosion_Hit:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y - 0.95f, posOrigin.z);
                }
                break;
            case DefEnum.EParticle.SlashHit:
                {
                    var posOrigin = particleObj.transform.position;
                    particleObj.transform.position = new Vector3(posOrigin.x, posOrigin.y - 2.5f, posOrigin.z);
                }
                break;
        }
    }

    //# 각 파티클에 트리거 된 타겟들 처리 설정(데미지, 힐, CC기 등)
    void SetParticleTriggerValue(DefEnum.ESkill eSkill, Transform caster, Transform target, Transform triggerTarget, Vector3 hitPosition, GameObject particleObj, SkillData.EffectData effectData)
    {
        switch (effectData.EParticle)
        {
            case DefEnum.EParticle.Fx_Healing:
                {
                    CHMSkill.Instance.ApplySkillValue(eSkill, caster, new List<Transform> { caster }, effectData);
                }
                break;
            case DefEnum.EParticle.FX_Tornado:
                {
                    TargetAirborne(triggerTarget);

                    CHMSkill.Instance.ApplySkillValue(eSkill, caster, new List<Transform> { triggerTarget }, effectData);
                }
                break;
            case DefEnum.EParticle.FX_Arrow_impact:
                {
                    particleObj.SetActive(false);

                    var hitParticle = GetParticleObject(DefEnum.EParticle.FX_IceArrow_Hit);
                    hitParticle.transform.position = hitPosition;

                    triggerTarget.DOShakePosition(0.1f);
                    CHMSkill.Instance.ApplySkillValue(eSkill, caster, new List<Transform> { triggerTarget }, effectData);
                }
                break;
            default:
                {
                    CHMSkill.Instance.ApplySkillValue(eSkill, caster, new List<Transform> { triggerTarget }, effectData);
                }
                break;
        }
    }

    //# 해당하는 방향으로 파티클 이동
    async Task MoveDirection(Vector3 direction, float speed, float effectTime, GameObject particleObj)
    {
        float time = 0;
        while (_cancleTokenSource.Token.IsCancellationRequested == false && time <= effectTime)
        {
            if (particleObj == null)
                break;

            particleObj.transform.position += direction.normalized * speed * Time.deltaTime;

            time += Time.deltaTime;
            await Task.Delay((int)(Time.deltaTime * 1000f));
        }
    }

    //# 해당하는 위치로 파티클 이동
    async Task MoveTrasnform(Transform startTransform, Transform endTransform, float speed, float effectTime, float offset, GameObject particleObj)
    {
        if (startTransform == null || endTransform == null)
            return;

        float time = 0;

        //# 일정한 속도로 일정 시간 동안 타겟에게 다가감
        if (speed >= 0 && effectTime >= 0)
        {
            while (_cancleTokenSource.Token.IsCancellationRequested == false && time <= effectTime)
            {
                if (startTransform == null || endTransform == null || particleObj == null) break;

                var direction = endTransform.position - startTransform.position;
                direction.y = 0f;

                particleObj.transform.forward = direction;
                particleObj.transform.position += direction.normalized * speed * Time.deltaTime;

                var posParticle = particleObj.transform.position;
                var posEnd = endTransform.position;
                posParticle.y = 0f;
                posEnd.y = 0f;

                if (Vector3.Distance(posParticle, posEnd) <= offset) break;

                time += Time.deltaTime;
                await Task.Delay((int)(Time.deltaTime * 1000f));
            }
        }
        //# 일정한 속도로 타겟에게 offset 거리가 될 때까지 다가감
        else if (speed >= 0 && effectTime < 0)
        {
            while (_cancleTokenSource.Token.IsCancellationRequested == false)
            {
                if (startTransform == null || endTransform == null || particleObj == null) break;

                var direction = endTransform.position - startTransform.position;
                direction.y = 0f;

                particleObj.transform.forward = direction;
                particleObj.transform.position += direction.normalized * speed * Time.deltaTime;

                var posParticle = particleObj.transform.position;
                var posEnd = endTransform.position;
                posParticle.y = 0f;
                posEnd.y = 0f;

                if (Vector3.Distance(posParticle, posEnd) <= offset) break;

                await Task.Delay((int)(Time.deltaTime * 1000f));
            }
        }
        //# 타겟에게 일정 시간동안 정해진 거리를 유지한채 다가감
        else if (speed < 0 && effectTime >= 0)
        {
            while (_cancleTokenSource.Token.IsCancellationRequested == false && time <= effectTime)
            {
                if (startTransform == null || endTransform == null || particleObj == null) break;

                var direction = (endTransform.position - startTransform.position).normalized;
                direction.y = 0f;

                var posParticle = particleObj.transform.position;
                var posEnd = endTransform.position;
                posParticle.y = 0f;
                posEnd.y = 0f;

                var distance = Vector3.Distance(posParticle, posEnd);

                particleObj.transform.forward = direction;
                particleObj.transform.position += direction * (distance - offset);

                time += Time.deltaTime;
                await Task.Delay((int)(Time.deltaTime * 1000f));
            }
        }
    }

    //# 타겟 에어본 시킴
    void TargetAirborne(Transform target)
    {
        var unitBase = target.GetComponent<CHUnitData>();
        if (unitBase == null)
            return;

        var seq = target.DOJump(target.transform.position, 5f, 1, 3f).OnComplete(() =>
        {
            unitBase?.SetAirborne(false);
        });

        unitBase?.SetAirborne(true, seq);
    }
}