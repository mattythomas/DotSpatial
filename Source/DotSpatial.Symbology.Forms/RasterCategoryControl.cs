// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DotSpatial.Data;
using DotSpatial.Serialization;

namespace DotSpatial.Symbology.Forms
{
    /// <summary>
    /// Dialog for the 'unique values' feature symbol classification scheme.
    /// </summary>
    public partial class RasterCategoryControl : UserControl, ICategoryControl
    {
        #region Fields
        private int _activeCategoryIndex;
        private Timer _cleanupTimer;
        private int _dblClickEditIndex;
        private bool _ignoreEnter;

        private bool _ignoreRefresh;
        private bool _ignoreValidation;
        private IRasterLayer _newLayer;
        private IColorScheme _newScheme;
        private IRasterLayer _originalLayer;
        private IColorScheme _originalScheme;
        private ContextMenu _quickSchemes;
        private IRaster _raster;
        private IRasterSymbolizer _symbolizer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RasterCategoryControl"/> class.
        /// </summary>
        public RasterCategoryControl()
        {
            InitializeComponent();
            Configure();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RasterCategoryControl"/> class.
        /// </summary>
        /// <param name="layer">The raster layer that is used.</param>
        public RasterCategoryControl(IRasterLayer layer)
        {
            InitializeComponent();
            Configure();
            Initialize(layer);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the apply changes option has been triggered.
        /// </summary>
        public event EventHandler ChangesApplied;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Maximum value currently displayed in the graph.
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Gets or sets the Minimum value currently displayed in the graph.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets the current progress bar.
        /// </summary>
        public SymbologyProgressBar ProgressBar => mwProgressBar1;

        #endregion

        #region Methods

        /// <summary>
        /// Fires the apply changes situation externally, forcing the Table to
        /// write its values to the original layer.
        /// </summary>
        public void ApplyChanges()
        {
            OnApplyChanges();
        }

        /// <summary>
        /// Cancel the action.
        /// </summary>
        public void Cancel()
        {
            OnCancel();
        }

        /// <summary>
        /// Sets up the Table to work with the specified layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public void Initialize(IRasterLayer layer)
        {
            _originalLayer = layer;
            _newLayer = layer.Copy();
            _symbolizer = layer.Symbolizer;
            _newScheme = _symbolizer.Scheme;
            _originalScheme = (IColorScheme)_symbolizer.Scheme.Clone();
            _raster = layer.DataSet;
            GetSettings();
        }

        /// <summary>
        /// Initializes the specified layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public void Initialize(ILayer layer)
        {
            Initialize(layer as IRasterLayer);
        }

        /// <summary>
        /// Applies the changes that have been specified in this control.
        /// </summary>
        protected virtual void OnApplyChanges()
        {
            _originalLayer.Symbolizer = _newLayer.Symbolizer.Copy();
            _originalScheme = _newLayer.Symbolizer.Scheme.Copy();
            if (_originalLayer.Symbolizer.ShadedRelief.IsUsed)
            {
                if (_originalLayer.Symbolizer.ShadedRelief.HasChanged || _originalLayer.Symbolizer.HillShade == null)
                    _originalLayer.Symbolizer.CreateHillShade(mwProgressBar1);
            }

            _originalLayer.WriteBitmap(mwProgressBar1);
            ChangesApplied?.Invoke(_originalLayer, EventArgs.Empty);
        }

        /// <summary>
        /// Event that fires when the action is canceled.
        /// </summary>
        protected virtual void OnCancel()
        {
            _originalLayer.Symbolizer.Scheme = _originalScheme;
        }

        /// <summary>
        /// Handles the mouse wheel, allowing the breakSliderGraph to zoom in or out.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Point screenLoc = PointToScreen(e.Location);
            Point bsPoint = breakSliderGraph1.PointToClient(screenLoc);
            if (breakSliderGraph1.ClientRectangle.Contains(bsPoint))
            {
                breakSliderGraph1.DoMouseWheel(e.Delta, bsPoint.X);
                return;
            }

            base.OnMouseWheel(e);
        }

        private void AngLightDirectionAngleChanged(object sender, EventArgs e)
        {
            _symbolizer.ShadedRelief.LightDirection = angLightDirection.Angle;
        }

        private void BreakSliderGraph1SliderMoved(object sender, BreakSliderEventArgs e)
        {
            _ignoreRefresh = true;
            cmbInterval.SelectedItem = IntervalMethod.Manual;
            _ignoreRefresh = false;
            _symbolizer.EditorSettings.IntervalMethod = IntervalMethod.Manual;
            int index = _newScheme.Categories.IndexOf(e.Slider.Category as IColorCategory);
            if (index == -1) return;
            UpdateTable();
            dgvCategories.Rows[index].Selected = true;
        }

        private void BreakSliderGraph1SliderSelected(object sender, BreakSliderEventArgs e)
        {
            int index = breakSliderGraph1.Breaks.IndexOf(e.Slider);
            dgvCategories.Rows[index].Selected = true;
        }

        private void BtnAddClick(object sender, EventArgs e)
        {
            nudCategoryCount.Value += 1;
        }

        private void BtnDeleteClick(object sender, EventArgs e)
        {
            if (dgvCategories.SelectedRows.Count == 0) return;
            List<IColorCategory> deleteList = new List<IColorCategory>();
            ColorCategoryCollection categories = _newScheme.Categories;
            int count = 0;
            foreach (DataGridViewRow row in dgvCategories.SelectedRows)
            {
                int index = dgvCategories.Rows.IndexOf(row);
                deleteList.Add(categories[index]);
                count++;
            }

            foreach (IColorCategory category in deleteList)
            {
                int index = categories.IndexOf(category);
                if (index > 0 && index < categories.Count - 1)
                {
                    categories[index - 1].Maximum = categories[index + 1].Minimum;
                    categories[index - 1].ApplyMinMax(_newScheme.EditorSettings);
                }

                _newScheme.RemoveCategory(category);
                breakSliderGraph1.UpdateBreaks();
            }

            UpdateTable();
            _newScheme.EditorSettings.IntervalMethod = IntervalMethod.Manual;
            _newScheme.EditorSettings.NumBreaks -= count;
            UpdateStatistics(false);
        }

        private void BtnElevationClick(object sender, EventArgs e)
        {
            _elevationQuickPick.Show(grpHillshade, new Point(dbxElevationFactor.Left, btnElevation.Bottom));
        }

        private void BtnQuickClick(object sender, EventArgs e)
        {
            _quickSchemes.Show(btnQuick, new Point(0, 0));
        }

        private void BtnRampClick(object sender, EventArgs e)
        {
            _symbolizer.EditorSettings.RampColors = true;
            RefreshValues();
        }

        private void BtnShadedReliefClick(object sender, EventArgs e)
        {
            _shadedReliefDialog.PropertyGrid.SelectedObject = _symbolizer.ShadedRelief.Copy();
            _shadedReliefDialog.ShowDialog();
        }

        private void ChkHillshadeCheckedChanged(object sender, EventArgs e)
        {
            _symbolizer.ShadedRelief.IsUsed = chkHillshade.Checked;
        }

        private void ChkLogCheckedChanged(object sender, EventArgs e)
        {
            breakSliderGraph1.LogY = chkLog.Checked;
        }

        private void ChkShowMeanCheckedChanged(object sender, EventArgs e)
        {
            breakSliderGraph1.ShowMean = chkShowMean.Checked;
        }

        private void ChkShowStdCheckedChanged(object sender, EventArgs e)
        {
            breakSliderGraph1.ShowStandardDeviation = chkShowStd.Checked;
        }

        private void CleanupTimerTick(object sender, EventArgs e)
        {
            // When a row validation causes rows above the edit row to be removed,
            // we can't easily update the Table during the validation event.
            // The timer allows the validation to finish before updating the Table.
            _cleanupTimer.Stop();
            _ignoreValidation = true;
            UpdateTable();
            if (_activeCategoryIndex >= 0 && _activeCategoryIndex < dgvCategories.Rows.Count)
            {
                dgvCategories.Rows[_activeCategoryIndex].Selected = true;
            }

            _ignoreValidation = false;
            _ignoreEnter = false;
        }

        private void CmbIntervalSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_symbolizer == null) return;
            _symbolizer.EditorSettings.IntervalMethod = (IntervalMethod)cmbInterval.SelectedItem;
            RefreshValues();
        }

