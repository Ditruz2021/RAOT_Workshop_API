using ExampleApi.Dto;
using ExampleApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService Service;

        public UsersController(IUserService Service)
        {
            this.Service = Service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] string? Keyword = null, [FromQuery] int Page = 1, [FromQuery] int PageSize = 10)
        {
            var Result = await Service.GetPageAsync(Keyword ?? "", Page, PageSize);
            return Ok(Result);
        }


        [HttpGet("{Id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int Id)
        {
            var Result = await Service.GetByIdAsync(Id);
            return Result is null ? NotFound() : Ok(Result);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateRequest Request)
        {
            var Created = await Service.CreateAsync(Request);
            return CreatedAtAction(nameof(GetById), new { id = Created.UserId }, Created);
        }


        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update([FromRoute] int Id, [FromBody] UserUpdateRequest Request)
        {
            var Updated = await Service.UpdateAsync(Id, Request);
            return Updated is null ? NotFound() : Ok(Updated);
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int Id)
        {
            var ok = await Service.DeleteAsync(Id);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("role")]
        public async Task<IActionResult> GetRole()
        {
            var Result = await Service.GetDDL();
            return Ok(Result);
        }
    }
}
