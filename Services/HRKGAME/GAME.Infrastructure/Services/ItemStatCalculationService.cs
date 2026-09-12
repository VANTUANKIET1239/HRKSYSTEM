using Core.Common.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class ItemStatCalculationService : IItemStatCalculationService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public ItemStatCalculationService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tính toán chỉ số hiện tại từ thuộc tính gốc, cấp cường hóa và số sao.
        /// </summary>
        public Dictionary<string, decimal> CalculateCurrentStats(HrkItemTemplate template, int enhancement, int stars)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (template == null || template.Attributes == null || template.Attributes.Count == 0)
            {
                return result;
            }

            int safeEnhancement = Math.Max(0, enhancement);
            int safeStars = Math.Max(0, stars);

            foreach (var attr in template.Attributes)
            {
                var code = attr.AttributeType?.Code ?? $"ATTR_{attr.AttributeTypeId}";
                bool isPercentage = attr.AttributeType?.IsPercentage ?? false;
                decimal baseVal = attr.Value;

                decimal calculatedVal;
                if (isPercentage)
                {
                    // Thuộc tính tỷ lệ %: tăng trưởng nhẹ 2% mỗi cấp cường hóa để cân bằng game
                    decimal multiplier = 1.0m + (safeEnhancement * 0.02m);
                    calculatedVal = Math.Round(baseVal * multiplier, 4);
                }
                else
                {
                    // Thuộc tính số cố định (HP, ATK, ARMOR, SPEED...): +8% mỗi cấp cường hóa, +10% mỗi sao
                    decimal levelMultiplier = 1.0m + (safeEnhancement * 0.08m);
                    decimal starMultiplier = 1.0m + (safeStars * 0.10m);
                    calculatedVal = Math.Round(baseVal * levelMultiplier * starMultiplier, 2);
                }

                result[code] = calculatedVal;
            }

            return result;
        }

        /// <summary>
        /// Tạo chuỗi JSON snapshot chuẩn cho CurrentStats.
        /// </summary>
        public string CalculateCurrentStatsJson(HrkItemTemplate template, int enhancement, int stars)
        {
            var stats = CalculateCurrentStats(template, enhancement, stars);
            return JsonSerializer.Serialize(stats);
        }

        /// <summary>
        /// Xác thực danh sách thuộc tính có hợp lệ với danh mục/slot trang bị hay không.
        /// </summary>
        public async Task ValidateAttributesForCategoryAsync(int categoryId, IEnumerable<int> attributeTypeIds, CancellationToken cancellationToken = default)
        {
            if (attributeTypeIds == null || !attributeTypeIds.Any())
            {
                return;
            }

            var allowedAttributeIds = await _unitOfWork.ReadOnlyRepository<HrkCategoryAllowedAttribute>().Query()
                .Where(c => c.CategoryId == categoryId)
                .Select(c => c.AttributeTypeId)
                .ToListAsync(cancellationToken);

            var invalidIds = attributeTypeIds.Where(id => !allowedAttributeIds.Contains(id)).ToList();
            if (invalidIds.Any())
            {
                throw new InvalidOperationException($"Danh mục trang bị (ID: {categoryId}) không cho phép các thuộc tính có ID: {string.Join(", ", invalidIds)}.");
            }
        }
    }
}
