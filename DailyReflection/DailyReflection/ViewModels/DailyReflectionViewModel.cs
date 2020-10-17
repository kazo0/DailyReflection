﻿using DailyReflection.Models;
using DailyReflection.Services;
using DailyReflection.Views;
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
		public bool IsRefreshing { get; set; }
		public DateTime Today { get; set; } = DateTime.Now;

		public ICommand RefreshCommand => new AsyncCommand(Refresh);
		public ICommand ShareCommand => new AsyncCommand(Share);
		public ICommand NavToSettingsCommand => new AsyncCommand(NavToSettings);

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
			IsRefreshing = true;

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

		private async Task NavToSettings()
		{
			await Navigation.PushAsync(new SettingsView());
		}

	}
}
