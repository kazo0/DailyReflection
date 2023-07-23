﻿using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DailyReflection.Core.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static void AddAllSubclassesOf<T>(
			this IServiceCollection services,
			Assembly assembly,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			var types = assembly
				.DefinedTypes
				.Where(t => t.IsSubclassOf(typeof(T)) &&
							!t.IsAbstract);

			foreach (var type in types)
			{
				services.Add(new ServiceDescriptor(type.AsType(), type.AsType(), lifetime));
			}
		}
	}
}
