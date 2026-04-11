using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Controllers;

internal record class ParticleContext(CParticleSystem Particle, CancellationTokenSource Token);