// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using DotSpatial.Serialization;

namespace DotSpatial.Symbology
{
    /// <summary>
    /// PolygonCategory.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class PolygonCategory : FeatureCategory, IPolygonCategory
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class.
        /// </summary>
        public PolygonCategory()
        {
            Symbolizer = new PolygonSymbolizer();
            SelectionSymbolizer = new PolygonSymbolizer(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class that is made up from a simple color.
        /// </summary>
        /// <param name="fillColor">The color to fill the polygons with.</param>
        /// <param name="outlineColor">The border color for the polygons.</param>
        /// <param name="outlineWidth">The width of the line drawn on the border.</param>
        public PolygonCategory(Color fillColor, Color outlineColor, double outlineWidth)
        {
            Symbolizer = new PolygonSymbolizer(fillColor, outlineColor, outlineWidth);
            SelectionSymbolizer = new PolygonSymbolizer(Color.Transparent, Color.Cyan, outlineWidth);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class with the specified image being tiled within the category.
        /// </summary>
        /// <param name="picture">The picture to draw.</param>
        /// <param name="wrap">The way to wrap the picture.</param>
        /// <param name="angle">The angle to rotate the image.</param>
        public PolygonCategory(Image picture, WrapMode wrap, double angle)
        {
            Symbolizer = new PolygonSymbolizer(picture, wrap, angle);
            SelectionSymbolizer = new PolygonSymbolizer(Color.Transparent, Color.Cyan, 2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class with the specified image being tiled within the category.
        /// The simple outline characteristics are also defined.
        /// </summary>
        /// <param name="picture">The picture to draw.</param>
        /// <param name="wrap">The way to wrap the picture.</param>
        /// <param name="angle">The angle to rotate the image.</param>
        /// <param name="outlineColor">The color to use.</param>
        /// <param name="outlineWidth">The outline width.</param>
        public PolygonCategory(Image picture, WrapMode wrap, double angle, Color outlineColor, double outlineWidth)
        {
            Symbolizer = new PolygonSymbolizer(picture, wrap, angle, outlineColor, outlineWidth);
            SelectionSymbolizer = new PolygonSymbolizer(Color.Transparent, Color.Cyan, 2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class using a Gradient Pattern with the specified colors and angle.
        /// </summary>
        /// <param name="startColor">The start color.</param>
        /// <param name="endColor">The end color.</param>
        /// <param name="angle">The direction of the gradient.</param>
        /// <param name="style">The type of gradient to use.</param>
        /// <param name="outlineColor">The color to use for the border symbolizer.</param>
        /// <param name="outlineWidth">The width of the line to use for the border symbolizer.</param>
        public PolygonCategory(Color startColor, Color endColor, double angle, GradientType style, Color outlineColor, double outlineWidth)
        {
            Symbolizer = new PolygonSymbolizer(startColor, endColor, angle, style, outlineColor, outlineWidth);
            SelectionSymbolizer = new PolygonSymbolizer(Color.LightCyan, Color.DarkCyan, angle, style, Color.DarkCyan, outlineWidth);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonCategory"/> class based on a symbolizer,
        /// and uses the same symbolizer, but with a fill and border color of light cyan for the selection symbolizer.
        /// </summary>
        /// <param name="polygonSymbolizer">The symbolizer to use in order to create a category.</param>
        public PolygonCategory(IPolygonSymbolizer polygonSymbolizer)
        {
            Symbolizer = polygonSymbolizer;
            IPolygonSymbolizer select = polygonSymbolizer.Copy();
            select.OutlineSymbolizer.ScaleMode = ScaleMode.Symbolic;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the symbolizer to use to draw selected features from this category.
        /// </summary>
        [Description("Gets or sets the symbolizer to use to draw selected features from this category.")]
        public new IPolygonSymbolizer SelectionSymbolizer
        {
            get
            {
                return base.SelectionSymbolizer as IPolygonSymbolizer;
            }

            set
            {
                base.SelectionSymbolizer = value;
            }
        }

        /// <summary>
        /// Gets or sets the symbolizer for this category.
        /// </summary>
        [Description("Gets or sets the symbolizer for this category")]
        public new IPolygonSymbolizer Symbolizer
        {
            get
            {
                return base.Symbolizer as IPolygonSymbolizer;
            }

            set
            {
                base.Symbolizer = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// This gets a single color that attempts to represent the specified
        /// category. For polygons, for example, this is the fill color (or central fill color)
        /// of the top pattern. If an image is being used, the color will be gray.
        /// </summary>
        /// <returns>The System.Color that can be used as an approximation to represent this category.</returns>
        public override Color GetColor()
        {
            if (Symbolizer?.Patterns == null || Symbolizer.Patterns.Count == 0) return Color.Gray;

            IPattern p = Symbolizer.Patterns[0];
            return p.GetFillColor();
        }

        /// <summary>
        /// Sets the fill color of the top-most pattern to the specified color, if the pattern can specify a color.
        /// </summary>
        /// <param name="color">Sets the color of the top most pattern for the principal symbolizer.</param>
        public override void SetColor(Color color)
        {
            if (Symbolizer?.Patterns == null || Symbolizer.Patterns.Count == 0) return;

            Symbolizer.Patterns[0].SetFillColor(color);
        }

        /// <summary>
        /// A string representation of this category.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return "Filter: " + FilterExpression + " Color: " + Symbolizer.GetFillColor();
        }

        #endregion
    }
}