using Core.Common.Common;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.FluentValidation
{
    public class HRKValidator<T> : AbstractValidator<T>
    {
        protected HRKValidator()
        {
            // Stop on first failure per rule for clearer errors
            ClassLevelCascadeMode = CascadeMode.Stop;
        }

        // --- Common property helpers -------------------------------------------
        protected IRuleBuilderOptions<T, string> NotEmpty(Expression<Func<T, string>> expr)
        {
            return RuleFor(expr)
                .NotEmpty()
                .WithMessage("{PropertyName} is empty");
        }
        protected IRuleBuilderOptions<T, string> RequiredSafeText(Expression<Func<T, string>> expr)
        {
            return RuleFor(expr)
                .Must(x =>  string.IsNullOrEmpty(x) || TextSafety.LooksSafe(x))
                .WithMessage("{PropertyName} contains unsafe or invalid characters.");
        }

        protected IRuleBuilderOptions<T, string?> Email(Expression<Func<T, string?>> expr)
        => RuleFor(expr)
        .EmailAddress().WithMessage("{PropertyName} must be a valid email.");

        // Phone (simple international format)
        protected IRuleBuilderOptions<T, string?> Phone(Expression<Func<T, string?>> expr)
            => RuleFor(expr)
               
                .Matches(@"^\+?[0-9]{7,15}$")
                .WithMessage("{PropertyName} must be a valid phone number.");



        protected IRuleBuilderOptions<T, string> RequiredTrimmed(
            Expression<Func<T, string>> expr,
            int maxLength = 200,
            string? displayName = null)
            => RuleFor(expr)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage($"{displayName ?? "{PropertyName}"} is required.")
                .Must(s => !string.IsNullOrWhiteSpace(s))
                    .WithMessage($"{displayName ?? "{PropertyName}"} cannot be whitespace.")
                .MaximumLength(maxLength);

        protected IRuleBuilderOptions<T, string?> OptionalTrimmed(
            Expression<Func<T, string?>> expr,
            int maxLength = 200)
            => RuleFor(expr)
                .MaximumLength(maxLength);

        protected IRuleBuilderOptions<T, Guid> RequiredGuid(
            Expression<Func<T, Guid>> expr,
            string? displayName = null)
            => RuleFor(expr)
                .NotEmpty()
                .WithMessage($"{displayName ?? "{PropertyName}"} must not be empty.");

        protected IRuleBuilderOptions<T, Guid?> OptionalGuid(
            Expression<Func<T, Guid?>> expr,
            string? displayName = null)
            => RuleFor(expr)
                .Must(g => g is null || g != Guid.Empty)
                .WithMessage($"{displayName ?? "{PropertyName}"} must be null or a non-empty GUID.");

        protected IRuleBuilderOptions<T, int> Positive(
            Expression<Func<T, int>> expr) => RuleFor(expr).GreaterThan(0);

        protected IRuleBuilderOptions<T, int?> PositiveIfHasValue(
            Expression<Func<T, int?>> expr) => RuleFor(expr).Must(v => v is null || v > 0);

        protected IRuleBuilderOptions<T, IEnumerable<TItem>> NonEmptyDistinct<TItem>(
            Expression<Func<T, IEnumerable<TItem>>> expr,
            string? displayName = null)
            => RuleFor(expr)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage($"{displayName ?? "{PropertyName}"} is required.")
                .Must(list => list.Any())
                    .WithMessage($"{displayName ?? "{PropertyName}"} must contain at least one item.")
                .Must(list => list.Distinct().Count() == list.Count())
                    .WithMessage($"{displayName ?? "{PropertyName}"} must not contain duplicates.");

        // --- Cross-field / object-level helpers --------------------------------

        /// <summary>
        /// Validates a (Start <= End) relationship for DateTime?/DateOnly? pairs.
        /// </summary>
        protected void AddDateRangeRule(
            Func<T, DateTime?> start,
            Func<T, DateTime?> end,
            string? memberName = "DateRange",
            string? message = "Start date must be less than or equal to end date.")
        {
            RuleFor(x => x).Custom((model, ctx) =>
            {
                var s = start(model);
                var e = end(model);
                if (s.HasValue && e.HasValue && s > e)
                    ctx.AddFailure(memberName!, message!);
            });
        }

        /// <summary>
        /// Simple pagination guard: page >= 1, size within [1, maxSize].
        /// </summary>
        protected void AddPaginationRule(
            Func<T, int> page,
            Func<T, int> pageSize,
            int maxSize = 200,
            string? memberName = "Pagination")
        {
            RuleFor(x => x).Custom((model, ctx) =>
            {
                var p = page(model);
                var s = pageSize(model);
                if (p < 1) ctx.AddFailure(memberName!, "Page must be >= 1.");
                if (s < 1 || s > maxSize)
                    ctx.AddFailure(memberName!, $"PageSize must be between 1 and {maxSize}.");
            });
        }
    }

}
