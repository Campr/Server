using System;
using System.Linq;
using Campr.Server.Lib.Helpers;
using Campr.Server.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Campr.Server.Tests.UnitTests.Helpers
{
    public class UriHelpersTests
    {
        public UriHelpersTests()
        {
            this.uriHelpers = ServiceProvider.Current.GetService<IUriHelpers>();
        }

        private readonly IUriHelpers uriHelpers;

        [Fact]
        public void IsCamprHandle_GoodHandles()
        {
            // Arrange.
            var goodHandles = new []
            {
                "quentez",
                "plau-rac"
            };

            // Act.
            var results = goodHandles.Select(this.uriHelpers.IsCamprHandle);

            // Assert.
            Assert.True(results.All(r => r));
        }

        [Fact]
        public void IsCamprHandle_BadHandles()
        {
            // Arrange.
            var badHandles = new[]
            {
                "quen@tez",
                "quen@tez.com",
                "ab",
                "a.b",
                "verylonghandlethatisverylongandmore",
                "quentez" + Environment.NewLine
            };

            // Act.
            var results = badHandles.Select(this.uriHelpers.IsCamprHandle);

            // Assert.
            Assert.True(results.All(r => !r));
        }

        [Fact]
        public void IsCamprEntity_GoodEntities()
        {
            // Arrange.
            var goodEntities = new []
            {
                new { Entity = "https://quentez.campr.me/", Handle = "quentez" },
                new { Entity = "https://quent-ez.campr.me/", Handle = "quent-ez" }
            };

            foreach (var entity in goodEntities)
            {
                // Act.
                string actualHandle;
                var actualResult = this.uriHelpers.IsCamprEntity(entity.Entity, out actualHandle);

                // Assert.
                Assert.True(actualResult);
                Assert.Equal(entity.Handle, actualHandle);
            }
        }

        [Fact]
        public void IsCamprEntity_BadEntities()
        {
            // Arrange.
            var badEntities = new[]
            {
                "http://quentez.campr.me/",
                "http://quentez@.campr.me/"
            };

            foreach (var entity in badEntities)
            {
                // Act.
                string actualHandle;
                var actualResult = this.uriHelpers.IsCamprEntity(entity, out actualHandle);

                // Assert.
                Assert.False(actualResult);
            }
        }

        [Fact]
        public void IsCamprDomain_GoodDomains()
        {
            // Arrange.
            var goodDomains = new[]
            {
                new { Domain = "quentez.campr.me", Handle = "quentez" },
                new { Domain = "quent-ez.campr.me", Handle = "quent-ez" }
            };

            foreach (var goodDomain in goodDomains)
            {
                // Act.
                string actualHandle;
                var result = this.uriHelpers.IsCamprDomain(goodDomain.Domain, out actualHandle);

                // Assert.
                Assert.True(result);
                Assert.Equal(goodDomain.Handle, actualHandle);
            }
        }

        [Fact]
        public void IsCamprDomain_BadDomains()
        {
            // Arrange.
            var badDomains = new[]
            {
                "quent.ez.campr.me",
                "quentez@.campr.me",
                "quentez.campr",
                "quentez.tent.is"
            };

            foreach (var badDomain in badDomains)
            {
                // Act.
                string actualHandle;
                var result = this.uriHelpers.IsCamprDomain(badDomain, out actualHandle);

                // Assert.
                Assert.False(result);
                Assert.Null(actualHandle);
            }
        }

        [Fact]
        public void GetStandardEntity_Tests()
        {
            // Arrange.
            var entities = new[]
            {
                new { Source = "http://quentez.campr.me", Expected = "http://quentez.campr.me" },
                new { Source = "http://quentez.campr.me/", Expected = "http://quentez.campr.me" },
                new { Source = "http://quentez.campr.me//", Expected = "http://quentez.campr.me" }
            };

            foreach (var entity in entities)
            {
                // Act.
                var actualEntity = this.uriHelpers.GetStandardEntity(entity.Source);

                // Assert.
                Assert.Equal(entity.Expected, actualEntity);
            }
        }
    }
}