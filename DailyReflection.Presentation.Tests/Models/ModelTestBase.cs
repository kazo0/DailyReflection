using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests.Models;

[TestFixture(Category = "View Model Tests")]
public abstract class ModelTestBase<TModel>
{
	protected TModel ModelUnderTest { get; private set; } = default!;
	protected abstract TModel GetModel();

	[SetUp]
	public virtual Task Setup()
	{
		ResetDefaults();
		ModelUnderTest = GetModel();
		return Task.CompletedTask;
	}

	/// <summary>
	/// NUnit reuses the fixture instance across tests — override to restore
	/// scenario fields to their defaults so mutations can't pollute later
	/// setups. Called once per test, before <see cref="GetModel"/>.
	/// </summary>
	protected virtual void ResetDefaults()
	{
	}

	/// <summary>
	/// MVUX feeds recompute and ForEach side effects dispatch asynchronously off
	/// the state update path, so assertions on derived values and mock
	/// interactions must tolerate dispatch latency. Polls the assertion until it
	/// passes or the timeout elapses — the outcome is deterministic, only the
	/// wait is bounded.
	/// </summary>
	protected static async Task Eventually(Func<Task> assertion, int timeoutMs = 5000)
	{
		var deadline = Environment.TickCount + timeoutMs;
		while (true)
		{
			try
			{
				await assertion();
				return;
			}
			catch (AssertionException) when (Environment.TickCount < deadline)
			{
				await Task.Delay(25);
			}
		}
	}

	protected static Task Eventually(Action assertion, int timeoutMs = 5000)
		=> Eventually(() =>
		{
			assertion();
			return Task.CompletedTask;
		}, timeoutMs);
}
