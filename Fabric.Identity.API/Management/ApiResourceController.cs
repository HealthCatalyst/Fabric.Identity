using System;
using Fabric.Identity.API.CouchDb;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class ApiResourceController : Controller
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public ApiResourceController(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ApiResource Get(string id)
        {
            try
            {
                var apiResource = _documentDbService.GetDocument<ApiResource>(id).Result;
                return apiResource;

            }
            catch (Exception)
            {
                _logger.Error($"The specified api resource with id: {id} was not found.");
                throw;
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ApiResource value)
        {
            try
            {
                var id = value.Name;
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to create a new api resource. Error: {e.Message}");
                throw;
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]ApiResource value)
        {
            try
            {
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to update api resource. Error: {e.Message}");
                throw;
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            try
            {
                _documentDbService.DeleteDocument<ApiResource>(id);
            }
            catch (Exception)
            {
                _logger.Error($"Unable to delete api resource with id: {id}");
                throw;
            }
        }
    }
}
