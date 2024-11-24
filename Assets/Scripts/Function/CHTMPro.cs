using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using DG.Tweening;

[RequireComponent(typeof(TMP_Text))]
public class CHTMPro : MonoBehaviour
{
    [SerializeField] int _stringID = -1;
    [SerializeField] RectTransform _textRectTransfrom;
    [SerializeField] TMP_Text _text;
    [ReadOnly] object[] _arrArgObject;

    private void Awake()
    {
        if (_textRectTransfrom == null)
            _textRectTransfrom = GetComponent<RectTransform>();
        
        if (_text == null)
            _text = GetComponent<TMP_Text>();

        if (_text)
        {
            if (_stringID != -1)
            {
                _text.text = CHMJson.Instance.GetString(_stringID);
            }
        }
    }

    public void SetText(string text)
    {
        if (_text)
        {
            _text.text = text;
        }
    }

    public void SetText(params object[] arrArgObject)
    {
        _arrArgObject = arrArgObject;
        if (_text)
        {
            _text.text = string.Format(CHMJson.Instance.GetString(_stringID), arrArgObject);
        }
    }

    public void SetColor(Color color)
    {
        if (_text)
        {
            _text.color = color;
        }
    }

    public void SetStringID(int stringID)
    {
        this._arrArgObject = null;
        this._stringID = stringID;
        if (_text)
        {
            _text.text = CHMJson.Instance.GetString(this._stringID);
        }
    }

    public void SetPlusString(string plusString)
    {
        if (_text && string.IsNullOrEmpty(plusString) == false)
        {
            _text.text = _text.text + " + " + plusString;
        }
    }

    public void DOFade(float value, float time)
    {
        _text.DOFade(value, time);
    }

    public void SetAlpha(float value)
    {
        _text.alpha = value;
    }
}
