using System;
using System.Collections.Generic;
using static DefEnum;

public class CHMItem : CHSingleton<CHMItem>
{
    #region Private Argument
    Dictionary<DefEnum.EItem, ItemData> _dicItemData = new Dictionary<DefEnum.EItem, ItemData>();
    #endregion

    #region Initialize
    public bool Initialize => _initialize;

    bool _initialize = false;

    public void Init()
    {
        if (_initialize)
            return;

        _initialize = true;

        for (int i = 0; i < Enum.GetValues(typeof(EItem)).Length; ++i)
        {
            var item = (EItem)i;

            CHMResource.Instance.LoadItemData(item, (_) =>
            {
                if (_ == null) return;

                _dicItemData.Add(item, _);
            });
        }
    }

    public void Clear()
    {
        _dicItemData.Clear();
    }
    #endregion

    #region Getter
    public ItemData GetItemData(DefEnum.EItem _item)
    {
        if (_dicItemData.ContainsKey(_item) == false) return null;

        return _dicItemData[_item];
    }
    #endregion
}
