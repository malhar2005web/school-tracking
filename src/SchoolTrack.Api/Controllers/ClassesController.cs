using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Classes;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Classes;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Classes.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class ClassesController : ControllerBase
{
    private readonly CreateClassCommandHandler _createClassHandler;
    private readonly GetClassesQueryHandler _getClassesHandler;

    public ClassesController(
        CreateClassCommandHandler createClassHandler,
        GetClassesQueryHandler getClassesHandler)
    {
        _createClassHandler = createClassHandler;
        _getClassesHandler = getClassesHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClassDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ClassDto>>> CreateClass(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateClassCommand(request.Grade, request.Division, request.AcademicYear);
        var result = await _createClassHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetClasses),
            new { id = result.Id },
            ApiResponse<ClassDto>.Ok(result, "Class created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ClassDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClassDto>>>> GetClasses(CancellationToken cancellationToken)
    {
        var result = await _getClassesHandler.HandleAsync(new GetClassesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ClassDto>>.Ok(result, "Classes retrieved.", HttpContext.TraceIdentifier));
    }
}

