using System;
using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;
using Object = UnityEngine.Object;

namespace HotUpdate.Manager
{
    /// <summary>
    /// UI 面板管理器：只负责 Canvas 搭建、面板层级 / 显隐 / 生命周期，以及打开面板时的输入锁。
    ///
    /// 预制体的加载与释放已拆到 <see cref="UIAssetLoader"/>，
    /// 面板资源路径由 <see cref="PanelPathAttribute"/> 声明（见 PanelPathResolver）。
    /// </summary>
    public class UIManager
    {
        // 已实例化的面板
        private readonly Dictionary<string, BasePanel> _uiPanelDict = new();

        // Canvas相关
        [HideInInspector] public RectTransform Canvas;
        private Canvas _canvasComponent;
        private CanvasScaler _canvasScaler;

        // 面板容器
        private GameObject _currentPanel;

        #region 不进入面板字典

        // 全屏转场遮罩
        private const string TransitionAddress = "TransitionPanel";
        private TransitionPanel _transitionPanel;

        // 左下角提示条
        private const string ToastAddress = "ToastPanel";
        private ToastPanel _toastPanel;

        #endregion

        private bool _isCanvasInitialized;

        #region 打开面板

        public void OpenPanel<T>(Action<T> onLoadComplete = null) where T : BasePanel
        {
            if (!_isCanvasInitialized || _currentPanel == null)
            {
                Debug.LogError("Canvas未初始化完成，无法打开面板！");
                onLoadComplete?.Invoke(null);
                return;
            }

            string path = PanelPathResolver.Resolve<T>();

            // 面板已存在：恢复原始 Transform 后直接显示
            if (_uiPanelDict.TryGetValue(path, out var existPanel))
            {
                T cached = existPanel as T;
                AppContext.UILoader.RestoreTransform(path, cached.transform, _currentPanel.transform);
                cached.Show();
                PushInputLock(path, cached);
                onLoadComplete?.Invoke(cached);
                return;
            }

            AppContext.UILoader.LoadAsync(path, prefab =>
            {
                if (prefab == null)
                {
                    onLoadComplete?.Invoke(null);
                    return;
                }

                GameObject panelObj = GameObject.Instantiate(prefab, _currentPanel.transform, false);
                panelObj.name = path;

                BasePanel panel = panelObj.GetComponent<T>();
                if (panel == null)
                {
                    Debug.LogError($"面板 {path} 上找不到 {typeof(T).Name} 组件");
                    GameObject.Destroy(panelObj);
                    onLoadComplete?.Invoke(null);
                    return;
                }

                _uiPanelDict.Add(path, panel);
                panel.Show();
                PushInputLock(path, panel);
                onLoadComplete?.Invoke(panel as T);
            });
        }

        /// <summary>
        /// 开 / 关一个面板。由 InputManager 触发，这里不再自己读 Input.GetKeyDown。
        /// </summary>
        public void TogglePanel<T>() where T : BasePanel
        {
            T panel = GetPanel<T>();
            // 正在播关闭动画的面板视作「已关」：再按一次应该重新打开，而不是把关动画重播一遍
            if (panel == null || panel.IsClosing || !panel.gameObject.activeInHierarchy)
            {
                OpenPanel<T>();
                AppContext.Events.EventTrigger(GameEvent.CursorShow);
            }
            else
            {
                ClosePanel<T>();
                AppContext.Events.EventTrigger(GameEvent.CursorHide);
            }
        }

        #endregion

        #region 关闭 / 销毁

        public void ClosePanel<T>() where T : BasePanel
        {
            string path = PanelPathResolver.Resolve<T>();
            if (_uiPanelDict.TryGetValue(path, out var panel)) DoClosePanel(path, panel);
        }

        /// <summary>关闭指定面板实例，供 BasePanel 的关闭按钮回调使用</summary>
        public void ClosePanel(BasePanel panel)
        {
            if (panel == null) return;
            foreach (var kv in _uiPanelDict)
            {
                if (kv.Value != panel) continue;
                DoClosePanel(kv.Key, panel);
                return;
            }

            panel.Hide();
        }

