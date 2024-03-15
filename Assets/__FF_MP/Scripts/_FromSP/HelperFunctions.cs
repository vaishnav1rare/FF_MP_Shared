using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class HelperFunctions
{
    public static float GetDistance(Vector3 _a, Vector3 _b) => Vector3.Distance(_a, _b);

    public static Vector3 GetDirection(Vector3 _a, Vector3 _b) => _b - _a;

    public static Vector3 GetDirectionNormalized(Vector3 _a, Vector3 _b) => (_b - _a).normalized;

    public static Enums.Direction GetGlobalDirection(Vector3 _direction, float _accuracy = 0.98f)
    {
        if (Vector3.Dot(Vector3.forward, _direction) >= _accuracy) return Enums.Direction.Forward;
        else if (Vector3.Dot(Vector3.back, _direction) >= _accuracy) return Enums.Direction.Backward;
        else if (Vector3.Dot(Vector3.left, _direction) >= _accuracy) return Enums.Direction.Left;
        else if (Vector3.Dot(Vector3.right, _direction) >= _accuracy) return Enums.Direction.Right;
        else return Enums.Direction.None;
    }

    public static Vector3 GetGlobalVectorDirection(Vector3 _direction, float _accuracy = 0.98f)
    {
        if (Vector3.Dot(Vector3.forward, _direction) >= _accuracy) return Vector3.forward;
        else if (Vector3.Dot(Vector3.back, _direction) >= _accuracy) return Vector3.back;
        else if (Vector3.Dot(Vector3.left, _direction) >= _accuracy) return Vector3.left;
        else if (Vector3.Dot(Vector3.right, _direction) >= _accuracy) return Vector3.right;
        else return _direction;
    }

    public static Vector3 ToVector3(this Enums.Direction _direction)
    {
        Vector3 _result = Vector3.zero; ;
        switch (_direction)
        {
            case Enums.Direction.Forward:
                _result = Vector3.forward;
                break;
            case Enums.Direction.Backward:
                _result = Vector3.back;
                break;
            case Enums.Direction.Left:
                _result = Vector3.left;
                break;
            case Enums.Direction.Right:
                _result = Vector3.right;
                break;
        }
        return _result;
    }




    public static string ToTimerString(int _value)
    {
        System.TimeSpan t = System.TimeSpan.FromSeconds(_value);
        return t.Hours == 0 ? string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds) : string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
    }




    public static string FormatNumber(float _value)
    {
        if (_value < 1000) return _value.ToString();

        if (_value < 10000) return string.Format("{0:#,.##}K", _value - 5);

        if (_value < 100000) return string.Format("{0:#,.#}K", _value - 50);

        if (_value < 1000000) return string.Format("{0:#,.}K", _value - 500);

        if (_value < 10000000) return string.Format("{0:#,,.##}M", _value - 5000);

        if (_value < 100000000) return string.Format("{0:#,,.#}M", _value - 50000);

        if (_value < 1000000000) return string.Format("{0:#,,.}M", _value - 500000);

        return string.Format("{0:#,,,.##}B", _value - 5000000);
    }


    public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }



    public static float GetAbs(float _value) => _value < 0 ? -_value : _value;
    public static int GetAbs(int _value) => _value < 0 ? -_value : _value;



    public static string ToJSONString(string _key, string _value)
    {
        JObject _object = new JObject { new JProperty(_key, _value) };
        return JsonConvert.SerializeObject(_object);
    }
    public static string ToJSONString(string[] _keys, object[] _values)
    {
        JObject _object = new JObject();
        for (int i = 0; i < _keys.Length; i++)
            _object.Add(new JProperty(_keys[i], _values[i]));
        return JsonConvert.SerializeObject(_object);
    }



    public static JObject ToJSONObject(string _key, string _value) => new JObject { new JProperty(_key, _value) };

    public static JObject ToJSONObject(string[] _keys, object[] _values)
    {
        JObject _object = new JObject();
        for (int i = 0; i < _keys.Length; i++)
            _object.Add(new JProperty(_keys[i], _values[i]));
        return _object;
    }


}
