using Microsoft.AspNetCore.Mvc;

namespace ZWave.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class InfoController : ControllerBase
{
    private readonly ILogger<InfoController> _logger;

    private readonly Driver _driver;

    public InfoController(
        ILogger<InfoController> logger,
        Driver driver)
    {
        _logger = logger;
        _driver = driver;
    }

    [HttpGet]
    public object Get()
    {
        var controller = _driver.Controller;
        return new
        {
            HomeId = controller.HomeId,
            ControllerNodeId = controller.NodeId,
            SerialApiVersion = controller.SerialApiVersion,
            SerialApiRevision = controller.SerialApiRevision,
            ManufacturerId = controller.ManufacturerId,
            ProductType = controller.ProductType,
            ProductId = controller.ProductId,
            SupportedCommandIds = controller.SupportedCommandIds,
            LibraryVersion = controller.LibraryVersion,
            LibraryType = controller.LibraryType,
            SucNodeId = controller.SucNodeId,
            ApiVersion = controller.ApiVersion,
            ChipType = controller.ChipType,
            ChipVersion = controller.ChipVersion,
            IsPrimary = controller.IsPrimary,
            NodeIds = controller.Nodes.Select(pair => pair.Value.Id).ToList(),
        };
    }
}
