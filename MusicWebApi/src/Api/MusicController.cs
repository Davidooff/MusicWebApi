using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("music/service")]
public class MusicController : ControllerBase
{
    private PlatformsService _platformsService;

    public MusicController(PlatformsService platformsService) =>
        _platformsService = platformsService;

    [HttpPost("{id}/search")]
    public async Task<IResult> Search (string id, MusicSearch search)
    {
        var result = await _platformsService.Search(search.search, id);
        return Results.Ok(result);
    }

    [HttpGet("{serviceId}/stream/{elId}")]
    public async Task Stream(string serviceId, string elId)
    {
        // 1. grab your not‐yet‐complete source stream
        var source = await _platformsService.StreamTrack(elId, serviceId);

        // 2. tell the client what kind of bytes these are
        Response.ContentType = "application/octet-stream"; // or e.g. "audio/mpeg"

        // 3. disable ASP.NET Core buffering so flushes go straight out
        HttpContext.Features
            .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()
            .DisableBuffering();

        // 4. read/write loop: Kestrel will automatically chunk for you
        var buffer = new byte[81920];
        while (await source.ReadAsync(buffer, 0, buffer.Length) is var bytesRead)
        {
            await Response.Body.WriteAsync(buffer, 0, bytesRead);
            await Response.Body.FlushAsync();    
        }
    }

}

