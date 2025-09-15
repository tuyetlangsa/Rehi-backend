using MediatR;
using Rehi.Domain.Common;

namespace Rehi.Application.Abstraction.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;