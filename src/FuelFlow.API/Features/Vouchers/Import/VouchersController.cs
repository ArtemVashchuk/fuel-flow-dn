using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelFlow.Features.Vouchers.Import;

[ApiController]
[Route("api/[controller]")]
public sealed class VouchersController : ControllerBase
{
    private readonly ImportVouchersCommandHandler _handler;

    public VouchersController(ImportVouchersCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportVouchersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportVouchers(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file was uploaded.");

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are supported.");

        using var stream = file.OpenReadStream();
        var command = new ImportVouchersCommand(stream, file.FileName);
        var result = await _handler.HandleAsync(command, cancellationToken);

        return Ok(result);
    }
}
