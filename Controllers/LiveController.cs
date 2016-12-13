using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Relisten.Api;
using Relisten.Api.Models.Api;
using Relisten.Data;

namespace Relisten.Controllers
{
    [Route("api/v2")]
    [Produces("application/json")]
    public class LiveController : RelistenBaseController
    {
        public ShowService _showService { get; set; }
        public SourceTrackService _sourceTrackService { get; set; }

        public LiveController(
            RedisService redis,
            DbService db,
            ShowService showService,
            SourceTrackService sourceTrackService
        ) : base(redis, db)
        {
            _showService = showService;
            _sourceTrackService = sourceTrackService;
        }


        [HttpPostAttribute("live/play")]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
        public async Task<IActionResult> PlayedTrack(int track_id)
        {
            var track = await _sourceTrackService.ForId(track_id);

            if(track == null)
            {
                return JsonNotFound(false);
            }

            return JsonSuccess(true);
        }
    }
}