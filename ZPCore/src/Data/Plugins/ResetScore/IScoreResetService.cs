using SwiftlyS2.Shared.Commands;

namespace ZPCore.Data.Plugins.ResetScore;

internal interface IScoreResetService
{
    public void Initialize();
    public void ResetScoreHandler(ICommandContext context);
}