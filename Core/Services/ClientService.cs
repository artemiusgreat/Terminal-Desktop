using Core.ManagerSpace;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Core.ModelSpace
{
  /// <summary>
  /// Definition
  /// </summary>
  public interface IClientService : IDisposable
  {
    /// <summary>
    /// Single instance
    /// </summary>
    HttpClient Client { get; }

    /// <summary>
    /// Send GET request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="queryParams"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<T> Get<T>(
      string source,
      IDictionary<dynamic, dynamic> queryParams = null,
      CancellationTokenSource cts = null) where T : new();

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="queryParams"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<T> Post<T>(
      string source,
      IDictionary<dynamic, dynamic> queryParams = null,
      HttpContent content = null,
      CancellationTokenSource cts = null) where T : new();
  }

  /// <summary>
  /// Service to track account changes, including equity and quotes
  /// </summary>
  public class ClientService : IClientService
  {
    /// <summary>
    /// HTTP client instance
    /// </summary>
    protected HttpClient _client = new HttpClient();

    /// <summary>
    /// Logger instance
    /// </summary>
    public HttpClient Client => _client;

    /// <summary>
    /// Send GET request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="queryParams"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<T> Get<T>(
      string source,
      IDictionary<dynamic, dynamic> queryParams = null,
      CancellationTokenSource cts = null) where T : new()
    {
      var response = await Send(source, queryParams, HttpMethod.Get, null, cts);

      if (response == null)
      {
        return default;
      }

      return ConversionManager.Deserialize<T>(response);
    }

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="queryParams"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<T> Post<T>(
      string source,
      IDictionary<dynamic, dynamic> queryParams = null,
      HttpContent content = null,
      CancellationTokenSource cts = null) where T : new()
    {
      var response = await Send(source, queryParams, HttpMethod.Post, content, cts);

      if (response == null)
      {
        return default;
      }
      
      return ConversionManager.Deserialize<T>(response);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
      _client.Dispose();
    }

    /// <summary>
    /// Generic query sender
    /// </summary>
    /// <param name="source"></param>
    /// <param name="queryParams"></param>
    /// <param name="queryType"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    protected async Task<string> Send(
      string source,
      IDictionary<dynamic, dynamic> queryParams = null,
      HttpMethod queryType = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      var query = string.Empty;
      var cancellation = cts == null ? default : cts.Token;
      var inputs = HttpUtility.ParseQueryString(string.Empty);

      if (queryParams != null)
      {
        foreach (var item in queryParams)
        {
          inputs.Add($"{ item.Key }", $"{ item.Value }");
        }

        query = inputs.ToString();
      }

      var message = new HttpRequestMessage
      {
        Content = content,
        Method = queryType,
        RequestUri = new Uri(source + "?" + query)
      };

      var response = await _client.SendAsync(message, cancellation);

      if (response.Content == null)
      {
        return null;
      }

      return await response.Content.ReadAsStringAsync();
    }
  }
}
