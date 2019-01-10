using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guid.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Guid.Api.Controllers
{
    [Route("api/guid")]
    [ApiController]
    public class GuidInfosController : ControllerBase
    {
        private IGuidRepositoryContext _context;
        private static readonly int _expireDays = 30;

        public GuidInfosController(IGuidRepositoryContext context)
        {
            _context = context;
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
                var infos = await _context.GuidInfos.GetAsync(i => i.Guid == guid);
                if (!infos.Any())
                {
                    return NotFound();
                }
                else if (infos[0].Expire < DateTime.UtcNow)
                {
                    // 410 = Gone
                    return StatusCode(410);
                }

                return infos[0].ToGuidInfo();
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
                    // guid info found, update meta data
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
                    _context.GuidInfos.Delete(infos[0]);
                    await _context.SaveChangesAsync();

                    return Ok();
                }
            } 
        }
    }
}
