using System.Collections.Concurrent;

namespace Eng.Agilium.Be.Services;

/// <summary>Sliding-window counter for sensitive anonymous actions (resending e-mails, requesting a password reset).</summary>
public interface IActionLimiter
{
  /// <summary>Records one use of <paramref name="key"/> and returns false when it would exceed <paramref name="max"/> within <paramref name="window"/>.</summary>
  bool TryAcquire(string key, int max, TimeSpan window);
}

public sealed class ActionLimiter(TimeProvider clock) : IActionLimiter
{
  private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> uses = new();

  public bool TryAcquire(string key, int max, TimeSpan window)
  {
    var queue = uses.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
    lock (queue)
    {
      var now = clock.GetUtcNow();
      while (queue.Count > 0 && queue.Peek() <= now - window)
        queue.Dequeue();
      if (queue.Count >= max)
        return false;
      queue.Enqueue(now);
      return true;
    }
  }
}
