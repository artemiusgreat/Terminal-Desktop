using Core.ModelSpace;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

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
  }
}
