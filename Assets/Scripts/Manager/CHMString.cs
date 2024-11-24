using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CHMJson
{
    public string GetString(int stringID)
    {
        if (_dicStringData.TryGetValue(stringID, out var value))
            return value;
        return "";
    }
}