        private void CmbIntervalSnappingSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_newScheme == null) return;
            IntervalSnapMethod method = (IntervalSnapMethod)cmbIntervalSnapping.SelectedItem;
            _newScheme.EditorSettings.IntervalSnapMethod = method;
            switch (method)
            {
                case IntervalSnapMethod.SignificantFigures:
                    lblSigFig.Visible = true;
                    nudSigFig.Visible = true;
                    nudSigFig.Minimum = 1;
                    lblSigFig.Text = SymbologyFormsMessageStrings.RasterCategoryControl_SignificantFigures;
                    break;
                case IntervalSnapMethod.Rounding:
                    nudSigFig.Visible = true;
                    lblSigFig.Visible = true;
                    nudSigFig.Minimum = 0;
                    lblSigFig.Text = SymbologyFormsMessageStrings.RasterCategoryControl_RoundingDigits;
                    break;
                case IntervalSnapMethod.None:
                    lblSigFig.Visible = false;
                    nudSigFig.Visible = false;
                    break;
                case IntervalSnapMethod.DataValue:
                    lblSigFig.Visible = false;
                    nudSigFig.Visible = false;
                    break;
            }

            RefreshValues();
        }

        private void CmdRefreshClick(object sender, EventArgs e)
        {
            _symbolizer.EditorSettings.RampColors = false;
            RefreshValues();
        }

        private void ColorNoDataColorChanged(object sender, EventArgs e)
        {
            _symbolizer.NoDataColor = colorNoData.Color;
        }

        private void Configure()
        {
            _elevationQuickPick = new ContextMenu();

            _elevationQuickPick.MenuItems.Add("Z Feet     | XY Lat Long", SetElevationFeetLatLong);
            _elevationQuickPick.MenuItems.Add("Z Feet     | XY Meters", SetElevationFeetMeters);
            _elevationQuickPick.MenuItems.Add("Z Feet     | XY Feet", SetElevationSameUnits);
            _elevationQuickPick.MenuItems.Add("Z Meters | XY Lat Long", SetElevationMetersLatLong);
            _elevationQuickPick.MenuItems.Add("Z Meters | XY Meters", SetElevationSameUnits);
            _elevationQuickPick.MenuItems.Add("Z Meters | XY Feet", SetElevationMetersFeet);
            dgvCategories.CellFormatting += DgvCategoriesCellFormatting;
            dgvCategories.CellDoubleClick += DgvCategoriesCellDoubleClick;
            dgvCategories.SelectionChanged += DgvCategoriesSelectionChanged;
            dgvCategories.CellValidated += DgvCategoriesCellValidated;
            dgvCategories.MouseDown += DgvCategoriesMouseDown;

            foreach (var enumValue in Enum.GetValues(typeof(IntervalMethod)))
            {
                cmbInterval.Items.Add(enumValue);
            }

            cmbInterval.SelectedItem = IntervalMethod.EqualInterval;

            breakSliderGraph1.SliderSelected += BreakSliderGraph1SliderSelected;
            _quickSchemes = new ContextMenu();
            string[] names = Enum.GetNames(typeof(ColorSchemeType));
            foreach (string name in names)
            {
                MenuItem mi = new MenuItem(name, QuickSchemeClicked);
                _quickSchemes.MenuItems.Add(mi);
            }

            cmbIntervalSnapping.Items.Clear();
            foreach (var item in Enum.GetValues(typeof(IntervalSnapMethod)))
            {
                cmbIntervalSnapping.Items.Add(item);
            }

            cmbIntervalSnapping.SelectedItem = IntervalSnapMethod.DataValue;
            _cleanupTimer = new Timer { Interval = 10 };
            _cleanupTimer.Tick += CleanupTimerTick;

            // Allows shaded Relief to be edited
            _shadedReliefDialog = new PropertyDialog();
            _shadedReliefDialog.ChangesApplied += PropertyDialogChangesApplied;
        }

        private void DbxElevationFactorTextChanged(object sender, EventArgs e)
        {
            _symbolizer.ShadedRelief.ElevationFactor = (float)dbxElevationFactor.Value;
        }

        private void DbxMaxTextChanged(object sender, EventArgs e)
        {
            _symbolizer.EditorSettings.Max = dbxMax.Value;
            _symbolizer.Scheme.CreateCategories(_raster);
            UpdateStatistics(true); // if the parameter is true, even on manual, the breaks are reset.
            UpdateTable();
        }

        private void DbxMinTextChanged(object sender, EventArgs e)
        {
            _symbolizer.EditorSettings.Min = dbxMin.Value;
            _symbolizer.Scheme.CreateCategories(_raster);
            UpdateStatistics(true); // if the parameter is true, even on manual, the breaks are reset.
            UpdateTable();
        }

        /// <summary>
        /// When the user double clicks the cell then we should display the detailed
        /// symbology dialog.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void DgvCategoriesCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int count = _newScheme.Categories.Count;
            if (e.ColumnIndex == 0 && e.RowIndex < count)
            {
                _dblClickEditIndex = e.RowIndex;
                _tabColorDialog = new TabColorDialog();
                _tabColorDialog.ChangesApplied += TabColorDialogChangesApplied;
                _tabColorDialog.StartColor = _newScheme.Categories[_dblClickEditIndex].LowColor;
                _tabColorDialog.EndColor = _newScheme.Categories[_dblClickEditIndex].HighColor;
                _tabColorDialog.Show(ParentForm);
            }
        }

        /// <summary>
        /// When the cell is formatted.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void DgvCategoriesCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_newScheme == null) return;
            int count = _newScheme.Categories.Count;
            if (count == 0) return;

            // Replace string values in the column with images.
            if (e.ColumnIndex != 0) return;
            Image img = e.Value as Image;
            if (img == null)
            {
                img = SymbologyFormsImages.info;
                e.Value = img;
            }

            Graphics g = Graphics.FromImage(img);
            g.Clear(Color.White);

            if (count > e.RowIndex)
            {
                Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
                _newScheme.DrawCategory(e.RowIndex, g, rect);
            }

            g.Dispose();
        }

        private void DgvCategoriesCellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (_ignoreValidation) return;
            if (_newScheme.Categories.Count <= e.RowIndex) return;

            if (e.ColumnIndex == 2)
            {
                IColorCategory fctxt = _newScheme.Categories[e.RowIndex];
                fctxt.LegendText = (string)dgvCategories[e.ColumnIndex, e.RowIndex].Value;
                return;
            }

            if (e.ColumnIndex != 1) return;

            IColorCategory cb = _newScheme.Categories[e.RowIndex];
            if ((string)dgvCategories[e.ColumnIndex, e.RowIndex].Value == cb.LegendText) return;
            _ignoreEnter = true;
            string exp = (string)dgvCategories[e.ColumnIndex, e.RowIndex].Value;
            cb.LegendText = exp;

            cb.Range = new Range(exp);
            if (cb.Range.Maximum != null && cb.Range.Maximum > _raster.Maximum)
            {
                cb.Range.Maximum = _raster.Maximum;
            }

            if (cb.Range.Minimum != null && cb.Range.Minimum > _raster.Maximum)
            {
                cb.Range.Minimum = _raster.Maximum;
            }

            if (cb.Range.Maximum != null && cb.Range.Minimum < _raster.Minimum)
            {
                cb.Range.Minimum = _raster.Minimum;
            }

            if (cb.Range.Minimum != null && cb.Range.Minimum < _raster.Minimum)
            {
                cb.Range.Minimum = _raster.Minimum;
            }

            cb.ApplyMinMax(_newScheme.EditorSettings);
            ColorCategoryCollection breaks = _newScheme.Categories;
            breaks.SuspendEvents();
            if (cb.Range.Minimum == null && cb.Range.Maximum == null)
            {
                breaks.Clear();
                breaks.Add(cb);
            }
            else if (cb.Range.Maximum == null)
            {
                List<IColorCategory> removeList = new List<IColorCategory>();

                int iPrev = e.RowIndex - 1;
                for (int i = 0; i < e.RowIndex; i++)
                {
                    // If the specified max is below the minima of a lower range, remove the lower range.
                    if (breaks[i].Minimum > cb.Minimum)
                    {
                        removeList.Add(breaks[i]);
                        iPrev--;
                    }
                    else if (breaks[i].Maximum > cb.Minimum || i == iPrev)
                    {
                        // otherwise, if the maximum of a lower range is higher than the value, adjust it.
                        breaks[i].Maximum = cb.Minimum;
                        breaks[i].ApplyMinMax(_symbolizer.EditorSettings);
                    }
                }

                for (int i = e.RowIndex + 1; i < breaks.Count; i++)
                {
                    // Since we have just assigned an absolute maximum, any previous categories
                    // that fell above the edited category should be removed.
                    removeList.Add(breaks[i]);
                }

                foreach (IColorCategory brk in removeList)
                {
                    // Do the actual removal.
                    breaks.Remove(brk);
                }
            }
            else if (cb.Range.Minimum == null)
            {
                List<IColorCategory> removeList = new List<IColorCategory>();

                int iNext = e.RowIndex + 1;
                for (int i = e.RowIndex + 1; i < breaks.Count; i++)
                {
                    // If the specified max is below the minima of a lower range, remove the lower range.
                    if (breaks[i].Maximum < cb.Maximum)
                    {
                        removeList.Add(breaks[i]);
                        iNext++;
                    }
                    else if (breaks[i].Minimum < cb.Maximum || i == iNext)
                    {
                        // otherwise, if the maximum of a lower range is higher than the value, adjust it.
                        breaks[i].Minimum = cb.Maximum;
                        breaks[i].ApplyMinMax(_symbolizer.EditorSettings);
                    }
                }

                for (int i = 0; i < e.RowIndex; i++)
                {
                    // Since we have just assigned an absolute minimum, any previous categories
                    // that fell above the edited category should be removed.
                    removeList.Add(breaks[i]);
                }

                foreach (IColorCategory brk in removeList)
                {
                    // Do the actual removal.
                    breaks.Remove(brk);
                }
            }
            else
            {
                // We have two values. Adjust any above or below that conflict.
                List<IColorCategory> removeList = new List<IColorCategory>();
                int iPrev = e.RowIndex - 1;
                for (int i = 0; i < e.RowIndex; i++)
                {
                    // If the specified max is below the minima of a lower range, remove the lower range.
                    if (breaks[i].Minimum > cb.Minimum)
                    {
                        removeList.Add(breaks[i]);
                        iPrev--;
                    }
                    else if (breaks[i].Maximum > cb.Minimum || i == iPrev)
                    {
                        // otherwise, if the maximum of a lower range is higher than the value, adjust it.
                        breaks[i].Maximum = cb.Minimum;
                        breaks[i].ApplyMinMax(_symbolizer.EditorSettings);
                    }
                }

                int iNext = e.RowIndex + 1;
                for (int i = e.RowIndex + 1; i < breaks.Count; i++)
                {
                    // If the specified max is below the minima of a lower range, remove the lower range.
                    if (breaks[i].Maximum < cb.Maximum)
                    {
                        removeList.Add(breaks[i]);
                        iNext++;
                    }
                    else if (breaks[i].Minimum < cb.Maximum || i == iNext)
                    {
                        // otherwise, if the maximum of a lower range is higher than the value, adjust it.
                        breaks[i].Minimum = cb.Maximum;
                        breaks[i].ApplyMinMax(_symbolizer.EditorSettings);
                    }
                }

                foreach (IColorCategory brk in removeList)
                {
                    // Do the actual removal.
                    breaks.Remove(brk);
                }
            }

            breaks.ResumeEvents();
            _ignoreRefresh = true;
            cmbInterval.SelectedItem = IntervalMethod.Manual;
            _symbolizer.EditorSettings.IntervalMethod = IntervalMethod.Manual;
            _ignoreRefresh = false;
            UpdateStatistics(false);
            _cleanupTimer.Start();
        }

        private void DgvCategoriesMouseDown(object sender, MouseEventArgs e)
        {
            if (_ignoreEnter) return;
            _activeCategoryIndex = dgvCategories.HitTest(e.X, e.Y).RowIndex;
        }

        private void DgvCategoriesSelectionChanged(object sender, EventArgs e)
        {
            if (breakSliderGraph1?.Breaks == null) return;
            if (dgvCategories.SelectedRows.Count > 0)
            {
                int index = dgvCategories.Rows.IndexOf(dgvCategories.SelectedRows[0]);
                if (breakSliderGraph1.Breaks.Count == 0 || index >= breakSliderGraph1.Breaks.Count) return;
                breakSliderGraph1.SelectBreak(breakSliderGraph1.Breaks[index]);
            }
            else
            {
                breakSliderGraph1.SelectBreak(null);
            }

            breakSliderGraph1.Invalidate();
        }

        private void GetSettings()
        {
            _ignoreRefresh = true;
            EditorSettings settings = _symbolizer.EditorSettings;
            tccColorRange.Initialize(new ColorRangeEventArgs(settings.StartColor, settings.EndColor, settings.HueShift, settings.HueSatLight, settings.UseColorRange));
            UpdateTable();
            cmbInterval.SelectedItem = settings.IntervalMethod;
            UpdateStatistics(false);
            nudCategoryCount.Value = _newScheme.EditorSettings.NumBreaks;
            cmbIntervalSnapping.SelectedItem = settings.IntervalSnapMethod;
            nudSigFig.Value = settings.IntervalRoundingDigits;
            angLightDirection.Angle = (int)_symbolizer.ShadedRelief.LightDirection;
            dbxElevationFactor.Value = _symbolizer.ShadedRelief.ElevationFactor;
            chkHillshade.Checked = _symbolizer.ShadedRelief.IsUsed;
            colorNoData.Color = _symbolizer.NoDataColor;
            opacityNoData.Value = _symbolizer.NoDataColor.GetOpacity();
            sldSchemeOpacity.Value = _symbolizer.Opacity;
            _ignoreRefresh = false;
        }

        private void NudCategoryCountValueChanged(object sender, EventArgs e)
        {
            if (_ignoreRefresh) return;
            _ignoreRefresh = true;
            cmbInterval.SelectedItem = IntervalMethod.EqualInterval;
            _ignoreRefresh = false;
            RefreshValues();
        }

        private void NudColumnsValueChanged(object sender, EventArgs e)
        {
            breakSliderGraph1.NumColumns = (int)nudColumns.Value;
        }

        private void NudSigFigValueChanged(object sender, EventArgs e)
        {
            if (_newScheme == null) return;
            _newScheme.EditorSettings.IntervalRoundingDigits = (int)nudSigFig.Value;

            RefreshValues();
        }

        private void PropertyDialogChangesApplied(object sender, EventArgs e)
        {
            _symbolizer.ShadedRelief = (_shadedReliefDialog.PropertyGrid.SelectedObject as IShadedRelief).Copy();
            angLightDirection.Angle = (int)_symbolizer.ShadedRelief.LightDirection;
            dbxElevationFactor.Value = _symbolizer.ShadedRelief.ElevationFactor;
        }

        private void QuickSchemeClicked(object sender, EventArgs e)
        {
            _ignoreRefresh = true;
            _newScheme.EditorSettings.NumBreaks = 2;
            nudCategoryCount.Value = 2;
            _ignoreRefresh = false;
            MenuItem mi = sender as MenuItem;
            if (mi == null) return;
            ColorSchemeType cs = (ColorSchemeType)Enum.Parse(typeof(ColorSchemeType), mi.Text);
            _newScheme.ApplyScheme(cs, _raster);
            UpdateTable();
            UpdateStatistics(true); // if the parameter is true, even on manual, the breaks are reset.
            breakSliderGraph1.Invalidate();
        }

        private void RefreshValues()
        {
            if (_ignoreRefresh) return;
            SetSettings();
            _newScheme.CreateCategories(_raster);
            UpdateTable();
            UpdateStatistics(false); // if the parameter is true, even on manual, the breaks are reset.
            breakSliderGraph1.Invalidate();
        }

        private void SetElevationFeetLatLong(object sender, EventArgs e)
        {
            dbxElevationFactor.Value = 1 / (111319.9 * 3.2808399);
        }

        private void SetElevationFeetMeters(object sender, EventArgs e)
        {
            dbxElevationFactor.Value = .3048;
        }

        private void SetElevationMetersFeet(object sender, EventArgs e)
        {
            dbxElevationFactor.Value = 3.2808399;
        }

        private void SetElevationMetersLatLong(object sender, EventArgs e)
        {
            dbxElevationFactor.Value = 1 / 111319.9;
        }

        private void SetElevationSameUnits(object sender, EventArgs e)
        {
            dbxElevationFactor.Value = 1;
        }

        private void SetSettings()
        {
            if (_ignoreRefresh) return;
            EditorSettings settings = _symbolizer.EditorSettings;
            settings.NumBreaks = (int)nudCategoryCount.Value;
            settings.IntervalSnapMethod = (IntervalSnapMethod)cmbIntervalSnapping.SelectedItem;
            settings.IntervalRoundingDigits = (int)nudSigFig.Value;
        }

        private void SldSchemeOpacityValueChanged(object sender, EventArgs e)
        {
            if (_ignoreRefresh) return;
            _symbolizer.Opacity = Convert.ToSingle(sldSchemeOpacity.Value);
            foreach (var cat in _symbolizer.Scheme.Categories)
            {
                cat.HighColor = cat.HighColor.ToTransparent(_symbolizer.Opacity);
                cat.LowColor = cat.LowColor.ToTransparent(_symbolizer.Opacity);
            }

            dgvCategories.Invalidate();
        }

        private void TabColorDialogChangesApplied(object sender, EventArgs e)
        {
            if (_newScheme?.Categories == null) return;
            if (_dblClickEditIndex < 0 || _dblClickEditIndex > _newScheme.Categories.Count) return;
            _newScheme.Categories[_dblClickEditIndex].LowColor = _tabColorDialog.StartColor;
            _newScheme.Categories[_dblClickEditIndex].HighColor = _tabColorDialog.EndColor;
            UpdateTable();
        }

        private void TccColorRangeColorChanged(object sender, ColorRangeEventArgs e)
        {
            if (_ignoreRefresh) return;
            RasterEditorSettings settings = _newScheme.EditorSettings;
            settings.StartColor = e.StartColor;
            settings.EndColor = e.EndColor;
            settings.UseColorRange = e.UseColorRange;
            settings.HueShift = e.HueShift;
            settings.HueSatLight = e.Hsl;
            RefreshValues();
        }

        private void UpdateStatistics(bool clear)
        {
            // Graph
            SetSettings();
            breakSliderGraph1.RasterLayer = _newLayer;
            breakSliderGraph1.Title = _newLayer.LegendText;
            breakSliderGraph1.ResetExtents();
            if (_symbolizer.EditorSettings.IntervalMethod == IntervalMethod.Manual && !clear)
            {
                breakSliderGraph1.UpdateBreaks();
            }
            else
            {
                breakSliderGraph1.ResetBreaks(null);
            }

            Statistics stats = breakSliderGraph1.Statistics;

            // Stat list
            dgvStatistics.Rows.Clear();
            dgvStatistics.Rows.Add(7);
            dgvStatistics[0, 0].Value = "Count";
            dgvStatistics[1, 0].Value = _raster.NumValueCells.ToString("#, ###");
            dgvStatistics[0, 1].Value = "Min";
            dgvStatistics[1, 1].Value = _raster.Minimum.ToString("#, ###");
            dgvStatistics[0, 2].Value = "Max";
            dgvStatistics[1, 2].Value = _raster.Maximum.ToString("#, ###");
            dgvStatistics[0, 3].Value = "Sum";
            dgvStatistics[1, 3].Value = (_raster.Mean * _raster.NumValueCells).ToString("#, ###");
            dgvStatistics[0, 4].Value = "Mean";
            dgvStatistics[1, 4].Value = _raster.Mean.ToString("#, ###");
            dgvStatistics[0, 5].Value = "Median";
            dgvStatistics[1, 5].Value = stats.Median.ToString("#, ###");
            dgvStatistics[0, 6].Value = "Std";
            dgvStatistics[1, 6].Value = stats.StandardDeviation.ToString("#, ###");
        }

        /// <summary>
        /// Updates the Table using the unique values.
        /// </summary>
        private void UpdateTable()
        {
            dgvCategories.SuspendLayout();
            dgvCategories.Rows.Clear();

            ColorCategoryCollection breaks = _newScheme.Categories;
            int i = 0;
            if (breaks.Count > 0)
            {
                dgvCategories.Rows.Add(breaks.Count);
                foreach (IColorCategory brk in breaks)
                {
                    dgvCategories[1, i].Value = brk.Range.ToString(_symbolizer.EditorSettings.IntervalSnapMethod, _symbolizer.EditorSettings.IntervalRoundingDigits);
                    dgvCategories[2, i].Value = brk.LegendText;
                    i++;
                }
            }

            dgvCategories.ResumeLayout();
            dgvCategories.Invalidate();
        }

        #endregion
    }
}