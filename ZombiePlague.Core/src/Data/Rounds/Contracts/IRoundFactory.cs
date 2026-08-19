using System.Diagnostics.CodeAnalysis;
using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal interface IRoundFactory
{
    /// <exception cref="NotSupportedException">
    /// Выбрасывается, если передан неподдерживаемый тип раунда.
    /// </exception>
    public RoundBase Create<TRound>() where TRound : RoundBase;

    /// <exception cref="NotSupportedException">
    /// Выбрасывается, если передан неподдерживаемый конфиг раунда.
    /// </exception>
    public RoundBase Create(IRoundConfig roundConfig);

    public bool TryCreate(string roundId, [NotNullWhen(true)] out RoundBase? round);
}