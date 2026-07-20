using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Web.Controllers;

/// <summary>
/// Download completed Excel export jobs produced by the background export worker.
/// </summary>
[Authorize]
public sealed class ExportsController(IExcelExportService exports) : ControllerBase
{
    [HttpGet("/exports/download/{jobId:guid}")]
    public IActionResult Download(Guid jobId)
    {
        var path = exports.GetFilePath(jobId);
        if (path is null)
        {
            return NotFound();
        }

        var job = exports.GetJob(jobId);
        var downloadName = job?.FileName ?? Path.GetFileName(path);
        return PhysicalFile(
            path,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            downloadName);
    }
}
