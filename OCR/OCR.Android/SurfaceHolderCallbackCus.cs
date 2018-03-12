using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace OCR.Droid
{
    public class SurfaceHolderCallbackCus : Java.Lang.Object, ISurfaceHolderCallback
    {

        private MainActivity activity;

        public SurfaceHolderCallbackCus(MainActivity mainActivity)
        {
            activity = mainActivity;
        }

        //public IntPtr Handle
        //{
        //    get
        //    {
        //        return base.Handle;
        //    }
        //}

        //public void Dispose()
        //{
        //    base.Dispose();
        //}

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            if (activity.camera != null)
            {
                activity.setCameraPreviewDisplayAndStartPreview();
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            activity.previewSurfaceHolder = holder;
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            // When surface is destroyed, clear previewSurfaceHolder
            activity.previewSurfaceHolder = null;
        }
    }
}