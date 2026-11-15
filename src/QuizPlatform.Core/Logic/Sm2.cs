using System.Collections.Immutable;
using QuizPlatform.Core.Domain;

namespace QuizPlatform.Core.Logic;

/// <summary>SuperMemo-2 spaced repetition. quality: 0 (blackout) .. 5 (perfect).</summary>
public static class Sm2
{
    public static CardProgress Initial(string player, string cardId, DateOnly today) =>
        new(player, cardId, 2.5, 0, 0, today);

    public static CardProgress Review(CardProgress p, int quality, DateOnly today)
    {
        var q = Math.Clamp(quality, 0, 5);
        if (q < 3)
            return p with { Repetitions = 0, IntervalDays = 1, Due = today.AddDays(1) };

        var ease = Math.Max(1.3, p.Ease + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02)));
        var reps = p.Repetitions + 1;
        var interval = reps switch
        {
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(p.IntervalDays * ease, MidpointRounding.AwayFromZero)
        };
        return p with { Ease = ease, Repetitions = reps, IntervalDays = interval, Due = today.AddDays(interval) };
    }

    public static ImmutableArray<Flashcard> Due(
        IEnumerable<Flashcard> cards, IEnumerable<CardProgress> progress, string player, DateOnly today)
    {
        var byCard = progress.Where(p => p.Player == player).ToDictionary(p => p.CardId);
        return cards.Where(c => !byCard.TryGetValue(c.Id, out var p) || p.Due <= today).ToImmutableArray();
    }
}
