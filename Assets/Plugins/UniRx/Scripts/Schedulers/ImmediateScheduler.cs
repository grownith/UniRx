﻿using System;
using System.Threading;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace UniRx
{
/*
	public static partial class Scheduler
	{
		public static readonly IScheduler Immediate = new ImmediateScheduler();

		class ImmediateScheduler : IScheduler
		{
			public ImmediateScheduler()
			{
			}

			public DateTimeOffset Now
			{
				get { return Scheduler.Now; }
			}

			public IDisposable Schedule<TState>(TState state,Func<IScheduler,TState,IDisposable> action)
			{
				return action(this,state);
			}

			public IDisposable Schedule<TState>(TState state,TimeSpan dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				var wait = Scheduler.Normalize(dueTime);
				if (wait.Ticks > 0)
				{
					Thread.Sleep(wait);
				}

				return action(this,state);
			}

			public IDisposable Schedule<TState>(TState state,DateTimeOffset dueTime,Func<IScheduler,TState,IDisposable> action)
			{
				return Schedule(state,dueTime - Now,action);
			}
		}
	}
*/
}