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

    [SerializeField, ReadOnly] CHUnitData unitBase;
    [SerializeField, ReadOnly] float originPosYText;

    private void Update()
    {
        transform.rotation = Camera.main.transform.rotation;
    }

    public void Init(CHUnitData unitBase, float posY, float gaugeBarPosY)
    {
        this.unitBase = unitBase;
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
                ShowDamageText(damage, .5f);
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
        var copyTextDamage = CHMResource.Instance.Instantiate(CHMUnit.Instance.GetOriginDamageText(), transform).GetComponent<CHTMPro>();
        copyTextDamage.gameObject.SetActive(true);
        copyTextDamage.transform.localPosition = new Vector3(copyTextDamage.transform.localPosition.x, copyTextDamage.transform.position.y + originPosYText);
        copyTextDamage.SetText(damage);

        if (damage < 0)
        {
            copyTextDamage.SetColor(Color.red);
        }
        else if (damage > 0)
        {
            copyTextDamage.SetColor(Color.green);
        }
        else
        {
            copyTextDamage.SetColor(Color.gray);
        }

        copyTextDamage.DOFade(0, time);

        var rtTextDamage = copyTextDamage.GetComponent<RectTransform>();
        if (rtTextDamage)
        {
            rtTextDamage.DOAnchorPosY(originPosYText + 6f, time).OnComplete(() =>
            {
                copyTextDamage.SetAlpha(1);
                rtTextDamage.anchoredPosition = new Vector2(rtTextDamage.anchoredPosition.x, originPosYText);
                CHMResource.Instance.Destroy(copyTextDamage.gameObject);
            });
        }
    }
}
