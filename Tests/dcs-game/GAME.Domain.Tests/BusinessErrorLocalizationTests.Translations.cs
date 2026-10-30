using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Errors;
using Core.InternalApi;
using Core.Localization;
using Core.Persistence.Repositories;
using Core.Web.Responses;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class BusinessErrorLocalizationTests
{
    private sealed class Translations : ITranslationCache
    {
        public Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct) => Task.FromResult<TranslationBundle?>(new(group, culture, 1, new Dictionary<string, string> { [TranslationIdentity.TextKey("Không tìm thấy ví người chơi.")] = "Player wallet not found.", [TranslationIdentity.TextKey("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")] = "A system error occurred. Please try again later." }));
        public Task PublishAsync(TranslationBundle bundle, CancellationToken ct) => throw new NotSupportedException();
    }
}
