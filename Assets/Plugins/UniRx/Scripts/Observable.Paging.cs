using System;

using UniRx;
using UniRx.Operators;

namespace System.Reactive.Linq
{
    // Take, Skip, etc..
    public static partial class Observable
    {
        /// <summary>Projects old and new element of a sequence into a new form.</summary>
        public static IObservable<Pair<T>> Pairwise<T>(this IObservable<T> source)
        {
            return new PairwiseObservable<T>(source);
        }

        /// <summary>Projects old and new element of a sequence into a new form.</summary>
        public static IObservable<TR> Pairwise<T, TR>(this IObservable<T> source, Func<T, T, TR> selector)
        {
            return new PairwiseObservable<T, TR>(source, selector);
        }
    }
}