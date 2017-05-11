using System;
using Fabric.Identity.API.CouchDb;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class IdentityResourceController : Controller
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public IdentityResourceController(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IdentityResource Get(string id)
        {
            try
            {
                var identityResource = _documentDbService.GetDocument<IdentityResource>(id).Result;
                return identityResource;

            }
            catch (Exception)
            {
                _logger.Error($"The specified identity resource with id: {id} was not found.");
                throw;
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]IdentityResource value)
        {
            try
            {
                var id = value.Name;
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to create a new identity resource. Error: {e.Message}");
                throw;
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]IdentityResource value)
        {
            try
            {
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to update identity resource. Error: {e.Message}");
                throw;
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            try
            {
                _documentDbService.DeleteDocument<IdentityResource>(id);
            }
            catch (Exception)
            {
                _logger.Error($"Unable to delete identity resource with id: {id}");
                throw;
            }
        }
    }
}
