using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class BasePanel : MonoBehaviour
{
    [HideInInspector] public bool isAnimating;
    private RectTransform rootRect;
    protected IEnumerator DelayedLayoutUpdate()
    {
        yield return null; // 等待一帧
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }
    private Button closeBtn;

    protected virtual void Awake()
    {
        rootRect = transform as RectTransform;
        closeBtn = transform.Find("CloseBtn")?.GetComponent<Button>();
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(OnCloseClicked);
        }
    }

    /// <summary>
    /// 关闭按钮点击回调。默认只隐藏自身。
    /// 需要连带关闭其他面板时由子类重写（见 ImprovePanel）。
    /// </summary>
    protected virtual void OnCloseClicked()
    {
        Hide();
    }

    protected virtual void OnDestroy()
    {
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(OnCloseClicked);
        }
    }

    public virtual void Show()//虚函数 能够被重写  
    {
        gameObject.SetActive(true);
    }


    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
