using Core.ManagerSpace;
using Core.ModelSpace;
using System;

namespace Client.StrategySpace
{
  public class BaseStrategy : ProcessorModel
  {
    /// <summary>
    /// Genrate output based on input data
    /// </summary>
    protected virtual IPointModel Parse(dynamic input)
    {
      var props = input.Split(" ");

      long.TryParse(props[0], out long dateTime);

      double.TryParse(props[1], out double bid);
      double.TryParse(props[2], out double bidSize);
      double.TryParse(props[3], out double ask);
      double.TryParse(props[4], out double askSize);

      var response = new PointModel
      {
        Time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dateTime),
        Ask = ask,
        Bid = bid,
        Last = ask,
        AskSize = askSize,
        BidSize = bidSize
      };

      if (ConversionManager.Equals(askSize, 0))
      {
        response.Last = bid;
      }

      return response;
    }
  }
}
