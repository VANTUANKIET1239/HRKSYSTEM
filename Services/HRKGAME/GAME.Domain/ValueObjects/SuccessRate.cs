using System;

namespace GAME.Domain.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho tỷ lệ thành công cường hóa (từ 0.0000 đến 1.0000).
    /// </summary>
    public readonly record struct SuccessRate
    {
        public decimal Value { get; }

        private SuccessRate(decimal value)
        {
            Value = Math.Clamp(value, 0m, 1.0m);
        }

        public static SuccessRate From(decimal value) => new(value);

        public SuccessRate AddBonus(decimal bonus)
        {
            if (bonus < 0)
                throw new ArgumentOutOfRangeException(nameof(bonus), "Tỷ lệ cộng thêm không thể là số âm.");

            return new SuccessRate(Value + bonus);
        }

        public SuccessRate CapAt100Percent() => new(Math.Min(1.0m, Value));

        public decimal ToPercentage() => Value * 100m;

        public override string ToString() => $"{ToPercentage():F2}%";
    }
}
