using System.Collections.Generic;

namespace Yautbox.Postgres.IntegrationTests.DbHelper.Shared.Extensions;

public static class TrackerExtensions
{
    public static void Track<T>(this ITracker<T> tracker, IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            tracker.Track(entity);
    }

    public static T Track<T>(this T entity, ITracker<T> tracker) => tracker.Track(entity);
}
