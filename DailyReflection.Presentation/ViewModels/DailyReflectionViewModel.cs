using DailyReflection.Data.Models;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Share;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyReflection.Presentation.ViewModels
{
	public class DailyReflectionViewModel : ViewModelBase
	{
		private readonly IDailyReflectionService _dailyReflectionService;
		private readonly IShareService _shareService;
		private bool _initialized;
		private Reflection _dailyReflection;
		private bool _hasError;
		private DateTime _date;

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
			get => _date;
			set { SetProperty(ref _date, value); }
		}

		public ICommand ShareCommand { get; }
		public IAsyncRelayCommand GetReflectionCommand { get; }

		public DailyReflectionViewModel(
			IDailyReflectionService dailyReflectionService,
			IShareService shareService)
		{
			GetReflectionCommand = new AsyncRelayCommand<DateTime?>(GetDailyReflection);
			ShareCommand = new AsyncRelayCommand(Share);

			_dailyReflectionService = dailyReflectionService;
			_shareService = shareService;
			_date = DateTime.Now;
		}

		public async Task Init()
		{
			if (!_initialized)
			{
				_initialized = true;
				await GetReflectionCommand.ExecuteAsync(DateTime.Today);
			}
		}

		private async Task GetDailyReflection(DateTime? date = null)
		{
			Date = date ?? DateTime.Today;
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
			await _shareService.ShareText(
				title: $"Daily Reflection {Date:MMM d}",
				body: DailyReflection.ToString());
		}
	}
}
