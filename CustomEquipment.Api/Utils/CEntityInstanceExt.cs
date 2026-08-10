using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils;

internal static class CEntityInstanceExt
{
    extension(CEntityInstance entity)
    {
        public void ChangeSubclass(string subclass)
        {
            entity.AcceptInput<string>("ChangeSubclass", subclass);
        }
    }
}