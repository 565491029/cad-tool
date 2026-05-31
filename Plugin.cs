using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(BuildingAreaTool.Plugin))]

namespace BuildingAreaTool
{
    public class Plugin : IExtensionApplication
    {
        private const string RegAppName = "BUILDING_AREA_TOOL";
        private const string TableMarker = "BUILDING_AREA_TABLE";
        private const int ColIndex = 0;
        private const int ColHandle = 1;
        private const int ColFootprint = 2;
        private const int ColFloors = 3;
        private const int ColTotal = 4;

        private static readonly List<ObjectId> PendingPolylines = new List<ObjectId>();
        private static readonly HashSet<ObjectId> PendingModifiedPolylines = new HashSet<ObjectId>();
        private static ObjectId _currentTableId = ObjectId.Null;
        private static bool _autoEnabled;
        private static bool _eventsAttached;
        private static bool _pendingTableRefresh;
        private static bool _isRefreshing;
        private static double _areaFactor = 0.000001; // default: square millimeters to square meters

        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage(
                "\n建筑面积统计工具已加载。命令：JZMJTABLE 创建表格，JZMJAUTO 自动统计，JZMJADD 添加轮廓，JZMJUPDATE 更新汇总。");
        }

        public void Terminate()
        {
            DetachEvents();
        }

        [CommandMethod("JZMJTABLE")]
        public void CreateAreaTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            EnsureRegApp(db);
            _areaFactor = PromptAreaFactor(ed);

            var polylineIds = PromptClosedPolylines(ed, "\n选择已有的闭合 PLINE 建筑轮廓，或直接回车创建空表：");
            var pointResult = ed.GetPoint("\n指定面积统计表插入点：");
            if (pointResult.Status != PromptStatus.OK)
            {
                return;
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var table = new Table();
                table.TableStyle = db.Tablestyle;
                table.Position = pointResult.Value;
                table.SetSize(2, 5);
                table.SetRowHeight(3.5);
                table.Columns[ColIndex].Width = 14.0;
                table.Columns[ColHandle].Width = 28.0;
                table.Columns[ColFootprint].Width = 28.0;
                table.Columns[ColFloors].Width = 14.0;
                table.Columns[ColTotal].Width = 28.0;
                table.Cells[0, ColIndex].TextString = "序号";
                table.Cells[0, ColHandle].TextString = "图元句柄";
                table.Cells[0, ColFootprint].TextString = "占地面积(m²)";
                table.Cells[0, ColFloors].TextString = "层数";
                table.Cells[0, ColTotal].TextString = "总面积(m²)";
                ApplyTableXData(table, _areaFactor);
                FillTotalRow(table, 1, 0, 0);
                table.GenerateLayout();

                modelSpace.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);
                _currentTableId = table.ObjectId;

