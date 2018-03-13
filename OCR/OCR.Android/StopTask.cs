using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace OCR.Droid
{
    public class StopTask : AsyncTask
    {
        private MainActivity activity;
        public StopTask(MainActivity mainActivity)
        {
            activity = mainActivity;
        }

        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
        {
           
            activity.textCaptureService.Stop();
            return null;
        }

        protected override void OnPostExecute(Java.Lang.Object result)
        {
            if (activity.previewSurfaceHolder != null)
            {
                // Restore normal power saving behaviour
                activity.previewSurfaceHolder.SetKeepScreenOn(false);
            }
            // Change the text on the stop button back to 'Start'
            activity.startButton.Text = "Start";
            activity.startButton.Enabled = true;
        }
    }
}