using UnityEngine;
using System;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif



public class CHMResource : CHSingleton<CHMResource>
{
    #region Main
    public void InstantiateAsObservable<T>(string bundleName, string assetName, Action<T> callback = null) where T : UnityEngine.Object
    {
        Action<T> callbackTemp = original =>
        {
            if (original == null)
            {
                callback(null);
            }
            else
            {
                if (typeof(T) == typeof(GameObject))
                {
                    GameObject go = original as GameObject;
                    T t = Instantiate(go) as T;
                    if (callback != null) callback(t);
                }
                else
                {
                    if (callback != null) callback(GameObject.Instantiate(original));
                }
            }
        };

        LoadAsset<T>(bundleName, assetName, callbackTemp);
    }

    void LoadAsset<T>(string bundleName, string assetName, Action<T> callback) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        CHMAssetBundle.Instance.LoadAssetOnEditor<T>(bundleName, assetName, callback);
#else
        CHMAssetBundle.Instance.LoadAsset<T>(bundleName, assetName, callback);
#endif
    }

    public GameObject Instantiate(GameObject gameObject, Transform parent = null)
    {
        if (gameObject == null) return null;

        CHPoolable poolable = gameObject.GetComponent<CHPoolable>();
        if (poolable != null)
        {
            GameObject go = CHMPool.Instance.Pop(gameObject, parent).gameObject;
            return go;
        }
        else
        {
            GameObject go = GameObject.Instantiate(gameObject, parent);
            return go;
        }
    }

    public async void Destroy(GameObject gameObject, float time = 0)
    {
        if (gameObject == null) return;

        CHPoolable poolable = gameObject.GetComponent<CHPoolable>();
        if (poolable != null)
        {
            await Task.Delay((int)(time * 1000f));
            CHMPool.Instance.Push(poolable);
        }
        else
        {
            UnityEngine.Object.Destroy(gameObject, time);
        }
    }
    #endregion

    public void LoadData(string name, Action<TextAsset> callback)
    {
        LoadAsset<TextAsset>($"data", name, callback);
    }

    public void LoadJson(DefEnum.EJson jsonType, Action<TextAsset> callback)
    {
        LoadAsset<TextAsset>($"{DefEnum.EResource.Json.ToString()}", $"{jsonType.ToString()}", callback);
    }

    public void LoadPlayerData(Action<TextAsset> callback)
    {
        LoadAsset<TextAsset>($"data", $"Player", callback);
    }

    public void LoadSkillData(DefEnum.ESkill skill, Action<SkillData> callback)
    {
        LoadAsset<SkillData>($"{DefEnum.EResource.Scriptable.ToString()}/skill", $"{skill.ToString()}", callback);
    }

    public void LoadOriginBall(Action<GameObject> callback)
    {
        LoadAsset<GameObject>($"unit", $"Ball", callback);
    }

    public void LoadOriginGaugeBar(Action<GameObject> callback)
    {
        LoadAsset<GameObject>($"major", $"GaugeBar", callback);
    }

    public void LoadOriginDamageText(Action<GameObject> callback)
    {
        LoadAsset<GameObject>($"major", $"DamageText", callback);
    }

    public void LoadUnitData(DefEnum.EUnit unit, Action<UnitData> callback)
    {
        LoadAsset<UnitData>($"{DefEnum.EResource.Scriptable.ToString()}/unit", $"{unit.ToString()}", callback);
    }

    public void LoadUnitMaterial(DefEnum.EUnit material, Action<Material> callback)
    {
        LoadAsset<Material>($"{DefEnum.EAssetPiece.Material.ToString()}", $"PBR_{material.ToString()}", callback);
    }

    public void LoadLevelData(DefEnum.EUnit unit, DefEnum.ELevel level, Action<LevelData> callback)
    {
        LoadAsset<LevelData>($"{DefEnum.EResource.Scriptable.ToString()}/level", $"{unit.ToString()}{level.ToString()}", callback);
    }

    public void LoadItemData(DefEnum.EItem item, Action<ItemData> callback)
    {
        LoadAsset<ItemData>($"{DefEnum.EResource.Scriptable.ToString()}/item", $"{item.ToString()}", callback);
    }

    public void InstantiateBall(Action<GameObject> callback = null)
    {
        InstantiateAsObservable<GameObject>($"{DefEnum.EResource.Unit.ToString()}", $"Ball", callback);
    }

    public void InstantiateRobot(DefEnum.EUnit unit, Action<GameObject> callback = null)
    {
        InstantiateAsObservable<GameObject>($"{DefEnum.EResource.Unit.ToString()}", $"Robot", callback);
    }

    public void InstantiateUI(DefEnum.EUI ui, Action<GameObject> callback = null)
    {
        InstantiateAsObservable<GameObject>($"{DefEnum.EResource.UI.ToString()}", $"{ui.ToString()}", callback);
    }

    public void InstantiateParticle(DefEnum.EParticle particle, Action<GameObject> callback = null)
    {
        InstantiateAsObservable<GameObject>($"{DefEnum.EResource.Particle.ToString()}", $"{particle.ToString()}", callback);
    }

    public void InstantiateMajor(DefEnum.EMajor major, Action<GameObject> callback = null)
    {
        InstantiateAsObservable<GameObject>($"{DefEnum.EResource.Major.ToString()}", $"{major.ToString()}", callback);
    }
}
