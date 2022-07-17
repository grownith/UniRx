using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace UniRx.Operators
{
    // implements note : all field must be readonly.
    public abstract class OperatorObservableBase<T> : IOptimizedObservable<T>
    {
        readonly bool isRequiredSubscribeOnCurrentThread;

        public OperatorObservableBase(bool isRequiredSubscribeOnCurrentThread)
        {
            this.isRequiredSubscribeOnCurrentThread = isRequiredSubscribeOnCurrentThread;
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return isRequiredSubscribeOnCurrentThread;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var subscription = new SingleAssignmentDisposable();

            // note:
            // does not make the safe observer, it breaks exception durability.
            // var safeObserver = Observer.CreateAutoDetachObserver<T>(observer, subscription);

            if (isRequiredSubscribeOnCurrentThread && CurrentThreadScheduler.IsScheduleRequired)
            {
                CurrentThreadScheduler.Instance.Schedule(Unit.Default,(scheduler,state) => subscription.Disposable = SubscribeCore(observer, subscription));
            }
            else
            {
                subscription.Disposable = SubscribeCore(observer, subscription);
            }

            return subscription;
        }

        protected abstract IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel);
    }
}