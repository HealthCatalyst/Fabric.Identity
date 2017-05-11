using System;
using Fabric.Identity.API.CouchDb;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Quickstart
{
    [Route("api/[controller]")]
    public class ClientController : Controller
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public ClientController(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public Client Get(string id)
        {
            try
            {
                var client = _documentDbService.GetDocument<Client>(id).Result;
                return client;

            }
            catch (Exception)
            {
                _logger.Error($"The specified client with id: {id} was not found.");
                throw;
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]Client value)
        {
            try
            {
                var id = value.ClientId;
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to create a new client. Error: {e.Message}");
                throw;
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]Client value)
        {
            try
            {
                _documentDbService.AddOrUpdateDocument(id, value);
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to update client. Error: {e.Message}");
                throw;
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            try
            {
                _documentDbService.DeleteDocument<Client>(id);
            }
            catch (Exception)
            {
                _logger.Error($"Unable to delete client with id: {id}");
                throw;
            }
            
        }               
    }
}
