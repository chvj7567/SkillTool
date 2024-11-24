using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System.Linq;

public partial class DefEnum
{
    public enum EPoolingScrollView_Direction
    {
        Vertical,
        Horizontal,
    }

    public enum EPoolingScrollView_Align
    {
        LeftOrTop,
        Center,
        RightOrBottom,
    }

    public enum EPoolingScrollView_ScrollingDirection
    {
        UpOrLeft,
        DownOrRight,
    }
}

public partial class DefClass
{
    public class PoolingScrollViewItem<TItem>
    {
        public int index;
        public TItem item;
    }
}

[RequireComponent(typeof(ScrollRect))]
public abstract class CHPoolingScrollView<TItem, TData> : MonoBehaviour where TItem : MonoBehaviour
{
    #region Parameter
    protected LinkedList<DefClass.PoolingScrollViewItem<TItem>> _liPoolItem = new LinkedList<DefClass.PoolingScrollViewItem<TItem>>();

    public List<TItem> ItemList
    {
        get
        {
            return _liPoolItem.Select(_ => _.item).ToList();
        }
    }

    protected List<TData> _liData = new List<TData>();

    public List<TData> DataList
    {
        get
        {
            return _liData.ToList();
        }
    }

    [SerializeField, Header("������ ������ ������Ʈ")]
    GameObject origin;

    [SerializeField, Header("������ ũ��(Zero�̸� Origin ������� ����")]
    Vector2 itemSize = Vector2.zero;

    [SerializeField, Header("������ ���� ����")]
    Vector2 itemGap = Vector2.zero;

    [SerializeField, Header("�е� ����")]
    RectOffset padding = new RectOffset();

    [SerializeField, Header("��ũ�� ���� ����")]
    DefEnum.EPoolingScrollView_Direction scrollDirection = DefEnum.EPoolingScrollView_Direction.Vertical;

    [SerializeField, Header("�� ����, 0�����̸� �ڵ� ���")]
    int rowCount = 0;

    [SerializeField, Header("�� ����, 0�����̸� �ڵ� ���")]
    int columnCount = 0;

    [SerializeField, Header("������ƮǮ ���� ����, 0 �����̸� �ڵ� �Ҵ�")]
    int poolItemCount = 0;

    [SerializeField, Header("������ ���� ����")]
    DefEnum.EPoolingScrollView_Align align = DefEnum.EPoolingScrollView_Align.Center;

    [SerializeField, Header("��ũ�Ѻ� ������ ����� ���ΰ�ħ ����")]
    bool refresh = false;

    [Space]

    protected Vector2 _prevScrollPosition = Vector2.zero;
    protected GameObject _objContent;
    protected RectTransform _rtContent;
    protected RectTransform _rtViewPort;
    protected CanvasGroup _canvasGroupContent;
    protected ScrollRect _scrollRect;

    protected int _rowCount = 0;
    protected int _columnCount = 0;

    public int LineCount
    {
        get
        {
            int line = 0;

            if (_liData.Count == 0)
                return line;

            int count = 1;

            switch (scrollDirection)
            {
                case DefEnum.EPoolingScrollView_Direction.Vertical:
                    {
                        count = _columnCount;
                    }
                    break;
                case DefEnum.EPoolingScrollView_Direction.Horizontal:
                    {
                        count = _rowCount;
                    }
                    break;
            }

            line = _liData.Count / count;
            if (_liData.Count % count != 0)
            {
                line += 1;
            }

            return line;
        }
    }

    public float ItemsWidth
    {
        get
        {
            return _columnCount * itemSize.x + (_columnCount - 1) * itemGap.x;
        }
    }

    public float ItemsHeight
    {
        get
        {
            return _rowCount * itemSize.y + (_rowCount - 1) * itemGap.y;
        }
    }

    public float ContentWidth
    {
        get
        {
            return _rtContent.rect.width;
        }
    }

    public float ContentHeight
    {
        get
        {
            return _rtContent.rect.height;
        }
    }

    public float ScrollViewWidth
    {
        get
        {
            int line = LineCount;
            float width = line * itemSize.x;
            width += (line - 1) * itemGap.x;
            width += padding.left + padding.right;
            return width;
        }
    }

    public float ScrollViewHeight
    {
        get
        {
            int line = LineCount;
            float height = line * itemSize.y;
            height += (line - 1) * itemGap.y;
            height += padding.top + padding.bottom;
            return height;
        }
    }
    #endregion

    private void Awake()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();

        if (_rtViewPort == null)
            _rtViewPort = _scrollRect.viewport;

        if (_objContent == null)
            _objContent = _scrollRect.content.gameObject;

