using Admin.Core.Di;
using Common.Di;
using SwiftlyS2.Shared;

namespace Admin.Core;

[PluginMetadata(
    Id = "Admin.Core", 
    Version = "0.1.0", 
    Name = "Admin Core", 
    Author = "illusion & fdrinv",
    Description = "Added privileges"
)]
internal sealed partial class Admin(ISwiftlyCore core) : Plugin<AdminModule>(core)
{
    
}