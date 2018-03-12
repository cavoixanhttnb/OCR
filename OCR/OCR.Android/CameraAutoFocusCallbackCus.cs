using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace OCR.Droid
{
    public class CameraAutoFocusCallbackCus : Java.Lang.Object, Camera.IAutoFocusCallback
    {
        private MainActivity activity;
        private string typeCallback;
        //public IntPtr Handle
        //{
        //    get
        //    {
        //        return base.Handle;
        //    }
        //}

        public CameraAutoFocusCallbackCus(MainActivity mainActivity, string type)
        {
            activity = mainActivity;
            typeCallback = type;
        }

        //public void Dispose()
        //{
        //    base.Dispose();
        //}

        public void OnAutoFocus(bool success, Camera camera)
        {
            activity.onAutoFocusFinished(success, camera);
            switch (typeCallback)
            {
                case "Start":
                    activity.startRecognition();
                    break;
                case "Finish":
                    activity.startButton.Text = "Start";
                    activity.startButton.Enabled = true;
                    if (activity.startRecognitionWhenReady)
                    {
                        activity.startRecognition();
                        activity.startRecognitionWhenReady = false;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}