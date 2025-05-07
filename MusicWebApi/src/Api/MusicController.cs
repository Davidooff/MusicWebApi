using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Infrastructure.Database;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("music/service")]
public class MusicController : ControllerBase
{
    private readonly PlatformsService _platformsService;
    private readonly MusicRepository _musicRepository;

    public MusicController(PlatformsService platformsService, MusicRepository musicRepository)
    {
        _platformsService = platformsService;
        _musicRepository = musicRepository;
    }

    [HttpPost("search")]
    public async Task<IResult> Search (MusicSearch search)
    {
        var result = await _platformsService.Search(search.Search, search.Platform);
        return Results.Ok(result);
    }

    [HttpPost("openPlaylist")]
    public async Task<IResult> OpenPlaylist(MusicSearch search)
    {
        var result = await _platformsService.GetAlbum(search.Search, search.Platform);
        if (result is null)
            return Results.NotFound();
        
        await _musicRepository.AddAlbum(result, search.Platform);
        Console.WriteLine(JsonSerializer.Serialize(result));
        return Results.Ok(result);
    }

    [HttpGet("{serviceId}/stream/{elId}")]
    public async Task Stream(string serviceId, string elId)
    {
        var source = await _platformsService.ListenTrack(elId, (EPlatform)int.Parse(serviceId));

        if (source is null)
        {
            Response.StatusCode = 404;
            return;
        }
        else if (source is (Stream stream, bool isListningAdded))
        {
            Response.ContentType = "application/octet-stream";

            var bodyFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            if (bodyFeature is not null)
            {
                bodyFeature.DisableBuffering();
            }

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                Console.WriteLine("Bytes read: " + bytesRead);
                await Response.Body.WriteAsync(buffer, 0, bytesRead);
                await Response.Body.FlushAsync();
            }
        }
    }

}

