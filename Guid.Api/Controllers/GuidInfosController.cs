using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Api.Common.Cache;
using Guid.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Guid.Api.Controllers
{
    [Route("api/guid")]
    [ApiController]
    public class GuidInfosController : ControllerBase
    {
        private readonly IGuidRepositoryContext _context;
        private readonly IEntityCache<GuidInfoEntity> _cache;

        private static readonly int _expireDays = 30;
        private static readonly string _cachePrefix = "guidinfo";

        public GuidInfosController(IGuidRepositoryContext context, IEntityCache<GuidInfoEntity> cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("{id}", Name = "GetGuidInfo")]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.Gone)]
        public async Task<ActionResult<GuidInfo>> GetGuidInfoAsync(System.Guid id)
        {
            // try cache first
            var info = await _cache.GetEntityAsync(_cachePrefix, id.ToString());
            if (info != null)
            {
                // return cached entity
                return info.ToGuidInfo();
            }
            else
            {
                // not cached, get it from database
                var infos = await _context.GuidInfos.GetAsync(i => i.Guid == id);
                if (!infos.Any())
                {
                    // entity does not exist
                    return NotFound(new GuidApiError(GuidErrorCode.GuidNotFound));
                }
                else if (infos[0].Expire >= DateTime.UtcNow)
                {
                    // entity has not expired, cache it
                    await _cache.SetEntityAsync(_cachePrefix, id.ToString(), infos[0]);

                    // and return it
                    return infos[0].ToGuidInfo();
                }
                else
                {
                    // entity has expired, remove it from cache
                    await InvalidateCache(id);

                    // Gone = 410
                    return StatusCode((int)HttpStatusCode.Gone, new GuidApiError(GuidErrorCode.GuidExpired));
                }
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<GuidInfo>> CreateGuidInfoAsync([FromBody] GuidInfoBase info)
        {
            return await CreateGuidInfo(System.Guid.NewGuid(), info);
        }

        private async Task<ActionResult<GuidInfo>> CreateGuidInfo(System.Guid guid, GuidInfoBase info)
        {
            if (string.IsNullOrWhiteSpace(info.User))
            {
                return BadRequest(new GuidApiError(GuidErrorCode.InvalidUser));
            }
            else
            {
                var entity = new GuidInfoEntity()
                {
                    Guid = guid,
                    Expire = info.Expire ?? GetDefaultExpireDate(),
                    User = info.User
                };

                await _context.GuidInfos.AddAsync(entity);
                await _context.SaveChangesAsync();

                var guidInfo = entity.ToGuidInfo();
                return CreatedAtRoute("GetGuidInfo", new { id = guidInfo.Guid }, guidInfo);
            }
        }

        private DateTime GetDefaultExpireDate()
        {
            // default is 30 days from now
            var date = DateTime.UtcNow.AddDays(_expireDays);

            // remove milliseconds since we are dealing with UNIX dates
            return date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
        }

        [HttpPost("{id}")]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<GuidInfo>> CreateOrUpdateGuidInfoAsync(System.Guid id, [FromBody] GuidInfoBase info)
        {
            var infos = await _context.GuidInfos.GetAsync(i => i.Guid == id);
            if (!infos.Any())
            {
                // guid info not found, create a new one
                return await CreateGuidInfo(id, info);
            }
            else
            {
                // remove from cache
                await InvalidateCache(id);

                // guid info found, update database
                infos[0].UpdateFrom(info);
                _context.GuidInfos.Update(infos[0]);
                await _context.SaveChangesAsync();

                return infos[0].ToGuidInfo();
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> DeleteGuidInfoAsync(System.Guid id)
        {
            var infos = await _context.GuidInfos.GetAsync(i => i.Guid == id);
            if (!infos.Any())
            {
                return NotFound(new GuidApiError(GuidErrorCode.GuidNotFound));
            }
            else
            {
                // remove from cache
                await InvalidateCache(id);

                // remove from database
                _context.GuidInfos.Delete(infos[0]);
                await _context.SaveChangesAsync();

                return Ok();
            }
        }

        private async Task InvalidateCache(System.Guid guid)
        {
            await _cache.InvalidateEntityAsync(_cachePrefix, guid.ToString());
        }
    }
}
