using ModernFlyouts.Core;
using ModernFlyouts.Core.Display;
using ModernFlyouts.Helpers;
using System;
using System.Linq;
using Xunit;

namespace ModernFlyouts.Tests
{
    public class FlyoutManagerTests
    {
        [Fact]
        public void Initialize_ShouldSetUpDisplayMonitor()
        {
            // Arrange
            var manager = FlyoutManager.Instance;
            var preferredMonitorId = "test-monitor";

            // Act
            manager.Initialize();

            // Assert
            Assert.NotNull(manager.OnScreenFlyoutWindow);
            Assert.NotNull(manager.OnScreenFlyoutView);
            Assert.NotNull(manager.UIManager);
        }

        [Fact]
        public void AlignFlyout_ShouldPositionWindowCorrectly()
        {
            // Arrange
            var manager = FlyoutManager.Instance;
            manager.Initialize();
            var position = new BindablePoint { X = 100, Y = 100 };

            // Act
            manager.AlignFlyout(position);

            // Assert
            Assert.Equal(100, manager.OnScreenFlyoutWindow.Left);
            Assert.Equal(100, manager.OnScreenFlyoutWindow.Top);
        }

        [Fact]
        public void MoveFlyoutToAnotherMonitor_ShouldUpdatePosition()
        {
            // Arrange
            var manager = FlyoutManager.Instance;
            manager.Initialize();
            var monitor = DisplayManager.Instance.PrimaryDisplayMonitor;
            manager.OnScreenFlyoutPreferredMonitor = monitor;

            // Act
            manager.MoveFlyoutToAnotherMonitor();

            // Assert
            var expectedX = monitor.Bounds.Left + monitor.Bounds.Width / 2;
            var expectedY = monitor.Bounds.Top + monitor.Bounds.Height / 2;
            Assert.Equal(expectedX, manager.OnScreenFlyoutWindow.Left);
            Assert.Equal(expectedY, manager.OnScreenFlyoutWindow.Top);
        }
    }
} 