using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Persistence.Repositories
{

    public class ReadOnlyRepositoryAdapter<T> : IReadOnlyRepository<T> where T : class
    {
        private readonly IReadOnlyRepository<T> _repo;

        public ReadOnlyRepositoryAdapter(IUnitOfWork uow)
        {
            _repo = uow.ReadOnlyRepository<T>();
        }

        public virtual Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector) => _repo.GetByIdAsync(id, keySelector);

        public virtual Task<IEnumerable<T>> GetAllAsync() => _repo.GetAllAsync();

        public virtual IQueryable<T> Query() => _repo.Query();

        public virtual Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => _repo.ExistsAsync(predicate);

        public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => _repo.FirstOrDefaultAsync(predicate, cancellationToken);
    }
}
