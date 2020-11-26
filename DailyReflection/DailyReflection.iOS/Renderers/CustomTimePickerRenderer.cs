using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DailyReflection.Controls;
using DailyReflection.iOS.Renderers;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomTimePicker), typeof(CustomTimePickerRenderer))]
namespace DailyReflection.iOS.Renderers
{
	public class CustomTimePickerRenderer : TimePickerRenderer
	{
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            Control.Layer.BorderWidth = 0;
            Control.BorderStyle = UITextBorderStyle.None;

            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
            {
                UIDatePicker picker = (UIDatePicker)Control.InputView;
                picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;
            }
        }
    }
}