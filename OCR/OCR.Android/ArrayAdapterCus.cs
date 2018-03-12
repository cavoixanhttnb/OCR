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

namespace OCR.Droid
{
    public class ArrayAdapterCus: ArrayAdapter, IFilterable
    {
        public ArrayAdapterCus(Context context, int resource):base(context, resource)
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = base.GetView(position, convertView, parent);
            RelativeLayout.LayoutParams params1 = new RelativeLayout.LayoutParams(
                RelativeLayout.LayoutParams.WrapContent,
                RelativeLayout.LayoutParams.WrapContent);
            view.LayoutParameters = params1;
            return view;
        }
    }
}