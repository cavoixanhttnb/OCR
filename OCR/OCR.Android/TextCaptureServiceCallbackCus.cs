using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Abbyy.Mobile.Rtr;
using Java.Lang;

namespace OCR.Droid
{
    public class TextCaptureServiceCallbackCus : Java.Lang.Object, ITextCaptureServiceCallback
    {
        private MainActivity activity;
        //public IntPtr Handle
        //{
        //    get
        //    {
        //        return base.Handle;
        //    }
        //}

        public TextCaptureServiceCallbackCus(MainActivity mainActivity)
        {
            activity = mainActivity;
        }

        //public void Dispose()
        //{
        //    throw new NotImplementedException();
        //}

        public void OnError(Java.Lang.Exception p0)
        {
            // An error occurred while processing. Log it. Processing will continue
            Log.Error("OCR", "Error: " + p0.Message);
            if (BuildConfig.Debug)
            {
                // Make the error easily visible to the developer
                string message = p0.Message;
                if (message == null)
                {
                    message = "Unspecified error while creating the service. See logcat for details.";
                }
                else
                {
                    if (message.Contains("ChineseJapanese.rom"))
                    {
                        message = "Chinese, Japanese and Korean are available in EXTENDED version only. Contact us for more information.";
                    }
                    if (message.Contains("Russian.edc"))
                    {
                        message = "Cyrillic script languages are available in EXTENDED version only. Contact us for more information.";
                    }
                    else if (message.Contains(".trdic"))
                    {
                        message = "Translation is available in EXTENDED version only. Contact us for more information.";
                    }
                }
                activity.errorTextView.Text = message;
            }
        }

        public void OnFrameProcessed(TextCaptureServiceTextLine[] lines, RecognitionServiceResultStabilityStatus resultStatus, RecognitionServiceWarning warning)
        {
            // Frame has been processed. Here we process recognition results. In this sample we
            // stop when we get stable result. This callback may continue being called for some time
            // even after the service has been stopped while the calls queued to this thread (UI thread)
            // are being processed. Just ignore these calls:
            if (!activity.stableResultHasBeenReached)
            {
                if (resultStatus.Ordinal() >= 3)
                {
                    // The result is stable enough to show something to the user
                    activity.surfaceViewWithOverlay.setLines(lines, resultStatus);
                }
                else
                {
                    // The result is not stable. Show nothing
                    activity.surfaceViewWithOverlay.setLines(null, RecognitionServiceResultStabilityStatus.NotReady);
                }

                // Show the warning from the service if any. The warnings are intended for the user
                // to take some action (zooming in, checking recognition language, etc.)
                activity.warningTextView.Text = (warning != null ? warning.Name() : "");

                if (resultStatus == RecognitionServiceResultStabilityStatus.Stable)
                {
                    // Stable result has been reached. Stop the service
                    activity.stopRecognition();
                    activity.stableResultHasBeenReached = true;

                    // Show result to the user. In this sample we whiten screen background and play
                    // the same sound that is used for pressing buttons
                    activity.surfaceViewWithOverlay.setFillBackground(true);
                    activity.startButton.PlaySoundEffect(Android.Views.SoundEffects.Click);
                }
            }
        }

        public void OnRequestLatestFrame(byte[] buffer)
        {
            // The service asks to fill the buffer with image data for the latest frame in NV21 format.
            // Delegate this task to the camera. When the buffer is filled we will receive
            // Camera.PreviewCallback.onPreviewFrame (see below)
            activity.camera.AddCallbackBuffer(buffer);
            activity.textCaptureService.SubmitRequestedFrame(buffer);
        }
    }
}