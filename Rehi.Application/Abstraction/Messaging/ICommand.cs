using MediatR;
using Rehi.Domain.Common;

namespace Rehi.Application.Abstraction.Messaging;

public interface ICommand : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> : IBaseCommand, IRequest<Result<TResponse>>;

public interface IBaseCommand;