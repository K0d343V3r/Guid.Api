using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<ActionResult<GuidInfo>> GetGuidInfo(string id)
        {
            if (!System.Guid.TryParse(id, out System.Guid guid))
            {
                return BadRequest();
            }
            else
            {
                // try cache first
                var info = await _cache.GetEntityAsync(_cachePrefix, guid.ToString());
                if (info != null)
                {
                    // return cached entity
                    return info.ToGuidInfo();
                }
                else
                {
                    // not cached, get it from database
                    var infos = await _context.GuidInfos.GetAsync(i => i.Guid == guid);
                    if (!infos.Any())
                    {
                        // entity does not exist
                        return NotFound();
                    }
                    else if (infos[0].Expire >= DateTime.UtcNow)
                    {
                        // entity has not expired, cache it
                        await _cache.SetEntityAsync(_cachePrefix, guid.ToString(), infos[0]);

                        // and return it
                        return infos[0].ToGuidInfo();
                    }
                    else
                    {
                        // entity has expired, remove it from cache
                        await InvalidateCache(guid);

                        // 410 = Gone
                        return StatusCode(410);
                    }
                }
            }
        }

        [HttpPost]
        public async Task<ActionResult<GuidInfo>> CreateGuidInfo([FromBody] GuidInfoBase info)
        {
            return await CreateGuidInfo(System.Guid.NewGuid(), info);
        }

        private async Task<ActionResult<GuidInfo>> CreateGuidInfo(System.Guid guid, GuidInfoBase info)
        {
            if (string.IsNullOrWhiteSpace(info.User))
            {
                return BadRequest();
            }
            else
            {
                var entity = new GuidInfoEntity()
                {
                    Guid = guid,
                    Expire = info.Expire ?? DateTime.UtcNow.AddDays(_expireDays),
                    User = info.User
                };

                await _context.GuidInfos.AddAsync(entity);
                await _context.SaveChangesAsync();

                var guidInfo = entity.ToGuidInfo();
                return CreatedAtRoute("GetGuidInfo", new { id = guidInfo.Guid }, guidInfo);
            }
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<GuidInfo>> UpdateGuidInfo(string id, [FromBody] GuidInfoBase info)
        {
            if (!System.Guid.TryParse(id, out System.Guid guid))
            {
                return BadRequest();
            }
            else
            {
                var infos = await _context.GuidInfos.GetAsync(i => i.Guid == guid);
                if (!infos.Any())
                {
                    // guid info not found, create a new one
                    return await CreateGuidInfo(guid, info);
                }
                else
                {
                    // remove from cache
                    await InvalidateCache(guid);

                    // guid info found, update database
                    infos[0].UpdateFrom(info);
                    _context.GuidInfos.Update(infos[0]);
                    await _context.SaveChangesAsync();

                    return infos[0].ToGuidInfo();
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGuidInfo(string id)
        {
            if (!System.Guid.TryParse(id, out System.Guid guid))
            {
                return BadRequest();
            }
            else
            {
                var infos = await _context.GuidInfos.GetAsync(i => i.Guid == guid);
                if (!infos.Any())
                {
                    return NotFound();
                }
                else
                {
                    // remove from cache
                    await InvalidateCache(guid);

                    // remove from database
                    _context.GuidInfos.Delete(infos[0]);
                    await _context.SaveChangesAsync();

                    return Ok();
                }
            } 
        }

        private async Task InvalidateCache(System.Guid guid)
        {
            await _cache.InvalidateEntityAsync(_cachePrefix, guid.ToString());
        }
    }
}
