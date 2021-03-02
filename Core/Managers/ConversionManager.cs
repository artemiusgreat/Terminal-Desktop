using Core.ModelSpace;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Core.ManagerSpace
{
  /// <summary>
  /// JSON serialization options
  /// </summary>
  public class ConversionOptions : JsonSerializerSettings
  {
  }

  /// <summary>
  /// Helper class for the most common conversion operations
  /// </summary>
  public class ConversionManager
  {
    /// <summary>
    /// Compare double values
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    public static bool Compare(double? v1, double? v2, double epsilon = double.Epsilon)
    {
      return Math.Abs(v1.Value - v2.Value) < epsilon;
    }

    /// <summary>
    /// Parse T
    /// </summary>
    /// <param name="content"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T To<T>(dynamic content, T value = default)
    {
      if (content == null)
      {
        return value;
      }

      try
      {
        return Convert.ChangeType(content, typeof(T));
      }
      catch (Exception e)
      {
        InstanceManager<LogService>.Instance.Log.Error(e.Message);
      }

      return value;
    }

    /// <summary>
    /// Get collection property
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    public static T Take<T>(dynamic collection, params dynamic[] props)
    {
      try
      {
        dynamic value = default;

        foreach (var i in props)
        {
          value = value == null ? collection[i] : value[i];
        }

        return Convert.ChangeType(value, typeof(T));
      }
      catch (Exception e)
      {
        InstanceManager<LogService>.Instance.Log.Error(e.Message);
      }

      return default;
    }

    /// <summary>
    /// Round value to specified interval
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="span"></param>
    /// <returns></returns>
    public static DateTime? Cut(DateTime? dateTime, TimeSpan? span)
    {
      if (dateTime == null || span == null)
      {
        return null;
      }

      return new DateTime(dateTime.Value.Ticks - (dateTime.Value.Ticks % span.Value.Ticks), dateTime.Value.Kind);
    }

    /// <summary>
    /// Serialize to JSON
    /// </summary>
    /// <param name="data"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string Serialize(object data, ConversionOptions options = null)
    {
      return JsonConvert.SerializeObject(data, options);
    }

    /// <summary>
    /// Deserialize from JSON
    /// </summary>
    /// <param name="data"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static T Deserialize<T>(string data, ConversionOptions options = null)
    {
      try
      {
        return JsonConvert.DeserializeObject<T>(data, options);
      }
      catch (Exception e)
      {
        InstanceManager<LogService>.Instance.Log.Error(e.Message);
      }

      return default;
    }

    /// <summary>
    /// Deserialize stream
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static T Deserialize<T>(Stream stream)
    {
      try
      {
        using (var reader = new StreamReader(stream))
        using (var content = new JsonTextReader(reader))
        {
          return new JsonSerializer().Deserialize<T>(content);
        }
      }
      catch (Exception e)
      {
        InstanceManager<LogService>.Instance.Log.Error(e.Message);
      }

      return default;
    }

    /// <summary>
    /// Encode as Base64
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static byte[] Bytes(string message)
    {
      return Encoding.UTF8.GetBytes(message);
    }

    /// <summary>
    /// Convert dictionary to URL params
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public static string GetQuery(IDictionary<dynamic, dynamic> query)
    {
      var inputs = HttpUtility.ParseQueryString(string.Empty);

      if (query is IEnumerable)
      {
        foreach (var item in query)
        {
          inputs.Add($"{ item.Key }", $"{ item.Value }");
        }
      }

      return $"{ inputs }";
    }

    /// <summary>
    /// Sign with SHA256
    /// </summary>
    /// <param name="message"></param>
    /// <param name="secret"></param>
    /// <returns></returns>
    public static string Sha256(string message, string secret)
    {
      using (var algo = new HMACSHA256(Bytes(secret)))
      {
        return algo
          .ComputeHash(Bytes(message))
          .Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2}", b), (sb) => sb.ToString());
      }
    }

    /// <summary>
    /// Sign with SHA256
    /// </summary>
    /// <param name="message"></param>
    /// <param name="secret"></param>
    /// <returns></returns>
    public static string Sha384(string message, string secret)
    {
      using (var algo = new HMACSHA384(Bytes(secret)))
      {
        return algo
          .ComputeHash(Bytes(message))
          .Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2}", b), (sb) => sb.ToString());
      }
    }

    /// <summary>
    /// Sign with SHA512
    /// </summary>
    /// <param name="message"></param>
    /// <param name="secret"></param>
    /// <returns></returns>
    public static string Sha512(string message, string secret)
    {
      using (var algo = new HMACSHA512(Bytes(secret)))
      {
        return algo
          .ComputeHash(Bytes(message))
          .Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2}", b), (sb) => sb.ToString());
      }
    }
  }
}
