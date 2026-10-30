using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Web.Responses;
using Core.Web.Filters;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Core.Modules.Tests;
public sealed class NameCommandHandler : IRequestHandler<NameCommand, BaseResponse<string>>
{
    public Task<BaseResponse<string>> Handle(NameCommand request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invalid request reached the mutating handler.");
}
