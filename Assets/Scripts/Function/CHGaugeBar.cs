using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class CHGaugeBar : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Image imgBackground;
    [SerializeField] Image imgBackGaugeBar;
    [SerializeField] Image imgGaugeBar;
    [SerializeField, ReadOnly] float originPosYText;

    private void Update()
    {
        transform.rotation = Camera.main.transform.rotation;
    }

    public void Init(float posY, float gaugeBarPosY)
    {
        canvas.worldCamera = Camera.main;
        transform.localPosition = new Vector3(0f, posY, 0f);
        originPosYText = posY;
        imgBackground.rectTransform.anchoredPosition = new Vector2(imgBackground.rectTransform.anchoredPosition.x, gaugeBarPosY);
        imgBackGaugeBar.rectTransform.anchoredPosition = new Vector2(imgBackGaugeBar.rectTransform.anchoredPosition.x, gaugeBarPosY);
        imgGaugeBar.rectTransform.anchoredPosition = new Vector2(imgGaugeBar.rectTransform.anchoredPosition.x, gaugeBarPosY);
    }

    public void SetGaugeBar(float maxValue, float curValue, float damage, float backGaugeTime = 1.5f, float gaugeTime = 1.0f, bool viewDamage = true)
    {
        if (imgBackGaugeBar)
            imgBackGaugeBar.DOFillAmount(curValue / maxValue, backGaugeTime);
        if (imgGaugeBar)
            imgGaugeBar.DOFillAmount(curValue / maxValue, gaugeTime);

        if (maxValue > curValue + damage)
        {
            if (viewDamage == true && Mathf.Approximately(damage, 0f) == false)
            {
                ShowDamageText(damage, 2f);
            }
        }
    }

    public void ResetGaugeBar()
    {
        if (imgBackGaugeBar)
            imgBackGaugeBar.DOFillAmount(1f, 0.1f);
        if (imgGaugeBar)
            imgGaugeBar.DOFillAmount(1f, 0.1f);
    }

    public void ActiveHpBar(bool active)
    {
        if (imgBackground)
            imgBackground.gameObject.SetActive(active);
        if (imgBackGaugeBar)
            imgBackGaugeBar.gameObject.SetActive(active);
        if (imgGaugeBar)
            imgGaugeBar.gameObject.SetActive(active);
    }

    void ShowDamageText(float damage, float time)
    {
        CHTMPro copyText = CHMResource.Instance.Instantiate(CHMUnit.Instance.GetOriginDamageText(), transform).GetComponent<CHTMPro>();
        RectTransform rectTransform = copyText.GetComponent<RectTransform>();

        float startY = originPosYText + 6f;
        rectTransform.anchoredPosition = new Vector2(0f, startY);

        copyText.gameObject.SetActive(true);
        copyText.SetText(damage);

        if (damage < 0)
        {
            copyText.SetColor(Color.red);
        }
        else if (damage > 0)
        {
            copyText.SetColor(Color.green);
        }
        else
        {
            copyText.SetColor(Color.gray);
        }

        copyText.DOFade(0, time, () => copyText.SetAlpha(1f));

        rectTransform.DOAnchorPosY(startY + 3f, time).OnComplete(() =>
        {
            CHMResource.Instance.Destroy(copyText.gameObject);
        });
    }
}
