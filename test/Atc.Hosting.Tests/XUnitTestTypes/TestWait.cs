namespace Atc.Hosting.Tests.XUnitTestTypes;

public static class TestWait
{
    // Polls condition in real time until it's true, or fails the test after timeout (default 5 seconds).
    public static async Task UntilAsync(
        Func<bool> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("The condition wasn't met within the timeout.");
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    // Advances fake time in steps, giving the background loops a moment of real time to react after each step,
    // until condition is true, or fails the test after maxSteps steps.
    public static async Task AdvanceUntilAsync(
        FakeTimeProvider timeProvider,
        TimeSpan step,
        Func<bool> condition,
        int maxSteps = 100)
    {
        for (var i = 0; i < maxSteps && !condition(); i++)
        {
            timeProvider.Advance(step);
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.True(condition(), "The condition wasn't met within the allowed fake-time steps.");
    }
}