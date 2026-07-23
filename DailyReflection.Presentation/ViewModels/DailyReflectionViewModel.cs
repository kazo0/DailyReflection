using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyReflection.Data.Models;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Share;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyReflection.Presentation.ViewModels;

public partial class DailyReflectionViewModel : ViewModelBase
{
	private readonly IDailyReflectionService _dailyReflectionService;
	private readonly IShareService _shareService;
	private bool _initialized;
	
	[ObservableProperty]
	private Reflection _dailyReflection;

	[ObservableProperty]
	private bool _hasError;

	[ObservableProperty]
	private DateTime _date;


	public DailyReflectionViewModel(
		IDailyReflectionService dailyReflectionService,
		IShareService shareService)
	{
		_dailyReflectionService = dailyReflectionService;
		_shareService = shareService;
		_date = DateTime.Now;
	}

	public async Task Init()
	{
		if (!_initialized)
		{
			_initialized = true;
			await GetDailyReflection(DateTime.Today);
		}
	}

	[RelayCommand]
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

	[RelayCommand]
	private async Task Share()
	{
		await _shareService.ShareText(
			title: $"Daily Reflection {Date:MMM d}",
			body: DailyReflection.ToString());
	}
}
