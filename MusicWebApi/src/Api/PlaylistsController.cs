using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Infrastructure.Database;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("music/service/playlist")]
public class PlayListController : ControllerBase
{
    private readonly UserAlbumRepository _userAlbumRepository;
    private readonly PlatformsService _platformsService;
    private readonly MusicFileRepository _musicFileRepo;

    public PlayListController(UserAlbumRepository userAlbumRepository, 
        PlatformsService platformsService, 
        MusicFileRepository musicFileRepo)
    {
        _userAlbumRepository = userAlbumRepository;
        _platformsService = platformsService;
        _musicFileRepo = musicFileRepo;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IResult> CreatePlaylist(string name)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        await _userAlbumRepository.CreateAsync(userId);
        return Results.Ok();
    }

    [Authorize]
    [HttpPost("addTrack")]
    public async Task<IResult> AddTrack(AddTrack trackDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        var song = await _platformsService.Search(trackDto.TrackId, trackDto.EPlatform);

        if (song is null || song.LongCount() == 0)
            return Results.NotFound();

        await _userAlbumRepository.AddTrack(trackDto.PlayListId, song.FirstOrDefault() ?? throw new Exception());
        
        var trackInfo = _musicFileRepo.FindFileInfo(trackDto.TrackId, trackDto.EPlatform);
        if (trackInfo is null)
        {
            var trackStream = await _platformsService.StreamTrack(trackDto.TrackId, trackDto.EPlatform);
            if (trackStream is Stream stream)
            {
                _musicFileRepo.UploadStreams(trackDto.TrackId, stream, trackDto.EPlatform);
                return Results.Ok();
            }
            return Results.NotFound();
        }

        return Results.Ok();
    }

    [Authorize]
    [HttpPost("removeTrack")]
    public async Task<IResult> RemoveTrack(AddTrack trackDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();


        if (await _userAlbumRepository.RemoveTrack(trackDto.PlayListId, trackDto.TrackId, trackDto.EPlatform))
            return Results.Ok();

        return Results.NotFound();
    }
}