using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using APIWebScraping.Models;
using Microsoft.AspNetCore.Authorization;

namespace APIWebScraping.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class Controlador : ControllerBase
    {
        private readonly WebScrapingService _webScrapingService;


        public Controlador(WebScrapingService webScrapingService)
        {
            _webScrapingService = webScrapingService;
        }

        [HttpGet("Ofac/{companyName}")]
        public async Task<ActionResult> ofacSearchSelenium(string companyName)
        {
            var Companies = await _webScrapingService.OFACSearchCompany(companyName);

            if (Companies == null || Companies.Count == 0)
            {
                return NotFound("No se encontraron compañías.");
            }

            if (!_webScrapingService.EsValidaLlamada())
            {
                return StatusCode(429, "Se ha alcanzado el límite de llamadas por minuto");
            }

            int count = Companies.Count;

            return Ok(Companies);

        }


        //[HttpGet("Offshore/{companyName}")]
        //public IActionResult offshoreSearchSelenium(string companyName)
        //{
        //    var companyNames = _webScrapingService.OffshoreSearchCompany(companyName);

        //    if (companyNames.Count == 0)
        //    {
        //        return NotFound("No se encontraron compañías. de offshore");
        //    }
        //    if (!_webScrapingService.EsValidaLlamada())
        //    {
        //        return StatusCode(429, "Se ha alcanzado el límite de llamadas por minuto");
        //    }
        //    int count = companyNames.Count;


        //    return Ok(new
        //    {
        //        Companies = companyNames.Select(name => new Company { Name = name, HitCount = count-- }).ToList()
        //    });
        //}



        [HttpGet("{nombre}")]
        public async Task<ActionResult> ObtenerInformacionEntidad(string nombre)
        {
            // Validaciones
            if (string.IsNullOrEmpty(nombre))
            {
                return BadRequest("Nombre de entidad no proporcionado");
            }

            if (!_webScrapingService.EsValidaLlamada())
            {
                return StatusCode(429, "Se ha alcanzado el límite de llamadas por minuto");
            }

            try
            {
                // Realizar la búsqueda en la fuente Offshore Leaks Database
                var resultados = await _webScrapingService.BuscarEnOffshoreLeaks(nombre);

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener información: {ex.Message}");
            }
        }

        [HttpGet("worldbank/{nombre}")]
        public async Task<ActionResult> ObtenerInformacionEntidadWorld(string nombre)
        {
            if (!_webScrapingService.EsValidaLlamada())
            {
                return StatusCode(429, "Se ha alcanzado el límite de llamadas por minuto");
            }
            try
            {

                // Realizar la búsqueda en la fuente Offshore Leaks Database
                var resultados = await _webScrapingService.ScrapeWorldBankData(nombre);
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

    }
}
