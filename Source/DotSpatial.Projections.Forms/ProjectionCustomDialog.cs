// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Windows.Forms;
using DotSpatial.Projections.Transforms;

namespace DotSpatial.Projections.Forms
{
    /// <summary>
    /// ProjectionCustomDialog.
    /// </summary>
    public partial class ProjectionCustomDialog : Form
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionCustomDialog"/> class.
        /// </summary>
        public ProjectionCustomDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs whenever the apply changes button is clicked, or else when the ok button is clicked.
        /// </summary>
        public event EventHandler ChangesApplied;

        #endregion

        #region Properties

        /// <summary>
        /// gets or sets the selected projection info.
        /// </summary>
        public ProjectionInfo SelectedProjectionInfo { get; set; } = new ProjectionInfo();

        #endregion

        #region Methods

        /// <summary>
        /// Fires the ChangesApplied event.
        /// </summary>
        protected virtual void OnApplyChanges()
        {
            ChangesApplied?.Invoke(this, EventArgs.Empty);
        }

        private void BtnApplyClick(object sender, EventArgs e)
        {
            OnApplyChanges();
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void CmbDatumsSelectedIndexChanged(object sender, EventArgs e)
        {
            Datum d = new Datum((string)cmbDatums.SelectedItem);
            SelectedProjectionInfo.GeographicInfo.Datum = d;
            switch (d.DatumType)
            {
                case DatumType.GridShift:
                    radGridShift.Checked = true;
                    break;
                case DatumType.Param3:
                    rad3.Checked = true;
                    break;
                case DatumType.Param7:
                    rad7.Checked = true;
                    break;
                case DatumType.WGS84:
                    radWGS84.Checked = true;
                    break;
            }

            cmbEllipsoid.SelectedItem = d.Spheroid.Name;
        }

        private void CmbEllipsoidSelectedIndexChanged(object sender, EventArgs e)
        {
            Proj4Ellipsoid ell = (Proj4Ellipsoid)Enum.Parse(typeof(Proj4Ellipsoid), (string)cmbEllipsoid.SelectedItem);
            Spheroid sph = new Spheroid(ell);
            SelectedProjectionInfo.GeographicInfo.Datum.Spheroid = sph;
            dbA.Value = sph.EquatorialRadius;
            dbB.Value = sph.PolarRadius;
        }

        private void CmbMeridianSelectedIndexChanged(object sender, EventArgs e)
        {
            Proj4Meridian mer = (Proj4Meridian)Enum.Parse(typeof(Proj4Meridian), (string)cmbMeridian.SelectedItem);
            Meridian m = new Meridian(mer);
            SelectedProjectionInfo.GeographicInfo.Meridian = m;
            dbMeridian.Value = m.Longitude;
        }

        private void CmdOkClick(object sender, EventArgs e)
        {
            OnApplyChanges();
            Close();
        }

        private void ProjectionCustomDialogLoad(object sender, EventArgs e)
        {
            foreach (ITransform transform in TransformManager.DefaultTransformManager.Transforms)
            {
                cmbTransform.Items.Add(transform.Name);
            }

            cmbTransform.SelectedIndex = 0;

            string[] meridians = Enum.GetNames(typeof(Proj4Meridian));
            foreach (string meridian in meridians)
            {
                cmbMeridian.Items.Add(meridian);
            }

            cmbMeridian.SelectedIndex = 0;

            string[] datums = Enum.GetNames(typeof(Proj4Datum));
            foreach (string datum in datums)
            {
                cmbDatums.Items.Add(datum);
            }

            cmbDatums.SelectedIndex = 0;

            string[] ellipsoids = Enum.GetNames(typeof(Proj4Ellipsoid));
            foreach (string ellipsoid in ellipsoids)
            {
                cmbEllipsoid.Items.Add(ellipsoid);
            }

            cmbEllipsoid.SelectedIndex = 0;
        }

        #endregion
    }
}