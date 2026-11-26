using System.Collections.Concurrent;
using Eng.Agilium.Be.Exceptions;

namespace Eng.Agilium.Be.Services;

/// <summary>Limits failed attempts to use an invitation key (against guessing): per user and per IP, in a sliding window.</summary>
public interface IJoinAttemptLimiter
{
  /// <summary>Throws <see cref="TooManyRequestsException"/> when the caller has too many recent failures.</summary>
  void EnsureAllowed(int userId, string? ip);

  void RegisterFailure(int userId, string? ip);
}

public sealed class JoinAttemptLimiter(TimeProvider clock) : IJoinAttemptLimiter
{
  public const int MaxFailures = 10;
  public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

  private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> failures = new();

  public void EnsureAllowed(int userId, string? ip)
  {
    if (Count($"u:{userId}") >= MaxFailures || (ip != null && Count($"ip:{ip}") >= MaxFailures))
      throw new TooManyRequestsException("Too many invalid keys, try again later");
  }

  public void RegisterFailure(int userId, string? ip)
  {
    Add($"u:{userId}");
    if (ip != null)
      Add($"ip:{ip}");
  }

  private int Count(string key)
  {
    if (!failures.TryGetValue(key, out var queue))
      return 0;
    lock (queue)
    {
      Trim(queue);
      return queue.Count;
    }
  }

  private void Add(string key)
  {
    var queue = failures.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
    lock (queue)
    {
      Trim(queue);
      queue.Enqueue(clock.GetUtcNow());
    }
  }

  private void Trim(Queue<DateTimeOffset> queue)
  {
    var limit = clock.GetUtcNow() - Window;
    while (queue.Count > 0 && queue.Peek() <= limit)
      queue.Dequeue();
  }
}