                AppendPolylineRows(table, tr, polylineIds);
                RecalculateTable(table, tr);
                tr.Commit();
            }

            EnableAutoMode();
            ed.WriteMessage("\n面积统计表已创建。填写/修改“层数”后运行 JZMJUPDATE 可刷新总面积和合计。");
        }

        [CommandMethod("JZMJADD")]
        public void AddPolylinesToTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var polylineIds = PromptClosedPolylines(ed, "\n选择要加入统计表的闭合 PLINE：");
            if (polylineIds.Count == 0)
            {
                ed.WriteMessage("\n未选择闭合 PLINE。");
                return;
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = GetCurrentTable(db, tr, ed, OpenMode.ForWrite);
                if (table == null)
                {
                    return;
                }

                ReadTableSettings(table);
                AppendPolylineRows(table, tr, polylineIds);
                RecalculateTable(table, tr);
                tr.Commit();
            }
        }

        [CommandMethod("JZMJUPDATE")]
        public void UpdateAreaTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = GetCurrentTable(db, tr, ed, OpenMode.ForWrite);
                if (table == null)
                {
                    return;
                }

                ReadTableSettings(table);
                RecalculateTable(table, tr);
                tr.Commit();
            }

            ed.WriteMessage("\n面积统计表已更新。");
        }

        [CommandMethod("JZMJAUTO")]
        public void ToggleAutoMode()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            if (_autoEnabled)
            {
                _autoEnabled = false;
                DetachEvents();
                ed.WriteMessage("\n自动统计已关闭。");
            }
            else
            {
                EnableAutoMode();
                ed.WriteMessage("\n自动统计已开启：新画出的闭合 PLINE 会自动加入当前面积表。");
            }
        }

        [CommandMethod("JZMJHELP")]
        public void ShowHelp()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage(
                "\n建筑面积统计工具已加载。" +
                "\nJZMJTABLE  创建/选择统计表" +
                "\nJZMJAUTO   开启/关闭新 PLINE 自动统计" +
                "\nJZMJADD    手动添加闭合 PLINE 到当前表" +
                "\nJZMJUPDATE 修改层数后刷新面积和汇总");
        }

        private static void EnableAutoMode()
        {
            _autoEnabled = true;
            AttachEvents();
        }

        private static void AttachEvents()
        {
            if (_eventsAttached)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            doc.Database.ObjectAppended += OnObjectAppended;
            doc.Database.ObjectModified += OnObjectModified;
            doc.CommandEnded += OnCommandEnded;
            doc.CommandCancelled += OnCommandInterrupted;
            doc.CommandFailed += OnCommandInterrupted;
            _eventsAttached = true;
        }

        private static void DetachEvents()
        {
            if (!_eventsAttached)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Database.ObjectAppended -= OnObjectAppended;
                doc.Database.ObjectModified -= OnObjectModified;
                doc.CommandEnded -= OnCommandEnded;
                doc.CommandCancelled -= OnCommandInterrupted;
                doc.CommandFailed -= OnCommandInterrupted;
            }

            PendingPolylines.Clear();
            PendingModifiedPolylines.Clear();
            _pendingTableRefresh = false;
            _eventsAttached = false;
        }

        private static void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            if (!_autoEnabled || e.DBObject == null || e.DBObject.ObjectId.IsNull)
            {
                return;
            }

            if (e.DBObject is Polyline)
            {
                PendingPolylines.Add(e.DBObject.ObjectId);
            }
        }

        private static void OnObjectModified(object sender, ObjectEventArgs e)
        {
            if (!_autoEnabled || _isRefreshing || e.DBObject == null || e.DBObject.ObjectId.IsNull)
            {
                return;
            }

            if (e.DBObject is Polyline)
            {
                PendingModifiedPolylines.Add(e.DBObject.ObjectId);
                _pendingTableRefresh = true;
                return;
            }

            var table = e.DBObject as Table;
            if (table != null && (e.DBObject.ObjectId == _currentTableId || IsAreaTable(table)))
            {
                _currentTableId = e.DBObject.ObjectId;
                _pendingTableRefresh = true;
            }
        }

        private static void OnCommandEnded(object sender, CommandEventArgs e)
        {
            if (!_autoEnabled ||
                (PendingPolylines.Count == 0 && PendingModifiedPolylines.Count == 0 && !_pendingTableRefresh))
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                PendingPolylines.Clear();
                return;
            }

            try
            {
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var table = GetCurrentTable(doc.Database, tr, doc.Editor, OpenMode.ForWrite);
                    if (table != null)
                    {
                        _isRefreshing = true;
                        try
                        {
                            ReadTableSettings(table);
                            AppendPolylineRows(table, tr, PendingPolylines);
                            RecalculateTable(table, tr);
                        }
                        finally
                        {
                            _isRefreshing = false;
                        }
                    }

                    tr.Commit();
                }
            }
            finally
            {
                PendingPolylines.Clear();
                PendingModifiedPolylines.Clear();
                _pendingTableRefresh = false;
            }
        }

        private static void OnCommandInterrupted(object sender, CommandEventArgs e)
        {
            PendingPolylines.Clear();
            PendingModifiedPolylines.Clear();
            _pendingTableRefresh = false;
        }

        private static List<ObjectId> PromptClosedPolylines(Editor ed, string message)
        {
            var options = new PromptSelectionOptions
            {
                MessageForAdding = message,
                AllowDuplicates = false
            };
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
            });
            var result = ed.GetSelection(options, filter);
            var ids = new List<ObjectId>();
            if (result.Status != PromptStatus.OK)
            {
                return ids;
            }

            foreach (SelectedObject selected in result.Value)
            {
                if (selected != null)
                {
                    ids.Add(selected.ObjectId);
                }
            }

            return ids;
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

        private static void AppendPolylineRows(Table table, Transaction tr, IEnumerable<ObjectId> polylineIds)
        {
            var existingHandles = ReadExistingHandles(table);
            foreach (var id in polylineIds)
            {
                if (id.IsNull || id.IsErased)
                {
                    continue;
                }

                var polyline = tr.GetObject(id, OpenMode.ForRead, false) as Polyline;
                if (polyline == null || !polyline.Closed)
                {
                    continue;
                }

                var handle = polyline.Handle.ToString();
                if (existingHandles.Contains(handle))
                {
                    continue;
                }

                var row = Math.Max(1, table.Rows.Count - 1);
                table.InsertRows(row, 3.5, 1);
                table.Cells[row, ColIndex].TextString = row.ToString(CultureInfo.InvariantCulture);
                table.Cells[row, ColHandle].TextString = handle;
                table.Cells[row, ColFootprint].TextString = FormatArea(polyline.Area * _areaFactor);
                table.Cells[row, ColFloors].TextString = "1";
                table.Cells[row, ColTotal].TextString = FormatArea(polyline.Area * _areaFactor);
                existingHandles.Add(handle);
            }
        }

        private static void RecalculateTable(Table table, Transaction tr)
        {
            var footprintSum = 0.0;
            var totalSum = 0.0;
            var totalRow = table.Rows.Count - 1;

            for (var row = 1; row < totalRow; row++)
            {
                table.Cells[row, ColIndex].TextString = row.ToString(CultureInfo.InvariantCulture);

                var handleText = GetCellText(table, row, ColHandle);
                var polyline = GetPolylineByHandle(table.Database, tr, handleText);
                if (polyline == null || !polyline.Closed)
                {
                    table.Cells[row, ColFootprint].TextString = "轮廓无效";
                    table.Cells[row, ColTotal].TextString = "-";
                    continue;
                }

                var footprint = polyline.Area * _areaFactor;
                var floors = ParsePositiveNumber(GetCellText(table, row, ColFloors), 1.0);
                var total = footprint * floors;
                footprintSum += footprint;
                totalSum += total;

                table.Cells[row, ColFootprint].TextString = FormatArea(footprint);
                table.Cells[row, ColFloors].TextString = FormatNumber(floors);
                table.Cells[row, ColTotal].TextString = FormatArea(total);
            }

            FillTotalRow(table, totalRow, footprintSum, totalSum);
            table.GenerateLayout();
        }

        private static void FillTotalRow(Table table, int row, double footprintSum, double totalSum)
        {
            table.Cells[row, ColIndex].TextString = "合计";
            table.Cells[row, ColHandle].TextString = "";
            table.Cells[row, ColFootprint].TextString = FormatArea(footprintSum);
            table.Cells[row, ColFloors].TextString = "";
            table.Cells[row, ColTotal].TextString = FormatArea(totalSum);
        }

        private static HashSet<string> ReadExistingHandles(Table table)
        {
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var row = 1; row < table.Rows.Count - 1; row++)
            {
                var handle = GetCellText(table, row, ColHandle).Trim();
                if (!string.IsNullOrEmpty(handle))
                {
                    handles.Add(handle);
                }
            }

            return handles;
        }

        private static Table GetCurrentTable(Database db, Transaction tr, Editor ed, OpenMode mode)
        {
            if (!_currentTableId.IsNull && !_currentTableId.IsErased)
            {
                var table = tr.GetObject(_currentTableId, mode, false) as Table;
                if (table != null)
                {
                    return table;
                }
            }

            var found = FindAreaTable(db, tr, mode);
            if (found != null)
            {
                _currentTableId = found.ObjectId;
                return found;
            }

            ed.WriteMessage("\n未找到面积统计表，请先运行 JZMJTABLE。");
            return null;
        }

        private static Table FindAreaTable(Database db, Transaction tr, OpenMode mode)
        {
            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in modelSpace)
            {
                var table = tr.GetObject(id, mode, false) as Table;
                if (table != null && IsAreaTable(table))
                {
                    return table;
                }
            }

            return null;
        }

        private static bool IsAreaTable(Table table)
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

        private static void ReadTableSettings(Table table)
        {
            var data = table.XData;
            if (data == null)
            {
                return;
            }

            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataReal && value.Value is double factor && factor > 0)
                {
                    _areaFactor = factor;
                    return;
                }
            }
        }

        private static void ApplyTableXData(Table table, double areaFactor)
        {
            table.XData = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, TableMarker),
                new TypedValue((int)DxfCode.ExtendedDataReal, areaFactor));
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

        private static Polyline GetPolylineByHandle(Database db, Transaction tr, string handleText)
        {
            if (string.IsNullOrWhiteSpace(handleText))
            {
                return null;
            }

            try
            {
                var handle = new Handle(Convert.ToInt64(handleText.Trim(), 16));
                var id = db.GetObjectId(false, handle, 0);
                return tr.GetObject(id, OpenMode.ForRead, false) as Polyline;
            }
            catch
            {
                return null;
            }
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

        private static string FormatArea(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
