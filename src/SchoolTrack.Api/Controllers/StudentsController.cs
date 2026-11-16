using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Students;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Students;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Students.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class StudentsController : ControllerBase
{
    private readonly CreateStudentCommandHandler _createStudentHandler;
    private readonly UpdateStudentCommandHandler _updateStudentHandler;
    private readonly GetStudentsQueryHandler _getStudentsHandler;
    private readonly GetStudentByIdQueryHandler _getStudentByIdHandler;

    public StudentsController(
        CreateStudentCommandHandler createStudentHandler,
        UpdateStudentCommandHandler updateStudentHandler,
        GetStudentsQueryHandler getStudentsHandler,
        GetStudentByIdQueryHandler getStudentByIdHandler)
    {
        _createStudentHandler = createStudentHandler;
        _updateStudentHandler = updateStudentHandler;
        _getStudentsHandler = getStudentsHandler;
        _getStudentByIdHandler = getStudentByIdHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent(
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStudentCommand(
            request.ClassId,
            request.AdmissionNumber,
            request.FirstName,
            request.LastName,
            request.PhotoUrl,
            request.Gender,
            request.DateOfBirth,
            request.EmergencyContactPhone,
            request.SeatNumber);

        var result = await _createStudentHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetStudentById),
            new { id = result.Id },
            ApiResponse<StudentDto>.Ok(result, "Student registered successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut(ApiRoutes.Students.Id)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudent(
        [FromRoute] Guid id,
        [FromBody] UpdateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStudentCommand(
            id,
            request.ClassId,
            request.FirstName,
            request.LastName,
            request.PhotoUrl,
            request.EmergencyContactPhone,
            request.SeatNumber,
            request.IsActive);

        var result = await _updateStudentHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<StudentDto>.Ok(result, "Student updated successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StudentDto>>>> GetStudents(CancellationToken cancellationToken)
    {
        var result = await _getStudentsHandler.HandleAsync(new GetStudentsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StudentDto>>.Ok(result, "Students retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Students.Id)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudentById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getStudentByIdHandler.HandleAsync(new GetStudentByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<StudentDto>.Ok(result, "Student details retrieved.", HttpContext.TraceIdentifier));
    }
}

