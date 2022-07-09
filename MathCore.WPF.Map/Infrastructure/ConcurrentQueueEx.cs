#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Collections.Concurrent;

internal static class ConcurrentQueueEx
{
    public static void Clear<T>(this IProducerConsumerCollection<T> collection)
    {
        do
        {
            // empty
        }
        while (collection.TryTake(out _));
    }
}
#endif
