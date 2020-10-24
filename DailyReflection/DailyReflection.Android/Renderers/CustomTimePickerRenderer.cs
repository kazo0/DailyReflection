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
using DailyReflection.Controls;
using DailyReflection.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomTimePicker), typeof(CustomTimePickerRenderer))]
namespace DailyReflection.Droid.Renderers
{
    public class CustomTimePickerRenderer : TimePickerRenderer
    {
        public CustomTimePickerRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.TimePicker> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.Background = null;
            }
        }
    }
}