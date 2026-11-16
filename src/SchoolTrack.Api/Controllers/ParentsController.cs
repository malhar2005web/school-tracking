using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Parents;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Parents;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Parents.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class ParentsController : ControllerBase
{
    private readonly CreateParentCommandHandler _createParentHandler;
    private readonly LinkStudentCommandHandler _linkStudentHandler;
    private readonly GetParentsQueryHandler _getParentsHandler;

    public ParentsController(
        CreateParentCommandHandler createParentHandler,
        LinkStudentCommandHandler linkStudentHandler,
        GetParentsQueryHandler getParentsHandler)
    {
        _createParentHandler = createParentHandler;
        _linkStudentHandler = linkStudentHandler;
        _getParentsHandler = getParentsHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ParentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ParentDto>>> CreateParent(
        [FromBody] CreateParentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateParentCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.PrimaryPhone,
            request.AlternatePhone,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.Address);

        var result = await _createParentHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetParents),
            new { id = result.Id },
            ApiResponse<ParentDto>.Ok(result, "Parent registered successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Parents.LinkStudent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> LinkStudent(
        [FromRoute] Guid id,
        [FromBody] LinkStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LinkStudentCommand(
            id,
            request.StudentId,
            request.Relationship,
            request.IsPrimaryGuardian,
            request.CanAuthorizePickup);

        await _linkStudentHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Student linked to parent successfully.", "Success", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ParentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParentDto>>>> GetParents(CancellationToken cancellationToken)
    {
        var result = await _getParentsHandler.HandleAsync(new GetParentsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ParentDto>>.Ok(result, "Parents retrieved.", HttpContext.TraceIdentifier));
    }
}

