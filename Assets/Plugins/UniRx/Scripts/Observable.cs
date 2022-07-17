using System;

using UniRx.Operators;

namespace System.Reactive.Linq
{
    // Standard Query Operators

    // onNext implementation guide. enclose otherFunc but onNext is not catch.
    // try{ otherFunc(); } catch { onError() }
    // onNext();

    public static partial class Observable
    {
        /// <summary>
        /// Lightweight SelectMany for Single Async Operation.
        /// </summary>
        public static IObservable<TR> ContinueWith<T, TR>(this IObservable<T> source, IObservable<TR> other)
        {
            return ContinueWith(source, _ => other);
        }

        /// <summary>
        /// Lightweight SelectMany for Single Async Operation.
        /// </summary>
        public static IObservable<TR> ContinueWith<T, TR>(this IObservable<T> source, Func<T, IObservable<TR>> selector)
        {
            return new ContinueWithObservable<T, TR>(source, selector);
        }

        public static IObservable<T> DoOnError<T>(this IObservable<T> source, Action<Exception> onError)
        {
            return new DoOnErrorObservable<T>(source, onError);
        }

        public static IObservable<T> DoOnCompleted<T>(this IObservable<T> source, Action onCompleted)
        {
            return new DoOnCompletedObservable<T>(source, onCompleted);
        }

        public static IObservable<T> DoOnTerminate<T>(this IObservable<T> source, Action onTerminate)
        {
            return new DoOnTerminateObservable<T>(source, onTerminate);
        }

        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action onSubscribe)
        {
            return new DoOnSubscribeObservable<T>(source, onSubscribe);
        }

        public static IObservable<T> DoOnCancel<T>(this IObservable<T> source, Action onCancel)
        {
            return new DoOnCancelObservable<T>(source, onCancel);
        }
    }
}