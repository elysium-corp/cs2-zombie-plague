using System.Numerics;

namespace SupplyBox.Services;

internal readonly record struct SupplyBoxFallBounds(Vector3 Mins, Vector3 Maxs)
{
    public static SupplyBoxFallBounds FromModel(Vector3 mins, Vector3 maxs, float pitch, float yaw, float roll)
    {
        if (!Finite(mins) || !Finite(maxs) || mins.X >= maxs.X || mins.Y >= maxs.Y || mins.Z >= maxs.Z)
            throw new ArgumentException("Модель ящика не содержит допустимых границ для автоматического приземления.");

        const float radians = MathF.PI / 180;
        var rotation = Matrix4x4.CreateRotationX(roll * radians)
            * Matrix4x4.CreateRotationY(pitch * radians)
            * Matrix4x4.CreateRotationZ(yaw * radians);
        var resultMin = new Vector3(float.PositiveInfinity);
        var resultMax = new Vector3(float.NegativeInfinity);
        // Охватывающий объём учитывает поворот и смещение центра модели относительно её origin.
        for (var corner = 0; corner < 8; corner++)
        {
            var point = Vector3.Transform(new Vector3(
                (corner & 1) == 0 ? mins.X : maxs.X,
                (corner & 2) == 0 ? mins.Y : maxs.Y,
                (corner & 4) == 0 ? mins.Z : maxs.Z), rotation);
            resultMin = Vector3.Min(resultMin, point);
            resultMax = Vector3.Max(resultMax, point);
        }
        return new(resultMin, resultMax);
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
