using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static SkillData;

public static class Util
{
    public static async Task<bool> Delay(float delayTime, CancellationTokenSource token)
    {
        await Task.Delay((int)(delayTime * 1000f));

        if (token == null || token.IsCancellationRequested)
            return false;

        return true;
    }
}
