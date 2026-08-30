using Admin.Api;
using CustomEquipment.Api;

namespace Economy.Core.Services;

internal sealed class EconomyExternalApis
{
    public IAdminApi? Admin { get; set; }

    public ICustomEquipmentApi? CustomEquipment { get; set; }
}
