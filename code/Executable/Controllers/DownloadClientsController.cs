using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Factory;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

/// <summary>
/// Controller for managing individual download clients
/// </summary>
[ApiController]
[Route("api/clients")]
public class DownloadClientsController : ControllerBase
{
    private readonly ILogger<DownloadClientsController> _logger;
    private readonly IConfigManager _configManager;
    private readonly IDownloadClientFactory _clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadClientsController"/> class
    /// </summary>
    public DownloadClientsController(
        ILogger<DownloadClientsController> logger,
        IConfigManager configManager,
        IDownloadClientFactory clientFactory)
    {
        _logger = logger;
        _configManager = configManager;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Gets all download clients
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClients()
    {
        try
        {
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            return Ok(config.Clients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download clients");
            return StatusCode(500, new { Error = "An error occurred while retrieving download clients" });
        }
    }

    /// <summary>
    /// Gets a specific download client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(string id)
    {
        try
        {
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            var client = config.GetClientConfig(id);
            if (client == null)
            {
                return NotFound(new { Message = $"Client with ID '{id}' not found" });
            }

            return Ok(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download client {id}", id);
            return StatusCode(500, new { Error = "An error occurred while retrieving the download client" });
        }
    }

    /// <summary>
    /// Adds a new download client
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] ClientConfig clientConfig)
    {
        try
        {
            // Validate the new client configuration
            clientConfig.Validate();

            // Get the current configuration
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                config = new DownloadClientConfig
                {
                    Clients = new List<ClientConfig>()
                };
            }

            // Check if a client with the same ID already exists
            if (config.GetClientConfig(clientConfig.Id) != null)
            {
                return BadRequest(new { Error = $"A client with ID '{clientConfig.Id}' already exists" });
            }

            // Add the new client
            config.Clients.Add(clientConfig);

            // Persist the updated configuration
            var result = await _configManager.SaveDownloadClientConfigAsync(config);
            if (!result)
            {
                return StatusCode(500, new { Error = "Failed to save download client configuration" });
            }

            _logger.LogInformation("Added new download client: {name} ({id})", clientConfig.Name, clientConfig.Id);
            return CreatedAtAction(nameof(GetClient), new { id = clientConfig.Id }, clientConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding download client");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing download client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(string id, [FromBody] ClientConfig clientConfig)
    {
        try
        {
            // Ensure the ID in the route matches the ID in the body
            if (id != clientConfig.Id)
            {
                return BadRequest(new { Error = "Client ID in the URL does not match the ID in the request body" });
            }

            // Validate the updated client configuration
            clientConfig.Validate();

            // Get the current configuration
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            // Find the client to update
            var existingClientIndex = config.Clients.FindIndex(c => c.Id == id);
            if (existingClientIndex == -1)
            {
                return NotFound(new { Message = $"Client with ID '{id}' not found" });
            }

            // Update the client
            config.Clients[existingClientIndex] = clientConfig;

            // Persist the updated configuration
            var result = await _configManager.SaveDownloadClientConfigAsync(config);
            if (!result)
            {
                return StatusCode(500, new { Error = "Failed to save download client configuration" });
            }

            _logger.LogInformation("Updated download client: {name} ({id})", clientConfig.Name, clientConfig.Id);
            return Ok(clientConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating download client {id}", id);
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a download client
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(string id)
    {
        try
        {
            // Get the current configuration
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            // Find the client to delete
            var existingClientIndex = config.Clients.FindIndex(c => c.Id == id);
            if (existingClientIndex == -1)
            {
                return NotFound(new { Message = $"Client with ID '{id}' not found" });
            }

            // Remove the client
            config.Clients.RemoveAt(existingClientIndex);

            // Persist the updated configuration
            var result = await _configManager.SaveDownloadClientConfigAsync(config);
            if (!result)
            {
                return StatusCode(500, new { Error = "Failed to save download client configuration" });
            }

            _logger.LogInformation("Deleted download client with ID: {id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting download client {id}", id);
            return StatusCode(500, new { Error = "An error occurred while deleting the download client" });
        }
    }

    /// <summary>
    /// Tests connection to a download client
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConnection(string id)
    {
        try
        {
            // Get the client configuration
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            var clientConfig = config.GetClientConfig(id);
            if (clientConfig == null)
            {
                return NotFound(new { Message = $"Client with ID '{id}' not found" });
            }

            // Ensure the client is initialized
            try
            {
                // Get the client instance
                var client = _clientFactory.GetClient(id);
                
                // Try to login
                await client.LoginAsync();
                
                _logger.LogInformation("Successfully connected to download client: {name} ({id})", clientConfig.Name, id);
                return Ok(new { Success = true, Message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to download client: {name} ({id})", clientConfig.Name, id);
                return Ok(new { Success = false, Message = $"Connection failed: {ex.Message}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to download client {id}", id);
            return StatusCode(500, new { Error = "An error occurred while testing the connection" });
        }
    }
    
    /// <summary>
    /// Gets all clients of a specific type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetClientsByType(DownloadClientType type)
    {
        try
        {
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                return NotFound(new { Message = "No download client configuration found" });
            }

            var clients = config.Clients.Where(c => c.Type == type).ToList();
            return Ok(clients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download clients of type {type}", type);
            return StatusCode(500, new { Error = "An error occurred while retrieving download clients" });
        }
    }
}