        private void DoClosePanel(string path, BasePanel panel)
        {
            // Hide 会先播关闭动画、动画结束才 SetActive(false)；
            // reparent 与解锁照旧立即执行（_currentPanel 与 Canvas 根都是全屏拉伸且 pivot 一致，
            // 带 worldPositionStays=false 换父节点不会让动画中的面板位移 / 缩放跳变）
            panel.Hide();
            panel.transform.SetParent(Canvas.transform, false);
            PopInputLock(path);
        }

        public void DestroyPanel<T>() where T : BasePanel
        {
            string path = PanelPathResolver.Resolve<T>();
            if (!_uiPanelDict.TryGetValue(path, out var panel)) return;

            GameObject.Destroy(panel.gameObject);
            _uiPanelDict.Remove(path);
            PopInputLock(path);
            AppContext.UILoader.Release(path);
        }

        public void ClearAllPanel()
        {
            // 整体清理（退出游戏 / 卸载资源）不需要动画，直接立即隐藏
            foreach (var kv in _uiPanelDict) kv.Value.HideImmediate();
        }

        /// <summary>
        /// 销毁所有已加载面板并释放其资源句柄。
        /// 用于彻底清理（卸载资源包 / 退出游戏）；日常开关面板请用 ClosePanel，它会缓存实例。
        /// </summary>
        public void DestroyAllPanels()
        {
            foreach (var kv in _uiPanelDict)
            {
                if (kv.Value != null) GameObject.Destroy(kv.Value.gameObject);
            }

            _uiPanelDict.Clear();

            // 面板全没了，输入锁也该全部释放，避免残留锁死玩法操作
            _panelInputLocks.Clear();
            InputManager.Instance.PopAllInputLocks();

            AppContext.UILoader.ReleaseAll();
        }

        #endregion

        public T GetPanel<T>() where T : BasePanel
        {
            string path = PanelPathResolver.Resolve<T>();
            return _uiPanelDict.TryGetValue(path, out var panel) ? panel as T : null;
        }

        #region 输入锁

        /// <summary>面板路径 → 它压入的输入锁 token</summary>
        private readonly Dictionary<string, object> _panelInputLocks = new();

        private void PushInputLock(string path, BasePanel panel)
        {
            if (panel == null || !panel.BlocksGameplayInput) return;
            if (_panelInputLocks.ContainsKey(path)) return;
            _panelInputLocks[path] = InputManager.Instance.PushInputLock();
        }

        private void PopInputLock(string path)
        {
            if (!_panelInputLocks.TryGetValue(path, out var token)) return;
            InputManager.Instance.PopInputLock(token);
            _panelInputLocks.Remove(path);
        }

        #endregion

        #region 转场

        /// <summary>
        /// 全屏转场：淡入遮罩 → 执行 onCovered（开面板 / 切场景）→ 等 TransitionDone() → 淡出。
        /// </summary>
        public void Transition(float minHold, TransitionPanel.ScrollBgType bgType, Action onCovered)
        {
            if (_transitionPanel != null)
            {
                _transitionPanel.Play(minHold, bgType, onCovered);
                return;
            }

            // 预制体还没加载好：等加载完再播，不跳过遮罩
            AppContext.UILoader.LoadAsync(TransitionAddress, prefab =>
            {
                if (prefab == null)
                {
                    onCovered?.Invoke();
                    return;
                }

                EnsureTransition(prefab);
                _transitionPanel?.Play(minHold, bgType, onCovered);
            });
        }

        // 切换完成，可以揭开遮罩了
        public void TransitionDone()
        {
            if (_transitionPanel != null) _transitionPanel.Done();
        }

        private void PreloadTransition()
        {
            AppContext.UILoader.LoadAsync(TransitionAddress, prefab =>
            {
                if (prefab != null) EnsureTransition(prefab);
            });
        }

