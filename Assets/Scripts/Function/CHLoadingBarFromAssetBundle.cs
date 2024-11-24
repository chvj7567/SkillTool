using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CHLoadingBarFromAssetBundle : MonoBehaviour
{
    [SerializeField] Image loadingBar;
    [SerializeField] TMP_Text loadingText;
    [SerializeField] TMP_Text downloadText;
    [SerializeField] bool useStreamingAssets;
    [SerializeField] bool googleDriveDownload;
    [SerializeField] string bundleKey;
    [SerializeField, ReadOnly] int totalLoadCount = 0;
    [SerializeField, ReadOnly] int loadingCount = 0;
    [SerializeField, ReadOnly] int totalDownoadCount = 0;
    [SerializeField, ReadOnly] int downloadingCount = 0;

    string _googleDriveDownloadURL = "https://docs.google.com/uc?export=download&id=";
    string _url = "";

    Dictionary<string, string> _dicGoogleDownload = new Dictionary<string, string>();

    public Action actOneBundleLoadSuccess;
    public Action actAllBundleLoadSuccess;

    public void Init()
    {
        if (CHMAssetBundle.Instance.FirstDownload == false)
            return;

        if (loadingBar) loadingBar.fillAmount = 0f;

        totalDownoadCount = totalLoadCount = _dicGoogleDownload.Count;

        // 에셋 번들 저장 경로 설정
        string savePath = Path.Combine(Application.persistentDataPath, bundleKey);
        if (Directory.Exists(savePath) == false)
        {
            Directory.CreateDirectory(savePath);
        }

        var valueList = _dicGoogleDownload.Values.ToList();

        if (googleDriveDownload)
        {
            for (int i = 0; i < valueList.Count; ++i)
            {
                StartCoroutine(DownloadAssetBundle(valueList[i]));
            }
        }
        else
        {
            StartCoroutine(LoadAssetBundleAll());
        }

        actAllBundleLoadSuccess += () =>
        {
            Debug.Log("bundleDownloadSuccess");
            foreach (var value in valueList)
            {
                StartCoroutine(LoadAssetBundle(value));
            }
        };
    }

    IEnumerator LoadAssetBundleAll()
    {
        Debug.Log("LoadAssetBundle");

        string bundlePath = "";

        if (useStreamingAssets)
        {
            bundlePath = Path.Combine(Application.streamingAssetsPath, bundleKey);
        }
        else
        {
            bundlePath = Path.Combine(Application.persistentDataPath, bundleKey);
        }

        Debug.Log($"BundlePath : {bundlePath}");

        // 에셋 번들 로드
        AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);

        while (!bundleRequest.isDone)
        {
            yield return null;
        }

        AssetBundle assetBundle = bundleRequest.assetBundle;

        if (assetBundle == null)
        {
            Debug.Log($"Load Fail : {bundleKey}");
        }
        else
        {
            AssetBundleManifest manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] arrBundleName = manifest.GetAllAssetBundles();

            assetBundle.Unload(false);

            totalLoadCount = arrBundleName.Length;

            foreach (string name in arrBundleName)
            {
                yield return LoadAssetBundle(name);
            }
        }
    }

    IEnumerator LoadAssetBundle(string bundleName)
    {
        string bundlePath = "";
        AssetBundleCreateRequest bundleRequest;
        if (useStreamingAssets)
        {
            bundlePath = Path.Combine(Application.streamingAssetsPath, bundleName);
            bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        }
        else
        {
            bundlePath = Path.Combine(Application.persistentDataPath, bundleKey);
            bundleRequest = AssetBundle.LoadFromFileAsync(Path.Combine(bundlePath + "/", $"{bundleName}.unity3d"));
        }

        if (downloadText) downloadText.text = $"{bundleName} Loading...";

        // 다운로드 표시
        float downloadProgress = 0;

        while (!bundleRequest.isDone)
        {
            downloadProgress = bundleRequest.progress;

            if (loadingBar) loadingBar.fillAmount = downloadProgress / totalLoadCount * loadingCount;
            if (loadingText) loadingText.text = downloadProgress / totalLoadCount * loadingCount * 100f + "%";

            yield return null;
        }

        if (bundleRequest.assetBundle == null)
        {
            Debug.LogError($"{bundleName} is Null");
        }
        else
        {
            downloadProgress = bundleRequest.progress;

            AssetBundle assetBundle = bundleRequest.assetBundle;

            CHMAssetBundle.Instance.LoadAssetBundle(bundleName, assetBundle);

            ++loadingCount;

            if (loadingBar) loadingBar.fillAmount = downloadProgress / totalLoadCount * loadingCount;
            if (loadingText) loadingText.text = downloadProgress / totalLoadCount * loadingCount * 100f + "%";

            if (downloadText) downloadText.text = $"{bundleName} Load Success";
            Debug.Log($"{bundleName} Load Success");

            if (loadingCount == totalLoadCount)
            {
                if (loadingBar) loadingBar.fillAmount = 1;
                if (loadingText) loadingText.text = "100%";
                Debug.Log($"Bundle load Success");
                actOneBundleLoadSuccess.Invoke();
            }
        }
    }

    IEnumerator DownloadAllAssetBundle()
    {
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(_url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log($"Error : {request.error}");
        }
        else
        {
            // 에셋 번들 저장 경로 설정
            string savePath = Path.Combine(Application.persistentDataPath, bundleKey);

            // 파일 저장
            File.WriteAllBytes(savePath, request.downloadHandler.data);

            AssetBundle assetBundle = DownloadHandlerAssetBundle.GetContent(request);
            AssetBundleManifest manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] arrBundleName = manifest.GetAllAssetBundles();

            assetBundle.Unload(false);
            foreach (string name in arrBundleName)
            {
                yield return DownloadAssetBundle(name);
            }
        }
    }

    IEnumerator DownloadAssetBundle(string bundleName)
    {
        string path = "";
        if (googleDriveDownload)
        {
            path = $"{_googleDriveDownloadURL}{bundleName}";
        }
        else
        {
            path = Path.Combine(_url, bundleName);
        }

        Debug.Log(path);

        UnityWebRequest request = UnityWebRequest.Get(path);

        request.SendWebRequest();

        // 다운로드 표시
        float downloadProgress = 0;
        while (!request.isDone)
        {
            downloadProgress = request.downloadProgress;
            int downloadPercentage = Mathf.RoundToInt(downloadProgress * 100);

            if (loadingBar) loadingBar.fillAmount = downloadProgress / totalDownoadCount * loadingCount;
            if (loadingText) loadingText.text = downloadProgress / totalDownoadCount * loadingCount * 100f + "%";

            yield return null;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log($"Error : {request.error}");
        }
        else
        {
            // 에셋 번들 저장 경로 설정
            string savePath = Path.Combine(Application.persistentDataPath, bundleKey);

            // 파일 저장
            File.WriteAllBytes(savePath + "/" + $"{bundleName}.unity3d", request.downloadHandler.data);

            Debug.Log($"Saving asset bundle to: {savePath}");

            ++downloadingCount;

            if (loadingBar) loadingBar.fillAmount = downloadProgress / totalDownoadCount * loadingCount;
            if (loadingText) loadingText.text = downloadProgress / totalDownoadCount * loadingCount * 100f + "%";

            if (downloadText) downloadText.text = $"{bundleName} Download Success";
            Debug.Log($"{bundleName} Download Success");

            if (downloadingCount == totalDownoadCount)
            {
                if (loadingBar) loadingBar.fillAmount = 1;
                if (loadingText) loadingText.text = "100%";
                Debug.Log($"Bundle download Success");
                actAllBundleLoadSuccess.Invoke();
            }
        }
    }
}
