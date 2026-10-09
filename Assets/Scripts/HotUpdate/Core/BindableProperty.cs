using System;
using System.Collections.Generic;

namespace HotUpdate.Core
{
    /// <summary>
    /// 可绑定属性。值变化时自动通知订阅者，用于 Model → View 的单向刷新，
    /// 这样 Model 里就不需要再直接引用 UIManager 或具体面板。
    /// </summary>
    public class BindableProperty<T>
    {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        private T _value;

        public BindableProperty(T defaultValue = default)
        {
            _value = defaultValue;
        }

        /// <summary>当前值。赋值时若与旧值相同则不会触发通知。</summary>
        public T Value
        {
            get => _value;
            set
            {
                if (Comparer.Equals(_value, value)) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        /// <summary>值变化回调，参数为新值</summary>
        public event Action<T> OnValueChanged;

        /// <summary>静默赋值，不触发通知</summary>
        public void SetWithoutNotify(T newValue)
        {
            _value = newValue;
        }

        public override string ToString()
        {
            return _value == null ? string.Empty : _value.ToString();
        }
    }
}
