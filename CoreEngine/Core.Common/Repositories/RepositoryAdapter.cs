using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Common.Repositories
{
    public class RepositoryAdapter<T> : IRepository<T> where T : class
    {
        private readonly IRepository<T> _repo;

        public RepositoryAdapter(IUnitOfWork uow)
        {
            _repo = uow.Repository<T>();
        }

        public virtual Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector) => _repo.GetByIdAsync(id, keySelector);

        public virtual Task<IEnumerable<T>> GetAllAsync() => _repo.GetAllAsync();

        public virtual IQueryable<T> Query() => _repo.Query();

        public virtual Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => _repo.ExistsAsync(predicate);

        public virtual Task AddAsync(T entity) => _repo.AddAsync(entity);

        public virtual void Update(T entity) => _repo.Update(entity);

        public virtual void Remove(T entity) => _repo.Remove(entity);

        public virtual IQueryable<T> Table() => _repo.Table();

        public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => _repo.FirstOrDefaultAsync(predicate, cancellationToken);
    }

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
