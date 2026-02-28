using SwiftlyS2.Shared.Commands;

namespace CS2ZombiePlague.Data.Plugins.ResetScore;

public interface IScoreResetService
{
    public void Initialize();
    public void ResetScoreHandler(ICommandContext context);
}