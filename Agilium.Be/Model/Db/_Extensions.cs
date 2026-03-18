using Eng.Agilium.Be.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Model.Db;

public static class Extensions
{
  public static async Task EnsureExistsAsync<T>(
    this DbSet<T> dbSet,
    int id,
    CancellationToken cancellationToken = default
  )
    where T : class
  {
    var _ = await dbSet.FindAsync([id], cancellationToken) ?? throw new EntityNotFoundException(typeof(T), id);
  }
}