        private void EnsureTransition(GameObject prefab)
        {
            if (_transitionPanel != null) return;

            GameObject obj = Object.Instantiate(prefab, Canvas, false);
            obj.name = TransitionAddress;
            _transitionPanel = obj.GetComponent<TransitionPanel>();
            _transitionPanel.gameObject.SetActive(false);
        }

        #endregion

        #region 左下角提示

        /// <summary>
        /// 左下角滑入一条提示
        /// </summary>
        public void ShowToast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ShowToasts(new[] { text });
        }

        /// <summary>
        /// 左下角排队滑入一批提示
        /// </summary>
        public void ShowToasts(IList<string> texts)
        {
            if (texts == null) return;

            // 加载是异步的，先把要显示的东西固化下来，别让外部列表之后再变
            List<string> buffer = new List<string>(texts);
            if (buffer.Count == 0) return;

            if (_toastPanel != null)
            {
                _toastPanel.PushBatch(buffer);
                return;
            }

            // 第一次用：等预制体加载完再压入，不丢这些提示
            AppContext.UILoader.LoadAsync(ToastAddress, prefab =>
            {
                if (prefab == null) return;
                EnsureToast(prefab);
                _toastPanel?.PushBatch(buffer);
            });
        }

        private void PreloadToast()
        {
            AppContext.UILoader.LoadAsync(ToastAddress, prefab =>
            {
                if (prefab != null) EnsureToast(prefab);
            });
        }

        private void EnsureToast(GameObject prefab)
        {
            if (_toastPanel != null) return;

            GameObject obj = Object.Instantiate(prefab, Canvas, false);
            obj.name = ToastAddress;
            _toastPanel = obj.GetComponent<ToastPanel>();
            _toastPanel.gameObject.SetActive(false);
        }

        #endregion

        #region Canvas 初始化

        public void Init()
        {
            // 幂等：重复初始化会再 new 一个 Canvas + EventSystem 并覆盖引用，旧的留在场景里（Hierarchy 里一堆 Canvas）
            if (_isCanvasInitialized && Canvas != null) return;

            InitCanvas();
            InitEventSystem();
            _isCanvasInitialized = true;

            // 提前把转场 / 提示条加载好，第一次用的时候才不会卡在加载上
            PreloadTransition();
            PreloadToast();
        }

        public void ShowTip(string text, float showTime = 0f)
        {
            OpenPanel<TipPanel>(panel =>
            {
                if (showTime > 0f) panel.showTime = showTime;
                panel.ShowTip(text);
            });
        }

        private void InitCanvas()
        {
            GameObject canvasObj = Resources.Load<GameObject>("Canvas") ?? new GameObject("Canvas");
            if (canvasObj.name != "Canvas")
            {
                canvasObj.name = "Canvas";
                _canvasComponent = canvasObj.AddComponent<Canvas>();
                _canvasScaler = canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                _canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _canvasScaler.referenceResolution = new Vector2(1920, 1080);
            }
            else
            {
                canvasObj = Object.Instantiate(canvasObj);
                canvasObj.name = "Canvas";
                _canvasComponent = canvasObj.GetComponent<Canvas>();
                _canvasScaler = canvasObj.GetComponent<CanvasScaler>();
            }

            Canvas = canvasObj.transform as RectTransform;
            Object.DontDestroyOnLoad(canvasObj);

            _currentPanel = new GameObject("currentShowPanel");
            RectTransform rect = _currentPanel.AddComponent<RectTransform>();
            rect.SetParent(Canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private void InitEventSystem()
        {
            var eventObj = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventObj is not null) Object.Destroy(eventObj);

            GameObject eventSystemObj = Resources.Load<GameObject>("EventSystem") ?? new GameObject("EventSystem");
            if (eventSystemObj.name != "EventSystem")
            {
                eventSystemObj.name = "EventSystem";
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else
            {
                eventSystemObj = Object.Instantiate(eventSystemObj);
            }

            Object.DontDestroyOnLoad(eventSystemObj);
        }

        #endregion
    }
}