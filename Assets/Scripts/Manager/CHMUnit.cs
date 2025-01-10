using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DefEnum;

public class CHMUnit : CHSingleton<CHMUnit>
{
    int curUnitID = 0;
    Dictionary<EUnit, UnitData> _dicUnitData = new Dictionary<EUnit, UnitData>();
    List<Material> _liMaterial = new List<Material>();
    Dictionary<int, CHUnit> _dicUnit = new Dictionary<int, CHUnit>();
    GameObject _originGaugeBar = null;
    GameObject _originDamageText = null;

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
    public void SetUnit(CHUnit unit, DefEnum.EUnit eUnit)
    {
        unit.UnitType = eUnit;
        SetColor(unit, eUnit);
    }

    public void SetColor(CHUnit unit, DefEnum.EUnit eUnit)
    {
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

    public void SetLayer(CHUnit unit, DefEnum.ELayer eLayer)
    {
        unit.Layer = (int)eLayer;
    }

    public void SetTargetMask(CHUnit unit, DefEnum.ELayer eLayer)
    {
        unit.TargetMask = (int)eLayer;
    }
    #endregion

    #region Getter
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

    public void CreateUnit(Transform parent, DefEnum.EUnit eUnit, DefEnum.ELayer eTeamLayer, DefEnum.ELayer eTargetLayer, Vector3 position,
        bool onHpBar = true, bool onMpBar = false, bool onCoolTimeBar = false)
    {
        CHMResource.Instance.InstantiateRobot(eUnit, (obj) =>
        {
            CHUnit unit = obj?.GetComponent<CHUnit>();
            if (unit == null)
                return;

            unit.transform.SetParent(parent);

            _dicUnit.Add(++curUnitID, unit);
            if (eTeamLayer == DefEnum.ELayer.Red)
            {
                unit.name = $"{eUnit} Unit {curUnitID}(Red)";
            }
            else
            {
                unit.name = $"{eUnit} Unit {curUnitID}(Blue)";
            }

            SetUnit(unit, eUnit);
            SetLayer(unit, eTeamLayer);
            SetTargetMask(unit, eTargetLayer);

            var unitBase = unit.GetComponent<CHUnit>();
            if (unitBase != null)
            {
                unitBase.ShowHp = onHpBar;
                unitBase.ShowMp = onMpBar;
                unitBase.ShowCoolTime = onCoolTimeBar;
            }

            unit.transform.position = position;

            unit.Init(curUnitID);
        });
    }

    public void RemoveUnitAll()
    {
        foreach (var unit in _dicUnit)
        {
            CHMResource.Instance.Destroy(unit.Value.transform.gameObject);
        }

        _dicUnit.Clear();
    }
}
