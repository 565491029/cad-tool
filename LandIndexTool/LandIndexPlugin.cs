using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

[assembly: CommandClass(typeof(LandIndexTool.LandIndexPlugin))]

namespace LandIndexTool
{
    public class LandIndexPlugin : IExtensionApplication
    {
        private const string RegAppName = "LAND_INDEX_TOOL";
        private const string TableMarker = "LAND_INDEX_TABLE";
        private const string InfoLabelMarker = "LAND_INDEX_INFO_LABEL";
        private const string InfoLabelLayer = "1xx";
        private const string LayerYd = LandIndexSettings.DefaultParcelLayer;
        private const string LayerBh = LandIndexSettings.DefaultLabelLayer;
        private const string LayerJz = LandIndexSettings.DefaultBuildingLayer;
        private const string LayerFjdc = LandIndexSettings.DefaultBikeLayer;
        private const string LayerLd = LandIndexSettings.DefaultGreenLayer;
        private const string LayerCs = LandIndexSettings.DefaultFloorLayer;
        private const string LayerZcz = LandIndexSettings.DefaultGrassPaverLayer;
        private const string LayerWm1 = LandIndexSettings.DefaultRoofGreen1Layer;
        private const string LayerWm2 = LandIndexSettings.DefaultRoofGreen2Layer;
        private const string LayerWm3 = LandIndexSettings.DefaultRoofGreen3Layer;
        private const string SummaryRole = "SUMMARY";
        private const string DetailRole = "DETAIL";
        private const string OverrideRole = "OVERRIDE";
        private const int SummaryRowCount = 23;
        private const int SummaryColumnCount = 5;
        private const int DetailHeaderRow = 1;
        private const int DetailStartRow = 2;
        private const int DetailColumnCount = 17;
        private const int OverrideHeaderRow = 1;
        private const int OverrideStartRow = 2;
        private const int OverrideColumnCount = 4;
        private const int ColumnCount = DetailColumnCount;
        private const double RowHeight = 7.2;
        private const double TitleRowHeight = 9.0;
        private const double TextHeight = 2.35;
        private const double HeaderTextHeight = 2.2;
        private const double TitleTextHeight = 3.0;
        private const double TableGap = 12.0;
        private const double Tolerance = 0.01;

        private static ObjectId _summaryTableId = ObjectId.Null;
        private static ObjectId _detailTableId = ObjectId.Null;
        private static ObjectId _overrideTableId = ObjectId.Null;
        private static bool _attached;
        private static bool _pendingRefresh;
        private static bool _isRefreshing;
        private static LandIndexSettings _settings = LandIndexSettingsStore.Load();
        private static double _areaFactor = 0.000001;
        private static string _parcelLayer = LayerYd;
        private static string _labelLayer = LayerBh;
        private static string _buildingLayer = LayerJz;
        private static string _bikeLayer = LayerFjdc;
        private static string _greenLayer = LayerLd;
        private static string _floorLayer = LayerCs;
        private static string _grassPaverLayer = LayerZcz;
        private static string _roofGreen1Layer = LayerWm1;
        private static string _roofGreen2Layer = LayerWm2;
        private static string _roofGreen3Layer = LayerWm3;

        public void Initialize()
        {
            DetachOtherLoadedLandIndexPlugins();
            ApplySettings(_settings);
            Attach();
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                "\n地块指标表工具已加载。命令：" + GetCreateCommandName() + " 生成指标表，" + GetUpdateCommandName() + " 刷新指标表。");
        }

        public void Terminate()
        {
            Detach();
        }

        private static void DetachOtherLoadedLandIndexPlugins()
        {
            var currentAssembly = typeof(LandIndexPlugin).Assembly;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == currentAssembly)
                {
                    continue;
                }

                var name = assembly.GetName().Name ?? "";
                if (!name.StartsWith("LandIndexTool", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pluginType = assembly.GetType("LandIndexTool.LandIndexPlugin", false);
                var detach = pluginType?.GetMethod("Detach", BindingFlags.NonPublic | BindingFlags.Static);
                try
                {
                    detach?.Invoke(null, null);
                }
                catch
                {
                    // Older loaded test assemblies are best-effort detached to avoid duplicate refresh handlers.
                }
            }
        }

#if LANDINDEX_V30
        [CommandMethod("dulang30")]
#elif LANDINDEX_V29
        [CommandMethod("dulang29")]
#elif LANDINDEX_V28
        [CommandMethod("dulang28")]
#elif LANDINDEX_V27
        [CommandMethod("dulang27")]
#elif LANDINDEX_V24
        [CommandMethod("dulang1")]
#elif LANDINDEX_V23
        [CommandMethod("JZMJYDTABLE23")]
#elif LANDINDEX_V22
        [CommandMethod("JZMJYDTABLE22")]
#elif LANDINDEX_V21
        [CommandMethod("JZMJYDTABLE21")]
#elif LANDINDEX_V20
        [CommandMethod("JZMJYDTABLE20")]
#elif LANDINDEX_V19
        [CommandMethod("JZMJYDTABLE19")]
#elif LANDINDEX_V18
        [CommandMethod("JZMJYDTABLE18")]
#elif LANDINDEX_V17
        [CommandMethod("JZMJYDTABLE17")]
#elif LANDINDEX_V16
        [CommandMethod("JZMJYDTABLE16")]
#elif LANDINDEX_V15
        [CommandMethod("JZMJYDTABLE15")]
#elif LANDINDEX_V14
        [CommandMethod("JZMJYDTABLE14")]
#elif LANDINDEX_V13
        [CommandMethod("JZMJYDTABLE13")]
#elif LANDINDEX_V12
        [CommandMethod("JZMJYDTABLE12")]
#elif LANDINDEX_V11
        [CommandMethod("JZMJYDTABLE11")]
#elif LANDINDEX_V10
        [CommandMethod("JZMJYDTABLE10")]
#elif LANDINDEX_V9
        [CommandMethod("JZMJYDTABLE9")]
#elif LANDINDEX_V8
        [CommandMethod("JZMJYDTABLE8")]
#elif LANDINDEX_V7
        [CommandMethod("JZMJYDTABLE7")]
#elif LANDINDEX_V6
        [CommandMethod("JZMJYDTABLE6")]
#elif LANDINDEX_V5
        [CommandMethod("JZMJYDTABLE5")]
#elif LANDINDEX_V4
        [CommandMethod("JZMJYDTABLE4")]
#elif LANDINDEX_V3
        [CommandMethod("JZMJYDTABLE3")]
#elif LANDINDEX_V2
        [CommandMethod("JZMJYDTABLE2")]
#else
        [CommandMethod("dulang1")]
#endif
        public void CreateLandIndexTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var settings = LandIndexSettingsStore.Load();
            ApplySettings(settings);
            if (!PromptSelectionScope(ed, db, settings))
            {
                return;
            }
            ApplySettings(settings);
            var previewRows = CreatePreviewRows(db, settings);
            var seeds = previewRows
                .Select(x => new AreaOverrideSeed { Number = x.Number, DefaultCalcArea = x.CapacityArea })
                .ToList();
            using (var form = new LandIndexSettingsForm(settings, seeds, previewRows))
            {
                if (Application.ShowModalDialog(form) != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                settings = form.Settings;
            }

            LandIndexSettingsStore.Save(settings);
            ApplySettings(settings);
            EnsureRegApp(db);

            var point = ed.GetPoint("\n指定地块指标表插入点：");
            if (point.Status != PromptStatus.OK)
            {
                return;
            }

            string scanSummary;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var records = ScanDrawing(db, tr);
                var inputs = new Dictionary<string, RowInput>(StringComparer.OrdinalIgnoreCase);
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var summaryTable = new Table
                {
                    TableStyle = db.Tablestyle,
                    Position = point.Value
                };
                var detailTable = new Table
                {
                    TableStyle = db.Tablestyle,
                    Position = point.Value
                };
                var overrideTable = new Table
                {
                    TableStyle = db.Tablestyle,
                    Position = point.Value
                };
                ApplyTableXData(summaryTable, SummaryRole, _settings);
                ApplyTableXData(detailTable, DetailRole, _settings);
                ApplyTableXData(overrideTable, OverrideRole, _settings);
                var outputs = BuildOutputs(records, inputs, _settings);
                FillSummaryTable(summaryTable, outputs, _settings);
                detailTable.Position = GetNextTablePosition(summaryTable);
                FillDetailTable(detailTable, outputs);
                overrideTable.Position = GetNextTablePosition(detailTable);
                FillOverrideTable(overrideTable, outputs, _settings);
                UpdateBuildingInfoLabels(db, tr, outputs, _settings);
                scanSummary = BuildScanSummary(outputs);
                ms.AppendEntity(summaryTable);
                tr.AddNewlyCreatedDBObject(summaryTable, true);
                ms.AppendEntity(detailTable);
                tr.AddNewlyCreatedDBObject(detailTable, true);
                ms.AppendEntity(overrideTable);
                tr.AddNewlyCreatedDBObject(overrideTable, true);
                _summaryTableId = summaryTable.ObjectId;
                _detailTableId = detailTable.ObjectId;
                _overrideTableId = overrideTable.ObjectId;
                tr.Commit();
            }

            ed.WriteMessage("\n已根据 " + _parcelLayer + "/" + _labelLayer + "/" + _buildingLayer + "/" + _bikeLayer + "/" + _greenLayer + "/" + _floorLayer + "/" + _grassPaverLayer + "/" + _roofGreen1Layer + "/" + _roofGreen2Layer + "/" + _roofGreen3Layer + " 图层生成地块指标表。" + scanSummary);
        }

#if LANDINDEX_V30
        [CommandMethod("JZMJYDUPDATE30")]
#elif LANDINDEX_V29
        [CommandMethod("JZMJYDUPDATE29")]
#elif LANDINDEX_V28
        [CommandMethod("JZMJYDUPDATE28")]
#elif LANDINDEX_V27
        [CommandMethod("JZMJYDUPDATE27")]
#elif LANDINDEX_V24
        [CommandMethod("JZMJYDUPDATE24")]
#elif LANDINDEX_V23
        [CommandMethod("JZMJYDUPDATE23")]
#elif LANDINDEX_V22
        [CommandMethod("JZMJYDUPDATE22")]
#elif LANDINDEX_V21
        [CommandMethod("JZMJYDUPDATE21")]
#elif LANDINDEX_V20
        [CommandMethod("JZMJYDUPDATE20")]
#elif LANDINDEX_V19
        [CommandMethod("JZMJYDUPDATE19")]
#elif LANDINDEX_V18
        [CommandMethod("JZMJYDUPDATE18")]
#elif LANDINDEX_V17
        [CommandMethod("JZMJYDUPDATE17")]
#elif LANDINDEX_V16
        [CommandMethod("JZMJYDUPDATE16")]
#elif LANDINDEX_V15
        [CommandMethod("JZMJYDUPDATE15")]
#elif LANDINDEX_V14
        [CommandMethod("JZMJYDUPDATE14")]
#elif LANDINDEX_V13
        [CommandMethod("JZMJYDUPDATE13")]
#elif LANDINDEX_V12
        [CommandMethod("JZMJYDUPDATE12")]
#elif LANDINDEX_V11
        [CommandMethod("JZMJYDUPDATE11")]
#elif LANDINDEX_V10
        [CommandMethod("JZMJYDUPDATE10")]
#elif LANDINDEX_V9
        [CommandMethod("JZMJYDUPDATE9")]
