using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DailyReflection.Core.Extensions;

public static class ServiceCollectionExtensions
{
	// Assembly scanning is invisible to the trimmer: under Native AOT (the store
	// builds) unreferenced subclasses are removed before this runs. The attribute
	// makes any caller inherit the warning; the app's own DI wiring is explicit
	// and does not use this helper.
	[RequiresUnreferencedCode("Enumerates the assembly's types; trimming may have removed subclasses of T.")]
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
