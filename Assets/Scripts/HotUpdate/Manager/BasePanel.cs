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
    private bool isShow;
    private Button closeBtn;
    public virtual void Awake()
    {
        rootRect = transform as RectTransform;
        closeBtn = gameObject.transform.Find("CloseBtn") ==  null ? null : gameObject.transform.Find("CloseBtn").GetComponent<Button>();
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(() =>
            {
                if (UIManager.Instance.GetPanel<ImprovePanel>()!=null&& UIManager.Instance.GetPanel<ImprovePanel>().gameObject.activeSelf)
                {
                    UIManager.Instance.ClosePanel<ImprovePanel>();
                }
                UIManager.Instance.ClosePanel<PlayerDataPanel>();
            });
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
