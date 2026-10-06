using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.dll",
		"DOTween.dll",
		"Google.Protobuf.dll",
		"Network.dll",
		"System.Core.dll",
		"System.dll",
		"Unity.Addressables.dll",
		"Unity.RenderPipelines.Core.Runtime.dll",
		"Unity.ResourceManager.dll",
		"UnityEngine.CoreModule.dll",
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
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// DelegateList<float>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__28<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<object>
	// Google.Protobuf.FieldCodec.InputMerger<object>
	// Google.Protobuf.FieldCodec.ValuesMerger<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.IDeepCloneable<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// Google.Protobuf.ValueReader<object>
	// Google.Protobuf.ValueWriter<object>
	// SingleTonMono<object>
	// Singleton<object>
	// System.Action<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Action<UnityEngine.EventSystems.RaycastResult>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Action<UnityEngine.Vector3,UnityEngine.Quaternion>
	// System.Action<byte>
	// System.Action<float>
	// System.Action<int,object>
	// System.Action<int>
	// System.Action<object,object>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ArraySortHelper<byte>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.Comparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.Comparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.ComparisonComparer<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ComparisonComparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ComparisonComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ComparisonComparer<byte>
	// System.Collections.Generic.ComparisonComparer<int>
	// System.Collections.Generic.ComparisonComparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<ulong,object>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.Dictionary<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.Dictionary<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<ulong,object>
	// System.Collections.Generic.EqualityComparer<UIAssetLoader.RectTransformData>
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
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UIAssetLoader.RectTransformData>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ICollection<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ICollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ICollection<byte>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IComparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UIAssetLoader.RectTransformData>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerable<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UIAssetLoader.RectTransformData>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerator<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IEnumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<ulong>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IList<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.IList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IList<byte>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyDictionary<int,int>
	// System.Collections.Generic.IReadOnlyDictionary<int,object>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,UIAssetLoader.RectTransformData>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<ulong,object>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.List.Enumerator<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.List.Enumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List.Enumerator<byte>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.Generic.ObjectComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<UIAssetLoader.RectTransformData>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<ulong>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.EventSystems.RaycastResult>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.ObjectModel.ReadOnlyCollection<byte>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Comparison<UnityEngine.EventSystems.RaycastResult>
	// System.Comparison<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Comparison<byte>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<System.Collections.Generic.KeyValuePair<ulong,object>,byte>
	// System.Func<UnityEngine.EventSystems.RaycastResult,byte>
	// System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Func<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Func<byte>
	// System.Func<int,byte>
	// System.Func<object,UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Func<object,byte>
	// System.Func<object,int>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.Linq.Enumerable.Iterator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Linq.Enumerable.Iterator<int>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Linq.Enumerable.WhereArrayIterator<int>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Linq.Enumerable.WhereEnumerableIterator<int>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Linq.Enumerable.WhereListIterator<int>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Predicate<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Predicate<UnityEngine.EventSystems.RaycastResult>
	// System.Predicate<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Predicate<byte>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskCompletionSource<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.TaskCompletionSource<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.TaskFactory<object>
	// System.Threading.ThreadLocal.FinalizationHelper<object>
	// System.Threading.ThreadLocal.IdManager<object>
	// System.Threading.ThreadLocal.LinkedSlot<object>
	// System.Threading.ThreadLocal<object>
	// System.ValueTuple<object,object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass81_0<object>
	// UnityEngine.Awaitable.<>c<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.Awaitable.<>c<object>
	// UnityEngine.Awaitable.Awaiter<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.Awaitable.Awaiter<object>
	// UnityEngine.Awaitable<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.Awaitable<object>
	// UnityEngine.AwaitableCompletionSource<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.AwaitableCompletionSource<object>
	// UnityEngine.Events.InvokableCall<byte>
	// UnityEngine.Events.InvokableCall<int>
	// UnityEngine.Events.InvokableCall<object>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityAction<int>
	// UnityEngine.Events.UnityAction<object>
	// UnityEngine.Events.UnityEvent<byte>
	// UnityEngine.Events.UnityEvent<int>
	// UnityEngine.Events.UnityEvent<object>
	// UnityEngine.Rendering.VolumeParameter<float>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass66_0<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass66_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.TypelessCompletedCallback<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.TypelessCompletedCallback<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c__DisplayClass52_0<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c__DisplayClass52_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>
	// UnityEngine.ResourceManagement.ChainOperationTypelessDepedency<object>
	// UnityEngine.ResourceManagement.Exceptions.AsyncOperationHandleException<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.Exceptions.AsyncOperationHandleException<object>
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
		// bool StateMachine.ChangeState<object>(bool)
		// StateBase StateMachine.GetType<object>()
		// object System.Activator.CreateInstance<object>()
		// System.Collections.Generic.KeyValuePair<int,object>[] System.Array.Empty<System.Collections.Generic.KeyValuePair<int,object>>()
		// bool System.Linq.Enumerable.Any<UnityEngine.EventSystems.RaycastResult>(System.Collections.Generic.IEnumerable<UnityEngine.EventSystems.RaycastResult>,System.Func<UnityEngine.EventSystems.RaycastResult,bool>)
		// System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,object>> System.Linq.Enumerable.Where<System.Collections.Generic.KeyValuePair<ulong,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,object>>,System.Func<System.Collections.Generic.KeyValuePair<ulong,object>,bool>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>,System.Func<int,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object& Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<object,object>(object&)
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
		// bool UnityEngine.GameObject.TryGetComponent<object>(object&)
		// object UnityEngine.Object.FindAnyObjectByType<object>()
		// object UnityEngine.Object.FindAnyObjectByType<object>(UnityEngine.FindObjectsInactive)
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