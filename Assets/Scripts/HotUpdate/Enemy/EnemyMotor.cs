using UnityEngine;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 敌人的物理与位移：重力、CharacterController 移动、动画根运动落地。
    /// 所有「改位置」都走这里，避免 transform 与 CharacterController 互相覆盖。
    /// </summary>
    public class EnemyMotor
    {
        private const float Gravity = -5f;

        private readonly Transform _transform;
        private readonly CharacterController _controller;
        private Vector3 _velocity;

        public EnemyMotor(EnemyCtrl ctrl)
        {
            _transform = ctrl.transform;
            _controller = ctrl.GetComponent<CharacterController>();
        }

        /// <summary>瞬移（生成 / 纠偏），会顺手把 CC 的内部位置对齐</summary>
        public void Teleport(Vector3 pos)
        {
            if (_controller == null || !_controller.enabled)
            {
                _transform.position = pos;
                return;
            }

            _controller.enabled = false;
            _transform.position = pos;
            _controller.enabled = true;
        }

        /// <summary>水平位移：有 CC 必须走 Move，直接写 transform 会被它覆盖</summary>
        public void Move(Vector3 delta)
        {
            if (delta.sqrMagnitude <= 0f) return;

            if (_controller != null && _controller.enabled) _controller.Move(delta);
            else _transform.position += delta;
        }

        /// <summary>重力（竖直方向）</summary>
        public void TickGravity(float dt)
        {
            if (_controller == null || !_controller.enabled) return;

            _controller.Move(_velocity * dt);
            _velocity.y = _controller.isGrounded ? -2f : _velocity.y + Gravity * dt;
        }

        /// <summary>死亡后关掉 CC（它同时是命中体积）</summary>
        public void Stop()
        {
            if (_controller != null) _controller.enabled = false;
        }
    }
}
