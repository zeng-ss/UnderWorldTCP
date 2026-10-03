using UnityEngine;

public class GameController : MonoBehaviour
{
    private bool isLockMouse;

    private void Start()
    {
        EventCenter.Instance.AddEventListener(GameEvent.光标出现, NoLock);
        EventCenter.Instance.AddEventListener(GameEvent.光标消失, Lock);
    }

    private void NoLock() { isLockMouse = false; }
    private void Lock() { isLockMouse = true; }

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
        EventCenter.Instance.RemoveEventListener(GameEvent.光标出现, NoLock);
        EventCenter.Instance.RemoveEventListener(GameEvent.光标消失, Lock);
    }
}
