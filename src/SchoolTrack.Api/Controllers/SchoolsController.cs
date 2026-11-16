using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Schools.Commands.CreateSchool;
using SchoolTrack.Application.Features.Schools.Queries.GetSchoolById;
using SchoolTrack.Application.Features.Schools.Queries.GetSchools;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Schools;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

/// <summary>
/// Manages multi-school tenant onboarding, configurations, and organizational profiles.
/// </summary>
[ApiController]
[Route(ApiRoutes.Schools.Base)]
public sealed class SchoolsController : ControllerBase
{
    private readonly CreateSchoolCommandHandler _createSchoolHandler;
    private readonly GetSchoolsQueryHandler _getSchoolsHandler;
    private readonly GetSchoolByIdQueryHandler _getSchoolByIdHandler;

    public SchoolsController(
        CreateSchoolCommandHandler createSchoolHandler,
        GetSchoolsQueryHandler getSchoolsHandler,
        GetSchoolByIdQueryHandler getSchoolByIdHandler)
    {
        _createSchoolHandler = createSchoolHandler;
        _getSchoolsHandler = getSchoolsHandler;
        _getSchoolByIdHandler = getSchoolByIdHandler;
    }

    /// <summary>
    /// Registers a new school tenant and its initial administrative account. Requires SuperAdmin.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.RequireSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> CreateSchool(
        [FromBody] CreateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSchoolCommand(
            Name: request.Name,
            Code: request.Code,
            TimeZoneId: request.TimeZoneId,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone,
            Address: request.Address,
            AdminEmail: request.AdminEmail,
            AdminPassword: request.AdminPassword,
            AdminFullName: request.AdminFullName);

        var result = await _createSchoolHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetSchoolById),
            new { id = result.Id },
            ApiResponse<SchoolDto>.Ok(
                result,
                message: "School registered successfully.",
                traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Lists schools accessible to the authenticated caller (all for SuperAdmin; single school for SchoolAdmin).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SchoolDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SchoolDto>>>> GetSchools(CancellationToken cancellationToken)
    {
        var result = await _getSchoolsHandler.HandleAsync(new GetSchoolsQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<SchoolDto>>.Ok(
            result,
            message: "Schools retrieved successfully.",
            traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Retrieves a specific school tenant by ID with strict tenant boundary enforcement.
    /// </summary>
    [HttpGet(ApiRoutes.Schools.Id)]
    [Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> GetSchoolById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getSchoolByIdHandler.HandleAsync(new GetSchoolByIdQuery(id), cancellationToken);

        return Ok(ApiResponse<SchoolDto>.Ok(
            result,
            message: "School profile retrieved.",
            traceId: HttpContext.TraceIdentifier));
    }
}

