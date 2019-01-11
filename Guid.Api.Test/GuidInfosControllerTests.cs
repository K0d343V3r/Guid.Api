using Api.Common.Cache;
using Api.Common.Repository;
using Api.Common.Time;
using Guid.Api.Controllers;
using Guid.Api.Models;
using Guid.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Guid.Api.Test
{
    public class GuidInfosControllerTests
    {
        [Fact]
        public void CreateController()
        {
            var mockRepo = new Mock<IGuidRepositoryContext>();
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            var mockClock = new Mock<ISystemClock>();

            var controller = new GuidInfosController(mockRepo.Object, mockCache.Object, mockClock.Object);
        }

        [Fact]
        public async Task CreateGuidInfo_InvalidUser()
        {
            var mockRepo = new Mock<IGuidRepositoryContext>();
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            var mockClock = new Mock<ISystemClock>();

            var controller = new GuidInfosController(mockRepo.Object, mockCache.Object, mockClock.Object);

            var infoBase = new GuidInfoBase()
            {
                User = string.Empty,
                Expire = null
            };

            var info = await controller.CreateGuidInfoAsync(infoBase);
            var result = Assert.IsType<BadRequestObjectResult>(info.Result);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.InvalidUser);
        }

        [Fact]
        public async Task CreateGuidInfo_DefaultExpire()
        {
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<GuidInfoEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            var mockClock = new Mock<ISystemClock>();
            var now = DateTime.UtcNow;
            now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
            mockClock.Setup(c => c.UtcNow)
                .Returns(now)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, mockClock.Object);

            var infoBase = new GuidInfoBase()
            {
                User = "Cylance, Inc."
            };

            var info = await controller.CreateGuidInfoAsync(infoBase);
            var result = Assert.IsType<CreatedAtRouteResult>(info.Result);
            Assert.Equal("GetGuidInfo", result.RouteName);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
            var value = Assert.IsType<GuidInfo>(result.Value);
            Assert.Equal(value.User, infoBase.User);
            Assert.Equal(value.Expire, now.AddDays(30));
            Assert.True(System.Guid.TryParse(value.Guid, out System.Guid guid));

            mockClock.Verify();
            mockRepo.Verify();
            mockContext.Verify();
        }

        [Fact]
        public async Task CreateGuidInfo_ExplicitExpire()
        {
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<GuidInfoEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            var mockClock = new Mock<ISystemClock>();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, mockClock.Object);

            var infoBase = new GuidInfoBase()
            {
                User = "Cylance, Inc.",
                Expire = new DateTime(2019, 2, 2, 2, 2, 2, DateTimeKind.Utc)
            };

            var info = await controller.CreateGuidInfoAsync(infoBase);
            var result = Assert.IsType<CreatedAtRouteResult>(info.Result);
            Assert.Equal("GetGuidInfo", result.RouteName);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
            var value = Assert.IsType<GuidInfo>(result.Value);
            Assert.Equal(value.User, infoBase.User);
            Assert.Equal(value.Expire, infoBase.Expire);
            Assert.True(System.Guid.TryParse(value.Guid, out System.Guid guid));

            mockRepo.Verify();
            mockContext.Verify();
        }
    }
}
