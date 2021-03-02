using Core.EnumSpace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Gateway.Oanda
{
  public class InputPointPrice
  {
    [JsonProperty("liquidity")]
    public double Size { get; set; }

    [JsonProperty("price")]
    public double Price { get; set; }
  }

  public class InputPoint
  {
    [JsonProperty("tradeable")]
    public bool Tradeable { get; set; }

    [JsonProperty("closeoutBid")]
    public double Bid { get; set; }

    [JsonProperty("closeoutAsk")]
    public double Ask { get; set; }

    [JsonProperty("instrument")]
    public string Instrument { get; set; } = string.Empty;

    [JsonProperty("time")]
    public DateTime Time { get; set; }

    [JsonProperty("bids")]
    public List<InputPointPrice> Bids { get; set; }

    [JsonProperty("asks")]
    public List<InputPointPrice> Asks { get; set; }
  }
}
