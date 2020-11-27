﻿using DailyReflection.Core.Extensions;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.DependencyInjection
{
	public static class Dependencies
	{
		public static void AddPresentationDependencies(this IServiceCollection services)
		{
			services.AddAllSubclassesOf<ViewModelBase>(typeof(ViewModelBase).Assembly, ServiceLifetime.Singleton);
			services.AddServiceDependencies();
		}
	}
}
