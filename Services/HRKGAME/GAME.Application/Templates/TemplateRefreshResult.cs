using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Templates;

public sealed record TemplateRefreshResult(string Revision, DateTimeOffset LoadedAt, int TableCount, int RowCount);
