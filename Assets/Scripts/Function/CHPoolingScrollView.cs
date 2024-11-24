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

    [SerializeField, Header("복제할 아이템 오브젝트")]
    GameObject origin;

    [SerializeField, Header("아이템 크기(Zero이면 Origin 사이즈로 적용")]
    Vector2 itemSize = Vector2.zero;

    [SerializeField, Header("아이템 사이 간격")]
    Vector2 itemGap = Vector2.zero;

    [SerializeField, Header("패딩 설정")]
    RectOffset padding = new RectOffset();

    [SerializeField, Header("스크롤 방향 설정")]
    DefEnum.EPoolingScrollView_Direction scrollDirection = DefEnum.EPoolingScrollView_Direction.Vertical;

    [SerializeField, Header("행 갯수, 0이하이면 자동 계산")]
    int rowCount = 0;

    [SerializeField, Header("열 갯수, 0이하이면 자동 계산")]
    int columnCount = 0;

    [SerializeField, Header("오브젝트풀 개수 설정, 0 이하이면 자동 할당")]
    int poolItemCount = 0;

    [SerializeField, Header("아이템 정렬 기준")]
    DefEnum.EPoolingScrollView_Align align = DefEnum.EPoolingScrollView_Align.Center;

    [SerializeField, Header("스크롤뷰 사이즈 변경시 새로고침 여부")]
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
    /// 외부에서 해당 함수 호출 (스크롤 될 데이터 세팅)
    /// </summary>
    /// <param name="dataList"></param>
    public virtual void InitItemList(List<TData> dataList)
    {
        _liData.Clear();
        _liData.AddRange(dataList);

        // 아이템 크기 설정
        SetItemSize();

        // 행 갯수 설정
        SetRowCount();

        // 열 갯수 설정
        SetColumnCount();

        // 아이템 풀링 갯수 설정
        SetPoolItemCount();

        // Content 오브젝트 설정
        SetContentTransform();

        // 풀링 오브젝트 생성
        CreatePoolingObject();

        // 아이템 초기화
        InitItem();
    }

    void InitItem()
    {
        _liPoolItem.Clear();

        // 생성된 풀링 오브젝트 가져옴
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

            // Content와 겹치는 여부 반환
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
    /// 아이템 초기화 스크롤하여 인덱스가 바뀔때마다 호출
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="info"></param>
    /// <param name="index"></param>
    public abstract void InitItem(TItem obj, TData info, int index);

    /// <summary>
    /// 풀링 오브젝트 생성 시 호출(최초 1회)
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
                    // 스트레치 앵커로 설정
                    _rtContent.anchorMax = new Vector2(.5f, 1f);
                    _rtContent.anchorMin = new Vector2(.5f, 1f);
                    _rtContent.pivot = new Vector2(.5f, 1f);

                    // 사이즈 재설정
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rtViewPort.rect.width);
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ScrollViewHeight);

                    // 컨텐츠 최상단으로 이동
                    _rtContent.anchoredPosition = Vector2.zero;
                }
                break;
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                {
                    // 스트레치 앵커로 설정
                    _rtContent.anchorMax = new Vector2(0f, .5f);
                    _rtContent.anchorMin = new Vector2(0f, .5f);
                    _rtContent.pivot = new Vector2(0f, .5f);

                    // 사이즈 재설정
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ScrollViewWidth);
                    _rtContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rtViewPort.rect.height);

                    // 컨텐츠 최상단으로 이동
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
        // 행 인덱스
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

        // 열 인덱스
        int colIndex = index / _rowCount;

        float x = colIndex * itemSize.x;
        x += colIndex * itemGap.x;
        x += padding.left;

        return new Vector2(x, y);
    }

    public virtual Vector2 GetItemVerticalPosition(int index)
    {
        Debug.Log($"GetItemVerticalPosition {index}");

        // 열 인덱스
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

        // 행 인덱스
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
                    // 이전 위치와 달라졌다면
                    if (delta.y != 0)
                    {
                        UpdateContent(delta.y > 0 ? DefEnum.EPoolingScrollView_ScrollingDirection.UpOrLeft : DefEnum.EPoolingScrollView_ScrollingDirection.DownOrRight);
                    }
                }
                break;
            case DefEnum.EPoolingScrollView_Direction.Horizontal:
                {
                    // 이전 위치와 달라졌다면
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

        // 영역검사할 컨텐츠 사각형의 위아래로 마진추가
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
        diff = Mathf.Min(diff, _liData.Count); // 필요한 만큼만 풀을 만들자
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
