using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WinForms = System.Windows.Forms;

namespace LandIndexTool
{
    internal sealed class LandIndexSettingsForm : WinForms.Form
    {
        private static readonly Color ConsoleBackground = ColorTranslator.FromHtml("#F5F7FA");
        private static readonly Color CardBackground = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color PrimaryColor = ColorTranslator.FromHtml("#2563EB");
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#D0D7DE");
        private static readonly Color TextColor = ColorTranslator.FromHtml("#1F2937");
        private static readonly Color MutedTextColor = ColorTranslator.FromHtml("#6B7280");
        private static readonly Color HeaderBackground = ColorTranslator.FromHtml("#EFF6FF");
        private const int SectionHeaderHeight = 70;
        private const int InputLabelHeight = 34;
        private const int InputBoxHeight = 32;
        private const int InputRowHeight = 88;
        private const int LeftPanelMinWidth = 380;
        private const int LeftPanelPreferredWidth = 520;
        private const int ReviewPanelMinWidth = 560;
        private static readonly float LayoutScale = GetLayoutScale();

        private WinForms.RadioButton _unitMmButton;
        private WinForms.RadioButton _unitMButton;
        private WinForms.TextBox _parcelLayerBox;
        private WinForms.TextBox _labelLayerBox;
        private WinForms.TextBox _buildingLayerBox;
        private WinForms.TextBox _bikeLayerBox;
        private WinForms.TextBox _greenLayerBox;
        private WinForms.TextBox _floorLayerBox;
        private WinForms.TextBox _grassPaverLayerBox;
        private WinForms.TextBox _roofGreen1LayerBox;
        private WinForms.TextBox _roofGreen2LayerBox;
        private WinForms.TextBox _roofGreen3LayerBox;
        private WinForms.TextBox _greenMinBox;
        private WinForms.TextBox _greenMaxBox;
        private WinForms.TextBox _bikePer100Box;
        private WinForms.TextBox _bikeAreaBox;
        private WinForms.TextBox _motorPer100Box;
        private WinForms.TextBox _grassPaverFactorBox;
        private WinForms.TextBox _roofGreen1FactorBox;
        private WinForms.TextBox _roofGreen2FactorBox;
        private WinForms.TextBox _roofGreen3FactorBox;
        private WinForms.CheckBox _showInfoLabelsBox;
        private WinForms.TextBox _infoLabelTextHeightBox;
        private WinForms.TextBox _summaryTableTextHeightBox;
        private WinForms.TextBox _detailTableTextHeightBox;
        private WinForms.DataGridView _areaGrid;
        private WinForms.DataGridView _detailGrid;
        private WinForms.DataGridView _summaryGrid;
        private WinForms.Label _parcelCountValue;
        private WinForms.Label _landAreaValue;
        private WinForms.Label _capacityAreaValue;
        private WinForms.Label _greenRateValue;
        private WinForms.SplitContainer _mainSplit;
        private readonly List<LandIndexPreviewRow> _previewRows;

        public LandIndexSettings Settings { get; private set; }

        public LandIndexSettingsForm(LandIndexSettings settings, IEnumerable<AreaOverrideSeed> seeds, IEnumerable<LandIndexPreviewRow> previewRows)
        {
            Settings = settings.Clone();
            Settings.Normalize();
            _previewRows = (previewRows ?? Enumerable.Empty<LandIndexPreviewRow>()).ToList();

            Text = "独狼建筑指标统计控制台";
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            FormBorderStyle = WinForms.FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            AutoScaleMode = WinForms.AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            BackColor = ConsoleBackground;
            ApplyInitialWindowSize();

            BuildConsoleLayout(seeds ?? Enumerable.Empty<AreaOverrideSeed>());
            Shown += (s, e) => ApplySafeSplitterDistance();
            ResizeEnd += (s, e) => ApplySafeSplitterDistance();
        }

        private static float GetLayoutScale()
        {
            var baseHeight = 15f;
            var menuFontHeight = Math.Max(baseHeight, WinForms.SystemInformation.MenuFont.Height);
            return Math.Max(1f, Math.Min(2.0f, menuFontHeight / baseHeight));
        }

        private static int Scale(int value)
        {
            return (int)Math.Ceiling(value * LayoutScale);
        }

        private static Size ScaledSize(int width, int height)
        {
            return new Size(Scale(width), Scale(height));
        }

        private static WinForms.Padding ScaledPadding(int all)
        {
            return new WinForms.Padding(Scale(all));
        }

        private static WinForms.Padding ScaledPadding(int left, int top, int right, int bottom)
        {
            return new WinForms.Padding(Scale(left), Scale(top), Scale(right), Scale(bottom));
        }

        private void ApplyInitialWindowSize()
        {
            var workingArea = WinForms.Screen.FromPoint(WinForms.Cursor.Position).WorkingArea;
            var margin = Scale(48);
            var maxWidth = Math.Max(Scale(900), workingArea.Width - margin);
            var maxHeight = Math.Max(Scale(620), workingArea.Height - margin);
            var desired = ScaledSize(1360, 840);
            var minimum = ScaledSize(980, 660);
            ClientSize = new Size(Math.Min(desired.Width, maxWidth), Math.Min(desired.Height, maxHeight));
            MinimumSize = new Size(Math.Min(minimum.Width, maxWidth), Math.Min(minimum.Height, maxHeight));

            if (workingArea.Width < Scale(1180) || workingArea.Height < Scale(760))
            {
                WindowState = WinForms.FormWindowState.Maximized;
            }
        }

