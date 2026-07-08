using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WebApiTemplate.HealthChecks;

namespace WebApiTemplate.Tests.HealthChecks;

public class ReadinessHealthCheckTests
{
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly Mock<ILogger<ReadinessHealthCheck>> _loggerMock;
    private readonly Mock<IDatabaseHealthChecker> _dbCheckerMock;

    public ReadinessHealthCheckTests()
    {
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _loggerMock = new Mock<ILogger<ReadinessHealthCheck>>();
        _dbCheckerMock = new Mock<IDatabaseHealthChecker>();
        _lifetimeMock.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);
        _dbCheckerMock.Setup(d => d.CheckAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static HealthCheckContext CreateContext()
    {
        var mockCheck = new Mock<IHealthCheck>();
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "ready",
                mockCheck.Object,
                failureStatus: null,
                tags: new[] { "ready" })
        };
    }

    [Test]
    public async Task CheckHealthAsync_WhenNormal_ReturnsHealthy()
    {
        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        var result = await sut.CheckHealthAsync(context);

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenShutdown_ReturnsUnhealthy()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _lifetimeMock.Setup(l => l.ApplicationStopping).Returns(cts.Token);

        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        var result = await sut.CheckHealthAsync(context);

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).Contains("shutting down");
    }

    [Test]
    public async Task CheckHealthAsync_WhenShutdown_DoesNotQueryDatabase()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _lifetimeMock.Setup(l => l.ApplicationStopping).Returns(cts.Token);

        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        await sut.CheckHealthAsync(context);

        _dbCheckerMock.Verify(d => d.CheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CheckHealthAsync_WhenDatabaseFails_ReturnsUnhealthy()
    {
        _dbCheckerMock.Setup(d => d.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection refused"));

        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        var result = await sut.CheckHealthAsync(context);

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).Contains("Database is unavailable");
    }

    [Test]
    public async Task CheckHealthAsync_WhenDatabaseFails_LogsError()
    {
        _dbCheckerMock.Setup(d => d.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection refused"));

        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        await sut.CheckHealthAsync(context);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task CheckHealthAsync_WhenDatabaseFails_CachesUnhealthyResult()
    {
        _dbCheckerMock.Setup(d => d.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection refused"));

        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        var result1 = await sut.CheckHealthAsync(context);
        var result2 = await sut.CheckHealthAsync(context);

        await Assert.That(result1.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result2.Status).IsEqualTo(HealthStatus.Unhealthy);

        // DB called only once (cached on second call)
        _dbCheckerMock.Verify(d => d.CheckAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CheckHealthAsync_WhenCached_ReturnsCachedResult()
    {
        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        var result1 = await sut.CheckHealthAsync(context);
        var result2 = await sut.CheckHealthAsync(context);

        await Assert.That(result1.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result2.Status).IsEqualTo(HealthStatus.Healthy);

        // DB called only once (cached on second call)
        _dbCheckerMock.Verify(d => d.CheckAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CheckHealthAsync_WhenCacheExpired_QueriesDatabaseAgain()
    {
        var sut = new ReadinessHealthCheck(_lifetimeMock.Object, _loggerMock.Object, _dbCheckerMock.Object);
        var context = CreateContext();

        // First call
        await sut.CheckHealthAsync(context);

        // Wait for cache to expire (> 5 seconds)
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Second call should query DB again
        await sut.CheckHealthAsync(context);

        _dbCheckerMock.Verify(d => d.CheckAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
