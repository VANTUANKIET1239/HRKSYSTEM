namespace GAME.Application.Interfaces
{
    public interface IRandomService
    {
        double NextDouble();
        int Next(int minValue, int maxValue);
        decimal NextDecimal(decimal minValue, decimal maxValue, int decimals = 4);
    }
}
