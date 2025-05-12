
using cw9cwcw.Exceptions;
using cw9cwcw.Models.DTo;
using cw9cwcw.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace cw9cwcw.Controllers{
    
    [ApiController]
    [Route("api/warehouse")]
    public class WarehouseController : ControllerBase
    {
        private readonly IDbService _dbService;
        public WarehouseController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseProductDto dto)
        {
            try
            {
                var id = await _dbService.AddProductToWarehouseAsync(dto);
                return Ok(new { id });
            }
            catch (NotFoundException ex) { return NotFound(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(ex.Message); }
            catch (Exception) { return StatusCode(500, "Unexpected error"); }
        }
        
        
        [HttpGet("test-db")]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                using var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True");
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT 1", connection);
                var result = await command.ExecuteScalarAsync();

                return Ok("Połączenie działa, wynik: " + result.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Brak połączenia z bazą: " + ex.Message);
            }
        }

    }
    
    
}