using Geometry;
using System;
using System.Collections.Generic;
using Avalonia.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry.Graphic
{
    public interface IFigureGraphicProperties
    {
        Color Color { get; }
        double Thickness { get; }
        bool IsFilled { get; }
        Color FillColor { get; }
    }

    public class FigureGraphicProperties : IFigureGraphicProperties
    {
        public FigureGraphicProperties(Color color, double thickness, bool isFilled = false, Color? fillColor = null)
        {
            Color = color;
            Thickness = thickness;
            IsFilled = isFilled;
            FillColor = fillColor ?? color;
        }

        public Color Color { get; }

        public double Thickness { get; }

        public bool IsFilled { get; }

        public Color FillColor { get; }
    }
}
