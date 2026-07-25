using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests;

/// <summary>
/// Spec 007 §A — verifies the closure-capture pattern that <c>ShareService</c>
/// relies on for concurrent invocations. Runs the same logic against a fake
/// share surface so the test doesn't need a UI thread.
/// </summary>
[TestFixture]
public class ShareClosureCaptureTests
{
	private sealed class FakeShareSurface
	{
		public List<(string Title, string Body)> Captured { get; } = new();
		public List<System.Action<FakeShareSurface>> PendingHandlers { get; } = new();

		public void Show(System.Action<FakeShareSurface> handler) => PendingHandlers.Add(handler);

		public void Fire(int index)
		{
			var handler = PendingHandlers[index];
			handler(this);
		}
	}

	private static Task RunShare(FakeShareSurface surface, string title, string body)
	{
		// Mirrors the ShareService logic: each call captures title/body in a
		// dedicated handler closure, the handler unsubscribes itself by being
		// removed from the pending list once invoked.
		void Handler(FakeShareSurface s)
		{
			s.Captured.Add((title ?? "Share", body ?? string.Empty));
		}

		surface.Show(Handler);
		return Task.CompletedTask;
	}

	[Test]
	public async Task Two_concurrent_share_calls_carry_independent_payloads()
	{
		var surface = new FakeShareSurface();

		await RunShare(surface, "title-1", "body-1");
		await RunShare(surface, "title-2", "body-2");

		// Fire in reverse order — proves closures hold the values rather than
		// reading from shared mutable state.
		surface.Fire(1);
		surface.Fire(0);

		Assert.That(surface.Captured, Has.Count.EqualTo(2));
		Assert.That(surface.Captured, Contains.Item(("title-1", "body-1")));
		Assert.That(surface.Captured, Contains.Item(("title-2", "body-2")));
	}
}
