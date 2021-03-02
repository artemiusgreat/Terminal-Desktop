using Core.CollectionSpace;
using Core.EnumSpace;
using System.Linq;
using System.Threading.Tasks;

namespace Core.ModelSpace
{
  /// <summary>
  /// Definition
  /// </summary>
  public interface IProcessorModel : IStateModel
  {
    /// <summary>
    /// List of charts
    /// </summary>
    IIndexCollection<IChartModel> Charts { get; set; }

    /// <summary>
    /// List of accounts operated by the strategy
    /// </summary>
    IIndexCollection<IGatewayModel> Gateways { get; set; }

    /// <summary>
    /// Initialize user variables
    /// </summary>
    Task OnLoad();

    /// <summary>
    /// Dispose resources
    /// </summary>
    Task OnUnload();
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public abstract class ProcessorModel : StateModel, IProcessorModel
  {
    /// <summary>
    /// List of charts
    /// </summary>
    public virtual IIndexCollection<IChartModel> Charts { get; set; }

    /// <summary>
    /// List of accounts operated by the strategy
    /// </summary>
    public virtual IIndexCollection<IGatewayModel> Gateways { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ProcessorModel()
    {
      Charts = new IndexCollection<IChartModel>();
      Gateways = new IndexCollection<IGatewayModel>();
    }

    /// <summary>
    /// Prepare strategy variables
    /// </summary>
    public virtual Task OnLoad()
    {
      return Task.FromResult(0);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public virtual Task OnUnload()
    {
      return Task.FromResult(0);
    }

    /// <summary>
    /// Set initial state
    /// </summary>
    public override Task Connect()
    {
      Disconnect();
      OnLoad();

      StateStream.OnNext(StatusEnum.Connection);

      foreach (var gateway in Gateways)
      {
        _connections.Add(gateway);

        gateway.Account.Instruments.ForEach(o => o.Value.Account = gateway.Account);
        gateway.Account.Gateway = gateway;
        gateway.Connect();
      }

      return Task.FromResult(0);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public override Task Disconnect()
    {
      Unsubscribe();

      StateStream.OnNext(StatusEnum.Disconnection);

      Gateways.ForEach(o => o.Disconnect());
      Gateways.Clear();
      Charts.Clear();

      _connections.ForEach(o => o.Dispose());
      _connections.Clear();

      return Task.FromResult(0);
    }

    /// <summary>
    /// Pause execution 
    /// </summary>
    /// <param name="message"></param>
    public override Task Unsubscribe()
    {
      Gateways.ForEach(o => o.Unsubscribe());

      return Task.FromResult(0);
    }

    /// <summary>
    /// Resume execution 
    /// </summary>
    /// <param name="message"></param>
    public override Task Subscribe()
    {
      Gateways.ForEach(o => o.Subscribe());

      return Task.FromResult(0);
    }

    /// <summary>
    /// Dispose the strategy and its relations
    /// </summary>
    public override void Dispose()
    {
      Disconnect();
    }
  }
}
