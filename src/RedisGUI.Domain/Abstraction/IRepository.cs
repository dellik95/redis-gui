using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Abstraction;

public interface IRepository<T> where T : Entity
{
    void Add(T connection);

    Task<Result> DeleteAsync(Guid id, CancellationToken token = default);

    void Update(T connection);
}