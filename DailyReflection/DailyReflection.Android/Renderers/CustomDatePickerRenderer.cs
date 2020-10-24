using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.Graphics.Drawables;
using DailyReflection.Controls;
using DailyReflection.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomDatePicker), typeof(CustomDatePickerRenderer))]
namespace DailyReflection.Droid.Renderers
{
	public class CustomDatePickerRenderer : DatePickerRenderer
	{
        public CustomDatePickerRenderer(Context context) : base(context)
		{

		}
        
        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.Background = null;
            }
        }
    }
}