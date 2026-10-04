using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRoomItem : MonoBehaviour
{
    private const string DefaultHeadIcon = "Icon/icon_question";

    public TMP_Text playerName;
    public TMP_Text isReadyText;
    public Image playerHeadImage;

    public void Init(PlayerConfig playerConfig)
    {
        playerName.text = playerConfig.name;
        isReadyText.text = playerConfig.isReady ? "已准备" : "未准备";
        SetHeadImage(playerConfig.headImageName);
    }

    public void UpdateRoomItem(PlayerConfig playerConfig)
    {
        playerName.text = playerConfig.name;
        isReadyText.text = playerConfig.isReady ? "已准备" : "未准备";
        SetHeadImage(playerConfig.headImageName);
        DOVirtual.DelayedCall(0.2f, () => { SetHeadImage(playerConfig.headImageName); });
    }

    /// <summary>
    /// 设置头像。加载失败时回退到默认头像。
    /// 图片经 ResMgr 缓存，重复设置同一张图不会有额外开销。
    /// </summary>
    private void SetHeadImage(string headImageName)
    {
        ResMgr.Instance.LoadSpriteAsync($"Icon/{headImageName}", sprite =>
        {
            if (!playerHeadImage) return;

            if (sprite != null)
            {
                playerHeadImage.sprite = sprite;
                return;
            }

            ResMgr.Instance.LoadSpriteAsync(DefaultHeadIcon, fallback =>
            {
                if (playerHeadImage) playerHeadImage.sprite = fallback;
            });
        });
    }
}
