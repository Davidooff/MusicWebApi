using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dto;
using Application.Services;
using Infrastructure.Database;
using Infrastructure.Redis;
using Infrastructure.Datasbase;
using Domain.Entities;

namespace MusicWebApi.Controllers;

[ApiController]
[Route("playlists")]
public class PlaylistsController : ControllerBase
{
    private readonly UserAlbumRepository _userAlbumRepository;
    private readonly PlatformsService _platformsService;
    private readonly MusicFileRepository _musicFileRepo;
    private readonly UserRedisRepository _userRedis;
    private readonly UsersRepository _usersRepository;

    public PlaylistsController(UserAlbumRepository userAlbumRepository, 
        PlatformsService platformsService, 
        MusicFileRepository musicFileRepo,
        UserRedisRepository userRedis,
        UsersRepository usersRepository)
    {
        _usersRepository = usersRepository;
        _userRedis = userRedis;
        _userAlbumRepository = userAlbumRepository;
        _platformsService = platformsService;
        _musicFileRepo = musicFileRepo;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IResult> CreatePlaylist(CreatePlaylist createDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        var user = await _usersRepository.GetByIdAsync(userId);

        if (user is null) return Results.Unauthorized();

        await _userAlbumRepository.CreateAsync(userId, user.Username, createDto.Name);
        var update = await _userRedis.UpdateUserAlbums(userId);
        if (update is null) return Results.BadRequest();

        return Results.Ok(update.Value.data);
    }

    [Authorize]
    [HttpPost("add-track")]
    public async Task<IResult> AddTrack(AddTrack trackDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        var album = await _platformsService.GetAlbum(trackDto.AlbumId, trackDto.EPlatform);

        if (album is null)
            return Results.NotFound();

        var track = album.Trackes.FirstOrDefault(el => el.Id == trackDto.TrackId);

        if (track is null) return Results.NotFound();

        await _userAlbumRepository.AddTrack(trackDto.PlayListId, new(track, trackDto.EPlatform, album.Author));
        await _userRedis.UpdateUserAlbums(userId);
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
    [HttpPost("remove-track")]
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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var playlist = await _userRedis.GetUserAlbums(userId);
        if (playlist is null)
        {
            await _userRedis.UpdateUserAlbums(userId);
            playlist = await _userRedis.GetUserAlbums(userId);
            if (playlist is null) return Results.NotFound();
        }
        return Results.Ok(playlist);
    }
}