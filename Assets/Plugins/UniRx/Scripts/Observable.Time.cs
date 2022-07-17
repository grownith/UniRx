using System;
using System.Reactive.Concurrency;

using UniRx.Operators;

namespace System.Reactive.Linq
{
    // Timer, Interval, etc...
    public static partial class Observable
    {
        public static IObservable<TSource> ThrottleFirst<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            return source.ThrottleFirst(dueTime, Scheduler.Default);
        }

        public static IObservable<TSource> ThrottleFirst<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            return new ThrottleFirstObservable<TSource>(source, dueTime, scheduler);
        }
    }
}