        if (_rtContent == null)
            _rtContent = _scrollRect.content.GetComponent<RectTransform>();

        if (_canvasGroupContent = null)
            _canvasGroupContent = _scrollRect.content.GetComponent<CanvasGroup>();
    }

    public virtual void Start()
    {
        origin.SetActive(false);

        var scrollRect = GetComponent<ScrollRect>();
        scrollRect.OnValueChangedAsObservable().Subscribe(OnScroll);
        scrollRect.OnRectTransformDimensionsChangeAsObservable().Subscribe(_ =>
        {
            if (refresh)
            {
                Refresh();
            }
        });
    }

    #region Initialize
    /// <summary>
    /// �ܺο��� �ش� �Լ� ȣ�� (��ũ�� �� ������ ����)
    /// </summary>
    /// <param name="dataList"></param>
    public virtual void InitItemList(List<TData> dataList)
    {
        _liData.Clear();
        _liData.AddRange(dataList);

        // ������ ũ�� ����
        SetItemSize();

        // �� ���� ����
        SetRowCount();

        // �� ���� ����
        SetColumnCount();

        // ������ Ǯ�� ���� ����
        SetPoolItemCount();

        // Content ������Ʈ ����
        SetContentTransform();

        // Ǯ�� ������Ʈ ����
        CreatePoolingObject();

        // ������ �ʱ�ȭ
        InitItem();
    }

    void InitItem()
    {
        _liPoolItem.Clear();

        // ������ Ǯ�� ������Ʈ ������
        int childCount = _objContent.transform.childCount;
        GameObject[] arrChildObj = new GameObject[childCount];
        for (int i = 0; i < childCount; ++i)
        {
            arrChildObj[i] = _objContent.transform.GetChild(i).gameObject;
        }

        Rect contentRect = GetViewConentRect();
        Rect itemRect = new Rect();
        int firstIndex = Enumerable.Range(0, _liData.Count).FirstOrDefault(_ =>
        {
            Vector2 itemPosition = GetItemPosition(_);
            itemRect.Set(itemPosition.x, itemPosition.y, itemSize.x, itemSize.y);

            // Content�� ��ġ�� ���� ��ȯ
            return contentRect.Overlaps(itemRect);
        });

        for (int i = 0; i < childCount; ++i)
        {
            arrChildObj[i].SetActive(true);
        }

        for (int i = 0; i < arrChildObj.Length; ++i)
        {
            int index = i + firstIndex;
            TItem item = arrChildObj[i].GetComponent<TItem>();

            InitItem(item, index);
            DefClass.PoolingScrollViewItem<TItem> poolItem = new DefClass.PoolingScrollViewItem<TItem>() { index = index, item = item };
            _liPoolItem.AddLast(poolItem);
        }
    }

    void InitItem(TItem item, int index)
    {
        TData info = _liData.ElementAtOrDefault(index);

        bool isNotNull = info != null;
        item.gameObject.SetActive(isNotNull);

        if (isNotNull)
        {
            InitItem(item, info, index);
        }

        InitItemTransform(item.gameObject, index);
        item.gameObject.name = $"{origin.name} {index}";
    }

    /// <summary>
    /// ������ �ʱ�ȭ ��ũ���Ͽ� �ε����� �ٲ𶧸��� ȣ��
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="info"></param>
    /// <param name="index"></param>
    public abstract void InitItem(TItem obj, TData info, int index);

    /// <summary>
    /// Ǯ�� ������Ʈ ���� �� ȣ��(���� 1ȸ)
    /// </summary>
    /// <param name="obj"></param>
    public virtual void InitPoolingObject(TItem obj) { }

    public virtual void InitItemTransform(GameObject item, int index)
    {
        var rectTransform = item.GetComponent<RectTransform>();

        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        rectTransform.anchoredPosition = GetItemPosition(index);
        rectTransform.SetSiblingIndex(index);
    }
    #endregion

    void SetItemSize()
    {
        RectTransform rectTransform = origin.GetComponent<RectTransform>();
        if (rectTransform == null)
            throw new NullReferenceException();

        itemSize.x = rectTransform.rect.width * rectTransform.localScale.x;
        itemSize.y = rectTransform.rect.height * rectTransform.localScale.y;
    }

    void SetColumnCount()
    {
        if (columnCount <= 0)
        {
            float width = _rtViewPort.rect.width - (padding.left + padding.right);
            _columnCount = Mathf.Max(1, Mathf.FloorToInt(width / (itemSize.x + itemGap.x)));
        }
        else
        {
            _columnCount = columnCount;
        }
    }

    void SetRowCount()
    {
        if (rowCount <= 0)
        {
            float width = _rtViewPort.rect.height - (padding.top + padding.bottom);
            _rowCount = Mathf.Max(1, Mathf.FloorToInt(width / (itemSize.y + itemGap.y)));
        }
        else
        {
            _rowCount = rowCount;
        }
    }

    void SetPoolItemCount()
    {
        if (poolItemCount <= 0)
        {
            switch (scrollDirection)
            {
                case DefEnum.EPoolingScrollView_Direction.Vertical:
                    {
                        int line = Mathf.RoundToInt(_rtViewPort.rect.size.y / itemSize.y);
                        line += 2;
                        poolItemCount = line * _columnCount;
                    }
                    break;
                case DefEnum.EPoolingScrollView_Direction.Horizontal:
                    {
                        int line = Mathf.RoundToInt(_rtViewPort.rect.size.x / itemSize.x);
                        line += 2;
                        poolItemCount = line * _rowCount;
                    }
                    break;
            }
        }
    }

    public virtual void SetContentTransform()
    {
        switch (scrollDirection)
        {
            case DefEnum.EPoolingScrollView_Direction.Vertical:
                {
                    // ��Ʈ��ġ ��Ŀ�� ����
                    _rtContent.anchorMax = new Vector2(.5f, 1f);
                    _rtContent.anchorMin = new Vector2(.5f, 1f);
                    _rtContent.pivot = new Vector2(.5f, 1f);

                    // ������ �缳��
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rtViewPort.rect.width);
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ScrollViewHeight);

                    // ������ �ֻ������ �̵�
                    _rtContent.anchoredPosition = Vector2.zero;
                }
                break;
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                {
                    // ��Ʈ��ġ ��Ŀ�� ����
                    _rtContent.anchorMax = new Vector2(0f, .5f);
                    _rtContent.anchorMin = new Vector2(0f, .5f);
                    _rtContent.pivot = new Vector2(0f, .5f);

                    // ������ �缳��
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ScrollViewWidth);
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rtViewPort.rect.height);

                    // ������ �ֻ������ �̵�
                    _rtContent.anchoredPosition = Vector2.zero;
                }
                break;
        }
    }

    public Vector2 GetItemPosition(int index)
    {
        Vector2 pos = Vector2.zero;

        switch (scrollDirection)
        {
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                pos = GetItemHorizontalPosition(index);
                break;
            case DefEnum.EPoolingScrollView_Direction.Vertical:
                pos = GetItemVerticalPosition(index);
                break;
        }

        return pos;
    }

    public virtual Vector2 GetItemHorizontalPosition(int index)
    {
        // �� �ε���
        int rowIndex = index % _rowCount;

        float y = rowIndex * itemSize.y;
        y += rowIndex * itemGap.y;

        float diff = ContentHeight - ItemsHeight;

        Debug.Log($"GetItemHorizontalPosition {index} H {ContentHeight} - {_rowCount} * {itemSize.y} + {(_rowCount - 1)} * {itemGap.y}");
        Debug.Log($"GetItemHorizontalPosition {index} W {ContentWidth} - {ItemsWidth}");
        switch (align)
        {
            case DefEnum.EPoolingScrollView_Align.LeftOrTop:
                {
                    y += 0;
                    y += padding.top;
                }
                break;
            case DefEnum.EPoolingScrollView_Align.Center:
                {
                    y += (diff) * 0.5f;
                    y += padding.top;
                }
                break;
            case DefEnum.EPoolingScrollView_Align.RightOrBottom:
                {
                    y += diff;
                    y -= padding.bottom;
                }
                break;
        }

        y *= -1;

        // �� �ε���
        int colIndex = index / _rowCount;

        float x = colIndex * itemSize.x;
        x += colIndex * itemGap.x;
        x += padding.left;

        return new Vector2(x, y);
    }

    public virtual Vector2 GetItemVerticalPosition(int index)
    {
        Debug.Log($"GetItemVerticalPosition {index}");

        // �� �ε���
        int colIndex = index % _columnCount;

        float x = colIndex * itemSize.x;
        x += colIndex * itemGap.x;

        float diff = ContentWidth - ItemsWidth;

        switch (align)
        {
            case DefEnum.EPoolingScrollView_Align.LeftOrTop:
                {
                    x += 0;
                    x += padding.left;
                }
                break;
            case DefEnum.EPoolingScrollView_Align.Center:
                {
                    x += (diff) * 0.5f;
                    x += padding.left;
                }
                break;
            case DefEnum.EPoolingScrollView_Align.RightOrBottom:
                {
                    x += diff;
                    x -= padding.right;
                }
                break;
        }

        // �� �ε���
        int rowIndex = index / _columnCount;

        float y = rowIndex * itemSize.y;
        y += rowIndex * itemGap.y;
        y += padding.top;
        y *= -1;

        return new Vector2(x, y);
    }

    public Rect GetViewConentRect()
    {
        Rect rect = new Rect();

        switch (scrollDirection)
        {
            case DefEnum.EPoolingScrollView_Direction.Vertical:
                {
                    float scrollRectY = _rtViewPort.rect.size.y;
                    rect.Set(_rtContent.anchoredPosition.x, -1 * (_rtContent.anchoredPosition.y + scrollRectY), ContentWidth, scrollRectY + itemSize.y);
                }
                break;
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                {
                    float scrollRectX = _rtViewPort.rect.size.x;
                    float scrollRectY = _rtViewPort.rect.size.y;
                    rect.Set(-1 * _rtContent.anchoredPosition.x, -1 * (_rtContent.anchoredPosition.y + scrollRectY), scrollRectX + itemSize.x, ContentHeight);
                }
                break;
        }

        return rect;
    }

    private void OnScroll(Vector2 scrollPosition)
    {
        if (_liPoolItem.Count <= 0)
            return;

        Vector2 delta = scrollPosition - _prevScrollPosition;
        _prevScrollPosition = scrollPosition;

        switch (scrollDirection)
        {
            case DefEnum.EPoolingScrollView_Direction.Vertical:
                {
                    // ���� ��ġ�� �޶����ٸ�
                    if (delta.y != 0)
                    {
                        UpdateContent(delta.y > 0 ? DefEnum.EPoolingScrollView_ScrollingDirection.UpOrLeft : DefEnum.EPoolingScrollView_ScrollingDirection.DownOrRight);
                    }
                }
                break;
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                {
                    // ���� ��ġ�� �޶����ٸ�
                    if (delta.x != 0)
                    {
                        UpdateContent(delta.x < 0 ? DefEnum.EPoolingScrollView_ScrollingDirection.UpOrLeft : DefEnum.EPoolingScrollView_ScrollingDirection.DownOrRight);
                    }
                }
                break;
        }
    }

    void UpdateContent(DefEnum.EPoolingScrollView_ScrollingDirection direction)
    {
        if (_liPoolItem.Count <= 0)
            return;

        Rect contentRect = GetViewConentRect();

        // �����˻��� ������ �簢���� ���Ʒ��� �����߰�
        Rect itemRect = new Rect();

        switch (direction)
        {
            case DefEnum.EPoolingScrollView_ScrollingDirection.UpOrLeft:
                {
                    int firstIndex = _liPoolItem.First.Value.index;
                    for (int i = firstIndex - 1; i >= 0; --i)
                    {
                        Vector2 itemPosition = GetItemPosition(i);
                        itemRect.Set(itemPosition.x, itemPosition.y, itemSize.x, itemSize.y);

                        if (contentRect.Overlaps(itemRect))
                        {
                            var node = _liPoolItem.Last;
                            _liPoolItem.Remove(node);

                            InitItem(node.Value.item, i);

                            node.Value.index = i;
                            _liPoolItem.AddFirst(node);
                        }
                    }
                }
                break;
            case DefEnum.EPoolingScrollView_ScrollingDirection.DownOrRight:
                {
                    int lastIndex = _liPoolItem.Last.Value.index;
                    for (int i = lastIndex + 1; i < _liData.Count; ++i)
                    {
                        Vector2 itemPosition = GetItemPosition(i);
                        itemRect.Set(itemPosition.x, itemPosition.y, itemSize.x, itemSize.y);

                        if (contentRect.Overlaps(itemRect))
                        {
                            var node = _liPoolItem.First;
                            _liPoolItem.Remove(node);

                            InitItem(node.Value.item, i);

                            node.Value.index = i;
                            _liPoolItem.AddLast(node);
                        }
                    }
                }
                break;
        }
    }

    private void CreatePoolingObject()
    {
        int diff = poolItemCount - _objContent.transform.childCount;
        diff = Mathf.Min(diff, _liData.Count); // �ʿ��� ��ŭ�� Ǯ�� ������
        if (diff > 0)
        {
            for (int i = 0; i < diff; ++i)
            {
                GameObject obj = Instantiate(origin, _objContent.transform);
                InitPoolingObject(obj.GetComponent<TItem>());
            }
        }
    }

    public void Refresh()
    {
        InitItemList(_liData.ToList());
    }

    public void Clear()
    {
        _liData.Clear();
        _liPoolItem.Clear();
        if (_objContent)
        {
            int childCount = _objContent.transform.childCount;
            GameObject[] children = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                children[i] = _objContent.transform.GetChild(i).gameObject;
            }

            foreach (var child in children)
            {
                child.SetActive(false);
            }
        }
    }
}
