using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Relisten.Api;
using Dapper;
using Relisten.Api.Models;
using Relisten.Api.Models.Api;
using Relisten.Data;
using Newtonsoft.Json;

namespace Relisten.Controllers
{
    [Route("api/v2")]
    [Produces("application/json")]
    public class ShowsController : RelistenBaseController
    {
        public ShowService _showService { get; set; }
        public SourceService _sourceService { get; set; }

        public ShowsController(
            RedisService redis,
            DbService db,
			ArtistService artistService,
            ShowService showService,
            SourceService sourceService
		) : base(redis, db, artistService)
        {
            _showService = showService;
            _sourceService = sourceService;
        }

        [HttpGet("shows/today")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
        public async Task<IActionResult> Today()
        {
            return JsonSuccess(await _showService.ShowsForCriteriaWithArtists(@"
                EXTRACT(month from s.date) = EXTRACT(month from NOW())
	            AND EXTRACT(day from s.date) = EXTRACT(day from NOW())
            ", new { }));
        }

        [HttpGet("shows/on-date")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
		public async Task<IActionResult> OnDayInHistory([FromQuery] int month, [FromQuery] int day)
        {
            return JsonSuccess(await _showService.ShowsForCriteriaWithArtists(@"
                EXTRACT(month from s.date) = @month
	            AND EXTRACT(day from s.date) = @day
            ", new { month, day }));
        }

        [HttpGet("shows/recently-performed")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
		public async Task<IActionResult> RecentlyPerformed([FromQuery] string[] artistIds = null, [FromQuery] int? shows = null, [FromQuery] int? days = null)
        {
            return await ApiRequest(artistIds, (arts) => _showService.RecentlyPerformed(arts, shows, days));
        }

        [HttpGet("shows/recently-updated")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
		public async Task<IActionResult> RecentlyUpdated([FromQuery] string[] artistIds = null, [FromQuery] int? shows = null, [FromQuery] int? days = null)
        {
            return await ApiRequest(artistIds, (arts) => _showService.RecentlyUpdated(arts, shows, days));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/today")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> TodayArtist([FromRoute] string artistIdOrSlug)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.ShowsForCriteriaWithArtists(@"
                s.artist_id = @artistId
                AND EXTRACT(month from s.date) = EXTRACT(month from NOW())
	            AND EXTRACT(day from s.date) = EXTRACT(day from NOW())
            ", new { artistId = art.id }));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/recently-performed")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> ArtistRecentlyPerformed([FromRoute] string artistIdOrSlug, [FromQuery] int? shows = null, [FromQuery] int? days = null)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.RecentlyPerformed(new[] { art }, shows, days));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/recently-updated")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> ArtistRecentlyUpdated([FromRoute] string artistIdOrSlug, [FromQuery] int? shows = null, [FromQuery] int? days = null)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.RecentlyUpdated(new[] { art }, shows, days));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/on-date")]
        [ProducesResponseType(typeof(IEnumerable<ShowWithArtist>), 200)]
		public async Task<IActionResult> ArtistOnDayInHistory([FromRoute] string artistIdOrSlug, [FromQuery] int month, [FromQuery] int day)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.ShowsForCriteriaWithArtists(@"
                s.artist_id = @artistId
                AND EXTRACT(month from s.date) = @month
	            AND EXTRACT(day from s.date) = @day
            ", new { artistId = art.id, month, day }));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/top")]
        [ProducesResponseType(typeof(IEnumerable<Show>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> TopByArtist([FromRoute] string artistIdOrSlug, [FromQuery] int limit = 25)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.ShowsForCriteria(art, @"
                s.artist_id = @artistId
            ", new { artistId = art.id }, limit, "cnt.max_avg_rating_weighted DESC"));
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/random")]
        [ProducesResponseType(typeof(ShowWithSources), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> RandomByArtist([FromRoute] string artistIdOrSlug)
        {
            return await ApiRequest(artistIdOrSlug, async (art) =>
            {
                var randShow = await db.WithConnection(con => con.QuerySingleAsync<Show>(@"
                    SELECT
                        *
                    FROM
                        shows
                    WHERE
                        artist_id = @id
                    ORDER BY
                        RANDOM()
                    LIMIT 1
                ", art));

                return await _showService.ShowWithSourcesForArtistOnDate(art, randShow.display_date);
            });
        }

        [HttpGet("artists/{artistIdOrSlug}/shows/{showDate}")]
        [ProducesResponseType(typeof(ShowWithSources), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
		public async Task<IActionResult> ShowsOnSpecificDate([FromRoute] string artistIdOrSlug, [FromRoute] string showDate)
        {
            return await ApiRequest(artistIdOrSlug, (art) => _showService.ShowWithSourcesForArtistOnDate(art, showDate));
        }

        [HttpGet("artists/{artistIdOrSlug}/sources/{sourceId}/reviews")]
        [ProducesResponseType(typeof(IEnumerable<SourceReview>), 200)]
        [ProducesResponseType(typeof(ResponseEnvelope<bool>), 404)]
        public Task<IActionResult> ReviewsForShow([FromRoute] string artistIdOrSlug, [FromRoute] int sourceId)
        {
            return ApiRequest(artistIdOrSlug, (art) => _sourceService.ReviewsForSource(sourceId));
        }
    }
}
