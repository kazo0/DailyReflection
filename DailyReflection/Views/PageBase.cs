using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Page base that gives each view a uniform lifecycle for messenger
/// registrations and view-model activation. Replaces the ad-hoc per-page
/// constructors that registered messengers without ever unregistering and
/// flipped <c>IsActive</c> on once but never reset it on unload.
/// </summary>
/// <remarks>
/// <para>
/// Non-generic on purpose — Uno's XAML compiler emits a partial declaring
/// <c>: Page</c>, which clashes with a generic <c>PageBase&lt;T&gt;</c>. The
/// concrete view exposes its typed view-model in its own code-behind.
/// </para>
/// <para>
/// On <see cref="Page.Loaded"/>: <c>ActiveViewModel.IsActive = true</c>, then
/// derived classes can register message handlers in <see cref="RegisterMessages"/>.
/// </para>
/// <para>
/// On <see cref="Page.Unloaded"/>: <see cref="UnregisterMessages"/> drops every
/// recipient registered against <c>this</c> on <see cref="WeakReferenceMessenger"/>,
/// then <c>ActiveViewModel.IsActive = false</c>.
/// </para>
/// </remarks>
public abstract class PageBase : Page
{
	protected PageBase()
	{
		Loaded += OnPageLoadedInternal;
		Unloaded += OnPageUnloadedInternal;
	}

	/// <summary>
	/// Returns the page's <see cref="ViewModelBase"/> for activation toggling.
	/// Concrete pages override to expose their typed VM.
	/// </summary>
	protected abstract ViewModelBase ActiveViewModel { get; }

	private void OnPageLoadedInternal(object sender, RoutedEventArgs e)
	{
		ActiveViewModel.IsActive = true;
		RegisterMessages();
		OnPageLoaded();
	}

	private void OnPageUnloadedInternal(object sender, RoutedEventArgs e)
	{
		OnPageUnloaded();
		UnregisterMessages();
		ActiveViewModel.IsActive = false;
	}

	/// <summary>Override to subscribe message handlers when the page is loaded.</summary>
	protected virtual void RegisterMessages() { }

	/// <summary>Default unregisters every recipient keyed against <c>this</c>.</summary>
	protected virtual void UnregisterMessages()
		=> WeakReferenceMessenger.Default.UnregisterAll(this);

	/// <summary>Override for additional Loaded behaviour after VM activation.</summary>
	protected virtual void OnPageLoaded() { }

	/// <summary>Override for additional Unloaded behaviour before VM deactivation.</summary>
	protected virtual void OnPageUnloaded() { }
}
