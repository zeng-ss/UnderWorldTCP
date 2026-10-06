using System.Collections.Generic;
using UnityEngine;

public class ParticleCtrl : MonoBehaviour
{
    private PlayerCtrl _player;
    public float high;
    private HashSet<GameObject> _damagedEnemies = new(); // 记录已伤害的敌人

    private void OnEnable()
    {
        _damagedEnemies.Clear(); // 粒子启用时清空记录
    }

    public void Init(PlayerCtrl player)
    {
        this._player = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_player == null) return;
        if (!_player.IsLocalPlayer) return;
        if (!other.CompareTag("enemy")) return;
        // 如果这个敌人还没被伤害过
        if (_damagedEnemies.Add(other.gameObject))
        {
            IHurt enemy = other.GetComponent<IHurt>();
            Vector3 pos = other.ClosestPoint(transform.position);
            pos.y += high;
            _player.OnHit(enemy, pos);
        }
    }

    private void OnDestroy()
    {
        _player = null;
        _damagedEnemies.Clear();
    }
}