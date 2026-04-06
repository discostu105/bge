using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BrowserGameEngine.FrontendServer.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase {
	private readonly ReportStore reportStore;
	private readonly CurrentUserContext currentUserContext;

	public ReportsController(ReportStore reportStore, CurrentUserContext currentUserContext) {
		this.reportStore = reportStore;
		this.currentUserContext = currentUserContext;
	}

	/// <summary>Submit a report against another player/user.</summary>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public ActionResult SubmitReport([FromBody] SubmitReportRequest request) {
		if (!currentUserContext.IsValid) return Unauthorized();
		if (string.IsNullOrWhiteSpace(request.TargetUserId)) return BadRequest("TargetUserId is required.");
		if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Reason is required.");
		if (request.TargetUserId == currentUserContext.UserId) return BadRequest("Cannot report yourself.");

		var report = new Report(
			Id: Guid.NewGuid().ToString("N")[..12],
			CreatedAt: DateTime.UtcNow,
			ReporterUserId: currentUserContext.UserId!,
			TargetUserId: request.TargetUserId,
			Reason: request.Reason,
			Details: request.Details?.Trim(),
			Status: ReportStatus.Pending,
			ResolvedByUserId: null,
			ResolutionNote: null,
			ResolvedAt: null
		);
		reportStore.Add(report);
		return StatusCode(201, new { id = report.Id });
	}
}

public record SubmitReportRequest(string TargetUserId, string Reason, string? Details);
