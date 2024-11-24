using System;
using System.Collections.Generic;
using static DefEnum;

public class CHMLevel : CHSingleton<CHMLevel>
{
    #region Private Argument
    Dictionary<DefEnum.EUnit, List<LevelData>> _dicLevelData = new Dictionary<DefEnum.EUnit, List<LevelData>>();
    #endregion

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
            List<LevelData> liLevelData = new List<LevelData>();
            var unit = (EUnit)i;

            if (unit == EUnit.None)
                continue;

            for (int j = 1; j < Enum.GetValues(typeof(ELevel)).Length + 1; ++j)
            {
                var level = (ELevel)j;

                CHMResource.Instance.LoadLevelData(unit, level, (_) =>
                {
                    if (_ == null) return;

                    liLevelData.Add(_);
                });
            }

            _dicLevelData.Add(unit, liLevelData);
        }
    }
    
    public void Clear()
    {
        _dicLevelData.Clear();
    }
    #endregion

    #region Getter
    public LevelData GetLevelData(DefEnum.EUnit _unit, DefEnum.ELevel _level)
    {
        if (_dicLevelData.ContainsKey(_unit) == false)
            return null;

        return _dicLevelData[_unit][(int)_level - 1];
    }
    #endregion
}
