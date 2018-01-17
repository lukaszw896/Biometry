using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinningProject
{
    public class Coord
    {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int Stride { get; private set; }
        public int Size { get; private set; }
        public Coord(int height, int width)
        {
            this.Height = height;
            this.Width = width;
            this.Stride = this.Width * 4;
            this.Size = this.Height * this.Stride;
        }
        public Coord(Coord coord)
        {
            this.Height = coord.Height;
            this.Width = coord.Width;
            this.Stride = this.Width * 4;
            this.Size = this.Height * this.Stride;
        }

        public int Get(int x, int y)
        {
            var index = y * this.Stride + x * 4;
            return index;
        }
    }
}
