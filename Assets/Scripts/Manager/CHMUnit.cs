using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DefEnum;

public class CHMUnit : CHSingleton<CHMUnit>
{
    Dictionary<EUnit, UnitData> _dicUnitData = new Dictionary<EUnit, UnitData>();
    List<Material> _liMaterial = new List<Material>();
    List<GameObject> _liUnit = new List<GameObject>();
    GameObject _originBall = null;
    GameObject _originGaugeBar = null;
    GameObject _originDamageText = null;

    public int RedIndex { get; private set; }

    public int BlueIndex { get; private set; }

    #region Initialize
    public bool Initialize => _initialize;

    bool _initialize = false;

    public void Init()
    {
        if (_initialize)
            return;

        _initialize = true;

        for (int i = 0; i < Enum.GetValues(typeof(EUnit)).Length; ++i)
        {
            var unit = (EUnit)i;

            CHMResource.Instance.LoadUnitData(unit, (_) =>
            {
                if (_ == null) return;

                _dicUnitData.Add(unit, _);
            });

            CHMResource.Instance.LoadUnitMaterial((DefEnum.EUnit)i, (mat) =>
            {
                _liMaterial.Add(mat);
            });
        }

        CHMResource.Instance.LoadOriginBall((ball) =>
        {
            _originBall = ball;
        });

        CHMResource.Instance.LoadOriginGaugeBar((gaugeBar) =>
        {
            _originGaugeBar = gaugeBar;
        });

        CHMResource.Instance.LoadOriginDamageText((damageText) =>
        {
            _originDamageText = damageText;
        });
    }

    public void Clear()
    {
        _dicUnitData.Clear();
        _liMaterial.Clear();

        RemoveUnitAll();
    }
    #endregion

    #region Setter
    public void SetUnit(GameObject unit, DefEnum.EUnit eUnit)
    {
        if (unit == null)
            return;

        CHUnitBase unitBase = unit.GetComponent<CHUnitBase>();
        if (unitBase != null)
        {
            unitBase.UnitType = eUnit;
            SetColor(unit, eUnit);
        }
    }

    public void SetColor(GameObject unit, DefEnum.EUnit eUnit)
    {
        if (unit == null)
            return;

        int index = (int)eUnit;
        var arrMesh = unit.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (arrMesh != null && index < _liMaterial.Count)
        {
            foreach (var mesh in arrMesh)
            {
                mesh.material = _liMaterial[index];
            }
        }
    }

    public void SetLayer(GameObject unit, DefEnum.ELayer eLayer)
    {
        if (unit == null)
            return;

        unit.layer = (int)eLayer;
    }

    public void SetTargetMask(GameObject unit, DefEnum.ELayer eLayer)
    {
        if (unit == null)
            return;

        CHTargetTracker targetTracker = unit.GetComponent<CHTargetTracker>();
        if (targetTracker != null)
        {
            targetTracker.SetTargetMask(1 << (int)eLayer);
        }
    }
    #endregion

    #region Getter
    public GameObject GetOriginBall()
    {
        if (_originBall == null)
            throw new NullReferenceException();

        return _originBall;
    }

    public GameObject GetOriginGaugeBar()
    {
        if (_originGaugeBar == null)
            throw new NullReferenceException();

        return _originGaugeBar;
    }

    public GameObject GetOriginDamageText()
    {
        if (_originDamageText == null)
            throw new NullReferenceException();

        return _originDamageText;
    }

    public UnitData GetUnitData(DefEnum.EUnit eUnit)
    {
        if (_dicUnitData.ContainsKey(eUnit) == false)
            return null;

        return _dicUnitData[eUnit];
    }

    public List<UnitData> GetUnitDataAll()
    {
        return _dicUnitData.Values.ToList();
    }
    #endregion

    public void CreateUnit(Transform parent, DefEnum.EUnit eUnit, DefEnum.ELayer eTeamLayer, DefEnum.ELayer eTargetLayer, Vector3 position, List<CHTargetTracker> liTargetTracker = null, List<LayerMask> liTargetMask = null, bool onHpBar = true, bool onMpBar = false, bool onCoolTimeBar = false)
    {
        CHMResource.Instance.InstantiateRobot(eUnit, (ball) =>
        {
            if (ball == null)
                return;

            ball.transform.SetParent(parent);

            _liUnit.Add(ball);
            if (eTargetLayer == DefEnum.ELayer.Red)
            {
                ball.name = $"{eUnit}Unit(My) {RedIndex++}";
            }
            else
            {
                ball.name = $"{eUnit}Unit(Enemy) {BlueIndex++}";
            }

            SetUnit(ball, eUnit);
            SetLayer(ball, eTeamLayer);
            SetTargetMask(ball, eTargetLayer);

            var unitBase = ball.GetComponent<CHUnitBase>();
            if (unitBase != null)
            {
                unitBase.ShowHp = onHpBar;
                unitBase.ShowMp = onMpBar;
                unitBase.ShowCoolTime = onCoolTimeBar;
            }

            ball.transform.position = position;

            var targetTracker = ball.GetComponent<CHTargetTracker>();
            if (targetTracker != null)
            {
                if (liTargetTracker != null)
                    liTargetTracker.Add(targetTracker);
                if (liTargetMask != null)
                    liTargetMask.Add(targetTracker.TargetMask);
            }
        });
    }

    public void RemoveUnitAll()
    {
        foreach (var unit in _liUnit)
        {
            CHMResource.Instance.Destroy(unit);
        }

        _liUnit.Clear();
    }
}
