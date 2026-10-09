using HotUpdate.Core;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;

namespace HotUpdate.Controller
{
    public class NpcCtrl : MonoBehaviour
    {
        private Camera _camera;
        private TMP_Text _tipText;
        private bool _isEnter;

        public void Start()
        {
            _camera = Camera.main;
            _tipText = GetComponentInChildren<TMP_Text>();
            _tipText.gameObject.SetActive(false);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
        }

        private void Update()
        {
            // 提示文字始终朝向相机
            if (_tipText.gameObject.activeSelf)
            {
                _tipText.transform.LookAt(_camera.transform.position);
                _tipText.transform.Rotate(0, 180, 0);
            }
        }

        private void OnInteractPressed()
        {
            if (!_isEnter) return;
            // 当前该播哪段对话由 StoryService 决定
            FindAnyObjectByType<DialogueManager>().StartDialogue();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isEnter = true;
                _tipText.gameObject.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isEnter = false;
                _tipText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
        }
    }
}