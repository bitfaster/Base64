using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Base64
{
	public class ObjectPool<T>
			where T : class
	{
		[DebuggerDisplay("{Value,nq}")]
		private struct Element
		{
			internal T Value;
		}

		/// <remarks>
		/// Not using System.Func{T} because this file is linked into the (debugger) Formatter,
		/// which does not have that type (since it compiles against .NET 2.0).
		/// </remarks>
		public delegate T Factory();

		// Storage for the pool objects. The first item is stored in a dedicated field because we
		// expect to be able to satisfy most requests from it.
		private T _firstItem;
		private readonly Element[] _items;

		// factory is stored for the lifetime of the pool. We will call this only when pool needs to
		// expand. compared to "new T()", Func gives more flexibility to implementers and faster
		// than "new T()".
		private readonly Factory _factory;

#if DETECT_LEAKS
        private static readonly ConditionalWeakTable<T, LeakTracker> leakTrackers = new ConditionalWeakTable<T, LeakTracker>();

        private class LeakTracker : IDisposable
        {
            private volatile bool disposed;

#if TRACE_LEAKS
            internal volatile object Trace = null;
#endif

            public void Dispose()
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }

            private string GetTrace()
            {
#if TRACE_LEAKS
                return Trace == null ? "" : Trace.ToString();
#else
                return "Leak tracing information is disabled. Define TRACE_LEAKS on ObjectPool`1.cs to get more info \n";
#endif
            }

            ~LeakTracker()
            {
                if (!this.disposed && !Environment.HasShutdownStarted)
                {
                    var trace = GetTrace();

                    // If you are seeing this message it means that object has been allocated from the pool 
                    // and has not been returned back. This is not critical, but turns pool into rather 
                    // inefficient kind of "new".
                    Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nPool detected potential leaking of {typeof(T)}. \n Location of the leak: \n {GetTrace()} TRACEOBJECTPOOLLEAKS_END");
                }
            }
        }
#endif

		public ObjectPool(Factory factory)
			: this(factory, Environment.ProcessorCount * 2)
		{
		}

		public ObjectPool(Factory factory, int size)
		{
			Debug.Assert(size >= 1);
			_factory = factory;
			_items = new Element[size - 1];
		}

		private T CreateInstance()
		{
			var inst = _factory();
			return inst;
		}

		/// <summary>
		/// Produces an instance.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically 
		/// reducing how far we will typically search.
		/// </remarks>
		public T Allocate()
		{
			// PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
			// Note that the initial read is optimistically not synchronized. That is intentional. 
			// We will interlock only when we have a candidate. in a worst case we may miss some
			// recently returned objects. Not a big deal.
			T inst = _firstItem;
			if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
			{
				inst = AllocateSlow();
			}

#if DETECT_LEAKS
            var tracker = new LeakTracker();
            leakTrackers.Add(inst, tracker);

#if TRACE_LEAKS
            var frame = CaptureStackTrace();
            tracker.Trace = frame;
#endif
#endif
			return inst;
		}

		private T AllocateSlow()
		{
			var items = _items;

			for (int i = 0; i < items.Length; i++)
			{
				// Note that the initial read is optimistically not synchronized. That is intentional. 
				// We will interlock only when we have a candidate. in a worst case we may miss some
				// recently returned objects. Not a big deal.
				T inst = items[i].Value;
				if (inst != null)
				{
					if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
					{
						return inst;
					}
				}
			}

			return CreateInstance();
		}

		/// <summary>
		/// Returns objects to the pool.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically 
		/// reducing how far we will typically search in Allocate.
		/// </remarks>
		public void Free(T obj)
		{
			Validate(obj);
			ForgetTrackedObject(obj);

			if (_firstItem == null)
			{
				// Intentionally not using interlocked here. 
				// In a worst case scenario two objects may be stored into same slot.
				// It is very unlikely to happen and will only mean that one of the objects will get collected.
				_firstItem = obj;
			}
			else
			{
				FreeSlow(obj);
			}
		}

		private void FreeSlow(T obj)
		{
			var items = _items;
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i].Value == null)
				{
					// Intentionally not using interlocked here. 
					// In a worst case scenario two objects may be stored into same slot.
					// It is very unlikely to happen and will only mean that one of the objects will get collected.
					items[i].Value = obj;
					break;
				}
			}
		}

		/// <summary>
		/// Removes an object from leak tracking.  
		/// 
		/// This is called when an object is returned to the pool.  It may also be explicitly 
		/// called if an object allocated from the pool is intentionally not being returned
		/// to the pool.  This can be of use with pooled arrays if the consumer wants to 
		/// return a larger array to the pool than was originally allocated.
		/// </summary>
		[Conditional("DEBUG")]
		public void ForgetTrackedObject(T old, T replacement = null)
		{
#if DETECT_LEAKS
            LeakTracker tracker;
            if (leakTrackers.TryGetValue(old, out tracker))
            {
                tracker.Dispose();
                leakTrackers.Remove(old);
            }
            else
            {
                var trace = CaptureStackTrace();
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
            }

            if (replacement != null)
            {
                tracker = new LeakTracker();
                leakTrackers.Add(replacement, tracker);
            }
#endif
		}

#if DETECT_LEAKS
        private static Lazy<Type> _stackTraceType = new Lazy<Type>(() => Type.GetType("System.Diagnostics.StackTrace"));

        private static object CaptureStackTrace()
        {
            return Activator.CreateInstance(_stackTraceType.Value);
        }
#endif

		[Conditional("DEBUG")]
		private void Validate(object obj)
		{
			Debug.Assert(obj != null, "freeing null?");

			Debug.Assert(_firstItem != obj, "freeing twice?");

			var items = _items;
			for (int i = 0; i < items.Length; i++)
			{
				var value = items[i].Value;
				if (value == null)
				{
					return;
				}

				Debug.Assert(value != obj, "freeing twice?");
			}
		}
	}
}
