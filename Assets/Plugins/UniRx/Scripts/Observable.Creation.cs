using System;
using System.Collections.Generic;
using UniRx.Operators;

namespace System.Reactive.Linq
{
    public static partial class Observable
    {
        /// <summary>
        /// Create anonymous observable. Observer has exception durability. This is recommended for make operator and event like generator.
        /// </summary>
        public static IObservable<T> CreateWithState<T, TState>(TState state, Func<TState, IObserver<T>, IDisposable> subscribe)
        {
            if (subscribe == null) throw new ArgumentNullException("subscribe");

            return new CreateObservable<T, TState>(state, subscribe);
        }

        /// <summary>
        /// Create anonymous observable. Observer has exception durability. This is recommended for make operator and event like generator(HotObservable).
        /// </summary>
        public static IObservable<T> CreateWithState<T, TState>(TState state, Func<TState, IObserver<T>, IDisposable> subscribe, bool isRequiredSubscribeOnCurrentThread)
        {
            if (subscribe == null) throw new ArgumentNullException("subscribe");

            return new CreateObservable<T, TState>(state, subscribe, isRequiredSubscribeOnCurrentThread);
        }

        /// <summary>
        /// Create anonymous observable. Safe means auto detach when error raised in onNext pipeline. This is recommended for make generator (ColdObservable).
        /// </summary>
        public static IObservable<T> CreateSafe<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            if (subscribe == null) throw new ArgumentNullException("subscribe");

            return new CreateSafeObservable<T>(subscribe);
        }

        /// <summary>
        /// Create anonymous observable. Safe means auto detach when error raised in onNext pipeline. This is recommended for make generator (ColdObservable).
        /// </summary>
        public static IObservable<T> CreateSafe<T>(Func<IObserver<T>, IDisposable> subscribe, bool isRequiredSubscribeOnCurrentThread)
        {
            if (subscribe == null) throw new ArgumentNullException("subscribe");

            return new CreateSafeObservable<T>(subscribe, isRequiredSubscribeOnCurrentThread);
        }

        static IEnumerable<IObservable<T>> RepeatInfinite<T>(IObservable<T> source)
        {
            while (true)
            {
                yield return source;
            }
        }

        /// <summary>
        /// Same as Repeat() but if arriving contiguous "OnComplete" Repeat stops.
        /// </summary>
        public static IObservable<T> RepeatSafe<T>(this IObservable<T> source, bool isRequiredSubscribeOnCurrentThread)
        {
            return new RepeatSafeObservable<T>(RepeatInfinite(source), isRequiredSubscribeOnCurrentThread);
        }
    }
}