using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UniRx
{
	public static partial class Scheduler
	{
		[RuntimeInitializeOnLoadMethod]
		public static void SetDefaultForUnity()
		{
			Scheduler.DefaultSchedulers.ConstantTimeOperations = ImmediateScheduler.Instance;
			Scheduler.DefaultSchedulers.TailRecursion = ImmediateScheduler.Instance;
			Scheduler.DefaultSchedulers.Iteration = CurrentThreadScheduler.Instance;

            Scheduler.DefaultSchedulers.TimeBasedOperations = Scheduler.MainThread;
#if UNITY_WEBGL
            Scheduler.DefaultSchedulers.AsyncConversions = Scheduler.MainThread;
#else
            Scheduler.SchedulerDefaults.AsyncConversions = ThreadPoolOnlyScheduler.Instance;
#endif
		}

		static IScheduler mainThread;

		/// <summary>
		/// Unity native MainThread Queue Scheduler. Run on mainthread and delayed on coroutine update loop, elapsed time is calculated based on Time.time.
		/// </summary>
		public static IScheduler MainThread
		{
			get
			{
				return mainThread ?? (mainThread = new MainThreadScheduler());
			}
		}

		static IScheduler mainThreadIgnoreTimeScale;

		/// <summary>
		/// Another MainThread scheduler, delay elapsed time is calculated based on Time.unscaledDeltaTime.
		/// </summary>
		public static IScheduler MainThreadIgnoreTimeScale
		{
			get
			{
				return mainThreadIgnoreTimeScale ?? (mainThreadIgnoreTimeScale = new IgnoreTimeScaleMainThreadScheduler());
			}
		}

		static IScheduler mainThreadFixedUpdate;

		/// <summary>
		/// Run on fixed update mainthread, delay elapsed time is calculated based on Time.fixedTime.
		/// </summary>
		public static IScheduler MainThreadFixedUpdate
		{
			get
			{
				return mainThreadFixedUpdate ?? (mainThreadFixedUpdate = new FixedUpdateMainThreadScheduler());
			}
		}

		static IScheduler mainThreadEndOfFrame;

		/// <summary>
		/// Run on end of frame mainthread, delay elapsed time is calculated based on Time.deltaTime.
		/// </summary>
		public static IScheduler MainThreadEndOfFrame
		{
			get
			{
				return mainThreadEndOfFrame ?? (mainThreadEndOfFrame = new EndOfFrameMainThreadScheduler());
			}
		}

		abstract class BaseUnityScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
		{
			public DateTimeOffset Now
			{
				get { return Scheduler.Now; }
			}

			public IDisposable Schedule<TState>(TState state,DateTimeOffset dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state,dueTime - Now, action);
			}

			public IDisposable Schedule<TState>(TState state,TimeSpan dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				var d = new BooleanDisposable();
				var time = Scheduler.Normalize(dueTime);

				Iterate(DelayAction(time, () => action(this,state), d));

				return d;
			}

			public IDisposable SchedulePeriodic<TState>(TState state,TimeSpan period,Func<TState,TState> action)
			{
				var d = new BooleanDisposable();
				var time = Scheduler.Normalize(period);

				Iterate(PeriodicAction(time, () => state = action(state), d));

				return d;
			}

			protected abstract void Iterate(IEnumerator itor);
			protected abstract IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation);
			protected abstract IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation);

			public abstract IDisposable Schedule<TState>(TState state,Func<IScheduler,TState,IDisposable> action);

			public abstract void ScheduleQueueing<T>(ICancelable cancel,T state,Action<T> action);
		}

		static class QueuedAction<T>
		{
			public static readonly Action<object> Instance = new Action<object>(Invoke);

			public static void Post(ICancelable cancel, T state, Action<T> action)
			{
				MainThreadDispatcher.Post(Instance, Tuple.Create(cancel, state, action));
			}

			public static void Invoke(object state)
			{
				var t = (Tuple<ICancelable, T, Action<T>>)state;

				if (!t.Item1.IsDisposed)
				{
					t.Item3(t.Item2);
				}
			}
		}
		class MainThreadScheduler : BaseUnityScheduler
		{
			readonly Action<object> scheduleAction;

			public MainThreadScheduler()
			{
				MainThreadDispatcher.Initialize();
				scheduleAction = new Action<object>(Schedule);
			}

			// delay action is run in StartCoroutine
			// Okay to action run synchronous and guaranteed run on MainThread
			protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
			{
				// zero == every frame
				if (dueTime == TimeSpan.Zero)
				{
					yield return null; // not immediately, run next frame
				}
				else
				{
					yield return new WaitForSeconds((float)dueTime.TotalSeconds);
				}

				if (cancellation.IsDisposed) yield break;
				MainThreadDispatcher.UnsafeSend(action);
			}

			protected override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
			{
				// zero == every frame
				if (period == TimeSpan.Zero)
				{
					while (true)
					{
						yield return null; // not immediately, run next frame
						if (cancellation.IsDisposed) yield break;

						MainThreadDispatcher.UnsafeSend(action);
					}
				}
				else
				{
					var seconds = (float)(period.TotalMilliseconds / 1000.0);
					var yieldInstruction = new WaitForSeconds(seconds); // cache single instruction object

					while (true)
					{
						yield return yieldInstruction;
						if (cancellation.IsDisposed) yield break;

						MainThreadDispatcher.UnsafeSend(action);
					}
				}
			}

			void Schedule(object state)
			{
				var t = (Tuple<BooleanDisposable, Action>)state;
				if (!t.Item1.IsDisposed)
				{
					t.Item2();
				}
			}

			public override IDisposable Schedule<T>(T state,Func<IScheduler,T,IDisposable> action)
			{
				var d = new BooleanDisposable();
				QueuedAction<T>.Post(d, state, (s) => action(this,s));
				return d;
			}

			protected override void Iterate(IEnumerator itor)
			{
				MainThreadDispatcher.SendStartCoroutine(itor);
			}

			void ScheduleQueueing<T>(object state)
			{
				var t = (Tuple<ICancelable, T, Action<T>>)state;
				if (!t.Item1.IsDisposed)
				{
					t.Item3(t.Item2);
				}
			}

			public override void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
			{
				QueuedAction<T>.Post(cancel, state, action);
			}
		}

		class IgnoreTimeScaleMainThreadScheduler : BaseUnityScheduler
		{
			readonly Action<object> scheduleAction;

			public IgnoreTimeScaleMainThreadScheduler()
			{
				MainThreadDispatcher.Initialize();
				scheduleAction = new Action<object>(Schedule);
			}

			protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
			{
				if (dueTime == TimeSpan.Zero)
				{
					yield return null;
					if (cancellation.IsDisposed) yield break;

					MainThreadDispatcher.UnsafeSend(action);
				}
				else
				{
					var elapsed = 0f;
					var dt = (float)dueTime.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						elapsed += Time.unscaledDeltaTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							break;
						}
					}
				}
			}

			protected override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
			{
				// zero == every frame
				if (period == TimeSpan.Zero)
				{
					while (true)
					{
						yield return null; // not immediately, run next frame
						if (cancellation.IsDisposed) yield break;

						MainThreadDispatcher.UnsafeSend(action);
					}
				}
				else
				{
					var elapsed = 0f;
					var dt = (float)period.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						elapsed += Time.unscaledDeltaTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							elapsed = 0;
						}
					}
				}
			}

			void Schedule(object state)
			{
				var t = (Tuple<BooleanDisposable, Action>)state;
				if (!t.Item1.IsDisposed)
				{
					t.Item2();
				}
			}

			public override IDisposable Schedule<T>(T state,Func<IScheduler,T,IDisposable> action)
			{
				var d = new BooleanDisposable();
				QueuedAction<T>.Post(d, state, (s) => action(this,s));
				return d;
			}

			protected override void Iterate(IEnumerator itor)
			{
				MainThreadDispatcher.SendStartCoroutine(itor);
			}

			public override void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
			{
				QueuedAction<T>.Post(cancel, state, action);
			}
		}

		class FixedUpdateMainThreadScheduler : BaseUnityScheduler
		{
			public FixedUpdateMainThreadScheduler()
			{
				MainThreadDispatcher.Initialize();
			}

			IEnumerator ImmediateAction<T>(T state, Action<T> action, ICancelable cancellation)
			{
				yield return null;
				if (cancellation.IsDisposed) yield break;

				MainThreadDispatcher.UnsafeSend(action, state);
			}

			protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
			{
				if (dueTime == TimeSpan.Zero)
				{
					yield return null;
					if (cancellation.IsDisposed) yield break;

					MainThreadDispatcher.UnsafeSend(action);
				}
				else
				{
					var startTime = Time.fixedTime;
					var dt = (float)dueTime.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						var elapsed = Time.fixedTime - startTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							break;
						}
					}
				}
			}

			protected override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
			{
				// zero == every frame
				if (period == TimeSpan.Zero)
				{
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) yield break;

						MainThreadDispatcher.UnsafeSend(action);
					}
				}
				else
				{
					var startTime = Time.fixedTime;
					var dt = (float)period.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						var ft = Time.fixedTime;
						var elapsed = ft - startTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							startTime = ft;
						}
					}
				}
			}

			public override IDisposable Schedule<TState>(TState state,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state ,TimeSpan.Zero, action);
			}

			protected override void Iterate(IEnumerator itor)
			{
				MainThreadDispatcher.StartFixedUpdateMicroCoroutine(itor);
			}

			public override void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
			{
				Iterate(ImmediateAction(state, action, cancel));
			}
		}

		class EndOfFrameMainThreadScheduler : BaseUnityScheduler
		{
			public EndOfFrameMainThreadScheduler()
			{
				MainThreadDispatcher.Initialize();
			}

			IEnumerator ImmediateAction<T>(T state, Action<T> action, ICancelable cancellation)
			{
				yield return null;
				if (cancellation.IsDisposed) yield break;

				MainThreadDispatcher.UnsafeSend(action, state);
			}

			protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
			{
				if (dueTime == TimeSpan.Zero)
				{
					yield return null;
					if (cancellation.IsDisposed) yield break;

					MainThreadDispatcher.UnsafeSend(action);
				}
				else
				{
					var elapsed = 0f;
					var dt = (float)dueTime.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						elapsed += Time.deltaTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							break;
						}
					}
				}
			}

			protected override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
			{
				// zero == every frame
				if (period == TimeSpan.Zero)
				{
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) yield break;

						MainThreadDispatcher.UnsafeSend(action);
					}
				}
				else
				{
					var elapsed = 0f;
					var dt = (float)period.TotalSeconds;
					while (true)
					{
						yield return null;
						if (cancellation.IsDisposed) break;

						elapsed += Time.deltaTime;
						if (elapsed >= dt)
						{
							MainThreadDispatcher.UnsafeSend(action);
							elapsed = 0;
						}
					}
				}
			}

			public override IDisposable Schedule<TState>(TState state,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state,TimeSpan.Zero, action);
			}

			protected override void Iterate(IEnumerator itor)
			{
				MainThreadDispatcher.StartEndOfFrameMicroCoroutine(itor);
			}

			public override void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
			{
				Iterate(ImmediateAction(state, action, cancel));
			}
		}
	}
}