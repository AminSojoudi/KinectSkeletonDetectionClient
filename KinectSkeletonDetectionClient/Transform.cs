using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSkeletonDetectionClient
{
    class Transform
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Transform(float _x , float _y , float _z)
        {
            X = _x;
            Y = _y;
            Z = _z;
        }
    }
}
