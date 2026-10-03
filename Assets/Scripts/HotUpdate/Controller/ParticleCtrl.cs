using System.Collections.Generic;
using UnityEngine;

public class ParticleCtrl : MonoBehaviour
{
    private PlayerCtrl player;
    public float high;
    private HashSet<GameObject> damagedEnemies = new(); // 记录已伤害的敌人
    
    private void OnEnable()
    {
        damagedEnemies.Clear(); // 粒子启用时清空记录
    }

    public void Init(PlayerCtrl player)
    {
        this.player = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (player == null) return;
        if (!player.IsLocalPlayer) return;
        if (!other.CompareTag("enemy")) return;
        // 如果这个敌人还没被伤害过
        if (damagedEnemies.Add(other.gameObject))
        {
            IHurt enemy = other.GetComponent<IHurt>();
            Vector3 pos = other.ClosestPoint(transform.position);
            pos.y += high;
            player.OnHit(enemy, pos);
        }
    }

    private void OnDestroy()
    {
        player = null;
        damagedEnemies.Clear();
    }
}
