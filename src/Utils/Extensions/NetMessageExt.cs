using SwiftlyS2.Shared.NetMessages;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace CS2ZombiePlague.Utils.Extensions;

public static class NetMessageExt
{
    public const uint FFadeIn = 0x0000;
    public const uint FFadeOut = 0x0001;
    public const uint FFadeModulate = 0x0002;
    public const uint FFadeStayout = 0x0004;
    
    public static void SendCUserMessageFade(this INetMessageService messageService, int playerId, uint duration,
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
    
    public static uint Rgba(byte r, byte g, byte b, byte a)
        => (uint)(r | (g << 8) | (b << 16) | (a << 24));
}