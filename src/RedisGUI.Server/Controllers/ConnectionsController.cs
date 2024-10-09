using MediatR;
using Microsoft.AspNetCore.Mvc;
using RedisGUI.Application.Connections.CreateConnection;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Server.Controllers
{
	[Route("api/v{v:apiVersion}/[controller]")]
	[ApiController]
	public class ConnectionsController : ControllerBase
	{
		private readonly ISender _sender;

		public ConnectionsController(ISender sender) => _sender = sender;

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var query = new GetConnectionByIdQuery(id);
			var result = await _sender.Send(query);
			if (result.IsSuccess)
			{
				return Ok(result);
			}

			return BadRequest(result.Error);
		}
		
		[HttpPost]
		public async Task<IActionResult> CreateConnection([FromBody] CreateConnectionRequest request)
		{
			var createConnectionCommand = new CreateConnectionCommand(
				request.Name,
				request.Host,
				request.Port,
				request.Database,
				request.UserName, request.Password);

			return await this._sender.Send<Result<Guid>>(createConnectionCommand)
				.Match<Guid, IActionResult>(result => Ok(result), BadRequest);
		}
	}
}
