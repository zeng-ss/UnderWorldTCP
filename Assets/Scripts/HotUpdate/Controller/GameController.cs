using UnityEngine;

public class GameController : MonoBehaviour
{
    private bool isLockMouse;

    private void Start()
    {
        EventMgr.Instance.AddEventListener(GameEvent.CursorShow, NoLock);
        EventMgr.Instance.AddEventListener(GameEvent.CursorHide, Lock);
    }

    private void NoLock(EventArgs args) { isLockMouse = false; }
    private void Lock(EventArgs args) { isLockMouse = true; }

    private bool noLock;

    private void Update()
    {
        if (!noLock)
            Cursor.lockState = isLockMouse ? CursorLockMode.Locked : CursorLockMode.None;

        if (Input.GetKeyDown(KeyCode.K))
            noLock = !noLock;

        UIManager.Instance.TogglePanel<ChatPanel>(KeyCode.C);
        UIManager.Instance.TogglePanel<DepotPanel>(KeyCode.V);

        if (Input.GetKeyDown(KeyCode.Alpha5) && UIManager.Instance.GetPanel<ImprovePanel>())
            UIManager.Instance.GetPanel<ImprovePanel>().UpdateFillBar(100f);

        if (Input.GetKeyDown(KeyCode.B) && UIManager.Instance.GetPanel<DepotPanel>())
        {
            var panel = UIManager.Instance.GetPanel<PlayerDataPanel>();
            if (panel && panel.isAnimating) return;
            if (!panel || !panel.gameObject.activeInHierarchy)
            {
                UIManager.Instance.OpenPanel<PlayerDataPanel>(dataPanel =>
                {
                    dataPanel.UpdatePlayerData(UIManager.Instance.GetPanel<DepotPanel>().equipedDepotList);
                });
            }
            else { UIManager.Instance.ClosePanel<PlayerDataPanel>(); }
        }
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(GameEvent.CursorShow, NoLock);
        EventMgr.Instance.RemoveEventListener(GameEvent.CursorHide, Lock);
    }
}
