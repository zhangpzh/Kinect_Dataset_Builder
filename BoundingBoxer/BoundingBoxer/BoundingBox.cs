using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoundingBoxer
{
    class BoundingBox
    {
        public int m_personId { get; set; }
        public Rec rectangle  { get; set; }

        public BoundingBox(int id, Rec rec)
        {
            m_personId = id;
            rectangle = rec;
        }
        public BoundingBox()
        { }
    }
}
