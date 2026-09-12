using GAME.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IItemStatCalculationService
    {
        /// <summary>
        /// Tính toán snapshot chỉ số hiện tại dựa trên Base Attributes của Template, Enhancement và Stars.
        /// </summary>
        Dictionary<string, decimal> CalculateCurrentStats(HrkItemTemplate template, int enhancement, int stars);

        /// <summary>
        /// Tính toán snapshot chỉ số hiện tại và trả về chuỗi JSON để lưu trữ vào HRK_PlayerInventory.CurrentStats.
        /// </summary>
        string CalculateCurrentStatsJson(HrkItemTemplate template, int enhancement, int stars);

        /// <summary>
        /// Kiểm tra tính hợp lệ của các thuộc tính đối với danh mục trang bị (đối chiếu với HRK_CategoryAllowedAttributes).
        /// Ném InvalidOperationException nếu có thuộc tính không được phép.
        /// </summary>
        Task ValidateAttributesForCategoryAsync(int categoryId, IEnumerable<int> attributeTypeIds, CancellationToken cancellationToken = default);
    }
}
