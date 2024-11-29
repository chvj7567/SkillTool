using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;
using static DefClass;
using static DefEnum;
using UnityEngine.Rendering.Universal;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class CHMSkill : CHSingleton<CHMSkill>
{
    Dictionary<DefEnum.ESkill, SkillData> _dicSkillData = new Dictionary<DefEnum.ESkill, SkillData>();

    CancellationTokenSource _cancleTokenSource;

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
        for (int i = 0; i < Enum.GetValues(typeof(ESkill)).Length; ++i)
        {
            var skill = (DefEnum.ESkill)i;

            CHMResource.Instance.LoadSkillData(skill, (skillData) =>
            {
                if (skillData == null)
                    return;

                _dicSkillData.Add(skill, skillData);
            });
        }
    }

    public void Clear()
    {
        _dicSkillData.Clear();

        if (_cancleTokenSource != null && _cancleTokenSource.IsCancellationRequested == false)
        {
            _cancleTokenSource.Cancel();
        }
    }
    #endregion

    //# 스킬 데이터 확인
    public SkillData GetSkillData(DefEnum.ESkill eSkill)
    {
        if (_dicSkillData.ContainsKey(eSkill) == false)
            return null;

        return _dicSkillData[eSkill];
    }

    //# 타겟의 레이어 확인
    public LayerMask GetTargetMask(int myLayer, DefEnum.ETargetMask eTargetMask)
    {
        LayerMask myLayerMask = 1 << myLayer;
        LayerMask enemyLayerMask;
        if (LayerMask.LayerToName(myLayer) == "Red")
        {
            enemyLayerMask = LayerMask.GetMask("Blue");
        }
        else
        {
            enemyLayerMask = LayerMask.GetMask("Red");
        }

        switch (eTargetMask)
        {
            case DefEnum.ETargetMask.Me:
            case DefEnum.ETargetMask.MyTeam:
                return myLayerMask;
            case DefEnum.ETargetMask.Enemy:
                return enemyLayerMask;
            case DefEnum.ETargetMask.MyTeam_Enemy:
                return myLayerMask | enemyLayerMask;
            default:
                return -1;
        }
    }

    //# 지정 범위 내의 타겟 정보 확인
    public List<TargetInfo> GetTargetInfoListInRange(Vector3 originPos, Vector3 direction, LayerMask targetMask, float range, float viewAngle = 360f)
    {
        List<TargetInfo> targetInfoList = new List<TargetInfo>();

        //# 범위내에 있는 타겟들 확인
        Collider[] targets = Physics.OverlapSphere(originPos, range, targetMask);

        float minDistance = float.MaxValue;

        foreach (Collider target in targets)
        {
            Transform targetTr = target.transform;
            Vector3 targetDir = (targetTr.position - originPos).normalized;

            //# 시야각에 걸리는지 확인
            if (Vector3.Angle(direction, targetDir) < viewAngle / 2)
            {
                float distance = Vector3.Distance(originPos, targetTr.position);

                var unitBase = target.GetComponent<CHUnitBase>();

                //# 타겟이 살아있으면 타겟으로 지정
                if (unitBase != null && unitBase.IsDie == false)
                {
                    //# 제일 짧은 거리에 있는 타겟을 리스트의 첫번째로
                    if (minDistance > distance)
                    {
                        minDistance = distance;

                        targetInfoList.Insert(0, new TargetInfo
                        {
                            target = target.gameObject,
                            distance = distance,
                        });
                    }
                    else
                    {
                        targetInfoList.Add(new TargetInfo
                        {
                            target = target.gameObject,
                            distance = distance,
                        });
                    }
                }
            }
        }

        return targetInfoList;
    }

    //# 타겟 정보의 Transform 정보 확인
    public List<Transform> GetTargetTransformList(List<TargetInfo> liTargetInfo)
    {
        if (liTargetInfo == null)
            return new List<Transform>();

        List<Transform> targetTransformList = new List<Transform>();
        foreach (TargetInfo targetInfo in liTargetInfo)
        {
            targetTransformList.Add(targetInfo.target.transform);
        }

        return targetTransformList;
    }

    //# 타겟 정보의 Transform 정보 확인
    public List<Transform> GetTargetTransformList(TargetInfo targetInfo)
    {
        if (targetInfo == null)
            return new List<Transform>();

        List<Transform> targetTransformList = new List<Transform>();
        targetTransformList.Add(targetInfo.target.transform);

        return targetTransformList;
    }

    //# 해당 스킬을 쓸 수 있는지 확인
    bool CheckUseSkill(DefEnum.ESkill eSkill, CHUnitBase casterUnit, SkillData skillInfo)
    {
        switch (skillInfo.eSkillCost)
        {
            case DefEnum.ESkillCost.None:
                {
                    return true;
                }
            case DefEnum.ESkillCost.Fixed_HP:
                {
                    if (casterUnit.GetCurrentHp() >= skillInfo.cost)
                    {
                        casterUnit.ChangeHp(eSkill, casterUnit, Extension.ReverseValue(skillInfo.cost), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            case DefEnum.ESkillCost.Percent_MaxHP:
                {
                    var costValue = casterUnit.GetCurrentMaxHp() * skillInfo.cost / 100f;

                    if (casterUnit.GetCurrentHp() >= costValue)
                    {
                        casterUnit.ChangeHp(eSkill, casterUnit, Extension.ReverseValue(costValue), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            case DefEnum.ESkillCost.Percent_RemainHP:
                {
                    var costValue = casterUnit.GetCurrentHp() * skillInfo.cost / 100f;

                    if (casterUnit.GetCurrentHp() >= costValue)
                    {
                        casterUnit.ChangeHp(eSkill, casterUnit, Extension.ReverseValue(costValue), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            case DefEnum.ESkillCost.Fixed_MP:
                {
                    if (casterUnit.GetCurrentMp() >= skillInfo.cost)
                    {
                        casterUnit.ChangeMp(eSkill, casterUnit, Extension.ReverseValue(skillInfo.cost), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            case DefEnum.ESkillCost.Percent_MaxMP:
                {
                    var costValue = casterUnit.GetCurrentMaxMp() * skillInfo.cost / 100f;

                    if (casterUnit.GetCurrentMp() >= costValue)
                    {
                        casterUnit.ChangeMp(eSkill, casterUnit, Extension.ReverseValue(costValue), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            case DefEnum.ESkillCost.Percent_RemainMP:
                {
                    var costValue = casterUnit.GetCurrentMp() * skillInfo.cost / 100f;

                    if (casterUnit.GetCurrentMp() >= costValue)
                    {
                        casterUnit.ChangeMp(eSkill, casterUnit, Extension.ReverseValue(costValue), DefEnum.EDamageType1.None);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            default:
                return false;
        }
    }

    //# 스킬 시전
    public async void CreateSkill(SkillLocationInfo skillLocationInfo, DefEnum.ESkill eSkill)
    {
        //# 스킬 시전자가 죽었으면 스킬 발동 X
        var isDeath = skillLocationInfo.trCaster.GetComponent<CHUnitBase>().IsDie;
        if (isDeath)
            return;

        //# 스킬 데이터 확인
        var skillData = GetSkillData(eSkill);
        if (skillData == null)
            return;

        var casterUnit = skillLocationInfo.trCaster.GetComponent<CHUnitBase>();
        if (casterUnit != null)
        {
            // 스킬을 사용할 비용이 있는지 확인
            if (CheckUseSkill(eSkill, casterUnit, skillData) == false)
                return;
        }

        //# 스킬의 각 이펙트 정보 확인
        foreach (var effectInfo in skillData.liEffectData)
        {
            //# 해당 위치로 움직일지 여부
            if (effectInfo.moveToPos)
            {
                float distance = Vector3.Distance(skillLocationInfo.trCaster.position, skillLocationInfo.posSkill);
                effectInfo.startDelay = (distance + effectInfo.offsetToTarget) / effectInfo.moveSpeed;
            }

            //# 스킬 시전 딜레이 시간 전에 데칼로 스킬 시전 구역 알려줌
            if (effectInfo.onDecal && (Mathf.Approximately(0f, effectInfo.startDelay) == false) && effectInfo.onDecal)
            {
                await CreateDecal(skillLocationInfo, effectInfo, skillData.isTargeting);
            }
            else
            {
                //# 스킬 시전자가 해당 위치로 이동하는지 여부(ex:말파 궁)
                if (effectInfo.moveToPos)
                {
                    float time = 0f;
                    while (time <= effectInfo.startDelay)
                    {
                        skillLocationInfo.trCaster.position += skillLocationInfo.dirSkill.normalized * effectInfo.moveSpeed * Time.deltaTime;

                        time += Time.deltaTime;
                        await Task.Delay((int)(Time.deltaTime * 1000f));
                    }
                }
                else
                {
                    await Task.Delay((int)(effectInfo.startDelay * 1000f));
                }
            }

            if (skillLocationInfo.trCaster == null)
                return;

            //# 스킬 충돌 범위 생성
            CreateSkillCollision(eSkill, skillLocationInfo, effectInfo, skillData.isTargeting);
        }
    }

    //# 스킬 효과 적용(데미지, 힐 등)
    public void ApplySkillValue(DefEnum.ESkill eSkill, Transform caster, List<Transform> liTarget, SkillData.EffectData effectData)
    {
        var casterUnit = caster.GetComponent<CHUnitBase>();
        foreach (var target in liTarget)
        {
            if (target == null)
                continue;

            var targetUnit = target.GetComponent<CHUnitBase>();
            if (targetUnit != null)
            {
                ApplyEffectType(eSkill, casterUnit, targetUnit, effectData);
            }
        }
    }

    //# 스킬의 콜리젼 생성
    void CreateSkillCollision(DefEnum.ESkill eSkill, SkillLocationInfo skillLocationInfo, SkillData.EffectData effectData, bool isTargeting)
    {
        SkillLocationInfo copySkillLocationInfo = skillLocationInfo.Copy();
        LayerMask targetMask = GetTargetMask(copySkillLocationInfo.trCaster.gameObject.layer, effectData.eTargetMask);

        List<TargetInfo> liTargetInfo = new List<TargetInfo>();
        List<Transform> liTarget = new List<Transform>();

        //# 논타겟팅 스킬
        if (isTargeting == false)
        {
            liTargetInfo = GetTargetInfoListInRange(copySkillLocationInfo.posSkill, copySkillLocationInfo.dirSkill, targetMask, effectData.sphereRadius, effectData.collisionAngle);
            liTarget = GetTargetTransformList(liTargetInfo);

            //# 논타겟팅 스킬은 생성 시에 타겟이 없을 수도 있음
            if (liTargetInfo == null || liTargetInfo.Count <= 0)
            {
                if (effectData.createCasterPosition)
                {
                    //# 파티클을 시전자 위치에서 생성
                    copySkillLocationInfo.posSkill = copySkillLocationInfo.posCaster;
                    copySkillLocationInfo.dirSkill = copySkillLocationInfo.dirCaster;
                }

                if (effectData.createOnEmpty)
                {
                    CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                        new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { copySkillLocationInfo.dirSkill }, effectData);
                }

                return;
            }
        }
        //# 타겟팅 스킬
        else
        {
            liTargetInfo = GetTargetInfoListInRange(copySkillLocationInfo.trTarget.position, copySkillLocationInfo.trTarget.forward, targetMask, effectData.sphereRadius, effectData.collisionAngle);
            liTarget = GetTargetTransformList(liTargetInfo);

            if (liTargetInfo == null || liTargetInfo.Count <= 0)
            {
                Debug.Log("Targeting Skill : No Target Error");
                return;
            }
        }

        //# 파티클을 시전자 위치에서 생성
        if (effectData.createCasterPosition)
        {
            copySkillLocationInfo.posSkill = copySkillLocationInfo.posCaster;
            copySkillLocationInfo.dirSkill = copySkillLocationInfo.dirCaster;
        }

        //# eSkillTarget 타입에 따라 파티클 생성
        switch (effectData.eTarget)
        {
            case DefEnum.ESkillTarget.Position:
                {
                    if (effectData.duplication)
                    {
                        foreach (var target in liTarget)
                        {
                            CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                                new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { copySkillLocationInfo.dirSkill }, effectData);
                        }
                    }
                    else
                    {
                        CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                            new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { copySkillLocationInfo.dirSkill }, effectData);
                    }
                }
                break;
            case DefEnum.ESkillTarget.Target_One:
                {
                    Transform targetOne = null;

                    foreach (var target in liTarget)
                    {
                        if (target == copySkillLocationInfo.trTarget)
                        {
                            targetOne = target;
                            break;
                        }
                    }
                    
                    if (targetOne == null)
                    {
                        targetOne = liTarget.Last();
                    }

                    Vector3 direction = (targetOne.position - copySkillLocationInfo.trCaster.position).normalized;

                    //# 맞은 타겟 수 만큼 파티클 중복 여부
                    if (effectData.duplication)
                    {
                        if (effectData.createCasterPosition == false)
                        {
                            foreach (var target in liTarget)
                            {
                                CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { targetOne },
                                    new List<Vector3> { targetOne.position }, new List<Vector3> { direction }, effectData);
                            }
                        }
                        else
                        {
                            foreach (var target in liTarget)
                            {
                                CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                                    new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { direction }, effectData);
                            }
                        }
                    }
                    else
                    {
                        if (effectData.createCasterPosition == false)
                        {
                            CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { targetOne },
                                new List<Vector3> { targetOne.position }, new List<Vector3> { direction }, effectData);
                        }
                        else
                        {
                            CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                                new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { direction }, effectData);
                        }
                    }
                }
                break;
            case DefEnum.ESkillTarget.Target_All:
                {
                    List<Vector3> liParticlePos = new List<Vector3>();
                    List<Vector3> liParticleDir = new List<Vector3>();

                    if (effectData.createCasterPosition == false)
                    {
                        for (int i = 0; i < liTarget.Count; ++i)
                        {
                            liParticlePos.Add(liTarget[i].position);
                            liParticleDir.Add((liTarget[i].position - copySkillLocationInfo.trCaster.position).normalized);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < liTarget.Count; ++i)
                        {
                            liParticlePos.Add(copySkillLocationInfo.trCaster.position);
                            liParticleDir.Add((liTarget[i].position - copySkillLocationInfo.trCaster.position).normalized);
                        }
                    }

                    CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, liTarget, liParticlePos, liParticleDir, effectData);
                }
                break;
            default:
                {
                    CHMParticle.Instance.CreateParticle(eSkill, copySkillLocationInfo.trCaster, new List<Transform> { copySkillLocationInfo.trTarget },
                        new List<Vector3> { copySkillLocationInfo.posSkill }, new List<Vector3> { copySkillLocationInfo.dirSkill }, effectData);
                }
                break;
        }
    }

    //# 스킬 효과 적용
    void ApplyEffectType(DefEnum.ESkill eSkill, CHUnitBase casterUnit, CHUnitBase targetUnit, SkillData.EffectData effectData)
    {
        if (casterUnit == null || targetUnit == null || effectData == null)
            return;

        float skillValue = CalculateSkillDamage(casterUnit, targetUnit, effectData);

        //# 스킬 시전자 스탯
        float casterAttackPower = casterUnit.GetCurrentAttackPower();
        float casterDefensePower = casterUnit.GetCurrentDefensePower();

        //# 타겟 스탯
        float targetAttackPower = targetUnit.GetCurrentAttackPower();
        float targetDefensePower = targetUnit.GetCurrentDefensePower();

        switch (effectData.eStatModifyType)
        {
            case DefEnum.EStatModifyType.Hp_Up:
                Debug.Log($"HpUp : {skillValue}");
                targetUnit.ChangeHp(eSkill, casterUnit, skillValue, effectData.eDamageType1);
                break;
            case DefEnum.EStatModifyType.Hp_Down:
                {
                    // 데미지 계산 : 스킬 데미지 + 스킬 시전자 공격력 - 타겟 방어력
                    float totalValue = skillValue + casterAttackPower - targetDefensePower;

                    // 데미지가 -이면 데미지를 줄 수 없다는 뜻
                    if (totalValue < 0)
                    {
                        totalValue = 0f;
                    }
                    Debug.Log($"HpDown : {totalValue}");
                    targetUnit.ChangeHp(eSkill, casterUnit, Extension.ReverseValue(totalValue), effectData.eDamageType1);
                }
                break;
            /*case Defines.EStatModifyType.Mp_Up:
                _targetUnit.ChangeMp(_casterUnit, skillValue, _effectData.eDamageType1);
                break;
            case Defines.EStatModifyType.Mp_Down:
                _targetUnit.ChangeMp(_casterUnit, CHUtil.ReverseValue(skillValue), _effectData.eDamageType1);
                break;
            case Defines.EStatModifyType.AttackPower_Up:
                _targetUnit.ChangeAttackPower(_casterUnit, skillValue, _effectData.eDamageType1);
                break;
            case Defines.EStatModifyType.AttackPower_Down:
                _targetUnit.ChangeAttackPower(_casterUnit, CHUtil.ReverseValue(skillValue), _effectData.eDamageType1);
                break;
            case Defines.EStatModifyType.DefensePower_Up:
                _targetUnit.ChangeDefensePower(_casterUnit, skillValue, _effectData.eDamageType1);
                break;
            case Defines.EStatModifyType.DefensePower_Down:
                _targetUnit.ChangeDefensePower(_casterUnit, CHUtil.ReverseValue(skillValue), _effectData.eDamageType1);
                break;*/
            default:
                break;
        }
    }

    //# 스킬 데미지 계산
    float CalculateSkillDamage(CHUnitBase casterUnit, CHUnitBase targetUnit, SkillData.EffectData effectData)
    {
        if (casterUnit == null || targetUnit == null || effectData == null)
            return 0f;

        float damage = effectData.damage;

        var levelData = casterUnit.MyLevelData;
        if (levelData != null)
        {
            damage += levelData.damage;
        }

        var itemData = casterUnit.GetOriginItem1Data();
        if (itemData != null)
        {
            damage += itemData.damage;
        }
        
        //# 데미지 타입에 따라 구분(고정 데미지, 퍼센트 데미지 등)
        switch (effectData.eDamageType2)
        {
            case DefEnum.EDamageType2.Fixed:
                return damage;
            case DefEnum.EDamageType2.Percent_Me_MaxHp:
                return casterUnit.GetCurrentMaxHp() * damage / 100f;
            case DefEnum.EDamageType2.Percent_Me_RemainHp:
                return casterUnit.GetCurrentHp() * damage / 100f;
            case DefEnum.EDamageType2.Percent_Target_MaxHp:
                return targetUnit.GetCurrentMaxHp() * damage / 100f;
            case DefEnum.EDamageType2.Percent_Target_RemainHp:
                return targetUnit.GetCurrentHp() * damage / 100f;
            default:
                return 0f;
        }
    }

    //# 스킬 범위 DP
    async Task CreateDecal(SkillLocationInfo skillLocationInfo, SkillData.EffectData effectData, bool isTargeting)
    {
        GameObject decalObject = null;

        switch (effectData.eCollision)
        {
            case DefEnum.ECollision.Sphere:
                {
                    CHMResource.Instance.InstantiateDecal(DefEnum.EDecal.Round, (decal) =>
                    {
                        decalObject = decal;
                        decalObject.SetActive(false);
                        decalObject.GetOrAddComponent<CHPoolable>();

                        if (isTargeting)
                        {
                            decalObject.transform.SetParent(skillLocationInfo.trTarget.transform);
                            decalObject.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            decalObject.transform.position = skillLocationInfo.posSkill;
                            decalObject.transform.forward = skillLocationInfo.dirSkill;
                        }

                        decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                        var decalProjector = decal.GetComponent<DecalProjector>();
                        if (decalProjector != null)
                        {
                            decalProjector.size = Vector3.one * effectData.sphereRadius * 2;
                        }
                    });
                }
                break;
            case DefEnum.ECollision.Box:
                break;
        }

        await CreateTimeDecal(skillLocationInfo, decalObject, effectData, isTargeting);
    }

    async Task CreateTimeDecal(SkillLocationInfo skillLocationInfo, GameObject areaDecal, SkillData.EffectData effectData, bool isTargeting)
    {
        GameObject decalObject = null;

        switch (effectData.eCollision)
        {
            case DefEnum.ECollision.Sphere:
                {
                    CHMResource.Instance.InstantiateDecal(DefEnum.EDecal.Round, (decal) =>
                    {
                        decalObject = decal;
                        decalObject.SetActive(false);
                        decalObject.GetOrAddComponent<CHPoolable>();
                    });

                    if (isTargeting)
                    {
                        decalObject.transform.SetParent(skillLocationInfo.trTarget.transform);
                        decalObject.transform.localPosition = Vector3.zero;
                    }
                    else
                    {
                        decalObject.transform.position = skillLocationInfo.posSkill;
                        decalObject.transform.forward = skillLocationInfo.dirSkill;
                    }

                    decalObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    var decalProjector = decalObject.GetComponent<DecalProjector>();
                    if (decalProjector != null)
                    {
                        float time = 0;
                        while (_cancleTokenSource.IsCancellationRequested == false && time <= effectData.startDelay)
                        {
                            var curValue = Mathf.Lerp(0, effectData.sphereRadius * 2, time / effectData.startDelay);

                            if (decalProjector == null) break;

                            decalProjector.size = Vector3.one * curValue;
                            time += Time.deltaTime;

                            if (effectData.moveToPos)
                            {
                                skillLocationInfo.trCaster.position += skillLocationInfo.dirSkill.normalized * effectData.moveSpeed * Time.deltaTime;
                            }

                            await Task.Delay((int)(Time.deltaTime * 1000f));
                        }

                        CHMResource.Instance.Destroy(decalObject);
                        CHMResource.Instance.Destroy(areaDecal);
                    }
                }
                break;
            case DefEnum.ECollision.Box:
                break;
            default:
                break;
        }
    }
}
