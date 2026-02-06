//using fn_backend.DTO;
//using fs_backend.Repositories;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Authentication.JwtBearer;

//namespace fs_backend.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]


//public class TypeServicesController : ControllerBase
//{
//    private readonly ITypeServiceService _typeServiceService;

//    public TypeServicesController(ITypeServiceService typeServiceService)
//    {
//        _typeServiceService = typeServiceService;
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetTypeServices()
//    {
//        var typeServices = await _typeServiceService.GetTypeServicesAsync();
//        return Ok(typeServices);
//    }

//    [HttpGet("{id}")]
//    public async Task<IActionResult> GetTypeServiceById(int id)
//    {
//        var typeService = await _typeServiceService.GetTypeServiceByIdAsync(id);
//        if (typeService == null)
//        {
//            return NotFound(new { message = "Tipo de servicio no encontrado" });
//        }

//        return Ok(typeService);
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreateTypeService(TypeServiceDto dto)
//    {
//        var result = await _typeServiceService.CreateTypeServiceAsync(dto);
//        if (!result.Succeeded)
//        {
//            return BadRequest(result.Errors);
//        }

//        return CreatedAtAction(nameof(GetTypeServiceById), new { id = result.Data!.Id }, result.Data);
//    }

//    [HttpPut("{id}")]
//    public async Task<IActionResult> UpdateTypeService(int id, TypeServiceDto dto)
//    {
//        var result = await _typeServiceService.UpdateTypeServiceAsync(id, dto);
//        if (!result.Succeeded)
//        {
//            return NotFound(result.Errors);
//        }

//        return Ok(new { message = "Tipo de servicio actualizado exitosamente" });
//    }

//    [HttpDelete("{id}")]
//    public async Task<IActionResult> DeleteTypeService(int id)
//    {
//        var result = await _typeServiceService.DeleteTypeServiceAsync(id);
//        if (!result.Succeeded)
//        {
//            return BadRequest(result.Errors);
//        }

//        return Ok(new { message = "Tipo de servicio eliminado exitosamente" });
//    }
//}