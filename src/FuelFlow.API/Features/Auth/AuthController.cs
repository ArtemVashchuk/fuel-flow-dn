using FuelFlow.Features.Auth.Refresh;
using FuelFlow.Features.Auth.SendCode;
using FuelFlow.Features.Auth.Verify;
using Microsoft.AspNetCore.Mvc;

namespace FuelFlow.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly SendCodeCommandHandler _sendCodeHandler;
    private readonly VerifyCodeCommandHandler _verifyCodeHandler;
    private readonly RefreshTokenCommandHandler _refreshTokenHandler;

    public AuthController(
        SendCodeCommandHandler sendCodeHandler,
        VerifyCodeCommandHandler verifyCodeHandler,
        RefreshTokenCommandHandler refreshTokenHandler)
    {
        _sendCodeHandler = sendCodeHandler;
        _verifyCodeHandler = verifyCodeHandler;
        _refreshTokenHandler = refreshTokenHandler;
    }

    [HttpPost("send-code")]
    [ProducesResponseType(typeof(SendCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendCode([FromBody] SendCodeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
            return BadRequest("Phone number is required");

        var result = await _sendCodeHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] VerifyCodeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
            return BadRequest("Phone number is required");

        if (string.IsNullOrWhiteSpace(command.Code))
            return BadRequest("Verification code is required");

        try
        {
            var result = await _verifyCodeHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return BadRequest("Refresh token is required");

        try
        {
            var result = await _refreshTokenHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
