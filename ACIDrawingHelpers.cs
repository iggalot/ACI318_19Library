using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ACI318_19Library
{
    public static class ACIDrawingHelpers
    {
        // Assume "this" is your Window/UserControl with Canvas named cnvCrossSection
        public static void DrawCrossSection(Canvas cnv, CrossSection section)
        {
            if (section == null || cnv == null) return;

            RebarCatalog catalog = new RebarCatalog();
            cnv.Children.Clear();

            double margin = 10; // pixels around edges

            double canvasW = cnv.ActualWidth > 0 ? cnv.ActualWidth : 200;
            double canvasH = cnv.ActualHeight > 0 ? cnv.ActualHeight : 200;

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
                DrawBarLayer(cnv, layer, section.SideCover, catalog, scale, offsetX, offsetY, section.Width, Brushes.Red);
            }

            // Draw Compression Bars
            foreach (var layer in section.CompressionRebars)
            {
                DrawBarLayer(cnv, layer, section.SideCover,catalog, scale, offsetX, offsetY, section.Width, Brushes.Blue);
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

    }
}
