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

namespace Com.Abbyy.Mobile.Ocr4
{
    public interface IRecognitionContextHolder: IJavaObject
    {
        void Close();
    }
}