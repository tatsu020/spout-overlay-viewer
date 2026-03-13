using SpoutOverlay.App.Models;
using SpoutOverlay.App.Services;

namespace SpoutOverlay.Tests;

public sealed class OverlayStateCoordinatorTests
{
    [Fact]
    public void UpdateAvailableSendersMarksReconnectWhenSenderDisappears()
    {
        var coordinator = new OverlayStateCoordinator();
        coordinator.SelectSender("SenderA");

        coordinator.UpdateAvailableSenders(["SenderB"]);

        Assert.True(coordinator.WaitingForReconnect);
    }

    [Fact]
    public void UpdateAvailableSendersClearsReconnectWhenSenderReturns()
    {
        var coordinator = new OverlayStateCoordinator();
        coordinator.SelectSender("SenderA");
        coordinator.UpdateAvailableSenders(["SenderB"]);

        coordinator.UpdateAvailableSenders(["SenderA", "SenderB"]);

        Assert.False(coordinator.WaitingForReconnect);
    }

    [Fact]
    public void SetModeUpdatesCurrentMode()
    {
        var coordinator = new OverlayStateCoordinator();

        coordinator.SetMode(OverlayMode.Passthrough);

        Assert.Equal(OverlayMode.Passthrough, coordinator.Mode);
    }
}
