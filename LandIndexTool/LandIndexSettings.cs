using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace LandIndexTool
{
    internal sealed class AreaOverride
    {
        public string Number { get; set; }
        public double? BikeCalcArea { get; set; }
        public double? MotorCalcArea { get; set; }
    }

    internal sealed class AreaOverrideSeed
    {
        public string Number { get; set; }
        public double DefaultCalcArea { get; set; }
    }

    internal sealed class DetailOverride
    {
        public string Number { get; set; }
        public string Type { get; set; }
        public bool BaseAreaManual { get; set; }
        public double BaseArea { get; set; }
        public bool CapacityAreaManual { get; set; }
        public double CapacityArea { get; set; }
        public bool LandAreaManual { get; set; }
        public double LandArea { get; set; }
        public bool GreenAreaManual { get; set; }
        public double GreenArea { get; set; }
        public bool BikeAreaManual { get; set; }
        public double BikeArea { get; set; }
        public double NonCapacityArea { get; set; }
        public double Floors { get; set; }
        public double GroundParking { get; set; }
        public double IndoorParking { get; set; }
    }

    internal sealed class LandIndexPreviewRow
    {
        public string Number { get; set; }
        public string Type { get; set; }
        public double BaseArea { get; set; }
        public double CapacityArea { get; set; }
        public double NonCapacityArea { get; set; }
        public double BuildingArea { get; set; }
        public double LandArea { get; set; }
        public double GreenArea { get; set; }
        public double BikeArea { get; set; }
        public double Floors { get; set; }
        public double GroundParking { get; set; }
        public double IndoorParking { get; set; }
        public double Far { get; set; }
    }

    internal sealed class SummaryOverride
    {
        public string Key { get; set; }
        public string Parent { get; set; }
        public string Item { get; set; }
        public string SubItem { get; set; }
        public string Unit { get; set; }
        public bool Manual { get; set; }
        public double Value { get; set; }
        public string Remark { get; set; }
        public bool Custom { get; set; }
    }

    internal sealed class LandIndexSettings
    {
        public const string DefaultParcelLayer = "1yd";
        public const string DefaultLabelLayer = "1bh";
        public const string DefaultBuildingLayer = "1jz";
        public const string DefaultBikeLayer = "1fjdc";
        public const string DefaultGreenLayer = "1ld";
        public const string DefaultFloorLayer = "1cs";
        public const string DefaultGrassPaverLayer = "1zcz";
        public const string DefaultRoofGreen1Layer = "1wm1";
        public const string DefaultRoofGreen2Layer = "1wm2";
        public const string DefaultRoofGreen3Layer = "1wm3";

        public string UnitMode { get; set; } = "M";
        public string ParcelLayer { get; set; } = DefaultParcelLayer;
        public string LabelLayer { get; set; } = DefaultLabelLayer;
        public string BuildingLayer { get; set; } = DefaultBuildingLayer;
        public string BikeLayer { get; set; } = DefaultBikeLayer;
        public string GreenLayer { get; set; } = DefaultGreenLayer;
        public string FloorLayer { get; set; } = DefaultFloorLayer;
        public string GrassPaverLayer { get; set; } = DefaultGrassPaverLayer;
        public string RoofGreen1Layer { get; set; } = DefaultRoofGreen1Layer;
        public string RoofGreen2Layer { get; set; } = DefaultRoofGreen2Layer;
        public string RoofGreen3Layer { get; set; } = DefaultRoofGreen3Layer;
        public double GreenRateMin { get; set; } = 0.10;
        public double GreenRateMax { get; set; } = 1.00;
        public double BikeSpacesPer100 { get; set; } = 1.0;
        public double BikeAreaPerSpace { get; set; } = 1.5;
        public double MotorSpacesPer100 { get; set; } = 0.3;
        public double GrassPaverFactor { get; set; } = 1.0;
        public double RoofGreen1Factor { get; set; } = 1.0;
        public double RoofGreen2Factor { get; set; } = 1.0;
        public double RoofGreen3Factor { get; set; } = 1.0;
        public bool ShowBuildingInfoLabels { get; set; }
        public double InfoLabelTextHeight { get; set; } = 3.0;
        public double SummaryTableTextHeight { get; set; } = 2.35;
        public double DetailTableTextHeight { get; set; } = 2.35;
        public bool HasSelectionScope { get; set; }
        public double ScopeMinX { get; set; }
        public double ScopeMinY { get; set; }
        public double ScopeMaxX { get; set; }
        public double ScopeMaxY { get; set; }
        public Dictionary<string, AreaOverride> AreaOverrides { get; } =
            new Dictionary<string, AreaOverride>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DetailOverride> DetailOverrides { get; } =
            new Dictionary<string, DetailOverride>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SummaryOverride> SummaryOverrides { get; } =
            new Dictionary<string, SummaryOverride>(StringComparer.OrdinalIgnoreCase);

        public double AreaFactor => string.Equals(UnitMode, "M", StringComparison.OrdinalIgnoreCase)
            ? 1.0
            : 0.000001;

        public LandIndexSettings Clone()
        {
            var clone = new LandIndexSettings
            {
                UnitMode = UnitMode,
                ParcelLayer = ParcelLayer,
                LabelLayer = LabelLayer,
                BuildingLayer = BuildingLayer,
                BikeLayer = BikeLayer,
                GreenLayer = GreenLayer,
                FloorLayer = FloorLayer,
                GrassPaverLayer = GrassPaverLayer,
                RoofGreen1Layer = RoofGreen1Layer,
                RoofGreen2Layer = RoofGreen2Layer,
                RoofGreen3Layer = RoofGreen3Layer,
                GreenRateMin = GreenRateMin,
                GreenRateMax = GreenRateMax,
                BikeSpacesPer100 = BikeSpacesPer100,
                BikeAreaPerSpace = BikeAreaPerSpace,
                MotorSpacesPer100 = MotorSpacesPer100,
                GrassPaverFactor = GrassPaverFactor,
                RoofGreen1Factor = RoofGreen1Factor,
                RoofGreen2Factor = RoofGreen2Factor,
                RoofGreen3Factor = RoofGreen3Factor,
                ShowBuildingInfoLabels = ShowBuildingInfoLabels,
                InfoLabelTextHeight = InfoLabelTextHeight,
                SummaryTableTextHeight = SummaryTableTextHeight,
                DetailTableTextHeight = DetailTableTextHeight,
                HasSelectionScope = HasSelectionScope,
                ScopeMinX = ScopeMinX,
                ScopeMinY = ScopeMinY,
                ScopeMaxX = ScopeMaxX,
                ScopeMaxY = ScopeMaxY
            };
            foreach (var pair in AreaOverrides)
            {
                clone.AreaOverrides[pair.Key] = new AreaOverride
                {
                    Number = pair.Value.Number,
                    BikeCalcArea = pair.Value.BikeCalcArea,
                    MotorCalcArea = pair.Value.MotorCalcArea
                };
            }

            foreach (var pair in DetailOverrides)
            {
                clone.DetailOverrides[pair.Key] = CloneDetailOverride(pair.Value);
            }

            foreach (var pair in SummaryOverrides)
            {
                clone.SummaryOverrides[pair.Key] = new SummaryOverride
                {
                    Key = pair.Value.Key,
                    Parent = pair.Value.Parent,
                    Item = pair.Value.Item,
                    SubItem = pair.Value.SubItem,
                    Unit = pair.Value.Unit,
                    Manual = pair.Value.Manual,
                    Value = pair.Value.Value,
                    Remark = pair.Value.Remark,
                    Custom = pair.Value.Custom
                };
            }

            return clone;
        }

        public void Normalize()
        {
            UnitMode = string.Equals(UnitMode, "M", StringComparison.OrdinalIgnoreCase) ? "M" : "MM";
            ParcelLayer = CleanLayer(ParcelLayer, DefaultParcelLayer);
            LabelLayer = CleanLayer(LabelLayer, DefaultLabelLayer);
            BuildingLayer = CleanLayer(BuildingLayer, DefaultBuildingLayer);
            BikeLayer = CleanLayer(BikeLayer, DefaultBikeLayer);
            GreenLayer = CleanLayer(GreenLayer, DefaultGreenLayer);
            FloorLayer = CleanLayer(FloorLayer, DefaultFloorLayer);
            GrassPaverLayer = CleanLayer(GrassPaverLayer, DefaultGrassPaverLayer);
            RoofGreen1Layer = CleanLayer(RoofGreen1Layer, DefaultRoofGreen1Layer);
            RoofGreen2Layer = CleanLayer(RoofGreen2Layer, DefaultRoofGreen2Layer);
            RoofGreen3Layer = CleanLayer(RoofGreen3Layer, DefaultRoofGreen3Layer);
            GreenRateMin = Clamp(GreenRateMin, 0, 1);
            GreenRateMax = Clamp(GreenRateMax, 0, 1);
            if (GreenRateMax < GreenRateMin)
            {
                var temp = GreenRateMin;
                GreenRateMin = GreenRateMax;
                GreenRateMax = temp;
            }

            BikeSpacesPer100 = Math.Max(0, BikeSpacesPer100);
            BikeAreaPerSpace = Math.Max(0, BikeAreaPerSpace);
            MotorSpacesPer100 = Math.Max(0, MotorSpacesPer100);
            GrassPaverFactor = GrassPaverFactor > 0 ? GrassPaverFactor : 1.0;
            RoofGreen1Factor = RoofGreen1Factor > 0 ? RoofGreen1Factor : 1.0;
            RoofGreen2Factor = RoofGreen2Factor > 0 ? RoofGreen2Factor : 1.0;
            RoofGreen3Factor = RoofGreen3Factor > 0 ? RoofGreen3Factor : 1.0;
            InfoLabelTextHeight = InfoLabelTextHeight > 0 ? InfoLabelTextHeight : 3.0;
            SummaryTableTextHeight = SummaryTableTextHeight > 0 ? SummaryTableTextHeight : 2.35;
            DetailTableTextHeight = DetailTableTextHeight > 0 ? DetailTableTextHeight : 2.35;
            if (HasSelectionScope)
            {
                if (ScopeMaxX < ScopeMinX)
                {
                    var temp = ScopeMinX;
                    ScopeMinX = ScopeMaxX;
                    ScopeMaxX = temp;
                }

                if (ScopeMaxY < ScopeMinY)
                {
                    var temp = ScopeMinY;
                    ScopeMinY = ScopeMaxY;
                    ScopeMaxY = temp;
                }
            }
            foreach (var key in AreaOverrides.Keys.ToList())
            {
                var value = AreaOverrides[key];
                if (value == null || string.IsNullOrWhiteSpace(value.Number))
                {
                    AreaOverrides.Remove(key);
                    continue;
                }

                value.Number = value.Number.Trim();
                if (value.BikeCalcArea.HasValue && value.BikeCalcArea.Value < 0)
                {
                    value.BikeCalcArea = null;
                }

                if (value.MotorCalcArea.HasValue && value.MotorCalcArea.Value < 0)
                {
                    value.MotorCalcArea = null;
                }

                if (!value.BikeCalcArea.HasValue && !value.MotorCalcArea.HasValue)
                {
                    AreaOverrides.Remove(key);
                }
            }

            foreach (var key in DetailOverrides.Keys.ToList())
            {
                var value = DetailOverrides[key];
                if (value == null || string.IsNullOrWhiteSpace(value.Number))
                {
                    DetailOverrides.Remove(key);
                    continue;
                }

                value.Number = value.Number.Trim();
                value.Type = value.Type == null ? "" : value.Type.Trim();
                value.BaseArea = Math.Max(0, value.BaseArea);
                value.CapacityArea = Math.Max(0, value.CapacityArea);
                value.LandArea = Math.Max(0, value.LandArea);
                value.GreenArea = Math.Max(0, value.GreenArea);
                value.BikeArea = Math.Max(0, value.BikeArea);
                value.NonCapacityArea = Math.Max(0, value.NonCapacityArea);
                value.Floors = Math.Max(0, value.Floors);
                value.GroundParking = Math.Max(0, value.GroundParking);
                value.IndoorParking = Math.Max(0, value.IndoorParking);
            }

            foreach (var key in SummaryOverrides.Keys.ToList())
            {
                var value = SummaryOverrides[key];
                if (value == null || string.IsNullOrWhiteSpace(value.Key))
                {
                    SummaryOverrides.Remove(key);
                    continue;
                }

                value.Key = value.Key.Trim();
                value.Parent = value.Parent == null ? "" : value.Parent.Trim();
                value.Item = value.Item == null ? "" : value.Item.Trim();
                value.SubItem = value.SubItem == null ? "" : value.SubItem.Trim();
                value.Unit = value.Unit == null ? "" : value.Unit.Trim();
                value.Value = Math.Max(0, value.Value);
                value.Remark = value.Remark == null ? "" : value.Remark.Trim();
                if (!value.Manual && !value.Custom && string.IsNullOrWhiteSpace(value.Remark) &&
                    string.IsNullOrWhiteSpace(value.Item) && string.IsNullOrWhiteSpace(value.SubItem) &&
                    string.IsNullOrWhiteSpace(value.Unit) && string.IsNullOrWhiteSpace(value.Parent))
                {
                    SummaryOverrides.Remove(key);
                }
            }
        }

        public IEnumerable<string> ToKeyValueStrings()
        {
            Normalize();
            yield return "VER=2";
            yield return "UNIT=" + UnitMode;
            yield return "PARCEL=" + ParcelLayer;
            yield return "LABEL=" + LabelLayer;
            yield return "BUILDING=" + BuildingLayer;
            yield return "BIKE_LAYER=" + BikeLayer;
            yield return "GREEN_LAYER=" + GreenLayer;
            yield return "FLOOR_LAYER=" + FloorLayer;
            yield return "GRASS_PAVER_LAYER=" + GrassPaverLayer;
            yield return "ROOF_GREEN1_LAYER=" + RoofGreen1Layer;
            yield return "ROOF_GREEN2_LAYER=" + RoofGreen2Layer;
            yield return "ROOF_GREEN3_LAYER=" + RoofGreen3Layer;
            yield return "GREEN_MIN=" + Format(GreenRateMin);
            yield return "GREEN_MAX=" + Format(GreenRateMax);
            yield return "BIKE_PER100=" + Format(BikeSpacesPer100);
            yield return "BIKE_AREA=" + Format(BikeAreaPerSpace);
            yield return "MOTOR_PER100=" + Format(MotorSpacesPer100);
            yield return "GRASS_PAVER_FACTOR=" + Format(GrassPaverFactor);
            yield return "ROOF_GREEN1_FACTOR=" + Format(RoofGreen1Factor);
            yield return "ROOF_GREEN2_FACTOR=" + Format(RoofGreen2Factor);
            yield return "ROOF_GREEN3_FACTOR=" + Format(RoofGreen3Factor);
            yield return "SHOW_INFO_LABELS=" + (ShowBuildingInfoLabels ? "1" : "0");
            yield return "INFO_TEXT_HEIGHT=" + Format(InfoLabelTextHeight);
            yield return "SUMMARY_TABLE_TEXT_HEIGHT=" + Format(SummaryTableTextHeight);
            yield return "DETAIL_TABLE_TEXT_HEIGHT=" + Format(DetailTableTextHeight);
            if (HasSelectionScope)
            {
                yield return "SCOPE=1";
                yield return "SCOPE_MIN_X=" + Format(ScopeMinX);
                yield return "SCOPE_MIN_Y=" + Format(ScopeMinY);
                yield return "SCOPE_MAX_X=" + Format(ScopeMaxX);
                yield return "SCOPE_MAX_Y=" + Format(ScopeMaxY);
            }

            foreach (var chunk in SplitOverrideText(SerializeOverrides(), 220))
            {
                yield return "OVR=" + chunk;
            }

            foreach (var chunk in SplitOverrideText(SerializeDetailOverrides(), 220))
            {
                yield return "DOVR=" + chunk;
            }

            foreach (var chunk in SplitOverrideText(SerializeSummaryOverrides(), 220))
            {
                yield return "SOVR=" + chunk;
            }
        }

        public void ApplyKeyValueStrings(IEnumerable<string> values)
        {
            var overrideBuilder = new StringBuilder();
            var detailOverrideBuilder = new StringBuilder();
            var summaryOverrideBuilder = new StringBuilder();
            foreach (var raw in values ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var separator = raw.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = raw.Substring(0, separator).Trim().ToUpperInvariant();
                var value = raw.Substring(separator + 1).Trim();
                switch (key)
                {
                    case "UNIT":
                        UnitMode = value;
                        break;
                    case "PARCEL":
                        ParcelLayer = value;
                        break;
                    case "LABEL":
                        LabelLayer = value;
                        break;
                    case "BUILDING":
                        BuildingLayer = value;
                        break;
                    case "BIKE_LAYER":
                        BikeLayer = value;
                        break;
                    case "GREEN_LAYER":
                        GreenLayer = value;
                        break;
                    case "FLOOR_LAYER":
                        FloorLayer = value;
                        break;
                    case "GRASS_PAVER_LAYER":
                        GrassPaverLayer = value;
                        break;
                    case "ROOF_GREEN1_LAYER":
                        RoofGreen1Layer = value;
                        break;
                    case "ROOF_GREEN2_LAYER":
                        RoofGreen2Layer = value;
                        break;
                    case "ROOF_GREEN3_LAYER":
                        RoofGreen3Layer = value;
                        break;
                    case "GREEN_MIN":
                        GreenRateMin = ParseDouble(value, GreenRateMin);
                        break;
                    case "GREEN_MAX":
                        GreenRateMax = ParseDouble(value, GreenRateMax);
                        break;
                    case "BIKE_PER100":
                        BikeSpacesPer100 = ParseDouble(value, BikeSpacesPer100);
                        break;
                    case "BIKE_AREA":
                        BikeAreaPerSpace = ParseDouble(value, BikeAreaPerSpace);
                        break;
                    case "MOTOR_PER100":
                        MotorSpacesPer100 = ParseDouble(value, MotorSpacesPer100);
                        break;
                    case "GRASS_PAVER_FACTOR":
                        GrassPaverFactor = ParseDouble(value, GrassPaverFactor);
                        break;
                    case "ROOF_GREEN1_FACTOR":
                        RoofGreen1Factor = ParseDouble(value, RoofGreen1Factor);
                        break;
                    case "ROOF_GREEN2_FACTOR":
                        RoofGreen2Factor = ParseDouble(value, RoofGreen2Factor);
                        break;
                    case "ROOF_GREEN3_FACTOR":
                        RoofGreen3Factor = ParseDouble(value, RoofGreen3Factor);
                        break;
                    case "SHOW_INFO_LABELS":
                        ShowBuildingInfoLabels = value == "1" ||
                                                 value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                                 value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "INFO_TEXT_HEIGHT":
                        InfoLabelTextHeight = ParseDouble(value, InfoLabelTextHeight);
                        break;
                    case "SUMMARY_TABLE_TEXT_HEIGHT":
                        SummaryTableTextHeight = ParseDouble(value, SummaryTableTextHeight);
                        break;
                    case "DETAIL_TABLE_TEXT_HEIGHT":
                        DetailTableTextHeight = ParseDouble(value, DetailTableTextHeight);
                        break;
                    case "SCOPE":
                        HasSelectionScope = value == "1" ||
                                            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                            value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "SCOPE_MIN_X":
                        ScopeMinX = ParseDouble(value, ScopeMinX);
                        break;
                    case "SCOPE_MIN_Y":
                        ScopeMinY = ParseDouble(value, ScopeMinY);
                        break;
                    case "SCOPE_MAX_X":
                        ScopeMaxX = ParseDouble(value, ScopeMaxX);
                        break;
                    case "SCOPE_MAX_Y":
                        ScopeMaxY = ParseDouble(value, ScopeMaxY);
                        break;
                    case "OVR":
                        overrideBuilder.Append(value);
                        break;
                    case "DOVR":
                        detailOverrideBuilder.Append(value);
                        break;
                    case "SOVR":
                        summaryOverrideBuilder.Append(value);
                        break;
                }
            }

            if (overrideBuilder.Length > 0)
            {
                DeserializeOverrides(overrideBuilder.ToString());
            }

            if (detailOverrideBuilder.Length > 0)
            {
                DeserializeDetailOverrides(detailOverrideBuilder.ToString());
            }

            if (summaryOverrideBuilder.Length > 0)
            {
                DeserializeSummaryOverrides(summaryOverrideBuilder.ToString());
            }

            Normalize();
        }

        public string SerializeOverrides()
        {
            var parts = new List<string>();
            foreach (var item in AreaOverrides.Values.OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(item.Number))
                {
                    continue;
                }

                var bike = item.BikeCalcArea.HasValue ? Format(item.BikeCalcArea.Value) : "";
                var motor = item.MotorCalcArea.HasValue ? Format(item.MotorCalcArea.Value) : "";
                if (bike.Length == 0 && motor.Length == 0)
                {
                    continue;
                }

                parts.Add(Escape(item.Number.Trim()) + "," + bike + "," + motor);
            }

            return string.Join("|", parts);
        }

        public void DeserializeOverrides(string text)
        {
            AreaOverrides.Clear();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var part in text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var cells = part.Split(',');
                if (cells.Length < 1)
                {
                    continue;
                }

                var number = Unescape(cells[0]).Trim();
                if (number.Length == 0)
                {
                    continue;
                }

                var bike = cells.Length > 1 && cells[1].Length > 0
                    ? (double?)ParseDouble(cells[1], 0)
                    : null;
                var motor = cells.Length > 2 && cells[2].Length > 0
                    ? (double?)ParseDouble(cells[2], 0)
                    : null;
                if (bike.HasValue || motor.HasValue)
                {
                    AreaOverrides[number] = new AreaOverride
                    {
                        Number = number,
                        BikeCalcArea = bike,
                        MotorCalcArea = motor
                    };
                }
            }
        }

        public string SerializeDetailOverrides()
        {
            var parts = new List<string>();
            foreach (var item in DetailOverrides.Values.OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase))
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Number))
                {
                    continue;
                }

                var hasValue =
                    !string.IsNullOrWhiteSpace(item.Type) ||
                    item.BaseAreaManual ||
                    item.CapacityAreaManual ||
                    item.LandAreaManual ||
                    item.GreenAreaManual ||
                    item.BikeAreaManual ||
                    item.NonCapacityArea > 0 ||
                    item.Floors > 0 ||
                    item.GroundParking > 0 ||
                    item.IndoorParking > 0;
                if (!hasValue)
                {
                    continue;
                }

                parts.Add(string.Join(",", new[]
                {
                    Escape(item.Number.Trim()),
                    Escape(item.Type ?? ""),
                    BoolText(item.BaseAreaManual),
                    Format(item.BaseArea),
                    BoolText(item.CapacityAreaManual),
                    Format(item.CapacityArea),
                    BoolText(item.LandAreaManual),
                    Format(item.LandArea),
                    BoolText(item.GreenAreaManual),
                    Format(item.GreenArea),
                    BoolText(item.BikeAreaManual),
                    Format(item.BikeArea),
                    Format(item.NonCapacityArea),
                    Format(item.Floors),
                    Format(item.GroundParking),
                    Format(item.IndoorParking)
                }));
            }

            return string.Join("|", parts);
        }

        public void DeserializeDetailOverrides(string text)
        {
            DetailOverrides.Clear();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var part in text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var cells = part.Split(',');
                if (cells.Length < 1)
                {
                    continue;
                }

                var number = Unescape(cells[0]).Trim();
                if (number.Length == 0)
                {
                    continue;
                }

                DetailOverrides[number] = new DetailOverride
                {
                    Number = number,
                    Type = cells.Length > 1 ? Unescape(cells[1]) : "",
                    BaseAreaManual = cells.Length > 2 && cells[2] == "1",
                    BaseArea = cells.Length > 3 ? ParseDouble(cells[3], 0) : 0,
                    CapacityAreaManual = cells.Length > 4 && cells[4] == "1",
                    CapacityArea = cells.Length > 5 ? ParseDouble(cells[5], 0) : 0,
                    LandAreaManual = cells.Length > 6 && cells[6] == "1",
                    LandArea = cells.Length > 7 ? ParseDouble(cells[7], 0) : 0,
                    GreenAreaManual = cells.Length > 8 && cells[8] == "1",
                    GreenArea = cells.Length > 9 ? ParseDouble(cells[9], 0) : 0,
                    BikeAreaManual = cells.Length > 10 && cells[10] == "1",
                    BikeArea = cells.Length > 11 ? ParseDouble(cells[11], 0) : 0,
                    NonCapacityArea = cells.Length > 12 ? ParseDouble(cells[12], 0) : 0,
                    Floors = cells.Length > 13 ? ParseDouble(cells[13], 0) : 0,
                    GroundParking = cells.Length > 14 ? ParseDouble(cells[14], 0) : 0,
                    IndoorParking = cells.Length > 15 ? ParseDouble(cells[15], 0) : 0
                };
            }
        }

        public string SerializeSummaryOverrides()
        {
            var parts = new List<string>();
            foreach (var item in SummaryOverrides.Values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                if (!item.Manual && !item.Custom && string.IsNullOrWhiteSpace(item.Remark) &&
                    string.IsNullOrWhiteSpace(item.Parent) && string.IsNullOrWhiteSpace(item.Item) &&
                    string.IsNullOrWhiteSpace(item.SubItem) && string.IsNullOrWhiteSpace(item.Unit))
                {
                    continue;
                }

                parts.Add(string.Join(",", new[]
                {
                    Escape(item.Key.Trim()),
                    Escape(item.Parent ?? ""),
                    Escape(item.Item ?? ""),
                    Escape(item.SubItem ?? ""),
                    Escape(item.Unit ?? ""),
                    BoolText(item.Manual),
                    Format(item.Value),
                    Escape(item.Remark ?? ""),
                    BoolText(item.Custom)
                }));
            }

            return string.Join("|", parts);
        }

        public void DeserializeSummaryOverrides(string text)
        {
            SummaryOverrides.Clear();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var part in text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var cells = part.Split(',');
                if (cells.Length < 1)
                {
                    continue;
                }

                var key = Unescape(cells[0]).Trim();
                if (key.Length == 0)
                {
                    continue;
                }

                if (cells.Length == 4 && (cells[1] == "1" || cells[1] == "0"))
                {
                    SummaryOverrides[key] = new SummaryOverride
                    {
                        Key = key,
                        Manual = cells[1] == "1",
                        Value = ParseDouble(cells[2], 0),
                        Remark = Unescape(cells[3])
                    };
                    continue;
                }

                SummaryOverrides[key] = new SummaryOverride
                {
                    Key = key,
                    Parent = cells.Length > 1 ? Unescape(cells[1]) : "",
                    Item = cells.Length > 2 ? Unescape(cells[2]) : "",
                    SubItem = cells.Length > 3 ? Unescape(cells[3]) : "",
                    Unit = cells.Length > 4 ? Unescape(cells[4]) : "",
                    Manual = cells.Length > 5 && cells[5] == "1",
                    Value = cells.Length > 6 ? ParseDouble(cells[6], 0) : 0,
                    Remark = cells.Length > 7 ? Unescape(cells[7]) : "",
                    Custom = cells.Length > 8 && cells[8] == "1"
                };
            }
        }

        private static DetailOverride CloneDetailOverride(DetailOverride value)
        {
            if (value == null)
            {
                return null;
            }

            return new DetailOverride
            {
                Number = value.Number,
                Type = value.Type,
                BaseAreaManual = value.BaseAreaManual,
                BaseArea = value.BaseArea,
                CapacityAreaManual = value.CapacityAreaManual,
                CapacityArea = value.CapacityArea,
                LandAreaManual = value.LandAreaManual,
                LandArea = value.LandArea,
                GreenAreaManual = value.GreenAreaManual,
                GreenArea = value.GreenArea,
                BikeAreaManual = value.BikeAreaManual,
                BikeArea = value.BikeArea,
                NonCapacityArea = value.NonCapacityArea,
                Floors = value.Floors,
                GroundParking = value.GroundParking,
                IndoorParking = value.IndoorParking
            };
        }

        private static IEnumerable<string> SplitOverrideText(string text, int chunkSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            for (var i = 0; i < text.Length; i += chunkSize)
            {
                yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            }
        }

        internal static double ParseDouble(string text, double fallback)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                ? value
                : fallback;
        }

        internal static string Format(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string CleanLayer(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
        }

        private static string Escape(string value)
        {
            return value.Replace("%", "%25").Replace("|", "%7C").Replace(",", "%2C");
        }

        private static string Unescape(string value)
        {
            return value.Replace("%2C", ",").Replace("%7C", "|").Replace("%25", "%");
        }
    }

    internal static class LandIndexSettingsStore
    {
        private const string RegistryPath = @"Software\BuildingAreaTool\LandIndexTool";

        public static LandIndexSettings Load()
        {
            var settings = new LandIndexSettings();
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null)
                    {
                        return settings;
                    }

                    settings.UnitMode = ReadString(key, "UnitMode", settings.UnitMode);
                    settings.ParcelLayer = ReadString(key, "ParcelLayer", settings.ParcelLayer);
                    settings.LabelLayer = ReadString(key, "LabelLayer", settings.LabelLayer);
                    settings.BuildingLayer = ReadString(key, "BuildingLayer", settings.BuildingLayer);
                    settings.BikeLayer = ReadString(key, "BikeLayer", settings.BikeLayer);
                    settings.GreenLayer = ReadString(key, "GreenLayer", settings.GreenLayer);
                    settings.FloorLayer = ReadString(key, "FloorLayer", settings.FloorLayer);
                    settings.GrassPaverLayer = ReadString(key, "GrassPaverLayer", settings.GrassPaverLayer);
                    settings.RoofGreen1Layer = ReadString(key, "RoofGreen1Layer", settings.RoofGreen1Layer);
                    settings.RoofGreen2Layer = ReadString(key, "RoofGreen2Layer", settings.RoofGreen2Layer);
                    settings.RoofGreen3Layer = ReadString(key, "RoofGreen3Layer", settings.RoofGreen3Layer);
                    settings.GreenRateMin = ReadDouble(key, "GreenRateMin", settings.GreenRateMin);
                    settings.GreenRateMax = ReadDouble(key, "GreenRateMax", settings.GreenRateMax);
                    settings.BikeSpacesPer100 = ReadDouble(key, "BikeSpacesPer100", settings.BikeSpacesPer100);
                    settings.BikeAreaPerSpace = ReadDouble(key, "BikeAreaPerSpace", settings.BikeAreaPerSpace);
                    settings.MotorSpacesPer100 = ReadDouble(key, "MotorSpacesPer100", settings.MotorSpacesPer100);
                    settings.GrassPaverFactor = ReadDouble(key, "GrassPaverFactor", settings.GrassPaverFactor);
                    settings.RoofGreen1Factor = ReadDouble(key, "RoofGreen1Factor", settings.RoofGreen1Factor);
                    settings.RoofGreen2Factor = ReadDouble(key, "RoofGreen2Factor", settings.RoofGreen2Factor);
                    settings.RoofGreen3Factor = ReadDouble(key, "RoofGreen3Factor", settings.RoofGreen3Factor);
                    settings.ShowBuildingInfoLabels = ReadBool(key, "ShowBuildingInfoLabels", settings.ShowBuildingInfoLabels);
                    settings.InfoLabelTextHeight = ReadDouble(key, "InfoLabelTextHeight", settings.InfoLabelTextHeight);
                    settings.SummaryTableTextHeight = ReadDouble(key, "SummaryTableTextHeight", settings.SummaryTableTextHeight);
                    settings.DetailTableTextHeight = ReadDouble(key, "DetailTableTextHeight", settings.DetailTableTextHeight);
                    settings.DeserializeOverrides(ReadString(key, "AreaOverrides", ""));
                    settings.DeserializeDetailOverrides(ReadString(key, "DetailOverrides", ""));
                    settings.DeserializeSummaryOverrides(ReadString(key, "SummaryOverrides", ""));
                }
            }
            catch
            {
                return new LandIndexSettings();
            }

            settings.Normalize();
            return settings;
        }

        public static void Save(LandIndexSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.Normalize();
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    key.SetValue("UnitMode", settings.UnitMode);
                    key.SetValue("ParcelLayer", settings.ParcelLayer);
                    key.SetValue("LabelLayer", settings.LabelLayer);
                    key.SetValue("BuildingLayer", settings.BuildingLayer);
                    key.SetValue("BikeLayer", settings.BikeLayer);
                    key.SetValue("GreenLayer", settings.GreenLayer);
                    key.SetValue("FloorLayer", settings.FloorLayer);
                    key.SetValue("GrassPaverLayer", settings.GrassPaverLayer);
                    key.SetValue("RoofGreen1Layer", settings.RoofGreen1Layer);
                    key.SetValue("RoofGreen2Layer", settings.RoofGreen2Layer);
                    key.SetValue("RoofGreen3Layer", settings.RoofGreen3Layer);
                    key.SetValue("GreenRateMin", LandIndexSettings.Format(settings.GreenRateMin));
                    key.SetValue("GreenRateMax", LandIndexSettings.Format(settings.GreenRateMax));
                    key.SetValue("BikeSpacesPer100", LandIndexSettings.Format(settings.BikeSpacesPer100));
                    key.SetValue("BikeAreaPerSpace", LandIndexSettings.Format(settings.BikeAreaPerSpace));
                    key.SetValue("MotorSpacesPer100", LandIndexSettings.Format(settings.MotorSpacesPer100));
                    key.SetValue("GrassPaverFactor", LandIndexSettings.Format(settings.GrassPaverFactor));
                    key.SetValue("RoofGreen1Factor", LandIndexSettings.Format(settings.RoofGreen1Factor));
                    key.SetValue("RoofGreen2Factor", LandIndexSettings.Format(settings.RoofGreen2Factor));
                    key.SetValue("RoofGreen3Factor", LandIndexSettings.Format(settings.RoofGreen3Factor));
                    key.SetValue("ShowBuildingInfoLabels", settings.ShowBuildingInfoLabels ? "1" : "0");
                    key.SetValue("InfoLabelTextHeight", LandIndexSettings.Format(settings.InfoLabelTextHeight));
                    key.SetValue("SummaryTableTextHeight", LandIndexSettings.Format(settings.SummaryTableTextHeight));
                    key.SetValue("DetailTableTextHeight", LandIndexSettings.Format(settings.DetailTableTextHeight));
                    key.SetValue("AreaOverrides", settings.SerializeOverrides());
                    key.SetValue("DetailOverrides", settings.SerializeDetailOverrides());
                    key.SetValue("SummaryOverrides", settings.SerializeSummaryOverrides());
                }
            }
            catch
            {
                // Registry persistence is a convenience feature; the command should still work if it is blocked.
            }
        }

        private static string ReadString(RegistryKey key, string name, string fallback)
        {
            return key.GetValue(name) as string ?? fallback;
        }

        private static double ReadDouble(RegistryKey key, string name, double fallback)
        {
            return LandIndexSettings.ParseDouble(ReadString(key, name, ""), fallback);
        }

        private static bool ReadBool(RegistryKey key, string name, bool fallback)
        {
            var value = ReadString(key, name, fallback ? "1" : "0");
            return value == "1" ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
