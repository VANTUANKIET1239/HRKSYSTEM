using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.HeroStarUpgrade;
public abstract partial class HeroStarUpgradeHandlerSupport
{
    protected sealed record UpgradeData(HrkPlayer Player, HrkPlayerHero Hero, HrkHeroRarityUpgradeConfig RarityConfig, HrkHeroStarUpgradeConfig Config, HrkHeroStoneConfig StoneConfig, HrkHeroTemplate Template);
}
