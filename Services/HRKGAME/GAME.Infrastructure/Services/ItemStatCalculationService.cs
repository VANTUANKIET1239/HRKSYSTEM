using Core.Common.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
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
        private readonly IUnitOfWork? _unitOfWork;

        public ItemStatCalculationService(IUnitOfWork? unitOfWork = null)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tính toán chỉ số hiện tại từ thuộc tính gốc của template, cấp cường hóa và số sao.
        /// Sử dụng công thức tuyến tính chuẩn: Base * (1 + Enhancement * 10%) * StarMultiplier.
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

                // Công thức tuyến tính: Base * (1 + Level * GrowthPercent / 100) * StarMultiplier
                decimal growthMultiplier = 1.0m + (safeEnhancement * 0.10m);
                decimal starMultiplier = isPercentage ? 1.0m : (1.0m + (safeStars * 0.10m));

                decimal calculatedVal = isPercentage
                    ? Math.Round(baseVal * growthMultiplier, 4)
                    : Math.Round(baseVal * growthMultiplier * starMultiplier, 2);

                result[code] = calculatedVal;
            }

            return result;
        }

        /// <summary>
        /// Tính toán chỉ số hiện tại từ thuộc tính instance (HRK_PlayerInventoryAttributes) và tỷ lệ tăng trưởng cường hóa của instance.
        /// Công thức: CurrentValue = BaseRolledValue * (1 + EnhancementLevel * EnhancementGrowthPercent / 100) * StarMultiplier
        /// </summary>
        public Dictionary<string, decimal> CalculateCurrentStats(HrkPlayerInventory item, int? overrideEnhancement = null)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (item == null) return result;

            int level = Math.Max(0, overrideEnhancement ?? item.Enhancement);
            int safeStars = Math.Max(0, item.Stars);
            decimal growthPercent = item.EnhancementGrowthPercent ?? 10.0m;

            if (item.Attributes != null && item.Attributes.Count > 0)
            {
                foreach (var attr in item.Attributes)
                {
                    var code = attr.AttributeType?.Code ?? $"ATTR_{attr.AttributeTypeId}";
                    bool isPercentage = attr.AttributeType?.IsPercentage ?? false;
                    decimal baseVal = attr.BaseRolledValue;

                    decimal levelMultiplier = 1.0m + (level * growthPercent / 100.0m);
                    decimal starMultiplier = isPercentage ? 1.0m : (1.0m + (safeStars * 0.10m));

                    decimal calculatedVal = isPercentage
                        ? Math.Round(baseVal * levelMultiplier, 4)
                        : Math.Round(baseVal * levelMultiplier * starMultiplier, 2);

                    result[code] = calculatedVal;
                }
                return result;
            }

            // Fallback sang template attributes nếu instance chưa có attributes riêng
            if (item.ItemTemplate != null)
            {
                return CalculateCurrentStats(item.ItemTemplate, level, safeStars);
            }

            return result;
        }

        /// <summary>
        /// Tạo chuỗi JSON snapshot chuẩn cho CurrentStats từ template.
        /// </summary>
        public string CalculateCurrentStatsJson(HrkItemTemplate template, int enhancement, int stars)
        {
            var stats = CalculateCurrentStats(template, enhancement, stars);
            return JsonSerializer.Serialize(stats);
        }

        /// <summary>
        /// Tạo chuỗi JSON snapshot chuẩn cho CurrentStats từ instance trang bị.
        /// </summary>
        public string CalculateCurrentStatsJson(HrkPlayerInventory item, int? overrideEnhancement = null)
        {
            var stats = CalculateCurrentStats(item, overrideEnhancement);
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
