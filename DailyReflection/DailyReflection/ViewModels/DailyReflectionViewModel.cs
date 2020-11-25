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
		private bool _noNetwork;
		private bool _isRefreshing;
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
		
		public bool NoNetwork
		{
			get => _noNetwork;
			set { SetProperty(ref _noNetwork, value); }
		}
		
		public bool IsRefreshing
		{
			get => _isRefreshing;
			set { SetProperty(ref _isRefreshing, value); }
		}

		public DateTime Today
		{
			get => _today;
			set { SetProperty(ref _today, value); }
		}

		public ICommand RefreshCommand => new AsyncRelayCommand(Refresh);
		public ICommand ShareCommand => new AsyncRelayCommand(Share);

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
				await GetDailyReflection();
			}
		}

		private async Task GetDailyReflection()
		{
			IsRefreshing = true;

			Today = DateTime.Now;

			if (Connectivity.NetworkAccess != NetworkAccess.Internet)
			{
				HasError = true;
				NoNetwork = true;
				IsRefreshing = false;
				return;
			}

			NoNetwork = false;
			HasError = false;

			var reflection = await _dailyReflectionService.GetDailyReflection();
			if (reflection == null)
			{
				HasError = true;
			}
			else
			{
				HasError = false;
				DailyReflection = reflection;
			}

			IsRefreshing = false;
		}

		private async Task Refresh()
		{
			await GetDailyReflection();
		}

		private async Task Share()
		{
			await Xamarin.Essentials.Share.RequestAsync(
				title: $"Daily Reflection {Today:MMM dd}",
				text: DailyReflection.ToString());
		}
	}
}
