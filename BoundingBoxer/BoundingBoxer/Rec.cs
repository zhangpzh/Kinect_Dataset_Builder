//------------------------------------------------------------------------------
// <copyright file="Rec.cs" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BoundingBoxer
{
    class Rec
    {
        public Point m_topLeftPoint { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tlp"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public Rec(Point tlp, double w, double h)
        {
            m_topLeftPoint = tlp;
            width = w;
            height = h;
        }
        public Rec()
        { }
    }
}