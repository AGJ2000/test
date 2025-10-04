namespace SimpleDB;

public interface IDatabaseRepository<T>
{
    // Read all items from storage
    IEnumerable<T> Read();

    // Append a single item to storage
    void Store(T item);
}