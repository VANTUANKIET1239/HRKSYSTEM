using System.Security.Cryptography;
using GAME.Application.Common;
using Core.Application.CQRS;
using Core.Messaging.Contracts;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Inbox;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.Player;

public sealed record InitializePlayerCommand(AccountRegisteredV1 Account) : ICommand<bool>;
