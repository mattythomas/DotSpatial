// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using DotSpatial.Data;
using DotSpatial.Symbology;
using NetTopologySuite.Geometries;

namespace DotSpatial.Controls
{
    /// <summary>
    /// This is a specialized FeatureLayer that specifically handles point drawing.
    /// </summary>
    public class MapPointLayer : PointLayer, IMapPointLayer
    {
        #region  Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPointLayer"/> class.
        /// This creates a blank MapPointLayer with the DataSet set to an empty new featureset of the Point featuretype.
        /// </summary>
        public MapPointLayer()
        {
            Configure();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPointLayer"/> class.
        /// </summary>
        /// <param name="featureSet">The point feature set used as data source.</param>
        public MapPointLayer(IFeatureSet featureSet)
            : base(featureSet)
        {
            // this simply handles the default case where no status messages are requested
            Configure();
            OnFinishedLoading();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPointLayer"/> class.
        /// Creates a new instance of the point layer where the container is specified.
        /// </summary>
        /// <param name="featureSet">The point feature set used as data source.</param>
        /// <param name="container">An IContainer that the point layer should be created in.</param>
        public MapPointLayer(IFeatureSet featureSet, ICollection<ILayer> container)
            : base(featureSet, container, null)
        {
            Configure();
            OnFinishedLoading();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPointLayer"/> class.
        /// Creates a new instance of the point layer where the container is specified.
        /// </summary>
        /// <param name="featureSet">The point feature set used as data source.</param>
        /// <param name="container">An IContainer that the point layer should be created in.</param>
        /// <param name="notFinished">Indicates whether the OnFinishedLoading event should be suppressed after loading finished.</param>
        public MapPointLayer(IFeatureSet featureSet, ICollection<ILayer> container, bool notFinished)
            : base(featureSet, container, null)
        {
            Configure();
            if (!notFinished) OnFinishedLoading();
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when drawing content has changed on the buffer for this layer
        /// </summary>
        public event EventHandler<ClipArgs> BufferChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the back buffer that will be drawn to as part of the initialization process.
        /// </summary>
        [ShallowCopy]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image BackBuffer { get; set; }

        /// <summary>
        /// Gets or sets the current buffer.
        /// </summary>
        [ShallowCopy]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image Buffer { get; set; }

        /// <summary>
        /// Gets or sets the geographic region represented by the buffer
        /// Calling Initialize will set this automatically.
        /// </summary>
        [ShallowCopy]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Envelope BufferEnvelope { get; set; }

        /// <summary>
        /// Gets or sets the rectangle in pixels to use as the back buffer.
        /// Calling Initialize will set this automatically.
        /// </summary>
        [ShallowCopy]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle BufferRectangle { get; set; }

        /// <summary>
        /// Gets or sets the label layer that is associated with this point layer.
        /// </summary>
        [ShallowCopy]
        public new IMapLabelLayer LabelLayer
        {
            get
            {
                return base.LabelLayer as IMapLabelLayer;
            }

            set
            {
                base.LabelLayer = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Attempts to create a new GeoPointLayer using the specified file. If the filetype is not
        /// does not generate a point layer, an exception will be thrown.
        /// </summary>
        /// <param name="fileName">A string fileName to create a point layer for.</param>
        /// <param name="progressHandler">Any valid implementation of IProgressHandler for receiving progress messages.</param>
        /// <returns>A GeoPointLayer created from the specified fileName.</returns>
        public static new MapPointLayer OpenFile(string fileName, IProgressHandler progressHandler)
        {
            ILayer fl = LayerManager.DefaultLayerManager.OpenLayer(fileName, progressHandler);
            return fl as MapPointLayer;
        }

        /// <summary>
        /// Attempts to create a new GeoPointLayer using the specified file. If the filetype is not
        /// does not generate a point layer, an exception will be thrown.
        /// </summary>
        /// <param name="fileName">A string fileName to create a point layer for.</param>
        /// <returns>A GeoPointLayer created from the specified fileName.</returns>
        public static new MapPointLayer OpenFile(string fileName)
        {
            IFeatureLayer fl = LayerManager.DefaultLayerManager.OpenVectorLayer(fileName);
            return fl as MapPointLayer;
        }

        /// <summary>
        /// Call StartDrawing before using this.
        /// </summary>
        /// <param name="rectangles">The rectangular region in pixels to clear.</param>
        /// <param name= "color">The color to use when clearing. Specifying transparent
        /// will replace content with transparent pixels.</param>
        public void Clear(List<Rectangle> rectangles, Color color)
        {
            if (BackBuffer == null) return;
            Graphics g = Graphics.FromImage(BackBuffer);
            foreach (Rectangle r in rectangles)
            {
                if (r.IsEmpty == false)
                {
                    g.Clip = new Region(r);
                    g.Clear(color);
                }
            }

            g.Dispose();
        }

        /// <summary>
        /// This is testing the idea of using an input parameter type that is marked as out
        /// instead of a return type.
        /// </summary>
        /// <param name="result">The result of the creation.</param>
        /// <returns>Boolean, true if a layer can be created.</returns>
        public override bool CreateLayerFromSelectedFeatures(out IFeatureLayer result)
        {
            bool resultOk = CreateLayerFromSelectedFeatures(out MapPointLayer temp);
            result = temp;
            return resultOk;
        }

        /// <summary>
        /// This is the strong typed version of the same process that is specific to geo point layers.
        /// </summary>
        /// <param name="result">The new GeoPointLayer to be created.</param>
        /// <returns>Boolean, true if there were any values in the selection.</returns>
        public virtual bool CreateLayerFromSelectedFeatures(out MapPointLayer result)
        {
            result = null;
            if (Selection == null || Selection.Count == 0) return false;
            FeatureSet fs = Selection.ToFeatureSet();
            result = new MapPointLayer(fs);
            return true;
        }

        /// <summary>
        /// If EditMode is true, then this method is used for drawing.
        /// </summary>
        /// <param name="args">The GeoArgs that control how these features should be drawn.</param>
        /// <param name="features">The features that should be drawn.</param>
        /// <param name="clipRectangles">If an entire chunk is drawn and an update is specified, this clarifies the changed rectangles.</param>
        /// <param name="useChunks">Boolean, if true, this will refresh the buffer in chunks.</param>
        /// <param name="selected">Indicates whether to draw the normal colored features or the selection colored features.</param>
        public virtual void DrawFeatures(MapArgs args, List<IFeature> features, List<Rectangle> clipRectangles, bool useChunks, bool selected)
        {
            if (!useChunks || features.Count < ChunkSize)
            {
                DrawFeatures(args, features, selected);
                return;
            }

            int count = features.Count;
            int numChunks = (int)Math.Ceiling(count / (double)ChunkSize);
            for (int chunk = 0; chunk < numChunks; chunk++)
            {
                int groupSize = ChunkSize;
                if (chunk == numChunks - 1) groupSize = count - (chunk * ChunkSize);
                List<IFeature> subset = features.GetRange(chunk * ChunkSize, groupSize);
                DrawFeatures(args, subset, selected);
                if (numChunks <= 0 || chunk >= numChunks - 1) continue;
                FinishDrawing();
                OnBufferChanged(clipRectangles);
                Application.DoEvents();
            }
        }

        /// <summary>
        /// If EditMode is false, then this method is used for drawing.
        /// </summary>
        /// <param name="args">The GeoArgs that control how these features should be drawn.</param>
        /// <param name="indices">The features that should be drawn.</param>
        /// <param name="clipRectangles">If an entire chunk is drawn and an update is specified, this clarifies the changed rectangles.</param>
        /// <param name="useChunks">Boolean, if true, this will refresh the buffer in chunks.</param>
        /// <param name="selected">Indicates whether to draw the normal colored features or the selection colored features.</param>
        public virtual void DrawFeatures(MapArgs args, List<int> indices, List<Rectangle> clipRectangles, bool useChunks, bool selected)
        {
            if (!useChunks)
            {
                DrawFeatures(args, indices, selected);
                return;
            }

            int count = indices.Count;
            int numChunks = (int)Math.Ceiling(count / (double)ChunkSize);

            for (int chunk = 0; chunk < numChunks; chunk++)
            {
                int numFeatures = ChunkSize;
                if (chunk == numChunks - 1) numFeatures = indices.Count - (chunk * ChunkSize);
                DrawFeatures(args, indices.GetRange(chunk * ChunkSize, numFeatures), selected);

                if (numChunks > 0 && chunk < numChunks - 1)
                {
                    OnBufferChanged(clipRectangles);
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// This will draw any features that intersect this region. To specify the features
        /// directly, use OnDrawFeatures. This will not clear existing buffer content.
        /// For that call Initialize instead.
        /// </summary>
        /// <param name="args">A GeoArgs clarifying the transformation from geographic to image space.</param>
        /// <param name="regions">The geographic regions to draw.</param>
        /// <param name="selected">Indicates whether to draw the normal colored features or the selection colored features.</param>
        public virtual void DrawRegions(MapArgs args, List<Extent> regions, bool selected)
        {
            // First determine the number of features we are talking about based on region.
            List<Rectangle> clipRects = args.ProjToPixel(regions);
            if (EditMode)
            {
                List<IFeature> drawList = new List<IFeature>();
                foreach (Extent region in regions)
                {
                    if (region != null)
                    {
                        // Use union to prevent duplicates. No sense in drawing more than we have to.
                        drawList = drawList.Union(DataSet.Select(region)).ToList();
                    }
                }

                DrawFeatures(args, drawList, clipRects, true, selected);
            }
            else
            {
                List<int> drawList = new List<int>();
                double[] verts = DataSet.Vertex;

                if (DataSet.FeatureType == FeatureType.Point)
                {
                    for (int shp = 0; shp < verts.Length / 2; shp++)
                    {
                        foreach (Extent extent in regions)
                        {
                            if (extent.Intersects(verts[shp * 2], verts[(shp * 2) + 1]))
                            {
                                drawList.Add(shp);
                            }
                        }
                    }
                }
                else
                {
                    List<ShapeRange> shapes = DataSet.ShapeIndices;
                    for (int shp = 0; shp < shapes.Count; shp++)
                    {
                        foreach (Extent region in regions)
                        {
                            if (!shapes[shp].Extent.Intersects(region)) continue;
                            drawList.Add(shp);
                            break;
                        }
                    }
                }

                DrawFeatures(args, drawList, clipRects, true, selected);
            }
        }

        /// <summary>
        /// Indicates that the drawing process has been finalized and swaps the back buffer
        /// to the front buffer.
        /// </summary>
        public void FinishDrawing()
        {
            OnFinishDrawing();
            if (Buffer != null && Buffer != BackBuffer) Buffer.Dispose();
            Buffer = BackBuffer;
        }

        /// <summary>
        /// Copies any current content to the back buffer so that drawing should occur on the
        /// back buffer (instead of the fore-buffer). Calling draw methods without
        /// calling this may cause exceptions.
        /// </summary>
        /// <param name="preserve">Boolean, true if the front buffer content should be copied to the back buffer
        /// where drawing will be taking place.</param>
        public void StartDrawing(bool preserve)
        {
            Bitmap backBuffer = new Bitmap(BufferRectangle.Width, BufferRectangle.Height);
            if (Buffer?.Width == backBuffer.Width && Buffer.Height == backBuffer.Height && preserve)
            {
                Graphics g = Graphics.FromImage(backBuffer);
                g.DrawImageUnscaled(Buffer, 0, 0);
            }

            if (BackBuffer != null && BackBuffer != Buffer) BackBuffer.Dispose();
            BackBuffer = backBuffer;
            OnStartDrawing();
        }

        /// <summary>
        /// Fires the OnBufferChanged event.
        /// </summary>
        /// <param name="clipRectangles">The Rectangle in pixels.</param>
        protected virtual void OnBufferChanged(List<Rectangle> clipRectangles)
        {
            if (BufferChanged != null)
            {
                ClipArgs e = new ClipArgs(clipRectangles);
                BufferChanged(this, e);
            }
        }

        /// <summary>
        /// A default method to generate a label layer.
        /// </summary>
        protected override void OnCreateLabels()
        {
            LabelLayer = new MapLabelLayer(this);
        }

        /// <summary>
        /// Indiciates that whatever drawing is going to occur has finished and the contents
        /// are about to be flipped forward to the front buffer.
        /// </summary>
        protected virtual void OnFinishDrawing()
        {
        }

        /// <summary>
        /// Occurs when a new drawing is started, but after the BackBuffer has been established.
        /// </summary>
        protected virtual void OnStartDrawing()
        {
        }

        private void Configure()
        {
            ChunkSize = 50000;
        }

        // This draws the individual point features
        private void DrawFeatures(MapArgs e, IEnumerable<int> indices, bool selected)
        {
            if (selected && (!DrawnStatesNeeded || !DrawnStates.Any(_ => _.Selected))) return; // there are no selected features

            Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);
            Matrix origTransform = g.Transform;
            FeatureType featureType = DataSet.FeatureType;

            if (!DrawnStatesNeeded)
            {
                if (Symbology == null || Symbology.Categories.Count == 0) return;
                FastDrawnState state = new FastDrawnState(false, Symbology.Categories[0]);
                IPointSymbolizer ps = (state.Category as IPointCategory)?.Symbolizer;
                if (ps == null) return;
                double[] vertices = DataSet.Vertex;

                foreach (int index in indices)
                {
                    if (featureType == FeatureType.Point)
                    {
                        DrawPoint(vertices[index * 2], vertices[(index * 2) + 1], e, ps, g, origTransform);
                    }
                    else
                    {
                        // multi-point
                        ShapeRange range = DataSet.ShapeIndices[index];
                        for (int i = range.StartIndex; i <= range.EndIndex(); i++)
                        {
                            DrawPoint(vertices[i * 2], vertices[(i * 2) + 1], e, ps, g, origTransform);
                        }
                    }
                }
            }
            else
            {
                FastDrawnState[] states = DrawnStates;

                var indexList = indices as IList<int> ?? indices.ToList();
                if (indexList.Max() >= states.Length)
                {
                    AssignFastDrawnStates();
                    states = DrawnStates;
                }

                double[] vertices = DataSet.Vertex;

                foreach (int index in indexList)
                {
                    if (index >= states.Length) break;
                    FastDrawnState state = states[index];
                    if (!state.Visible || state.Category == null) continue;
                    if (selected && !state.Selected) continue;

                    if (!(state.Category is IPointCategory pc)) continue;

                    IPointSymbolizer ps = selected ? pc.SelectionSymbolizer : pc.Symbolizer;
                    if (ps == null) continue;

                    if (featureType == FeatureType.Point)
                    {
                        DrawPoint(vertices[index * 2], vertices[(index * 2) + 1], e, ps, g, origTransform);
                    }
                    else
                    {
                        ShapeRange range = DataSet.ShapeIndices[index];
                        for (int i = range.StartIndex; i <= range.EndIndex(); i++)
                        {
                            DrawPoint(vertices[i * 2], vertices[(i * 2) + 1], e, ps, g, origTransform);
                        }
                    }
                }
            }

            if (e.Device == null) g.Dispose();
            else g.Transform = origTransform;
        }

        // This draws the individual point features
        private void DrawFeatures(MapArgs e, IEnumerable<IFeature> features, bool selected)
        {
            IDictionary<IFeature, IDrawnState> states = DrawingFilter.DrawnStates;
            if (states == null) return;
            if (selected && !states.Any(_ => _.Value.IsSelected)) return;

            Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);
            Matrix origTransform = g.Transform;
            foreach (IFeature feature in features)
            {
                if (!states.ContainsKey(feature)) continue;
                IDrawnState ds = states[feature];
                if (ds == null || !ds.IsVisible || ds.SchemeCategory == null) continue;

                if (selected && !ds.IsSelected) continue;

                if (!(ds.SchemeCategory is IPointCategory pc)) continue;

                IPointSymbolizer ps = selected ? pc.SelectionSymbolizer : pc.Symbolizer;
                if (ps == null) continue;

                foreach (Coordinate c in feature.Geometry.Coordinates)
                {
                    DrawPoint(c.X, c.Y, e, ps, g, origTransform);
                }
            }

            if (e.Device == null) g.Dispose();
            else g.Transform = origTransform;
        }

        /// <summary>
        /// Draws a point at the given location.
        /// </summary>
        /// <param name="ptX">X-Coordinate of the point, that should be drawn.</param>
        /// <param name="ptY">Y-Coordinate of the point, that should be drawn.</param>
        /// <param name="e">MapArgs for calculating the scaleSize.</param>
        /// <param name="ps">PointSymbolizer with which the point gets drawn.</param>
        /// <param name="g">Graphics-Object that should be used by the PointSymbolizer.</param>
        /// <param name="origTransform">The original transformation that is used to position the point.</param>
        private void DrawPoint(double ptX, double ptY, MapArgs e, IPointSymbolizer ps, Graphics g, Matrix origTransform)
        {
            var x = Convert.ToInt32((ptX - e.MinX) * e.Dx);
            var y = Convert.ToInt32((e.MaxY - ptY) * e.Dy);
            double scaleSize = ps.GetScale(e);
            Matrix shift = origTransform.Clone();
            shift.Translate(x, y);
            g.Transform = shift;
            ps.Draw(g, scaleSize);
        }

        #endregion
    }
}