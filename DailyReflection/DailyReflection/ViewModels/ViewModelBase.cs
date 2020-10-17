using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.ViewModels
{
	public abstract class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsBusy { get; set; }

		public INavigation Navigation { get; set; }
	}
}
