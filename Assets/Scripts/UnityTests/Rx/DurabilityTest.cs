using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

namespace UniRx.Operators
{

	public class DurabilityTest
	{
		public delegate void LikeUnityAction();
		public delegate void LikeUnityAction<T0>(T0 arg0);

		public class MyEventArgs : EventArgs
		{
			public int MyProperty { get; set; }

			public MyEventArgs(int x)
			{
				this.MyProperty = x;
			}
		}

		public class EventTester
		{
			public EventHandler<MyEventArgs> genericEventHandler;
			public LikeUnityAction unityAction;
			public LikeUnityAction<int> intUnityAction;
			public Action<int> intAction;
			public Action unitAction;

			public void Fire(int x)
			{
				if (genericEventHandler != null)
				{
					genericEventHandler.Invoke(this, new MyEventArgs(x));
				}
				else if (unityAction != null)
				{
					unityAction();
				}
				else if (intUnityAction != null)
				{
					intUnityAction(x);
				}
				else if (intAction != null)
				{
					intAction.Invoke(x);
				}
				else if (unitAction != null)
				{
					unitAction.Invoke();
				}
			}
		}

		[Test,Combinatorial]
		public void FromTest([Values(false,true)]bool useLocal,[Values("EventPattern","EventUnityLike","EventUnity")]string type)
		{
			var tester = new EventTester();
			var list = new List<int>();

			if(useLocal)
			{
				var i = 0;
				var d = (type switch {
					"EventPattern" => Observable.FromEventPattern<EventHandler<MyEventArgs>, MyEventArgs>((h) => h.Invoke, h => tester.genericEventHandler += h, h => tester.genericEventHandler -= h).Select((v) => Unit.Default),
					"EventUnityLike" => Observable.FromEvent<LikeUnityAction,Unit>((h) => new LikeUnityAction(() => h(Unit.Default)), h => tester.unityAction += h, h => tester.unityAction -= h),
					_ => Observable.FromEvent(h => tester.unitAction += h, h => tester.unitAction -= h)
				}).Subscribe(xx => {
					list.Add(i);
					Observable.Return(i)
						.Do(x => { if (x == 1) throw new Exception(); })
						.CatchIgnore()
						.Subscribe(x => list.Add(i));
				});

				try { i = 5; tester.Fire(5); } catch { }
				try { i = 1; tester.Fire(1); } catch { }
				try { i = 10; tester.Fire(10); } catch { }

				list.Is(5, 5, 1, 10, 10);
				d.Dispose();
			}
			else
			{
				var d = (type switch {
					"EventPattern" => Observable.FromEventPattern<EventHandler<MyEventArgs>, MyEventArgs>((h) => h.Invoke, h => tester.genericEventHandler += h, h => tester.genericEventHandler -= h).Select((v) => v.EventArgs.MyProperty),
					"EventUnityLike" => Observable.FromEvent<LikeUnityAction<int>, int>(h => new LikeUnityAction<int>(h), h => tester.intUnityAction += h, h => tester.intUnityAction -= h),
					_ => Observable.FromEvent<int>(h => tester.intAction += h, h => tester.intAction -= h)
				}).Subscribe(xx => {
					list.Add(xx);
					Observable.Return(xx)
						.Do(x => { if (x == 1) throw new Exception(); })
						.CatchIgnore()
						.Subscribe(x => list.Add(x));
				});

				try { tester.Fire(5); } catch { }
				try { tester.Fire(1); } catch { }
				try { tester.Fire(10); } catch { }

				list.Is(5, 5, 1, 10, 10);
				d.Dispose();
			}

			tester.Fire(1000);
			list.Count.Is(5);
		}

		[Test]
		public void Durability([NUnit.Framework.Range(0,3)]int condition)
		{
			var s1 = new Subject<int>();

			var list = new List<int>();
			var d = (condition switch {
				0 => s1,
				1 => s1.Take(1000),
				2 => s1.Select(x => x),
				_ => s1.Select(x => x).Take(1000),
			}).Subscribe(xx => {
				list.Add(xx);
				Observable.Return(xx)
					.Do(x => { if (x == 1) throw new Exception(); })
					.CatchIgnore()
					.Subscribe(x => list.Add(x));
			});

			try { s1.OnNext(5); } catch { }
			try { s1.OnNext(1); } catch { }
			try { s1.OnNext(10); } catch { }

			Assert.That(list,Is.EqualTo(new[] { 5, 5, 1, 10, 10 }).AsCollection);

			d.Dispose();
			s1.OnNext(1000);
			Assert.That(list,Has.Count.EqualTo(5));
		}
	}
}