        private void BuildConsoleLayout(IEnumerable<AreaOverrideSeed> seeds)
        {
            var main = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = ScaledPadding(14),
                BackColor = ConsoleBackground
            };
            main.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(96)));
            main.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(132)));
            main.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            main.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(70)));
            Controls.Add(main);

            main.Controls.Add(CreateTitlePanel(), 0, 0);
            main.Controls.Add(CreateSummaryCards(), 0, 1);

            var split = new WinForms.SplitContainer
            {
                Dock = WinForms.DockStyle.Fill,
                FixedPanel = WinForms.FixedPanel.Panel1,
                SplitterDistance = Scale(LeftPanelPreferredWidth),
                SplitterWidth = Scale(8),
                BackColor = ConsoleBackground
            };
            _mainSplit = split;
            main.Controls.Add(split, 0, 2);
            split.Panel1.BackColor = ConsoleBackground;
            split.Panel2.BackColor = ConsoleBackground;
            split.Panel1.Controls.Add(CreateLeftConsole());
            split.Panel2.Controls.Add(CreateReviewConsole(seeds));
            main.Controls.Add(CreateButtonBar(), 0, 3);
        }

        private WinForms.Control CreateTitlePanel()
        {
            var card = CreateCardShell();
            card.Padding = ScaledPadding(20, 14, 20, 12);
            var layout = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 100));
            layout.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Absolute, Scale(220)));
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 58));
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 42));
            card.Controls.Add(layout);

            layout.Controls.Add(new WinForms.Label
            {
                Text = "独狼建筑指标统计控制台",
                Dock = WinForms.DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            layout.Controls.Add(new WinForms.Label
            {
                Text = "读取 CAD 图层与 PLINE，复核地块、建筑、绿地、车位指标",
                Dock = WinForms.DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            var commandBadge = new WinForms.Label
            {
                Text = "CAD 命令：dulang1",
                Dock = WinForms.DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            layout.Controls.Add(commandBadge, 1, 0);
            layout.SetRowSpan(commandBadge, 2);
            return card;
        }

        private void ApplySafeSplitterDistance()
        {
            if (_mainSplit == null)
            {
                return;
            }

            var available = _mainSplit.Width - _mainSplit.SplitterWidth;
            if (available <= 0)
            {
                return;
            }

            var panel1Min = Math.Min(Scale(LeftPanelMinWidth), Math.Max(Scale(120), available / 4));
            var panel2Min = Math.Min(Scale(ReviewPanelMinWidth), Math.Max(Scale(180), available / 3));
            if (available <= panel1Min + panel2Min)
            {
                panel1Min = Math.Max(Scale(120), available / 3);
                panel2Min = Math.Max(Scale(180), available / 3);
            }

            _mainSplit.Panel1MinSize = 0;
            _mainSplit.Panel2MinSize = 0;

            var maxDistance = Math.Max(1, available - panel2Min);
            var preferred = Math.Min(Scale(LeftPanelPreferredWidth), Math.Max(Scale(LeftPanelMinWidth), available / 3));
            var distance = Math.Min(preferred, maxDistance);
            distance = Math.Max(panel1Min, distance);
            if (distance > 0 && distance < available)
            {
                _mainSplit.SplitterDistance = distance;
                _mainSplit.Panel1MinSize = Math.Min(panel1Min, distance);
                _mainSplit.Panel2MinSize = Math.Min(panel2Min, available - distance);
            }
        }

        private WinForms.Control CreateSummaryCards()
        {
            var panel = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = ConsoleBackground,
                Padding = ScaledPadding(0, 10, 0, 10)
            };
            for (var i = 0; i < 4; i++)
            {
                panel.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 25));
            }

            panel.Controls.Add(CreateMetricCard("地块数量", "当前识别编号", out _parcelCountValue), 0, 0);
            panel.Controls.Add(CreateMetricCard("用地面积", "规划用地汇总", out _landAreaValue), 1, 0);
            panel.Controls.Add(CreateMetricCard("计容面积", "按类型汇总", out _capacityAreaValue), 2, 0);
            panel.Controls.Add(CreateMetricCard("绿地率", "当前复核比例", out _greenRateValue), 3, 0);
            return panel;
        }

        private WinForms.Control CreateLeftConsole()
        {
            var scroll = new WinForms.Panel
            {
                Dock = WinForms.DockStyle.Fill,
                AutoScroll = true,
                BackColor = ConsoleBackground,
                Padding = ScaledPadding(0, 0, 8, 0)
            };

            var stack = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ConsoleBackground,
                MinimumSize = new Size(Scale(LeftPanelMinWidth - 24), 0)
            };
            stack.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 100));
            stack.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(480)));
            stack.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(610)));
            stack.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(300)));
            scroll.Controls.Add(stack);

            stack.Controls.Add(CreateSectionCard("指标规则区", CreateRulesContent(), "绿地、非机动车、机动车与绿化折算系数"), 0, 0);
            stack.Controls.Add(CreateSectionCard("图层识别区", CreateLayerContent(), "保持默认图层即可直接识别标准制图"), 0, 1);
            stack.Controls.Add(CreateSectionCard("标注输出区", CreateInfoLabelContent(), "写入建筑轮廓内的 1xx 图层文字"), 0, 2);
            return scroll;
        }

        private WinForms.Control CreateRulesContent()
        {
            var container = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = ScaledPadding(12, 2, 12, 10),
                BackColor = CardBackground
            };
            container.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(220)));
            container.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(100)));
            container.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));

            var ruleGrid = CreateRulePanel();
            ruleGrid.Padding = new WinForms.Padding(0);
            container.Controls.Add(ruleGrid, 0, 0);
            _greenMinBox = AddRuleTextBox(ruleGrid, 0, 0, "绿地率下限（%）", FormatPercentText(Settings.GreenRateMin));
            _greenMaxBox = AddRuleTextBox(ruleGrid, 1, 0, "绿地率上限（%）", FormatPercentText(Settings.GreenRateMax));
            _bikePer100Box = AddRuleTextBox(ruleGrid, 0, 1, "非机动车位数 / 100㎡", FormatText(Settings.BikeSpacesPer100));
            _bikeAreaBox = AddRuleTextBox(ruleGrid, 1, 1, "每个非机动车位折算面积（㎡）", FormatText(Settings.BikeAreaPerSpace));
            _motorPer100Box = AddRuleTextBox(ruleGrid, 0, 2, "机动车位数 / 100㎡", FormatText(Settings.MotorSpacesPer100));

            var factorGrid = CreateGridPanel(4, 1);
            factorGrid.Padding = new WinForms.Padding(0);
            container.Controls.Add(factorGrid, 0, 1);
            _grassPaverFactorBox = AddTextBox(factorGrid, 0, 0, "植草砖系数", FormatText(Settings.GrassPaverFactor));
            _roofGreen1FactorBox = AddTextBox(factorGrid, 1, 0, "屋绿1系数", FormatText(Settings.RoofGreen1Factor));
            _roofGreen2FactorBox = AddTextBox(factorGrid, 2, 0, "屋绿2系数", FormatText(Settings.RoofGreen2Factor));
            _roofGreen3FactorBox = AddTextBox(factorGrid, 3, 0, "屋绿3系数", FormatText(Settings.RoofGreen3Factor));

            container.Controls.Add(new WinForms.Label
            {
                Text = "绿地率可填 10 或 10% 或 0.1，均表示 10%。非机动车最低面积 = 折算面积 / 100 × 位数 / 100㎡ × 每位面积；机动车（个）= 折算面积 / 100 × 机动车位数 / 100㎡。",
                Dock = WinForms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = MutedTextColor,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Padding = ScaledPadding(2, 4, 2, 0)
            }, 0, 2);
            return container;
        }

        private WinForms.Control CreateLayerContent()
        {
            var layerGrid = CreateGridPanel(2, 6);
            layerGrid.Padding = ScaledPadding(12, 4, 12, 10);
            var unitPanel = new WinForms.FlowLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                FlowDirection = WinForms.FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = CardBackground,
                Padding = ScaledPadding(0, 6, 0, 0)
            };
            _unitMmButton = new WinForms.RadioButton { Text = "MM", AutoSize = true, Checked = !string.Equals(Settings.UnitMode, "M", StringComparison.OrdinalIgnoreCase) };
            _unitMButton = new WinForms.RadioButton { Text = "M", AutoSize = true, Checked = string.Equals(Settings.UnitMode, "M", StringComparison.OrdinalIgnoreCase) };
            unitPanel.Controls.Add(_unitMmButton);
            unitPanel.Controls.Add(_unitMButton);
            AddLabeledControl(layerGrid, 0, 0, "图纸单位", unitPanel);

            _parcelLayerBox = AddTextBox(layerGrid, 1, 0, "用地范围 PLINE", Settings.ParcelLayer);
            _labelLayerBox = AddTextBox(layerGrid, 0, 1, "地块编号文字", Settings.LabelLayer);
            _buildingLayerBox = AddTextBox(layerGrid, 1, 1, "建筑基底 PLINE", Settings.BuildingLayer);
            _bikeLayerBox = AddTextBox(layerGrid, 0, 2, "非机动车 PLINE", Settings.BikeLayer);
            _greenLayerBox = AddTextBox(layerGrid, 1, 2, "绿地 PLINE", Settings.GreenLayer);
            _floorLayerBox = AddTextBox(layerGrid, 0, 3, "层数文字", Settings.FloorLayer);
            _grassPaverLayerBox = AddTextBox(layerGrid, 1, 3, "植草砖 PLINE", Settings.GrassPaverLayer);
            _roofGreen1LayerBox = AddTextBox(layerGrid, 0, 4, "屋面绿化1 PLINE", Settings.RoofGreen1Layer);
            _roofGreen2LayerBox = AddTextBox(layerGrid, 1, 4, "屋面绿化2 PLINE", Settings.RoofGreen2Layer);
            _roofGreen3LayerBox = AddTextBox(layerGrid, 0, 5, "屋面绿化3 PLINE", Settings.RoofGreen3Layer);
            return layerGrid;
        }

        private WinForms.Control CreateInfoLabelContent()
        {
            var infoLabelGrid = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 3,
                Padding = ScaledPadding(12, 6, 12, 10),
                BackColor = CardBackground
            };
            infoLabelGrid.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 34));
            infoLabelGrid.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 33));
            infoLabelGrid.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 33));
            infoLabelGrid.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(48)));
            infoLabelGrid.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(InputRowHeight)));
            infoLabelGrid.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            _showInfoLabelsBox = new WinForms.CheckBox
            {
                Text = "将指标信息显示到建筑上",
                Checked = Settings.ShowBuildingInfoLabels,
                Dock = WinForms.DockStyle.Fill,
                AutoSize = true,
                ForeColor = TextColor,
                Margin = ScaledPadding(4, 8, 8, 4)
            };
            infoLabelGrid.Controls.Add(_showInfoLabelsBox, 0, 0);
            infoLabelGrid.SetColumnSpan(_showInfoLabelsBox, 3);

            _infoLabelTextHeightBox = AddOutputTextBox(infoLabelGrid, 0, "建筑标注文字高度", FormatText(Settings.InfoLabelTextHeight));
            _summaryTableTextHeightBox = AddOutputTextBox(infoLabelGrid, 1, "总指标表字体高度", FormatText(Settings.SummaryTableTextHeight));
            _detailTableTextHeightBox = AddOutputTextBox(infoLabelGrid, 2, "分地块明细表字体高度", FormatText(Settings.DetailTableTextHeight));

            infoLabelGrid.Controls.Add(new WinForms.Label
            {
                Text = "建筑标注写入 1xx 图层；两个表格字体高度会直接影响 CAD 中生成表格的文字大小。",
                Dock = WinForms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = MutedTextColor,
                Margin = ScaledPadding(0, 8, 4, 4)
            }, 0, 2);
            infoLabelGrid.SetColumnSpan(infoLabelGrid.GetControlFromPosition(0, 2), 3);
            return infoLabelGrid;
        }

        private WinForms.Control CreateReviewConsole(IEnumerable<AreaOverrideSeed> seeds)
        {
            var tabs = new WinForms.TabControl
            {
                Dock = WinForms.DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9.5F)
            };
            var detailPage = new WinForms.TabPage("分地块明细");
            var summaryPage = new WinForms.TabPage("总指标");
            var areaPage = new WinForms.TabPage("折算面积");
            tabs.TabPages.Add(detailPage);
            tabs.TabPages.Add(summaryPage);
            tabs.TabPages.Add(areaPage);

            _detailGrid = CreateDetailGrid();
            detailPage.Controls.Add(_detailGrid);
            PopulateDetailRows();

            _summaryGrid = CreateSummaryGrid();
            summaryPage.Controls.Add(_summaryGrid);
            
            _areaGrid = CreateAreaGrid();
            areaPage.Controls.Add(_areaGrid);
            PopulateAreaRows(seeds);
            PopulateSummaryRows();

            return CreateSectionCard("指标复核表", tabs, "可在表格内手动修正类型、面积、车位与汇总分项");
        }

        private WinForms.Control CreateButtonBar()
        {
            var panel = new WinForms.FlowLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                FlowDirection = WinForms.FlowDirection.RightToLeft,
                Padding = ScaledPadding(0, 14, 0, 0),
                BackColor = ConsoleBackground
            };

            var writeButton = CreateActionButton("写入CAD", true);
            writeButton.DialogResult = WinForms.DialogResult.None;
            writeButton.Click += OnOkClick;
            var previewButton = CreateActionButton("预览标注", false);
            previewButton.Click += OnPreviewLabelsClick;
            var cancelButton = CreateActionButton("取消", false);
            cancelButton.DialogResult = WinForms.DialogResult.Cancel;
            panel.Controls.Add(writeButton);
            panel.Controls.Add(previewButton);
            panel.Controls.Add(cancelButton);
            AcceptButton = writeButton;
            CancelButton = cancelButton;
            return panel;
        }

        private static BorderPanel CreateCardShell()
        {
            return new BorderPanel
            {
                Dock = WinForms.DockStyle.Fill,
                BackColor = CardBackground,
                BorderColor = BorderColor,
                Margin = ScaledPadding(0, 0, 0, 10)
            };
        }

        private static WinForms.Control CreateSectionCard(string title, WinForms.Control content, string subtitle)
        {
            var card = CreateCardShell();
            card.Padding = new WinForms.Padding(0);
            var layout = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = CardBackground
            };
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(SectionHeaderHeight)));
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            card.Controls.Add(layout);

            var header = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = ScaledPadding(14, 8, 14, 2),
                BackColor = CardBackground
            };
            header.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 58));
            header.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 42));
            header.Controls.Add(new WinForms.Label
            {
                Text = title,
                Dock = WinForms.DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            header.Controls.Add(new WinForms.Label
            {
                Text = subtitle ?? "",
                Dock = WinForms.DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            layout.Controls.Add(header, 0, 0);
            content.Dock = WinForms.DockStyle.Fill;
            layout.Controls.Add(content, 0, 1);
            return card;
        }

        private static WinForms.Control CreateMetricCard(string title, string subtitle, out WinForms.Label valueLabel)
        {
            var card = CreateCardShell();
            card.Margin = ScaledPadding(0, 0, 10, 0);
            card.Padding = ScaledPadding(16, 10, 16, 10);
            var layout = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = CardBackground
            };
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(28)));
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            layout.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(26)));
            card.Controls.Add(layout);

            layout.Controls.Add(new WinForms.Label
            {
                Text = title,
                Dock = WinForms.DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Microsoft YaHei UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            valueLabel = new WinForms.Label
            {
                Text = "-",
                Dock = WinForms.DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(valueLabel, 0, 1);
            layout.Controls.Add(new WinForms.Label
            {
                Text = subtitle,
                Dock = WinForms.DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);
            return card;
        }

        private static WinForms.Button CreateActionButton(string text, bool primary)
        {
            var button = new WinForms.Button
            {
                Text = text,
                Width = Scale(primary ? 112 : 96),
                Height = Scale(38),
                BackColor = primary ? PrimaryColor : CardBackground,
                ForeColor = primary ? Color.White : TextColor,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular),
                Margin = ScaledPadding(8, 0, 0, 0),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = primary ? PrimaryColor : BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? ColorTranslator.FromHtml("#1D4ED8") : HeaderBackground;
            return button;
        }

        private static WinForms.TableLayoutPanel CreateGridPanel(int columns, int rows)
        {
            var panel = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = columns,
                RowCount = rows,
                Padding = ScaledPadding(8),
                BackColor = CardBackground,
                MinimumSize = new Size(0, Scale(rows * InputRowHeight + 16))
            };
            for (var i = 0; i < columns; i++)
            {
                panel.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 100f / columns));
            }

            for (var i = 0; i < rows; i++)
            {
                panel.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100f / rows));
            }

            return panel;
        }

        private static WinForms.TableLayoutPanel CreateRulePanel()
        {
            var panel = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = ScaledPadding(0),
                BackColor = CardBackground,
                MinimumSize = new Size(0, Scale(3 * InputRowHeight))
            };
            panel.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 50));
            panel.ColumnStyles.Add(new WinForms.ColumnStyle(WinForms.SizeType.Percent, 50));
            panel.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 33.3F));
            panel.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 33.3F));
            panel.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 33.4F));
            return panel;
        }

        private static WinForms.TextBox AddRuleTextBox(WinForms.TableLayoutPanel panel, int column, int row, string label, string value)
        {
            var inner = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = ScaledPadding(0, 0, 12, 4),
                BackColor = CardBackground,
                MinimumSize = new Size(0, Scale(InputRowHeight))
            };
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(InputLabelHeight)));
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            inner.Controls.Add(new WinForms.Label
            {
                Text = label,
                Dock = WinForms.DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 8.4F),
                AutoEllipsis = true,
                Margin = ScaledPadding(0)
            }, 0, 0);

            var box = CreateInputTextBox(value);
            box.Margin = ScaledPadding(0, 2, 0, 2);
            inner.Controls.Add(box, 0, 1);
            panel.Controls.Add(inner, column, row);
            return box;
        }

        private static void AddLabeledControl(WinForms.TableLayoutPanel panel, int column, int row, string label, WinForms.Control control)
        {
            var inner = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = ScaledPadding(4),
                MinimumSize = new Size(0, Scale(InputRowHeight))
            };
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(InputLabelHeight)));
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            inner.Controls.Add(new WinForms.Label
            {
                Text = label,
                Dock = WinForms.DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 8.6F)
            }, 0, 0);
            control.Margin = ScaledPadding(0, 2, 0, 0);
            inner.Controls.Add(control, 0, 1);
            panel.Controls.Add(inner, column, row);
        }

        private static WinForms.TextBox AddTextBox(WinForms.TableLayoutPanel panel, int column, int row, string label, string value)
        {
            var box = CreateInputTextBox(value);
            AddLabeledControl(panel, column, row, label, box);
            return box;
        }

        private static WinForms.TextBox AddOutputTextBox(WinForms.TableLayoutPanel panel, int column, string label, string value)
        {
            var box = CreateInputTextBox(value);
            var inner = new WinForms.TableLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = ScaledPadding(0, 0, 12, 2),
                BackColor = CardBackground,
                MinimumSize = new Size(0, Scale(InputRowHeight))
            };
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Absolute, Scale(InputLabelHeight)));
            inner.RowStyles.Add(new WinForms.RowStyle(WinForms.SizeType.Percent, 100));
            inner.Controls.Add(new WinForms.Label
            {
                Text = label,
                Dock = WinForms.DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 8.4F),
                AutoEllipsis = true
            }, 0, 0);
            box.Margin = ScaledPadding(0, 2, 0, 2);
            inner.Controls.Add(box, 0, 1);
            panel.Controls.Add(inner, column, 1);
            return box;
        }

        private static WinForms.TextBox CreateInputTextBox(string value)
        {
            return new WinForms.TextBox
            {
                Dock = WinForms.DockStyle.Fill,
                Text = value,
                AutoSize = false,
                Multiline = false,
                Height = Scale(InputBoxHeight),
                MinimumSize = ScaledSize(96, InputBoxHeight),
                BorderStyle = WinForms.BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = TextColor,
                BackColor = Color.White
            };
        }

        private WinForms.DataGridView CreateAreaGrid()
        {
            var grid = new WinForms.DataGridView
            {
                Dock = WinForms.DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = WinForms.DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = WinForms.DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Number", HeaderText = "编号", FillWeight = 18 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "DefaultArea", HeaderText = "默认计容面积㎡", ReadOnly = true, FillWeight = 22 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "BikeArea", HeaderText = "非机动车折算面积㎡", FillWeight = 30 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "MotorArea", HeaderText = "机动车折算面积㎡", FillWeight = 30 });
            StyleGrid(grid);
            return grid;
        }

        private WinForms.DataGridView CreateDetailGrid()
        {
            var grid = new WinForms.DataGridView
            {
                Dock = WinForms.DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = WinForms.DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = WinForms.DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Number", HeaderText = "编号", ReadOnly = true });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Type", HeaderText = "类型" });
            AddManualPair(grid, "Base", "基底面积㎡");
            AddManualPair(grid, "Capacity", "计容面积㎡");
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "NonCapacity", HeaderText = "不计容面积㎡" });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Building", HeaderText = "建筑面积㎡", ReadOnly = true });
            AddManualPair(grid, "Land", "用地面积㎡");
            AddManualPair(grid, "Green", "绿地㎡");
            AddManualPair(grid, "Bike", "非机动车㎡");
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Floors", HeaderText = "层数" });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Ground", HeaderText = "地面" });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Indoor", HeaderText = "室内" });
            grid.CellValueChanged += (s, e) =>
            {
                UpdateDetailGridBuildingArea(e.RowIndex);
                RefreshSummaryGrid();
            };
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                {
                    grid.CommitEdit(WinForms.DataGridViewDataErrorContexts.Commit);
                }
            };
            StyleGrid(grid);
            return grid;
        }

        private static void AddManualPair(WinForms.DataGridView grid, string prefix, string header)
        {
            grid.Columns.Add(new WinForms.DataGridViewCheckBoxColumn { Name = prefix + "Manual", HeaderText = "手动" });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = prefix, HeaderText = header });
        }

        private WinForms.DataGridView CreateSummaryGrid()
        {
            var grid = new WinForms.DataGridView
            {
                Dock = WinForms.DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = WinForms.DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = WinForms.DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Visible = false });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Parent", HeaderText = "归属", FillWeight = 26 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Item", HeaderText = "项目", FillWeight = 32 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "SubItem", HeaderText = "分项", FillWeight = 24 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "单位", FillWeight = 12 });
            grid.Columns.Add(new WinForms.DataGridViewCheckBoxColumn { Name = "Manual", HeaderText = "手动", FillWeight = 10 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Value", HeaderText = "数值", FillWeight = 22 });
            grid.Columns.Add(new WinForms.DataGridViewTextBoxColumn { Name = "Remark", HeaderText = "备注", FillWeight = 28 });
            grid.DefaultValuesNeeded += (s, e) =>
            {
                e.Row.Tag = true;
                e.Row.Cells["Parent"].Value = "总建筑面积";
                e.Row.Cells["Item"].Value = "其中";
                e.Row.Cells["Unit"].Value = "㎡";
                e.Row.Cells["Manual"].Value = true;
                e.Row.Cells["Value"].Value = "0";
                e.Row.Cells["Key"].Value = "";
            };
            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count)
                {
                    var row = grid.Rows[e.RowIndex];
                    var key = Convert.ToString(row.Cells["Key"].Value);
                    if (string.IsNullOrWhiteSpace(key) || key.EndsWith("||", StringComparison.Ordinal))
                    {
                        row.Cells["Key"].Value = SummaryKey(
                            Convert.ToString(row.Cells["Parent"].Value),
                            Convert.ToString(row.Cells["Item"].Value),
                            Convert.ToString(row.Cells["SubItem"].Value));
                    }
                }
            };
            StyleGrid(grid);
            return grid;
        }

        private static void StyleGrid(WinForms.DataGridView grid)
        {
            grid.BackgroundColor = CardBackground;
            grid.BorderStyle = WinForms.BorderStyle.None;
            grid.GridColor = BorderColor;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersWidth = Scale(28);
            grid.RowTemplate.Height = Scale(28);
            grid.ColumnHeadersHeight = Scale(32);
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = CardBackground;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
        }

        private void PopulateDetailRows()
        {
            foreach (var row in _previewRows)
            {
                Settings.DetailOverrides.TryGetValue(row.Number ?? "", out var detail);
                var capacity = detail != null && detail.CapacityAreaManual ? detail.CapacityArea : row.CapacityArea;
                var nonCapacity = detail != null ? detail.NonCapacityArea : row.NonCapacityArea;
                _detailGrid.Rows.Add(
                    row.Number,
                    detail != null ? detail.Type ?? "" : row.Type ?? "",
                    detail?.BaseAreaManual ?? false,
                    FormatText(detail != null && detail.BaseAreaManual ? detail.BaseArea : row.BaseArea),
                    detail?.CapacityAreaManual ?? false,
                    FormatText(capacity),
                    FormatText(nonCapacity),
                    FormatText(capacity + nonCapacity),
                    detail?.LandAreaManual ?? false,
                    FormatText(detail != null && detail.LandAreaManual ? detail.LandArea : row.LandArea),
                    detail?.GreenAreaManual ?? false,
                    FormatText(detail != null && detail.GreenAreaManual ? detail.GreenArea : row.GreenArea),
                    detail?.BikeAreaManual ?? false,
                    FormatText(detail != null && detail.BikeAreaManual ? detail.BikeArea : row.BikeArea),
                    FormatText(detail != null && detail.Floors > 0 ? detail.Floors : row.Floors),
                    FormatText(detail != null ? detail.GroundParking : row.GroundParking),
                    FormatText(detail != null ? detail.IndoorParking : row.IndoorParking));
            }
        }

        private void PopulateSummaryRows()
        {
            RefreshSummaryGrid();
        }

        private void RefreshSummaryGrid()
        {
            if (_summaryGrid == null)
            {
                return;
            }

            CaptureSummaryGridRows(Settings);
            var rows = GetCurrentDetailRows();
            _summaryGrid.Rows.Clear();
            var totalLand = rows.Sum(x => x.LandArea);
            var totalBase = rows.Sum(x => x.BaseArea);
            var totalGreen = rows.Sum(x => x.GreenArea);
            var totalBike = rows.Sum(x => x.BikeArea);
            var totalGround = rows.Sum(x => x.GroundParking);
            var totalIndoor = rows.Sum(x => x.IndoorParking);
            UpdateSummaryCards(rows, totalLand, totalGreen);

            var buildingSubItems = BuildSummarySubItems(
                "总建筑面积",
                rows.Where(x => !string.IsNullOrWhiteSpace(x.Type))
                    .GroupBy(x => x.Type.Trim())
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.BuildingArea), StringComparer.OrdinalIgnoreCase));
            var capacitySubItems = BuildSummarySubItems(
                "计容总建筑面积",
                rows.Where(x => !string.IsNullOrWhiteSpace(x.Type))
                    .GroupBy(x => x.Type.Trim())
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.CapacityArea), StringComparer.OrdinalIgnoreCase));

            var totalBuilding = buildingSubItems.Count > 0 ? buildingSubItems.Sum(x => x.Value) : rows.Sum(x => x.BuildingArea);
            var totalCapacity = capacitySubItems.Count > 0 ? capacitySubItems.Sum(x => x.Value) : rows.Sum(x => x.CapacityArea);

            AddSummaryGridRow("", "总建筑面积", "", "㎡", totalBuilding, "", false);
            foreach (var item in buildingSubItems)
            {
                AddSummaryGridRow(item.Parent, item.Item, item.SubItem, item.Unit, item.Value, item.Remark, item.Manual, item.Custom);
            }

            AddSummaryGridRow("", "计容总建筑面积", "", "㎡", totalCapacity, "", false);
            foreach (var item in capacitySubItems)
            {
                AddSummaryGridRow(item.Parent, item.Item, item.SubItem, item.Unit, item.Value, item.Remark, item.Manual, item.Custom);
            }

            AddSummaryGridRow("", "规划总用地面积", "", "㎡", totalLand, "");
            AddSummaryGridRow("", "规划净用地面积", "", "㎡", totalLand, "");
            AddSummaryGridRow("", "容积率", "", "", totalLand > 0 ? totalCapacity / totalLand : 0, "");
            AddSummaryGridRow("", "建筑基底总面积", "", "㎡", totalBase, "");
            AddSummaryGridRow("", "建筑密度", "", "%", totalLand > 0 ? totalBase / totalLand * 100.0 : 0, "");
            AddSummaryGridRow("", "总绿化面积", "", "㎡", totalGreen, "");
            AddSummaryGridRow("", "绿地率", "", "%", totalLand > 0 ? totalGreen / totalLand * 100.0 : 0, "");
            AddSummaryGridRow("", "机动车停车位数", "", "个", totalGround + totalIndoor, "");
            AddSummaryGridRow("", "其中", "地面车位", "个", totalGround, "");
            AddSummaryGridRow("", "", "室内车位", "个", totalIndoor, "");
            AddSummaryGridRow("", "非机动车停车数", "", "㎡", totalBike, "");
            AddSummaryGridRow("", "配套占总计容面积比例", "", "%", 0, "/");
            AddSummaryGridRow("", "配套占总用地面积比例", "", "%", 0, "/");
        }

        private void UpdateSummaryCards(List<LandIndexPreviewRow> rows, double totalLand, double totalGreen)
        {
            if (_parcelCountValue == null)
            {
                return;
            }

            _parcelCountValue.Text = rows.Count.ToString();
            _landAreaValue.Text = FormatText(totalLand) + " ㎡";
            _capacityAreaValue.Text = FormatText(rows.Sum(x => x.CapacityArea)) + " ㎡";
            _greenRateValue.Text = totalLand > 0
                ? FormatPercentText(totalGreen / totalLand) + "%"
                : "-";
        }

        private List<SummaryOverride> BuildSummarySubItems(string parent, Dictionary<string, double> automaticValues)
        {
            var result = new Dictionary<string, SummaryOverride>(StringComparer.OrdinalIgnoreCase);
            var activeTypes = new HashSet<string>(automaticValues.Keys, StringComparer.OrdinalIgnoreCase);
            var staleKeys = new List<string>();
            foreach (var item in Settings.SummaryOverrides.Values.Where(x => string.Equals(x.Parent, parent, StringComparison.OrdinalIgnoreCase)))
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

                result[subItem] = new SummaryOverride
                {
                    Key = item.Key,
                    Parent = parent,
                    Item = string.IsNullOrWhiteSpace(item.Item) ? "其中" : item.Item,
                    SubItem = subItem,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "㎡" : item.Unit,
                    Manual = item.Manual,
                    Value = item.Manual ? item.Value : item.Value,
                    Remark = item.Remark,
                    Custom = item.Custom
                };
            }

            foreach (var key in staleKeys)
            {
                Settings.SummaryOverrides.Remove(key);
            }

            foreach (var pair in automaticValues)
            {
                if (!result.TryGetValue(pair.Key, out var item))
                {
                    result[pair.Key] = new SummaryOverride
                    {
                        Key = SummaryKey(parent, "其中", pair.Key),
                        Parent = parent,
                        Item = "其中",
                        SubItem = pair.Key,
                        Unit = "㎡",
                        Manual = false,
                        Value = pair.Value,
                        Remark = "",
                        Custom = false
                    };
                    continue;
                }

                if (!item.Manual)
                {
                    item.Value = pair.Value;
                }
            }

            return result.Values
                .OrderBy(x => x.SubItem, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void AddSummaryGridRow(string parent, string item, string subItem, string unit, double defaultValue, string remark, bool manual = false, bool custom = false)
        {
            var key = SummaryKey(parent, item, subItem);
            Settings.SummaryOverrides.TryGetValue(key, out var summaryOverride);
            if (summaryOverride != null)
            {
                parent = string.IsNullOrWhiteSpace(summaryOverride.Parent) ? parent : summaryOverride.Parent;
                item = string.IsNullOrWhiteSpace(summaryOverride.Item) ? item : summaryOverride.Item;
                subItem = string.IsNullOrWhiteSpace(summaryOverride.SubItem) ? subItem : summaryOverride.SubItem;
                unit = string.IsNullOrWhiteSpace(summaryOverride.Unit) ? unit : summaryOverride.Unit;
                manual = summaryOverride.Manual;
                if (summaryOverride.Manual)
                {
                    defaultValue = summaryOverride.Value;
                }
                remark = string.IsNullOrWhiteSpace(summaryOverride.Remark) ? remark : summaryOverride.Remark;
                custom = summaryOverride.Custom;
            }

            _summaryGrid.Rows.Add(
                key,
                parent,
                item,
                subItem,
                unit,
                manual,
                FormatText(defaultValue),
                remark);
            var row = _summaryGrid.Rows[_summaryGrid.Rows.Count - 1];
            row.Tag = custom;
        }

        private void CaptureSummaryGridRows(LandIndexSettings settings)
        {
            if (_summaryGrid == null || settings == null)
            {
                return;
            }

            settings.SummaryOverrides.Clear();
            foreach (WinForms.DataGridViewRow row in _summaryGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var parent = Convert.ToString(row.Cells["Parent"].Value)?.Trim() ?? "";
                var item = Convert.ToString(row.Cells["Item"].Value)?.Trim() ?? "";
                var subItem = Convert.ToString(row.Cells["SubItem"].Value)?.Trim() ?? "";
                var unit = Convert.ToString(row.Cells["Unit"].Value)?.Trim() ?? "";
                var key = Convert.ToString(row.Cells["Key"].Value)?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    key = SummaryKey(parent, item, subItem);
                }

                if (string.IsNullOrWhiteSpace(parent) &&
                    string.IsNullOrWhiteSpace(item) &&
                    string.IsNullOrWhiteSpace(subItem))
                {
                    continue;
                }

                var manual = GridBool(row, "Manual");
                var remark = Convert.ToString(row.Cells["Remark"].Value)?.Trim() ?? "";
                var custom = row.Tag is bool tagValue && tagValue;

                settings.SummaryOverrides[key] = new SummaryOverride
                {
                    Key = key,
                    Parent = parent,
                    Item = item,
                    SubItem = subItem,
                    Unit = unit,
                    Manual = manual,
                    Value = ParseGridNumber(row, "Value", 0),
                    Remark = remark,
                    Custom = custom
                };
            }
        }

        private List<LandIndexPreviewRow> GetCurrentDetailRows()
        {
            if (_detailGrid == null || _detailGrid.Rows.Count == 0)
            {
                return _previewRows.ToList();
            }

            var rows = new List<LandIndexPreviewRow>();
            foreach (WinForms.DataGridViewRow gridRow in _detailGrid.Rows)
            {
                if (gridRow.IsNewRow)
                {
                    continue;
                }

                var capacity = ParseGridNumber(gridRow, "Capacity", 0);
                var nonCapacity = ParseGridNumber(gridRow, "NonCapacity", 0);
                rows.Add(new LandIndexPreviewRow
                {
                    Number = Convert.ToString(gridRow.Cells["Number"].Value),
                    Type = Convert.ToString(gridRow.Cells["Type"].Value),
                    BaseArea = ParseGridNumber(gridRow, "Base", 0),
                    CapacityArea = capacity,
                    NonCapacityArea = nonCapacity,
                    BuildingArea = capacity + nonCapacity,
                    LandArea = ParseGridNumber(gridRow, "Land", 0),
                    GreenArea = ParseGridNumber(gridRow, "Green", 0),
                    BikeArea = ParseGridNumber(gridRow, "Bike", 0),
                    GroundParking = ParseGridNumber(gridRow, "Ground", 0),
                    IndoorParking = ParseGridNumber(gridRow, "Indoor", 0)
                });
            }

            return rows;
        }

        private void UpdateDetailGridBuildingArea(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _detailGrid.Rows.Count)
            {
                return;
            }

            var row = _detailGrid.Rows[rowIndex];
            var capacity = ParseGridNumber(row, "Capacity", 0);
            var nonCapacity = ParseGridNumber(row, "NonCapacity", 0);
            row.Cells["Building"].Value = FormatText(capacity + nonCapacity);
        }

        private void PopulateAreaRows(IEnumerable<AreaOverrideSeed> seeds)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var seed in seeds.OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(seed.Number) || !seen.Add(seed.Number))
                {
                    continue;
                }

                Settings.AreaOverrides.TryGetValue(seed.Number, out var overrideValue);
                _areaGrid.Rows.Add(
                    seed.Number,
                    seed.DefaultCalcArea.ToString("0.##"),
                    overrideValue?.BikeCalcArea?.ToString("0.##") ?? "",
                    overrideValue?.MotorCalcArea?.ToString("0.##") ?? "");
            }

            foreach (var item in Settings.AreaOverrides.Values.OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(item.Number) || !seen.Add(item.Number))
                {
                    continue;
                }

                _areaGrid.Rows.Add(
                    item.Number,
                    "",
                    item.BikeCalcArea?.ToString("0.##") ?? "",
                    item.MotorCalcArea?.ToString("0.##") ?? "");
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            Accept();
        }

        private void OnPreviewLabelsClick(object sender, EventArgs e)
        {
            var rows = GetCurrentDetailRows()
                .Where(x => !string.IsNullOrWhiteSpace(x.Number))
                .Take(5)
                .ToList();
            if (rows.Count == 0)
            {
                WinForms.MessageBox.Show("当前没有可预览的地块标注。", "预览标注", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                return;
            }

            var text = new System.Text.StringBuilder();
            text.AppendLine("标注将写入 1xx 图层，示例：");
            text.AppendLine();
            foreach (var row in rows)
            {
                var density = row.LandArea > 0 ? row.BaseArea / row.LandArea : 0;
                text.AppendLine("编号 " + row.Number);
                text.AppendLine("基底面积" + FormatText(row.BaseArea) + "㎡");
                text.AppendLine("计容面积" + FormatText(row.CapacityArea) + "㎡");
                text.AppendLine("用地面积" + FormatText(row.LandArea) + "㎡");
                text.AppendLine(FormatText(row.LandArea / 666.6666667) + "亩");
                text.AppendLine("建筑密度" + FormatPercentText(density) + "%");
                text.AppendLine();
            }

            WinForms.MessageBox.Show(text.ToString(), "预览标注", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
        }

        private void Accept()
        {
            var settings = Settings.Clone();
            settings.UnitMode = _unitMButton.Checked ? "M" : "MM";
            settings.ParcelLayer = _parcelLayerBox.Text;
            settings.LabelLayer = _labelLayerBox.Text;
            settings.BuildingLayer = _buildingLayerBox.Text;
            settings.BikeLayer = _bikeLayerBox.Text;
            settings.GreenLayer = _greenLayerBox.Text;
            settings.FloorLayer = _floorLayerBox.Text;
            settings.GrassPaverLayer = _grassPaverLayerBox.Text;
            settings.RoofGreen1Layer = _roofGreen1LayerBox.Text;
            settings.RoofGreen2Layer = _roofGreen2LayerBox.Text;
            settings.RoofGreen3Layer = _roofGreen3LayerBox.Text;

            double value;
            if (!TryParsePercent(_greenMinBox.Text, out value))
            {
                ShowError("绿地率下限必须是数字，例如 10、10% 或 0.1。");
                return;
            }
            settings.GreenRateMin = value;

            if (!TryParsePercent(_greenMaxBox.Text, out value))
            {
                ShowError("绿地率上限必须是数字，例如 35、35% 或 0.35。");
                return;
            }
            settings.GreenRateMax = value;

            if (!TryParseNonNegative(_bikePer100Box.Text, out value))
            {
                ShowError("非机动车位数 / 100㎡ 必须是非负数字。");
                return;
            }
            settings.BikeSpacesPer100 = value;

            if (!TryParseNonNegative(_bikeAreaBox.Text, out value))
            {
                ShowError("每个非机动车位折算面积必须是非负数字。");
                return;
            }
            settings.BikeAreaPerSpace = value;

            if (!TryParseNonNegative(_motorPer100Box.Text, out value))
            {
                ShowError("机动车位数 / 100㎡ 必须是非负数字。");
                return;
            }
            settings.MotorSpacesPer100 = value;

            if (!TryParseNonNegative(_grassPaverFactorBox.Text, out value) || value <= 0)
            {
                ShowError("植草砖折算系数必须是大于 0 的数字。");
                return;
            }
            settings.GrassPaverFactor = value;

            if (!TryParseNonNegative(_roofGreen1FactorBox.Text, out value) || value <= 0)
            {
                ShowError("屋面绿化1折算系数必须是大于 0 的数字。");
                return;
            }
            settings.RoofGreen1Factor = value;

            if (!TryParseNonNegative(_roofGreen2FactorBox.Text, out value) || value <= 0)
            {
                ShowError("屋面绿化2折算系数必须是大于 0 的数字。");
                return;
            }
            settings.RoofGreen2Factor = value;

            if (!TryParseNonNegative(_roofGreen3FactorBox.Text, out value) || value <= 0)
            {
                ShowError("屋面绿化3折算系数必须是大于 0 的数字。");
                return;
            }
            settings.RoofGreen3Factor = value;

            settings.ShowBuildingInfoLabels = _showInfoLabelsBox.Checked;
            if (!TryParseNonNegative(_infoLabelTextHeightBox.Text, out value) || value <= 0)
            {
                ShowError("建筑指标文字高度必须是大于 0 的数字。");
                return;
            }
            settings.InfoLabelTextHeight = value;

            if (!TryParseNonNegative(_summaryTableTextHeightBox.Text, out value) || value <= 0)
            {
                ShowError("总指标表字体高度必须是大于 0 的数字。");
                return;
            }
            settings.SummaryTableTextHeight = value;

            if (!TryParseNonNegative(_detailTableTextHeightBox.Text, out value) || value <= 0)
            {
                ShowError("分地块明细表字体高度必须是大于 0 的数字。");
                return;
            }
            settings.DetailTableTextHeight = value;

            settings.DetailOverrides.Clear();
            foreach (WinForms.DataGridViewRow row in _detailGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var number = Convert.ToString(row.Cells["Number"].Value)?.Trim();
                if (string.IsNullOrWhiteSpace(number))
                {
                    continue;
                }

                settings.DetailOverrides[number] = new DetailOverride
                {
                    Number = number,
                    Type = Convert.ToString(row.Cells["Type"].Value)?.Trim() ?? "",
                    BaseAreaManual = GridBool(row, "BaseManual"),
                    BaseArea = ParseGridNumber(row, "Base", 0),
                    CapacityAreaManual = GridBool(row, "CapacityManual"),
                    CapacityArea = ParseGridNumber(row, "Capacity", 0),
                    LandAreaManual = GridBool(row, "LandManual"),
                    LandArea = ParseGridNumber(row, "Land", 0),
                    GreenAreaManual = GridBool(row, "GreenManual"),
                    GreenArea = ParseGridNumber(row, "Green", 0),
                    BikeAreaManual = GridBool(row, "BikeManual"),
                    BikeArea = ParseGridNumber(row, "Bike", 0),
                    NonCapacityArea = ParseGridNumber(row, "NonCapacity", 0),
                    Floors = ParseGridNumber(row, "Floors", 0),
                    GroundParking = ParseGridNumber(row, "Ground", 0),
                    IndoorParking = ParseGridNumber(row, "Indoor", 0)
                };
            }

            CaptureSummaryGridRows(settings);

            settings.AreaOverrides.Clear();
            if (settings.GreenRateMax < settings.GreenRateMin)
            {
                ShowError("绿地率上限不能小于下限。");
                return;
            }

            foreach (WinForms.DataGridViewRow row in _areaGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var number = Convert.ToString(row.Cells["Number"].Value)?.Trim();
                if (string.IsNullOrWhiteSpace(number))
                {
                    continue;
                }

                var bike = ParseOptionalArea(row.Cells["BikeArea"].Value, "非机动车折算面积");
                if (bike.Invalid)
                {
                    return;
                }

                var motor = ParseOptionalArea(row.Cells["MotorArea"].Value, "机动车折算面积");
                if (motor.Invalid)
                {
                    return;
                }

                if (bike.Value.HasValue || motor.Value.HasValue)
                {
                    if (settings.AreaOverrides.ContainsKey(number))
                    {
                        ShowError("折算面积覆盖表中存在重复编号：" + number);
                        return;
                    }

                    settings.AreaOverrides[number] = new AreaOverride
                    {
                        Number = number,
                        BikeCalcArea = bike.Value,
                        MotorCalcArea = motor.Value
                    };
                }
            }

            settings.Normalize();
            Settings = settings;
            DialogResult = WinForms.DialogResult.OK;
            Close();
        }

        private OptionalArea ParseOptionalArea(object raw, string label)
        {
            var text = Convert.ToString(raw)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new OptionalArea();
            }

            double value;
            if (!TryParseNonNegative(text, out value))
            {
                ShowError(label + "必须为空或非负数字。");
                return new OptionalArea { Invalid = true };
            }

            return new OptionalArea { Value = value };
        }

        private static double ParseGridNumber(WinForms.DataGridViewRow row, string columnName, double fallback)
        {
            var text = Convert.ToString(row.Cells[columnName].Value)?.Trim();
            return string.IsNullOrWhiteSpace(text)
                ? fallback
                : LandIndexSettings.ParseDouble(text, fallback);
        }

        private static bool GridBool(WinForms.DataGridViewRow row, string columnName)
        {
            var value = row.Cells[columnName].Value;
            return value is bool boolValue && boolValue;
        }

        private static string SummaryKey(string parent, string item, string subItem)
        {
            return (parent ?? "").Trim() + "||" + (item ?? "").Trim() + "||" + (subItem ?? "").Trim();
        }

        private static bool TryParseNonNegative(string text, out double value)
        {
            value = LandIndexSettings.ParseDouble((text ?? "").Trim(), double.NaN);
            return !double.IsNaN(value) && value >= 0;
        }

        private static bool TryParsePercent(string text, out double ratio)
        {
            var clean = (text ?? "").Trim();
            var hasPercent = clean.EndsWith("%", StringComparison.Ordinal);
            if (hasPercent)
            {
                clean = clean.Substring(0, clean.Length - 1).Trim();
            }

            double value;
            if (!TryParseNonNegative(clean, out value))
            {
                ratio = 0;
                return false;
            }

            ratio = hasPercent || value > 1.0 ? value / 100.0 : value;
            return ratio >= 0 && ratio <= 1.0;
        }

        private static string FormatText(double value)
        {
            return value.ToString("0.###");
        }

        private static string FormatPercentText(double ratio)
        {
            return (ratio * 100.0).ToString("0.##");
        }

        private static void ShowError(string message)
        {
            WinForms.MessageBox.Show(message, "指标表设置", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
        }

        private struct OptionalArea
        {
            public bool Invalid;
            public double? Value;
        }

        private sealed class BorderPanel : WinForms.Panel
        {
            public Color BorderColor { get; set; } = Color.LightGray;

            public BorderPanel()
            {
                SetStyle(WinForms.ControlStyles.UserPaint | WinForms.ControlStyles.ResizeRedraw | WinForms.ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(WinForms.PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var pen = new Pen(BorderColor))
                {
                    var rect = ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
    }
}
