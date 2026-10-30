using System.Security.Claims;
using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Application.Validation;
using Core.Common.Constants.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Web.CQRS;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Cqrs.Tests;
public sealed partial class CoreCqrsDispatchTests
{
    public sealed class ChangeValueValidator : HRKValidator<ChangeValueCommand>
    {
        public ChangeValueValidator() => RuleFor(command => command.Amount).NotEqual(0);
    }
}