#elif LANDINDEX_V8
        [CommandMethod("JZMJYDUPDATE8")]
#elif LANDINDEX_V7
        [CommandMethod("JZMJYDUPDATE7")]
#elif LANDINDEX_V6
        [CommandMethod("JZMJYDUPDATE6")]
#elif LANDINDEX_V5
        [CommandMethod("JZMJYDUPDATE5")]
#elif LANDINDEX_V4
        [CommandMethod("JZMJYDUPDATE4")]
#elif LANDINDEX_V3
        [CommandMethod("JZMJYDUPDATE3")]
#elif LANDINDEX_V2
        [CommandMethod("JZMJYDUPDATE2")]
#else
        [CommandMethod("JZMJYDUPDATE")]
#endif
        public void UpdateLandIndexTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                RefreshExistingTable(doc.Database, tr, doc.Editor, true);
                tr.Commit();
            }
        }

#if LANDINDEX_V30
        [CommandMethod("JZMJYDHELP30")]
#elif LANDINDEX_V29
        [CommandMethod("JZMJYDHELP29")]
#elif LANDINDEX_V28
        [CommandMethod("JZMJYDHELP28")]
#elif LANDINDEX_V27
        [CommandMethod("JZMJYDHELP27")]
#elif LANDINDEX_V24
        [CommandMethod("JZMJYDHELP24")]
#elif LANDINDEX_V23
        [CommandMethod("JZMJYDHELP23")]
#elif LANDINDEX_V22
        [CommandMethod("JZMJYDHELP22")]
#elif LANDINDEX_V21
        [CommandMethod("JZMJYDHELP21")]
#elif LANDINDEX_V20
        [CommandMethod("JZMJYDHELP20")]
#elif LANDINDEX_V19
        [CommandMethod("JZMJYDHELP19")]
#elif LANDINDEX_V18
        [CommandMethod("JZMJYDHELP18")]
#elif LANDINDEX_V17
        [CommandMethod("JZMJYDHELP17")]
#elif LANDINDEX_V16
        [CommandMethod("JZMJYDHELP16")]
#elif LANDINDEX_V15
        [CommandMethod("JZMJYDHELP15")]
#elif LANDINDEX_V14
        [CommandMethod("JZMJYDHELP14")]
#elif LANDINDEX_V13
        [CommandMethod("JZMJYDHELP13")]
#elif LANDINDEX_V12
        [CommandMethod("JZMJYDHELP12")]
#elif LANDINDEX_V11
        [CommandMethod("JZMJYDHELP11")]
#elif LANDINDEX_V10
        [CommandMethod("JZMJYDHELP10")]
#elif LANDINDEX_V9
        [CommandMethod("JZMJYDHELP9")]
#elif LANDINDEX_V8
        [CommandMethod("JZMJYDHELP8")]
#elif LANDINDEX_V7
        [CommandMethod("JZMJYDHELP7")]
#elif LANDINDEX_V6
        [CommandMethod("JZMJYDHELP6")]
#elif LANDINDEX_V5
        [CommandMethod("JZMJYDHELP5")]
#elif LANDINDEX_V4
        [CommandMethod("JZMJYDHELP4")]
#elif LANDINDEX_V3
        [CommandMethod("JZMJYDHELP3")]
#elif LANDINDEX_V2
        [CommandMethod("JZMJYDHELP2")]
#else
        [CommandMethod("JZMJYDHELP")]
