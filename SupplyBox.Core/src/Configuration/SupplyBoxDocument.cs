using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using SupplyBox.Data.Configs;

namespace SupplyBox.Configuration;

internal sealed class SupplyBoxDocument
{
    internal const int MaximumConfigBytes = 8_388_608;
    public int SchemaVersion { get; set; } = 1;
    public SupplyBoxConfig Settings { get; set; } = new();
    public List<SupplyBoxType> BoxTypes { get; set; } = [new()];
    public List<SupplyBoxMap> Maps { get; set; } = [];

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public SupplyBoxDocument Clone() => Parse(JsonSerializer.Serialize(this, Json));

    public static SupplyBoxDocument Parse(string json)
    {
        var document = JsonSerializer.Deserialize<SupplyBoxDocument>(json, Json)
            ?? throw new InvalidDataException("SupplyBox configuration is empty.");
        document.Validate();
        return document;
    }

    public void Validate()
    {
        if (SchemaVersion != 1 || Settings is null || Maps is null || BoxTypes is null)
            throw new InvalidDataException("Unsupported or incomplete SupplyBox configuration.");
        ValidateObject(Settings);
        if (BoxTypes.Count is < 1 or > 64 || Maps.Count > 512)
            throw new InvalidDataException("SupplyBox supports 1–64 box types and up to 512 maps.");
        Unique(BoxTypes.Select(box => box.Key), "box key");
        Unique(Maps.Select(map => map.Name), "map name");
        foreach (var box in BoxTypes)
        {
            ValidateObject(box);
            if (!Key(box.Key) || box.Loot is null || box.Loot.Count is < 1 or > 128)
                throw new InvalidDataException("Invalid box key or loot count.");
            Model(box.Model); Model(box.ParachuteModel, true);
            foreach (var loot in box.Loot)
            {
                ValidateObject(loot);
                if (loot.MaxAmount < loot.MinAmount || !RewardKinds.Contains(loot.Kind)
                    || (loot.Kind == "equipment" && !Key(loot.ItemKey))
                    || (loot.Kind == "weapon" && !StandardWeapons.Contains(loot.ItemKey))
                    || (loot.Kind is "weapon" or "equipment" && (loot.MinAmount != 1 || loot.MaxAmount != 1)))
                    throw new InvalidDataException("Invalid SupplyBox reward.");
            }
        }
        foreach (var map in Maps)
        {
            ValidateObject(map);
            if (!Regex.IsMatch(map.Name, @"^[A-Za-z0-9_/-]{1,128}$") || map.Name.Contains("..")
                || map.Points is null || map.Points.Count > 512)
                throw new InvalidDataException("Invalid map or point count.");
            Unique(map.Points.Select(point => point.Id.ToString()), "point ID");
            foreach (var point in map.Points)
            {
                ValidateObject(point);
                if (point.BoxType != "" && !BoxTypes.Any(box => box.Key == point.BoxType))
                    throw new InvalidDataException("Spawn point references an unknown box type.");
            }
        }
        Model(Settings.SupplyBoxModel); Model(Settings.ParachuteModel, true);
        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(this, Json)) > MaximumConfigBytes)
            throw new InvalidDataException("SupplyBox configuration exceeds the 8 MiB fallback limit.");
    }

    private static void ValidateObject(object value) => Validator.ValidateObject(value, new ValidationContext(value), true);
    private static void Unique(IEnumerable<string> keys, string name)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (keys.Any(key => !seen.Add(key))) throw new InvalidDataException($"Duplicate SupplyBox {name}.");
    }
    private static bool Key(string value) => Regex.IsMatch(value, @"^[A-Za-z0-9_.-]{1,128}$");
    private static void Model(string value, bool optional = false)
    {
        if (optional && value == "") return;
        if (value.Length > 256 || value.Contains("..") || !Regex.IsMatch(value, @"^[A-Za-z0-9_/-]+\.vmdl$"))
            throw new InvalidDataException("Invalid model path.");
    }
    internal static readonly HashSet<string> RewardKinds = ["money", "health", "armor", "weapon", "equipment"];
    internal static readonly HashSet<string> StandardWeapons = new(("glock hkp2000 usp_silencer elite p250 tec9 fiveseven cz75a deagle revolver mac10 mp9 mp7 mp5sd ump45 p90 bizon galilar famas ak47 m4a1 m4a1_silencer aug sg556 ssg08 awp scar20 g3sg1 nova xm1014 mag7 sawedoff m249 negev").Split(' ').Select(name => "weapon_" + name));
}

internal sealed class SupplyBoxType
{
    [Required, StringLength(128)] public string Key { get; set; } = "standard";
    [Required, StringLength(100)] public string Name { get; set; } = "Припасы";
    public bool Enabled { get; set; } = true;
    [Range(1, 10000)] public int Weight { get; set; } = 100;
    public string Model { get; set; } = "models/props/crates/cs2_drop_crate_01.vmdl";
    public string ParachuteModel { get; set; } = "";
    [StringLength(128)] public string FallingSound { get; set; } = "";
    [Range(1, 16)] public int Rolls { get; set; } = 1;
    public bool UniqueRewards { get; set; } = true;
    public List<SupplyBoxLoot> Loot { get; set; } = [new()];
}

internal sealed class SupplyBoxLoot
{
    public bool Enabled { get; set; } = true;
    [Required, StringLength(100)] public string Name { get; set; } = "Игровые деньги";
    [Required] public string Kind { get; set; } = "money";
    [StringLength(128)] public string ItemKey { get; set; } = "";
    [Range(1, 10000)] public int Weight { get; set; } = 100;
    [Range(1, 1000000)] public int MinAmount { get; set; } = 100;
    [Range(1, 1000000)] public int MaxAmount { get; set; } = 300;
}

internal sealed class SupplyBoxMap
{
    [Required, StringLength(128)] public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    [Range(0, 100)] public int? ChanceDrop { get; set; }
    [Range(1, 32)] public int? MaxCountTogether { get; set; }
    public List<SupplyBoxPoint> Points { get; set; } = [];
}

internal sealed class SupplyBoxPoint
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "Точка";
    public bool Enabled { get; set; } = true;
    [Range(-32768d, 32768d)] public double X { get; set; }
    [Range(-32768d, 32768d)] public double Y { get; set; }
    [Range(-32768d, 32768d)] public double Z { get; set; }
    [Range(-360d, 360d)] public double Pitch { get; set; }
    [Range(-360d, 360d)] public double Yaw { get; set; }
    [Range(-360d, 360d)] public double Roll { get; set; }
    [Range(1, 10000)] public int Weight { get; set; } = 100;
    [StringLength(128)] public string BoxType { get; set; } = "";
}
