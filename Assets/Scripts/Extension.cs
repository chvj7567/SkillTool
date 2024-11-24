using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadOnlyAttribute : PropertyAttribute {}

public static class Extension
{
    /// <summary>
    /// 널 또는 비어있는 리스트이면 true
    /// </summary>
    public static bool IsNullOrEmpty<T>(this List<T> list)
    {
        if (list == null || list.Count <= 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// T 컴포넌트를 가져오거나 없으면 추가하여 가져옴
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject obj) where T : UnityEngine.Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
            component = obj.AddComponent<T>();
        return component;
    }

    /// <summary>
    /// 자식 오브젝트 중 T 컴포넌트가 있다면 가져옴
    /// </summary>
    public static T FindChild<T>(this GameObject obj, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (obj == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform transform = obj.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach (T component in obj.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    /// <summary>
    /// int 값의 부호를 바꿔서 반환
    /// </summary>
    public static int ReverseValue(int value)
    {
        return -value;
    }

    /// <summary>
    /// float 값의 부호를 바꿔서 반환
    /// </summary>
    public static float ReverseValue(float value)
    {
        return -value;
    }

    /// <summary>
    /// 기준 축에 따라서 angle 값 만큼 회전한 Vector3 값 반환
    /// </summary>
    public static Vector3 Angle(this Transform transform, float angle, DefEnum.EStandardAxis standardAxis)
    {
        switch (standardAxis)
        {
            case DefEnum.EStandardAxis.X:
                {
                    angle += transform.eulerAngles.y + 90f;
                }
                break;
            case DefEnum.EStandardAxis.Z:
                {
                    angle += transform.eulerAngles.y;
                }
                break;
        }
        
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad));
    }
}
