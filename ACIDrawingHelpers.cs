using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ACI318_19Library
{
    public static class ACIDrawingHelpers
    {
        static Brush color_compressive = Brushes.Red;
        static Brush color_tension = Brushes.Blue;

        // Assume "this" is your Window/UserControl with Canvas named cnvCrossSection
        public static void DrawCrossSection(Canvas cnv, DesignResultModel design)
        {
            CrossSection section = design.crossSection;

            if (section == null || cnv == null) return;

            RebarCatalog catalog = new RebarCatalog();
            cnv.Children.Clear();

            double margin = 10; // pixels around edges

            double canvasW = cnv.ActualWidth > 0 ? cnv.ActualWidth : cnv.Width;
            double canvasH = cnv.ActualHeight > 0 ? cnv.ActualHeight : cnv.Height;

            // Compute scale to fit section inside canvas
            double scaleX = (canvasW - 2 * margin) / section.Width;
            double scaleY = (canvasH - 2 * margin) / section.Depth;
            double scale = Math.Min(scaleX, scaleY);

            // Center offsets
            double offsetX = (canvasW - section.Width * scale) / 2.0;
            double offsetY = (canvasH - section.Depth * scale) / 2.0;

            double bPix = section.Width * scale;
            double hPix = section.Depth * scale;

            // Draw concrete rectangle
            Rectangle rect = new Rectangle
            {
                Width = bPix,
                Height = hPix,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.LightGray
            };
            Canvas.SetLeft(rect, offsetX);
            Canvas.SetTop(rect, offsetY);
            cnv.Children.Add(rect);

            // Draw Tension Bars
            foreach (var layer in section.TensionRebars)
            {
                DrawBarLayer(cnv, layer, section.SideCover, catalog, scale, offsetX, offsetY, section.Width, color_tension);
            }

            // Draw Compression Bars
            foreach (var layer in section.CompressionRebars)
            {
                DrawBarLayer(cnv, layer, section.SideCover,catalog, scale, offsetX, offsetY, section.Width, color_compressive);
            }

            // Effective depth location (from top)
            double dEff = section.dEffective();  // effective depth in same units as your section
            double dPrimeEff = section.dPrimeEffective();

            if (section.TensionRebars.Count > 0)
            {
                DrawRebarCentroidMarker(cnv, dEff, section.SideCover, catalog, scale, offsetX, offsetY, section.Width, color_tension);
            }
            if (section.CompressionRebars.Count > 0)
            {
                DrawRebarCentroidMarker(cnv, dPrimeEff, section.SideCover, catalog, scale, offsetX, offsetY, section.Width, color_compressive);
            }



            // Optionally draw cover lines
            Line topCover = new Line
            {
                X1 = offsetX,
                Y1 = offsetY + section.CompressionCover * scale,
                X2 = offsetX + bPix,
                Y2 = offsetY + section.CompressionCover * scale,
                Stroke = Brushes.Green,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 2, 2 }
            };
            cnv.Children.Add(topCover);

            Line bottomCover = new Line
            {
                X1 = offsetX,
                Y1 = offsetY + (section.Depth - section.TensionCover) * scale,
                X2 = offsetX + bPix,
                Y2 = offsetY + (section.Depth - section.TensionCover) * scale,
                Stroke = Brushes.Green,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 2, 2 }
            };
            cnv.Children.Add(bottomCover);
        }

        private static void DrawRebarCentroidMarker(Canvas cnv, double loc, double side_cover, RebarCatalog catalog, double scale, double offsetX, double offsetY, double sectionWidth, Brush fill)
        {
            // Effective depth location (from top)
            double dEff = loc;
            double yEff = offsetY + dEff * scale;

            // Center X for the strain diagram
            double xCenter = cnv.Width / 2.0;

            // Size of the plus marker
            double markerSize = 6;

            // Horizontal line
            Line horiz = new Line()
            {
                X1 = xCenter - markerSize,
                Y1 = yEff,
                X2 = xCenter + markerSize,
                Y2 = yEff,
                Stroke = fill,
                StrokeThickness = 1
            };
            cnv.Children.Add(horiz);

            // Vertical line
            Line vert = new Line()
            {
                X1 = xCenter,
                Y1 = yEff - markerSize,
                X2 = xCenter,
                Y2 = yEff + markerSize,
                Stroke = fill,
                StrokeThickness = 1
            };
            cnv.Children.Add(vert);
        }

        private static void DrawBarLayer(Canvas cnv, RebarLayer layer, double side_cover, RebarCatalog catalog, double scale, double offsetX, double offsetY, double sectionWidth, Brush fill)
        {
            if (layer == null || layer.Qty == 0) return;

            double dia = catalog.RebarTable[layer.BarSize].Diameter * scale;
            double sectionWidthScaled = sectionWidth * scale;
            double y = offsetY + layer.DepthFromTop * scale;

            if (layer.Qty == 1)
            {
                // Single bar centered
                double x = offsetX + (sectionWidthScaled - dia) / 2.0;
                Ellipse circle = new Ellipse { Width = dia, Height = dia, Fill = fill, Stroke = Brushes.Black, StrokeThickness = 1 };
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y - dia / 2);
                cnv.Children.Add(circle);
                return;
            }

            // Multiple bars
            double totalBarsWidth = dia * layer.Qty;
            double spacing = 0;
            double usableWidth = sectionWidthScaled - 2 * side_cover * scale;

            if (layer.Qty == 2)
            {
                spacing = usableWidth - totalBarsWidth; // 2 bars: spacing between them
            }
            else
            {
                spacing = (usableWidth - totalBarsWidth) / (layer.Qty - 1); // 3+ bars: even spacing
            }

            // Center pattern horizontally
            double patternWidth = totalBarsWidth + spacing * (layer.Qty - 1);
            double startX = offsetX + (sectionWidthScaled - patternWidth) / 2.0;

            for (int i = 0; i < layer.Qty; i++)
            {
                double x = startX + i * (dia + spacing);
                Ellipse circle = new Ellipse { Width = dia, Height = dia, Fill = fill, Stroke = Brushes.Black, StrokeThickness = 1 };
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y - dia / 2);
                cnv.Children.Add(circle);
            }
        }

        // Assume "this" is your Window/UserControl with Canvas named cnvCrossSection
        public static void DrawStrainDiagram(Canvas cnv, DesignResultModel design)
        {



            CrossSection section = design.crossSection;

            if (section == null || cnv == null) return;

            RebarCatalog catalog = new RebarCatalog();
            cnv.Children.Clear();

            double margin = 10; // pixels around edges

            double canvasW = cnv.ActualWidth > 0 ? cnv.ActualWidth : cnv.Width;
            double canvasH = cnv.ActualHeight > 0 ? cnv.ActualHeight : cnv.Height;

            // Compute scale to fit section inside canvas
            double scaleX = (canvasW - 2 * margin) / section.Width;
            double scaleY = (canvasH - 2 * margin) / section.Depth;
            double scale = Math.Min(scaleX, scaleY);

            // Center offsets
            double offsetX = (canvasW - section.Width * scale) / 2.0;
            double offsetY = (canvasH - section.Depth * scale) / 2.0;

            double bPix = section.Width * scale;
            double hPix = section.Depth * scale;

            Line line = new Line()
            {
                X1 = margin,
                Y1 = offsetY,
                X2 = canvasW - margin,
                Y2 = offsetY,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            cnv.Children.Add(line);



            // Draw bottom line of graph
            Line line3 = new Line()
            {
                X1 = margin,
                Y1 = canvasH - offsetY,
                X2 = canvasW - margin,
                Y2 = canvasH - offsetY,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            cnv.Children.Add(line3);

            // draw the points on the strain line next
            double ecu = design.crossSection.EpsilonCu;
            double et = design.eps_T;
            double graph_width = ecu + et * 1.2;
            double minStrain = -ecu;
            double maxStrain = et;

            // map ecu (negative to left edge
            double x1 = MapStrainToX(-ecu, minStrain, maxStrain, canvasW, margin);
            double y1 = offsetY;

            // map et (positive to right edge
            double x2 = MapStrainToX(et, minStrain, maxStrain, canvasW, margin);
            double y2 = offsetY + design.DepthToEpsT * scale;

            Line line5 = new Line()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.Green,
                StrokeThickness = 2.0,
            };
            cnv.Children.Add(line5);

            // Label for ecu (above the point)
            TextBlock txtEcu = new TextBlock()
            {
                Text = $"εcu = -{ecu:F4}",   // format with 4 decimals
                Foreground = color_compressive,
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            cnv.Children.Add(txtEcu);
            // position just above ecu point
            Canvas.SetLeft(txtEcu, x1 - 20);  // offset a little to center
            Canvas.SetTop(txtEcu, y1 - 15);   // above the line point

            // Effective depth location (from top)
            double dEff = section.dEffective();  // effective depth in same units as your section
            double dPrimeEff = section.dPrimeEffective();











            // Draw vertical line of graph
            double x0 = MapStrainToX(0.0, minStrain, maxStrain, canvasW, margin);
            Line line2 = new Line()
            {
                X1 = x0,
                Y1 = offsetY,
                X2 = x0,
                Y2 = canvasH - offsetY,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            cnv.Children.Add(line2);

            // Draw the strain lines at the centroid of the tension and compression rebar group
            if (section.TensionRebars.Count > 0)
            {
                foreach (RebarLayer layer in section.TensionRebars)
                {
                    DrawHorizontalStrainLine(cnv, layer.DepthFromTop, 0, design.DepthToEpsT, section.Depth, section.EpsilonCu, design.eps_T, canvasW, canvasH, margin, color_tension);
                }
            }
            if (section.CompressionRebars.Count > 0)
            {
                foreach (RebarLayer layer in section.CompressionRebars)
                {
                    DrawHorizontalStrainLine(cnv, layer.DepthFromTop, 0, design.DepthToEpsT, section.Depth, section.EpsilonCu, design.eps_T, canvasW, canvasH, margin, color_compressive);
                }
            }
        }

        /// <summary>
        /// Draws a horizontal line from the vertical zero-strain line to the strain line at a given depth.
        /// </summary>
        /// <param name="cnv">Canvas to draw on</param>
        /// <param name="depthFromTop">Depth from top of section (same units as section)</param>
        /// <param name="x0">X coordinate of zero-strain vertical line</param>
        /// <param name="offsetY">Y coordinate of top of section on canvas</param>
        /// <param name="yBottom">Y coordinate of eps_t of section on canvas</param>
        /// <param name="minStrain">Minimum strain (negative, top of section)</param>
        /// <param name="maxStrain">Maximum strain (positive, bottom of section)</param>
        /// <param name="canvasW">Canvas width</param>
        /// <param name="margin">Canvas margin</param>
        public static void DrawHorizontalStrainLine(Canvas cnv, double depth_of_layer, double y_ecu, double y_et, double ht_of_section, double ecu, double et,
            double canvasW, double canvasH, double margin, Brush fill)
        {
            double conc_strain = -ecu;
            if (cnv == null) return;

            // Map the depth to canvas Y
            double scale_y = (canvasH - 2 * margin) / ht_of_section;
            double yDepth = margin + depth_of_layer * scale_y;

            // Linear interpolation of strain at this depth
            double strainAtDepth = conc_strain + (et - conc_strain) * (depth_of_layer / y_et);

            // Map the strain to canvas X
            double x0 = MapStrainToX(0.0, conc_strain, et, canvasW, margin);
            double xStrain = MapStrainToX(strainAtDepth, conc_strain, et, canvasW, margin);

            // Draw horizontal line
            Line hLine = new Line()
            {
                X1 = x0,
                Y1 = yDepth,
                X2 = xStrain,
                Y2 = yDepth,
                Stroke = fill,
                StrokeThickness = 1
            };
            cnv.Children.Add(hLine);

            // Optional: label the strain value
            TextBlock txt = new TextBlock()
            {
                Text = $"{strainAtDepth:F4}",
                Foreground = fill,
                FontSize = 9
            };
            cnv.Children.Add(txt);
            Canvas.SetLeft(txt, (x0 + xStrain) / 2.0);
            Canvas.SetTop(txt, yDepth - 10);
        }


        static double MapStrainToX(double strain, double minStrain, double maxStrain,
                    double canvasWidth, double margin)
        {
            double scale = (canvasWidth - 2 * margin) / (maxStrain - minStrain);
            return margin + (strain - minStrain) * scale;
        }


    }
}
