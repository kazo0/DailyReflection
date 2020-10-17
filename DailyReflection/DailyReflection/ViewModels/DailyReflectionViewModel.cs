using DailyReflection.Models;
using DailyReflection.Services;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyReflection.ViewModels
{
	public class DailyReflectionViewModel : ViewModelBase
	{
		private readonly IDailyReflectionService _dailyReflectionService;

		public Reflection DailyReflection { get; set; }
		public bool HasError { get; set; }

		public ICommand RefreshCommand => new AsyncCommand(Refresh);

		public DailyReflectionViewModel(IDailyReflectionService dailyReflectionService)
		{
			_dailyReflectionService = dailyReflectionService;
		}

		public async Task Init()
		{
			await GetDailyReflection();
		}

		private async Task GetDailyReflection()
		{
			IsBusy = true;

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

			IsBusy = false;
		}

		private async Task Refresh()
		{
			await GetDailyReflection();
		}
	}
}
