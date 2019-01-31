using Api.Common.Cache;
using Api.Common.Repository;
using Api.Common.Time;
using Guid.Api.Controllers;
using Guid.Api.Models;
using Guid.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Guid.Api.Test
{
    public class GuidInfosControllerTests
    {
        private readonly DateTime _now;
        private Mock<ISystemClock> _mockClock = new Mock<ISystemClock>();

        public GuidInfosControllerTests()
        {
            _now = DateTime.UtcNow;

            // remove milliseconds as per UNIX time
            _now = _now.AddTicks(-(_now.Ticks % TimeSpan.TicksPerSecond));

            _mockClock.Setup(c => c.UtcNow)
                .Returns(_now)
                .Verifiable();
        }

        [Fact]
        public void CreateController()
        {
            // verify creation does not result in exception
            var mockRepo = new Mock<IGuidRepositoryContext>();
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockRepo.Object, mockCache.Object, _mockClock.Object);
        }

        [Fact]
        public async Task CreateGuidInfo_InvalidUser()
        {
            // User must not be null, empty, or whitespace
            var infoBase = new GuidInfoBase()
            {
                User = string.Empty,
                Expire = null
            };

            var mockRepo = new Mock<IGuidRepositoryContext>();
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockRepo.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.CreateGuidInfoAsync(infoBase);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<BadRequestObjectResult>(info.Result);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.InvalidUser);
        }

        [Fact]
        public async Task CreateGuidInfo_DefaultExpire()
        {
            // do not specify expiration date
            var infoBase = new GuidInfoBase()
            {
                User = "User 123"
            };

            // we are going to add to database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<GuidInfoEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // and save changes
            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.CreateGuidInfoAsync(infoBase);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<CreatedAtRouteResult>(info.Result);
            Assert.Equal("GetGuidInfo", result.RouteName);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
            var value = Assert.IsType<GuidInfo>(result.Value);
            Assert.Equal(value.User, infoBase.User);
            Assert.Equal(value.Expire, _now.AddDays(30));
            Assert.True(System.Guid.TryParse(value.Guid, out System.Guid guid));

            _mockClock.Verify();
            mockRepo.Verify();
            mockContext.Verify();
        }

        [Fact]
        public async Task CreateGuidInfo_ExplicitExpire()
        {
            // use explicit expiration date
            var infoBase = new GuidInfoBase()
            {
                User = "User 123",
                Expire = new DateTime(2019, 2, 2, 2, 2, 2, DateTimeKind.Utc)
            };

            // we are going to add to database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<GuidInfoEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // and save changes
            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.CreateGuidInfoAsync(infoBase);

            Assert.IsType<ActionResult<GuidInfo>>(info);
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

        [Fact]
        public async Task GetGuidInfo_NotFound()
        {
            // return no hits from database query
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>())
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object)
                .Verifiable();

            // return no hits from cache
            var guid = System.Guid.NewGuid();
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.GetEntityAsync(It.IsAny<string>(), guid.ToString()))
                .ReturnsAsync((GuidInfoEntity)null)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.GetGuidInfoAsync(guid);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<NotFoundObjectResult>(info.Result);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.GuidNotFound);

            mockRepo.Verify();
            mockContext.Verify();
            mockCache.Verify();
        }

        [Fact]
        public async Task GetGuidInfo_FromCache()
        {
            // return no hits from database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>())
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var cachedEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = _now.AddDays(10)
            };

            // return entity from cache
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.GetEntityAsync(It.IsAny<string>(), cachedEntity.Guid.ToString()))
                .ReturnsAsync(cachedEntity)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.GetGuidInfoAsync(cachedEntity.Guid);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var value = Assert.IsType<GuidInfo>(info.Value);
            Assert.Equal(value.Guid, cachedEntity.Guid.ToString("N").ToUpper());
            Assert.Equal(value.User, cachedEntity.User);
            Assert.Equal(value.Expire, cachedEntity.Expire);

            // verify we accessed cache
            mockCache.Verify(r => r.GetEntityAsync(It.IsAny<string>(), cachedEntity.Guid.ToString()), Times.Once);

            // but not database
            mockRepo.Verify(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetGuidInfo_FromDatabase()
        {
            var dbEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = _now.AddDays(10)
            };

            // return entity from database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>() { dbEntity })
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            // return no hit from cache
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.GetEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString()))
                .ReturnsAsync((GuidInfoEntity)null)
                .Verifiable();

            // make sure we cache found database entry
            mockCache.Setup(r => r.SetEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString(), dbEntity))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.GetGuidInfoAsync(dbEntity.Guid);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var value = Assert.IsType<GuidInfo>(info.Value);
            Assert.Equal(value.Guid, dbEntity.Guid.ToString("N").ToUpper());
            Assert.Equal(value.User, dbEntity.User);
            Assert.Equal(value.Expire, dbEntity.Expire);

            // verify we read cache once (which returned null)
            mockCache.Verify(r => r.GetEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString()), Times.Once);

            // and accessed database once
            mockRepo.Verify(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()), Times.Once);

            // and stored in cache once 
            mockCache.Verify(r => r.SetEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString(), dbEntity), Times.Once);
        }

        [Fact]
        public async Task GetGuidInfo_ExpiredGuidFromCache()
        {
            // use guid created in the past
            var cachedEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = _now.AddDays(-10)
            };

            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            var mockContext = new Mock<IGuidRepositoryContext>();

            // return entity from cache
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.GetEntityAsync(It.IsAny<string>(), cachedEntity.Guid.ToString()))
                .ReturnsAsync(cachedEntity)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.GetGuidInfoAsync(cachedEntity.Guid);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<ObjectResult>(info.Result);
            Assert.Equal(result.StatusCode, (int)HttpStatusCode.Gone);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.GuidExpired);

            mockCache.Verify();
        }

        [Fact]
        public async Task GetGuidInfo_ExpiredGuidFromDatabase()
        {
            // use guid created in the past
            var dbEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = _now.AddDays(-10)
            };

            // retrieve entity from database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>() { dbEntity })
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            // make sure we get no cache hits
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.GetEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString()))
                .ReturnsAsync((GuidInfoEntity)null)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.GetGuidInfoAsync(dbEntity.Guid);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<ObjectResult>(info.Result);
            Assert.Equal(result.StatusCode, (int)HttpStatusCode.Gone);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.GuidExpired);

            mockRepo.Verify();
            mockCache.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateGuidInfo_Create()
        {
            // create with specific guid
            System.Guid guid = System.Guid.NewGuid();
            var infoBase = new GuidInfoBase()
            {
                User = "User 123",
                Expire = new DateTime(2020, 2, 2, 2, 2, 2, DateTimeKind.Utc)
            };

            // make sure we do not find it in database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>())
                .Verifiable();

            // so we can add it there
            mockRepo.Setup(r => r.AddAsync(It.IsAny<GuidInfoEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // and save changes
            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.CreateOrUpdateGuidInfoAsync(guid, infoBase);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var result = Assert.IsType<CreatedAtRouteResult>(info.Result);
            Assert.Equal("GetGuidInfo", result.RouteName);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
            var value = Assert.IsType<GuidInfo>(result.Value);
            Assert.Equal(value.User, infoBase.User);
            Assert.Equal(value.Expire, infoBase.Expire);
            Assert.Equal(value.Guid, guid.ToString("N").ToUpper());

            mockRepo.Verify();
            mockContext.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateGuidInfo_Update()
        {
            // entity in database
            var dbEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = new DateTime(2020, 2, 2, 2, 2, 2, DateTimeKind.Utc)
            };

            // modified properties
            var infoBase = new GuidInfoBase()
            {
                User = "New User",
                Expire = new DateTime(2022, 1, 1, 1, 1, 1, DateTimeKind.Utc)
            };

            // find entity in database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>() { dbEntity })
                .Verifiable();

            // update it
            mockRepo.Setup(r => r.Update(It.IsAny<GuidInfoEntity>()))
                .Verifiable();

            // save it
            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            // and invalidate cache
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.InvalidateEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.CreateOrUpdateGuidInfoAsync(dbEntity.Guid, infoBase);

            Assert.IsType<ActionResult<GuidInfo>>(info);
            var value = Assert.IsType<GuidInfo>(info.Value);
            Assert.Equal(value.User, infoBase.User);
            Assert.Equal(value.Expire, infoBase.Expire);

            mockRepo.Verify();
            mockContext.Verify();
            mockCache.Verify();
        }

        [Fact]
        public async Task DeleteGuidInfo_NotFound()
        {
            System.Guid guid = System.Guid.NewGuid();

            // do not find entity in database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>())
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.DeleteGuidInfoAsync(guid);

            var result = Assert.IsType<NotFoundObjectResult>(info);
            var value = Assert.IsType<GuidApiError>(result.Value);
            Assert.Equal((int)value.Code, (int)GuidErrorCode.GuidNotFound);

            mockRepo.Verify();
        }

        [Fact]
        public async Task DeleteGuidInfo_Found()
        {
            var dbEntity = new GuidInfoEntity()
            {
                Guid = System.Guid.NewGuid(),
                User = "User 123",
                Expire = new DateTime(2020, 2, 2, 2, 2, 2, DateTimeKind.Utc)
            };

            // find entity in database
            var mockRepo = new Mock<IRepository<GuidInfoEntity>>();
            mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<GuidInfoEntity, bool>>>()))
                .ReturnsAsync(new List<GuidInfoEntity>() { dbEntity })
                .Verifiable();

            // remove it from database
            mockRepo.Setup(r => r.Delete(It.IsAny<GuidInfoEntity>()))
                .Verifiable();

            var mockContext = new Mock<IGuidRepositoryContext>();
            mockContext.Setup(r => r.GuidInfos)
                .Returns(mockRepo.Object);

            // and invalidate cache
            var mockCache = new Mock<IEntityCache<GuidInfoEntity>>();
            mockCache.Setup(r => r.InvalidateEntityAsync(It.IsAny<string>(), dbEntity.Guid.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = new GuidInfosController(mockContext.Object, mockCache.Object, _mockClock.Object);
            var info = await controller.DeleteGuidInfoAsync(dbEntity.Guid);

            var result = Assert.IsType<OkResult>(info);
            Assert.Equal(result.StatusCode, (int)HttpStatusCode.OK);

            mockRepo.Verify();
            mockCache.Verify();
        }
    }
}
