using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentAssertions;
using Grex.Models;
using Xunit;

// NOTE: namespace is Grex.Tests (not Grex.Tests.Models) on purpose. A
// Grex.Tests.Models namespace would shadow the Grex.Models namespace for
// every Grex.Tests.* file that uses the "Models.Foo" idiom (e.g.
// SearchServiceTests, TabViewModelTests), breaking their compilation.
namespace Grex.Tests
{
    public class RangeObservableCollectionTests
    {
        [Fact]
        public void AddRange_AddsItemsAndRaisesSingleResetEvent()
        {
            // Arrange
            var collection = new RangeObservableCollection<int>();
            var events = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (_, e) => events.Add(e.Action);

            // Act
            collection.AddRange(Enumerable.Range(1, 100));

            // Assert
            collection.Count.Should().Be(100);
            events.Should().ContainSingle().Which.Should().Be(NotifyCollectionChangedAction.Reset);
        }

        [Fact]
        public void Reset_ReplacesItemsAndRaisesSingleResetEvent()
        {
            // Arrange
            var collection = new RangeObservableCollection<int> { 1, 2, 3 };
            var events = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (_, e) => events.Add(e.Action);

            // Act
            collection.Reset(new[] { 10, 20, 30, 40 });

            // Assert
            collection.Should().Equal(10, 20, 30, 40);
            events.Should().ContainSingle().Which.Should().Be(NotifyCollectionChangedAction.Reset);
        }

        [Fact]
        public void AddRange_WithEmptyCollection_DoesNotRaiseEvent()
        {
            // Arrange
            var collection = new RangeObservableCollection<int>();
            var eventRaised = false;
            collection.CollectionChanged += (_, _) => eventRaised = true;

            // Act
            collection.AddRange(new List<int>());

            // Assert
            eventRaised.Should().BeFalse();
            collection.Count.Should().Be(0);
        }
    }
}
