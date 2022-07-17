using System;
using System.Collections.Generic;

using System.Reactive;
using System.Reactive.Concurrency;

using UniRx.Operators;

namespace System.Reactive.Linq
{
    public static partial class Observable
    {
        /// <summary>
        /// Converting .Select(_ => Unit.Default) sequence.
        /// </summary>
        public static IObservable<Unit> AsUnitObservable<T>(this IObservable<T> source)
        {
            return new AsUnitObservableObservable<T>(source);
        }

        /// <summary>
        /// Same as LastOrDefault().AsUnitObservable().
        /// </summary>
        public static IObservable<Unit> AsSingleUnitObservable<T>(this IObservable<T> source)
        {
            return new AsSingleUnitObservableObservable<T>(source);
        }
    }
}