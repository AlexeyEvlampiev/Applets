using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Applets
{
    public static class Extensions
    {
        public static IObservable<Unit> ToObservable(this CancellationToken cancellation)
        {
            var subject = new AsyncSubject<Unit>();
            cancellation.Register(() =>
            {
                subject.OnNext(Unit.Default);
                subject.OnCompleted();
            });
            return subject.AsObservable();
        }
    }
}
