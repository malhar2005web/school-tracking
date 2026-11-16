using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Application.Features.Auth.Commands.Login;
using SchoolTrack.Application.Features.Auth.Commands.Logout;
using SchoolTrack.Application.Features.Auth.Commands.RefreshToken;
using SchoolTrack.Application.Features.Auth.Queries.GetCurrentUser;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Auth;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

/// <summary>
/// Handles user authentication, token issuance, refresh token rotation, and active session termination.
/// </summary>
[ApiController]
[Route(ApiRoutes.Auth.Base)]
public sealed class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshTokenHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly GetCurrentUserQueryHandler _currentUserHandler;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshTokenHandler,
        LogoutCommandHandler logoutHandler,
        GetCurrentUserQueryHandler currentUserHandler,
        ICurrentUser currentUser)
    {
        _loginHandler = loginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
        _currentUserHandler = currentUserHandler;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Authenticates a platform or school user and returns a JWT access token and rotating refresh token.
    /// </summary>
    [HttpPost(ApiRoutes.Auth.Login)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LoginCommand(request.Email, request.Password, ip);
        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Ok(
            result,
            message: "Authentication successful.",
            traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Rotates an active refresh token and returns a new access token and replacement refresh token.
    /// </summary>
    [HttpPost(ApiRoutes.Auth.RefreshToken)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshTokenCommand(request.RefreshToken, ip);
        var result = await _refreshTokenHandler.HandleAsync(command, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Ok(
            result,
            message: "Token refreshed successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Revokes the current refresh token or all active sessions for the calling user.
    /// </summary>
    [HttpPost(ApiRoutes.Auth.Logout)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> Logout(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LogoutCommand(request?.RefreshToken, _currentUser.UserId, ip);
        await _logoutHandler.HandleAsync(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Logged out successfully.",
            message: "Session terminated.",
            traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Returns the profile and tenant context of the currently authenticated caller.
    /// </summary>
    [HttpGet(ApiRoutes.Auth.Me)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Unauthorized.", HttpContext.TraceIdentifier));
        }

        var query = new GetCurrentUserQuery(_currentUser.UserId.Value);
        var result = await _currentUserHandler.HandleAsync(query, cancellationToken);

        return Ok(ApiResponse<UserDto>.Ok(
            result,
            message: "User profile retrieved.",
            traceId: HttpContext.TraceIdentifier));
    }
}

