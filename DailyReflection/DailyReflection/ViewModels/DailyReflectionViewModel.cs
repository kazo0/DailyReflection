using DailyReflection.Models;
using DailyReflection.Services;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;

namespace DailyReflection.ViewModels
{
	public class DailyReflectionViewModel : ViewModelBase
	{
		private readonly IDailyReflectionService _dailyReflectionService;
		private bool _initialized;
		private Reflection _dailyReflection;
		private bool _hasError;
		private DateTime _today;

		public Reflection DailyReflection
		{
			get => _dailyReflection;
			set { SetProperty(ref _dailyReflection, value); }
		}
		
		public bool HasError
		{
			get => _hasError;
			set { SetProperty(ref _hasError, value); }
		}

		public DateTime Date
		{
			get => _today;
			set { SetProperty(ref _today, value); }
		}

		public ICommand ShareCommand => new AsyncRelayCommand(Share);
		public IAsyncRelayCommand GetReflectionCommand => new AsyncRelayCommand<DateTime>(GetDailyReflection);

		public DailyReflectionViewModel(IDailyReflectionService dailyReflectionService)
		{
			_dailyReflectionService = dailyReflectionService;
			_today = DateTime.Now;
		}

		public async Task Init()
		{
			if (!_initialized)
			{
				_initialized = true;
				GetReflectionCommand.Execute(DateTime.Today);
			}
		}

		private async Task GetDailyReflection(DateTime date)
		{
			Date = date;
			HasError = false;

			var reflection = await _dailyReflectionService.GetDailyReflection(Date);
			if (reflection == null)
			{
				HasError = true;
			}
			else
			{
				HasError = false;
				DailyReflection = reflection;
			}
		}

		private async Task Share()
		{
			await Xamarin.Essentials.Share.RequestAsync(
				title: $"Daily Reflection {Date:MMM dd}",
				text: DailyReflection.ToString());
		}
	}
}
