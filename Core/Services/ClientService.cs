using Core.ManagerSpace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Core.ModelSpace
{
  /// <summary>
  /// HTTP service
  /// </summary>
  public interface IClientService : IDisposable
  {
    /// <summary>
    /// Max execution time
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Instance
    /// </summary>
    HttpClient Client { get; }

    /// <summary>
    /// Send GET request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<T> Get<T>(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<T> Post<T>(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      HttpContent content = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Stream HTTP content
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<Stream> Stream(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      CancellationTokenSource cts = null);
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
    /// Max execution time
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Logger instance
    /// </summary>
    public HttpClient Client => _client;

    /// <summary>
    /// Send GET request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<T> Get<T>(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      CancellationTokenSource cts = null)
    {
      return ConversionManager.Deserialize<T>(await Send(HttpMethod.Get, source, query, headers, null, cts).ConfigureAwait(false));
    }

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<T> Post<T>(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      return ConversionManager.Deserialize<T>(await Send(HttpMethod.Post, source, query, headers, content, cts).ConfigureAwait(false));
    }

    /// <summary>
    /// Stream HTTP content
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="headers"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<Stream> Stream(
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      CancellationTokenSource cts = null)
    {
      using (var client = new HttpClient())
      {
        var cancellation = cts == null ? CancellationToken.None : cts.Token;

        if (headers is IEnumerable)
        {
          foreach (var item in headers)
          {
            client.DefaultRequestHeaders.Add($"{ item.Key }", $"{ item.Value }");
          }
        }

        return await client
          .GetStreamAsync(source + "?" + ConversionManager.GetQuery(query), cancellation)
          .ConfigureAwait(false);
      }
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
    /// <param name="queryType"></param>
    /// <param name="source"></param>
    /// <param name="query"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    protected async Task<Stream> Send(
      HttpMethod queryType,
      string source,
      IDictionary<dynamic, dynamic> query = null,
      IDictionary<dynamic, dynamic> headers = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      _client.Timeout = Timeout;

      var message = new HttpRequestMessage
      {
        Content = content,
        Method = queryType,
        RequestUri = new Uri(source + "?" + ConversionManager.GetQuery(query))
      };

      if (headers is IEnumerable)
      {
        foreach (var item in headers)
        {
          message.Headers.Add($"{ item.Key }", $"{ item.Value }");
        }
      }

      if (cts == null)
      {
        cts = new CancellationTokenSource(Timeout);
      }

      HttpResponseMessage response = null;

      try
      {
        response = await _client
          .SendAsync(message, cts.Token)
          .ConfigureAwait(false);
      }
      catch (Exception e)
      {
        InstanceManager<LogService>.Instance.Log.Error(e.Message);
        return null;
      }

      return await response
        .Content
        .ReadAsStreamAsync(cts.Token)
        .ConfigureAwait(false);
    }
  }
}
