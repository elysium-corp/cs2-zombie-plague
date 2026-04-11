using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils;

internal static class ParticleExt
{
    extension(CParticleSystem particle)
    {
        public void SetParent(CEntityInstance? activator = null)
        {
            particle.AcceptInput<string>("SetParent", "!activator", activator);
        }

        public void SetParentAttachment(CEntityInstance? activator = null, Attachment attachment = Attachment.MuzzleFlash)
        {
            var attach = attachment switch
            {
                Attachment.MuzzleFlash => "muzzle_flash",
                _ => throw new ArgumentOutOfRangeException(nameof(attachment), attachment, null)
            };
            particle.AcceptInput("SetParentAttachment", attach, activator);
        }
    }
}