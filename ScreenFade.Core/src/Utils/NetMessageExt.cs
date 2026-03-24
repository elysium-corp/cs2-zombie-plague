using SwiftlyS2.Shared.NetMessages;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace ScreenFade.Utils;

internal static class NetMessageExt
{
    internal const uint FFadeIn = 0x0000;
    internal const uint FFadeOut = 0x0001;
    internal const uint FFadeModulate = 0x0002;
    internal const uint FFadeStayout = 0x0004;
    
    internal static void SendCUserMessageFade(this INetMessageService messageService, int playerId, uint duration,
        uint holdTime, uint flags, uint color)
    {
        messageService.Send<CUserMessageFade>(msg =>
        {
            msg.Duration = duration;
            msg.HoldTime = holdTime;
            msg.Flags = flags;
            msg.Color = color;
            msg.SendToPlayer(playerId);
        });
    }
    
    internal static uint Rgba(byte r, byte g, byte b, byte a)
        => (uint)(r | (g << 8) | (b << 16) | (a << 24));
}