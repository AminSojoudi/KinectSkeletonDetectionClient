using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSkeletonDetectionClient
{
    class MyJoint
    {
        public Transform transform { get; set; }
        public string jointName { get; set; }

        public MyJoint(string _name , Transform _transform)
        {
            jointName = _name;
            transform = _transform;
        }
    }
}
