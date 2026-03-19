using Geometry;
using System;
using System.Collections.Generic;
using Avalonia.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Models
{
    public interface IFigureGraphicProperties
    {
        Color Color { get; }
        double Thickness { get; }
    }

    public class FigureGraphicProperties : IFigureGraphicProperties
    {
        public FigureGraphicProperties(Color color, double thickness)
        {
            Color = color;
            Thickness = thickness;
        }

        public Color Color { get; }

        public double Thickness { get; }
    }
}
