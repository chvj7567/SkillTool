using System;
using UniRx;
using UnityEngine;

public class CHSphereCollision : MonoBehaviour
{
    public SphereCollider sphereCollider;
    float stayTickTime = -1f;
    float stayTickLastTime = -1f;

    Subject<Collider> subjectEnter = new Subject<Collider>();
    Subject<Collider> subjectStay = new Subject<Collider>();
    Subject<Collider> subjectExit = new Subject<Collider>();
    public IObservable<Collider> OnEnter => subjectEnter;
    public IObservable<Collider> OnStay => subjectStay;
    public IObservable<Collider> OnExit => subjectExit;

    IDisposable disposeEnter;
    IDisposable disposeStay;
    IDisposable disposeExit;

    bool useStay = false;

    Transform trCaster;
    SkillData.EffectData effectData;

    private void Update()
    {
        if (useStay)
        {
            if (stayTickLastTime >= 0f && stayTickLastTime < stayTickTime)
            {
                stayTickLastTime += Time.deltaTime;
            }
            else
            {
                stayTickLastTime = -1f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        subjectEnter.OnNext(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (useStay && stayTickLastTime < 0f)
        {
            stayTickLastTime = 0.0001f;

            subjectStay.OnNext(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        subjectExit.OnNext(other);
    }

    public void Init(Transform _trCaster, SkillData.EffectData _effectData)
    {
        trCaster = _trCaster;
        effectData = _effectData;

        sphereCollider = gameObject.GetOrAddComponent<SphereCollider>();
        SetTriggerCollider();

        gameObject.layer = 2;

        if (_effectData.stayTickTime >= 0f)
        {
            stayTickTime = _effectData.stayTickTime;
            stayTickLastTime = -1f;
            useStay = true;
        }
        else
        {
            useStay = false;
        }
    }

    public void TriggerEnterCallback(IDisposable _disposable)
    {
        // 풀링 사용시 구독 중독 방지
        if (disposeEnter != null)
        {
            disposeEnter.Dispose();
            disposeEnter = _disposable;
        }
        else
        {
            disposeEnter = _disposable;
        }
    }

    public void TriggerStayCallback(IDisposable _disposable)
    {
        // 풀링 사용시 구독 중독 방지
        if (disposeStay != null)
        {
            disposeStay.Dispose();
            disposeStay = _disposable;
        }
        else
        {
            disposeStay = _disposable;
        }
    }

    public void TriggerExitCallback(IDisposable _disposable)
    {
        // 풀링 사용시 구독 중독 방지
        if (disposeExit != null)
        {
            disposeExit.Dispose();
            disposeExit = _disposable;
        }
        else
        {
            disposeExit = _disposable;
        }
    }

    void SetTriggerCollider()
    {
        sphereCollider.isTrigger = true;

        switch (effectData.EParticle)
        {
            /*case Defines.EParticle.FX_Poison:
                {
                    sphereCollider.radius = effectData.sphereRadius;
                    sphereCollider.center = new Vector3(0f, 0f, 0f);
                }
                break;
            case Defines.EParticle.FX_Arrow_impact2:
                {
                    sphereCollider.radius = effectData.sphereRadius;
                    sphereCollider.center = new Vector3(0f, -23f, 0f);
                }
                break;*/
            case DefEnum.EParticle.SlashHit:
                {
                    sphereCollider.radius = effectData.sphereRadius;
                    sphereCollider.center = new Vector3(0f, 3f, 0f);
                }
                break;
            default:
                {
                    sphereCollider.radius = effectData.sphereRadius;
                    sphereCollider.center = new Vector3(0f, 0f, 0f);
                }
                break;
        }
    }
}
