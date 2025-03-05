namespace HandballBackend.Database;

public static class IncludeExtensions {
    public static IQueryable<T> IncludeRelevant<T>(this IQueryable<T> thiz) where T : IHasRelevant<T> {
        return T.GetRelevant(thiz);
    }
}