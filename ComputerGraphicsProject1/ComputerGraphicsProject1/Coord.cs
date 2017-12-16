using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerGraphicsProject1
{
    public class Coord
    {
        private int Height { get; set; }
        private int Width { get; set; }
        public int Stride { get; set; }
        public int Size { get; set; }
        public Coord(int height, int width)
        {
            this.Height = height;
            this.Width = width;
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
