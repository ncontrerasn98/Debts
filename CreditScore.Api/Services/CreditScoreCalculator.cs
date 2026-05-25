using CreditScore.Api.Entities;

namespace CreditScore.Api.Services;

public static class CreditScoreCalculator
{
    private const int BaseScore = 550;
    private const int MinScore = 150;
    private const int MaxScore = 950;

    public static int Calculate(CreditHistory history)
    {
        if (history.TotalDebts == 0)
            return BaseScore; // sin historial

        var score = BaseScore;

        // + por deudas settladas (max +400)
        var settledBonus = Math.Min(history.SettledDebts * 80, 400);
        score += settledBonus;

        // - por deudas activas
        score -= history.ActiveDebts * 60;

        // - por negociaciones muy bajas
        score -= history.LowNegotiations * 30;

        // + por antigüedad del historial (max +100)
        var months = (int)(DateTime.UtcNow - history.OldestDebtDate).TotalDays / 30;
        var antiquityBonus = Math.Min(months * 2, 100);
        score += antiquityBonus;

        return Math.Clamp(score, MinScore, MaxScore);
    }

    public static string GetRating(int score) => score switch
    {
        >= 800 => "Excelente",
        >= 700 => "Bueno",
        >= 550 => "Regular",
        >= 400 => "Malo",
        _      => "Muy malo"
    };
}