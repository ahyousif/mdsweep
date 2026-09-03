namespace Mdsweep.Application.Common.Abstractions;

public interface IRequest<T>;

public interface ICommand<T> : IRequest<T>;

public interface IQuery<T> : IRequest<T>;
