using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(BuildingAreaLiveUpdater.LiveUpdaterPlugin))]

namespace BuildingAreaLiveUpdater
{
    public class LiveUpdaterPlugin : IExtensionApplication
    {
        private const string TableMarker = "BUILDING_AREA_TABLE";
        private const int ColIndex = 0;
        private const int ColHandle = 1;
        private const int ColFootprint = 2;
        private const int ColFloors = 3;
        private const int ColTotal = 4;

        private static readonly List<ObjectId> NewPolylines = new List<ObjectId>();
        private static bool _attached;
        private static bool _pendingRefresh;
        private static bool _isRefreshing;
        private static double _areaFactor = 0.000001;

        public void Initialize()
        {
            Attach();
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                "\nJZMJ live updater loaded. PLINE/table edits will refresh the area table after each command.");
        }

        public void Terminate()
        {
            Detach();
        }

        [CommandMethod("JZMJLIVEON")]
        public void TurnOn()
        {
            Attach();
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nJZMJ live updater is on.");
        }

        [CommandMethod("JZMJREFRESH")]
        public void RefreshNow()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                RefreshTables(doc.Database, tr);
                tr.Commit();
            }

            doc.Editor.WriteMessage("\nJZMJ area table refreshed.");
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

            doc.Database.ObjectAppended += OnObjectAppended;
            doc.Database.ObjectModified += OnObjectModified;
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
                doc.Database.ObjectAppended -= OnObjectAppended;
                doc.Database.ObjectModified -= OnObjectModified;
                doc.CommandEnded -= OnCommandEnded;
                doc.CommandCancelled -= OnCommandInterrupted;
                doc.CommandFailed -= OnCommandInterrupted;
            }

            ClearPending();
            _attached = false;
        }

        private static void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            if (_isRefreshing || e.DBObject == null || e.DBObject.ObjectId.IsNull)
            {
                return;
            }

            if (e.DBObject is Polyline)
            {
                NewPolylines.Add(e.DBObject.ObjectId);
                _pendingRefresh = true;
            }
        }

        private static void OnObjectModified(object sender, ObjectEventArgs e)
        {
            if (_isRefreshing || e.DBObject == null || e.DBObject.ObjectId.IsNull)
            {
                return;
            }

            if (e.DBObject is Polyline)
            {
                _pendingRefresh = true;
                return;
            }

            var table = e.DBObject as Table;
            if (table != null && IsAreaTable(table))
            {
                _pendingRefresh = true;
            }
        }

        private static void OnCommandEnded(object sender, CommandEventArgs e)
        {
            if (!_pendingRefresh && NewPolylines.Count == 0)
            {
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ClearPending();
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
                        RefreshTables(doc.Database, tr);
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
                ClearPending();
            }
        }

        private static void OnCommandInterrupted(object sender, CommandEventArgs e)
        {
            ClearPending();
        }

        private static void ClearPending()
        {
            NewPolylines.Clear();
            _pendingRefresh = false;
        }

        private static void RefreshTables(Database db, Transaction tr)
        {
            var tables = FindAreaTables(db, tr);
            if (tables.Count == 0)
            {
                return;
            }

            var primaryTable = tables[0];
            ReadTableSettings(primaryTable);
            AppendPolylineRows(primaryTable, tr, NewPolylines);

            foreach (var table in tables)
            {
                ReadTableSettings(table);
                RecalculateTable(table, tr);
            }
        }

        private static List<Table> FindAreaTables(Database db, Transaction tr)
        {
            var result = new List<Table>();
            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in modelSpace)
            {
                var table = tr.GetObject(id, OpenMode.ForWrite, false) as Table;
                if (table != null && IsAreaTable(table))
                {
                    result.Add(table);
                }
            }

            return result;
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
                    table.Cells[row, ColFootprint].TextString = "Invalid";
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

            table.Cells[totalRow, ColIndex].TextString = "Total";
            table.Cells[totalRow, ColHandle].TextString = "";
            table.Cells[totalRow, ColFootprint].TextString = FormatArea(footprintSum);
            table.Cells[totalRow, ColFloors].TextString = "";
            table.Cells[totalRow, ColTotal].TextString = FormatArea(totalSum);
            table.GenerateLayout();
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