#endif
        public void ShowHelp()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                "\n地块指标表：默认 1yd 图层闭合 PLINE 为用地范围，1bh 图层文字为编号，1jz 图层闭合 PLINE 为建筑基底。" +
                "\n1fjdc 图层闭合 PLINE 计入非机动车㎡，1ld/1zcz/1wm1/1wm2/1wm3 图层闭合 PLINE 按折算系数计入绿地㎡，1cs 图层文字计入层数。" +
                "\n" + GetCreateCommandName() + "  生成指标表" +
                "\n" + GetUpdateCommandName() + " 刷新指标表" +
                "\n生成时会弹出设置窗口；可在表格中手动改“类型、地面、室内”，刷新时会保留这些手填项；机动车（个）=地面+室内；层数优先读取层数图层。");
        }

        private static void Attach()
        {
            if (_attached)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            doc.Database.ObjectModified += OnObjectModified;
            doc.Database.ObjectAppended += OnObjectAppended;
            doc.Database.ObjectErased += OnObjectErased;
            doc.CommandEnded += OnCommandEnded;
            doc.CommandCancelled += OnCommandInterrupted;
            doc.CommandFailed += OnCommandInterrupted;
            _attached = true;
        }

        private static void Detach()
        {
            if (!_attached)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Database.ObjectModified -= OnObjectModified;
                doc.Database.ObjectAppended -= OnObjectAppended;
                doc.Database.ObjectErased -= OnObjectErased;
                doc.CommandEnded -= OnCommandEnded;
                doc.CommandCancelled -= OnCommandInterrupted;
                doc.CommandFailed -= OnCommandInterrupted;
            }

            _pendingRefresh = false;
            _attached = false;
        }

        private static void OnObjectModified(object sender, ObjectEventArgs e)
        {
            TrackChangedObject(e.DBObject);
        }

        private static void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            TrackChangedObject(e.DBObject);
        }

        private static void OnObjectErased(object sender, ObjectErasedEventArgs e)
        {
            TrackChangedObject(e.DBObject);
        }

        private static void TrackChangedObject(DBObject dbObject)
        {
            if (_isRefreshing || dbObject == null || dbObject.ObjectId.IsNull)
            {
                return;
            }

            var entity = dbObject as Entity;
            if (entity == null)
            {
                return;
            }

            if (IsLayer(entity, _parcelLayer) ||
                IsLayer(entity, _labelLayer) ||
                IsLayer(entity, _buildingLayer) ||
                IsLayer(entity, _bikeLayer) ||
                IsLayer(entity, _greenLayer) ||
                IsLayer(entity, _floorLayer) ||
                IsGreenVariantLayer(entity, _grassPaverLayer, "zcz") ||
                IsGreenVariantLayer(entity, _roofGreen1Layer, "wm1") ||
                IsGreenVariantLayer(entity, _roofGreen2Layer, "wm2") ||
                IsGreenVariantLayer(entity, _roofGreen3Layer, "wm3"))
            {
                _pendingRefresh = true;
                return;
            }

            var table = dbObject as Table;
            if (table != null && IsLandIndexTable(table))
            {
                var role = GetTableRole(table);
                if (role == SummaryRole)
                {
                    _summaryTableId = table.ObjectId;
                }
                else if (role == DetailRole)
                {
                    _detailTableId = table.ObjectId;
                }
                else if (role == OverrideRole)
                {
                    _overrideTableId = table.ObjectId;
                }
                _pendingRefresh = true;
            }
        }

        private static void OnCommandEnded(object sender, CommandEventArgs e)
        {
            if (!_pendingRefresh)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _pendingRefresh = false;
                return;
            }

            try
            {
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    _isRefreshing = true;
                    try
                    {
                        RefreshExistingTable(doc.Database, tr, doc.Editor, false);
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }

                    tr.Commit();
                }
            }
            finally
            {
                _pendingRefresh = false;
            }
        }

        private static void OnCommandInterrupted(object sender, CommandEventArgs e)
        {
            _pendingRefresh = false;
        }

        private static void ApplySettings(LandIndexSettings settings)
        {
            _settings = settings?.Clone() ?? new LandIndexSettings();
            _settings.Normalize();
            _areaFactor = _settings.AreaFactor;
            _parcelLayer = _settings.ParcelLayer;
            _labelLayer = _settings.LabelLayer;
            _buildingLayer = _settings.BuildingLayer;
            _bikeLayer = _settings.BikeLayer;
            _greenLayer = _settings.GreenLayer;
            _floorLayer = _settings.FloorLayer;
            _grassPaverLayer = _settings.GrassPaverLayer;
            _roofGreen1Layer = _settings.RoofGreen1Layer;
            _roofGreen2Layer = _settings.RoofGreen2Layer;
            _roofGreen3Layer = _settings.RoofGreen3Layer;
        }

        private static bool PromptSelectionScope(Editor ed, Database db, LandIndexSettings settings)
        {
            var options = new PromptSelectionOptions
            {
                MessageForAdding = "\n请框选本次统计范围内的图形对象：",
                MessageForRemoval = "\n从统计范围中移除对象："
            };
            var result = ed.GetSelection(options);
            if (result.Status != PromptStatus.OK || result.Value == null || result.Value.Count == 0)
            {
                ed.WriteMessage("\n未选择统计范围，命令已取消。");
                return false;
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var hasExtents = false;
                var minX = 0.0;
                var minY = 0.0;
                var maxX = 0.0;
                var maxY = 0.0;
                foreach (var id in result.Value.GetObjectIds())
                {
                    var entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    try
                    {
                        var ext = entity.GeometricExtents;
                        if (!hasExtents)
                        {
                            minX = ext.MinPoint.X;
                            minY = ext.MinPoint.Y;
                            maxX = ext.MaxPoint.X;
                            maxY = ext.MaxPoint.Y;
                            hasExtents = true;
                        }
                        else
                        {
                            minX = Math.Min(minX, ext.MinPoint.X);
                            minY = Math.Min(minY, ext.MinPoint.Y);
                            maxX = Math.Max(maxX, ext.MaxPoint.X);
                            maxY = Math.Max(maxY, ext.MaxPoint.Y);
                        }
                    }
                    catch
                    {
                        // Some database objects do not expose extents; ignore them for the scope rectangle.
                    }
                }

                tr.Commit();
                if (!hasExtents)
                {
                    ed.WriteMessage("\n未能从选择集计算统计范围，命令已取消。");
                    return false;
                }

                var padding = Math.Max(maxX - minX, maxY - minY) * 0.001;
                settings.HasSelectionScope = true;
                settings.ScopeMinX = minX - padding;
                settings.ScopeMinY = minY - padding;
                settings.ScopeMaxX = maxX + padding;
                settings.ScopeMaxY = maxY + padding;
                return true;
            }
        }

        private static List<AreaOverrideSeed> CreateAreaOverrideSeeds(Database db)
        {
            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var records = ScanDrawing(db, tr);
                    var outputs = BuildOutputs(records, new Dictionary<string, RowInput>(StringComparer.OrdinalIgnoreCase), _settings);
                    tr.Commit();
                    return outputs
                        .Select(x => new AreaOverrideSeed
                        {
                            Number = x.Number,
                            DefaultCalcArea = x.CapacityArea
                        })
                        .ToList();
                }
            }
            catch
            {
                return new List<AreaOverrideSeed>();
            }
        }

        private static List<LandIndexPreviewRow> CreatePreviewRows(Database db, LandIndexSettings settings)
        {
            try
            {
                ApplySettings(settings);
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var records = ScanDrawing(db, tr);
                    var outputs = BuildOutputs(records, new Dictionary<string, RowInput>(StringComparer.OrdinalIgnoreCase), _settings);
                    tr.Commit();
                    return outputs.Select(ToPreviewRow).ToList();
                }
            }
            catch
            {
                return new List<LandIndexPreviewRow>();
            }
        }

        private static LandIndexPreviewRow ToPreviewRow(RowOutput output)
        {
            return new LandIndexPreviewRow
            {
                Number = output.Number,
                Type = output.Type,
                BaseArea = output.BaseArea,
                CapacityArea = output.CapacityArea,
                NonCapacityArea = output.NonCapacityArea,
                BuildingArea = output.BuildingArea,
                LandArea = output.LandArea,
                GreenArea = output.GreenArea,
                BikeArea = output.BikeArea,
                Floors = output.Floors,
                GroundParking = output.GroundParking,
                IndoorParking = output.IndoorParking,
                Far = output.Far
            };
        }

        private static void RefreshExistingTable(Database db, Transaction tr, Editor ed, bool report)
        {
            var tables = GetTables(db, tr, OpenMode.ForWrite);
            if (tables.Summary == null || tables.Detail == null)
            {
                if (report)
                {
                    ed.WriteMessage("\n未找到地块指标表，请先运行 " + GetCreateCommandName() + "。");
                }
                return;
            }

            ReadTableSettings(tables.Summary);
            var inputs = ReadInputs(tables.Detail);
            if (tables.Override != null)
            {
                ReadAreaOverrides(tables.Override, _settings);
            }
            MergeInputsIntoDetailOverrides(inputs, _settings);
            var records = ScanDrawing(db, tr);
            var outputs = BuildOutputs(records, inputs, _settings);
            FillSummaryTable(tables.Summary, outputs, _settings);
            RepositionDetailTableIfOverlapping(tables.Summary, tables.Detail);
            FillDetailTable(tables.Detail, outputs);
            tables.Override = EnsureOverrideTable(db, tr, tables);
            RepositionOverrideTable(tables.Detail, tables.Override);
            FillOverrideTable(tables.Override, outputs, _settings);
            ApplyTableXData(tables.Summary, SummaryRole, _settings);
            ApplyTableXData(tables.Detail, DetailRole, _settings);
            ApplyTableXData(tables.Override, OverrideRole, _settings);
            UpdateBuildingInfoLabels(db, tr, outputs, _settings);
            if (report)
            {
                ed.WriteMessage("\n地块指标表已刷新。" + BuildScanSummary(outputs));
            }
        }

        private static string BuildScanSummary(List<RowOutput> outputs)
        {
            var totalLand = outputs.Sum(x => x.LandArea);
            var totalBase = outputs.Sum(x => x.BaseArea);
            var totalGreen = outputs.Sum(x => x.GreenArea);
            var totalBike = outputs.Sum(x => x.BikeArea);
            var message = "\n扫描结果：地块 " + outputs.Count +
                          " 个，用地 " + FormatArea(totalLand) +
                          "㎡，基底 " + FormatArea(totalBase) +
                          "㎡，绿地 " + FormatArea(totalGreen) +
                          "㎡，非机动车 " + FormatArea(totalBike) + "㎡。";
            if (string.Equals(_settings.UnitMode, "MM", StringComparison.OrdinalIgnoreCase) && totalLand > 0 && totalLand < 1)
            {
                message += "\n提示：当前单位为 MM，统计面积很小。如果图纸坐标单位实际是米，请在设置窗口选择 M 后重新生成表格。";
            }

            return message;
        }

        private static Table EnsureOverrideTable(Database db, Transaction tr, TablePair tables)
        {
            if (tables.Override != null)
            {
                return tables.Override;
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var table = new Table
            {
                TableStyle = db.Tablestyle,
                Position = tables.Detail != null ? GetNextTablePosition(tables.Detail) : Point3d.Origin
            };
            ApplyTableXData(table, OverrideRole, _settings);
            ms.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            _overrideTableId = table.ObjectId;
            return table;
        }

        private static void UpdateBuildingInfoLabels(Database db, Transaction tr, List<RowOutput> outputs, LandIndexSettings settings)
        {
            if (settings == null || !settings.ShowBuildingInfoLabels)
            {
                DeleteExistingInfoLabels(db, tr);
                return;
            }

            EnsureInfoLabelLayer(db, tr);
            EnsureRegAppRecord(db, tr);
            var existingLabels = GetExistingInfoLabels(db, tr);
            var activeNumbers = new HashSet<string>(outputs.Select(x => x.Number ?? ""), StringComparer.OrdinalIgnoreCase);
            foreach (var pair in existingLabels.ToList())
            {
                if (!activeNumbers.Contains(pair.Key))
                {
                    foreach (var existingLabel in pair.Value)
                    {
                        existingLabel.UpgradeOpen();
                        existingLabel.Erase();
                    }
                    existingLabels.Remove(pair.Key);
                }
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            foreach (var output in outputs.Where(x => x.HasInfoLabelPoint))
            {
                var contents =
                    "基底面积" + FormatArea(output.BaseArea) + "㎡" +
                    "\\P计容面积" + FormatArea(output.CapacityArea) + "㎡" +
                    "\\P用地面积" + FormatArea(output.LandArea) + "㎡" +
                    "\\P" + FormatNumber(output.Mu) + "亩" +
                    "\\P建筑密度" + FormatPercent(output.Density);
                if (existingLabels.TryGetValue(output.Number ?? "", out var existing) && existing.Count > 0)
                {
                    foreach (var existingLabel in existing)
                    {
                        var location = existingLabel.Location;
                        existingLabel.UpgradeOpen();
                        existingLabel.Contents = contents;
                        existingLabel.TextHeight = GetInfoLabelTextHeight(settings);
                        existingLabel.Layer = InfoLabelLayer;
                        existingLabel.Location = location;
                    }
                    continue;
                }

                var newLabel = new MText
                {
                    Layer = InfoLabelLayer,
                    Location = output.InfoLabelPoint,
                    Attachment = AttachmentPoint.MiddleCenter,
                    TextHeight = GetInfoLabelTextHeight(settings),
                    Contents = contents
                };
                newLabel.SetDatabaseDefaults(db);
                newLabel.Layer = InfoLabelLayer;
                newLabel.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, InfoLabelMarker),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, output.Number ?? ""));
                ms.AppendEntity(newLabel);
                tr.AddNewlyCreatedDBObject(newLabel, true);
            }
        }

        private static Dictionary<string, List<MText>> GetExistingInfoLabels(Database db, Transaction tr)
        {
            var result = new Dictionary<string, List<MText>>(StringComparer.OrdinalIgnoreCase);
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                var label = tr.GetObject(id, OpenMode.ForRead, false) as MText;
                if (label == null || !IsInfoLabel(label))
                {
                    continue;
                }

                var number = GetInfoLabelNumber(label);
                if (!result.TryGetValue(number, out var labels))
                {
                    labels = new List<MText>();
                    result[number] = labels;
                }

                labels.Add(label);
            }

            return result;
        }

        private static string GetInfoLabelNumber(Entity entity)
        {
            var data = entity.XData;
            if (data == null)
            {
                return "";
            }

            var markerSeen = false;
            foreach (TypedValue value in data)
            {
                if (value.TypeCode != (int)DxfCode.ExtendedDataAsciiString)
                {
                    continue;
                }

                var text = value.Value as string;
                if (markerSeen)
                {
                    return text ?? "";
                }

                if (string.Equals(text, InfoLabelMarker, StringComparison.Ordinal))
                {
                    markerSeen = true;
                }
            }

            return "";
        }

        private static void DeleteExistingInfoLabels(Database db, Transaction tr)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            var ids = ms.Cast<ObjectId>().ToList();
            foreach (var id in ids)
            {
                var entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || !IsInfoLabel(entity))
                {
                    continue;
                }

                entity.UpgradeOpen();
                entity.Erase();
            }
        }

        private static bool IsInfoLabel(Entity entity)
        {
            var data = entity.XData;
            if (data == null)
            {
                return false;
            }

            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                    string.Equals(value.Value as string, InfoLabelMarker, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureInfoLabelLayer(Database db, Transaction tr)
        {
            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (layerTable.Has(InfoLabelLayer))
            {
                return;
            }

            layerTable.UpgradeOpen();
            var layer = new LayerTableRecord
            {
                Name = InfoLabelLayer,
                Color = Color.FromColorIndex(ColorMethod.ByAci, 2)
            };
            layerTable.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);
        }

        private static void EnsureRegAppRecord(Database db, Transaction tr)
        {
            var table = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName))
            {
                return;
            }

            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            tr.AddNewlyCreatedDBObject(record, true);
        }

        private static double GetInfoLabelTextHeight(LandIndexSettings settings)
        {
            return settings != null && settings.InfoLabelTextHeight > 0
                ? settings.InfoLabelTextHeight
                : 3.0;
        }

        private static List<ParcelRecord> ScanDrawing(Database db, Transaction tr)
        {
            var parcels = new List<ParcelRecord>();
            var labels = new List<TextRecord>();
            var buildings = new List<Polyline>();
            var bikeAreas = new List<Polyline>();
            var greenAreas = new List<WeightedPolyline>();
            var floorTexts = new List<TextRecord>();

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                var entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null)
                {
                    continue;
                }

                if (!IsEntityInScope(entity))
                {
                    continue;
                }

                if (IsLayer(entity, _parcelLayer))
                {
                    var yd = entity as Polyline;
                    if (yd != null && yd.Closed)
                    {
                        parcels.Add(new ParcelRecord { Boundary = yd, LandArea = yd.Area * _areaFactor });
                    }
                }
                else if (IsLayer(entity, _labelLayer))
                {
                    var text = TryReadText(entity);
                    if (text != null && IsLikelyParcelLabel(text.Text))
                    {
                        labels.Add(text);
                    }
                }
                else if (IsLayer(entity, _buildingLayer))
                {
                    var jz = entity as Polyline;
                    if (jz != null && jz.Closed)
                    {
                        buildings.Add(jz);
                    }
                }
                else if (IsLayer(entity, _bikeLayer))
                {
                    var fjdc = entity as Polyline;
                    if (fjdc != null && fjdc.Closed)
                    {
                        bikeAreas.Add(fjdc);
                    }
                }
                else if (IsLayer(entity, _greenLayer))
                {
                    var ld = entity as Polyline;
                    if (ld != null && ld.Closed)
                    {
                        greenAreas.Add(new WeightedPolyline { Boundary = ld, Factor = 1.0 });
                    }
                }
                else if (IsGreenVariantLayer(entity, _grassPaverLayer, "zcz"))
                {
                    var zcz = entity as Polyline;
                    if (zcz != null && zcz.Closed)
                    {
                        greenAreas.Add(new WeightedPolyline { Boundary = zcz, Factor = _settings.GrassPaverFactor });
                    }
                }
                else if (IsGreenVariantLayer(entity, _roofGreen1Layer, "wm1"))
                {
                    var wm1 = entity as Polyline;
                    if (wm1 != null && wm1.Closed)
                    {
                        greenAreas.Add(new WeightedPolyline { Boundary = wm1, Factor = _settings.RoofGreen1Factor });
                    }
                }
                else if (IsGreenVariantLayer(entity, _roofGreen2Layer, "wm2"))
                {
                    var wm2 = entity as Polyline;
                    if (wm2 != null && wm2.Closed)
                    {
                        greenAreas.Add(new WeightedPolyline { Boundary = wm2, Factor = _settings.RoofGreen2Factor });
                    }
                }
                else if (IsGreenVariantLayer(entity, _roofGreen3Layer, "wm3"))
                {
                    var wm3 = entity as Polyline;
                    if (wm3 != null && wm3.Closed)
                    {
                        greenAreas.Add(new WeightedPolyline { Boundary = wm3, Factor = _settings.RoofGreen3Factor });
                    }
                }
                else if (IsLayer(entity, _floorLayer))
                {
                    var text = TryReadText(entity);
                    if (text != null)
                    {
                        floorTexts.Add(text);
                    }
                }
            }

            foreach (var parcel in parcels)
            {
                var label = labels.FirstOrDefault(x => ContainsPoint(parcel.Boundary, x.Position));
                parcel.Number = label == null ? "" : NormalizeNumber(label.Text);
            }

            foreach (var building in buildings)
            {
                var point = GetEntityCenter(building);
                var parcel = FindParcelForPolyline(parcels, building);
                if (parcel != null)
                {
                    var area = building.Area * _areaFactor;
                    parcel.BaseArea += area;
                    if (area > parcel.InfoLabelBuildingArea)
                    {
                        parcel.InfoLabelBuildingArea = area;
                        parcel.InfoLabelPoint = point;
                        parcel.HasInfoLabelPoint = true;
                    }
                }
            }

            foreach (var bikeArea in bikeAreas)
            {
                var parcel = FindParcelForPolyline(parcels, bikeArea);
                if (parcel != null)
                {
                    parcel.BikeArea += bikeArea.Area * _areaFactor;
                }
            }

            foreach (var greenArea in greenAreas)
            {
                var parcel = FindParcelForPolyline(parcels, greenArea.Boundary);
                if (parcel != null)
                {
                    parcel.GreenArea += greenArea.Boundary.Area * _areaFactor * greenArea.Factor;
                }
            }

            foreach (var text in floorTexts)
            {
                var parcel = parcels.FirstOrDefault(x => ContainsPoint(x.Boundary, text.Position));
                if (parcel != null)
                {
                    var floors = ParseFirstPositiveNumber(text.Text);
                    if (floors > 0)
                    {
                        parcel.FloorsFromDrawing = floors;
                    }
                }
            }

            return parcels
                .Where(x => !string.IsNullOrWhiteSpace(x.Number))
                .OrderBy(x => ParseSortNumber(x.Number))
                .ThenBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<RowOutput> BuildOutputs(List<ParcelRecord> records, Dictionary<string, RowInput> inputs, LandIndexSettings settings)
        {
            settings = settings ?? _settings;
            var outputs = new List<RowOutput>();
            foreach (var record in records)
            {
                var input = GetRowInput(inputs, record.Number);
                settings.DetailOverrides.TryGetValue(record.Number, out var detailOverride);
                var baseArea = detailOverride != null && detailOverride.BaseAreaManual
                    ? detailOverride.BaseArea
                    : record.BaseArea;
                var landArea = detailOverride != null && detailOverride.LandAreaManual
                    ? detailOverride.LandArea
                    : record.LandArea;
                var greenArea = detailOverride != null && detailOverride.GreenAreaManual
                    ? detailOverride.GreenArea
                    : record.GreenArea;
                var bikeArea = detailOverride != null && detailOverride.BikeAreaManual
                    ? detailOverride.BikeArea
                    : record.BikeArea;
                var floors = record.FloorsFromDrawing > 0 ? record.FloorsFromDrawing : input.Floors;
                if (detailOverride != null && detailOverride.Floors > 0 && record.FloorsFromDrawing <= 0)
                {
                    floors = detailOverride.Floors;
                }

                var capacityArea = baseArea * floors;
                if (detailOverride != null && detailOverride.CapacityAreaManual)
                {
                    capacityArea = detailOverride.CapacityArea;
                }

                var nonCapacityArea = detailOverride != null
                    ? detailOverride.NonCapacityArea
                    : input.NonCapacityArea;
                var buildingArea = capacityArea + nonCapacityArea;
                var bikeCalcArea = capacityArea;
                var motorCalcArea = capacityArea;
                if (settings.AreaOverrides.TryGetValue(record.Number, out var areaOverride))
                {
                    if (areaOverride.BikeCalcArea.HasValue)
                    {
                        bikeCalcArea = areaOverride.BikeCalcArea.Value;
                    }

                    if (areaOverride.MotorCalcArea.HasValue)
                    {
                        motorCalcArea = areaOverride.MotorCalcArea.Value;
                    }
                }

                var requiredParking = Math.Ceiling(motorCalcArea / 100.0 * settings.MotorSpacesPer100 - Tolerance);
                if (requiredParking < 0)
                {
                    requiredParking = 0;
                }

                var groundSource = detailOverride != null ? detailOverride.GroundParking : input.GroundParking;
                var indoorSource = detailOverride != null ? detailOverride.IndoorParking : input.IndoorParking;
                var groundParking = Math.Ceiling(groundSource - Tolerance);
                var indoorParking = Math.Ceiling(indoorSource - Tolerance);
                if (groundParking < 0)
                {
                    groundParking = 0;
                }

                if (indoorParking < 0)
                {
                    indoorParking = 0;
                }

                var parking = groundParking + indoorParking;
                var requiredGreenArea = landArea * settings.GreenRateMin;
                var maxGreenArea = landArea * settings.GreenRateMax;
                var requiredBikeArea = bikeCalcArea / 100.0 * settings.BikeSpacesPer100 * settings.BikeAreaPerSpace;
                var greenRate = landArea > 0 ? greenArea / landArea : 0;
                outputs.Add(new RowOutput
                {
                    Type = detailOverride != null ? detailOverride.Type ?? "" : input.Type,
                    Number = record.Number,
                    BaseArea = baseArea,
                    CapacityArea = capacityArea,
                    NonCapacityArea = nonCapacityArea,
                    BuildingArea = buildingArea,
                    Floors = floors,
                    LandArea = landArea,
                    Mu = landArea / 666.66,
                    Density = landArea > 0 ? baseArea / landArea : 0,
                    GreenRate = greenRate,
                    GreenArea = greenArea,
                    RequiredGreenArea = requiredGreenArea,
                    MaxGreenArea = maxGreenArea,
                    GreenAreaOk = greenArea + Tolerance >= requiredGreenArea &&
                                  (settings.GreenRateMax <= 0 || greenArea <= maxGreenArea + Tolerance),
                    BikeArea = bikeArea,
                    BikeCalcArea = bikeCalcArea,
                    RequiredBikeArea = requiredBikeArea,
                    BikeAreaOk = bikeArea + Tolerance >= requiredBikeArea,
                    MotorCalcArea = motorCalcArea,
                    Parking = parking,
                    RequiredParking = requiredParking,
                    ParkingOk = parking + Tolerance >= requiredParking,
                    Far = landArea > 0 ? capacityArea / landArea : 0,
                    GroundParking = groundParking,
                    IndoorParking = indoorParking,
                    InfoLabelPoint = record.InfoLabelPoint,
                    HasInfoLabelPoint = record.HasInfoLabelPoint
                });
            }

            return outputs;
        }

        private static void FillSummaryTable(Table table, List<RowOutput> outputs, LandIndexSettings settings)
        {
            settings = settings ?? _settings;
            var buildingSubItems = BuildSummarySubItems(
                "总建筑面积",
                outputs.Where(x => !string.IsNullOrWhiteSpace(x.Type))
                    .GroupBy(x => x.Type.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.BuildingArea), StringComparer.OrdinalIgnoreCase),
                settings);
            var capacitySubItems = BuildSummarySubItems(
                "计容总建筑面积",
                outputs.Where(x => !string.IsNullOrWhiteSpace(x.Type))
                    .GroupBy(x => x.Type.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.CapacityArea), StringComparer.OrdinalIgnoreCase),
                settings);

            var totalLand = outputs.Sum(x => x.LandArea);
            var totalBase = outputs.Sum(x => x.BaseArea);
            var totalCapacity = capacitySubItems.Count > 0 ? capacitySubItems.Sum(x => x.Value) : outputs.Sum(x => x.CapacityArea);
            var totalBuilding = buildingSubItems.Count > 0 ? buildingSubItems.Sum(x => x.Value) : outputs.Sum(x => x.BuildingArea);
            var totalParking = outputs.Sum(x => x.Parking);
            var totalGroundParking = outputs.Sum(x => x.GroundParking);
            var totalGreenArea = outputs.Sum(x => x.GreenArea);
            var totalBikeArea = outputs.Sum(x => x.RequiredBikeArea);

            var lines = new List<SummaryLine>();
            lines.Add(CreateSummaryLine("", "规划总用地面积", "", "㎡", totalLand, "", settings));
            lines.Add(CreateSummaryLine("", "规划净用地面积", "", "㎡", totalLand, "", settings));
            lines.Add(CreateSummaryLine("", "总建筑面积", "", "㎡", totalBuilding, "", settings, false));
            var buildingStart = 2 + lines.Count;
            lines.AddRange(buildingSubItems);
            var buildingEnd = 2 + lines.Count - 1;
            lines.Add(CreateSummaryLine("", "计容总建筑面积", "", "㎡", totalCapacity, "", settings, false));
            var capacityStart = 2 + lines.Count;
            lines.AddRange(capacitySubItems);
            var capacityEnd = 2 + lines.Count - 1;
            lines.Add(CreateSummaryLine("", "容积率", "", "", totalLand > 0 ? totalCapacity / totalLand : 0, "", settings, true, false));
            lines.Add(CreateSummaryLine("", "建筑基底总面积", "", "㎡", totalBase, "", settings));
            lines.Add(CreateSummaryLine("", "建筑密度", "", "%", totalLand > 0 ? totalBase / totalLand * 100.0 : 0, "", settings, true, false));
            lines.Add(CreateSummaryLine("", "总绿化面积", "", "㎡", totalGreenArea, "", settings));
            lines.Add(CreateSummaryLine("", "绿地率", "", "%", totalLand > 0 ? totalGreenArea / totalLand * 100.0 : settings.GreenRateMin * 100.0, "", settings, true, false));
            lines.Add(CreateSummaryTextLine("", "最大层数（±0.00计）", "", "层", "", "", settings));
            lines.Add(CreateSummaryTextLine("", "最高建筑总高度（±0.00计）", "", "m", "", "", settings));
            lines.Add(CreateSummaryTextLine("", "机动车停车位数", "", "个", FormatInteger(totalParking), FormatNumber(settings.MotorSpacesPer100) + "个/100㎡", settings));
            lines.Add(CreateSummaryTextLine("", "其中", "地面车位", "个", FormatInteger(totalGroundParking), "", settings));
            lines.Add(CreateSummaryTextLine("", "", "室内车位", "个", FormatInteger(totalParking - totalGroundParking), "", settings));
            lines.Add(CreateSummaryLine("", "非机动车停车数", "", "㎡", totalBikeArea, "", settings));
            lines.Add(CreateSummaryTextLine("", "配套占总计容面积比例", "", "%", "/", "", settings));
            lines.Add(CreateSummaryTextLine("", "配套占总用地面积比例", "", "%", "/", "", settings));

            table.SetSize(2 + lines.Count, SummaryColumnCount);
            var tableTextHeight = GetTableTextHeight(settings.SummaryTableTextHeight);
            ApplyTableRowHeights(table, tableTextHeight);
            var columnScale = GetTableScale(tableTextHeight);
            table.Columns[0].Width = ScaleWidth(54.0, columnScale);
            table.Columns[1].Width = ScaleWidth(24.0, columnScale);
            table.Columns[2].Width = ScaleWidth(22.0, columnScale);
            table.Columns[3].Width = ScaleWidth(30.0, columnScale);
            table.Columns[4].Width = ScaleWidth(48.0, columnScale);

            ClearTable(table);
            table.Cells[0, 0].TextString = "总指标表";
            SetRow(table, 1, "项目", "", "计量单位", "数值", "备注");
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                SetRow(table, 2 + i, line.Item, line.SubItem, line.Unit, line.Text, line.Remark);
            }

            ApplyCommonTableStyle(table, tableTextHeight);
            MergeSummaryItemCells(table, buildingStart, buildingEnd);
            MergeSummaryItemCells(table, capacityStart, capacityEnd);
            table.GenerateLayout();
        }

        private static void FillDetailTable(Table table, List<RowOutput> outputs)
        {
            var settings = _settings ?? new LandIndexSettings();
            var tableTextHeight = GetTableTextHeight(settings.DetailTableTextHeight);
            var dataRows = Math.Max(outputs.Count, 1);
            var totalRow = DetailStartRow + dataRows;
            table.SetSize(totalRow + 1, DetailColumnCount);
            ApplyTableRowHeights(table, tableTextHeight);
            var columnScale = GetTableScale(tableTextHeight);
            for (var col = 0; col < DetailColumnCount; col++)
            {
                table.Columns[col].Width = ScaleWidth(GetColumnWidth(col), columnScale);
            }

            ClearTable(table);
            table.Cells[0, 0].TextString = "分地块明细表";
            WriteDetailHeader(table);
            ApplyCommonTableStyle(table, tableTextHeight);

            for (var i = 0; i < outputs.Count; i++)
            {
                var output = outputs[i];
                var row = DetailStartRow + i;
                table.Cells[row, 0].TextString = output.Type;
                table.Cells[row, 1].TextString = output.Number;
                table.Cells[row, 2].TextString = FormatArea(output.BaseArea);
                table.Cells[row, 3].TextString = FormatArea(output.CapacityArea);
                table.Cells[row, 4].TextString = FormatArea(output.BuildingArea);
                table.Cells[row, 5].TextString = FormatArea(output.NonCapacityArea);
                table.Cells[row, 6].TextString = FormatNumber(output.Far);
                table.Cells[row, 7].TextString = FormatNumber(output.Floors);
                table.Cells[row, 8].TextString = FormatArea(output.LandArea);
                table.Cells[row, 9].TextString = FormatNumber(output.Mu);
                table.Cells[row, 10].TextString = FormatPercent(output.Density);
                table.Cells[row, 11].TextString = FormatPercent(output.GreenRate);
                table.Cells[row, 12].TextString = FormatArea(output.GreenArea);
                table.Cells[row, 13].TextString = FormatArea(output.RequiredBikeArea);
                table.Cells[row, 14].TextString = FormatInteger(output.Parking);
                table.Cells[row, 15].TextString = FormatInteger(output.GroundParking);
                table.Cells[row, 16].TextString = FormatInteger(output.IndoorParking);
                if (!output.GreenAreaOk)
                {
                    SetCellRed(table, row, 12);
                }

                if (!output.BikeAreaOk)
                {
                    SetCellRed(table, row, 13);
                }

                if (!output.ParkingOk)
                {
                    SetCellRed(table, row, 14);
                }
            }

            table.Cells[totalRow, 0].TextString = "合计";
            table.Cells[totalRow, 2].TextString = FormatArea(outputs.Sum(x => x.BaseArea));
            table.Cells[totalRow, 3].TextString = FormatArea(outputs.Sum(x => x.CapacityArea));
            table.Cells[totalRow, 4].TextString = FormatArea(outputs.Sum(x => x.BuildingArea));
            table.Cells[totalRow, 5].TextString = FormatArea(outputs.Sum(x => x.NonCapacityArea));
            table.Cells[totalRow, 12].TextString = FormatArea(outputs.Sum(x => x.GreenArea));
            table.Cells[totalRow, 13].TextString = FormatArea(outputs.Sum(x => x.RequiredBikeArea));
            table.Cells[totalRow, 14].TextString = FormatInteger(outputs.Sum(x => x.Parking));
            table.Cells[totalRow, 15].TextString = FormatInteger(outputs.Sum(x => x.GroundParking));
            table.Cells[totalRow, 16].TextString = FormatInteger(outputs.Sum(x => x.IndoorParking));
            table.GenerateLayout();
        }

        private static void FillOverrideTable(Table table, List<RowOutput> outputs, LandIndexSettings settings)
        {
            settings = settings ?? _settings;
            var dataRows = Math.Max(outputs.Count, 1);
            table.SetSize(OverrideStartRow + dataRows, OverrideColumnCount);
            table.SetRowHeight(RowHeight);
            table.Rows[0].Height = TitleRowHeight;
            table.Columns[0].Width = 22.0;
            table.Columns[1].Width = 34.0;
            table.Columns[2].Width = 42.0;
            table.Columns[3].Width = 42.0;

            ClearTable(table);
            table.Cells[0, 0].TextString = "折算面积覆盖表";
            table.Cells[OverrideHeaderRow, 0].TextString = "编号";
            table.Cells[OverrideHeaderRow, 1].TextString = "默认计容面积㎡";
            table.Cells[OverrideHeaderRow, 2].TextString = "非机动车折算面积㎡";
            table.Cells[OverrideHeaderRow, 3].TextString = "机动车折算面积㎡";
            ApplyCommonTableStyle(table);

            for (var i = 0; i < outputs.Count; i++)
            {
                var output = outputs[i];
                var row = OverrideStartRow + i;
                table.Cells[row, 0].TextString = output.Number;
                table.Cells[row, 1].TextString = FormatArea(output.CapacityArea);
                if (settings.AreaOverrides.TryGetValue(output.Number, out var areaOverride))
                {
                    table.Cells[row, 2].TextString = areaOverride.BikeCalcArea.HasValue
                        ? FormatArea(areaOverride.BikeCalcArea.Value)
                        : "";
                    table.Cells[row, 3].TextString = areaOverride.MotorCalcArea.HasValue
                        ? FormatArea(areaOverride.MotorCalcArea.Value)
                        : "";
                }
            }

            table.GenerateLayout();
        }

        private static void ReadAreaOverrides(Table table, LandIndexSettings settings)
        {
            if (table == null || settings == null)
            {
                return;
            }

            settings.AreaOverrides.Clear();
            for (var row = OverrideStartRow; row < table.Rows.Count; row++)
            {
                var number = GetCellText(table, row, 0).Trim();
                if (string.IsNullOrWhiteSpace(number))
                {
                    continue;
                }

                var bike = ParseOptionalPositiveNumber(GetCellText(table, row, 2));
                var motor = ParseOptionalPositiveNumber(GetCellText(table, row, 3));
                if (!bike.HasValue && !motor.HasValue)
                {
                    continue;
                }

                settings.AreaOverrides[number] = new AreaOverride
                {
                    Number = number,
                    BikeCalcArea = bike,
                    MotorCalcArea = motor
                };
            }
        }

        private static void MergeInputsIntoDetailOverrides(Dictionary<string, RowInput> inputs, LandIndexSettings settings)
        {
            if (inputs == null || settings == null)
            {
                return;
            }

            foreach (var pair in inputs)
            {
                if (!settings.DetailOverrides.TryGetValue(pair.Key, out var detail))
                {
                    detail = new DetailOverride { Number = pair.Key };
                    settings.DetailOverrides[pair.Key] = detail;
                }

                detail.Type = pair.Value.Type ?? "";
                detail.Floors = pair.Value.Floors;
                detail.NonCapacityArea = pair.Value.NonCapacityArea;
                detail.GroundParking = pair.Value.GroundParking;
                detail.IndoorParking = pair.Value.IndoorParking;
            }
        }

        private static double? ParseOptionalPositiveNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var value = ParsePositiveNumber(text.Trim(), -1);
            return value >= 0 ? (double?)value : null;
        }

        private static void FillTable(Table table, List<ParcelRecord> records, Dictionary<string, RowInput> inputs)
        {
            var detailRows = Math.Max(records.Count, 1);
            var totalRow = DetailStartRow + detailRows;
            table.SetSize(totalRow + 1, ColumnCount);
            table.SetRowHeight(3.5);
            for (var col = 0; col < ColumnCount; col++)
            {
                table.Columns[col].Width = GetColumnWidth(col);
            }

            ClearTable(table);
            WriteSummaryLabels(table);
            WriteDetailHeader(table);

            var totalLand = records.Sum(x => x.LandArea);
            var totalBase = 0.0;
            var totalCapacity = 0.0;
            var totalGroundParking = 0.0;

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var row = DetailStartRow + i;
                var input = GetRowInput(inputs, record.Number);
                var capacityArea = record.BaseArea * input.Floors;
                var mu = record.LandArea / 666.66;
                var density = record.LandArea > 0 ? record.BaseArea / record.LandArea : 0;
                var greenArea = input.GreenRate * record.LandArea;
                var bikeArea = capacityArea / 100.0 * 1.5;
                var parking = capacityArea / 100.0 * 0.3;
                var far = record.LandArea > 0 ? capacityArea / record.LandArea : 0;
                var indoorParking = parking - input.GroundParking;

                totalBase += record.BaseArea;
                totalCapacity += capacityArea;
                totalGroundParking += input.GroundParking;

                table.Cells[row, 0].TextString = input.Type;
                table.Cells[row, 1].TextString = record.Number;
                table.Cells[row, 2].TextString = FormatArea(record.BaseArea);
                table.Cells[row, 3].TextString = FormatArea(capacityArea);
                table.Cells[row, 4].TextString = FormatNumber(input.Floors);
                table.Cells[row, 5].TextString = FormatArea(record.LandArea);
                table.Cells[row, 6].TextString = FormatNumber(mu);
                table.Cells[row, 7].TextString = FormatPercent(density);
                table.Cells[row, 8].TextString = FormatPercent(input.GreenRate);
                table.Cells[row, 9].TextString = FormatArea(greenArea);
                table.Cells[row, 10].TextString = FormatArea(bikeArea);
                table.Cells[row, 11].TextString = FormatNumber(parking);
                table.Cells[row, 12].TextString = FormatNumber(far);
                table.Cells[row, 13].TextString = FormatNumber(input.GroundParking);
                table.Cells[row, 14].TextString = FormatNumber(indoorParking);
            }

            var totalGreenRate = ParsePositiveNumber(GetCellText(table, 14, 3), 0.1);
            var totalParking = totalCapacity / 100.0 * 0.3;
            table.Cells[2, 3].TextString = FormatArea(totalLand);
            table.Cells[3, 3].TextString = FormatArea(totalLand);
            table.Cells[4, 3].TextString = FormatArea(totalCapacity);
            table.Cells[7, 3].TextString = FormatArea(totalCapacity);
            table.Cells[10, 3].TextString = FormatNumber(totalLand > 0 ? totalCapacity / totalLand : 0);
            table.Cells[11, 3].TextString = FormatArea(totalBase);
            table.Cells[12, 3].TextString = FormatPercent(totalLand > 0 ? totalBase / totalLand : 0);
            table.Cells[13, 3].TextString = FormatArea(totalGreenRate * totalLand);
            table.Cells[14, 3].TextString = FormatPercent(totalGreenRate);
            table.Cells[17, 3].TextString = FormatNumber(totalParking);
            table.Cells[17, 4].TextString = "0.3个/100平方米";
            table.Cells[18, 3].TextString = FormatNumber(totalGroundParking);
            table.Cells[19, 3].TextString = FormatNumber(totalParking - totalGroundParking);
            table.Cells[20, 3].TextString = FormatArea(totalCapacity / 100.0 * 1.5);
            table.Cells[21, 3].TextString = "/";
            table.Cells[22, 3].TextString = "/";

            table.Cells[totalRow, 0].TextString = "合计";
            table.Cells[totalRow, 2].TextString = FormatArea(totalBase);
            table.Cells[totalRow, 3].TextString = FormatArea(totalCapacity);
            table.GenerateLayout();
        }

        private static void WriteSummaryLabels(Table table)
        {
            table.Cells[0, 0].TextString = "总指标表";
            SetRow(table, 1, "项目", "", "计量单位", "数值", "备注");
            SetRow(table, 2, "规划总用地面积", "", "㎡", "", "");
            SetRow(table, 3, "规划净用地面积", "", "㎡", "", "");
            SetRow(table, 4, "总建筑面积", "", "㎡", "", "");
            SetRow(table, 5, "其中", "厂房", "㎡", "", "");
            SetRow(table, 6, "", "", "㎡", "", "");
            SetRow(table, 7, "计容总建筑面积", "", "㎡", "", "");
            SetRow(table, 8, "其中", "厂房", "㎡", "", "");
            SetRow(table, 9, "", "", "㎡", "", "");
            SetRow(table, 10, "容积率", "", "", "", "");
            SetRow(table, 11, "建筑基底总面积", "", "㎡", "", "");
            SetRow(table, 12, "建筑密度", "", "%", "", "");
            SetRow(table, 13, "总绿化面积", "", "㎡", "", "");
            SetRow(table, 14, "绿地率", "", "%", "", "");
            SetRow(table, 15, "最大层数（±0.00计）", "", "层", "", "");
            SetRow(table, 16, "最高建筑总高度（±0.00计）", "", "m", "", "");
            SetRow(table, 17, "机动车停车位数", "", "个", "", "0.3个/100平方米");
            SetRow(table, 18, "其中", "地面车位", "个", "", "");
            SetRow(table, 19, "", "室内车位", "个", "", "");
            SetRow(table, 20, "非机动车停车数", "", "㎡", "", "");
            SetRow(table, 21, "配套占总计容面积比例", "", "%", "/", "");
            SetRow(table, 22, "配套占总用地面积比例", "", "%", "/", "");
        }

        private static void WriteDetailHeader(Table table)
        {
            var headers = new[]
            {
                "类型", "编号", "基底面积", "计容面积", "建筑面积", "不计容面积", "容积率", "层数", "用地面积", "亩",
                "密度", "绿地率", "绿地㎡", "非机动车㎡", "机动车（个）", "地面", "室内"
            };
            for (var col = 0; col < headers.Length; col++)
            {
                table.Cells[DetailHeaderRow, col].TextString = headers[col];
            }
        }

        private static void SetRow(Table table, int row, params string[] values)
        {
            for (var col = 0; col < values.Length && col < table.Columns.Count; col++)
            {
                table.Cells[row, col].TextString = values[col];
            }
        }

        private static List<SummaryLine> BuildSummarySubItems(string parent, Dictionary<string, double> automaticValues, LandIndexSettings settings)
        {
            var result = new Dictionary<string, SummaryLine>(StringComparer.OrdinalIgnoreCase);
            var activeTypes = new HashSet<string>(automaticValues.Keys, StringComparer.OrdinalIgnoreCase);
            var staleKeys = new List<string>();
            if (settings != null)
            {
                foreach (var item in settings.SummaryOverrides.Values.Where(x => string.Equals(x.Parent, parent, StringComparison.OrdinalIgnoreCase)))
                {
                    var subItem = string.IsNullOrWhiteSpace(item.SubItem) ? item.Key : item.SubItem.Trim();
                    if (string.IsNullOrWhiteSpace(subItem))
                    {
                        continue;
                    }

                    if (!activeTypes.Contains(subItem) && !item.Manual)
                    {
                        staleKeys.Add(item.Key);
                        continue;
                    }

                    result[subItem] = new SummaryLine
                    {
                        Key = item.Key,
                        Parent = parent,
                        Item = string.IsNullOrWhiteSpace(item.Item) ? "其中" : item.Item,
                        SubItem = subItem,
                        Unit = string.IsNullOrWhiteSpace(item.Unit) ? "㎡" : item.Unit,
                        Value = item.Value,
                        Text = FormatArea(item.Value),
                        Remark = item.Remark ?? "",
                        Manual = item.Manual,
                        Custom = item.Custom
                    };
                }

                foreach (var key in staleKeys)
                {
                    settings.SummaryOverrides.Remove(key);
                }
            }

            foreach (var pair in automaticValues)
            {
                if (!result.TryGetValue(pair.Key, out var line))
                {
                    result[pair.Key] = new SummaryLine
                    {
                        Key = SummaryKey(parent, "其中", pair.Key),
                        Parent = parent,
                        Item = "其中",
                        SubItem = pair.Key,
                        Unit = "㎡",
                        Value = pair.Value,
                        Text = FormatArea(pair.Value),
                        Remark = ""
                    };
                    continue;
                }

                if (!line.Manual)
                {
                    line.Value = pair.Value;
                    line.Text = FormatArea(pair.Value);
                }
            }

            foreach (var line in result.Values)
            {
                if (line.Manual)
                {
                    line.Text = FormatArea(line.Value);
                }
            }

            return result.Values.OrderBy(x => x.SubItem, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static SummaryLine CreateSummaryLine(string parent, string item, string subItem, string unit, double value, string remark, LandIndexSettings settings, bool allowManual = true, bool areaFormat = true)
        {
            var key = SummaryKey(parent, item, subItem);
            var line = new SummaryLine
            {
                Key = key,
                Parent = parent,
                Item = item,
                SubItem = subItem,
                Unit = unit,
                Value = value,
                Text = areaFormat ? FormatArea(value) : FormatNumber(value),
                Remark = remark
            };
            ApplySummaryOverride(line, settings, allowManual, areaFormat);
            return line;
        }

        private static SummaryLine CreateSummaryTextLine(string parent, string item, string subItem, string unit, string text, string remark, LandIndexSettings settings)
        {
            var key = SummaryKey(parent, item, subItem);
            var line = new SummaryLine
            {
                Key = key,
                Parent = parent,
                Item = item,
                SubItem = subItem,
                Unit = unit,
                Text = text,
                Remark = remark
            };
            ApplySummaryOverride(line, settings, true, false);
            return line;
        }

        private static void ApplySummaryOverride(SummaryLine line, LandIndexSettings settings, bool allowManual, bool areaFormat)
        {
            if (settings == null || line == null || !settings.SummaryOverrides.TryGetValue(line.Key, out var summaryOverride))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(summaryOverride.Item))
            {
                line.Item = summaryOverride.Item;
            }

            if (!string.IsNullOrWhiteSpace(summaryOverride.SubItem))
            {
                line.SubItem = summaryOverride.SubItem;
            }

            if (!string.IsNullOrWhiteSpace(summaryOverride.Unit))
            {
                line.Unit = summaryOverride.Unit;
            }

            if (allowManual && summaryOverride.Manual)
            {
                line.Value = summaryOverride.Value;
                line.Text = areaFormat ? FormatArea(summaryOverride.Value) : FormatNumber(summaryOverride.Value);
            }

            if (!string.IsNullOrWhiteSpace(summaryOverride.Remark))
            {
                line.Remark = summaryOverride.Remark;
            }
        }

        private static void SetSummaryNumberRow(Table table, int row, string item, string subItem, string unit, double value, string remark, LandIndexSettings settings, string keyItem = null)
        {
            var key = SummaryKey("", keyItem ?? item, subItem);
            var text = FormatArea(value);
            if (settings != null && settings.SummaryOverrides.TryGetValue(key, out var summaryOverride))
            {
                if (summaryOverride.Manual)
                {
                    text = FormatArea(summaryOverride.Value);
                }

                if (!string.IsNullOrWhiteSpace(summaryOverride.Remark))
                {
                    remark = summaryOverride.Remark;
                }
            }

            SetRow(table, row, item, subItem, unit, text, remark);
        }

        private static void SetSummaryTextRow(Table table, int row, string item, string subItem, string unit, string value, string remark, LandIndexSettings settings, string keyItem = null)
        {
            var key = SummaryKey("", keyItem ?? item, subItem);
            if (settings != null && settings.SummaryOverrides.TryGetValue(key, out var summaryOverride))
            {
                if (summaryOverride.Manual)
                {
                    value = FormatNumber(summaryOverride.Value);
                }

                if (!string.IsNullOrWhiteSpace(summaryOverride.Remark))
                {
                    remark = summaryOverride.Remark;
                }
            }

            SetRow(table, row, item, subItem, unit, value, remark);
        }

        private static string SummaryKey(string parent, string item, string subItem)
        {
            return (parent ?? "").Trim() + "||" + (item ?? "").Trim() + "||" + (subItem ?? "").Trim();
        }

        private static void MergeSummaryItemCells(Table table, int startRow, int endRow)
        {
            if (startRow < 0 || endRow <= startRow || endRow >= table.Rows.Count)
            {
                return;
            }

            try
            {
                table.MergeCells(CellRange.Create(table, startRow, 0, endRow, 0));
            }
            catch
            {
                // Cell merging is cosmetic; keep the table usable if this AutoCAD build rejects the range.
            }
        }

        private static void ClearTable(Table table)
        {
            for (var row = 0; row < table.Rows.Count; row++)
            {
                for (var col = 0; col < table.Columns.Count; col++)
                {
                    table.Cells[row, col].TextString = "";
                }
            }
        }

        private static Dictionary<string, RowInput> ReadInputs(Table table)
        {
            var result = new Dictionary<string, RowInput>(StringComparer.OrdinalIgnoreCase);
            if (table.Rows.Count <= DetailStartRow)
            {
                return result;
            }

            var hasBuildingColumns = table.Columns.Count > 16 && GetCellText(table, DetailHeaderRow, 6).Contains("容积率");
            var reorderedColumns = !hasBuildingColumns && table.Columns.Count > 14 && GetCellText(table, DetailHeaderRow, 4).Contains("容积率");
            var floorsColumn = hasBuildingColumns ? 7 : (reorderedColumns ? 5 : 4);
            var greenRateColumn = hasBuildingColumns ? 11 : (reorderedColumns ? 9 : 8);
            var nonCapacityColumn = hasBuildingColumns ? 5 : -1;
            var groundColumn = hasBuildingColumns ? 15 : 13;
            var indoorColumn = hasBuildingColumns ? 16 : 14;
            for (var row = DetailStartRow; row < table.Rows.Count; row++)
            {
                var number = GetCellText(table, row, 1).Trim();
                if (string.IsNullOrWhiteSpace(number) || string.Equals(GetCellText(table, row, 0), "合计", StringComparison.Ordinal))
                {
                    continue;
                }

                result[number] = new RowInput
                {
                    Type = GetCellText(table, row, 0),
                    Floors = ParsePositiveNumber(GetCellText(table, row, floorsColumn), 1.0),
                    GreenRate = ParsePercent(GetCellText(table, row, greenRateColumn), 0.1),
                    NonCapacityArea = nonCapacityColumn >= 0 ? ParsePositiveNumber(GetCellText(table, row, nonCapacityColumn), 0.0) : 0.0,
                    GroundParking = ParsePositiveNumber(GetCellText(table, row, groundColumn), 0.0),
                    IndoorParking = ParsePositiveNumber(GetCellText(table, row, indoorColumn), 0.0)
                };
            }

            return result;
        }

        private static RowInput GetRowInput(Dictionary<string, RowInput> inputs, string number)
        {
            if (inputs.TryGetValue(number, out var input))
            {
                return input;
            }

            return new RowInput
            {
                Type = "",
                Floors = 1.0,
                GreenRate = 0.1,
                GroundParking = 0.0,
                IndoorParking = 0.0,
                NonCapacityArea = 0.0
            };
        }

        private static TablePair GetTables(Database db, Transaction tr, OpenMode mode)
        {
            var result = new TablePair();
            if (!_summaryTableId.IsNull && !_summaryTableId.IsErased)
            {
                result.Summary = tr.GetObject(_summaryTableId, mode, false) as Table;
            }

            if (!_detailTableId.IsNull && !_detailTableId.IsErased)
            {
                result.Detail = tr.GetObject(_detailTableId, mode, false) as Table;
            }

            if (!_overrideTableId.IsNull && !_overrideTableId.IsErased)
            {
                result.Override = tr.GetObject(_overrideTableId, mode, false) as Table;
            }

            if (result.Summary != null && result.Detail != null && result.Override != null)
            {
                return result;
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                var table = tr.GetObject(id, mode, false) as Table;
                if (table == null || !IsLandIndexTable(table))
                {
                    continue;
                }

                var role = GetTableRole(table);
                if (role == SummaryRole)
                {
                    result.Summary = table;
                    _summaryTableId = table.ObjectId;
                }
                else if (role == DetailRole)
                {
                    result.Detail = table;
                    _detailTableId = table.ObjectId;
                }
                else if (role == OverrideRole)
                {
                    result.Override = table;
                    _overrideTableId = table.ObjectId;
                }
            }

            return result;
        }

        private static bool IsLandIndexTable(Table table)
        {
            var data = table.XData;
            if (data == null)
            {
                return false;
            }

            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                    string.Equals(value.Value as string, TableMarker, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetTableRole(Table table)
        {
            var data = table.XData;
            if (data == null)
            {
                return "";
            }

            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                    value.Value is string text &&
                    (text == SummaryRole || text == DetailRole || text == OverrideRole))
                {
                    return text;
                }
            }

            return "";
        }

        private static void ApplyTableXData(Table table, string role, LandIndexSettings settings)
        {
            settings = settings ?? _settings;
            var values = new List<TypedValue>
            {
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, TableMarker),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, role),
                new TypedValue((int)DxfCode.ExtendedDataReal, settings.AreaFactor)
            };
            values.AddRange(settings.ToKeyValueStrings()
                .Select(x => new TypedValue((int)DxfCode.ExtendedDataAsciiString, x)));
            table.XData = new ResultBuffer(values.ToArray());
        }

        private static void ReadTableSettings(Table table)
        {
            var data = table.XData;
            if (data == null)
            {
                return;
            }

            var strings = new List<string>();
            var realValues = new List<double>();
            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataReal && value.Value is double factor && factor > 0)
                {
                    realValues.Add(factor);
                }
                else if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString && value.Value is string text)
                {
                    strings.Add(text);
                }
            }

            var settings = new LandIndexSettings();
            settings.ApplyKeyValueStrings(strings);
            if (!strings.Any(x => x.Contains("=")) && realValues.Count > 0)
            {
                settings.UnitMode = Math.Abs(realValues[0] - 1.0) < 1e-9 ? "M" : "MM";
            }

            var legacyLayerStrings = strings
                .Where(x => !string.Equals(x, TableMarker, StringComparison.Ordinal))
                .Where(x => !string.Equals(x, SummaryRole, StringComparison.Ordinal))
                .Where(x => !string.Equals(x, DetailRole, StringComparison.Ordinal))
                .Where(x => !string.Equals(x, OverrideRole, StringComparison.Ordinal))
                .Where(x => !x.Contains("="))
                .ToList();
            if (legacyLayerStrings.Count >= 3)
            {
                settings.ParcelLayer = legacyLayerStrings[0];
                settings.LabelLayer = legacyLayerStrings[1];
                settings.BuildingLayer = legacyLayerStrings[2];
            }

            ApplySettings(settings);
        }

        private static void EnsureRegApp(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                if (!table.Has(RegAppName))
                {
                    table.UpgradeOpen();
                    var record = new RegAppTableRecord { Name = RegAppName };
                    table.Add(record);
                    tr.AddNewlyCreatedDBObject(record, true);
                }

                tr.Commit();
            }
        }

        private static TextRecord TryReadText(Entity entity)
        {
            var dbText = entity as DBText;
            if (dbText != null)
            {
                return new TextRecord { Text = dbText.TextString, Position = dbText.Position };
            }

            var mText = entity as MText;
            if (mText != null)
            {
                return new TextRecord { Text = mText.Contents, Position = mText.Location };
            }

            return null;
        }

        private static bool ContainsPoint(Polyline boundary, Point3d point)
        {
            var pts = new List<Point2d>();
            for (var i = 0; i < boundary.NumberOfVertices; i++)
            {
                pts.Add(boundary.GetPoint2dAt(i));
            }

            if (pts.Count < 3)
            {
                return false;
            }

            var x = point.X;
            var y = point.Y;
            var inside = false;
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
            {
                var pi = pts[i];
                var pj = pts[j];
                var intersects = ((pi.Y > y) != (pj.Y > y)) &&
                                 (x < (pj.X - pi.X) * (y - pi.Y) / ((pj.Y - pi.Y) == 0 ? 1e-12 : (pj.Y - pi.Y)) + pi.X);
                if (intersects)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static ParcelRecord FindParcelForPolyline(IEnumerable<ParcelRecord> parcels, Polyline polyline)
        {
            if (polyline == null)
            {
                return null;
            }

            var center = GetEntityCenter(polyline);
            foreach (var parcel in parcels)
            {
                if (ContainsPoint(parcel.Boundary, center) || PolylineHasVertexInside(polyline, parcel.Boundary))
                {
                    return parcel;
                }
            }

            return null;
        }

        private static bool PolylineHasVertexInside(Polyline source, Polyline boundary)
        {
            for (var i = 0; i < source.NumberOfVertices; i++)
            {
                var p = source.GetPoint3dAt(i);
                if (ContainsPoint(boundary, p))
                {
                    return true;
                }
            }

            return false;
        }

        private static Point3d GetEntityCenter(Entity entity)
        {
            try
            {
                var ext = entity.GeometricExtents;
                return new Point3d(
                    (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0,
                    (ext.MinPoint.Z + ext.MaxPoint.Z) / 2.0);
            }
            catch
            {
                return Point3d.Origin;
            }
        }

        private static bool IsLayer(Entity entity, string layer)
        {
            if (entity == null || string.IsNullOrWhiteSpace(layer))
            {
                return false;
            }

            return string.Equals(entity.Layer, layer, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(NormalizeLayerName(entity.Layer), NormalizeLayerName(layer), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGreenVariantLayer(Entity entity, string configuredLayer, string token)
        {
            if (IsLayer(entity, configuredLayer))
            {
                return true;
            }

            var entityLayer = NormalizeLayerName(entity?.Layer);
            var tokenLayer = NormalizeLayerName(token);
            return tokenLayer.Length > 0 &&
                   entityLayer.IndexOf(tokenLayer, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeLayerName(string layer)
        {
            return string.IsNullOrWhiteSpace(layer)
                ? ""
                : Regex.Replace(layer, @"[-_\s\(\)（）]", "").Trim();
        }

        private static bool IsEntityInScope(Entity entity)
        {
            if (_settings == null || !_settings.HasSelectionScope)
            {
                return true;
            }

            try
            {
                var ext = entity.GeometricExtents;
                return ext.MaxPoint.X >= _settings.ScopeMinX &&
                       ext.MinPoint.X <= _settings.ScopeMaxX &&
                       ext.MaxPoint.Y >= _settings.ScopeMinY &&
                       ext.MinPoint.Y <= _settings.ScopeMaxY;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            var match = Regex.Match(text, @"\d+");
            if (!match.Success)
            {
                var cnMatch = Regex.Match(text, @"地块\s*([一二三四五六七八九十]+)");
                if (cnMatch.Success)
                {
                    var parsed = ParseChineseNumber(cnMatch.Groups[1].Value);
                    if (parsed > 0)
                    {
                        return parsed.ToString(CultureInfo.InvariantCulture) + "#";
                    }
                }
            }

            return match.Success ? match.Value + "#" : text.Trim();
        }

        private static int ParseSortNumber(string number)
        {
            var match = Regex.Match(number ?? "", @"\d+");
            return match.Success && int.TryParse(match.Value, out var value) ? value : int.MaxValue;
        }

        private static double PromptAreaFactor(Editor ed)
        {
            var options = new PromptKeywordOptions("\n请选择图纸单位，表格面积统一输出为平方米")
            {
                AllowNone = true
            };
            options.Keywords.Add("MM");
            options.Keywords.Add("M");
            options.Keywords.Default = "MM";
            var result = ed.GetKeywords(options);
            if (result.Status == PromptStatus.OK && result.StringResult == "M")
            {
                return 1.0;
            }

            return 0.000001;
        }

        private static string PromptLayerName(Editor ed, string label, string defaultLayer)
        {
            var options = new PromptStringOptions($"\n{label} <{defaultLayer}>：")
            {
                AllowSpaces = false
            };
            var result = ed.GetString(options);
            if (result.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(result.StringResult))
            {
                return result.StringResult.Trim();
            }

            return defaultLayer;
        }

        private static bool IsLikelyParcelLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (string.Equals(NormalizeLayerName(_labelLayer), NormalizeLayerName(LayerBh), StringComparison.OrdinalIgnoreCase))
            {
                return Regex.IsMatch(text, @"\d+") || text.Contains("地块");
            }

            return text.Contains("地块") || Regex.IsMatch(text, @"^\s*\d+\s*#\s*$");
        }

        private static int ParseChineseNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var values = new Dictionary<char, int>
            {
                { '一', 1 }, { '二', 2 }, { '三', 3 }, { '四', 4 }, { '五', 5 },
                { '六', 6 }, { '七', 7 }, { '八', 8 }, { '九', 9 }
            };

            if (text == "十")
            {
                return 10;
            }

            var tenIndex = text.IndexOf('十');
            if (tenIndex >= 0)
            {
                var tens = tenIndex == 0 ? 1 : (values.TryGetValue(text[0], out var t) ? t : 0);
                var ones = tenIndex == text.Length - 1 ? 0 : (values.TryGetValue(text[text.Length - 1], out var o) ? o : 0);
                return tens * 10 + ones;
            }

            return values.TryGetValue(text[0], out var value) ? value : 0;
        }

        private static string GetCellText(Table table, int row, int column)
        {
            return table.Cells[row, column].TextString ?? string.Empty;
        }

        private static double ParsePositiveNumber(string text, double fallback)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0)
            {
                return value;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && value >= 0)
            {
                return value;
            }

            return fallback;
        }

        private static double ParseFirstPositiveNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var match = Regex.Match(text, @"\d+(\.\d+)?");
            if (!match.Success)
            {
                return 0;
            }

            return ParsePositiveNumber(match.Value, 0);
        }

        private static double ParsePercent(string text, double fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            var clean = text.Trim();
            var hasPercent = clean.EndsWith("%", StringComparison.Ordinal);
            clean = clean.TrimEnd('%');
            var value = ParsePositiveNumber(clean, fallback);
            return hasPercent ? value / 100.0 : value;
        }

        private static string FormatArea(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatInteger(double value)
        {
            return Math.Ceiling(value - Tolerance).ToString("0", CultureInfo.InvariantCulture);
        }

        private static string FormatPercent(double value)
        {
            return (value * 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        private static double GetColumnWidth(int column)
        {
            switch (column)
            {
                case 0: return 22.0;
                case 1: return 18.0;
                case 2:
                case 3:
                case 4:
                case 5:
                case 8:
                case 12:
                case 13: return 28.0;
                case 6:
                case 7:
                case 9: return 16.0;
                case 10:
                case 11:
                    return 18.0;
                case 14:
                    return 22.0;
                case 15:
                case 16: return 18.0;
                default: return 20.0;
            }
        }

        private static double GetLegacyColumnWidth(int column)
        {
            switch (column)
            {
                case 4:
                case 7: return 16.0;
                default: return 20.0;
            }
        }

        private static void ApplyCommonTableStyle(Table table)
        {
            ApplyCommonTableStyle(table, TextHeight);
        }

        private static void ApplyCommonTableStyle(Table table, double bodyTextHeight)
        {
            var normalColor = Color.FromColorIndex(ColorMethod.ByAci, 7);
            var headerColor = Color.FromColorIndex(ColorMethod.ByAci, 2);
            var titleTextHeight = Math.Max(0.1, bodyTextHeight * 1.25);
            var headerTextHeight = Math.Max(0.1, bodyTextHeight);
            for (var row = 0; row < table.Rows.Count; row++)
            {
                for (var col = 0; col < table.Columns.Count; col++)
                {
                    var cell = table.Cells[row, col];
                    cell.TextHeight = row == 0
                        ? titleTextHeight
                        : row == 1
                            ? headerTextHeight
                            : bodyTextHeight;
                    cell.ContentColor = row <= 1 ? headerColor : normalColor;
                    cell.Alignment = row <= 1 || col > 1
                        ? CellAlignment.MiddleCenter
                        : CellAlignment.MiddleLeft;
                }
            }

            table.Cells[0, 0].TextHeight = titleTextHeight;
            table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
            table.Cells[0, 0].ContentColor = headerColor;
        }

        private static double GetTableTextHeight(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return TextHeight;
            }

            return Math.Max(0.5, Math.Min(50.0, value));
        }

        private static void ApplyTableRowHeights(Table table, double bodyTextHeight)
        {
            var rowHeight = Math.Max(RowHeight, bodyTextHeight * 2.7);
            var titleRowHeight = Math.Max(TitleRowHeight, bodyTextHeight * 3.2);
            table.SetRowHeight(rowHeight);
            if (table.Rows.Count > 0)
            {
                table.Rows[0].Height = titleRowHeight;
            }
        }

        private static double GetTableScale(double bodyTextHeight)
        {
            return Math.Max(0.75, Math.Min(4.0, bodyTextHeight / TextHeight));
        }

        private static double ScaleWidth(double baseWidth, double scale)
        {
            var width = baseWidth * scale;
            return Math.Max(baseWidth * 0.85, width);
        }

        private static Point3d GetDetailTablePosition(Point3d summaryPosition)
        {
            return summaryPosition + new Vector3d(0, -GetSummaryDetailOffset(), 0);
        }

        private static double GetSummaryDetailOffset()
        {
            return TitleRowHeight + (SummaryRowCount - 1) * RowHeight + TableGap;
        }

        private static Point3d GetNextTablePosition(Table table)
        {
            return table.Position + new Vector3d(0, -GetTableHeight(table) - GetTableGap(table), 0);
        }

        private static double GetTableHeight(Table table)
        {
            var height = 0.0;
            for (var row = 0; row < table.Rows.Count; row++)
            {
                height += table.Rows[row].Height;
            }

            return height > 0
                ? height
                : TitleRowHeight + Math.Max(0, table.Rows.Count - 1) * RowHeight;
        }

        private static double GetTableGap(Table table)
        {
            var firstBodyRow = table.Rows.Count > 1 ? table.Rows[1].Height : RowHeight;
            return Math.Max(TableGap, firstBodyRow * 1.6);
        }

        private static void RepositionDetailTableIfOverlapping(Table summaryTable, Table detailTable)
        {
            var desired = GetNextTablePosition(summaryTable);
            var xAligned = Math.Abs(detailTable.Position.X - summaryTable.Position.X) < 1.0;
            var overlapsSummary = detailTable.Position.Y > desired.Y + 1.0;
            if (xAligned && overlapsSummary)
            {
                detailTable.Position = desired;
            }
        }

        private static void RepositionOverrideTable(Table detailTable, Table overrideTable)
        {
            if (detailTable == null || overrideTable == null)
            {
                return;
            }

            overrideTable.Position = GetNextTablePosition(detailTable);
        }

        private static string GetCreateCommandName()
        {
#if LANDINDEX_V30
            return "dulang30";
#elif LANDINDEX_V29
            return "dulang29";
#elif LANDINDEX_V28
            return "dulang28";
#elif LANDINDEX_V27
            return "dulang27";
#elif LANDINDEX_V24
            return "dulang1";
#elif LANDINDEX_V23
            return "JZMJYDTABLE23";
#elif LANDINDEX_V22
            return "JZMJYDTABLE22";
#elif LANDINDEX_V21
            return "JZMJYDTABLE21";
#elif LANDINDEX_V20
            return "JZMJYDTABLE20";
#elif LANDINDEX_V19
            return "JZMJYDTABLE19";
#elif LANDINDEX_V18
            return "JZMJYDTABLE18";
#elif LANDINDEX_V17
            return "JZMJYDTABLE17";
#elif LANDINDEX_V16
            return "JZMJYDTABLE16";
#elif LANDINDEX_V15
            return "JZMJYDTABLE15";
#elif LANDINDEX_V14
            return "JZMJYDTABLE14";
#elif LANDINDEX_V13
            return "JZMJYDTABLE13";
#elif LANDINDEX_V12
            return "JZMJYDTABLE12";
#elif LANDINDEX_V11
            return "JZMJYDTABLE11";
#elif LANDINDEX_V10
            return "JZMJYDTABLE10";
#elif LANDINDEX_V9
            return "JZMJYDTABLE9";
#elif LANDINDEX_V8
            return "JZMJYDTABLE8";
#elif LANDINDEX_V7
            return "JZMJYDTABLE7";
#elif LANDINDEX_V6
            return "JZMJYDTABLE6";
#elif LANDINDEX_V5
            return "JZMJYDTABLE5";
#elif LANDINDEX_V4
            return "JZMJYDTABLE4";
#elif LANDINDEX_V3
            return "JZMJYDTABLE3";
#elif LANDINDEX_V2
            return "JZMJYDTABLE2";
#else
            return "dulang1";
#endif
        }

        private static string GetUpdateCommandName()
        {
#if LANDINDEX_V30
            return "JZMJYDUPDATE30";
#elif LANDINDEX_V29
            return "JZMJYDUPDATE29";
#elif LANDINDEX_V28
            return "JZMJYDUPDATE28";
#elif LANDINDEX_V27
            return "JZMJYDUPDATE27";
#elif LANDINDEX_V24
            return "JZMJYDUPDATE24";
#elif LANDINDEX_V23
            return "JZMJYDUPDATE23";
#elif LANDINDEX_V22
            return "JZMJYDUPDATE22";
#elif LANDINDEX_V21
            return "JZMJYDUPDATE21";
#elif LANDINDEX_V20
            return "JZMJYDUPDATE20";
#elif LANDINDEX_V19
            return "JZMJYDUPDATE19";
#elif LANDINDEX_V18
            return "JZMJYDUPDATE18";
#elif LANDINDEX_V17
            return "JZMJYDUPDATE17";
#elif LANDINDEX_V16
            return "JZMJYDUPDATE16";
#elif LANDINDEX_V15
            return "JZMJYDUPDATE15";
#elif LANDINDEX_V14
            return "JZMJYDUPDATE14";
#elif LANDINDEX_V13
            return "JZMJYDUPDATE13";
#elif LANDINDEX_V12
            return "JZMJYDUPDATE12";
#elif LANDINDEX_V11
            return "JZMJYDUPDATE11";
#elif LANDINDEX_V10
            return "JZMJYDUPDATE10";
#elif LANDINDEX_V9
            return "JZMJYDUPDATE9";
#elif LANDINDEX_V8
            return "JZMJYDUPDATE8";
#elif LANDINDEX_V7
            return "JZMJYDUPDATE7";
#elif LANDINDEX_V6
            return "JZMJYDUPDATE6";
#elif LANDINDEX_V5
            return "JZMJYDUPDATE5";
#elif LANDINDEX_V4
            return "JZMJYDUPDATE4";
#elif LANDINDEX_V3
            return "JZMJYDUPDATE3";
#elif LANDINDEX_V2
            return "JZMJYDUPDATE2";
#else
            return "JZMJYDUPDATE";
#endif
        }

        private static void SetCellRed(Table table, int row, int column)
        {
            table.Cells[row, column].ContentColor = Color.FromColorIndex(ColorMethod.ByAci, 1);
        }

        private class ParcelRecord
        {
            public Polyline Boundary { get; set; }
            public string Number { get; set; }
            public double BaseArea { get; set; }
            public double LandArea { get; set; }
            public double BikeArea { get; set; }
            public double GreenArea { get; set; }
            public double FloorsFromDrawing { get; set; }
            public Point3d InfoLabelPoint { get; set; }
            public bool HasInfoLabelPoint { get; set; }
            public double InfoLabelBuildingArea { get; set; }
        }

        private class TextRecord
        {
            public string Text { get; set; }
            public Point3d Position { get; set; }
        }

        private class WeightedPolyline
        {
            public Polyline Boundary { get; set; }
            public double Factor { get; set; }
        }

        private class RowInput
        {
            public string Type { get; set; }
            public double Floors { get; set; }
            public double GreenRate { get; set; }
            public double GroundParking { get; set; }
            public double IndoorParking { get; set; }
            public double NonCapacityArea { get; set; }
        }

        private class RowOutput
        {
            public string Type { get; set; }
            public string Number { get; set; }
            public double BaseArea { get; set; }
            public double CapacityArea { get; set; }
            public double NonCapacityArea { get; set; }
            public double BuildingArea { get; set; }
            public double Floors { get; set; }
            public double LandArea { get; set; }
            public double Mu { get; set; }
            public double Density { get; set; }
            public double GreenRate { get; set; }
            public double GreenArea { get; set; }
            public double RequiredGreenArea { get; set; }
            public double MaxGreenArea { get; set; }
            public bool GreenAreaOk { get; set; }
            public double BikeArea { get; set; }
            public double BikeCalcArea { get; set; }
            public double RequiredBikeArea { get; set; }
            public bool BikeAreaOk { get; set; }
            public double MotorCalcArea { get; set; }
            public double Parking { get; set; }
            public double RequiredParking { get; set; }
            public bool ParkingOk { get; set; }
            public double Far { get; set; }
            public double GroundParking { get; set; }
            public double IndoorParking { get; set; }
            public Point3d InfoLabelPoint { get; set; }
            public bool HasInfoLabelPoint { get; set; }
        }

        private class SummaryLine
        {
            public string Key { get; set; }
            public string Parent { get; set; }
            public string Item { get; set; }
            public string SubItem { get; set; }
            public string Unit { get; set; }
            public double Value { get; set; }
            public string Text { get; set; }
            public string Remark { get; set; }
            public bool Manual { get; set; }
            public bool Custom { get; set; }
        }

        private class TablePair
        {
            public Table Summary { get; set; }
            public Table Detail { get; set; }
            public Table Override { get; set; }
        }
    }
}
