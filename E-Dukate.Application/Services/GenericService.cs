// using E_Dukate.Domain.Interfaces;
// using E_Dukate.Domain.Primitives;

// namespace E_Dukate.Application.Services;

// public class GenericService<T> where T : Entity
// {
//     private readonly IGenericRepository<T> _repository;

//     public GenericService(IGenericRepository<T> repository)
//     {
//         _repository = repository;
//     }

//     public void Update(T entity)
//     {
//         var existing = _repository.GetById(entity.Id);
//         if (existing == null) throw new Exception($"{typeof(T).Name} not found.");
//         _repository.Update(entity);
//     }

//     public void Delete(Guid id)
//     {
//         var entity = _repository.GetById(id);
//         if (entity == null) throw new Exception($"{typeof(T).Name} not found.");
//         _repository.Delete(id);
//     }

//     public T? FindById(Guid id) => _repository.GetById(id);
//     public IEnumerable<T> ListAll() => _repository.GetAll();
// }