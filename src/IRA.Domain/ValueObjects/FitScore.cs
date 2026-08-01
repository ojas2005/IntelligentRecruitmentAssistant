using IRA.Domain.Common;

namespace IRA.Domain.ValueObjects;

/// <summary>
/// A candidate-to-job fit score constrained to the inclusive range 0..100.
/// Enforcing the range here is a Domain business rule.
/// </summary>
public readonly record struct FitScore
{
    public double Value { get; }

    public FitScore(double value)
    {
        if (double.IsNaN(value) || value < 0 || value > 100)
        {
            throw new DomainException($"Fit score must be between 0 and 100 but was {value}.");
        }

        Value = Math.Round(value, 2);
    }

    public static FitScore FromFraction(double fraction) => new(Math.Clamp(fraction, 0, 1) * 100);

    public override string ToString() => $"{Value:0.##}";

    public static implicit operator double(FitScore score) => score.Value;
}
