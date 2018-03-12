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
    public class CameraPreviewCallbackCus : Java.Lang.Object, Camera.IPreviewCallback
    {
        private MainActivity activity;
        //public IntPtr Handle
        //{
        //    get
        //    {
        //        return base.Handle;
        //    }
        //}

        public CameraPreviewCallbackCus(MainActivity mainActivity)
        {
            activity = mainActivity;
        }

        //public void Dispose()
        //{
        //    base.Dispose();
        //}

        public void OnPreviewFrame(byte[] data, Camera camera)
        {
            // The buffer that we have given to the camera in ITextCaptureService.Callback.onRequestLatestFrame
            // above have been filled. Send it back to the Text Capture Service
            activity.textCaptureService.SubmitRequestedFrame(data);
        }
    }
}