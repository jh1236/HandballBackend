using Microsoft.EntityFrameworkCore.Query;

namespace HandballBackend.Database;

public interface IHasRelevant<T> {
    public static abstract IQueryable<T> GetRelevant(IQueryable<T> query);
}