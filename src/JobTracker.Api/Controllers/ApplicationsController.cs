using JobTracker.Api.Contracts;
using JobTracker.Application.Common.Models;
using JobTracker.Application.Features.Applications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class ApplicationsController(ISender sender) : ControllerBase
{
    /// <summary>List your applications with filtering, sorting and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationSummaryDto>>> List([FromQuery] ListApplicationsQuery query, CancellationToken ct) =>
        await sender.Send(query, ct);

    /// <summary>Pipeline statistics for the dashboard.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApplicationStatsDto>> Stats(CancellationToken ct) =>
        await sender.Send(new GetApplicationStatsQuery(), ct);

    /// <summary>Get one application with notes and status history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDetailsDto>> Get(Guid id, CancellationToken ct) =>
        await sender.Send(new GetApplicationQuery(id), ct);

    /// <summary>Create an application (starts as Wishlist or Applied).</summary>
    [HttpPost]
    [ProducesResponseType<ApplicationDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(SaveApplicationRequest body, CancellationToken ct)
    {
        var created = await sender.Send(new CreateApplicationCommand(body.CompanyName, body.Position, body.WorkMode,
            body.Location, body.JobUrl, body.SalaryMin, body.SalaryMax, body.Currency, body.Status), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update the descriptive fields of an application. Use /status to move it through the pipeline.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDetailsDto>> Update(Guid id, SaveApplicationRequest body, CancellationToken ct) =>
        await sender.Send(new UpdateApplicationCommand(id, body.CompanyName, body.Position, body.WorkMode,
            body.Location, body.JobUrl, body.SalaryMin, body.SalaryMax, body.Currency), ct);

    /// <summary>Move an application to a new pipeline status. Invalid transitions return 422.</summary>
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApplicationDetailsDto>> ChangeStatus(Guid id, ChangeStatusRequest body, CancellationToken ct) =>
        await sender.Send(new ChangeApplicationStatusCommand(id, body.Status, body.Comment), ct);

    /// <summary>Add a note to an application.</summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType<NoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(Guid id, AddNoteRequest body, CancellationToken ct)
    {
        var note = await sender.Send(new AddApplicationNoteCommand(id, body.Text), ct);
        return CreatedAtAction(nameof(Get), new { id }, note);
    }

    /// <summary>Delete an application and all of its notes and history.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteApplicationCommand(id), ct);
        return NoContent();
    }
}
