using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dto;
using Application.Services;
using Infrastructure.Database;

namespace MusicWebApi.Controllers;

[ApiController]
[Route("playlists")]
public class PlaylistsController : ControllerBase
{
    private readonly UserAlbumRepository _userAlbumRepository;
    private readonly PlatformsService _platformsService;
    private readonly MusicFileRepository _musicFileRepo;

    public PlaylistsController(UserAlbumRepository userAlbumRepository, 
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

    [HttpGet("{playlistId}")]
    public async Task<IResult> GetPlaylist(string playlistId)
    {
        var playlist = await _userAlbumRepository.GetAsync(playlistId);
        if (playlist is null)
            return Results.NotFound();

        return Results.Ok(playlist);
    }

    [HttpGet("user/{userId}")]
    public async Task<IResult> GetUsersPlaylists(string userId)
    {
        var playlist = await _userAlbumRepository.GetUserAlbums(userId);
        if (playlist is null)
            return Results.NotFound();

        return Results.Ok(playlist);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IResult> GetUsersPlaylists()
    {
        Console.WriteLine("Getting user playlists...");
        var sessionId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"Session ID: {sessionId}");
        if (string.IsNullOrEmpty(sessionId))
            return Results.Unauthorized();


        var playlist = await _userAlbumRepository.GetUserAlbums(sessionId);
        if (playlist is null)
            return Results.NotFound();

        return Results.Ok(playlist);
    }
}