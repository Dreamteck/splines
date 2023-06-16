using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    //This is a blank SplineUser-derived class which you can use to build your custom SplineUser
    //You can safely delete any functions that you won't use
    //DO NOT ADD Update, LateUpdate or FixedUpdate, use Run, it is automatically called through one of these methods
    public class BlankUser : SplineUser
    {
        protected override void Awake()
        {
            base.Awake();
            //Awake is also called in the editor
        }

        void Start()
        {
            //Write initialization code here
        }

        protected override void LateRun()
        {
            base.LateRun();
            //Code to run every Update/FixedUpdate/LateUpdate
        }

        protected override void Build()
        {
            base.Build();
            //Build is called after the spline has been sampled. 
            //Use it for calculations (example: generate mesh geometry, calculate object positions)
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            //Called on the main thread after Build has finished
            //Use it to apply the calculations from Build to GameObjects, Transforms, Meshes, etc.
        }

    }
}