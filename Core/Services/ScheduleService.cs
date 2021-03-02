using System.Reactive.Concurrency;

namespace Core.ModelSpace
{
  /// <summary>
  /// Asynchronous single-threaded scheduler
  /// </summary>
  public interface IScheduleService
  {
    /// <summary>
    /// Instance
    /// </summary>
    public EventLoopScheduler Scheduler { get; }
  }

  /// <summary>
  /// Asynchronous single-threaded scheduler
  /// </summary>
  public class ScheduleService : IScheduleService
  {
    /// <summary>
    /// Logger instance
    /// </summary>
    public EventLoopScheduler Scheduler { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleService() => Scheduler = new EventLoopScheduler();
  }
}
