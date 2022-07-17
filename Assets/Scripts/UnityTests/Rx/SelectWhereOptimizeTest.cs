using System;
using System.Linq;
using System.Reactive.Linq;

using NUnit.Framework;

namespace UniRx.Tests
{

	public class SelectWhereOptimizeTest
    {
        [Test]
        public void SelectSelect()
        {
            // Combine selector currently disabled.
            //var selectselect = Observable.Range(1, 10)
            //    .Select(x => x)
            //    .Select(x => x * -1);
        }

        [Test]
        public void WhereWhere()
        {
            var wherewhere = Observable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Where(x => x > 5);

            wherewhere.ToArrayWait().Is(6, 8, 10);

            var wherewhere2 = Observable.Range(1, 10)
                .Where((x, i) => x % 2 == 0)
                .Where(x => x > 5);

            wherewhere2.ToArrayWait().Is(6, 8, 10);
        }

        [Test]
        public void SelectWhere()
        {
            var selectWhere = Observable.Range(1, 10)
                .Select(x => x * x)
                .Where(x => x % 2 == 0);

            Assert.That(selectWhere.GetType().Name,Does.Contain("Predicate"));
            selectWhere.ToArrayWait().Is(4, 16, 36, 64, 100);

            var selectWhere2 = Observable.Range(1, 10)
                .Select((x, i) => x * x)
                .Where(x => x % 2 == 0);

            selectWhere2.ToArrayWait().Is(4, 16, 36, 64, 100);
        }

        [Test]
        public void WhereSelect()
        {
            var whereSelect = Observable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => x * x);

            Assert.That(whereSelect.GetType().Name,Does.Contain("Selector"));
            whereSelect.ToArrayWait().Is(4, 16, 36, 64, 100);

            var whereSelect2 = Observable.Range(1, 10)
                .Where((x, i) => x % 2 == 0)
                .Select(x => x * x);

            Assert.That(whereSelect.GetType().Name,Does.Contain("Selector"));
            whereSelect2.ToArrayWait().Is(4, 16, 36, 64, 100);
        }
    }
}
