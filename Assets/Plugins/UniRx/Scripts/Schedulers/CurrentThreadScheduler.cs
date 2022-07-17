// this code is borrowed from RxOfficial(rx.codeplex.com) and modified

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Reactive.Concurrency;

using UniRx.InternalUtil;

namespace UniRx
{

/*
	public static partial class Scheduler
	{
        public static readonly IScheduler CurrentThread = new CurrentThreadScheduler();

        public static bool IsCurrentThreadSchedulerScheduleRequired { get { return CurrentThreadScheduler.IsScheduleRequired; } }

		/// <summary>
		/// Represents an object that schedules units of work on the current thread.
		/// </summary>
		/// <seealso cref="Scheduler.CurrentThread">Singleton instance of this type exposed through this static property.</seealso>
		class CurrentThreadScheduler : IScheduler
		{
			[ThreadStatic]
			static SchedulerQueue<TimeSpan> s_threadLocalQueue;

			[ThreadStatic]
			static Stopwatch s_clock;

			private static SchedulerQueue<TimeSpan> GetQueue()
			{
				return s_threadLocalQueue;
			}

			private static void SetQueue(SchedulerQueue<TimeSpan> newQueue)
			{
				s_threadLocalQueue = newQueue;
			}

			private static TimeSpan Time
			{
				get
				{
					if (s_clock == null)
						s_clock = Stopwatch.StartNew();

					return s_clock.Elapsed;
				}
			}

			/// <summary>
			/// Gets a value that indicates whether the caller must call a Schedule method.
			/// </summary>
			[EditorBrowsable(EditorBrowsableState.Advanced)]
			public static bool IsScheduleRequired
			{
				get
				{
					return GetQueue() == null;
				}
			}

			public IDisposable Schedule<TState>(TState state,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state,TimeSpan.Zero,action);
			}

			public IDisposable Schedule<TState>(TState state,TimeSpan dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				if (action == null)
					throw new ArgumentNullException("action");

				var dt = Time + Scheduler.Normalize(dueTime);

				var si = new ScheduledItem<TimeSpan,TState>(this,state,action,dt);

				var queue = GetQueue();

				if (queue == null)
				{
					queue = new SchedulerQueue<TimeSpan>(4);
					queue.Enqueue(si);

					CurrentThreadScheduler.SetQueue(queue);
					try
					{
						Trampoline.Run(queue);
					}
					finally
					{
						CurrentThreadScheduler.SetQueue(null);
					}
				}
				else
				{
					queue.Enqueue(si);
				}

				return si;
			}

			public IDisposable Schedule<TState>(TState state,DateTimeOffset dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state,dueTime - Now,action);
			}

            static class Trampoline
            {
				public static void Run(SchedulerQueue<TimeSpan> queue)
                {
                    while (queue.Count > 0)
                    {
                        var item = queue.Dequeue();
                        if (!item.IsCanceled)
                        {
							var wait = item.DueTime - CurrentThreadScheduler.Time;
							if (wait.Ticks > 0)
							{
								Thread.Sleep(wait);
							}

							if (!item.IsCanceled)
								item.Invoke();
						}
					}
				}
			}

			public DateTimeOffset Now
			{
                get { return Scheduler.Now; }
			}
		}
	}
*/
}

