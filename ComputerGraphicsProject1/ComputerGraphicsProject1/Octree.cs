using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerGraphicsProject1
{
    public class Octree
    {
        public Octree[] octreeLeaves = new Octree[8];
        public Octree parent;
        public int counter = 0;
        public int nodeNumber = -1;
        public int notNullLeaves = 0;
        public bool isLeaf = true;
        public uint sumRed = 0;
        public uint sumGreen = 0;
        public uint sumBlue = 0;
    }
}
