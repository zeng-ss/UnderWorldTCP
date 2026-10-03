using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.dll",
		"DOTween.dll",
		"Newtonsoft.Json.dll",
		"System.Core.dll",
		"System.dll",
		"Unity.Addressables.dll",
		"Unity.Netcode.Runtime.dll",
		"Unity.RenderPipelines.Core.Runtime.dll",
		"Unity.ResourceManager.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// DG.Tweening.Core.DOGetter<float>
	// DG.Tweening.Core.DOGetter<int>
	// DG.Tweening.Core.DOSetter<float>
	// DG.Tweening.Core.DOSetter<int>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// DelegateList<float>
	// SingleTonMono<object>
	// System.Action<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Action<UnityEngine.EventSystems.RaycastResult>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Action<UnityEngine.Vector3,UnityEngine.Quaternion>
	// System.Action<float>
	// System.Action<int>
	// System.Action<object,object>
	// System.Action<object>
	// System.Action<ulong>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.Comparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.Comparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Comparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<ulong,object>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,UIManager.RectTransformData>
	// System.Collections.Generic.Dictionary<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<ulong,object>
	// System.Collections.Generic.EqualityComparer<UIManager.RectTransformData>
	// System.Collections.Generic.EqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.EqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<ulong>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UIManager.RectTransformData>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ICollection<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ICollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.ICollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IComparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.IComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,LitJson.ArrayMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.ObjectMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.PropertyMetadata>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UIManager.RectTransformData>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerable<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UIManager.RectTransformData>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerator<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IEnumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.IEnumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<ulong>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IList<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.IList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,UIManager.RectTransformData>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<ulong,object>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.List.Enumerator<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.List.Enumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.List.Enumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ObjectComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<UIManager.RectTransformData>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<ulong>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Comparison<UnityEngine.EventSystems.RaycastResult>
	// System.Comparison<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Comparison<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<System.Collections.Generic.KeyValuePair<int,int>,byte>
	// System.Func<System.Collections.Generic.KeyValuePair<int,int>,int>
	// System.Func<UnityEngine.EventSystems.RaycastResult,byte>
	// System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>,byte>
	// System.Func<int,byte>
	// System.Func<object,byte>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<byte>
	// System.IEquatable<float>
	// System.Linq.Enumerable.Iterator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Linq.Enumerable.Iterator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Linq.Enumerable.Iterator<int>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Linq.Enumerable.WhereArrayIterator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Linq.Enumerable.WhereArrayIterator<int>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Linq.Enumerable.WhereEnumerableIterator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Linq.Enumerable.WhereEnumerableIterator<int>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Linq.Enumerable.WhereListIterator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Linq.Enumerable.WhereListIterator<int>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<System.Collections.Generic.KeyValuePair<int,int>,int>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<System.Collections.Generic.KeyValuePair<int,int>,int>
	// System.Linq.Enumerable.WhereSelectListIterator<System.Collections.Generic.KeyValuePair<int,int>,int>
	// System.Predicate<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Predicate<UnityEngine.EventSystems.RaycastResult>
	// System.Predicate<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Predicate<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskCompletionSource<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory<object>
	// Unity.Netcode.BufferSerializer<Unity.Netcode.BufferSerializerReader>
	// Unity.Netcode.BufferSerializer<Unity.Netcode.BufferSerializerWriter>
	// Unity.Netcode.BufferSerializer<object>
	// Unity.Netcode.FallbackSerializer<PlayerData>
	// Unity.Netcode.FallbackSerializer<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.FallbackSerializer<byte>
	// Unity.Netcode.FallbackSerializer<float>
	// Unity.Netcode.FallbackSerializer<ulong>
	// Unity.Netcode.INetworkVariableSerializer<PlayerData>
	// Unity.Netcode.INetworkVariableSerializer<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.INetworkVariableSerializer<byte>
	// Unity.Netcode.INetworkVariableSerializer<float>
	// Unity.Netcode.INetworkVariableSerializer<ulong>
	// Unity.Netcode.NetworkVariable.CheckExceedsDirtinessThresholdDelegate<PlayerData>
	// Unity.Netcode.NetworkVariable.CheckExceedsDirtinessThresholdDelegate<byte>
	// Unity.Netcode.NetworkVariable.CheckExceedsDirtinessThresholdDelegate<float>
	// Unity.Netcode.NetworkVariable.CheckExceedsDirtinessThresholdDelegate<ulong>
	// Unity.Netcode.NetworkVariable.OnValueChangedDelegate<PlayerData>
	// Unity.Netcode.NetworkVariable.OnValueChangedDelegate<byte>
	// Unity.Netcode.NetworkVariable.OnValueChangedDelegate<float>
	// Unity.Netcode.NetworkVariable.OnValueChangedDelegate<ulong>
	// Unity.Netcode.NetworkVariable<PlayerData>
	// Unity.Netcode.NetworkVariable<byte>
	// Unity.Netcode.NetworkVariable<float>
	// Unity.Netcode.NetworkVariable<ulong>
	// Unity.Netcode.NetworkVariableSerialization.EqualsDelegate<PlayerData>
	// Unity.Netcode.NetworkVariableSerialization.EqualsDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.NetworkVariableSerialization.EqualsDelegate<byte>
	// Unity.Netcode.NetworkVariableSerialization.EqualsDelegate<float>
	// Unity.Netcode.NetworkVariableSerialization.EqualsDelegate<ulong>
	// Unity.Netcode.NetworkVariableSerialization<PlayerData>
	// Unity.Netcode.NetworkVariableSerialization<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.NetworkVariableSerialization<byte>
	// Unity.Netcode.NetworkVariableSerialization<float>
	// Unity.Netcode.NetworkVariableSerialization<ulong>
	// Unity.Netcode.UnmanagedNetworkSerializableSerializer<PlayerData>
	// Unity.Netcode.UnmanagedNetworkSerializableSerializer<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UnmanagedTypeSerializer<byte>
	// Unity.Netcode.UnmanagedTypeSerializer<float>
	// Unity.Netcode.UserNetworkVariableSerialization.DuplicateValueDelegate<PlayerData>
	// Unity.Netcode.UserNetworkVariableSerialization.DuplicateValueDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UserNetworkVariableSerialization.DuplicateValueDelegate<byte>
	// Unity.Netcode.UserNetworkVariableSerialization.DuplicateValueDelegate<float>
	// Unity.Netcode.UserNetworkVariableSerialization.DuplicateValueDelegate<ulong>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadDeltaDelegate<PlayerData>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadDeltaDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadDeltaDelegate<byte>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadDeltaDelegate<float>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadDeltaDelegate<ulong>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadValueDelegate<PlayerData>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadValueDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadValueDelegate<byte>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadValueDelegate<float>
	// Unity.Netcode.UserNetworkVariableSerialization.ReadValueDelegate<ulong>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteDeltaDelegate<PlayerData>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteDeltaDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteDeltaDelegate<byte>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteDeltaDelegate<float>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteDeltaDelegate<ulong>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteValueDelegate<PlayerData>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteValueDelegate<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteValueDelegate<byte>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteValueDelegate<float>
	// Unity.Netcode.UserNetworkVariableSerialization.WriteValueDelegate<ulong>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass79_0<object>
	// UnityEngine.Events.InvokableCall<byte>
	// UnityEngine.Events.InvokableCall<int>
	// UnityEngine.Events.InvokableCall<object>
	// UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,int>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityAction<float>
	// UnityEngine.Events.UnityAction<int>
	// UnityEngine.Events.UnityAction<object,object>
	// UnityEngine.Events.UnityAction<object>
	// UnityEngine.Events.UnityEvent<byte>
	// UnityEngine.Events.UnityEvent<int>
	// UnityEngine.Events.UnityEvent<object>
	// UnityEngine.Rendering.VolumeParameter<float>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass60_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass61_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>
	// UnityEngine.ResourceManagement.ChainOperationTypelessDepedency<object>
	// UnityEngine.ResourceManagement.ResourceManager.CompletedOperation<object>
	// UnityEngine.ResourceManagement.Util.GlobalLinkedListNodeCache<object>
	// UnityEngine.ResourceManagement.Util.LinkedListNodeCache<object>
	// UnitySingleTonMono<object>
	// }}

	public void RefMethods()
	{
		// object DG.Tweening.TweenExtensions.Play<object>(object)
		// object DG.Tweening.TweenSettingsExtensions.OnComplete<object>(object,DG.Tweening.TweenCallback)
		// object DG.Tweening.TweenSettingsExtensions.OnUpdate<object>(object,DG.Tweening.TweenCallback)
		// object DG.Tweening.TweenSettingsExtensions.SetDelay<object>(object,float)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,DG.Tweening.Ease)
		// object JsonMgr.LoadData<object>(string,string,JsonType)
		// object LitJson.JsonMapper.ToObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// bool State_Machine.ChangeState<object>(bool)
		// State_Base State_Machine.GetType<object>()
		// PlayerData System.Activator.CreateInstance<PlayerData>()
		// Unity.Netcode.NetworkObjectReference System.Activator.CreateInstance<Unity.Netcode.NetworkObjectReference>()
		// object System.Activator.CreateInstance<object>()
		// bool System.Linq.Enumerable.Any<UnityEngine.EventSystems.RaycastResult>(System.Collections.Generic.IEnumerable<UnityEngine.EventSystems.RaycastResult>,System.Func<UnityEngine.EventSystems.RaycastResult,bool>)
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<System.Collections.Generic.KeyValuePair<int,int>,int>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>,System.Func<System.Collections.Generic.KeyValuePair<int,int>,int>)
		// System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>> System.Linq.Enumerable.Where<System.Collections.Generic.KeyValuePair<int,int>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>,System.Func<System.Collections.Generic.KeyValuePair<int,int>,bool>)
		// System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>> System.Linq.Enumerable.Where<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>(System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>,System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>,bool>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>,System.Func<int,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Iterator<System.Collections.Generic.KeyValuePair<int,int>>.Select<int>(System.Func<System.Collections.Generic.KeyValuePair<int,int>,int>)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<PlayerData>(PlayerData&)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>(Unity.Netcode.Components.NetworkTransform.NetworkTransformState&)
		// System.Void Unity.Netcode.BufferSerializer<object>.SerializeValue<byte>(byte&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.BufferSerializer<object>.SerializeValue<ulong>(ulong&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.FastBufferReader.ReadNetworkSerializable<PlayerData>(PlayerData&)
		// System.Void Unity.Netcode.FastBufferReader.ReadNetworkSerializable<Unity.Netcode.NetworkObjectReference>(Unity.Netcode.NetworkObjectReference&)
		// System.Void Unity.Netcode.FastBufferReader.ReadNetworkSerializable<object>(object&)
		// System.Void Unity.Netcode.FastBufferReader.ReadUnmanagedSafe<byte>(byte&)
		// System.Void Unity.Netcode.FastBufferReader.ReadUnmanagedSafe<float>(float&)
		// System.Void Unity.Netcode.FastBufferReader.ReadValueSafe<PlayerData>(PlayerData&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.FastBufferReader.ReadValueSafe<Unity.Netcode.NetworkObjectReference>(Unity.Netcode.NetworkObjectReference&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.FastBufferReader.ReadValueSafe<byte>(byte&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.FastBufferReader.ReadValueSafe<float>(float&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.FastBufferReader.ReadValueSafe<object>(object&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.FastBufferWriter.WriteNetworkSerializable<PlayerData>(PlayerData&)
		// System.Void Unity.Netcode.FastBufferWriter.WriteNetworkSerializable<Unity.Netcode.NetworkObjectReference>(Unity.Netcode.NetworkObjectReference&)
		// System.Void Unity.Netcode.FastBufferWriter.WriteNetworkSerializable<object>(object&)
		// System.Void Unity.Netcode.FastBufferWriter.WriteUnmanagedSafe<byte>(byte&)
		// System.Void Unity.Netcode.FastBufferWriter.WriteUnmanagedSafe<float>(float&)
		// System.Void Unity.Netcode.FastBufferWriter.WriteValueSafe<PlayerData>(PlayerData&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.FastBufferWriter.WriteValueSafe<Unity.Netcode.NetworkObjectReference>(Unity.Netcode.NetworkObjectReference&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.FastBufferWriter.WriteValueSafe<byte>(byte&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.FastBufferWriter.WriteValueSafe<float>(float&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.FastBufferWriter.WriteValueSafe<object>(object&,Unity.Netcode.FastBufferWriter.ForNetworkSerializable)
		// System.Void Unity.Netcode.INetworkSerializable.NetworkSerialize<Unity.Netcode.BufferSerializerReader>(Unity.Netcode.BufferSerializer<Unity.Netcode.BufferSerializerReader>)
		// System.Void Unity.Netcode.INetworkSerializable.NetworkSerialize<Unity.Netcode.BufferSerializerWriter>(Unity.Netcode.BufferSerializer<Unity.Netcode.BufferSerializerWriter>)
		// System.Void Unity.Netcode.IReaderWriter.SerializeValue<byte>(byte&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// System.Void Unity.Netcode.IReaderWriter.SerializeValue<ulong>(ulong&,Unity.Netcode.FastBufferWriter.ForPrimitives)
		// bool Unity.Netcode.NetworkVariableSerialization<byte>.EqualityEquals<byte>(byte&,byte&)
		// bool Unity.Netcode.NetworkVariableSerialization<float>.EqualityEquals<float>(float&,float&)
		// bool Unity.Netcode.NetworkVariableSerialization<PlayerData>.ValueEquals<PlayerData>(PlayerData&,PlayerData&)
		// bool Unity.Netcode.NetworkVariableSerialization<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>.ValueEquals<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>(Unity.Netcode.Components.NetworkTransform.NetworkTransformState&,Unity.Netcode.Components.NetworkTransform.NetworkTransformState&)
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable<byte>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable<float>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedValueEquals<PlayerData>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedValueEquals<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy<byte>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy<float>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedINetworkSerializable<PlayerData>()
		// System.Void Unity.Netcode.NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedINetworkSerializable<Unity.Netcode.Components.NetworkTransform.NetworkTransformState>()
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<object>(object)
		// System.Void UnityEngine.AddressableAssets.Addressables.Release<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetAsync<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.TrackHandle<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// bool UnityEngine.GameObject.TryGetComponent<object>(object&)
		// object UnityEngine.JsonUtility.FromJson<object>(string)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object[] UnityEngine.Object.FindObjectsOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// bool UnityEngine.Rendering.VolumeProfile.TryGet<object>(System.Type,object&)
		// bool UnityEngine.Rendering.VolumeProfile.TryGet<object>(object&)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Convert<object>()
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateChainOperation<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateCompletedOperationInternal<object>(object,bool,System.Exception,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateCompletedOperationWithException<object>(object,System.Exception)
		// object UnityEngine.ResourceManagement.ResourceManager.CreateOperation<object>(System.Type,int,UnityEngine.ResourceManagement.Util.IOperationCacheKey,System.Action<UnityEngine.ResourceManagement.AsyncOperations.IAsyncOperation>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.ProvideResource<object>(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.StartOperation<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<object>,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)
		// object[] UnityEngine.Resources.ConvertObjects<object>(UnityEngine.Object[])
		// object UnityEngine.Resources.Load<object>(string)
	}
}