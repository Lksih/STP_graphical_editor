using Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
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
}
