using Api.Common.Cache;
using Api.Common.Time;
using Guid.Api.Models;
using Guid.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Guid.Api.Controllers
{
    [Route("api/guid")]
    [ApiController]
    public class GuidInfosController : ControllerBase
    {
        private readonly IGuidRepositoryContext _context;
        private readonly IEntityCache<GuidInfoEntity> _cache;
        private readonly ISystemClock _clock;

        private static readonly int _expireDays = 30;
        private static readonly string _cachePrefix = "guidinfo";

        /// <summary>
        /// Constructor.  Accepts dependency injected services.
        /// </summary>
        /// <param name="context">Repository pattern db context.</param>
        /// <param name="cache">Redis cache wrapper.</param>
        public GuidInfosController(
            IGuidRepositoryContext context, 
            IEntityCache<GuidInfoEntity> cache,
            ISystemClock clock
            )
        {
            _context = context;
            _cache = cache;
            _clock = clock;
        }

        /// <summary>
        /// Returns requested guid and its meta data.  Retrieves from Redis cache or database.
        /// </summary>
        /// <param name="id">The requested guid.</param>
        /// <returns>The requested guid and its meta data.</returns>
        /// <remarks>System.Guid accepts guids in 32 upper case, hex charater strings.</remarks>
        [HttpGet("{id}", Name = "GetGuidInfo")]
        [ProducesResponseType(typeof(GuidInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.Gone)]
        public async Task<ActionResult<GuidInfo>> GetGuidInfoAsync(System.Guid id)
        {
            // try cache first
            var info = await _cache.GetEntityAsync(_cachePrefix, id.ToString());
            if (info != null)
            {
                return await ProcessGuidInfo(info, true);
            }
            else
            {
                // not cached, try from database
                var infos = await _context.GuidInfos.GetAsync(i => i.Guid == id);
                if (!infos.Any())
                {
                    return NotFound(new GuidApiError(GuidErrorCode.GuidNotFound));
                }
                else
                {
                    return await ProcessGuidInfo(infos[0], false);
                }
            }
        }

        private async Task<ActionResult<GuidInfo>> ProcessGuidInfo(GuidInfoEntity info, bool fromCache)
        {
            if (info.Expire < _clock.UtcNow)
            {
                // retrieved info has expired
                if (fromCache)
                {
                    // remove it from cache
                    await InvalidateCache(info.Guid);
                }

                // and return 410 code (Gone)
                return StatusCode((int)HttpStatusCode.Gone, new GuidApiError(GuidErrorCode.GuidExpired));
            }
            else
            {
                // this is a valid (not expired) guid
                if (!fromCache)
                {
                    // retrieved from database, add it to cache
                    // NOTE:  Assuming Redis LRU caching, see https://redis.io/topics/lru-cache
                    // NOTE:  Alternatively, we could set an expiration time for entries
                    await _cache.SetEntityAsync(_cachePrefix, info.Guid.ToString(), info);
                }

                return info.ToGuidInfo();
            }
        }

        /// <summary>
        /// Generates a brand new guid with supplied meta data, and inserts it in the database.
        /// </summary>
        /// <param name="info">Guid meta data.</param>
        /// <returns>The created guid and its meta data.</returns>
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
            var date = _clock.UtcNow.AddDays(_expireDays);

            // remove milliseconds since we are dealing with UNIX dates
            return date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
        }

        /// <summary>
        /// Creates a guid entry if provided does not exist; otherwise, updates provided guid meta data.
        /// </summary>
        /// <param name="id">The guid to create or update.</param>
        /// <param name="info">The guid meta data.</param>
        /// <returns>The newly created or updated guid and its meta data.</returns>
        /// <remarks>System.Guid accepts guids in 32 upper case, hex charater strings.</remarks>
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

        /// <summary>
        /// Removes a guid and its meta data from the database.
        /// </summary>
        /// <param name="id">The guid to delete.</param>
        /// <returns>Success or failure status codes.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(GuidApiError), (int)HttpStatusCode.NotFound)]
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
