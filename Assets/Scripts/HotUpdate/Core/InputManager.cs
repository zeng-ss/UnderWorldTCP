using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HotUpdate.Core
{
    /// <summary>
    /// 输入集中管理。所有按键统一在这里注册、由唯一的一处 Update 轮询，
    /// 输入锁：打开需要独占操作的面板时压栈锁住玩法输入，关闭时弹栈。
    /// 由 UIManager 依据 BasePanel.BlocksGameplayInput 统一管理，业务代码不用关心。
    /// </summary>
    public class InputManager : UnitySingleTonMono<InputManager>
    {
        private readonly Dictionary<KeyCode, Action> _always = new();
        private readonly Dictionary<KeyCode, Action> _gameplay = new();
        private readonly Dictionary<int, Action> _mouse = new();
        private readonly Dictionary<int, Action> _gameplayMouse = new();
        private readonly List<object> _lockTokens = new();

        // Update 遍历用的快照，避免回调里注册/注销导致遍历集合被修改
        private KeyValuePair<KeyCode, Action>[] _alwaysSnapshot = Array.Empty<KeyValuePair<KeyCode, Action>>();
        private KeyValuePair<KeyCode, Action>[] _gameplaySnapshot = Array.Empty<KeyValuePair<KeyCode, Action>>();
        private KeyValuePair<int, Action>[] _mouseSnapshot = Array.Empty<KeyValuePair<int, Action>>();
        private KeyValuePair<int, Action>[] _gameplayMouseSnapshot = Array.Empty<KeyValuePair<int, Action>>();

        /// <summary>当前是否允许玩法输入</summary>
        public bool IsGameplayInputEnabled => _lockTokens.Count == 0;

        public override void Awake()
        {
            base.Awake();
            _blockingUILayerMask = LayerMask.GetMask("LockInput");
        }

        #region 注册 / 注销

        /// <summary>注册一个任何时候都生效的按键回调（UI 类快捷键）</summary>
        public void RegisterKeyDown(KeyCode key, Action action)
        {
            if (action == null) return;
            _always[key] = _always.TryGetValue(key, out var exist) ? exist + action : action;
            RebuildSnapshots();
        }

        public void UnregisterKeyDown(KeyCode key, Action action)
        {
            if (action == null || !_always.TryGetValue(key, out var exist)) return;
            exist -= action;
            if (exist == null) _always.Remove(key);
            else _always[key] = exist;
            RebuildSnapshots();
        }

        /// <summary>注册一个玩法按键回调，输入被锁时不会触发</summary>
        public void RegisterGameplayKeyDown(KeyCode key, Action action)
        {
            if (action == null) return;
            _gameplay[key] = _gameplay.TryGetValue(key, out var exist) ? exist + action : action;
            RebuildSnapshots();
        }

        public void UnregisterGameplayKeyDown(KeyCode key, Action action)
        {
            if (action == null || !_gameplay.TryGetValue(key, out var exist)) return;
            exist -= action;
            if (exist == null) _gameplay.Remove(key);
            else _gameplay[key] = exist;
            RebuildSnapshots();
        }

        /// <summary>注册鼠标按键回调（0 左键 / 1 右键 / 2 中键），任何时候都生效，用于 UI 层</summary>
        public void RegisterMouseDown(int button, Action action)
        {
            if (action == null) return;
            _mouse[button] = _mouse.TryGetValue(button, out var exist) ? exist + action : action;
            RebuildSnapshots();
        }

        public void UnregisterMouseDown(int button, Action action)
        {
            if (action == null || !_mouse.TryGetValue(button, out var exist)) return;
            exist -= action;
            if (exist == null) _mouse.Remove(button);
            else _mouse[button] = exist;
            RebuildSnapshots();
        }

        /// <summary>注册玩法用鼠标按键回调（攻击等），输入被锁时不会触发</summary>
        public void RegisterGameplayMouseDown(int button, Action action)
        {
            if (action == null) return;
            _gameplayMouse[button] = _gameplayMouse.TryGetValue(button, out var exist) ? exist + action : action;
            RebuildSnapshots();
        }

        public void UnregisterGameplayMouseDown(int button, Action action)
        {
            if (action == null || !_gameplayMouse.TryGetValue(button, out var exist)) return;
            exist -= action;
            if (exist == null) _gameplayMouse.Remove(button);
            else _gameplayMouse[button] = exist;
            RebuildSnapshots();
        }

        private void RebuildSnapshots()
        {
            _alwaysSnapshot = ToArray(_always);
            _gameplaySnapshot = ToArray(_gameplay);
            _mouseSnapshot = ToArray(_mouse);
            _gameplayMouseSnapshot = ToArray(_gameplayMouse);
        }

        private static KeyValuePair<TKey, Action>[] ToArray<TKey>(Dictionary<TKey, Action> dict)
        {
            var array = new KeyValuePair<TKey, Action>[dict.Count];
            int i = 0;
            foreach (var kv in dict) array[i++] = kv;
            return array;
        }

        #endregion

        #region 输入锁

        /// <summary>压入一个输入锁，返回用于释放的 token</summary>
        public object PushInputLock()
        {
            var token = new object();
            _lockTokens.Add(token);
            return token;
        }

        /// <summary>释放输入锁，token 无效则忽略</summary>
        public void PopInputLock(object token)
        {
            if (token == null) return;
            _lockTokens.Remove(token);
        }

        /// <summary>强制清空所有输入锁（面板全部销毁时使用）</summary>
        public void PopAllInputLocks() => _lockTokens.Clear();

        #endregion

        #region UI 命中判定

        private static int _blockingUILayerMask;
        private static readonly List<RaycastResult> RaycastBuffer = new();
        private static PointerEventData _pointerBuffer;

        /// <summary>
        /// 鼠标是否正悬停在会屏蔽玩法输入的 UI 上
        /// </summary>
        public static bool IsPointerOverBlockingUI()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _pointerBuffer ??= new PointerEventData(eventSystem);
            _pointerBuffer.position = Input.mousePosition;

            RaycastBuffer.Clear();
            eventSystem.RaycastAll(_pointerBuffer, RaycastBuffer);

            foreach (var result in RaycastBuffer)
            {
                if (result.gameObject == null) continue;
                if (((1 << result.gameObject.layer) & _blockingUILayerMask) != 0) return true;
            }

            return false;
        }

        #endregion

        private void Update()
        {
            foreach (var kv in _alwaysSnapshot)
            {
                if (Input.GetKeyDown(kv.Key)) kv.Value?.Invoke();
            }

            foreach (var kv in _mouseSnapshot)
            {
                if (Input.GetMouseButtonDown(kv.Key)) kv.Value?.Invoke();
            }

            if (!IsGameplayInputEnabled) return;

            foreach (var kv in _gameplaySnapshot)
            {
                if (Input.GetKeyDown(kv.Key)) kv.Value?.Invoke();
            }

            foreach (var kv in _gameplayMouseSnapshot)
            {
                if (Input.GetMouseButtonDown(kv.Key)) kv.Value?.Invoke();
            }
        }

        protected void OnDestroy() => PopAllInputLocks();
    }
}
