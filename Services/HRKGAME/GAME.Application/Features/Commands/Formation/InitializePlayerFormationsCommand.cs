using GAME.Application.Templates;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Commands.Formation;

public sealed record InitializePlayerFormationsCommand(string UserId) : ICommand<BaseResponse<bool>>;
