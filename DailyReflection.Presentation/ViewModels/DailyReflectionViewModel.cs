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
				reflection.Thought += @"Contrary to popular belief, Lorem Ipsum is not simply random text. It has roots in a piece of classical Latin literature from 45 BC, making it over 2000 years old. Richard McClintock, a Latin professor at Hampden-Sydney College in Virginia, looked up one of the more obscure Latin words, consectetur, from a Lorem Ipsum passage, and going through the cites of the word in classical literature, discovered the undoubtable source. Lorem Ipsum comes from sections 1.10.32 and 1.10.33 of ""de Finibus Bonorum et Malorum"" (The Extremes of Good and Evil) by Cicero, written in 45 BC. This book is a treatise on the theory of ethics, very popular during the Renaissance. The first line of Lorem Ipsum, ""Lorem ipsum dolor sit amet.."", comes from a line in section 1.10.32.";
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
