using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSkeletonDetectionClient
{
    class FrameData
    {
        public string UserID { get; set; }
        public List<MyJoint> Joints;
        public string Time { get; set; }
        
        public FrameData(string _userID)
        {
            UserID = _userID;
            Joints = new List<MyJoint>();
        }

        /// <summary>
        /// clear joints list
        /// </summary>
        public void ClearJoints()
        {
            Joints.Clear();
        }
        /// <summary>
        /// add joint to joints list
        /// </summary>
        /// <param name="_joint"></param>
        public void AddJoint(MyJoint _joint)
        {
            Joints.Add(_joint);
        }

    }
}
