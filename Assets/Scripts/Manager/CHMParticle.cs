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
    //# ��ƼŬ Ÿ�԰� ���� �ð� ���� ����
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

    //# Ÿ�� Ȯ��
    public bool IsTarget(int casterLayer, int targetLayer, DefEnum.ETargetMask eTargetMask)
    {
        var targetMask = CHMSkill.Instance.GetTargetMask(casterLayer, eTargetMask);

        return (1 << targetLayer & targetMask.value) != 0;
    }

    //# �ڵ� �ı��Ǵ� ��ƼŬ���� Ȯ��
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

        //# Ÿ���� �� ��ŭ ��ƼŬ ���� 
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

        //# �� ��ƼŬ�� �ݸ����� ��ġ�� ����
        for (int i = 0; i < liParticle.Count; ++i)
        {
            var obj = liParticle.ElementAtOrDefault(i);
            var target = liTarget.ElementAtOrDefault(i);
            var position = liParticlePos.ElementAtOrDefault(i);
            var direction = liParticleDir.ElementAtOrDefault(i);

            position.y = 0f;
            direction.y = 0f;

            obj.transform.position = position;

            //# ��ƼŬ�� ��� z���� ������ �ǵ���
            obj.transform.forward = direction;
            obj.transform.forward = obj.transform.Angle(effectData.effectAngle, DefEnum.EStandardAxis.Z);

            SetParticlePositionValue(eSkill, caster, target, obj, effectData);
            SetParticleCollision(eSkill, caster, target, obj, effectData);
        }
    }

    //# ��ƼŬ �ı�
    async void DestroyParticle(DefEnum.EParticle eParticle, GameObject particleObj)
    {
        if (await Util.Delay(_dicParticleTime[eParticle], _cancleTokenSource) == false)
            return;

        if (particleObj)
            CHMResource.Instance.Destroy(particleObj);
    }

    //# ��ƼŬ �ݸ��� ����
    async void SetParticleCollision(DefEnum.ESkill eSkill, Transform caster, Transform target, GameObject particleObj, SkillData.EffectData effectData)
    {
        if (effectData.eCollision == DefEnum.ECollision.None)
            return;

        //# ��ų ���� �� �����̰� �ִٸ� ������ ���� �ݸ��� ����
        if (await Util.Delay(effectData.triggerStartDelay, _cancleTokenSource) == false)
            return;

        var sphereCollision = particleObj.GetOrAddComponent<CHSphereCollision>();
        sphereCollision.Init(caster, effectData);
        sphereCollision.sphereCollider.enabled = true;

        //# ��ų�� ���� ��� ����
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

        //# ��ų�� �� ���� ���� ����
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

        //# ��ų�� �°� �ִ� ���� ����
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

        //# ��ų ���� �ð� ������ �ش� �ð���ŭ �ݸ��� ����
        if (effectData.triggerStayTime >= 0f)
        {
            if (await Util.Delay(effectData.triggerStayTime, _cancleTokenSource) == false)
                return;

            if (sphereCollision != null && sphereCollision.sphereCollider != null)
                sphereCollision.sphereCollider.enabled = false;
        }
    }

    //# ��ƼŬ ��ġ ����
    async void SetParticlePositionValue(DefEnum.ESkill eSkill, Transform caster, Transform target, GameObject particleObj, SkillData.EffectData effectData)
    {
        //# ����Ʈ�� �پ��־���ϴ� ��� SetParent�� �ع����� �ش� Ÿ���� �浹ü�� ������ �ȵǹǷ� Ÿ���� ����ٴϵ��� ����
        if (effectData.attach)
        {
            await MoveTrasnform(particleObj.transform, target, -1f, _dicParticleTime[effectData.EParticle], 0f, particleObj);
        }

        //# �� ����Ʈ���� ��ġ ������ �ʿ��� ���
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

    //# �� ��ƼŬ�� Ʈ���� �� Ÿ�ٵ� ó�� ����(������, ��, CC�� ��)
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

    //# �ش��ϴ� �������� ��ƼŬ �̵�
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

    //# �ش��ϴ� ��ġ�� ��ƼŬ �̵�
    async Task MoveTrasnform(Transform startTransform, Transform endTransform, float speed, float effectTime, float offset, GameObject particleObj)
    {
        if (startTransform == null || endTransform == null)
            return;

        float time = 0;

        //# ������ �ӵ��� ���� �ð� ���� Ÿ�ٿ��� �ٰ���
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
        //# ������ �ӵ��� Ÿ�ٿ��� offset �Ÿ��� �� ������ �ٰ���
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
        //# Ÿ�ٿ��� ���� �ð����� ������ �Ÿ��� ������ä �ٰ���
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

    //# Ÿ�� ��� ��Ŵ
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